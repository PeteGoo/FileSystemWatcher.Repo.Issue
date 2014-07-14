using System;
using System.IO;

namespace FileWatcherRepro
{
    public class FileWatcherImplementation
    {
        private FileSystemWatcher _watcher;

        public FileWatcherImplementation()
        {
            _watcher = new FileSystemWatcher("temp");
            _watcher.EnableRaisingEvents = true;

            _watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size;

            _watcher.Created += (sender, args) =>
            {
                FileUpdated(args.ChangeType, args.FullPath);
            };

            _watcher.Changed += (sender, args) =>
            {
                FileUpdated(args.ChangeType, args.FullPath);
            };
        }

        private void FileUpdated(WatcherChangeTypes changeType, string fullPath)
        {
            Console.WriteLine("ChangeType: {0}, FullPath: {1}: Size: {2}",
                changeType,
                fullPath,
                new FileInfo(fullPath).Length);
        }
    }
}