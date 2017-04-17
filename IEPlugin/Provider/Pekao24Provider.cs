using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using mshtml;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class Pekao24Provider : AbstractProvider {

        #region Contants
        internal static readonly String DEFINED_TRANSFERS_LIST = "frmMTPredList";
        internal static readonly String ANY_TRANSFER_FORM = "frmMTNotPredAcc";
        internal static readonly String DEFINED_TRANSFER_FORM = "frmMTTCOwnAcc";
        internal static readonly String AMOUNT_A_FIELD = "parAmountA";
        internal static readonly String AMOUNT_B_FIELD = "parAmountB";
        internal static readonly String DESTINATION_ACCOUNT_FIELD = "parDestinationAccount";
        internal static readonly String BENEFICIARY_NAME_FIELD = "parBeneficiaryName";
        internal static readonly String STREET_FIELD = "parBeneficiaryStreet";
        internal static readonly String CITY_FIELD = "parBeneficiaryCity";
        internal static readonly String POSTAL_CODE_FIELD = "parBeneficiaryCode";
        internal static readonly String DESCRIPTION_FIELD = "parDescription";

        private static readonly Char[] CHARS_TO_REPLACE = "`$&\"'><".ToCharArray();
        private static readonly Char[] CHAR_REPLACEMENT = "       ".ToCharArray();
        #endregion

        #region Overriden methods
        protected override PaymentInfo ProcessInProgress(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            if (ReadyToPaste(request)) {
                return info;

            } else if (IsConfirmPage(request)) {
                IEnumerator e = (request.Document.getElementsByTagName("td") as IHTMLElementCollection).GetEnumerator();
                bool firstAccount = true;
                while (e.MoveNext()) {
                    String html = TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower();

                    if ("numer rachunku".Equals(html)) {
                        if (firstAccount == true) {
                            firstAccount = false;
                            continue;
                        }
                        if (e.MoveNext()) {
                            info.BankAccountNo = (e.Current as IHTMLElement).innerHTML.Trim();
                        }
                    } else if ("kwota przelewu".Equals(html)) {
                        if (e.MoveNext()) {
                            String tmp = TrimHtml((e.Current as IHTMLElement).innerHTML);
                            info.AmountPaid = tmp.Substring(0, tmp.IndexOf(" ")).Replace(".", "");
                            info.State = State.IN_PROGRESS_CONFIRM;
                            return info;
                        }
                    }
                }
            }

            info.State = State.INVALID;
            return info;
        }

        protected override bool IsConfirmationPage(PaymentRequest request) {
            return request.Document.body.innerHTML.ToLower().Contains("<message>zlecenie przyjêto do realizacji.</message>");
        }

        protected override bool IsUserLogged(PaymentRequest request) {
            return request.Document.getElementById("log-off") != null;
        }

        protected override bool ReadyToPaste(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            if (request.PaymentInfo.IsDefinedTransfer == false && doc.getElementsByName(ANY_TRANSFER_FORM).length > 0) {
                IHTMLElement field = doc.getElementsByName(DESTINATION_ACCOUNT_FIELD).item(DESTINATION_ACCOUNT_FIELD, 0) as IHTMLElement;
                return (field != null && "text".Equals(field.getAttribute("type", 1)));

            } else if (request.PaymentInfo.IsDefinedTransfer && doc.getElementsByName(DEFINED_TRANSFER_FORM).length > 0) {
                if (doc.getElementsByName(DESTINATION_ACCOUNT_FIELD).length == 0) {
                    IHTMLElement field = doc.getElementsByName(DESCRIPTION_FIELD).item(DESCRIPTION_FIELD, 0) as IHTMLElement;
                    if (field != null && "textarea".Equals(field.getAttribute("type", 1))) {
                        return IsDefinedTransferPage(request);
                    }
                }
            }
            return false;
        }

        protected override void PastePaymentInfo(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            HTMLDocument doc = request.Document;

            Util.SetElementValue(doc.getElementsByName(AMOUNT_A_FIELD), info.AmountToPayDecimal);
            Util.SetElementValue(doc.getElementsByName(AMOUNT_B_FIELD), info.AmountToPayFloating);
            Util.SetElementValue(doc.getElementsByName(DESCRIPTION_FIELD), ReplaceSpecialChars(info.Title));
            if (info.IsDefinedTransfer == false) {
                Util.SetElementValue(doc.getElementsByName(DESTINATION_ACCOUNT_FIELD), info.BankAccountNo);
                Util.SetElementValue(doc.getElementsByName(BENEFICIARY_NAME_FIELD), ReplaceSpecialChars(info.BillerName));
                Util.SetElementValue(doc.getElementsByName(STREET_FIELD), ReplaceSpecialChars(info.Street));
                Util.SetElementValue(doc.getElementsByName(CITY_FIELD), ReplaceSpecialChars(info.City));
                Util.SetElementValue(doc.getElementsByName(POSTAL_CODE_FIELD), info.PostalCode);
            }
            info.State = State.IN_PROGRESS;
        }

        protected override void GoToTransferPage(PaymentRequest request) {
            String script = "";
            HTMLDocument doc = request.Document;
            if (request.PaymentInfo.IsDefinedTransfer == false) {
                if (doc.url.Contains(".jsp")) {
                    script = Settings.Default.Pekao24NotPredefinedJspUrl;
                } else {
                    script = Settings.Default.Pekao24NotPredefinedHtmUrl;
                }
            } else {
                if (doc.url.Contains(".jsp")) {
                    script = Settings.Default.Pekao24PredefinedJspUrl;
                } else {
                    script = Settings.Default.Pekao24PredefinedHtmUrl;
                }
            }

            if (Util.RunJavaScript(doc, script) == false) {
                request.PaymentInfo.State = State.INVALID;
            }
        }

        protected override void SelectDefinedTransferAndGoToTransferPage(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            HTMLDocument doc = request.Document;
            IEnumerator e = doc.getElementsByTagName("td").GetEnumerator();
            while (e.MoveNext()) {
                String html = (e.Current as IHTMLElement).innerHTML.ToLower();
                if (html.Contains("name=parradio") && html.Contains("<input")) {
                    IEnumerator input = ((e.Current as IHTMLElement).all as IHTMLElementCollection).GetEnumerator();
                    if (input.MoveNext() && e.MoveNext()) {
                        IEnumerator e1 = ((e.Current as IHTMLElement).all as IHTMLElementCollection).GetEnumerator();
                        if (e1.MoveNext() && e1.MoveNext()) {
                            if (info.DefinedTransferName.ToLower().Equals((e1.Current as IHTMLElement).innerHTML.Trim().ToLower())) {
                                (input.Current as IHTMLElement).click();
                                if (Util.RunJavaScript(doc, "Execute()") == false) {
                                    info.State = State.INVALID;
                                }
                                return;
                            }
                        }
                    }
                }
            }

            String question = String.Format(Resources.Pekao24NoDefinedTransfer, info.DefinedTransferName);
            if (Util.ShowYesNoQuestion(null, question) == true) {
                info.DefinedTransferName = "";
                GoToTransferPage(request);
            } else {
                info.State = State.INVALID;
            }
        }

        protected override bool IsDefinedTransfersPage(HTMLDocument doc) {
            IHTMLElementCollection forms = doc.getElementsByName(DEFINED_TRANSFERS_LIST);
            return forms.length == 1;
        }

        protected override Char[] CharReplacement() {
            return CHAR_REPLACEMENT;
        }

        protected override Char[] CharsToReplace() {
            return CHARS_TO_REPLACE;
        }
        #endregion

        private bool IsDefinedTransferPage(PaymentRequest request) {
            IEnumerator e = request.Document.getElementsByTagName("tr").GetEnumerator();
            while (e.MoveNext()) {
                if (TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower().Contains("nazwa przelewu") && e.MoveNext() && e.MoveNext()) {
                    IEnumerator tdEn = ((e.Current as IHTMLElement).all as IHTMLElementCollection).GetEnumerator();
                    if (tdEn.MoveNext() && tdEn.MoveNext()) {
                        return request.PaymentInfo.DefinedTransferName.ToLower().Equals((tdEn.Current as IHTMLElement).innerText.ToLower().Trim());
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if current page is the confirm page containing details of the payment to confirm.
        /// This method also checks the data that was provided by the user for this payment i.e. bank account no. and amount paid.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>true if current page is the confirm page or false otherwise</returns>
        private bool IsConfirmPage(PaymentRequest request) {
            IEnumerator e = request.Document.getElementsByName("TextCenter").GetEnumerator();
            while (e.MoveNext()) {
                if ("wciœnij przycisk zatwierdŸ, aby wykonaæ przelew.".Equals(TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower())) {
                    return true;
                }
            }
            return false;
        }
    }
}
