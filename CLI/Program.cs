using NLog;
using System.Text;
using Taffy.Lib;
using Taffy.Lib.Logging;

Logger LOG = LogMaster.GetLogger();
const string DEBUG_PATH = @"K:\creative materials";

LOG.Info(">>>>> Scan Directories");
var monitor = new FolderMonitor(DEBUG_PATH);
monitor.Scan().Wait();

LOG.Info(">>>>> Test Search");
var sb = new StringBuilder("found files:\n");
var count = 0;
foreach (var f in monitor.Search("mountain lake")) {
  sb.AppendLine($"> {f.Name}");
  count++;
}
sb.Append($"total {count} files match");

LOG.Info(sb.ToString());
