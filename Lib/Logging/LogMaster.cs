using NLog;
using System.Diagnostics;

namespace Taffy.Lib.Logging {
  public static class LogMaster {
    static LogMaster() {
      LogManager.Setup().LoadConfiguration(builder => {
        builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToConsole();
        //builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(fileName: "file.txt");
      });
    }
    public static Logger GetLogger() {
      return LogManager.GetCurrentClassLogger();
    }
    public static PerfContext Perf(this Logger logger, string tag) {
      var context = new PerfContext(Stopwatch.StartNew(), logger, tag);
      return context;
    }
  }

  public record PerfContext(Stopwatch Timer, Logger Logger, string Tag) : IDisposable {
    private int _stop = 0;
    public TimeSpan Elapsed => Timer.Elapsed;

    public void Pause() => Timer.Stop();
    public void Resume() => Timer.Start();
    public void Reset() => Timer.Reset();
    public void Restart() => Timer.Restart();

    public void Dispose() {
      Timer.Stop();
    }

    public void Log(string? stepNote = null) {
      Logger.Info("[PERF] {what}#{stop} - {$step} elapsed: {ms} ms",
        Tag,
        _stop++,
        string.IsNullOrWhiteSpace(stepNote) ? null : $"({stepNote})",
        Elapsed.Milliseconds);
    }
  }
}
