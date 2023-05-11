using System;
using System.Threading;
using System.Windows.Media.Imaging;
using NLog.Fluent;
using Xilium.CefGlue;
using Xilium.CefGlue.WPF;
using Application = Xwt.Application;
using BitmapImage = Xwt.Drawing.BitmapImage;
using Image = System.Windows.Controls.Image;
using Key = Xwt.Key;
using Point = Xwt.Point;
using Size = Xwt.Size;
using System;
using System.Collections.Generic;
using System.Text;
using Xilium.CefGlue.Platform.Windows;

namespace XiliumXWT
{
    public class XwtCefRawBrowser : ScreenCefBrowserGlue, IExternalTouchScreenHandler,IDisposable
    {
        public event Action OnClose;
        private Point _position;
        private Size _size;

        private static readonly Key[] HandledKeys =
        {
            Key.Tab, Key.Home, Key.End, Key.Left, Key.Right, Key.Up, Key.Down
        };
        private readonly ILogger _logger;

        private CefBrowser _browser;

        


        private CefBrowserHost _browserHost;
        private BitmapImage _browserPageImage;
        

        
        private ScreenCefClient _cefClient;
        private bool _created;

        private bool _disposed;

        //private ToolTip _tooltip;
        //private DispatcherTimer _tooltipTimer;

        //private PopupWindow _popup;
        private Image _popupImage;
        private WriteableBitmap _popupImageBitmap;
        private readonly CancellationTokenSource cancelsource = new CancellationTokenSource();

        

        public void HandleTouchChange(int id, XwtTouchEventType state, double x, double y)
        {
            switch (state)
            {
                case XwtTouchEventType.PutDown:
                    _browserHost.SetFocus(true);
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x, (int)y, CefEventFlags.None), false);
                    _browserHost.SendMouseClickEvent(new CefMouseEvent((int)x, (int)y, CefEventFlags.LeftMouseButton), CefMouseButtonType.Left, false, 1);
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x, (int)y, CefEventFlags.LeftMouseButton), false);
                    break;
                case XwtTouchEventType.PutUp:
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x, (int)y, CefEventFlags.LeftMouseButton), false);
                    _browserHost.SendMouseClickEvent(new CefMouseEvent((int)x, (int)y, CefEventFlags.None),
                        CefMouseButtonType.Left, true, 1);
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x, (int)y, CefEventFlags.None), false);
                    break;
                case XwtTouchEventType.Contact:
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x, (int)y, CefEventFlags.LeftMouseButton), false);
                    break;

            }


        }

        public XwtCefRawBrowser(Xwt.Point position, Size size,string url) : this(position, size,url, new NLogLogger("XwtCefBrowser"))
        {
            
            
        }
        public XwtCefRawBrowser(Xwt.Point position, Size size,string url, ILogger logger)
        {
            _position = position;
            _size = size;
            if (logger == null)
                throw new ArgumentNullException("logger");

            _logger = logger;

            StartUrl = url;

            CreateBrowser(position, size);
            //_popup = CreatePopup();

            //_tooltip = new Xwt.ToolTip();
            //_tooltip.StaysOpen = true;
            //_tooltip.Visibility = Visibility.Collapsed;
            //_tooltip.Closed += TooltipOnClosed;

            //_tooltipTimer = new DispatcherTimer();
            //_tooltipTimer.Interval = TimeSpan.FromSeconds(0.5);

            //KeyboardNavigation.SetAcceptsReturn(this, true);
        }

        public string StartUrl { get; set; }

        public void OnLoadStart(CefFrame frame)
        {
            if (LoadStart != null)
            {
                var e = new LoadStartEventArgs(frame);
                LoadStart(this, e);
            }
        }

        public void OnLoadEnd(CefFrame frame, int httpStatusCode)
        {
            if (LoadEnd != null)
            {
                var e = new LoadEndEventArgs(frame, httpStatusCode);
                LoadEnd(this, e);
            }
        }

        public void OnLoadingStateChange(bool isLoading, bool canGoBack, bool canGoForward)
        {
            if (LoadingStateChange != null)
            {
                var e = new LoadingStateChangeEventArgs(isLoading, canGoBack, canGoForward);
                LoadingStateChange(this, e);
            }
        }

        public void OnLoadError(CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl)
        {
            if (LoadError != null)
            {
                var e = new LoadErrorEventArgs(frame, errorCode, errorText, failedUrl);
                LoadError(this, e);
            }
        }

        public event LoadStartEventHandler LoadStart;
        public event LoadEndEventHandler LoadEnd;
        public event LoadingStateChangeEventHandler LoadingStateChange;
        public event LoadErrorEventHandler LoadError;


        public void ExecuteJavaScript(string code, string url, int line)
        {
            if (_browser != null)
                _browser.GetMainFrame().ExecuteJavaScript(code, url, line);
        }


        public void Resize(Size size)
        {
            if (!_created)
                return;
            if(_size==size)
                return;
            _size = size;
            if (_browserHost != null)
            {
                _logger.Trace("CefBrowserHost::WasResized to {0}x{1}.", size.Width, size.Height);
                _browserHost.WasResized();
            }
            
        }

        private void CreateBrowser(Xwt.Point position, Size size)
        {
            AttachEventHandlers(); // TODO: ?

            var windowInfo = CefWindowInfo.Create();
            windowInfo.Width= (int)size.Width;
            windowInfo.Height = (int)size.Height;
            windowInfo.X = (int)position.X;
            windowInfo.Y = (int)position.Y;
            windowInfo.Hidden = false;
            windowInfo.Style = WindowStyle.WS_VISIBLE | WindowStyle.WS_POPUP;
            //windowInfo.SetAsPopup(IntPtr.Zero, "test");

            var settings = new CefBrowserSettings
            {
                DefaultFontSize = 14,
                DefaultFixedFontSize = 14
            };
            _cefClient = new ScreenCefClient(this);


            // This is the first time the window is being rendered, so create it.
            CefBrowserHost.CreateBrowser(windowInfo, _cefClient, settings,
                !string.IsNullOrEmpty(StartUrl) ? StartUrl : "about:blank");

            _created = true;
            
        }

        private void AttachEventHandlers()
        {
            

            

            

            
        }

        #region Disposable

        ~XwtCefRawBrowser()
        {
            Dispose();
        }





        public void Dispose()
        {
            if (!_disposed)
            {
                cancelsource.Cancel();
                if (_browserPageImage != null)
                {
                    _browserPageImage.Dispose();
                    _browserPageImage = null;
                }
                if (_browserHost != null)
                {
                    _browserHost.CloseBrowser();
                    _browserHost = null;
                }

                if (_browser != null)
                {
                    _browser.Dispose();
                    _browser = null;
                }
                _disposed = true;
            }
        }

        #endregion

        #region Handlers

        public void InvokeUiAction(Action e)
        {
            try
            {
                var task = Application.InvokeAsync(e);
                task.Wait(cancelsource.Token);
            }
            catch (System.OperationCanceledException exception)
            {
                Log.Trace("InvokeUiAction operation cancelled");
                //ignored
            }
            catch (Exception exception)
            {
                Log.Error($"InvokeUiAction error{exception.Message} on stack {exception.StackTrace}");
                // ignored
            }
        }

        public void HandleAfterCreated(CefBrowser browser)
        {
            var hasAlreadyBeenInitialized = false;

            _browser = browser;
            _browserHost = _browser.GetHost();

            var handle = _browserHost.GetWindowHandle();

            NativeMethods.SetWindowPos(handle, IntPtr.Zero, (int) _position.X, (int) _position.Y, (int) _size.Width,
                (int) _size.Height, SetWindowPosFlags.ShowWindow);
            NavigateTo(StartUrl);
        }


        public bool OnTooltip(string text)
        {
            
            return true;
        }

        public void OnBeforeClose(CefBrowser browser)
        {
            OnClose?.Invoke();
        }

        #endregion

        #region Utils

        #endregion

        #region Methods

        public void NavigateTo(string url)
        {
            // Remove leading whitespace from the URL
            url = url.TrimStart();

            if (_browser != null)
                _browser.GetMainFrame().LoadUrl(url);
            else
                StartUrl = url;
        }


        public bool CanGoBack()
        {
            if (_browser != null)
                return _browser.CanGoBack;
            return false;
        }

        public void GoBack()
        {
            if (_browser != null)
                _browser.GoBack();
        }

        public bool CanGoForward()
        {
            if (_browser != null)
                return _browser.CanGoForward;
            return false;
        }

        public void GoForward()
        {
            if (_browser != null)
                _browser.GoForward();
        }

        public void Refresh()
        {
            _browser?.Reload();
        }

        #endregion

        public void SetPosition(Point point)
        {
            if (_browserHost != null)
            {
                _position = point;
                var handle = _browserHost.GetWindowHandle();
                NativeMethods.SetWindowPos(handle, IntPtr.Zero, (int) _position.X, (int) _position.Y, (int) _size.Width,
                    (int) _size.Height, SetWindowPosFlags.NoZOrder);
            }
        }
    }
}