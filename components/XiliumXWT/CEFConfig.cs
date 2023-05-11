using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NLog;
using ResourceCollection;
using Xilium.CefGlue;

namespace XiliumXWT
{
    public interface IInternalHttpRequestHandler
    {
        byte []  HandleHttpRequest(string collection,IDictionary<string, string> evt);
        byte []  HandleHttpProtobufRequest(string collectionname,byte [] data);
    }
    public class CEFConfig : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private const string Sheme = "http";
        private const string Domain = "app.mydomain.com";
        private const string AppUrlString = Sheme + "://" + Domain;
        private static CEFConfig config;
        private readonly IHttpResources _sources;

        private CEFConfig(IHttpResources sources)
        {
            _sources = sources;
        }

        public void Dispose()
        {
            CefRuntime.Shutdown();
        }
        private static CefMainArgs mainArgs = new(new string[] { });
        private static DemoCefApp cefApp = new();
        private bool Init(IInternalHttpRequestHandler actionhandler, string ceflogpath)
        {
            string path = Directory.GetCurrentDirectory();
            
            var moduledirectory=System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var runprocesspath = Path.Combine(moduledirectory, "CefGlueBrowserProcess");
            var clientpath = Path.Combine(runprocesspath,"Xilium.CefGlue.BrowserProcess.exe");
            if (!File.Exists(clientpath))
                throw new Exception("now browser process");
            var logpath = ceflogpath;
            try
            {
                

                var cefSettings = new CefSettings {
                    BrowserSubprocessPath = clientpath,
                    WindowlessRenderingEnabled = true,
                    MultiThreadedMessageLoop = CefRuntime.Platform == CefRuntimePlatform.Windows,
                    LogFile = logpath,
                    Locale = "en-US",
                    LogSeverity = CefLogSeverity.Error,
#if DEBUG
                    RemoteDebuggingPort = 9002,
#endif
                };
                CefRuntime.Initialize(mainArgs, cefSettings, cefApp, IntPtr.Zero);
                CefRuntime.RegisterSchemeHandlerFactory(Sheme, Domain, new MySchemeHandlerFactory(_sources, actionhandler));
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            return true;
        }

        public static CEFConfig Get()
        {
            if (config == null) throw new Exception("try to get not initialised interface");
            return config;
        }

        public static CEFConfig Configure(IHttpResources sources, IInternalHttpRequestHandler callback,
            string ceflogpath)
        {
            if (config != null)
                throw new Exception("second initialise detected for CEFConfig");
            config = new CEFConfig(sources);
            if (!config.Init(callback, ceflogpath))
            {
                config.Dispose();
                config = null;
            }
            return config;
        }

        public string AppUrl()
        {
            return AppUrlString;
        }
    }
}