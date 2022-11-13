using System.Diagnostics;

namespace Taffy.Lib {
  public class FolderMonitor {

    public string Path { get; private set; }
    public bool Enable {
      get { return _watcher.EnableRaisingEvents; }
      set { _watcher.EnableRaisingEvents = value; }
    }

    FileSystemWatcher _watcher;

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
      Stopwatch sw = new Stopwatch();
      sw.Start();
      var metas = await Task.Run(() => WalkFiles());
      sw.Stop();
      foreach (var meta in metas) {
        Console.WriteLine($"[{meta.Ext}] {meta.FullName}");
      }
      Console.WriteLine($"[DEBUG] time elapsed: {sw.ElapsedMilliseconds}ms");
    }
    #endregion

    #region File system operations
    IEnumerable<FileMeta> WalkFiles() {

      var di = new DirectoryInfo(Path);
      if (!di.Exists) yield break;

      var fsis = di.EnumerateFileSystemInfos("*.*", new EnumerationOptions() {
        MatchType = MatchType.Simple,
        RecurseSubdirectories = true,
      });
      foreach (var fsi in fsis) {
        if (fsi.Exists) {
          var meta = new FileMeta(fsi.FullName) { Ext = fsi.Extension, LastUpdated = fsi.LastWriteTimeUtc, LastAccessed = fsi.LastAccessTimeUtc };
          yield return meta;
        }
      }
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
}