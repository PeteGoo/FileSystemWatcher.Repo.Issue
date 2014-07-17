using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using log4net;
using log4net.Repository.Hierarchy;

namespace FileWatcherRepro
{
    public class FileWatcherRx : IDisposable
    {
        private FileSystemWatcher _watcher;
        private IDisposable _events;
        private const string DropPath = "temp";
        private const string ErrorFilePath = "failed";
        private const string SuccessFilePath = "complete";
        private readonly ConcurrentDictionary<string, long> _processing = new ConcurrentDictionary<string, long>();

        private static readonly ILog Logger = LogManager.GetLogger(typeof (Program));

        public FileWatcherRx()
        {
            _watcher = new FileSystemWatcher(DropPath)
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _events = GetFileWatcherEventStream() // A merged list of all different file created/changed/renamed events 
                .Merge(Observable.Interval(TimeSpan.FromMinutes(5))
                    .SelectMany(Directory.GetFiles("temp").ToObservable()
                    .Do(path => Logger.Info(string.Format("Detected change in file '{0}' when interval checking", path))))) // Merge with an interval check
                .GroupBy(i => i)
                .SelectMany(g => g.Throttle(TimeSpan.FromSeconds(7)))
                .Subscribe(ProcessFile);
        }

        private IObservable<string> GetFileWatcherEventStream()
        {
            return Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                h => _watcher.Created += h,
                h => _watcher.Created -= h)
                .Select(x => new { 
                    ChangeType = x.EventArgs.ChangeType,
                    FilePath = x.EventArgs.FullPath})
                .Merge(
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => _watcher.Changed += h,
                        h => _watcher.Changed -= h)
                        .Select(x => new
                        {
                            ChangeType = x.EventArgs.ChangeType,
                            FilePath = x.EventArgs.FullPath
                        }))
                .Merge(
                    Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                        h => _watcher.Renamed += h,
                        h => _watcher.Renamed -= h)
                        .Select(x => new
                        {
                            ChangeType = x.EventArgs.ChangeType,
                            FilePath = x.EventArgs.FullPath
                        }))
                .Do(x => Logger.Info(string.Format("Detected Change -  Type: {0}, File: {1}", x.ChangeType, x.FilePath)))
                .Select(x => x.FilePath);
        }

        private void ProcessFile(string fullPath)
        {
            var fileLength = new FileInfo(fullPath).Length;
            if (_processing.TryAdd(fullPath, fileLength))
            {
                Logger.Info(string.Format("Processing File: FullPath: {0}: Size: {1}",
                    fullPath,
                    fileLength));

                try
                {
                    // Do Stuff

                    Logger.Info(string.Format("Processed File: FullPath: {0}: Size: {1}",
                        fullPath,
                        fileLength));

                    MoveFile(fullPath, SuccessFilePath);
                }
                catch (Exception e)
                {
                    Logger.Error(string.Format("Failed to process file: FullPath: {0}: Size: {1}",
                        fullPath,
                        fileLength), e);
                }

                long previousFileLength;

                _processing.TryRemove(fullPath, out previousFileLength);
            }
            else
            {
                Logger.Error(string.Format(
                    "An changed event on a file was not processed as the file was already being processed. File: {0}, Previous Length: {1}, Updated Length: {2}", 
                    fullPath, 
                    _processing[fullPath], 
                    fileLength));

                MoveFile(fullPath, ErrorFilePath);
            }
        }

        private void MoveFile(string sourcePath, string targetDirectoryPath)
        {
            //TODO: Retry
            if (!Directory.Exists(targetDirectoryPath))
            {
                throw new DirectoryNotFoundException(string.Format("Could not find directory {0}", targetDirectoryPath));
            }
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(string.Format("Could not find source file {0}", sourcePath));
            }
            File.Move(sourcePath, Path.Combine( targetDirectoryPath, Path.GetFileNameWithoutExtension(sourcePath) ?? "unknown.txt"));
        }

        public void Dispose()
        {
            var events = Interlocked.Exchange(ref _events, null);
            if (events != null)
            {
                events.Dispose();
            }
        }
    }
}