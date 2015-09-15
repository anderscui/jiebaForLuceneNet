using System.Collections.Generic;
using System.IO;
using JiebaNet.Segmenter;
using Lucene.Net.Analysis;

namespace JiebaNet.Integration.LuceneNet
{
    public class JiebaAnalyzer : Analyzer
    {
        protected static readonly ISet<string> DefaultStopWords = StopAnalyzer.ENGLISH_STOP_WORDS_SET;

        private static ISet<string> StopWords;

        static JiebaAnalyzer()
        {
            var stopWordsFile = Path.GetFullPath(JiebaNet.Analyser.ConfigManager.StopWordsFile);
            if (File.Exists(stopWordsFile))
            {
                var lines = File.ReadAllLines(stopWordsFile);
                StopWords = new HashSet<string>();
                foreach (var line in lines)
                {
                    StopWords.Add(line.Trim());
                }
            }
            else
            {
                StopWords = DefaultStopWords;
            }
        }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            var seg = new JiebaSegmenter();
            TokenStream result = new JiebaTokenizer(seg, reader);
            // This filter is necessary, because the parser converts the queries to lower case.
            result = new LowerCaseFilter(result);
            result = new StopFilter(true, result, StopWords);
            return result;
        }
    }
}
