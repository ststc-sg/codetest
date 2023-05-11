using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;

namespace ResourceCollection
{
    public interface IHttpResources
    {
        IResourceInfo Search(string pathpart);
        void AddSource(IResourceSource source);
    }
    public class HttpResources : IHttpResources
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<string, ResourceInfo> _dynamicCollection = new Dictionary<string, ResourceInfo>();
        private readonly ICollection<IResourceSource> _sources = new List<IResourceSource>();
        private readonly IDictionary<string, ResourceInfo> _staticCollection = new Dictionary<string, ResourceInfo>();
        private readonly Dictionary<string,string> _relocation=new Dictionary<string, string>();
        private Action<string> NewFileFloaded;
        public IResourceInfo Search(string name)
        {

            var result = SearchDynamic(name);

            if (result == null)
                result = SearchStatic(name);

            return result;
        }

        private ResourceInfo SearchDynamic(string name)
        {
            ResourceInfo result;
            lock (_dynamicCollection)
            {
                _dynamicCollection.TryGetValue(name.ToLower(), out result);
            }
            return result;
        }

        private string SearchRelocation(string name) {
            string result;
            _relocation.TryGetValue(name.ToLower(),out result);
            return result;
        }

        private void AddRelocation(string name, string realname) {
            _relocation.Add(name.ToLower(), realname);
        }
        private ResourceInfo DirectSearchStatic(string name)
        {
            ResourceInfo result;
            lock (_staticCollection) //found exact sentence in static collection
            {
                _staticCollection.TryGetValue(name.ToLower(), out result);
            }
            return result;
        }

        private byte[] TryLoadFromSources(string cleanedname)
        {
            lock (_sources) {
                foreach (var source in _sources) {
                    var task = source.TryLoad(cleanedname);
                    task.Wait();
                    if (task.Result != null)
                        return task.Result;
                    
                }
            }
            return null;
        }
        private ResourceInfo SearchStatic(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (!name.StartsWith("/"))
            {
                throw new Exception("searching malformed string");
            }

            if (name.StartsWith("/wpo", StringComparison.InvariantCultureIgnoreCase)) {
                var sequencepos = name.IndexOf("/", 1, StringComparison.Ordinal);
                name = name.Substring(sequencepos);//drop wpo part for static data
            }
            var result = DirectSearchStatic(name);
            if (result != null)
                return result; //exactly this resource found
            var relocation = SearchRelocation(name);
            if (relocation != null)
                return DirectSearchStatic(relocation);  //return using existing relocation
            //try to remove workplace parameter before path and search again
            var sourcecontent = TryLoadFromSources(name);
            if (sourcecontent != null)
            {
                NewFileFloaded?.Invoke(name);
            }
            return sourcecontent!=null ? UploadStatic(name, sourcecontent) : null;
        }
        public void RemoveResource(string path) {
            lock (_dynamicCollection)
            {
                _dynamicCollection.Remove(path.ToLower());
            }
        }
        public ResourceInfo Upload(string path, byte[] resource, bool isStatic) {
            
            var resBind = new ResourceInfo(resource, ResourceInfo.MimeByName(path)) {IsStatic = isStatic};
            if (isStatic)
                lock (_staticCollection)
                {
                    _staticCollection[path.ToLower()] = resBind;
                }
            else
                lock (_dynamicCollection)
                {
                    _dynamicCollection[path.ToLower()] = resBind;
                }
            return resBind;
        }

        public ResourceInfo UploadStatic(string path, byte[] resource) => Upload(path, resource, true);
        public void AddSource(IResourceSource source)
        {
            source.OnResourceChanged += OnChanged;
            lock (_sources) {
                _sources.Add(source);
            }
        }
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            var filename = "/"+e.Name.Replace('\\', '/');
            lock (_staticCollection) {
                var key = _staticCollection.Keys.FirstOrDefault(v =>string.Compare(v, filename, StringComparison.CurrentCultureIgnoreCase)==0);
                if (key != null) {
                    var sourcecontent = TryLoadFromSources(key);
                    if (sourcecontent != null)
                        UploadStatic(key, sourcecontent);
                }
            }
            Logger.Trace("$File changed: {e.Name}");
        }


        public void Preload(string preloadpath)
        {
            try
            {
                if (!File.Exists(preloadpath))
                {
                    Logger.Error($"preload file {preloadpath} not found");
                    return;
                }
                var filenamestoPrefetch = File.ReadLines(preloadpath);
                foreach (var line in filenamestoPrefetch)
                {
                    var sourcecontent = TryLoadFromSources(line);
                    if (sourcecontent != null)
                        UploadStatic(line, sourcecontent);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void RememberNewFiles(string filepath)
        {
            NewFileFloaded += (piecetoappend) =>
            {
                using (var file = File.AppendText(filepath))
                {
                    file.WriteLine(piecetoappend);
                }
            };
        }
    }
}