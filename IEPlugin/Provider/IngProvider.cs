using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using mshtml;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class IngProvider : AbstractProvider {

        #region Contants
        internal static readonly String CREDIT_ACCOUNT_FIELD = "creditAccount";
        internal static readonly String RECIPIENT_FIELD = "recipient";
        internal static readonly String AMOUNT_FIELD = "amount";
        internal static readonly String TITLE_FIELD = "titled";
        internal static readonly String DATE_TRANSFER_FIELD = "dateTransfer";
        internal static readonly String CONFIRM_FORM = "confirm";

        private static readonly Char[] CHARS_TO_REPLACE = "|<>\"'".ToCharArray();
        private static readonly Char[] CHAR_REPLACEMENT = "     ".ToCharArray();
        #endregion

        #region Overriden methods
        protected override PaymentInfo ProcessInProgress(PaymentRequest request) {
            throw new Exception("This state [InProgress] is not supported.");
        }

        protected override PaymentInfo ProcessInProgressConfirm(PaymentRequest request) {
            if (IsConfirmationPage(request)) {
                CheckConfirmForm(request);
            } else if (ReadyToPaste(request) == false) {
                request.PaymentInfo.State = State.INVALID;
            }
            return request.PaymentInfo;
        }

        protected override bool IsUserLogged(PaymentRequest request) {
            if (request.Url.Contains(".html") || request.Url == String.Empty) {
                IHTMLElement toplinks = request.Document.getElementById("toplinks");
                if (toplinks != null) {
                    IEnumerator e = (toplinks.all as IHTMLElementCollection).GetEnumerator();
                    while (e.MoveNext() != false) {
                        IHTMLElement element = e.Current as IHTMLElement;
                        if (element != null && "logout".Equals(element.className)
                            && "Wyjœcie".Equals(element.innerHTML)) {

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected override void PastePaymentInfo(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            PaymentInfo info = request.PaymentInfo;

            Util.SetElementValue(doc.getElementById(CREDIT_ACCOUNT_FIELD), info.BankAccountNo);
            Util.SetElementValue(doc.getElementById(RECIPIENT_FIELD),
                String.Format("{0}\n{1}\n{2}", 
                    ReplaceSpecialChars(info.BillerName), 
                    ReplaceSpecialChars(info.Street), 
                    ReplaceSpecialChars(info.PostalCodeAndCity)).Trim());

            Util.SetElementValue(doc.getElementById(AMOUNT_FIELD), info.AmountToPay.Replace(".", ","));
            Util.SetElementValue(doc.getElementById(TITLE_FIELD), ReplaceSpecialChars(info.Title));
            Util.SetElementValue(doc.getElementById(DATE_TRANSFER_FIELD),
                info.DueDateTimeToPaste.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            // there is no confirm page - state already is IN_PROGRESS_CONFIRM
            request.PaymentInfo.State = State.IN_PROGRESS_CONFIRM;
        }

        protected override bool ReadyToPaste(PaymentRequest request) {
            return request.Document.getElementById("newtransfer") != null;
        }

        protected override bool IsConfirmationPage(PaymentRequest request) {
            IHTMLFormElement form = request.Document.getElementById("confirm") as IHTMLFormElement;
            return form != null;
        }

        protected override void GoToTransferPage(PaymentRequest request) {
            request.Document.url = "https://ssl.bsk.com.pl/bskonl/transaction/transfer/pln/newtransfer.html?formRefresh=false";
        }

        protected override Char[] CharReplacement() {
            return CHAR_REPLACEMENT;
        }

        protected override Char[] CharsToReplace() {
            return CHARS_TO_REPLACE;
        }
        #endregion

        /// <summary>
        /// Checks the confirm HTML form and copies the data that were entered and committed by the user.
        /// </summary>
        /// <param name="request">payment request</param>
        private void CheckConfirmForm(PaymentRequest request) {
            request.PaymentInfo.State = State.INVALID;

            HTMLDocument doc = request.Document;
            String html = doc.getElementById("confirm").innerHTML;
            int idx = html.IndexOf("Przelew z rachunku");
            if (idx == -1) {
                return;
            }

            idx = html.IndexOf("na rachunek ", idx);
            if (idx == -1) {
                return;
            }
            int end = html.IndexOf(" na kwotê ", idx);
            if (end == -1) {
                return;
            }

            String current = request.PaymentInfo.BankAccountNo;
            String tmp = html.Substring(idx + 12, end - idx - 12).Trim();
            if (tmp.EndsWith(",")) {
                tmp = tmp.Substring(0, tmp.Length - 1);
            }
            request.PaymentInfo.BankAccountNo = tmp;
            idx = html.IndexOf("na kwotê ", idx);
            if (idx == -1) {
                return;
            }
            String amountPaid = html.Substring(idx + 9, html.IndexOf(" zosta³", idx + 1) - idx - 9).Trim();
            // check if currency is added
            if (amountPaid.IndexOf(" ") != -1) {
                double result;
                if (double.TryParse(amountPaid.Replace(",", ".").Replace(" ", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out result) == false) {
                    amountPaid = amountPaid.Substring(0, amountPaid.LastIndexOf(" ")).Replace(" ", "");
                }
                amountPaid = amountPaid.Replace(" ", "");
            }

            if (current.Equals(request.PaymentInfo.BankAccountNo)) {
                request.PaymentInfo.AmountPaid = amountPaid;
                request.PaymentInfo.State = State.PAID;
            }
        }
    }
}
