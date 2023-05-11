using System;
using XiliumXWT;

namespace Xilium.CefGlue.WPF
{
    public class EmptyCefContextMenuHandlerImpl : CefContextMenuHandler
    {
        protected override void OnBeforeContextMenu(CefBrowser browser, CefFrame frame, CefContextMenuParams state, CefMenuModel model)
        {
            model.Clear();
        }
    }

    internal class ScreenCefClient : CefClient
    {
        private static CefContextMenuHandler _contextMenuHandler = new EmptyCefContextMenuHandlerImpl();
        private readonly WpfCefDisplayHandler _displayHandler;
        private readonly WpfCefJSDialogHandler _jsDialogHandler;

        private readonly WpfCefLifeSpanHandler _lifeSpanHandler;
        private readonly WpfCefLoadHandler _loadHandler;
        private readonly WpfCefRenderHandler _renderHandler;
        private ScreenCefBrowserGlue _owner;

        public ScreenCefClient(ScreenCefBrowserGlue owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");

            _owner = owner;

            _lifeSpanHandler = new WpfCefLifeSpanHandler(owner);
            _displayHandler = new WpfCefDisplayHandler(owner);
            _loadHandler = new WpfCefLoadHandler(owner);
            _jsDialogHandler = new WpfCefJSDialogHandler();
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return _lifeSpanHandler;
        }

        protected override CefDisplayHandler GetDisplayHandler()
        {
            return _displayHandler;
        }

        protected override CefRenderHandler GetRenderHandler()
        {
            return _renderHandler;
        }

        protected override CefLoadHandler GetLoadHandler()
        {
            return _loadHandler;
        }

        protected override CefJSDialogHandler GetJSDialogHandler()
        {
            return _jsDialogHandler;
        }

        protected override CefContextMenuHandler GetContextMenuHandler()
        {
            return _contextMenuHandler;
        }
    }

    internal sealed class OffscreenCefClient : ScreenCefClient
    {
        private static CefContextMenuHandler _contextMenuHandler=new EmptyCefContextMenuHandlerImpl(); 
        private readonly WpfCefRenderHandler _renderHandler;
        private OffscreenCefBrowserGlue _owner;

        public OffscreenCefClient(OffscreenCefBrowserGlue owner):base(owner)
        {
            _owner = owner;

            _renderHandler = new WpfCefRenderHandler(owner, new NLogLogger("WpfCefRenderHandler"),
                new UiHelper(new NLogLogger("WpfCefRenderHandler")));


        }

        

        protected override CefRenderHandler GetRenderHandler()
        {
            return _renderHandler;
        }
    }


    
}