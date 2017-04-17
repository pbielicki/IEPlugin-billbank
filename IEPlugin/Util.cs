using Microsoft.Win32;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using IE = Interop.SHDocVw;
using mshtml;

namespace Billbank.IEPlugin {
    internal class Util {

        #region Constants
        internal static readonly String CURRENT_PAYMENT = "CurrentPayment";
        internal static readonly String INSTALLED_VERSION = "InstalledVersion";
        internal static readonly String UPDATED_PAYMENT = "UpdatedPayment";
        private static readonly String[] MONTH_PL = new String[] { "", "styczeñ", "luty", "marzec", 
                                                                    "kwiecieñ", "maj", "czerwiec", "lipiec", 
                                                                    "sierpieñ", "wrzesieñ", "paŸdziernik", 
                                                                    "listopad", "grudzieñ" };
        #endregion

        #region Show different dialog windows
        public static void ShowWarningMessage(String message) {
            if (GlobalData.Instance.TestMode == false) {
                MessageBox.Show(message, Settings.Default.PluginTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static void ShowInfoMessage(String message) {
            if (GlobalData.Instance.TestMode == false) {
                MessageBox.Show(message, Settings.Default.PluginTitle);
            }
        }

        public static void ShowErrorMessage(String message) {
            if (GlobalData.Instance.TestMode == false) {
                MessageBox.Show(message, Settings.Default.PluginTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows Yes/No dialog with given question. Returns true if user pressed "Yes" or "No" otherwise.
        /// </summary>
        /// <param name="parent">parent window</param>
        /// <param name="message">question to be displayed</param>
        /// <returns>true if user pressed "Yes" or "No" otherwise</returns>
        public static bool ShowYesNoQuestion(IWin32Window parent, String message, params MessageBoxIcon[] icons) {
            if (GlobalData.Instance.TestMode == false) {
                MessageBoxIcon icon = MessageBoxIcon.Question;
                if (icons.Length == 1) {
                    icon = icons[0];
                }

                return MessageBox.Show(parent, message,
                    Settings.Default.PluginTitle,
                    MessageBoxButtons.YesNo,
                    icon) == DialogResult.Yes;

            } else {
                return GlobalData.Instance.TestQuestionAnswer;
            }
        }
        #endregion

        #region Web browser / HTML element utils
        /// <summary>
        /// Opens URL in the current window/tab
        /// </summary>
        public static void OpenUrl(IE.WebBrowser browser, String urlString) {
            OpenUrl(browser, urlString, 0x2, "_self");
        }

        /// <summary>
        /// Opens URL in new the window/tab
        /// </summary>
        public static void OpenUrlInNewTab(IE.WebBrowser browser, String urlString) {
            OpenUrl(browser, urlString, 0x0800, "_blank");
        }

        private static void OpenUrl(IE.WebBrowser browser, String urlString, object flags, String targetWindow) {
            object url = urlString;
            object window = targetWindow;
            object nullObj = null;
            browser.Navigate2(ref url, ref flags, ref window, ref nullObj, ref nullObj);
        }

        public static IHTMLDocument2 GetFrameDocument(HTMLDocument document, int frameIndex) {
            HTMLWindow2 frame = GetFrame(document, frameIndex);
            if (frame != null) {
                return frame.document;
            }
            return null;
        }

        public static HTMLWindow2 GetFrame(HTMLDocument document, int frameIndex) {
            if (document.frames.length > frameIndex) {
                object idx = frameIndex; // zero based index
                return document.frames.item(ref idx) as HTMLWindow2;
            }
            return null;
        }

        /// <summary>
        /// Retrieves number of frames from proviede document
        /// </summary>
        /// <param name="document">HTML document</param>
        /// <returns>frames count in provided document</returns>
        public static int GetFramesCount(HTMLDocument document) {
            if (document == null || document.frames == null) {
                return 0;
            }
            return document.frames.length;
        }

        /// <summary>
        /// Runs given script (e.g. method with parameters) from a given web document.
        /// </summary>
        /// <param name="doc">HTML document containing JavaScript to run</param>
        /// <param name="script">script string to run (e.g. doSomething('param1', 123))</param>
        /// <returns>true if execution of JavaScript was successful or false otherwise</returns>
        public static bool RunJavaScript(HTMLDocument doc, String script) {
            IHTMLWindow2 window = doc.parentWindow;
            if (window != null) {
                try {
                    window.execScript(script, "JScript");
                    return true;
                } catch {
                    // ignore
                }
            }
            return false;
        }

        /// <summary>
        /// Safely sets the value of given HTML element (IHTMLInputElement or IHTMLTextAreaElement).
        /// If any other element will be passed this request will be ignored.
        /// </summary>
        /// <param name="element">IHTMLInputElement or IHTMLTextAreaElement whose value is to be set</param>
        /// <param name="value">String value</param>
        /// <param name="args">optional params for firing event on Select element</param>
        public static int SetElementValue(Object element, String value, params Object[] args) {
            if (element != null) {
                if (element is IHTMLInputElement) {
                    ((IHTMLInputElement)element).value = value;
                } else if (element is IHTMLSelectElement) {
                    IHTMLElement select = element as IHTMLElement;
                    Dictionary<String, List<int>> options = GetOptionsMap(select);
                    if (options.ContainsKey(value.ToLower())) {
                        ((IHTMLSelectElement)element).selectedIndex = options[value.ToLower()][0];
                        if (args.Length > 0) {
                            ((IHTMLElement3)element).FireEvent("onchange", ref args[0]);
                        }
                        return options[value.ToLower()].Count;
                    }
                    return -1;
                } else if (element is IHTMLTextAreaElement) {
                    ((IHTMLTextAreaElement)element).value = value;
                } else if (element is IHTMLElementCollection) {
                    IHTMLElementCollection col = element as IHTMLElementCollection;
                    if (col.length > 0) {
                        return SetElementValue(col.item(null, 0), value, args);
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns the HTML OPTIONS map where the key of the entry is value of the OPTION tag 
        /// and the value of the entry is index of given element.
        /// </summary>
        /// <param name="select">SELECT HTML element</param>
        /// <returns>key of the entry is value of the OPTION tag 
        /// and the value of the entry is index of given element</returns>
        public static Dictionary<String, List<int>> GetOptionsMap(IHTMLElement select) {
            Dictionary<String, List<int>> result = new Dictionary<String, List<int>>();
            IEnumerator e = (select.all as IHTMLElementCollection).GetEnumerator();

            int i = 0;
            while (e.MoveNext()) {
                if (e.Current is IHTMLOptionElement) {
                    String text = (e.Current as IHTMLOptionElement).text;
                    if (text == null) {
                        continue;
                    }
                    text = text.ToLower();
                    if (result.ContainsKey(text) == false) {
                        List<int> list = new List<int>();
                        list.Add(i++);
                        result.Add(text, list);
                    } else {
                        result[text].Add(i++);
                    }
                }
            }
            return result;
        }
        #endregion

        #region Windows registry utils
        internal static void SetCurrentPaymentInfo(String infoAsString) {
            Application.UserAppDataRegistry.SetValue(CURRENT_PAYMENT, infoAsString);
        }

        internal static void SetUpdatedPaymentInfo(String infoAsString) {
            Application.UserAppDataRegistry.SetValue(UPDATED_PAYMENT, infoAsString);
        }

        internal static bool IsFirstExecution() {
            RegistryKey masterKey = Registry.CurrentUser.CreateSubKey("Software\\Billbank");
            return masterKey.GetValue(INSTALLED_VERSION) == null
                || "iRachunki".Equals(masterKey.GetValue(INSTALLED_VERSION)) == false;
        }

        internal static void ResetFirstExecutionFlag() {
            RegistryKey masterKey = Registry.CurrentUser.CreateSubKey("Software\\Billbank");
            masterKey.SetValue(INSTALLED_VERSION, "iRachunki");
        }
        #endregion

        #region String utils
        /// <summary>
        /// Gets the payment provider id from given URL
        /// </summary>
        /// <param name="url">URL of the current webpage displayed</param>
        /// <returns></returns>
        public static String GetProviderId(String url) {
            int idx = url.IndexOf("//");
            if (idx != -1) {
                idx = url.IndexOf("/", idx + 2);
                if (idx == -1) {
                    return url;
                } else {
                    return url.Substring(0, idx);
                }
            }

            return "";
        }

        public static String GetMonth(int monthNo) {
            if (monthNo < 1 || monthNo > 12) {
                return "";
            }
            return MONTH_PL[monthNo];
        }

        internal static bool ContainsLinkToHandle(String html) {
            for (int i = 0; i < Settings.Default.LinksInnerHtmlToHandle.Count; i++) {
                if (Settings.Default.LinksInnerHtmlToHandle[i].Equals(html)) {
                    return true;
                }
            }
            return false;
        }

        internal static String[] SplitString(String text, int partLenght, int maxParts) {
            List<String> result = new List<String>();

            text = text.Trim();

            StringBuilder sb = new StringBuilder();
            int count = 0;
            while (text.Length > 0 && count < maxParts) {
                int idx = text.IndexOf(" ");
                if (idx == -1 || idx > partLenght) {
                    if (sb.Length > 0) {
                        if (sb.Length + text.Length <= partLenght) {
                            sb.Append(text);
                            text = "";
                        }
                        result.Add(sb.ToString().Trim());
                        sb = new StringBuilder();
                        count++;
                        continue;
                    }

                    if (text.Length >= partLenght) {
                        result.Add(text.Substring(0, partLenght).Trim());
                        count++;
                        text = text.Substring(partLenght);
                    } else {
                        result.Add(text.Trim());
                        count++;
                        text = "";
                    }
                } else if (sb.Length + idx <= partLenght) {
                    sb.Append(text.Substring(0, idx + 1));
                    text = text.Substring(idx + 1);
                } else {
                    result.Add(sb.ToString().Trim());
                    count++;
                    sb = new StringBuilder();
                }
            }

            if (count < maxParts) {
                for (int i = 0; i < maxParts - count; i++) {
                    result.Add("");
                }
            }
            
            return result.ToArray();
        }
        #endregion
    }
}
