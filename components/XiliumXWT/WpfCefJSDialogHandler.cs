using Xilium.CefGlue;

namespace XiliumXWT
{
    public class WpfCefJSDialogHandler : CefJSDialogHandler
    {
        protected override bool OnJSDialog(CefBrowser browser, string originUrl, CefJSDialogType dialogType,
            string message_text, string default_prompt_text, CefJSDialogCallback callback, out bool suppress_message)
        {
            var success = false;
            string input = null;

            switch (dialogType)
            {
                case CefJSDialogType.Alert:
                    success = ShowJSAlert(message_text);
                    break;
                case CefJSDialogType.Confirm:
                    success = ShowJSConfirm(message_text);
                    break;
                case CefJSDialogType.Prompt:
                    success = ShowJSPromt(message_text, default_prompt_text, out input);
                    break;
            }

            callback.Continue(success, input);
            suppress_message = false;
            return true;
        }

        protected override bool OnBeforeUnloadDialog(CefBrowser browser, string messageText, bool isReload,
            CefJSDialogCallback callback)
        {
            return true;
        }

        protected override void OnDialogClosed(CefBrowser browser)
        {
        }

        protected override void OnResetDialogState(CefBrowser browser)
        {
        }

        private bool ShowJSAlert(string message)
        {
            var alert = new WpfCefJSAlert(message);
            return alert.ShowDialog() == true;
        }

        private bool ShowJSConfirm(string message)
        {
            var confirm = new WpfCefJSConfirm(message);
            return confirm.ShowDialog() == true;
        }

        private bool ShowJSPromt(string message, string defaultText, out string input)
        {
            var promt = new WpfCefJSPrompt(message, defaultText);
            if (promt.ShowDialog() == true)
            {
                input = promt.Input;
                return true;
            }
            input = null;
            return false;
        }
    }
}