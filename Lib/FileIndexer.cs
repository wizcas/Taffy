using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Taffy.Lib {
  internal class FileIndexer : IDisposable {
    const LuceneVersion VER = LuceneVersion.LUCENE_48;

    internal string? FolderPath { get; set; }

    internal string? IndexPath => !string.IsNullOrWhiteSpace(FolderPath) ? Path.Combine(FolderPath, ".taffy", "index") : null;

    LuceneDirectory _dir;
    IndexWriter _writer;
    IndexReader _reader;
    IndexSearcher _searcher;

    internal IndexWriter Writer => _writer;
    internal IndexSearcher Searcher => _searcher;

    internal FileIndexer(string path) {
      FolderPath = path;
      var analyzer = new CJKAnalyzer(VER);
      var config = new IndexWriterConfig(VER, analyzer);
      config.OpenMode = OpenMode.CREATE;

      _dir = Open();
      _writer = new IndexWriter(_dir, config);
      _reader = _writer.GetReader(true);
      _searcher = new IndexSearcher(_reader);
    }

    ~FileIndexer() {
      Dispose();
    }

    LuceneDirectory Open() {
      if (IndexPath == null)
        throw new InvalidOperationException("must specify a folder to index.");

      Console.WriteLine(IndexPath);
      return FSDirectory.Open(IndexPath);
    }

    public void Dispose() {
      _reader.Dispose();
      _writer.Dispose();
      _dir.Dispose();
    }

    public void Commit() {
      Writer.Commit();
    }
  }
}
