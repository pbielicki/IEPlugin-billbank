using System;
using System.Collections;
using System.Globalization;
using System.Windows.Forms;
using mshtml;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class PkoProvider : AbstractProvider {

        #region Constants
        internal static readonly String ACCOUNT_FIELD = "ben_acc";
        internal static readonly String[] BENEFICIARY_NAME_FIELD = new String[] { "ben_name.0", "ben_name.1" };
        internal static readonly String ADDRESS_1_FIELD = "ben_name.2";
        internal static readonly String ADDRESS_2_FIELD = "ben_name.3";
        internal static readonly String[] TITLE_FIELD = new String[] {"pay_title.0", "pay_title.1", "pay_title.2", "pay_title.3"};
        internal static readonly String AMOUNT_1_FIELD = "amount.part1";
        internal static readonly String AMOUNT_2_FIELD = "amount.part2";
        internal static readonly String DATE_FIELD = "pay_date";

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
                    String html = TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower();
                    if ("numer rachunku odbiorcy".Equals(html) && e.MoveNext() == true) {
                        html = TrimHtml((e.Current as IHTMLElement).innerHTML);
                        if (html.IndexOf("<") != -1) {
                            html = html.Substring(0, html.IndexOf("<"));
                        }
                        info.BankAccountNo = html;
                    } else if ("kwota".Equals(html) && e.MoveNext()) {
                        html = TrimHtml((e.Current as IHTMLElement).innerHTML);
                        if (html.IndexOf(" ") != -1) {
                            html = html.Substring(0, html.LastIndexOf(" ")).Replace(" ", "");
                        }
                        info.AmountPaid = html;
                        info.State = State.IN_PROGRESS_CONFIRM;
                        return info;
                    }
                }
            }

            info.State = State.INVALID;
            return info;
        }

        protected override bool IsUserLogged(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            return (doc.getElementById("inteligomenu") != null && doc.getElementById("leftmenucontener") != null);
        }

        protected override void GoToTransferPage(PaymentRequest request) {
            bool result = false;
            if (request.PaymentInfo.IsDefinedTransfer == false) {
                result = Util.RunJavaScript(request.Document, "clickMenu('pay_transfer_normal')");
            } else {
                result = Util.RunJavaScript(request.Document, "clickMenu('pay_payment_list')");
            }

            if (result == false) {
                request.PaymentInfo.State = State.INVALID;
            }
        }

        protected override bool ReadyToPaste(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            IHTMLElement docHeader = doc.getElementById("docheader");
            if (docHeader != null) {
                IHTMLElement titleField = doc.getElementById(TITLE_FIELD[0]);
                String html = TrimHtml(docHeader.innerHTML).ToLower();
                if (titleField != null && request.PaymentInfo.IsDefinedTransfer == false) {
                    return ("text".Equals(titleField.getAttribute("type", 1)) && html.Contains("przelew jednorazowy"));
                } else if (titleField != null) {
                    return ("text".Equals(titleField.getAttribute("type", 1)) && html.Contains("realizacja p³atnoœci"));
                }
            }
            return false;
        }

        protected override bool IsDefinedTransfersPage(HTMLDocument doc) {
            IEnumerator e = doc.getElementsByTagName("h1").GetEnumerator();
            while (e.MoveNext()) {
                if ("lista p³atnoœci".Equals(TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower())) {
                    return true;
                }
            }
            return false;
        }

        protected override void SelectDefinedTransferAndGoToTransferPage(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            IEnumerator e = request.Document.getElementsByTagName("tr").GetEnumerator();
            IHTMLElement transferLink = null;
            int count = 0;
            bool transferRowFound = false;

            while (e.MoveNext()) {
                String html = TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower();
                if (html.Contains("nazwa p³atnoœci") && html.Contains("dane odbiorcy")
                    && html.Contains("numer rachunku") && html.Contains("tytu³")) {

                    if (e.MoveNext() == true) {
                        transferRowFound = true;
                        break;
                    }
                }
            }

            if (transferRowFound == true) {
                while (e.MoveNext()) {
                    IEnumerator e1 = ((e.Current as IHTMLElement).all as IHTMLElementCollection).GetEnumerator();
                    IHTMLElement currentLink = GetTransferLink(e1, info.DefinedTransferName.ToLower(), transferLink);
                    if (currentLink != null) {
                        count++;
                        transferLink = currentLink;
                    }

                }
            }
            GoToDefinedTransferPage(request, count, transferLink);
        }

        protected override void PastePaymentInfo(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            HTMLDocument doc = request.Document;

            if (info.IsDefinedTransfer == false) {
                Util.SetElementValue(doc.getElementById(ACCOUNT_FIELD), info.BankAccountNo);
                String[] billerName = Util.SplitString(ReplaceSpecialChars(info.BillerName), 35, BENEFICIARY_NAME_FIELD.Length);
                for (int i = 0; i < billerName.Length; i++) {
                    Util.SetElementValue(doc.getElementById(BENEFICIARY_NAME_FIELD[i]), billerName[i]);
                }
                Util.SetElementValue(doc.getElementById(ADDRESS_1_FIELD), ReplaceSpecialChars(info.Street));
                Util.SetElementValue(doc.getElementById(ADDRESS_2_FIELD), ReplaceSpecialChars(info.PostalCodeAndCity));
            }

            String[] title = Util.SplitString(ReplaceSpecialChars(info.Title), 35, TITLE_FIELD.Length);
            for (int i = 0; i < title.Length; i++) {
                Util.SetElementValue(doc.getElementById(TITLE_FIELD[i]), title[i]);
            }

            Util.SetElementValue(doc.getElementById(AMOUNT_1_FIELD), info.AmountToPayDecimal);
            Util.SetElementValue(doc.getElementById(AMOUNT_2_FIELD), info.AmountToPayFloating);
            Util.SetElementValue(doc.getElementById(DATE_FIELD), info.DueDateTimeToPaste.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            info.State = State.IN_PROGRESS;
        }

        protected override bool IsConfirmationPage(PaymentRequest request) {
            IEnumerator e = request.Document.getElementsByTagName("h3").GetEnumerator();
            while (e.MoveNext()) {
                if (TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower().Contains("transakcja zosta³a przyjêta do realizacji.")) {
                    return true;
                }
            }
            return false;
        }

        protected override Char[] CharReplacement() {
            return CHAR_REPLACEMENT;
        }

        protected override Char[] CharsToReplace() {
            return CHARS_TO_REPLACE;
        }
        #endregion

        /// <summary>
        /// Checks if current page is the confirm page containing details of the payment to confirm.
        /// This method also checks the data that was provided by the user for this payment i.e. bank account no. and amount paid.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>true if current page is the confirm page or false otherwise</returns>
        private bool IsConfirmPage(PaymentRequest request) {
            IHTMLElement docHeader = request.Document.getElementById("docheader");
            if (docHeader != null) {
                if (request.PaymentInfo.IsDefinedTransfer == false) {
                    return TrimHtml(docHeader.innerHTML).ToLower().Contains("potwierdzenie zlecenia przelewu jednorazowego");
                } else {
                    return TrimHtml(docHeader.innerHTML).ToLower().Contains("potwierdzenie realizacji p³atnoœci");
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the "execute transfer" link from given HTML elements enumerator.
        /// It simply iterates over given iterator and searches elements and returns
        /// the one that contains "Zap³aæ" text - this is the one that executes the transfer.
        /// </summary>
        /// <param name="e">Enumerator of HTML elements to search</param>
        /// <param name="transferName">Defined transfer name to find</param>
        /// <param name="currentLink">Current defined transfer link (if any) - if not null the new link will not be returned</param>
        /// <returns>HTML element to click, that executes the defined transfer page, if exists, or null otherwise</returns>
        private IHTMLElement GetTransferLink(IEnumerator e, String transferName, IHTMLElement currentLink) {
            while (e.MoveNext()) {
                if (transferName.Equals(TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower()) == false) {
                    break;
                }
                // if link is already set don't check the rest of the HTML page
                if (currentLink != null) {
                    return currentLink;
                }

                // transfer link is four table cells ahead
                int i = 0;
                while (e.MoveNext() && i < 4) {
                    if (e.Current is IHTMLTableCell) {
                        i++;
                    }
                }

                IEnumerator en = ((e.Current as IHTMLElement).all as IHTMLElementCollection).GetEnumerator();
                while (en.MoveNext()) {
                    if (en.Current is IHTMLAnchorElement && "zap³aæ".Equals(TrimHtml((en.Current as IHTMLElement).innerHTML).ToLower())) {
                        return en.Current as IHTMLElement;
                    }
                }
            }
            return null;
        }

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
                    String msg = String.Format(Resources.PkoMoreThanOneDefinedTransfer, info.DefinedTransferName);
                    Util.ShowInfoMessage(msg);
                }
                transferLink.click();
            } else {
                // no defined transfer with the provided name
                String question = String.Format(Resources.PkoNoDefinedTransfer, info.DefinedTransferName);

                if (Util.ShowYesNoQuestion(null, question) == true) {
                    info.DefinedTransferName = "";
                    Application.UserAppDataRegistry.SetValue(Util.CURRENT_PAYMENT, info.ToString());
                    GoToTransferPage(request);
                } else {
                    info.State = State.INVALID;
                }
            }
        }
    }
}
