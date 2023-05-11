using Xilium.CefGlue;

namespace XiliumXWT
{
    internal class MyBrowserProcessHandler : CefBrowserProcessHandler
    {
        protected override void OnBeforeChildProcessLaunch(CefCommandLine commandLine)
        {
            base.OnBeforeChildProcessLaunch(commandLine);
        }

        protected override void OnContextInitialized()
        {
            base.OnContextInitialized();
        }
    }

    internal class DemoCefApp : CefApp
    {
        private readonly MyBrowserProcessHandler browser_prrocess = new MyBrowserProcessHandler();

        protected override CefBrowserProcessHandler GetBrowserProcessHandler()
        {
            return browser_prrocess;
        }

        protected override CefRenderProcessHandler GetRenderProcessHandler()
        {
            var result = base.GetRenderProcessHandler();
            return result;
        }

        protected override CefResourceBundleHandler GetResourceBundleHandler()
        {
            var result = base.GetResourceBundleHandler();
            return result;
        }
        static readonly string[] Nochachekeys = {
            "disable-application-cache", "disable-cache", "disable-gpu-program-cache",
            "disable-gpu-shader-disk-cache"
        };
        protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {
            commandLine.AppendSwitch("touch-events", "enabled");
            commandLine.AppendSwitch("enable-pinch");
            foreach (var s in Nochachekeys)
                commandLine.AppendSwitch(s);
            commandLine.AppendSwitch(@"no-proxy-server");
            //commandLine.AppendSwitch(@"process-per-tab"); 

            base.OnBeforeCommandLineProcessing(processType, commandLine);
        }
    }
}