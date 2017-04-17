using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using mshtml;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class BzWbkProvider : AbstractProvider {

        #region Constants
        internal static readonly String ACCOUNT_FIELD = "recipientInputPanel:accountNumberC:accountNumberBorder:_body:accountNumber";
        internal static readonly String NAME_FIELD = "recipientInputPanel:nameBorder:_body:name";
        internal static readonly String STREET_FIELD = "recipientInputPanel:streetBorder:_body:street";
        internal static readonly String CITY_FIELD = "recipientInputPanel:cityBorder:_body:city";
        internal static readonly String POSTAL_CODE_FIELD = "recipientInputPanel:zipCodeBorder:_body:zipCode";
        internal static readonly String AMOUNT_FIELD = "transferInputPanel:transferAmountBorder:_body:creditedAmount";
        internal static readonly String TITLE_FIELD = "transferInputPanel:titleBorder:_body:title";
        internal static readonly String DATE_FIELD = "transferInputPanel:datePanel:dateBorder:_body:date";
        internal static readonly String DEFINED_TRANSFER_FIELD = "recipientInputPanel:shortNameBeneficiaryContainer:_body:shortNameBeneficiary";

        private static readonly Char[] CHARS_TO_REPLACE = "~`!@#$%^&*()_+{}|:\"<>?=[]\\'".ToCharArray();
        private static readonly Char[] CHAR_REPLACEMENT = "                           ".ToCharArray();
        #endregion

        #region Overriden methods
        protected override PaymentInfo ProcessInProgress(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            if (ReadyToPaste(request)) {
                return info;
            }

            if (IsConfirmPage(request)) {
                IEnumerator e = request.Document.getElementsByTagName("span").GetEnumerator();
                while (e.MoveNext()) {
                    String html = TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower();
                    if ("numer rachunku:".Equals(html) && e.MoveNext()) {
                        info.BankAccountNo = TrimHtml((e.Current as IHTMLElement).innerHTML);
                    } else if ("kwota:".Equals(html) && e.MoveNext()) {
                        String amount = TrimHtml((e.Current as IHTMLElement).innerHTML);
                        if (amount.Contains(" ")) {
                            amount = amount.Substring(0, amount.LastIndexOf(" ")).Replace(" ", "");
                        }
                        info.AmountPaid = amount;
                        if (info.IsDefinedTransfer == false) {
                            IHTMLElement smsCodeField = request.Document.getElementById("authorization_response");
                            if (smsCodeField != null && "text".Equals(smsCodeField.getAttribute("type", 1))) {
                                info.State = State.IN_PROGRESS_CONFIRM;
                            } else {
                                info.State = State.IN_PROGRESS;
                            }
                        } else {
                            info.State = State.IN_PROGRESS_CONFIRM;
                        }
                        return info;
                    }
                }
            }

            info.State = State.INVALID;
            return info;
        }

        protected override bool IsUserLogged(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            if (doc.url.Contains("/centrum24-web")) {
                IEnumerator e = doc.getElementsByTagName("a").GetEnumerator();
                while (e.MoveNext()) {
                    if ("wyloguj".Equals(TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower())) {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void GoToTransferPage(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            IEnumerator e = (doc.getElementById("menu_transfers").all as IHTMLElementCollection).GetEnumerator();
            while (e.MoveNext()) {
                if ("przelewy".Equals(TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower())) {
                    (e.Current as IHTMLElement).click();
                    return;
                }
            }
            request.PaymentInfo.State = State.INVALID;
        }

        protected override bool ReadyToPaste(PaymentRequest request) {
            HTMLDocument doc = request.Document;

            bool readyToPaste = false;
            IEnumerator e = doc.getElementsByTagName("h2").GetEnumerator();
            while (e.MoveNext()) {
                if ("przelew krajowy na rachunek obcy".Equals(TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower())) {
                    readyToPaste = true;
                }
            }

            if (readyToPaste == false) {
                return false;
            }

            IHTMLElement menuTransfers = doc.getElementById("menu_transfers");
            if ("selected".Equals(menuTransfers.className)) {
                IEnumerator e1 = doc.getElementsByTagName("li").GetEnumerator();
                while (e1.MoveNext()) {
                    IHTMLElement element = e1.Current as IHTMLElement;
                    if (TrimHtml(element.innerHTML).ToLower().Contains("dane") && "first-step".Equals(element.className)) {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void PastePaymentInfo(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            PaymentInfo info = request.PaymentInfo;

            if (info.IsDefinedTransfer == false) {
                if (IsAnyTransferSelected(doc) == false) {
                    GoToTransferPage(request);
                    return;
                }
                Util.SetElementValue(doc.getElementsByName(ACCOUNT_FIELD), info.BankAccountNo);
                if (info.BillerName.Length > 32) {
                    Util.SetElementValue(doc.getElementsByName(NAME_FIELD), ReplaceSpecialChars(info.BillerName).Substring(0, 32));
                } else {
                    Util.SetElementValue(doc.getElementsByName(NAME_FIELD), ReplaceSpecialChars(info.BillerName));
                }
                Util.SetElementValue(doc.getElementsByName(STREET_FIELD), ReplaceSpecialChars(info.Street));
                Util.SetElementValue(doc.getElementsByName(CITY_FIELD), ReplaceSpecialChars(info.City));
                Util.SetElementValue(doc.getElementsByName(POSTAL_CODE_FIELD), info.PostalCode);
            } else {
                Object tmp = null;
                int result = Util.SetElementValue(doc.getElementsByName(DEFINED_TRANSFER_FIELD), info.DefinedTransferName, doc.CreateEventObject(ref tmp));
                if (result == -1) {
                    String question = String.Format(Resources.BzWbkNoDefinedTransfer, info.DefinedTransferName);
                    if (Util.ShowYesNoQuestion(null, question) == true) {
                        info.DefinedTransferName = "";
                        PastePaymentInfo(request);
                    } else {
                        info.State = State.INVALID;
                    }
                    return;
                } else if (result > 1) {
                    Util.ShowInfoMessage(String.Format(Resources.BzWbkMoreThanOneDefinedTransfer, info.DefinedTransferName));
                }
            }
            // we have to wait 300ms and paste data asynchronously because some
            // ugly JavaScript clears all the fields on the bank's page
            new Thread(new ThreadStart(new PasteHelper(request, this).Run)).Start();

            info.State = State.IN_PROGRESS;
        }

        protected override bool IsConfirmationPage(PaymentRequest request) {
            IEnumerator e = request.Document.getElementsByTagName("li").GetEnumerator();
            while (e.MoveNext()) {
                IHTMLElement element = e.Current as IHTMLElement;
                if ("selected-last-step".Equals(element.className) && TrimHtml(element.innerHTML).ToLower().Contains("koniec")) {
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
            IEnumerator e = request.Document.getElementsByTagName("li").GetEnumerator();
            while (e.MoveNext()) {
                IHTMLElement element = e.Current as IHTMLElement;
                if ("selected-step".Equals(element.className) && TrimHtml(element.innerHTML).ToLower().Contains("potwierdzenie")) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if any defined transfer is selected on the transfer page.
        /// </summary>
        /// <param name="doc">HTML document</param>
        /// <returns>true if any defined transfer is selected or false otherwise</returns>
        private bool IsAnyTransferSelected(HTMLDocument doc) {
            IHTMLSelectElement select = (doc.getElementsByName(DEFINED_TRANSFER_FIELD) as IHTMLElementCollection).item(0, null) as IHTMLSelectElement;
            return (select.selectedIndex == 0 && "wybierz".Equals(TrimHtml((select.item(0, null) as IHTMLOptionElement).text).ToLower()));
        }

        /// <summary>
        /// Asynchronous paste helper.
        /// </summary>
        protected class PasteHelper {
            private PaymentInfo info;
            private HTMLDocument doc;
            private BzWbkProvider parent;

            public PasteHelper(PaymentRequest request, BzWbkProvider parent) {
                this.doc = request.Document;
                this.info = request.PaymentInfo;
                this.parent = parent;
            }

            public void Run() {
                Thread.Sleep(300);
                Util.SetElementValue(doc.getElementsByName(AMOUNT_FIELD), info.AmountToPay.Replace(".", ","));
                Util.SetElementValue(doc.getElementsByName(TITLE_FIELD), parent.ReplaceSpecialChars(info.Title));
                Util.SetElementValue(doc.getElementsByName(DATE_FIELD), info.DueDateTimeToPaste.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
        }
    }
}
