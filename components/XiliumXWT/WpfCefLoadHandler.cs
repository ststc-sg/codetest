using System;

namespace Xilium.CefGlue.WPF
{
    public interface ScreenCefBrowserGlue
    {
        void HandleAfterCreated(CefBrowser browser);
        void OnLoadStart(CefFrame frame);
        void OnLoadError(CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl);
        void OnLoadEnd(CefFrame frame, int httpStatusCode);
        void OnLoadingStateChange(bool isLoading, bool canGoBack, bool canGoForward);
        bool OnTooltip(string text);
        void OnBeforeClose(CefBrowser browser);
    }
    public interface OffscreenCefBrowserGlue : ScreenCefBrowserGlue
    {
        bool GetViewRect(ref CefRectangle rect);

        void HandleViewPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer,
            int width, int height);

        void HandlePopupPaint(int width, int height, CefRectangle[] dirtyRects, IntPtr sourceBuffer);
        void GetScreenPoint(int viewX, int viewY, ref int screenX, ref int screenY);

        void OnCursorChange(IntPtr Win32Cursohandle);
        void OnPopupShow(bool show);
        void OnPopupSize(CefRectangle rect);
        void GetViewRect(CefBrowser browser, out CefRectangle rect);
    }

   

    public class WpfCefLoadHandler : CefLoadHandler
    {
        private readonly ScreenCefBrowserGlue _owner;

        public WpfCefLoadHandler(ScreenCefBrowserGlue owner)
        {
            _owner = owner;
        }

        protected override void OnLoadingStateChange(CefBrowser browser, bool isLoading, bool canGoBack,
            bool canGoForward)
        {
            _owner.OnLoadingStateChange(isLoading, canGoBack, canGoForward);
        }

        protected override void OnLoadError(CefBrowser browser, CefFrame frame, CefErrorCode errorCode, string errorText,
            string failedUrl)
        {
            _owner.OnLoadError(frame, errorCode, errorText, failedUrl);
        }

        protected override void OnLoadStart(CefBrowser browser, CefFrame frame, CefTransitionType transitionType)
        {
            _owner.OnLoadStart(frame);
        }

        protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
        {
            _owner.OnLoadEnd(frame, httpStatusCode);
        }
    }
}