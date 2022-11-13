using Lucene.Net.Documents;
using Lucene.Net.Index;
using System.Diagnostics;

namespace Taffy.Lib {
  public class FolderMonitor {

    public string Path { get; private set; }
    public bool Enable {
      get { return _watcher.EnableRaisingEvents; }
      set { _watcher.EnableRaisingEvents = value; }
    }

    readonly FileSystemWatcher _watcher;
    readonly Stopwatch _stopwatch = new Stopwatch();

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
      var files = await Task.Run(() => WalkFiles());
      Console.WriteLine($"{files.Count()} files found");
      using var indexer = new FileIndexer();
      using var writer = indexer.NewWriter();
      foreach (var f in files) {
        var doc = new FileDocument(f);
        doc.Write(writer);
      }
      writer.Commit();
      writer.Dispose();
    }
    #endregion

    #region File system operations
    IEnumerable<FileInfo> WalkFiles() {
      _stopwatch.Restart();
      var di = new DirectoryInfo(Path);
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
      _stopwatch.Stop();
      Console.WriteLine($"[DEBUG] scan all files time elapsed: {_stopwatch.ElapsedMilliseconds}ms");
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
      doc.Add(new TextField("name", f.Name, Field.Store.YES));
      w.AddDocument(doc);
    }

  }
}