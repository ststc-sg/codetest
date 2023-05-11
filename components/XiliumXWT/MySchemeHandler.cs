using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using ResourceCollection;
using Xilium.CefGlue;
using Newtonsoft.Json;
namespace XiliumXWT
{
    [Serializable]
    class postdata {
        public string command;
        public string parameter;
    };
    internal class MySchemeHandlerFactory : CefSchemeHandlerFactory
    {
        private readonly IInternalHttpRequestHandler _callback;
        private readonly IHttpResources _sources;

        public MySchemeHandlerFactory(IHttpResources sources,
            IInternalHttpRequestHandler callback)
        {
            _sources = sources;
            _callback = callback;
        }

        private byte[] HandleProtobufRequest(string path, CefRequest request)
        {
            string wpo;
            string subSystem;
            ParseUrl(path, out wpo, out subSystem);

            var collection = request.PostData;
            byte[] requestdata = null;
            if (collection != null)
            {
                foreach (var elem in collection.GetElements())
                    if (elem.ElementType == CefPostDataElementType.Bytes)
                    {
                        requestdata = elem.GetBytes();
                        break;
                    }
            }
            return _callback.HandleHttpProtobufRequest(subSystem,requestdata);
        }
        private byte[] HandleJsonRequest(string path, CefRequest request)
        {
            try
            {
                string wpo;
                string subSystem;
                Dictionary<string, string> recivedCollection = null;
                ParseUrl(path, out wpo, out subSystem);
                
                var collection = request.PostData;
                if (collection != null)
                {
                    recivedCollection = new Dictionary<string, string>();
                    foreach (var elem in collection.GetElements())
                        if (elem.ElementType == CefPostDataElementType.Bytes)
                        {
                            var payloadType=request.GetHeaderByName("Content-type");
                            if (!string.IsNullOrEmpty(payloadType))
                            {
                                if (payloadType.StartsWith("application/x-www-form-urlencoded"))
                                {
                                    var bytes = elem.GetBytes();
                                    var url = Encoding.UTF8.GetString(bytes);
                                    var s = Uri.UnescapeDataString(url);
                                    var query = HttpUtility.ParseQueryString(s);
                                    for (var i = 0; i < query.Count; i++)
                                    {
                                        var key = query.Keys[i];
                                        var value = query[key];
                                        recivedCollection.Add(key, value);
                                    }
                                }
                                else if (payloadType == "application/json")
                                {
                                    try
                                    {
                                        var bytes = elem.GetBytes();
                                        string requeststring = Encoding.UTF8.GetString(bytes);
                                        var posdata = JsonConvert.DeserializeObject<postdata>(requeststring);
                                        if (posdata != null) { 
                                            recivedCollection.Add("command",posdata.command);
                                            recivedCollection.Add("parameter", posdata.parameter);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        //
                                    } 
                                    
                                    
                                }
                            }
                            
                        }
                    
                }
                return _callback.HandleHttpRequest(subSystem,recivedCollection);
            }
            catch (Exception e)
            {

            }
            return null;
        } 
        private byte [] HandlePostRequest(string path, CefRequest request)
        {
            if (_callback == null)
                return null;
            path = path.ToLowerInvariant();
            if (path.EndsWith(".json"))
                return HandleJsonRequest(path,request);
            if (path.EndsWith(".protobin"))
                return HandleProtobufRequest(path,request);
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
        protected override CefResourceHandler Create(CefBrowser browser, CefFrame frame, string schemeName,
            CefRequest request)
        {
            var uri = new Uri(request.Url);
            var pathpart = uri.AbsolutePath;
            if (request.Method == "POST")
            {
                var result=HandlePostRequest(pathpart, request);
                if(result!=null)
                    return new MySchemeHandler(new ResourceInfo(result, ResourceInfo.MimeByName(pathpart)));
            }
            else if (request.Method == "PUT")
            {
                HandlePostRequest(pathpart, request);
                return new MySchemeHandler(new ResourceInfo(new byte[] {}, ResourceInfo.MimeByName(pathpart)));
            }
            else if (request.Method == "GET")
            {
                var handler = TryResolve(pathpart);
                if (handler == null)
                {
                    var pos = pathpart.LastIndexOf('/');
                    if (pos != -1)
                    {
                        var shortpath = pathpart.Substring(pos); //try to drop path and retry
                        handler = TryResolve(shortpath);
                    }
                }
                return handler;
            }
            return null;
        }

        private CefResourceHandler TryResolve(string pathpart)
        {
            var retval = _sources.Search(pathpart);
            return retval?.Resource?.Length > 0 ? new MySchemeHandler(retval) : null;
        }
    }

    internal class MySchemeHandler : CefResourceHandler
    {
        private readonly IResourceInfo _source;
        private bool _completed;
        private int _transmittedLen;

        public MySchemeHandler(IResourceInfo source){
            _source = source;
        }

        

        protected override bool ProcessRequest(CefRequest request, CefCallback callback){
            callback.Continue();
            _completed = false;
            _transmittedLen = 0;
            return true;
        }

        private static readonly NameValueCollection Staticfileheader=new NameValueCollection(StringComparer.InvariantCultureIgnoreCase)
        {
            {"Cache-Control", "max-age=3600,public"},
            {"Access-Control-Allow-Origin", "*"}
        };

        private static readonly NameValueCollection Dynamicfileheader=new NameValueCollection(StringComparer.InvariantCultureIgnoreCase)
        {
            {"Cache-Control", "no-cache, no-store, must-revalidate"},
            {"Access-Control-Allow-Origin", "*"}
        };
        protected override void GetResponseHeaders(CefResponse response, out long responseLength, out string redirectUrl)
        {
            response.SetHeaderMap(_source.IsStatic ? Staticfileheader : Dynamicfileheader);
            response.Status = 200;
            response.MimeType = _source.Mime; // "text/html";
            response.StatusText = "OK";
            responseLength = _source.Resource.Length; // unknown content-length
            redirectUrl = null; // no-redirect
        }

        protected override bool Open(CefRequest request, out bool handleRequest, CefCallback callback)
        {
            // Backwards compatibility. ProcessRequest will be called.
            callback.Dispose();
            handleRequest = false;
            return false;
        }
        protected override bool Skip(long bytesToSkip, out long bytesSkipped, CefResourceSkipCallback callback)
        {
            bytesSkipped = (long)CefErrorCode.Failed;
            return false;
        }

        protected override bool Read(Stream response, int bytesToRead, out int bytesRead, CefResourceReadCallback callback)
        {
            // Backwards compatibility. ReadResponse will be called.
            callback.Dispose();
            bytesRead = -1;
            return false;
        }

        protected override bool ReadResponse(Stream response, int bytesToRead, out int bytesRead, CefCallback callback)
        {
            if (_completed)
            {
                bytesRead = 0;
                return false;
            }
            // very simple response with one block
            var transmitSize = Math.Min(_source.Resource.Length - _transmittedLen, bytesToRead);

            response.Write(_source.Resource, _transmittedLen, transmitSize);
            bytesRead = transmitSize;
            _transmittedLen += transmitSize;
            if (_transmittedLen == _source.Resource.Length)
                _completed = true;
            if (!_completed)
                callback.Continue();
            return true;
        }

        //protected override bool CanGetCookie(CefCookie cookie) => false;
        //protected override bool CanSetCookie(CefCookie cookie) => false;

        protected override void Cancel(){}
    }
    internal class JsonreturnHandler : CefResourceHandler
    {
        private readonly byte [] _source;
        private bool _completed;
        private int _transmittedLen;

        public JsonreturnHandler(byte[] source){
            _source = source;
        }

        protected override bool ProcessRequest(CefRequest request, CefCallback callback){
            callback.Continue();
            _completed = false;
            _transmittedLen = 0;
            return true;
        }

        private static readonly NameValueCollection Dynamicfileheader=new NameValueCollection(StringComparer.InvariantCultureIgnoreCase)
        {
            {"Cache-Control", "no-cache, no-store, must-revalidate"},
            {"Access-Control-Allow-Origin", "*"}
        };
        protected override void GetResponseHeaders(CefResponse response, out long responseLength, out string redirectUrl)
        {
            response.SetHeaderMap(Dynamicfileheader);
            response.Status = 200;
            response.MimeType = "application/json";
            response.StatusText = "OK";
            responseLength = _source.Length; // unknown content-length
            redirectUrl = null; // no-redirect
        }

        protected override bool ReadResponse(Stream response, int bytesToRead, out int bytesRead, CefCallback callback)
        {
            if (_completed)
            {
                bytesRead = 0;
                return false;
            }
            // very simple response with one block
            var transmitSize = Math.Min(_source.Length - _transmittedLen, bytesToRead);

            response.Write(_source, _transmittedLen, transmitSize);
            bytesRead = transmitSize;
            _transmittedLen += transmitSize;
            if (_transmittedLen == _source.Length)
                _completed = true;
            if (!_completed)
                callback.Continue();
            return true;
        }

        //protected override bool CanGetCookie(CefCookie cookie) => false;
        //protected override bool CanSetCookie(CefCookie cookie) => false;

        protected override void Cancel(){}

        protected override bool Open(CefRequest request, out bool handleRequest, CefCallback callback)
        {
            // Backwards compatibility. ProcessRequest will be called.
            callback.Dispose();
            handleRequest = false;
            return false;
        }
        protected override bool Skip(long bytesToSkip, out long bytesSkipped, CefResourceSkipCallback callback)
        {
            bytesSkipped = (long)CefErrorCode.Failed;
            return false;
        }

        protected override bool Read(Stream response, int bytesToRead, out int bytesRead, CefResourceReadCallback callback)
        {
            // Backwards compatibility. ReadResponse will be called.
            callback.Dispose();
            bytesRead = -1;
            return false;
        }
    }
}