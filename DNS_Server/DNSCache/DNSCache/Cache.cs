using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace DNSCache
{
    class Cache
    {
        private Dictionary<string, byte[]> cache = new Dictionary<string, byte[]>();
        private Dictionary<string, int> ttls = new Dictionary<string, int>();
        private Timer cleaner;
        private int timerElapsedSeconds = 2;
        private object locker = new object();

        public Cache()
        {
            ReadCache();
            cleaner = new Timer(timerElapsedSeconds * 1000);
            cleaner.Elapsed += Cleaner_Elapsed;
            cleaner.Start();
        }

        public void ReadCache()
        {
            if (!Directory.Exists("Cache"))
                return;
            var time = long.Parse(File.ReadAllLines("Cache/time.txt")[0]);
            var oldDateTime = DateTime.FromFileTime(time);
            var now = DateTime.Now;

            if (!Directory.Exists("Cache/Info"))
            {
                var dir = new DirectoryInfo("Cache/Info");
                dir.Create();
            }

            if (!Directory.Exists("Cache/ttls"))
            {
                var dir = new DirectoryInfo("Cache/ttls");
                dir.Create();
            }

            lock (locker)
            {
                foreach (var file in Directory.GetFiles("Cache/Info"))
                {
                    cache.Add((new FileInfo(file).Name).Split(".")[0], File.ReadAllBytes(file));
                }
            }

            lock (locker)
            {
                foreach (var file in Directory.GetFiles("Cache/ttls"))
                {
                    ttls.Add((new FileInfo(file).Name).Split(".")[0], int.Parse(File.ReadAllLines(file)[0]));
                }
            }

            clean(now, oldDateTime);
        }

        private void clean(DateTime now, DateTime old)
        {
            foreach (var key in cache.Keys)
            {
                ttls[key] -= (int)((now - old).TotalSeconds);
                if (ttls[key] <= 0)
                {
                    cache.Remove(key);
                    ttls.Remove(key);
                }
            }
        }

        public void StopWork()
        {
            cleaner.Stop();
            if (Directory.Exists("Cache"))
            {
                DirectoryInfo dirInfo = new DirectoryInfo("Cache");
                dirInfo.Delete(true);
            }
            DirectoryInfo dir = new DirectoryInfo("Cache");
            dir.Create();
            dir = new DirectoryInfo("Cache/Info");
            dir.Create();
            dir = new DirectoryInfo("Cache/ttls");
            dir.Create();

            lock (locker)
            {
                foreach (var key in cache.Keys)
                {
                    File.WriteAllBytes("Cache/Info/" + key + ".txt", cache[key]);
                    File.WriteAllText("Cache/ttls/" + key + ".txt", ttls[key].ToString());
                }
                File.WriteAllText("Cache/time.txt", DateTime.Now.ToFileTime() + "");
            }
        }

        private void Cleaner_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (locker)
            {
                string[] keys = new string[ttls.Keys.Count];
                ttls.Keys.CopyTo(keys, 0);

                foreach (var key in keys)
                {
                    ttls[key] -= timerElapsedSeconds;
                    if (ttls[key] <= 0)
                    {
                        cache.Remove(key);
                        ttls.Remove(key);
                    }
                }
            }
        }

        private string getKey(byte[] key)
        {
            var strKeys = key.Select(x => x + "").ToArray();
            return String.Join("", strKeys);
        }

        public bool Contains(byte[] hostAndType)
        {
            var result = cache.ContainsKey(getKey(hostAndType));
            return result;
        }

        public byte[] getAnswer(byte[] hostAndType)
        {
            return cache[getKey(hostAndType)];
        }

        public void add(byte[] hostAndType, byte[] data, int ttl)
        {
            lock (locker)
            {
                cache.Add(getKey(hostAndType), data);
                ttls.Add(getKey(hostAndType), ttl);
            }
        }
    }
}
