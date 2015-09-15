### jiebaForLuceneNet
Integrate jieba.NET segmenter with Lucene.NET.

### 一、若对Lucene.NET不甚熟悉，请先看：

#### Lucene.NET的基本用法

* 看codeproject上的文章：[Lucene.Net ultra fast search for MVC or WebForms site](http://www.codeproject.com/Articles/320219/Lucene-Net-ultra-fast-search-for-MVC-or-WebForms?msg=4643090#xx4643090xx)

#### 如何自定义Tokenizer和Analyzer

* [Lucene.Net – Custom Synonym Analyzer](http://www.codeproject.com/Articles/32201/Lucene-Net-Custom-Synonym-Analyzer)
* https://github.com/JimLiu/Lucene.Net.Analysis.PanGu
* https://github.com/JimLiu/Lucene.Net.Analysis.MMSeg

#### 应用自定义Analyzer

* http://pangusegment.codeplex.com/wikipage?title=PanGu4Lucene

#### 调试：通过Luke来Look Lucene.NET的索引

* http://luke.codeplex.com/releases/view/82033

### 二、jiebaForLuceneNet的使用

* `JiebaAnalyzer`：与Lucene.NET集成的主接口
* `JiebaTokenizer`：为JiebaAnalyzer用于分词
* `Integration.LuceneNet.Sample`项目中有示例，演示如何通过jieba分词添加索引和搜索
