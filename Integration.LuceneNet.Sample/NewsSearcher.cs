using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JiebaNet.Segmenter;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace JiebaNet.Integration.LuceneNet.Sample
{
    public static class NewsSearcher
    {
        private static readonly string LuceneDir = "lucene_index";
        private static FSDirectory _directoryTemp;

        private static FSDirectory Directory
        {
            get
            {
                if (_directoryTemp == null)
                {
                    _directoryTemp = FSDirectory.Open(new DirectoryInfo(LuceneDir));
                }
                if (IndexWriter.IsLocked(_directoryTemp))
                {
                    IndexWriter.Unlock(_directoryTemp);
                }

                var lockFilePath = Path.Combine(LuceneDir, "write.lock");
                if (File.Exists(lockFilePath))
                {
                    File.Delete(lockFilePath);
                }

                return _directoryTemp;
            }
        }

        private static Analyzer GetAnalyzer()
        {
            return new JiebaAnalyzer();
        }

        #region Add & Update Index

        private static void AddToLuceneIndex(News data, IndexWriter writer)
        {
            // remove older index entry
            var searchQuery = new TermQuery(new Term("Id", data.Id.ToString()));
            writer.DeleteDocuments(searchQuery);

            // add new index entry
            var doc = new Document();
            doc.Add(new Field("Id", data.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Title", data.Title, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("Content", data.Content, Field.Store.YES, Field.Index.ANALYZED));

            writer.AddDocument(doc);
        }

        public static void UpdateLuceneIndex(IEnumerable<News> data)
        {
            //var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            var analyzer = GetAnalyzer();

            using (var writer = new IndexWriter(Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                // replaces older entry if any
                foreach (var sd in data)
                {
                    AddToLuceneIndex(sd, writer);
                }

                analyzer.Close();
            }
        }

        public static void UpdateLuceneIndex(News data)
        {
            UpdateLuceneIndex(new[] { data });
        }

        #endregion

        #region Clear Index

        public static void ClearLuceneIndexRecord(int recordId)
        {
            //var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            var analyzer = GetAnalyzer();
            using (var writer = new IndexWriter(Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                var searchQuery = new TermQuery(new Term("Id", recordId.ToString()));
                writer.DeleteDocuments(searchQuery);

                analyzer.Close();
            }
        }

        public static bool ClearLuceneIndex()
        {
            try
            {
                //var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                var analyzer = GetAnalyzer();
                using (var writer = new IndexWriter(Directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    writer.DeleteAll();

                    analyzer.Close();
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Optimize Index

        public static void OptimizeLuceneIndex()
        {
            //var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            var analyzer = GetAnalyzer();
            using (var writer = new IndexWriter(Directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                analyzer.Close();
                writer.Optimize();
            }
        }

        #endregion

        #region Mappers

        private static News MapDataToModel(Document doc)
        {
            return new News()
            {
                Id = int.Parse(doc.Get("Id")),
                Title = doc.Get("Title"),
                Content = doc.Get("Content"),
            };
        }

        private static IEnumerable<News> MapLuceneToDataList(IEnumerable<Document> hits)
        {
            return hits.Select(MapDataToModel).ToList();
        }

        private static IEnumerable<News> MapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            return hits.Select(hit => MapDataToModel(searcher.Doc(hit.Doc))).ToList();
        }

        #endregion

        #region Search

        private static string GetKeyWordsSplitBySpace(string keywords, JiebaTokenizer tokenizer)
        {
            var result = new StringBuilder();

            var words = tokenizer.Tokenize(keywords);

            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word.Word))
                {
                    continue;
                }

                result.AppendFormat("{0} ", word.Word);
            }

            return result.ToString().Trim();
        }

        private static Query ParseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException pe)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim() + "*"));
            }

            return query;
        }

        private static IEnumerable<News> SearchQuery(string searchQuery, string searchField = "")
        {
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", "")))
            {
                return new List<News>();
            }

            using (var searcher = new IndexSearcher(Directory, false))
            {
                var hitsLimit = 1000;
                //var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                var analyzer = GetAnalyzer();

                if (!string.IsNullOrEmpty(searchField))
                {
                    var parser = new QueryParser(Version.LUCENE_30, searchField, analyzer);
                    var query = ParseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, hitsLimit).ScoreDocs;
                    var results = MapLuceneToDataList(hits, searcher);

                    analyzer.Dispose();
                    return results;
                }
                else
                {
                    var parser = new MultiFieldQueryParser(Version.LUCENE_30, new[] { "Id", "Title", "Content" }, analyzer);
                    var query = ParseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, null, hitsLimit, Sort.RELEVANCE).ScoreDocs;
                    var results = MapLuceneToDataList(hits, searcher);

                    //QueryParser queryParser = new QueryParser(Version.LUCENE_30, "Content", analyzer);
                    //Query query = queryParser.Parse(searchQuery);

                    //QueryParser titleQueryParser = new QueryParser(Version.LUCENE_30, "Title", analyzer);
                    //Query titleQuery = titleQueryParser.Parse(searchQuery);

                    //BooleanQuery bq = new BooleanQuery();
                    //bq.Add(query, Occur.SHOULD);
                    //bq.Add(titleQuery, Occur.SHOULD);

                    //var hits = searcher.Search(bq, null, hitsLimit, Sort.RELEVANCE);
                    //var results = MapLuceneToDataList(hits.ScoreDocs, searcher);

                    analyzer.Close();
                    return results;
                }
            }
        }

        public static IEnumerable<News> Search(string input, string fieldName = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return new List<News>();
            }

            var kwords = input;
            kwords = GetKeyWordsSplitBySpace(kwords, new JiebaTokenizer(new JiebaSegmenter(), kwords));

            var terms = kwords.Trim().Replace("-", " ").Split(' ')
                .Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim() + "*");
            input = string.Join(" ", terms);

            return SearchQuery(input, fieldName);
        }

        public static IEnumerable<News> SearchDefault(string input, string fieldName = "")
        {
            return string.IsNullOrEmpty(input) ? new List<News>() : SearchQuery(input, fieldName);
        }

        #endregion

        /// <summary>
        /// All the data indexed.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<News> GetAllData()
        {
            if (!System.IO.Directory.EnumerateFiles(LuceneDir).Any())
            {
                return new List<News>();
            }

            var searcher = new IndexSearcher(Directory, false);
            var reader = IndexReader.Open(Directory, false);
            var docs = new List<Document>();
            var term = reader.TermDocs();

            while (term.Next())
            {
                docs.Add(searcher.Doc(term.Doc));
            }

            reader.Dispose();
            searcher.Dispose();
            return MapLuceneToDataList(docs);
        }
    }
}