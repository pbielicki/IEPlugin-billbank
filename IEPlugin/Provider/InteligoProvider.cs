using System;
using System.Collections;
using System.Windows.Forms;
using mshtml;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class InteligoProvider : AbstractProvider {

        #region Constatns
        internal static readonly String ACCOUNT_FIELD = "ben_acc";
        internal static readonly String[] BENEFICIARY_NAME_FIELD = new String[] {"beneficiary_name", "beneficiary_name_cont"};
        internal static readonly String ADDRESS_1_FIELD = "beneficiary_address_1";
        internal static readonly String ADDRESS_2_FIELD = "beneficiary_address_2";
        internal static readonly String[] TITLE_FIELD = new String[] { "title_1", "title_2", "title_3", "title_4" };
        internal static readonly String AMOUNT_1_FIELD = "amount1";
        internal static readonly String AMOUNT_2_FIELD = "amount2";
        internal static readonly String YEAR_FIELD = "pay_date_y";
        internal static readonly String MONTH_FIELD = "pay_date_m";
        internal static readonly String DAY_FIELD = "pay_date_d";

        private static readonly Char[] CHARS_TO_REPLACE = "|~\"".ToCharArray();
        private static readonly Char[] CHAR_REPLACEMENT = "  '".ToCharArray();
        #endregion

        #region Overriden methods
        protected override PaymentInfo ProcessInProgress(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            if (ReadyToPaste(request)) {
                return info;
            }
            if (IsConfirmPage(request)) {
                IEnumerator e = request.Document.getElementsByTagName("td").GetEnumerator();
                while (e.MoveNext()) {
                    IHTMLElement element = e.Current as IHTMLElement;
                    if ("Kwota".Equals(element.innerHTML) && e.MoveNext() == true) {
                        element = e.Current as IHTMLElement;
                        info.AmountPaid = element.innerHTML.Substring(0, element.innerHTML.IndexOf(" "));
                        info.State = State.IN_PROGRESS_CONFIRM;
                        return info;
                    } else if ("Numer rachunku odbiorcy".Equals(element.innerHTML) && e.MoveNext() == true) {
                        info.BankAccountNo = (e.Current as IHTMLElement).innerHTML;
                    }
                }
            }
            info.State = State.INVALID;
            return info;
        }

        /// <summary>
        /// Goes to transfer page depending on whether current payment is a defined transfer or not.
        /// </summary>
        /// <param name="request">payment request</param>
        protected override void GoToTransferPage(PaymentRequest request) {
            bool success = false;
            if (request.PaymentInfo.IsDefinedTransfer == false) {
                success = Util.RunJavaScript(request.Document, "clickMenu('onetime_transfer')");
            } else {
                success = Util.RunJavaScript(request.Document, "clickMenu('payments')");
            }

            if (success == false) {
                request.Document.getElementById("przelewy").click();
            }
        }

        /// <summary>
        /// Checks if current page is the page displaying list of defined transfers.
        /// </summary>
        /// <param name="doc">HTML document</param>
        /// <returns>true if current page is the page displaying list of defined transfers. or false otherwise</returns>
        protected override bool IsDefinedTransfersPage(HTMLDocument doc) {
            return TrimHtml(doc.body.innerHTML).ToLower().Contains("twoje ustalone p³atnoœci dostêpne przez www.");
        }

        protected override bool IsUserLogged(PaymentRequest request) {
            return request.Document.getElementById("koniec") != null;
        }

        /// <summary>
        /// Checks if current page is the page with the transfer form to fill (depending on whether
        /// current payment should be executed as defined transfer or not).
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>true if current page is the page with the transfer form to fill or false otherwise</returns>
        protected override bool ReadyToPaste(PaymentRequest request) {
            if (request.Document.getElementsByName("title_1").length != 1) {
                return false;
            }
            IHTMLElement title = request.Document.getElementsByName("title_1").item("title_1", 0) as IHTMLElement;
            bool condition = title != null && title.getAttribute("type", 1).Equals("text");

            if (condition == false) {
                return false;
            }

            IEnumerator e = request.Document.getElementsByTagName("td").GetEnumerator();
            while (e.MoveNext()) {
                IHTMLElement element = e.Current as IHTMLElement;
                if (element != null && "Twoja nazwa p³atnoœci".Equals(element.innerHTML)) {
                    if (request.PaymentInfo.IsDefinedTransfer == false) {
                        return false;
                    }
                    if (e.MoveNext() == false) {
                        break;
                    }
                    element = e.Current as IHTMLElement;
                    if (element == null) {
                        break;
                    }
                    return element.innerHTML.ToLower().Equals(request.PaymentInfo.DefinedTransferName.ToLower());
                }
            }

            return request.PaymentInfo.IsDefinedTransfer == false;
        }

        /// <summary>
        /// Selects defined transfer (if exists) and goes to the transfer page (if defined transfer 
        /// doesn't exists asks the questions and goes to the one-time transfer - if user agrees).
        /// </summary>
        /// <param name="request">payment request</param>
        protected override void SelectDefinedTransferAndGoToTransferPage(PaymentRequest request) {
            IEnumerator e = request.Document.getElementsByTagName("td").GetEnumerator();
            int count = 0;
            IHTMLElement transferLink = null;
            while (e.MoveNext()) {
                IHTMLElement element = e.Current as IHTMLElement;
                if (element != null && ("tableField1c".Equals(element.className) || "tableField2c".Equals(element.className))) {
                    String name = TrimHtml(element.innerHTML);
                    // skip 4 elements to see if this row contains links to pay
                    for (int i = 0; i < 4; i++) {
                        if (e.MoveNext() == false) {
                            break;
                        }
                    }

                    element = e.Current as IHTMLElement;
                    String html = element.innerHTML;
                    if (element != null && html != null && html.ToLower().StartsWith("<table")
                        && request.PaymentInfo.DefinedTransferName.ToLower().Equals(name.ToLower())) {

                        // get the transfer link
                        if (transferLink == null) {
                            transferLink = GetDefinedTransferLink(element);
                        }
                        count++;
                    }
                }
            }
            GoToDefinedTransferPage(request, count, transferLink);
        }

        /// <summary>
        /// Pastes payment information in the transfer HTML form.
        /// </summary>
        /// <param name="request">payment request</param>
        protected override void PastePaymentInfo(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            PaymentInfo info = request.PaymentInfo;
            Util.SetElementValue(doc.getElementById(ACCOUNT_FIELD), info.BankAccountNo);
            String[] billerName = Util.SplitString(ReplaceSpecialChars(info.BillerName), 35, BENEFICIARY_NAME_FIELD.Length);
            for (int i = 0; i < billerName.Length; i++) {
                Util.SetElementValue(doc.getElementsByName(BENEFICIARY_NAME_FIELD[i]), billerName[i]);
            }
            Util.SetElementValue(doc.getElementsByName(ADDRESS_1_FIELD), ReplaceSpecialChars(info.Street));
            Util.SetElementValue(doc.getElementsByName(ADDRESS_2_FIELD), ReplaceSpecialChars(info.PostalCodeAndCity));

            String[] title = Util.SplitString(ReplaceSpecialChars(info.Title), 35, TITLE_FIELD.Length);
            for (int i = 0; i < title.Length; i++) {
                Util.SetElementValue(doc.getElementsByName(TITLE_FIELD[i]), title[i]);
            }
            Util.SetElementValue(doc.getElementsByName(AMOUNT_1_FIELD), info.AmountToPayDecimal);
            Util.SetElementValue(doc.getElementsByName(AMOUNT_2_FIELD), info.AmountToPayFloating);
            Util.SetElementValue(doc.getElementsByName(YEAR_FIELD), info.DueDateTimeToPaste.Year.ToString());
            Util.SetElementValue(doc.getElementsByName(MONTH_FIELD), info.DueDateTimeToPaste.Month.ToString());
            Util.SetElementValue(doc.getElementsByName(DAY_FIELD), info.DueDateTimeToPaste.Day.ToString());

            info.State = State.IN_PROGRESS;
        }

        /// <summary>
        /// Checks if the current page is the confirmation page (the last one in the payment process).
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>true if current page is the confirmation page (the last one in the payment process)</returns>
        protected override bool IsConfirmationPage(PaymentRequest request) {
            if (request.PaymentInfo.IsDefinedTransfer) {
                return ElementsInnerHtmlContains(request.Document,
                    "p",
                    "Twoja p³atnoœæ zostanie zrealizowana w dniu");
            } else {
                return ElementsInnerHtmlContains(request.Document,
                    "p",
                    "Twój przelew zostanie zrealizowany w dniu");
            }
        }

        protected override Char[] CharReplacement() {
            return CHAR_REPLACEMENT;
        }

        protected override Char[] CharsToReplace() {
            return CHARS_TO_REPLACE;
        }
        #endregion

        #region Go To methods
        /// <summary>
        /// Goes to the defined tansfer page. 
        /// </summary>
        /// <param name="request">payment request</param>
        /// <param name="count">number of matching defined transfers (ones with the same name)</param>
        /// <param name="transferLink">transfer link element to click</param>
        private void GoToDefinedTransferPage(PaymentRequest request, int count, IHTMLElement transferLink) {
            PaymentInfo info = request.PaymentInfo;
            if (count > 0) {
                // more than one defined transfer with the provided name
                if (count > 1) {
                    String msg = String.Format(Resources.InteligoMoreThanOneDefinedTransfer, info.DefinedTransferName);
                    Util.ShowInfoMessage(msg);
                }
                transferLink.click();
            } else {
                // no defined transfer with the provided name
                String question = String.Format(Resources.InteligoNoDefinedTransfer, info.DefinedTransferName);

                if (Util.ShowYesNoQuestion(null, question) == true) {
                    info.DefinedTransferName = "";
                    Application.UserAppDataRegistry.SetValue(Util.CURRENT_PAYMENT, info.ToString());
                    GoToTransferPage(request);
                } else {
                    info.State = State.INVALID;
                }
            }
        }
        #endregion

        #region IsXXX methods
        /// <summary>
        /// Checks if the current page is the confirm page (last but one in the payment process).
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>true if current page is the confirm page (last but one in the payment process)</returns>
        private bool IsConfirmPage(PaymentRequest request) {
            if (request.PaymentInfo.IsDefinedTransfer) {
                return ElementsInnerHtmlEquals(request.Document,
                    "p",
                    "Szczegó³y realizowanej p³atnoœci. Aby zatwierdziæ, kliknij OK.");
            } else {
                return ElementsInnerHtmlEquals(request.Document,
                    "p",
                    "Szczegó³y Twojego przelewu jednorazowego. Aby zatwierdziæ, kliknij OK.");
            }
        }
        #endregion

        /// <summary>
        /// Gets the "execute transfer" link from given HTML table cell element.
        /// It simply iterates over given HTML element and searches all "a" tags and returns
        /// the one that contains "Zap³aæ" text - this is the one that executes the transfer.
        /// </summary>
        /// <param name="element">HTML table cell element</param>
        /// <returns>HTML element that executes the defined transfer to click if exists or null otherwise</returns>
        private IHTMLElement GetDefinedTransferLink(IHTMLElement element) {
            IEnumerator a = (element.all as IHTMLElementCollection).GetEnumerator();
            while (a.MoveNext()) {
                if (a.Current is IHTMLAnchorElement) {
                    IHTMLElement anchor = a.Current as IHTMLElement;
                    if ("Zap³aæ".Equals(anchor.innerHTML)) {
                        return anchor;
                    }
                }
            }
            return null;
        }
        #region HTML checks methods
        /// <summary>
        /// Checks if any element's inner HTML with given tag name in the provided HTML document 
        /// equals given text.
        /// </summary>
        /// <param name="doc">HTML document</param>
        /// <param name="tagName">HTML tag name</param>
        /// <param name="text">text to be found</param>
        /// <returns>true if any element's inner HTML with given tag name in the provided HTML document 
        /// equals given text, false otherwise</returns>
        private bool ElementsInnerHtmlEquals(HTMLDocument doc, String tagName, String text) {
            IEnumerator e = doc.getElementsByTagName(tagName).GetEnumerator();
            while (e.MoveNext()) {
                if (text.Equals((e.Current as IHTMLElement).innerHTML)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if any element with given tag name in the provided HTML document 
        /// contains given text in its inner HTML.
        /// </summary>
        /// <param name="doc">HTML document</param>
        /// <param name="tagName">HTML tag name</param>
        /// <param name="text">text to be found</param>
        /// <returns>true if any element with given tag name in the provided HTML document 
        /// contains given test in its inner HTML, false otherwise</returns>
        private bool ElementsInnerHtmlContains(HTMLDocument doc, String tagName, String text) {
            IEnumerator e = doc.getElementsByTagName(tagName).GetEnumerator();
            while (e.MoveNext()) {
                IHTMLElement element = e.Current as IHTMLElement;
                if (element == null || element.innerHTML == null) {
                    continue;
                }
                if (element.innerHTML.Contains(text)) {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
