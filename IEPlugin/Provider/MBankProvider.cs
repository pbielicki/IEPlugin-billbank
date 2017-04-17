using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using IE = Interop.SHDocVw;
using mshtml;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class MBankProvider : AbstractProvider {

        #region Contants
        internal static readonly String BASE_URL = "https://www.mbank.com.pl/";
        internal static readonly String ACCOUNTS_LIST = "accounts_list.aspx";
        internal static readonly String LOGON = "logon.aspx";
        internal static readonly String DEFINED_TRANSFERS_LIST = "defined_transfers_list.aspx";
        internal static readonly String ASPX = ".aspx";
        internal static readonly String DEFINED_TRANSFERS_SCRIPT = "doSubmit('defined_transfers_list.aspx','','POST',null, false,true,false,null);";
        internal static readonly String ANY_TRANSFER_SCRIPT = "doSubmit('transfer_exec.aspx','','POST',null, false,true,false,null);";

        internal static readonly String ACCOUNT_FIELD = "tbReceiverAccNo";
        internal static readonly String TITLE_FIELD = "tbTransferTitle";
        internal static readonly String NAME_FIELD = "tbRecName";
        internal static readonly String ADDRESS_FIELD = "tbRecAddress";
        internal static readonly String CITY_FIELD = "tbRecCity";
        internal static readonly String DAY_FIELD = "dtbTransferDate_day";
        internal static readonly String MONTH_FIELD = "dtbTransferDate_month";
        internal static readonly String YEAR_FIELD = "dtbTransferDate_year";
        internal static readonly String AMOUNT_FIELD = "tbAmount";
        internal static readonly String MODIFY_BUTTON = "Modify";
        internal static readonly String CONFIRM_BUTTON = "Confirm";
        internal static readonly String DEFINED_ACCOUNT_FIELD = "anRecAccount";
        internal static readonly String DEFINED_TRANSFER_EXEC_DIV = "defined_transfer_exec";
        internal static readonly String TRANSFER_EXEC_DIV = "transfer_exec";
        internal static readonly String SPAN_BEGIN = "<span>";
        internal static readonly String SPAN_END = "</span>";

        internal static readonly String[] SUPPORTED_ACCOUNTS = new String[] {"eKONTO", "izzyKONTO", "mBIZNES Konto"};
        private static readonly Char[] CHARS_TO_REPLACE = "|<>\"".ToCharArray();
        private static readonly Char[] CHAR_REPLACEMENT = " ()'".ToCharArray();
        #endregion

        #region Overriden methods
        /// <summary>
        /// Processes idle (original) payment request i.e. goes to the relevant URL (accounts list,
        /// defined transfers list, etc.) and copies data from payment request to the HTML form.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>payment request with updated state</returns>
        protected override PaymentInfo ProcessIdle(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            if (ReadyToPaste(request)) {
                PastePaymentInfo(request);

            } else if (request.PaymentInfo.IsDefinedTransfer && IsDefinedTransfersPage(doc)) {
                SelectDefinedTransferAndGoToTransferPage(request);

            } else if (IsAccountsListPage(doc)) {
                SelectAccountAndGoToTransferPage(request);

            } else if (AnyAccountSelected(doc)) {
                GoToTransferPage(request);

                // user logged but incorrect page displayed (we need accounts list)
            } else if (IsAccountsListPage(doc) == false && ShouldOpenAccountsList(doc)) {
                GoToAccountsListPage(doc);
            }

            return request.PaymentInfo;
        }

        /// <summary>
        /// Processes payment request that was copied and now waits for confirmation by the user.
        /// This method also takes real value that was declared in the transfer.
        /// If current page is the confirmation page the state of payment request is set to
        /// IN_PROGRESS_CONFIRM and INVALID otherwise.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>payment request with changed state</returns>
        protected override PaymentInfo ProcessInProgress(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;
            if (ReadyToPaste(request)) {
                return info;
            }
            if (IsConfirmPage(request.Document)) {
                IHTMLElement div = request.Document.getElementById(TRANSFER_EXEC_DIV);
                if (div == null) {
                    div = request.Document.getElementById(DEFINED_TRANSFER_EXEC_DIV);
                }
                if (div != null) {
                    info = CheckConfirmDiv(info, div);
                    if (info.State == State.IN_PROGRESS_CONFIRM) {
                        return info;
                    }
                }
            }
            info.State = State.INVALID;
            return info;
        }

        /// <summary>
        /// Checks if current page is the page to paste the payment information.
        /// Different behavior and conditions are checked for defined and any transfers.
        /// </summary>
        /// <returns>true if current page is the page on which payment info should be pasted</returns>
        protected override bool ReadyToPaste(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            IHTMLElement element = doc.getElementById(AMOUNT_FIELD);
            bool conditionForAll = doc.url.EndsWith(ASPX)
                    && element != null
                    && element.getAttribute("type", 1).ToString().Equals("text");

            if (conditionForAll == false) {
                return conditionForAll;
            }

            element = doc.getElementById(DEFINED_ACCOUNT_FIELD);
            if (conditionForAll
                && element != null
                && element.getAttribute("type", 1).ToString().Equals("hidden") == true) {

                element = doc.getElementById(DEFINED_TRANSFER_EXEC_DIV);
                if (element != null) {
                    if (request.PaymentInfo.IsDefinedTransfer == false) {
                        return false;
                    }
                    int start = element.innerHTML.ToLower().IndexOf(SPAN_BEGIN);
                    if (start == -1) {
                        return false;
                    }
                    int end = element.innerHTML.ToLower().IndexOf(SPAN_END, start);
                    if (end == -1) {
                        return false;
                    }
                    String currentName = element.innerHTML.Substring(start + 6, end - start - 6);
                    return request.PaymentInfo.DefinedTransferName.ToLower().Equals(currentName.ToLower());
                }
            }

            return request.PaymentInfo.IsDefinedTransfer == false;
        }

        /// <summary>
        /// Selects defined transfer (taken from payment request) and goes to the transfer page.
        /// If defined transfer from payment request doesn't exist it gives user opportunity
        /// to go to any transfer form - if user doesn't want that, this methods cancels payment request.
        /// </summary>
        /// <param name="request">payment request</param>
        protected override void SelectDefinedTransferAndGoToTransferPage(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            PaymentInfo info = request.PaymentInfo;
            IHTMLElement transfersGrid = doc.getElementById("BaseDefinedTransfersList") as IHTMLElement;
            List<String> transferId = GetIdList(transfersGrid, "BaseDefinedTransfersList_grid_ctl");

            String linkId = null;
            int count = 0;
            foreach (String id in transferId) {
                IHTMLElement a = doc.getElementById(id);
                if (a != null && info.DefinedTransferName.ToLower().Equals(a.innerHTML.ToLower())) {
                    if (linkId == null) {
                        linkId = id.Substring(0, id.Length - 2);
                    }
                    count++;
                }
            }

            if (count > 0) {
                // more than one defined transfer with the provided name
                if (count > 1) {
                    String msg = String.Format(Resources.MBankMoreThanOneDefinedTransfer, info.DefinedTransferName);
                    Util.ShowInfoMessage(msg);
                }
                doc.getElementById(String.Format("{0}_4_exec", linkId)).click();
            } else {
                // no defined transfer with the provided name
                String question = String.Format(Resources.MBankNoDefinedTransfer, info.DefinedTransferName);

                if (Util.ShowYesNoQuestion(null, question) == true) {
                    info.DefinedTransferName = "";
                    GoToTransferPage(request);
                } else {
                    info.State = State.INVALID;
                }
            }
        }

        /// <summary>
        /// Goes to the transfer page (when account is already chosen). Depending on whether
        /// it's defined transfer or not it goes to the appropriate page.
        /// </summary>
        /// <param name="request"></param>
        protected override void GoToTransferPage(PaymentRequest request) {
            bool result = false;
            if (request.PaymentInfo.IsDefinedTransfer) {
                result = Util.RunJavaScript(request.Document, DEFINED_TRANSFERS_SCRIPT);
            } else {
                result = Util.RunJavaScript(request.Document, ANY_TRANSFER_SCRIPT);
            }

            if (result == false) {
                request.PaymentInfo.State = State.INVALID;
            }
        }

        /// <summary>
        /// Copies payment data from object to HTML form.
        /// </summary>
        /// <param name="request">Payment request</param>
        protected override void PastePaymentInfo(PaymentRequest request) {
            PaymentInfo info = request.PaymentInfo;

            HTMLDocument doc = request.Document;
            Util.SetElementValue(doc.getElementById(ACCOUNT_FIELD), info.BankAccountNo);
            Util.SetElementValue(doc.getElementById(TITLE_FIELD), ReplaceSpecialChars(info.Title));
            Util.SetElementValue(doc.getElementById(NAME_FIELD), ReplaceSpecialChars(info.BillerName));
            Util.SetElementValue(doc.getElementById(ADDRESS_FIELD), ReplaceSpecialChars(info.Street));
            Util.SetElementValue(doc.getElementById(CITY_FIELD), ReplaceSpecialChars(info.PostalCodeAndCity));
            Util.SetElementValue(doc.getElementById(DAY_FIELD), info.DueDateTimeToPaste.Day.ToString());
            Util.SetElementValue(doc.getElementById(MONTH_FIELD), info.DueDateTimeToPaste.Month.ToString());
            Util.SetElementValue(doc.getElementById(YEAR_FIELD), info.DueDateTimeToPaste.Year.ToString());
            Util.SetElementValue(doc.getElementById(AMOUNT_FIELD), info.AmountToPay.Replace('.', ','));

            info.State = State.IN_PROGRESS;
        }

        protected override bool IsUserLogged(PaymentRequest request) {
            return BASE_URL.Equals(request.Document.url) == false;
        }

        protected override Char[] CharReplacement() {
            return CHAR_REPLACEMENT;
        }

        protected override Char[] CharsToReplace() {
            return CHARS_TO_REPLACE;
        }
        #endregion

        #region IsXXX, ShouldYYY, ContainsZZZ, etc. methods
        /// <summary>
        /// Checks if current page is the page with accounts list
        /// </summary>
        /// <param name="doc">HTML document</param>
        /// <returns>true if current page is the page with accounts list</returns>
        protected bool IsAccountsListPage(HTMLDocument doc) {
            if (Util.GetFramesCount(doc) == 2) {
                return false;
            }
            return (Util.GetFramesCount(doc) == 0 && doc.url.EndsWith(ACCOUNTS_LIST));
        }

        /// <summary>
        /// Checks if current page is the page with defined transfers list
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        protected override bool IsDefinedTransfersPage(HTMLDocument doc) {
            if (Util.GetFramesCount(doc) == 2) {
                return false;
            }
            return (Util.GetFramesCount(doc) == 0 && doc.url.EndsWith(DEFINED_TRANSFERS_LIST));
        }

        /// <summary>
        /// Checks if any supported account is already chosen.
        /// </summary>
        /// <param name="doc">HTML document</param>
        /// <returns>true is any of the supported accounts is already chosen or false otherwise</returns>
        protected bool AnyAccountSelected(HTMLDocument doc) {
            IHTMLElement contextInfo = doc.getElementById("contextInfo");
            if (contextInfo != null) {
                int start = contextInfo.innerHTML.ToLower().IndexOf(SPAN_BEGIN);
                if (start != -1) {
                    start = contextInfo.innerHTML.ToLower().IndexOf(SPAN_BEGIN, start + 1);
                    return (start != -1 && ContainsAccountName(contextInfo.innerHTML.Substring(start + 6)));
                }
            }
            return false;
        }

        protected bool ShouldOpenAccountsList(HTMLDocument doc) {
            if (Util.GetFramesCount(doc) == 2) {
                return true;
            }
            return (Util.GetFramesCount(doc) < 2
                && doc.url.EndsWith(ASPX)
                && doc.url.EndsWith(ACCOUNTS_LIST) == false
                && doc.url.EndsWith(LOGON) == false);
        }

        private bool IsConfirmPage(HTMLDocument doc) {
            return (doc.getElementById(MODIFY_BUTTON) != null && doc.getElementById(CONFIRM_BUTTON) != null);
        }

        protected override bool IsConfirmationPage(PaymentRequest request) {
            IHTMLElement div = request.Document.getElementById("msg");
            if (div != null) {
                return div.innerHTML.Contains("Dyspozycja przelewu zosta³a przyjêta.");
            }
            return false;
        }

        /// <summary>
        /// Checks if given HTML fragment starts with any of the supported account type.
        /// </summary>
        /// <param name="html">HTML fragment</param>
        /// <returns>true if given fragment starts with any of the supported account type or false otherwise</returns>
        private bool ContainsAccountName(String html) {
            for (int i = 0; i < SUPPORTED_ACCOUNTS.Length; i++) {
                if (html.StartsWith(SUPPORTED_ACCOUNTS[i])) {
                    return true;
                }
            }
            return false;
        }
        #endregion

        /// <summary>
        /// Selects account (first supported one) and goes to the pay execution page
        /// </summary>
        /// <param name="request">payment request passed from sidebar</param>
        protected void SelectAccountAndGoToTransferPage(PaymentRequest request) {
            HTMLDocument doc = request.Document;
            PaymentInfo info = request.PaymentInfo;
            IHTMLElement accountsGrid = doc.getElementById("AccountsGrid") as IHTMLElement;
            List<String> accountId = GetIdList(accountsGrid, "AccountsGrid_grid_ctl");

            String linkId = null;
            int count = 0;
            foreach (String id in accountId) {
                IHTMLElement a = doc.getElementById(id);
                if (ContainsAccountName(a.innerHTML)) {
                    linkId = id.Substring(0, id.Length - 2);
                    linkId = String.Format("{0}{1}", linkId, info.DefinedTransferName.Trim().Length == 0 ? "_3_A0" : "_3_A2");
                    count++;
                }
            }

            if (count > 0) {
                if (count > 1) {
                    Util.ShowInfoMessage(Resources.ChooseAccount);
                } else {
                    doc.getElementById(linkId).click();
                    return;
                }
            } else {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < SUPPORTED_ACCOUNTS.Length; i++) {
                    if (i == SUPPORTED_ACCOUNTS.Length - 1) {
                        sb.Append(" i ");
                    } else if (i > 0) {
                        sb.Append(", ");
                    }
                    sb.Append(SUPPORTED_ACCOUNTS[i]);
                }

                Util.ShowErrorMessage(String.Format(Resources.AccountNotFound, sb.ToString()));
            }
            info.State = State.INVALID;
        }

        /// <summary>
        /// Goes to the accounts list page
        /// </summary>
        /// <param name="doc">HTML document</param>
        protected void GoToAccountsListPage(HTMLDocument doc) {
            doc.url = String.Format("{0}{1}", BASE_URL, ACCOUNTS_LIST);
        }

        /// <summary>
        /// Gets Id list of all elements from given HTML fragment whose Ids starts with given Id string.
        /// </summary>
        /// <param name="html">HTML fragment</param>
        /// <param name="idString">Id string that is the beginning of each retrieved Id</param>
        /// <param name="prefixLength">prefix of the string to substring correctly e.g. length of "id=" string</param>
        /// <returns>list of all elements from given HTML fragment whose Ids starts with given Id string</returns>
        private List<String> GetIdList(IHTMLElement html, String idString) {
            List<String> result = new List<String>();
            IEnumerator e = (html.all as IHTMLElementCollection).GetEnumerator();

            while (e.MoveNext()) {
                if (e.Current is IHTMLAnchorElement) {
                    IHTMLElement element = e.Current as IHTMLElement;
                    if (element.id != null && element.id.StartsWith(idString)) {
                        result.Add(element.id);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if confirm DIV element contains all necessary onformation to move payment to the next state.
        /// If everything is OK state of the provided payment will be set to IN_PROGRESS_CONFIRM - it
        /// will be set to INVALID otherwise.
        /// </summary>
        /// <param name="info">payment information</param>
        /// <param name="div">HTML DIV element containing confirm information</param>
        /// <returns></returns>
        private PaymentInfo CheckConfirmDiv(PaymentInfo info, IHTMLElement div) {
            String divHtml = div.innerHTML.ToLower();
            int start = divHtml.IndexOf("<span class=\"text amount\">");
            if (start != -1) {
                int end = divHtml.IndexOf(SPAN_END, start);
                if (end != -1) {
                    info.AmountPaid = divHtml.Substring(start + 26, end - start - 26).Replace(" ", "");
                    info.State = State.IN_PROGRESS_CONFIRM;
                }
            }

            IEnumerator e = (div.all as IHTMLElementCollection).GetEnumerator();
            while (e.MoveNext()) {
                IHTMLElement element = e.Current as IHTMLElement;
                if (element != null && "Rachunek odbiorcy".Equals(element.innerHTML)) {
                    // account number is two fields ahead
                    if (e.MoveNext() == false) {
                        break;
                    }
                    if (e.MoveNext() == false) {
                        break;
                    }
                    element = e.Current as IHTMLElement;
                    info.BankAccountNo = element.innerHTML;
                    break;
                }
            }
            return info;
        }
    }
}
