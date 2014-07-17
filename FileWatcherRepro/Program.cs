using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Config;

namespace FileWatcherRepro
{
    class Program
    {

        static void Main(string[] args)
        {
            BasicConfigurator.Configure();

            var watcher = new FileWatcherRx();

            while(true)
            {
                Console.WriteLine("Press any key to create a file");
                Console.ReadKey(true);
                Task.Factory.StartNew(WriteFileSlow);
            }
        }

        private static void WriteFileQuick()
        {
            File.WriteAllBytes(Path.Combine("temp", Guid.NewGuid() + ".bin"), new Byte[100000]);
        }

        private static void WriteFileSlow()
        {
            using (var stream = File.Open(Path.Combine("temp", Guid.NewGuid() + ".bin"), FileMode.CreateNew))
            {
                stream.Write(new Byte[50000], 0, 50000);
                stream.Flush(true);
                Task.Delay(TimeSpan.FromMilliseconds(5000)).Wait();
                stream.Write(new Byte[50000], 0, 50000);
            }
        }
    }
}
