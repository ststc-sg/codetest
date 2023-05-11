using System;

namespace Xilium.CefGlue.WPF
{
    internal sealed class WpfCefLifeSpanHandler : CefLifeSpanHandler
    {
        private readonly ScreenCefBrowserGlue _owner;

        public WpfCefLifeSpanHandler(ScreenCefBrowserGlue owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");

            _owner = owner;
        }

        protected override void OnAfterCreated(CefBrowser browser)
        {
            _owner.HandleAfterCreated(browser);
        }

        protected override void OnBeforeClose(CefBrowser browser)
        {
            base.OnBeforeClose(browser);
            _owner.OnBeforeClose(browser);
            
        }
    }
}