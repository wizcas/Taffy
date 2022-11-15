using J2N;
using Taffy.Lib;

const string DEBUG_PATH = @"K:\creative materials";

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var monitor = new FolderMonitor(DEBUG_PATH);
monitor.Scan().Wait();

Console.WriteLine($"<{DateTime.Now.GetMillisecondsSinceUnixEpoch()}>\n===== [Test Search] =====\n");
foreach (var f in monitor.Search("Yellow")) {
  Console.WriteLine(f.Name);
}
