using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using IE = Interop.SHDocVw;
using mshtml;

using Billbank.IEPlugin.Provider;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin {
    /// <summary>
    /// Add-in Express .NET for Internet Explorer Bar
    /// </summary>
    [ComVisible(true), Guid("0B7FFCFD-65AB-4701-B112-5C488CED1A04")]
    public class SideBar : AddinExpress.IE.ADXIEBar {

        #region Constants and class variables
        private const int WM_REFRESH = -1;
        private const int WM_LOGOUT = -2;
        private const int WM_LOGIN = -3;
        private const int WM_USER = 1024;
        private const int MSG_BASE = 1000;
        private const String WM_LOGOUT_STRING = "-2";
        private static readonly String FOCUS_SCRIPT = "document.getElementById('plugin_close_input').style.display = 'block';"
                + "document.getElementById('plugin_close_input').value = '{0}';"
                + "document.getElementById('plugin_close_input').focus();"
                + "document.getElementById('plugin_close_input').style.display = 'none';";

        private static readonly String BANKS_LIST_LINK = String.Format("{0}{1}", Settings.Default.BanksListLink, Settings.Default.PluginVersion);

        private WebBrowser webBrowser;
        private HtmlElement[] button = new HtmlElement[0];
        private PaymentInfo[] paymentInfo = new PaymentInfo[0];
        private Dictionary<String, int> paymentInfoIndexMap = new Dictionary<String, int>();

        private Label titleLabel;
        private Label footerLabel1;
        private LinkLabel footerLabel2;
        private Label footerLabel3;
        #endregion

        public SideBar() {
            InitializeComponent();
        }

        public SideBar(int mode)
            :
            base(mode) {
            InitializeComponent();
        }

        #region Component Designer generated code
        /// <summary>
        /// Required by designer
        /// </summary>
        private System.ComponentModel.IContainer components;

        /// <summary>
        /// Required by designer support - do not modify
        /// the following method
        /// </summary>
        private void InitializeComponent() {
            this.BackColor = System.Drawing.SystemColors.MenuBar;
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.titleLabel = new System.Windows.Forms.Label();
            this.footerLabel2 = new System.Windows.Forms.LinkLabel();
            this.footerLabel1 = new System.Windows.Forms.Label();
            this.footerLabel3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // webBrowser
            // 
            this.webBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.webBrowser.Location = new System.Drawing.Point(0, 24);
            this.webBrowser.Margin = new System.Windows.Forms.Padding(6);
            this.webBrowser.MinimumSize = new System.Drawing.Size(110, 22);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(330, 148);
            this.webBrowser.TabIndex = 0;
            this.webBrowser.IsWebBrowserContextMenuEnabled = false;
            this.webBrowser.WebBrowserShortcutsEnabled = false;
            this.webBrowser.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.SideBar_DocumentCompleted);
            // 
            // titleLabel
            // 
            //this.titleLabel.BackColor = System.Drawing.SystemColors.MenuBar;
            this.titleLabel.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.titleLabel.Location = new System.Drawing.Point(3, 2);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(325, 20);
            this.titleLabel.TabIndex = 1;
            // 
            // footerLabel2
            // 
            System.Drawing.Font footerFont = new System.Drawing.Font("Arial Narrow", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.footerLabel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.footerLabel2.LinkArea = new System.Windows.Forms.LinkArea(20, 9);
            this.footerLabel2.Font = footerFont;
            this.footerLabel2.Location = new System.Drawing.Point(0, 189);
            this.footerLabel2.Name = "footerLabel2";
            this.footerLabel2.Size = new System.Drawing.Size(330, 14);
            this.footerLabel2.TabIndex = 3;
            this.footerLabel2.TabStop = true;
            this.footerLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.footerLabel2.UseCompatibleTextRendering = true;
            // 
            // footerLabel1
            // 
            this.footerLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.footerLabel1.Font = footerFont;
            this.footerLabel1.Location = new System.Drawing.Point(0, 175);
            this.footerLabel1.Name = "footerLabel1";
            this.footerLabel1.Size = new System.Drawing.Size(330, 14);
            this.footerLabel1.TabIndex = 2;
            this.footerLabel1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // footerLabel3
            // 
            this.footerLabel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.footerLabel3.Font = footerFont;
            this.footerLabel3.BackColor = System.Drawing.SystemColors.Control;
            this.footerLabel3.Location = new System.Drawing.Point(0, 203);
            this.footerLabel3.Name = "footerLabel3";
            this.footerLabel3.Size = new System.Drawing.Size(330, 14);
            this.footerLabel3.TabIndex = 4;
            this.footerLabel3.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // SideBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(106F, 106F);
            this.Controls.Add(this.footerLabel3);
            this.Controls.Add(this.footerLabel1);
            this.Controls.Add(this.footerLabel2);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.webBrowser);
            this.MinimumSize = new System.Drawing.Size(200, 100);
            this.Name = "SideBar";
            this.Size = new System.Drawing.Size(330, 220);
            this.DocumentComplete += new AddinExpress.IE.ADXIEDocumentComplete_EventHandler(this.IEModule_DocumentComplete);
            this.VisibleChanged += new EventHandler(this.SideBar_VisibleChanged);
            this.OnSendMessage += new AddinExpress.IE.ADXIESendMessage_EventHandler(SideBar_OnSendMessage);
            this.ResumeLayout(false);

            this.titleLabel.Text = Resources.LoginPlease;
            this.footerLabel1.Text = Resources.FooterText1;
            this.footerLabel2.Text = Resources.FooterText2;
            this.footerLabel3.Text = Resources.FooterText3;
            this.footerLabel2.LinkArea = new System.Windows.Forms.LinkArea(Resources.FooterText2.IndexOf(Resources.FooterLinkText), Resources.FooterLinkText.Length);
            this.footerLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.Footer_LinkClicked);
        }
        #endregion

        #region ADX automatic code

        // Required by Add-in Express - do not modify
        // the methods within this region

        public override System.ComponentModel.IContainer GetContainer() {
            if (components == null)
                components = new System.ComponentModel.Container();
            return components;
        }

        #endregion

        #region Class properties
        public IE.WebBrowser IEApp {
            get {
                return (this.IEObj as IE.WebBrowser);
            }
        }

        internal WebBrowser WebBrowser {
            set {
                this.webBrowser = value;
            }
        }

        public mshtml.HTMLDocument HTMLDocument {
            get {
                return (this.HTMLDocumentObj as mshtml.HTMLDocument);
            }
        }
        #endregion

        #region Auxiliary methods
        /// <summary>
        /// Adds event handlers (for 'Click') for all added buttons.
        /// </summary>
        private void AddPayButtonHandlers() {
            for (int i = 0; i < button.Length; i++) {
                HtmlElement buttonTmp = webBrowser.Document.GetElementById("plugin_payButton_" + i);
                if (buttonTmp != null) {
                    HtmlElementEventHandler e = new HtmlElementEventHandler(this.PayButton_Click);
                    button[i] = buttonTmp;
                    button[i].Click -= e; // remove the old one
                    button[i].Click += e;
                }
            }
        }

        /// <summary>
        /// Adds options to the iRachunki.pl mini web menu.
        /// </summary>
        private void AddMenuOptions() {
            if (webBrowser.Document.GetElementById("plugin_close_link") != null) {
                return;
            }
            HtmlElement menuList = webBrowser.Document.GetElementById("menuList");
            if (menuList == null) {
                return;
            }

            HtmlElement li = webBrowser.Document.CreateElement("li");
            HtmlElement link = webBrowser.Document.CreateElement("a");
            link.Id = "plugin_close_link"; 
            link.SetAttribute("href", "#");
            link.SetAttribute("onclick", String.Format(FOCUS_SCRIPT, "close"));
            link.InnerHtml = Resources.ClosePlugin;
            li.AppendChild(link);
            menuList.AppendChild(li);

            HtmlElement input = webBrowser.Document.CreateElement("input");
            input.SetAttribute("id", "plugin_close_input");
            input.SetAttribute("type", "text");
            input.Style = "display: none; width: 0px; height: 0px;";

            // this invisible element handles click elements on many links of the SideBar content
            // as well as some other notification events
            HtmlElementEventHandler focusEvent = new HtmlElementEventHandler(this.Link_OnClick);
            input.GotFocus -= focusEvent;
            input.GotFocus += focusEvent;
            webBrowser.Document.Body.AppendChild(input);
        }

        private void ModifyPageLinks() {
            IEnumerator e = webBrowser.Document.GetElementsByTagName("a").GetEnumerator();
            while (e.MoveNext()) {
                HtmlElement element = e.Current as HtmlElement;
                String onclick = element.GetAttribute("onclick");
                String href = element.GetAttribute("href");
                String target = element.GetAttribute("target");
                String clazz = (element.DomElement as IHTMLElement).className;
                String html = element.InnerHtml == null ? "" : element.InnerHtml.Trim().ToLower();

                if ("zamknij to okienko".Equals(html)) {
                    SetEventHandler(element, "");
                    continue;
                }

                // if the link is the one that logs out the user - notify all other SideBar instances
                if (href != null && href.Contains("logout.action")) {
                    element.SetAttribute("onclick", String.Format(FOCUS_SCRIPT, WM_LOGOUT));
                    continue;
                }

                if ("lista obs³ugiwanych banków".Equals(html)) {
                    element.SetAttribute("onclick", String.Format(FOCUS_SCRIPT, BANKS_LIST_LINK));
                    element.SetAttribute("href", "#");
                    element.SetAttribute("target", "_self");

                    continue;
                }

                // links that cannot be changed
                if (("".Equals(onclick) == false 
                    || href == null || href.Length == 0
                    || "refresh".Equals(clazz) 
                    || target == null || "_blank".Equals(target) == false)) {

                    continue;
                }

                // links in the main DIV that can only handle .Net Click event
                if ("gotoService".Equals(clazz) || "help".Equals(clazz)
                    || Util.ContainsLinkToHandle(html)) {

                    SetEventHandler(element, href);
                    continue;
                }
                // the rest of the links cannot handle .Net Click event (don't know why) and they have to
                // be handled by some other component on the main DIV (HTML Focus event)
                element.SetAttribute("onclick", String.Format(FOCUS_SCRIPT, href));
                element.SetAttribute("href", "#");
                element.SetAttribute("target", "_self");
            }
        }

        private void SetEventHandler(HtmlElement element, String linkValue) {
            element.SetAttribute("onclick", "");
            element.SetAttribute("link", linkValue);
            HtmlElementEventHandler clickEvent = new HtmlElementEventHandler(this.Link_OnClick);
            element.Click -= clickEvent;
            element.Click += clickEvent;
            element.SetAttribute("href", "#");
            element.SetAttribute("target", "_self");
        }

        /// <summary>
        /// Adds "Pay" links to the list of invoices.
        /// </summary>
        private void AddPayLinks() {
            HtmlElementCollection payActions = webBrowser.Document.GetElementsByTagName("td");
            if (payActions == null) {
                return;
            }
            payActions = payActions.GetElementsByName("payAction");
            button = new HtmlElement[payActions.Count];
            paymentInfo = new PaymentInfo[payActions.Count];

            for (int i = 0; i < payActions.Count; i++) {
                paymentInfo[i] = PaymentInfo.ValueOf(webBrowser.Document, payActions[i].Id);
                if (paymentInfo[i].Id != null && paymentInfoIndexMap.ContainsKey(paymentInfo[i].Id) == false) {
                    paymentInfoIndexMap.Add(paymentInfo[i].Id, i);
                }
                if (payActions[i] != null && (payActions[i].GetAttribute("changed") == null || "".Equals(payActions[i].GetAttribute("changed")))) {
                    button[i] = webBrowser.Document.CreateElement("a");
                    button[i].Id = "plugin_payButton_" + i;
                    button[i].SetAttribute("index", i.ToString());
                    button[i].SetAttribute("class", "link");
                    payActions[i].AppendChild(button[i]);
                    payActions[i].SetAttribute("changed", "true");
                }
            }
            AddPayButtonHandlers();
	    }

        /// <summary>
        /// Update side bar title with logged user name.
        /// </summary>
        private void UpdateSideBarTitle() {
            if (IsUserLogged()) {
                HtmlElement userLoginBox = webBrowser.Document.GetElementById("userLoginBox");
                titleLabel.Text = String.Format("{0} {1}", Resources.Welcome, userLoginBox.GetAttribute("value"));
                SendMessageToAll(WM_LOGIN);
            } else {
                titleLabel.Text = Resources.LoginPlease;
            }
        }

        private bool IsUserLogged() {
            HtmlElement userLoginBox = webBrowser.Document.GetElementById("userLoginBox");
            return (userLoginBox != null && userLoginBox.GetAttribute("value") != null && userLoginBox.GetAttribute("value").Length > 0);
        }

        /// <summary>
        /// Marks given payment info as paid.
        /// </summary>
        /// <param name="info">payment info</param>
        /// <param name="sendMessage">if true sends broadcast message to other tabs</param>
        private void MarkAsPaid(PaymentInfo info, bool sendMessage) {
            String markAmount = info.AmountPaid.Replace(",", ".");
            if (info.AmountToPayInt <= info.AmountPaidInt) {
                markAmount = info.AmountToPay.Replace(",", ".");
            }
            Object[] args = new Object[] { info.Id, 
                            info.Amount.Replace(",", "."), 
                            info.AmountToPay.Replace(",", "."), 
                            info.AmountPaid.Replace(",", "."), 
                            "zap³acono z plugina"};

            webBrowser.Document.InvokeScript("markAsPaidToday", args);
            if (info.AmountPaidInt < info.AmountToPayInt && paymentInfoIndexMap.ContainsKey(info.Id)) {
                paymentInfo[paymentInfoIndexMap[info.Id]].AmountToPayInt = info.AmountToPayInt - info.AmountPaidInt;
            }

            if (sendMessage == true) {
                Util.SetUpdatedPaymentInfo(info.ToString());
                SendMessageToAll(int.Parse(info.Id));
            }
        }

        private void ForceRefresh() {
            webBrowser.Url = new Uri(Settings.Default.LocationUrl);
        }

        private void ForceLogout() {
            webBrowser.Url = new Uri(Settings.Default.LogoutUrl);
        }

        private void ForceLogin() {
            if (IsUserLogged() == false) {
                webBrowser.Url = new Uri(Settings.Default.LocationUrl);
            }
        }

        private void SendMessageToAll(int msgId) {
            SendMessageToAll(WM_USER + MSG_BASE + msgId, IntPtr.Zero, IntPtr.Zero);
        }
        #endregion

        #region Browser / user events handling
        /// <summary>
        /// Event invoked when document is loaded into the sidebar web browser.
        /// </summary>
        private void SideBar_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) {
            try {
                UpdateSideBarTitle();
                AddMenuOptions();
                ModifyPageLinks();
                AddPayLinks();
            } catch (ArgumentException ex) {
                if (Util.ShowYesNoQuestion(this, Resources.CscErrorMessage, MessageBoxIcon.Error)) {
                    Util.ShowErrorMessage(ex.ToString());
                }
                Hide();
            } catch (Exception ex) {
                Util.ShowErrorMessage(ex.ToString());
            }
        }

        private void SideBar_VisibleChanged(object sender, EventArgs e) {
            if (Visible == true && webBrowser.Url == null) {
                webBrowser.Url = new Uri(Settings.Default.LocationUrl);
            }
        }

        /// <summary>
        /// Event invoked when user clicks "Pay" button in the sidebar.
        /// </summary>
        /// <param name="sender">pressed button's object</param>
        /// <param name="e">event arguments</param>
        private void PayButton_Click(object sender, System.EventArgs e) {
            try {
                Settings.Default.ReloadCount = 0;

                HtmlElement element = sender as HtmlElement;
                int idx = int.Parse(element.GetAttribute("index"));
                PaymentProvider provider = ProviderFactory.GetProvider(Util.GetProviderId(IEApp.LocationURL), true);

                paymentInfo[idx] = PaymentInfo.ValueOf(webBrowser.Document, paymentInfo[idx].Id);
                // leave original object unchanged
                PaymentInfo info = paymentInfo[idx].Clone();
                Util.SetCurrentPaymentInfo(info.ToString());
                provider.Execute(new PaymentRequest(IEApp, info, true));
                Util.SetCurrentPaymentInfo(info.ToString());
            } catch (Exception ex) {
                Util.ShowErrorMessage(ex.ToString());
            }
        }

        /// <summary>
        /// Opens miniWeb links in the new tab instead of new window.
        /// Sends notifications to all SideBar instances depending on the concrete event.
        /// </summary>
        /// <param name="sender">sender element</param>
        /// <param name="e">event arguments</param>
        private void Link_OnClick(object sender, HtmlElementEventArgs e) {
            // if event was caused by click()
            if ("click".Equals(e.EventType.ToLower())) {
                String link = (sender as HtmlElement).GetAttribute("link");
                if (link.Length == 0) {
                    Hide();
                } else {
                    Util.OpenUrl(IEApp, (sender as HtmlElement).GetAttribute("link"));
                }
                return;
            }

            // if event was caused by focus()
            String value = this.webBrowser.Document.GetElementById("plugin_close_input").GetAttribute("value");
            switch (value) {
                case "close":
                    Hide();
                    break;

                case WM_LOGOUT_STRING:
                    SendMessageToAll(WM_LOGOUT);
                    break;

                default:
                    if (value != null && value.Trim().Length > 0) {
                        Util.OpenUrl(IEApp, value);
                    }
                    break;
            }
            webBrowser.Document.GetElementById("plugin_close_input").SetAttribute("value", "");
        }

        /// <summary>
        /// Event invoked when IE browser completes loading the document.
        /// </summary>
        private void IEModule_DocumentComplete(object pDisp, string url) {
            try {
                if ("https://irachunki.pl/miniWeb/definedTransferSave.action".Equals(url)) {
                    SendMessageToAll(WM_REFRESH);
                    return;
                }

                Util.SetUpdatedPaymentInfo("");

                PaymentInfo info = PaymentInfo.ValueOf(Application.UserAppDataRegistry.GetValue(Util.CURRENT_PAYMENT).ToString());
                if (info.State != State.INVALID && Visible == true) {
                    PaymentProvider tmp = ProviderFactory.GetProvider(Util.GetProviderId(IEApp.LocationURL));
                    info = tmp.Execute(new PaymentRequest(IEApp, info, url));
                    Util.SetCurrentPaymentInfo(info.ToString());
                }

                if (info.State == State.PAID) {
                    String markAmount = info.AmountPaid.Replace(".", ",");
                    if (info.AmountToPayInt <= info.AmountPaidInt) {
                        markAmount = info.AmountToPay.Replace(".", ",");
                    }
                    if (Util.ShowYesNoQuestion(this, String.Format(Resources.MarkAsPaidQuestion, markAmount)) == true) {
                        MarkAsPaid(info, true);
                    }
                    Util.SetCurrentPaymentInfo("");
                }
            } catch (Exception e) {
                Util.SetCurrentPaymentInfo("");
                Util.ShowErrorMessage(e.ToString());
            }
        }

        /// <summary>
        /// Event invoked when user clicks footer's "Conditions" link
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Footer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Util.OpenUrl(IEApp, Resources.FooterLink);
        }

        /// <summary>
        /// Updates paid invoice in current tab.
        /// </summary>
        /// <param name="e">id of the payment info</param>
        private void SideBar_OnSendMessage(AddinExpress.IE.ADXIESendMessageEventArgs e) {
            try {
                int id = e.Message - MSG_BASE - WM_USER;
                switch (id) {
                    case WM_REFRESH:
                        ForceRefresh();
                        break;

                    case WM_LOGOUT:
                        ForceLogout();
                        break;

                    case WM_LOGIN:
                        ForceLogin();
                        break;

                    default:
                        PaymentInfo info = PaymentInfo.ValueOf(Application.UserAppDataRegistry.GetValue(Util.UPDATED_PAYMENT).ToString());
                        if (id.ToString().Equals(info.Id) && info.State == State.PAID) {
                            MarkAsPaid(info, false);
                        }
                        break;
                }
            } catch {
                // ignore
            }
        }
        #endregion
    }
}