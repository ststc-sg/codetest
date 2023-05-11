using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
namespace ResourceCollection
{
    
    public class BackgroundServer
    {
        private readonly HttpListener _listener;

        private readonly CachedFileHandler _handler;

        public BackgroundServer(HttpResources sources, IExternalHttpRequestHandler callback,string prefix)
        {
            _handler=new CachedFileHandler(sources, callback);
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            _listener.BeginGetContext(ListenerCallback, this);

        }
        public void ListenerCallback(IAsyncResult result) {
            // Call EndGetContext to complete the asynchronous operation.
            try {
                if (_listener.IsListening) {
                    var context = _listener.EndGetContext(result);
                    var task = _handler.Handle(context);
                    _listener.BeginGetContext(ListenerCallback, this); //recharge to next request
                }
            }
            catch (ObjectDisposedException e) {
                // ignored
            }
            catch (Exception e) {
                throw;
            }
        }

        public void Close()
        {
            _listener.Close();
        }
    }
    public interface IExternalHttpRequestHandler
    {
        byte []  HandleHttpRequest(string collection,IDictionary<string, string> evt);
        byte []  HandleHttpProtobufRequest(string collectionname,byte [] data);
    }
    public class CachedFileHandler
    {
        private readonly IExternalHttpRequestHandler _callback;
        private readonly HttpResources _sources;

        public CachedFileHandler(HttpResources sources, IExternalHttpRequestHandler callback)
        {
            _sources = sources;
            _callback = callback;
        }
        private static async Task Transfer(IResourceInfo info,Stream outputStream)
        {
            var resource = info.Resource;
            try {
                await outputStream.WriteAsync(resource, 0, resource.Length);
            }
            catch (Exception e) {
                
            }
            //using (var inputthread = new MemoryStream(Resource))
            //await inputthread.CopyToAsync(outputStream);
        }
        public async Task Handle(HttpListenerContext context)
        {
            var response = context.Response;
            var result = await ProcessRequest(context);
            if (result?.Resource != null)
            {
                // Get a response stream and write the response to it.
                response.ContentLength64 = result.Resource.LongLength;
                response.ContentType = result.Mime;
                response.AddHeader("Cache-Control",
                    result.IsStatic ? "max-age=3600" : "no-cache, no-store, must-revalidate");
                response.AddHeader("Access-Control-Allow-Origin", "*");

                await Transfer(result,response.OutputStream);
            }
            else
            {
                if (context.Request.HttpMethod == "PUT") {
                    response.StatusCode = (int)HttpStatusCode.NoContent;
                }
                else
                    response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            response.Close();
        }

        public Task<IResourceInfo> ProcessRequest(HttpListenerContext context)
        {

            var request = context.Request;
            var uri = request.Url;
            var pathpart = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
            if (request.HttpMethod == "POST")
            {
                var result = HandlePostAndPutRequest(pathpart, request);
                if (result != null)
                    return Task.FromResult<IResourceInfo>(new ResourceInfo(result, ResourceInfo.MimeByName(pathpart)));
            }
            else if (request.HttpMethod == "PUT") {
                var result=HandlePostAndPutRequest(pathpart, request);
                return Task.FromResult<IResourceInfo>(new ResourceInfo(new byte[] {}, ResourceInfo.MimeByName(pathpart)));
            }
            else
            {
                var resource = _sources.Search(pathpart); //search exact name
                if (resource == null)
                {
                    var pos = pathpart.LastIndexOf('/');
                    if (pos != -1)
                    {
                        var shortpath = pathpart.Substring(pos); //search name without workplace(for staticresources)
                        resource = _sources.Search(shortpath);
                    }
                }
                return Task.FromResult(resource);
            }
            return Task.FromResult<IResourceInfo>(null);
        }

        private Dictionary<string, string> ParseKeyValueJson(HttpListenerRequest input)
        {
            var collection = new Dictionary<string, string>();
            using (var body = input.InputStream) // here we have data
            {
                using (var reader = new StreamReader(body, input.ContentEncoding))
                {
                    var url = reader.ReadToEnd();
                    var s = Uri.UnescapeDataString(url);
                    var query = HttpUtility.ParseQueryString(s);
                    for (var i = 0; i < query.Count; i++)
                    {
                        var key = query.Keys[i];
                        var value = query[key];
                        collection.Add(key, value);
                    }
                }
            }
            return collection;
        }

        private byte [] HandlePostAndPutRequest(string path, HttpListenerRequest request) //this request can contains command
        {
            if (_callback == null)
                return null;
            
            try
            {
                string wpo;
                string subSystem;
                ParseUrl(path, out wpo, out subSystem);
                subSystem = subSystem.ToLower(CultureInfo.InvariantCulture);                    
                if (path.EndsWith(".protobin"))
                {
                    using (var stream = new MemoryStream())
                    {
                        request.InputStream.CopyTo(stream);
                        stream.Seek(0, SeekOrigin.Begin);   //rewind to start
                        return _callback.HandleHttpProtobufRequest(subSystem, stream.ToArray());    
                    }
                    
                }
                else
                {
                    var collection = ParseKeyValueJson(request);
                    return _callback.HandleHttpRequest(subSystem,collection);
                }
            }
            catch (Exception e)
            {
            }

            return null;
        }
        private void ParseUrl(string path, out string wpo, out string subSystem) {
            wpo = null;
            subSystem = null;
            {
                var searchstring = "/wpo";
                var fistBreakStart = path.IndexOf(searchstring,StringComparison.CurrentCultureIgnoreCase);
                if (fistBreakStart != -1) {
                    fistBreakStart += searchstring.Length;
                    var fistBreakPos = path.IndexOf('/', fistBreakStart);
                    if (fistBreakPos != -1) {
                        wpo = path.Substring(fistBreakStart, fistBreakPos - fistBreakStart);
                    }
                }
            }
            {
                var lastSlash = path.LastIndexOf('/');
                if (lastSlash != -1) {
                    var endsystemName = path.IndexOf('.', lastSlash);
                    if (endsystemName != -1) {
                        lastSlash++;
                        subSystem = path.Substring(lastSlash, endsystemName - lastSlash);
                    }
                    else {
                        subSystem = path.Substring(lastSlash);
                    }
                }
            }
            
        }
    }

    
}