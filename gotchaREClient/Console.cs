using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gotchaREClient
{
    public static class Console
    {
        private static MainPage pageHandle;
        public static void setTextBlock(MainPage page)
        {
            pageHandle = page;
        }

        public static async void WriteLine(string tag, string text)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
             {
                 if (pageHandle.ConsoleTextBlock.Text.Length > 5000)
                     pageHandle.ConsoleTextBlock.Text = "";

                 pageHandle.ConsoleTextBlock.Text = DateTime.Now.ToString() + " [" + tag + "] " + text + '\n' + pageHandle.ConsoleTextBlock.Text;
             });
        }
    }
}
