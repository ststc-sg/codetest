using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace ResourceCollection
{
    public class LazyZipDirectoryLoader : IResourceSource
    {
        private readonly string _innerpath;
        private readonly ZipFile _zf;
        public LazyZipDirectoryLoader(string zipfilepath, string password, string innerpath)
        {
            _innerpath = innerpath;
            _zf=new ZipFile(zipfilepath)
            {
                Password=password
            };
        }
        private readonly ISet<string> _failedrequest = new SortedSet<string>();
        public async Task<byte[]> TryLoad(string filepath)
        {
            

            try
            {
                var urilocator = _innerpath+"/"+(filepath.StartsWith("/") ? filepath.Substring(1) : filepath); //drop starting /
                
                if (_failedrequest.Contains(urilocator))
                    return null; //stop search not exists files
                var path = Uri.UnescapeDataString(urilocator);

                var entry = _zf.GetEntry(path);
                
                if (entry == null)
                {
                    _failedrequest.Add(urilocator);
                    return null;
                }
                
                var time = entry.DateTime;
                var zipStream = _zf.GetInputStream(entry);
                var bufer = new byte[entry.Size];
                var result = await zipStream.ReadAsync(bufer, 0, bufer.Length);
                zipStream.Dispose();
                return bufer;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return null;         
        }

        public event FileSystemEventHandler OnResourceChanged;
    }
    public class LazyDirectoryResourceLoader : IResourceSource
    {
        private readonly ISet<string> _failedrequest = new SortedSet<string>();
        private readonly string _rootdir;
        readonly FileSystemWatcher _watcher = new FileSystemWatcher();
        public LazyDirectoryResourceLoader(string rootdir) {
            _watcher.Path = rootdir;
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.IncludeSubdirectories = true;
            _watcher.Changed += OnChanged;
            _watcher.EnableRaisingEvents = true;
            _rootdir = rootdir;
        }
        static async Task<byte[]> ReadAllFileAsync(string filename) {
            try
            {
                using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true)) {
                    var buff = new byte[file.Length];
                    await file.ReadAsync(buff, 0, (int)file.Length);
                    return buff;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            
            return null;
        }
        public async Task<byte[]> TryLoad(string filepath)
        {
            var urilocator = filepath.StartsWith("/") ? filepath.Substring(1) : filepath; //drop starting /

            try
            {
                if (_failedrequest.Contains(filepath))
                    return null; //stop search not exists files
                var baseuri = new Uri(_rootdir + "/", UriKind.Absolute);
                var test = new Uri(baseuri, urilocator);
                var path = Uri.UnescapeDataString(test.AbsolutePath);

                if(!File.Exists(path))
                {
                    _failedrequest.Add(path);
                    return null;
                }
                return await ReadAllFileAsync(path);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return null;
        }
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            OnResourceChanged?.Invoke(source,e);
        }
        public event FileSystemEventHandler OnResourceChanged;
    }
}