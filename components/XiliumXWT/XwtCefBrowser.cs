using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using NLog.Fluent;
using Xilium.CefGlue;
using Xilium.CefGlue.WPF;
using Xwt;
using Xwt.Drawing;
using Application = Xwt.Application;
using BitmapImage = Xwt.Drawing.BitmapImage;
using Image = System.Windows.Controls.Image;
using Key = Xwt.Key;
using Keyboard = Xwt.Keyboard;
using ModifierKeys = Xwt.ModifierKeys;
using Point = Xwt.Point;
namespace XiliumXWT
{
    public enum XwtTouchEventType
    {
        PutDown, 
        PutUp, 
        Contact, 
    }
    public interface IExternalTouchScreenHandler
    {
        void HandleTouchChange(int Id, XwtTouchEventType state, double x, double y);
    }

    
    public class XwtCefBrowser : ImageView, OffscreenCefBrowserGlue ,IExternalTouchScreenHandler
    {
        private static readonly Key[] HandledKeys =
        {
            Key.Tab, Key.Home, Key.End, Key.Left, Key.Right, Key.Up, Key.Down
        };

        private readonly ILogger _logger;

        private CefBrowser _browser;
        private int _browserHeight;
        private CefBrowserHost _browserHost;
        private BitmapImage _browserPageImage;
        private bool _browserSizeChanged;

        private int _browserWidth;
        private OffscreenCefClient _cefClient;
        private bool _created;

        private bool _disposed;

        //private ToolTip _tooltip;
        //private DispatcherTimer _tooltipTimer;

        //private PopupWindow _popup;
        private Image _popupImage;
        private WriteableBitmap _popupImageBitmap;
        private readonly CancellationTokenSource cancelsource = new CancellationTokenSource();

        public XwtCefBrowser(string url) : this(url, new NLogLogger("XwtCefBrowser"))
        {
        }

        public void HandleTouchChange(int id, XwtTouchEventType state, double x, double y)
        {
            switch (state)
            {
                case XwtTouchEventType.PutDown:
                    _browserHost.SetFocus(true);
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x,(int)y,CefEventFlags.None), false);
                    _browserHost.SendMouseClickEvent(new CefMouseEvent((int)x,(int)y,CefEventFlags.LeftMouseButton),CefMouseButtonType.Left, false,1);
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x,(int)y,CefEventFlags.LeftMouseButton), false);
                    break;
                case XwtTouchEventType.PutUp:
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x,(int)y,CefEventFlags.LeftMouseButton), false);
                    _browserHost.SendMouseClickEvent(new CefMouseEvent((int)x,(int)y,CefEventFlags.None),
                        CefMouseButtonType.Left, true,1);
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x,(int)y,CefEventFlags.None), false);
                    break;
                case XwtTouchEventType.Contact:
                    _browserHost.SendMouseMoveEvent(new CefMouseEvent((int)x,(int)y,CefEventFlags.LeftMouseButton), false);
                    break;
                
            }
            
            
        }
        public XwtCefBrowser(string url, ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException("logger");

            _logger = logger;

            StartUrl = url;

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
        public bool AllowsTransparency { get; set; }
        private bool _isIgnoreRefresh = false; //ignore refresh while loading not finished

        public void OnLoadStart(CefFrame frame)
        {
            _isIgnoreRefresh = true;
            if (LoadStart != null)
            {
                var e = new LoadStartEventArgs(frame);
                LoadStart(this, e);
            }
        }

        public void OnLoadEnd(CefFrame frame, int httpStatusCode)
        {
            _isIgnoreRefresh = false;
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

        protected override void OnBoundsChanged()
        {
            var size = Size;
            var newWidth = (int) size.Width;
            var newHeight = (int) size.Height;

            _logger.Debug("BrowserResize. Old H{0}xW{1}; New H{2}xW{3}.", _browserHeight, _browserWidth, newHeight,
                newWidth);

            if (newWidth > 0 && newHeight > 0)
                if (!_created)
                {
                    AttachEventHandlers(); // TODO: ?

                    // Create the bitmap that holds the rendered website bitmap
                    _browserWidth = newWidth;
                    _browserHeight = newHeight;
                    _browserSizeChanged = true;

                    // Find the window that's hosting us
                    //Widget parentWnd = GrandParent(this);
                    Widget current = this;
                    var native = Toolkit.CurrentEngine.GetNativeWidget(current) as FrameworkElement;
                    var source = (HwndSource) PresentationSource.FromVisual(native);

                    if (source != null)
                    {
                        var hParentWnd = source.Handle;

                        var windowInfo = CefWindowInfo.Create();
                        windowInfo.SetAsWindowless(hParentWnd, AllowsTransparency);

                        var settings = new CefBrowserSettings {
                            DefaultFontSize = 14,
                            DefaultFixedFontSize = 14
                        };
                        _cefClient = new OffscreenCefClient(this);


                        // This is the first time the window is being rendered, so create it.
                        CefBrowserHost.CreateBrowser(windowInfo, _cefClient, settings,
                            !string.IsNullOrEmpty(StartUrl) ? StartUrl : "about:blank");

                        _created = true;
                    }
                }
                else
                {
                    // Only update the bitmap if the size has changed
                    if (_browserWidth != newWidth ||
                        _browserHeight != newHeight)
                    {
                        _browserWidth = newWidth;
                        _browserHeight = newHeight;
                        _browserSizeChanged = true;

                        // If the window has already been created, just resize it
                        if (_browserHost != null)
                        {
                            _logger.Trace("CefBrowserHost::WasResized to {0}x{1}.", newWidth, newHeight);
                            _browserHost.WasResized();
                        }
                    }
                }
        }

        private void AttachEventHandlers()
        {
            MouseExited += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        var mouseEvent = new CefMouseEvent {
                            X = 0,
                            Y = 0,
                            Modifiers = GetMouseModifiers()
                        };
                        _browserHost.SendMouseMoveEvent(mouseEvent, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in MouseLeave()", ex);
                }
            };

            MouseMoved += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        var cursorPos = arg.Position;

                        var mouseEvent = new CefMouseEvent {
                            X = (int) cursorPos.X,
                            Y = (int) cursorPos.Y,
                            Modifiers = GetMouseModifiers()
                        };
                        _browserHost.SendMouseMoveEvent(mouseEvent, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in MouseMove()", ex);
                }
            };

            ButtonPressed += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        SetFocus();
                        var cursorPos = arg.Position;

                        var mouseEvent = new CefMouseEvent {
                            X = (int) cursorPos.X,
                            Y = (int) cursorPos.Y,
                            Modifiers = GetMouseModifiers()
                        };


                        if (arg.Button == PointerButton.Left)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Left, false,
                                arg.MultiplePress);
                        else if (arg.Button == PointerButton.Middle)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Middle, false,
                                arg.MultiplePress);
                        else if (arg.Button == PointerButton.Right)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Right, false,
                                arg.MultiplePress);

                        //_logger.Debug(string.Format("Browser_MouseDown: ({0},{1})", cursorPos.X, cursorPos.Y));
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in MouseDown()", ex);
                }
            };

            ButtonReleased += (sender, arg) =>
            {
                try
                {
                    if (_browserHost != null)
                    {
                        var cursorPos = arg.Position;

                        var mouseEvent = new CefMouseEvent
                        {
                            X = (int) cursorPos.X,
                            Y = (int) cursorPos.Y,
                            Modifiers = GetMouseModifiers()
                        };


                        if (arg.Button == PointerButton.Left)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Left, true,
                                arg.MultiplePress);
                        else if (arg.Button == PointerButton.Middle)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Middle, true,
                                arg.MultiplePress);
                        else if (arg.Button == PointerButton.Right)
                            _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Right, true,
                                arg.MultiplePress);

                        //_logger.Debug(string.Format("Browser_MouseUp: ({0},{1})", cursorPos.X, cursorPos.Y));
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in MouseUp()", ex);
                }
            };
            /*GotKeyboardFocus += (sender, arg) =>
            {
                try {
                    if (_browserHost != null) {
                        _browserHost.SendFocusEvent(true);
                    }
                }
                catch (Exception ex) {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in GotFocus()", ex);
                }
            };

            browser.LostKeyboardFocus += (sender, arg) =>
            {
                try {
                    if (_browserHost != null) {
                        _browserHost.SendFocusEvent(false);
                    }
                }
                catch (Exception ex) {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in LostFocus()", ex);
                }
            };

            

            MouseWheel += (sender, arg) =>
            {
                try {
                    if (_browserHost != null) {
                        Point cursorPos = arg.GetPosition(this);

                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = (int)cursorPos.X,
                            Y = (int)cursorPos.Y,
                        };

                        _browserHost.SendMouseWheelEvent(mouseEvent, 0, arg.Delta);
                    }
                }
                catch (Exception ex) {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in MouseWheel()", ex);
                }
            };

            // TODO: require more intelligent processing
            PreviewTextInput += (sender, arg) =>
            {
                if (_browserHost != null) {
                    _logger.Debug("TextInput: text {0}", arg.Text);

                    foreach (var c in arg.Text) {
                        CefKeyEvent keyEvent = new CefKeyEvent()
                        {
                            EventType = CefKeyEventType.Char,
                            WindowsKeyCode = (int)c,
                            // Character = c,
                        };

                        keyEvent.Modifiers = GetKeyboardModifiers();

                        _browserHost.SendKeyEvent(keyEvent);
                    }
                }

                arg.Handled = true;
            };

            // TODO: require more intelligent processing
            PreviewKeyDown += (sender, arg) =>
            {
                try {
                    if (_browserHost != null) {
                        //_logger.Debug(string.Format("KeyDown: system key {0}, key {1}", arg.SystemKey, arg.Key));
                        CefKeyEvent keyEvent = new CefKeyEvent()
                        {
                            EventType = CefKeyEventType.RawKeyDown,
                            WindowsKeyCode = KeyInterop.VirtualKeyFromKey(arg.Key == Key.System ? arg.SystemKey : arg.Key),
                            NativeKeyCode = 0,
                            IsSystemKey = arg.Key == Key.System,
                        };

                        keyEvent.Modifiers = GetKeyboardModifiers();

                        _browserHost.SendKeyEvent(keyEvent);
                    }
                }
                catch (Exception ex) {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in PreviewKeyDown()", ex);
                }

                arg.Handled = HandledKeys.Contains(arg.Key);
            };

            // TODO: require more intelligent processing
            browser.PreviewKeyUp += (sender, arg) =>
            {
                try {
                    if (_browserHost != null) {
                        //_logger.Debug(string.Format("KeyUp: system key {0}, key {1}", arg.SystemKey, arg.Key));
                        CefKeyEvent keyEvent = new CefKeyEvent()
                        {
                            EventType = CefKeyEventType.KeyUp,
                            WindowsKeyCode = KeyInterop.VirtualKeyFromKey(arg.Key == Key.System ? arg.SystemKey : arg.Key),
                            NativeKeyCode = 0,
                            IsSystemKey = arg.Key == Key.System,
                        };

                        keyEvent.Modifiers = GetKeyboardModifiers();

                        _browserHost.SendKeyEvent(keyEvent);
                    }
                }
                catch (Exception ex) {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in PreviewKeyUp()", ex);
                }

                arg.Handled = true;
            };
            _popup.MouseMove += (sender, arg) =>
            {
                try {
                    if (_browserHost != null) {
                        Xwt.Point cursorPos = arg.GetPosition(this);

                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = (int)cursorPos.X,
                            Y = (int)cursorPos.Y
                        };

                        mouseEvent.Modifiers = GetMouseModifiers();

                        _browserHost.SendMouseMoveEvent(mouseEvent, false);

                        //_logger.Debug(string.Format("Popup_MouseMove: ({0},{1})", cursorPos.X, cursorPos.Y));
                    }
                }
                catch (Exception ex) {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in Popup.MouseMove()", ex);
                }
            };
            _popup.MouseDown += (sender, arg) =>
            {
                try {
                    if (_browserHost != null) {
                        Point cursorPos = arg.GetPosition(this);

                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = (int)cursorPos.X,
                            Y = (int)cursorPos.Y
                        };

                        mouseEvent.Modifiers = GetMouseModifiers();

                        _browserHost.SendMouseClickEvent(mouseEvent, CefMouseButtonType.Left, true, 1);

                        //_logger.Debug(string.Format("Popup_MouseDown: ({0},{1})", cursorPos.X, cursorPos.Y));
                    }
                }
                catch (Exception ex) {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in Popup.MouseDown()", ex);
                }
            };
            _popup.MouseWheel += (sender, arg) =>
            {
                try {
                    if (_browserHost != null) {
                        Point cursorPos = arg.GetPosition(this);
                        int delta = arg.Delta;
                        CefMouseEvent mouseEvent = new CefMouseEvent()
                        {
                            X = (int)cursorPos.X,
                            Y = (int)cursorPos.Y
                        };

                        mouseEvent.Modifiers = GetMouseModifiers();
                        _browserHost.SendMouseWheelEvent(mouseEvent, 0, delta);

                        //_logger.Debug(string.Format("MouseWheel: ({0},{1})", cursorPos.X, cursorPos.Y));
                    }
                }
                catch (Exception ex) {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in Popup.MouseWheel()", ex);
                }
            };*/
        }

        #region Disposable

        ~XwtCefBrowser()
        {
            Dispose(false);
        }

        

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancelsource.Cancel();
                /*if (_tooltipTimer != null) {
                    _tooltipTimer.Stop();
                }*/

                if (_browserPageImage != null)
                {
                    _browserPageImage.Dispose();
                    _browserPageImage = null;
                }

                // 					if (this.browserPageD3dImage != null)
                // 						this.browserPageD3dImage = null;

                // TODO: What's the right way of disposing the browser instance?
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
            int width = 0, height = 0;

            var hasAlreadyBeenInitialized = false;

            InvokeUiAction(() =>
            {
                if (_browser != null)
                {
                    hasAlreadyBeenInitialized = true;
                }
                else
                {
                    _browser = browser;
                    _browserHost = _browser.GetHost();
                    width = _browserWidth;
                    height = _browserHeight;
                }
            });
            if (hasAlreadyBeenInitialized)
                return;
            if (width > 0 && height > 0)
                _browserHost.WasResized();
            NavigateTo(StartUrl);
        }

        public void GetViewRect(CefBrowser browser, out CefRectangle rect)
        {
            var browserRect = new CefRectangle();
            browserRect.X = browserRect.Y = 0;
            browserRect.Width = _browserWidth;
            browserRect.Height = _browserHeight;
            rect = browserRect;

            _logger.Debug("GetViewRect result provided:{0} Rect: X{1} Y{2} H{3} W{4}", true, browserRect.X,
                browserRect.Y, browserRect.Height, browserRect.Width);
        }

        public bool GetViewRect(ref CefRectangle rect)
        {
            var browserRect = new CefRectangle();
            browserRect.X = browserRect.Y = 0;
            browserRect.Width = _browserWidth;
            browserRect.Height = _browserHeight;
            rect = browserRect;

            _logger.Debug("GetViewRect result provided:{0} Rect: X{1} Y{2} H{3} W{4}", true, browserRect.X,
                browserRect.Y, browserRect.Height, browserRect.Width);
            return true;
        }

        public void GetScreenPoint(int viewX, int viewY, ref int screenX, ref int screenY)
        {
            var ptScreen = new Point();
            InvokeUiAction(() =>
            {
                try
                {
                    var ptView = new Point(viewX, viewY);
                    ptScreen = ConvertToScreenCoordinates(ptView);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("WpfCefBrowser: Caught exception in GetScreenPoint()", ex);
                }
            });

            screenX = (int) ptScreen.X;
            screenY = (int) ptScreen.Y;
        }

        public void HandleViewPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects,
            IntPtr buffer, int width, int height)
        {
            if(_isIgnoreRefresh)
                return;
            try
            {
                InvokeUiAction(() =>
                {
                    try
                    {
                        if (_browserSizeChanged && (width != _browserWidth || height != _browserHeight))
                            return;
                        _browserSizeChanged = false;
                        var image = GetImage(_browserWidth, _browserHeight);
                        var nativeimage = NativeImage(image);
                        DoRenderBrowser(nativeimage, width, height, dirtyRects, buffer);
                        Image = image;
                    }
                    catch (Exception e)
                    {
                        _logger.ErrorException("WpfCefBrowser: Caught exception in HandleViewPaint()", e);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.ErrorException("WpfCefBrowser: Caught exception in HandleViewPaint()", ex);
            }
        }

        public void HandlePopupPaint(int width, int height, CefRectangle[] dirtyRects, IntPtr sourceBuffer)
        {
            if (width == 0 || height == 0) return;

            InvokeUiAction(
                () =>
                {
                    var stride = width * 4;
                    var sourceBufferSize = stride * height;

                    _logger.Debug("RenderPopup() Bitmap H{0}xW{1}, Browser H{2}xW{3}", _popupImageBitmap.Height,
                        _popupImageBitmap.Width, width, height);


                    foreach (var dirtyRect in dirtyRects)
                    {
                        _logger.Debug(
                            string.Format(
                                "Dirty rect [{0},{1},{2},{3}]",
                                dirtyRect.X,
                                dirtyRect.Y,
                                dirtyRect.Width,
                                dirtyRect.Height));

                        if (dirtyRect.Width == 0 || dirtyRect.Height == 0) continue;

                        var adjustedWidth = dirtyRect.Width;

                        var adjustedHeight = dirtyRect.Height;

                        var sourceRect = new Int32Rect(dirtyRect.X, dirtyRect.Y, adjustedWidth, adjustedHeight);

                        _popupImageBitmap.WritePixels(sourceRect, sourceBuffer, sourceBufferSize, stride,
                            dirtyRect.X, dirtyRect.Y);
                    }
                });
        }

        private BitmapImage GetImage(int PixelWidth, int PixelHeight)
        {
            if (_browserPageImage != null && _browserPageImage.PixelWidth != PixelWidth &&
                _browserPageImage.PixelHeight != PixelHeight)
            {
                _browserPageImage.Dispose();
                _browserPageImage = null;
            }
            if (_browserPageImage == null)
                using (var builder = new ImageBuilder(PixelWidth, PixelHeight))
                {
                    _browserPageImage = builder.ToBitmap(ImageFormat.ARGB32);
                }
            return _browserPageImage;
        }

        private static WriteableBitmap NativeImage(BitmapImage source)
        {
            if (source == null) return null;
            var nativeImage = Toolkit.CurrentEngine.GetNativeImage(source) as WriteableBitmap;
            if (nativeImage == null) //make it writible if not
            {
                source.SetPixel(0, 0, new Color());
                nativeImage = Toolkit.CurrentEngine.GetNativeImage(source) as WriteableBitmap;
            }
            return nativeImage;
        }

        public void DoRenderBrowser(WriteableBitmap target, int browserWidth, int browserHeight,
            CefRectangle[] dirtyRects, IntPtr sourceBuffer)
        {
            if(_disposed)
                return;
            try
            {
                var stride = browserWidth * 4;
                var sourceBufferSize = stride * browserHeight;
                if (browserWidth == 0 || browserHeight == 0) return;

                foreach (var dirtyRect in dirtyRects) {
                    if (dirtyRect.Width == 0 || dirtyRect.Height == 0) continue;

                    // If the window has been resized, make sure we never try to render too much
                    var adjustedWidth = dirtyRect.Width;
                    var adjustedHeight = dirtyRect.Height;

                    // Update the dirty region
                    var sourceRect = new Int32Rect(dirtyRect.X, dirtyRect.Y, adjustedWidth, adjustedHeight);
                    target.WritePixels(sourceRect, sourceBuffer, sourceBufferSize, stride, dirtyRect.X, dirtyRect.Y);
                }
            }
            catch(Exception e) 
            {
                _logger.ErrorException("WpfCefBrowser: Caught exception in HandleViewPaint()", e); 
            }
            
        }


        public void OnPopupShow(bool show)
        {
            /*if (_popup == null) {
                return;
            }

            _mainUiDispatcher.Invoke(new Action(() => _popup.IsOpen = show));*/
        }

        public void OnPopupSize(CefRectangle rect)
        {
            /*_mainUiDispatcher.Invoke(
                new Action(
                    () =>
                    {
                        _popupImageBitmap = null;
                        _popupImageBitmap = new WriteableBitmap(
                            rect.Width,
                            rect.Height,
                            96,
                            96,
                            PixelFormats.Bgr32,
                            null);

                        _popupImage.Source = this._popupImageBitmap;

                        _popup.Width = rect.Width;
                        _popup.Height = rect.Height;
                        _popup.HorizontalOffset = rect.X;
                        _popup.VerticalOffset = rect.Y;
                    }));*/
        }

        public bool OnTooltip(string text)
        {
            /*if (string.IsNullOrEmpty(text)) {
                _tooltipTimer.Stop();
                UpdateTooltip(null);
            }
            else {
                _tooltipTimer.Tick += (sender, args) => UpdateTooltip(text);
                _tooltipTimer.Start();
            }
            */
            return true;
        }

        public void OnBeforeClose(CefBrowser browser)
        {
            
        }

        public void OnCursorChange(IntPtr Win32Cursohandle)
        {
            //not inplement this
        }

        #endregion

        #region Utils

        /// <summary>
        ///     Finds a parent of the specific type
        /// </summary>
        private static Widget GrandParent(Widget obj)
        {
            var parentObj = obj.Parent;
            if (parentObj == null)
                return obj;

            return GrandParent(parentObj);
        }

        private static CefEventFlags GetMouseModifiers()
        {
            var modifiers = new CefEventFlags();

            if (Mouse.LeftButton == MouseButtonState.Pressed)
                modifiers |= CefEventFlags.LeftMouseButton;

            if (Mouse.MiddleButton == MouseButtonState.Pressed)
                modifiers |= CefEventFlags.MiddleMouseButton;

            if (Mouse.RightButton == MouseButtonState.Pressed)
                modifiers |= CefEventFlags.RightMouseButton;

            return modifiers;
        }

        private static CefEventFlags GetKeyboardModifiers()
        {
            var modifiers = new CefEventFlags();

            if (Keyboard.CurrentModifiers == ModifierKeys.Alt)
                modifiers |= CefEventFlags.AltDown;

            if (Keyboard.CurrentModifiers == ModifierKeys.Control)
                modifiers |= CefEventFlags.ControlDown;

            if (Keyboard.CurrentModifiers == ModifierKeys.Shift)
                modifiers |= CefEventFlags.ShiftDown;

            return modifiers;
        }

        /*private Xwt.PopupWindow CreatePopup() {
            var popup = new PopupWindow();
            return popup;
        }*/

        /*private Image CreatePopupImage() {
           var temp = new Image();

            RenderOptions.SetBitmapScalingMode(temp, BitmapScalingMode.NearestNeighbor);

            temp.Stretch = Stretch.None;
            temp.HorizontalAlignment = HorizontalAlignment.Left;
            temp.VerticalAlignment = VerticalAlignment.Top;
            temp.Source = _popupImageBitmap;

            return temp;
        }*/

        /*private void UpdateTooltip(string text) {
            _mainUiDispatcher.Invoke(
                DispatcherPriority.Render,
                new Action(
                    () =>
                    {
                        if (string.IsNullOrEmpty(text)) {
                            _tooltip.IsOpen = false;
                        }
                        else {
                            _tooltip.Placement = PlacementMode.Mouse;
                            _tooltip.Content = text;
                            _tooltip.IsOpen = true;
                            _tooltip.Visibility = Visibility.Visible;
                        }
                    }));

            _tooltipTimer.Stop();
        }*/

        private void TooltipOnClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            //_tooltip.Visibility = Visibility.Collapsed;
            //_tooltip.Placement = PlacementMode.Absolute;
        }

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
            if (_browser != null)
                _browser.Reload();
        }

        #endregion
    }
}