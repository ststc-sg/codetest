using BridgeConsole.Windows;
using NLog;
using ResourceCollection;
using System;
using System.IO;
using System.Threading;
using XiliumXWT;
using Xwt;

namespace BridgeConsole
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static HttpResources Globaleresources;
        private static GlobalState _interconnector;
        private static Config _config;
        private static BackgroundServer _debugserver;

        private static string configfilename = "config.xml";
        private static string originalconfigpath = "defaultconfig.xml";

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {

                ConfigureLog();
                Globaleresources = new HttpResources();
                Logger.Trace("Log ready. Search for config in user location");


                ReadConfiguration(originalconfigpath);

                if (_config.NetworkStartupDelay > 0)
                    Thread.Sleep(
                        TimeSpan.FromSeconds(_config.NetworkStartupDelay)); //sleep some time before network start
                _interconnector = new GlobalState();
                Logger.Trace("Configure cef");

                var ceflogpath = "./cef.log";
                var cefconfig = CEFConfig.Configure(Globaleresources, _interconnector, ceflogpath);
                if (cefconfig == null) return;
                ConfigureWebSources();
                Logger.Trace("Init Xwt application");
                Application.Initialize(ToolkitType.Wpf);
                Application.UnhandledException += Application_UnhandledException;
                Logger.Trace("create browser window");
                var webbridge = new RawBrowserWindow("/bridge/main.html", _config.WebBridge);


                try
                {
                    Logger.Trace("Bind local server");
                    _debugserver = new BackgroundServer(Globaleresources, _interconnector,
                        $"http://localhost:{_config.WwwhubPort()}/");
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    MessageDialog.ShowError(e.Message);
                }

                Application.Run();
                Logger.Trace("initiate shutdown program");
                _debugserver?.Close();
                webbridge?.Dispose();


                _interconnector.Dispose();
                Logger.Trace("start cleanup");
                Logger.Trace("stop hardware phone");
                Logger.Trace("clean resource generators");
                Logger.Trace("clean cef");
                cefconfig.Dispose();
                Logger.Trace("clean simulator");
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageDialog.ShowError(e.Message);
            }
        }

        private static void Application_UnhandledException(object sender, ExceptionEventArgs e)
        {
            Logger.Error(e);
        }





        private static void ReadConfiguration(string configpath)
        {
            _config = Config.Load(configpath);
            if (_config == null)
            {
                Logger.Warn("user configiration not found,try default");
                _config = Config.LoadOrCreate(originalconfigpath);
                _config.Save(configpath);
            }
        }

        private static void ConfigureWebSources()
        {
            var wwpath = Path.Combine(Directory.GetCurrentDirectory(), "www");
            Globaleresources.AddSource(new LazyDirectoryResourceLoader(wwpath));
            var preloadpath = Path.Combine(Directory.GetCurrentDirectory(), "preload.list");
            Globaleresources.Preload(preloadpath);
            Globaleresources.RememberNewFiles(preloadpath);
        }

        private static void ConfigureLog()
        {
            LogManager.ResumeLogging();
        }
    }
}