using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools.V156.Debugger;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Reposter;

namespace Reposter
{
    public static class JavaScriptHelper
    {
        public static async Task RunJavascript(this WebDriver driver, string code, string selector = null,string text = null)
        {
            if (!string.IsNullOrEmpty(selector))
            {
                selector = selector.Trim('\'','\"');
                code = code.Replace("selector", selector);
            }

            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim('\'', '\"');
                code = code.Replace("text", text);
            }

            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript(code);
                Program.logger.Debug($"Javascript code: {code}");
            }
            catch (Exception ex)
            {
                Program.logger.Error(ex);
            }
            await Task.Delay(1000);
        }

        public static string GetScriptCode(JavascriptFuncEnum snippets) 
        {
            switch (snippets)
            {
                case JavascriptFuncEnum.RemoveAds:
                    return "const elementToRemove = document.querySelector('div.trg-b-list');if(elementToRemove){elementToRemove.remove();}" +
                           "const elementToRemove2 = document.querySelector('div.trg-b-wrap');if(elementToRemove2){elementToRemove2.remove();}";
                case JavascriptFuncEnum.ClearInputs:
                    return "document.querySelector('selector').value = '';";
                case JavascriptFuncEnum.PasteText:
                    return "document.querySelector('selector').value = 'text';";
                default:
                    return null;
            }
            ;
        }
    }
}
