using System;
using JiebaNet.Segmenter;

namespace JiebaNet.Integration.LuceneNet.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            TestNewsData();
        }

        private static void TestNewsData()
        {
            var seg = new JiebaSegmenter();
            seg.AddWord("机器学习");

            NewsSearcher.ClearLuceneIndex();

            var data = NewsRepository.GetAll();
            NewsSearcher.UpdateLuceneIndex(data);

            var results = NewsSearcher.Search("进");
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }
        }
    }
}
