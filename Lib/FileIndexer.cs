using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Taffy.Lib {
  internal class FileIndexer {
    const LuceneVersion VER = LuceneVersion.LUCENE_48;

    internal string? FolderPath { get; set; }

    internal string? IndexPath => !string.IsNullOrWhiteSpace(FolderPath) ? Path.Combine(FolderPath, ".taffy", "index") : null;

    internal FileIndexer(string path) {
      FolderPath = path;
    }

    public LuceneDirectory Open() {
      if (IndexPath == null)
        throw new InvalidOperationException("must specify a folder to index.");

      Console.WriteLine(IndexPath);
      return FSDirectory.Open(IndexPath);
    }

    public IndexWriter NewWriter(LuceneDirectory dir) {
      var analyzer = new CJKAnalyzer(VER);
      var config = new IndexWriterConfig(VER, analyzer);
      config.OpenMode = OpenMode.CREATE;
      return new IndexWriter(dir, config);
    }
  }
}
