using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NLog;
using Taffy.Lib.Logging;

namespace Taffy.Lib {
  public class FolderMonitor {
    readonly static Logger LOG = LogMaster.GetLogger();

    public string Path { get; private set; }
    public bool Enable {
      get { return _watcher.EnableRaisingEvents; }
      set { _watcher.EnableRaisingEvents = value; }
    }

    readonly FileSystemWatcher _watcher;

    bool _initialized = false;

    public FolderMonitor(string path) {
      Path = path;

      _watcher = new FileSystemWatcher(path, "*.*") {
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.FileName |
                      NotifyFilters.DirectoryName |
                      NotifyFilters.Attributes |
                      NotifyFilters.LastAccess |
                      NotifyFilters.LastWrite |
                      NotifyFilters.Size,
      };
      _watcher.Changed += OnChanged;
      _watcher.Created += OnCreated;
      _watcher.Deleted += OnDeleted;
      _watcher.Renamed += OnRenamed;
      _watcher.Error += OnError;
    }

    #region Public APIs
    public async Task Scan() {
      _initialized = false;
      if (Path == null) return;

      var files = await Task.Run(() => WalkFiles());
      LOG.Debug("total {count} files are scanned in directory {dir}", files.Count(), Path);
      using var indexer = new FileIndexer(Path, reset: true);
      foreach (var f in files) {
        var doc = new FileDocument(f);
        doc.Write(indexer.Writer);
      }
      indexer.Commit();
      _initialized = true;
    }

    public IEnumerable<FileInfo> Search(params string[] terms) {
      if (!_initialized)
        throw new MonitorNotInitializedException(this);

      var query = new TermQuery(new Term("name", string.Join(" ", terms.Select(t => t.ToLower()))));
      using var perf = LOG.Perf($"search {query}");
      using var indexer = new FileIndexer(Path);
      var result = indexer.Searcher.Search(query, 20);
      var count = result.TotalHits;
      perf.Log("search completed");
      for (int i = 0; i < count; i++) {
        yield return new FileInfo(indexer.Searcher.Doc(result.ScoreDocs[i].Doc).Get("fullname"));
      }
      perf.Log("file info returned");
    }
    #endregion

    #region File system operations
    IEnumerable<FileInfo> WalkFiles() {
      using var perf = LOG.Perf($"walk {Path}");
      var di = new DirectoryInfo(Path!);
      if (!di.Exists) yield break;

      var enumOpts = new EnumerationOptions() {
        MatchType = MatchType.Simple,
        RecurseSubdirectories = true,
      };
      var files = di.EnumerateFiles("*.*", enumOpts);
      foreach (var f in files) {
        if (f != null && f.Exists) {
          yield return f;
        }
      }
      perf.Log();
    }
    #endregion

    #region Event handlers
    void OnChanged(object sender, FileSystemEventArgs e) {
      if (e.ChangeType == WatcherChangeTypes.Changed) {
        Console.WriteLine("changed: ", e.FullPath);
      }
    }
    void OnCreated(object sender, FileSystemEventArgs e) {
      Console.WriteLine("created: ", e.FullPath);
    }
    void OnDeleted(object sender, FileSystemEventArgs e) {
      Console.WriteLine("deleted: ", e.FullPath);
    }
    void OnRenamed(object sender, RenamedEventArgs e) {
      Console.WriteLine("renamed: ", e.FullPath);
    }
    void OnError(object sender, ErrorEventArgs e) {
      PrintException(e.GetException());
    }

    void PrintException(Exception? err) {
      if (err == null) return;
      var stderr = Console.Error;
      stderr.WriteLine(err.Message);
      stderr.WriteLine("Stacktrace: ");
      stderr.WriteLine(err.StackTrace);
      stderr.WriteLine();

      PrintException(err.InnerException);
    }
    #endregion
  }

  public record FileMeta(string FullName) {
    public string Ext { get; init; } = "";
    public DateTime LastUpdated { get; init; }
    public DateTime LastAccessed { get; init; }
  };

  public record FileDocument(FileInfo f) {
    public void Write(IndexWriter w) {
      var doc = new Document();
      doc.Add(new StringField("fullname", f.FullName, Field.Store.YES));
      doc.Add(new TextField("name", Path.GetFileNameWithoutExtension(f.Name), Field.Store.YES));
      doc.Add(new StringField("ext", f.Extension, Field.Store.YES));
      w.AddDocument(doc);
    }

  }

  public class MonitorNotInitializedException : Exception {
    public MonitorNotInitializedException(FolderMonitor monitor) : base(monitor.Path) { }
  }
}