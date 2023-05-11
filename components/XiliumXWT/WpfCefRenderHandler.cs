using System;
using XiliumXWT;

namespace Xilium.CefGlue.WPF
{
    internal sealed class WpfCefRenderHandler : CefRenderHandler
    {
        private readonly ILogger _logger;
        private readonly OffscreenCefBrowserGlue _owner;
        private readonly IUiHelper _uiHelper;

        public WpfCefRenderHandler(OffscreenCefBrowserGlue owner, ILogger logger, IUiHelper uiHelper)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");

            if (logger == null)
                throw new ArgumentNullException("logger");

            if (uiHelper == null)
                throw new ArgumentNullException("uiHelper");

            _owner = owner;
            _logger = logger;
            _uiHelper = uiHelper;
        }

        protected override CefAccessibilityHandler GetAccessibilityHandler() {
            return null;
        }

        protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
        {
            return _owner.GetViewRect(ref rect);
        }

        protected override void GetViewRect(CefBrowser browser, out CefRectangle rect)
        {
            _owner.GetViewRect(browser, out rect);
        }


        protected override bool GetScreenPoint(CefBrowser browser, int viewX, int viewY, ref int screenX,
            ref int screenY)
        {
            _owner.GetScreenPoint(viewX, viewY, ref screenX, ref screenY);
            return true;
        }

        protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
        {
            return false;
        }

        protected override void OnPopupShow(CefBrowser browser, bool show)
        {
            _owner.OnPopupShow(show);
        }

        protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
        {
            _owner.OnPopupSize(rect);
        }

        protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects,
            IntPtr buffer, int width, int height)
        {
            _logger.Debug("Type: {0} Buffer: {1:X8} Width: {2} Height: {3}", type, buffer, width, height);
            foreach (var rect in dirtyRects)
                _logger.Debug("   DirtyRect: X={0} Y={1} W={2} H={3}", rect.X, rect.Y, rect.Width, rect.Height);

            if (type == CefPaintElementType.View)
                _owner.HandleViewPaint(browser, type, dirtyRects, buffer, width, height);
            else if (type == CefPaintElementType.Popup)
                _owner.HandlePopupPaint(width, height, dirtyRects, buffer);
        }

        protected override void OnAcceleratedPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr sharedHandle)
        {
            //some shit
        }

        //protected override void OnCursorChange(CefBrowser browser, IntPtr cursorHandle, CefCursorType type,
        //    CefCursorInfo customCursorInfo)
        //{
        //    _owner.OnCursorChange(cursorHandle);
        //}
        
        protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
        {
        }

        protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange,
            CefRectangle[] characterBounds)
        {
        }
    }
}