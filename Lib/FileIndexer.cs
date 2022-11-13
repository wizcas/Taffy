using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Taffy.Lib {
  internal class FileIndexer : IDisposable {
    const LuceneVersion VER = LuceneVersion.LUCENE_48;
    const string INDEX_NAME = "taffy/file_index";
    static readonly string INDEX_PATH = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), INDEX_NAME);

    Lucene.Net.Store.Directory? _dir;

    public void Dispose() {
      _dir?.Dispose();
      _dir = null;
    }

    public IndexWriter NewWriter() {
      Dispose();

      Console.WriteLine(INDEX_PATH);
      _dir = FSDirectory.Open(INDEX_PATH);
      var analyzer = new CJKAnalyzer(VER);
      var config = new IndexWriterConfig(VER, analyzer);
      config.OpenMode = OpenMode.CREATE;
      return new IndexWriter(_dir, config);
    }
  }
}
