using System;
using XiliumXWT;
using Xwt;

namespace BridgeConsole.Windows
{
    internal class RawBrowserWindow : IDisposable
    {
        private readonly WindowInfo _settings;
        private IDisposable _timeout;
        private XwtCefRawBrowser _browser;
        public RawBrowserWindow(string path, WindowInfo settings)
        {
            _settings = settings;
            PreparePostition(settings);
            _browser = new XwtCefRawBrowser(new Point(_settings.Left, _settings.Top), new Size(_settings.Width, _settings.Heigth), FormatUrl(path));
            _browser.OnClose += _browser_OnClose;
            AdjustLocation(settings);
        }
        private static string FormatUrl(string baseUrl)
        {
            return CEFConfig.Get().AppUrl() + baseUrl;
        }


        public void Dispose()
        {
            _browser?.Dispose();
            _browser = null;
            _timeout?.Dispose();
            _timeout = null;
        }

        internal IExternalTouchScreenHandler Display() => _browser;

        void AdjustLocation(WindowInfo settings)
        {
            PreparePostition(settings);

            _browser?.Resize(new Size(_settings.Width, _settings.Heigth));

            FixPosition();
            _timeout = Xwt.Application.TimeoutInvoke(100, FixPosition);
        }

        private static void PreparePostition(WindowInfo settings)
        {
            if (settings.IsAutoFit)
            {
                var screens = Xwt.Desktop.Screens;
                if (settings.AutoDisplayIndex >= screens.Count)
                    throw new Exception($"AutoDisplayIndex is out of display range");
                var screen = screens[settings.AutoDisplayIndex];
                var bounds = screen.VisibleBounds;
                settings.Left = (long)bounds.Left;
                settings.Top = (long)bounds.Top;
                settings.Width = (long)bounds.Width;
                settings.Heigth = (long)bounds.Height;
            }
        }

        private bool FixPosition()
        {
            _browser?.SetPosition(new Point(_settings.Left, _settings.Top));
            return true;
        }

        private void _browser_OnClose()
        {
            Application.InvokeAsync(() => Application.Exit());
        }
    }
}