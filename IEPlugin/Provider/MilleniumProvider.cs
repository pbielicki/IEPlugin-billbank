using System;
using System.Collections;
using System.Windows.Forms;
using mshtml;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class MilleniumProvider : AbstractProvider {

        #region Constants
        internal static readonly String TRUSTED_POSTFIX = " - zaufany";
        internal static readonly String NORMAL_POSTFIX = " - zwyk³y";
        internal static readonly String TRANSFER_PAGE_DEMO_URL = "/demo/TRDomestic.qz";
        internal static readonly String TRANSFER_PAGE_URL = "/osobiste/TRDomestic.qz";
        internal static readonly String TRANSFER_PAGE_DEMO_ENT_URL = "/demofirmy/TRDomestic.qz";
        internal static readonly String TRANSFER_PAGE_ENT_URL = "/firmy/TRDomestic.qz";

        internal static readonly String ACCOUNT_FIELD = "AccountNumberFullDestinationInput";
        internal static readonly String NAME_FIELD = "FullName";
        internal static readonly String ADDRESS_FIELD = "Address";
        internal static readonly String POSTAL_CODE_FIELD = "PostalCode";
        internal static readonly String DESCRIPTION_FIELD = "Description";
        internal static readonly String AMOUNT_FIELD = "Amount";
        internal static readonly String AMOUNT_INT_FIELD = "Amount_int";
        internal static readonly String AMOUNT_DEC_FIELD = "Amount_dec";
        internal static readonly String YEAR_FIELD = "Date_year";
        internal static readonly String MONTH_FIELD = "Date_month";
        internal static readonly String DAY_FIELD = "Date_day";

        internal static readonly String BENEFICIARY_LIST = "BeneficiaryList";
        internal static readonly String DOMESTIC_TRANSFER_TEXT = "przelew krajowy";

        internal static readonly Char[] CHARS_TO_REPLACE = "`~!@#$%^&*_={}[];\"'<>|\\".ToCharArray();
        internal static readonly Char[] CHAR_REPLACEMENT = "            ()()   ()  ".ToCharArray();
        #endregion

        #region Overriden methods
        protected override PaymentInfo ProcessInProgress(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            if (ReadyToPaste(request)) {
                return info;
            }

            HTMLDocument doc = request.Document;
            if (IsConfirmPage(doc)) {
                IHTMLElement div = doc.getElementById("f_DestinationAccount");
                if (div != null) {
                    IEnumerator e = (div.all as IHTMLElementCollection).GetEnumerator();
                    if (e.MoveNext() && e.MoveNext()) {
                        info.BankAccountNo = TrimHtml((e.Current as IHTMLElement).innerText);
                    }
                }
                div = doc.getElementById("f_Amount");
                if (div != null) {
                    IEnumerator e = (div.all as IHTMLElementCollection).GetEnumerator();
                    if (e.MoveNext() && e.MoveNext()) {
                        String amount = TrimHtml((e.Current as IHTMLElement).innerText);
                        if (amount.Contains(" ")) {
                            amount = amount.Substring(0, amount.LastIndexOf(" ")).Replace(" ", "");
                        }
                        info.AmountPaid = amount;
                        info.State = State.IN_PROGRESS_CONFIRM;
                        return info;
                    }
                }
            }

            info.State = State.INVALID;
            return info;
        }

        protected override PaymentInfo ProcessInProgressConfirm(PaymentRequest request) {
            if (IsConfirmPage(request.Document) || IsCodeConfirmPage(request.Document)) {
                request.PaymentInfo.State = State.IN_PROGRESS_CONFIRM;
            } else if (IsConfirmationPage(request)) {
                request.PaymentInfo.State = State.PAID;
            } else {
                request.PaymentInfo.State = State.INVALID;
            }
            return request.PaymentInfo;
        }

        protected override bool IsUserLogged(PaymentRequest request) {
            if (request.Url.EndsWith(".qz") || request.Url == String.Empty) {
                IEnumerator e = request.Document.getElementsByTagName("a").GetEnumerator();
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
            IHTMLElement select = doc.getElementById(BENEFICIARY_LIST);
            if (select != null && request.Url.EndsWith("TRDomestic.qz")) {
                Util.SetElementValue(select, "Wybierz", true);
                return;
            }

            if (request.Url.Contains("/osobiste")) {
                request.Document.location.pathname = TRANSFER_PAGE_URL;
            } else if (request.Url.Contains("/firmy")) {
                request.Document.location.pathname = TRANSFER_PAGE_ENT_URL;
            } else if (request.Url.Contains("/demofirmy")) {
                request.Document.location.pathname = TRANSFER_PAGE_DEMO_ENT_URL;
            } else {
                request.Document.location.pathname = TRANSFER_PAGE_DEMO_URL;
            }
        }

        protected override bool ReadyToPaste(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            IEnumerator e = doc.getElementsByTagName("div").GetEnumerator();
            while (e.MoveNext()) {
                if (TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower().StartsWith(DOMESTIC_TRANSFER_TEXT)) {
                    IHTMLSelectElement beneficiaryList = doc.getElementById(BENEFICIARY_LIST) as IHTMLSelectElement;
                    if (beneficiaryList == null || doc.getElementsByName(DAY_FIELD).length == 0) {
                        return false;
                    } else if (request.PaymentInfo.IsDefinedTransfer == false) {
                        return beneficiaryList.selectedIndex == 0;
                    } else {
                        String text = TrimHtml((beneficiaryList.item(beneficiaryList.selectedIndex, null) as IHTMLOptionElement).text).ToLower();
                        if (text.EndsWith(TRUSTED_POSTFIX)) {
                            text = text.Substring(0, text.IndexOf(TRUSTED_POSTFIX));
                        } else if (text.EndsWith(NORMAL_POSTFIX)) {
                            text = text.Substring(0, text.IndexOf(NORMAL_POSTFIX));
                        }
                        return request.PaymentInfo.DefinedTransferName.ToLower().Equals(text);
                    }
                }
            }

            return false;
        }

        protected override void PastePaymentInfo(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            PaymentInfo info = request.PaymentInfo;
            if (info.IsDefinedTransfer == false) {
                IEnumerator e = doc.getElementsByTagName("input").GetEnumerator();
                while (e.MoveNext()) {
                    IHTMLInputElement input = e.Current as IHTMLInputElement;
                    if ("AccountNumberFullDestinationInput".Equals((e.Current as IHTMLInputElement).value)) {
                        (e.Current as IHTMLElement).click();
                        break;
                    }
                }

                Util.SetElementValue(doc.getElementById(ACCOUNT_FIELD), info.BankAccountNo);
                Util.SetElementValue(doc.getElementById(NAME_FIELD), ReplaceSpecialChars(info.BillerName));
                Util.SetElementValue(doc.getElementById(ADDRESS_FIELD), ReplaceSpecialChars(info.Street));
                Util.SetElementValue(doc.getElementById(POSTAL_CODE_FIELD), ReplaceSpecialChars(info.PostalCodeAndCity));
            }
            Util.SetElementValue(doc.getElementById(DESCRIPTION_FIELD), ReplaceSpecialChars(info.Title));
            Util.SetElementValue(doc.getElementsByName(AMOUNT_INT_FIELD), info.AmountToPayDecimal);
            Util.SetElementValue(doc.getElementsByName(AMOUNT_DEC_FIELD), info.AmountToPayFloating);
            // real value is taken from hidden field
            Util.SetElementValue(doc.getElementById(AMOUNT_FIELD), info.AmountToPay.Replace(".", ","));
            Util.SetElementValue(doc.getElementsByName(YEAR_FIELD), info.DueDateTimeToPaste.Year.ToString());
            Util.SetElementValue(doc.getElementsByName(MONTH_FIELD), Util.GetMonth(info.DueDateTimeToPaste.Month));
            Util.SetElementValue(doc.getElementsByName(DAY_FIELD), info.DueDateTimeToPaste.Day.ToString());

            info.State = State.IN_PROGRESS;
        }

        protected override void SelectDefinedTransferAndGoToTransferPage(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            PaymentInfo info = request.PaymentInfo;

            int count = Util.SetElementValue(doc.getElementById(BENEFICIARY_LIST), info.DefinedTransferName, true);
            if (count == -1) {
                count = Util.SetElementValue(doc.getElementById(BENEFICIARY_LIST), String.Format("{0}{1}", info.DefinedTransferName, TRUSTED_POSTFIX), true);
            }
            if (count == -1) {
                count = Util.SetElementValue(doc.getElementById(BENEFICIARY_LIST), String.Format("{0}{1}", info.DefinedTransferName, NORMAL_POSTFIX), true);
            }

            if (count == 1) {
                return;
            }
            if (count > 1) {
                Util.ShowInfoMessage(String.Format(Resources.MilleniumMoreThanOneDefinedTransfer, info.DefinedTransferName));
                return;
            }

            // count == -1 or 0
            String question = String.Format(Resources.MilleniumNoDefinedTransfer, info.DefinedTransferName);
            if (Util.ShowYesNoQuestion(null, question) == true) {
                info.DefinedTransferName = "";
                GoToTransferPage(request);
            } else {
                info.State = State.INVALID;
            }
        }

        protected override bool IsConfirmationPage(PaymentRequest request) {
            String html = TrimHtml(request.Document.getElementById("canvas").innerHTML).ToLower();
            return html.Contains("dyspozycja przelewu zosta³a przyjêta i zostanie wykonana zgodnie z zasadami obowi¹zuj¹cymi w banku")
                || html.Contains("zlecenie zosta³o wys³ane");
        }

        protected override bool IsDefinedTransfersPage(HTMLDocument doc) {
            IEnumerator e = doc.getElementsByTagName("div").GetEnumerator();
            while (e.MoveNext()) {
                if (TrimHtml((e.Current as IHTMLElement).innerHTML).ToLower().StartsWith(DOMESTIC_TRANSFER_TEXT)) {
                    return doc.getElementById(BENEFICIARY_LIST) != null;
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
        /// Checks if current page is the page with confirm information i.e. amount paid and entered bank account number.
        /// </summary>
        /// <param name="doc">HTML document (current page)</param>
        /// <returns>true if current page is the page with confirm information or false otherwise</returns>
        private bool IsConfirmPage(HTMLDocument doc) {
            return (doc.getElementById("f_Beneficiary") != null && doc.getElementById(BENEFICIARY_LIST) == null);
        }

        /// <summary>
        /// Checks if current page is the page where user should provide additional authentication for the transfer e.g. by typing SMS code.
        /// </summary>
        /// <param name="doc">HTML document (current page)</param>
        /// <returns>true if current page is the page where user should provide additional authentication for the transfer, 
        /// or false otherwise</returns>
        private bool IsCodeConfirmPage(HTMLDocument doc) {
            return doc.getElementsByName("Code").length == 1;
        }
    }
}
