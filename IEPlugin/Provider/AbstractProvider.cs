using System;
using mshtml;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public abstract class AbstractProvider : PaymentProvider {

        /// <summary>
        /// Executes payment request.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>payment infor modified by the provider</returns>
        public virtual PaymentInfo Execute(PaymentRequest request) {
            // user logged?
            if (IsUserLogged(request) == false) {
                if (request.UserAction == true) {
                    return ProviderListOpener.GetInstance().Execute(request);
                } else {
                    return request.PaymentInfo;
                }
            }

            if (Settings.Default.ReloadCount >= Settings.Default.MaxReload) {
                return request.PaymentInfo;
            }
            Settings.Default.ReloadCount++;

            switch (request.PaymentInfo.State) {
                case State.INVALID:
                case State.PAID:
                    request.PaymentInfo.State = State.INVALID;
                    return request.PaymentInfo;

                case State.IN_PROGRESS_CONFIRM:
                    return ProcessInProgressConfirm(request);

                case State.IN_PROGRESS:
                    String currentAccountNo = request.PaymentInfo.BankAccountNo;
                    PaymentInfo info = ProcessInProgress(request);
                    if (currentAccountNo.Equals(info.BankAccountNo) == false) {
                        if (info.IsDefinedTransfer == true) {
                            Util.ShowWarningMessage(Resources.WrongAccountNo);
                        } else {
                            info.State = State.INVALID;
                        }
                    }
                    return info;

                default:
                    return ProcessIdle(request);
            }
        }

        #region Utils
        protected String TrimHtml(String html) {
            if (html == null) {
                return "";
            }

            return html.Replace("&nbsp;", " ").Replace("<BR>", " ").Replace("<br>", " ").Trim();
        }

        internal String ReplaceSpecialChars(String text) {
            for (int i = 0; i < CharsToReplace().Length; i++) {
                text = text.Replace(CharsToReplace()[i], CharReplacement()[i]);
            }
            return text;
        }

        protected virtual Char[] CharReplacement() {
            return new Char[0];
        }

        protected virtual Char[] CharsToReplace() {
            return new Char[0];
        }
        #endregion

        #region ProcessXXX Methods
        /// <summary>
        /// Should process idle request i.e. after user clicks "Pay the Bill" button.
        /// This method should reload the bank's page(s) until it reaches relevant transfer form to fill.
        /// </summary>
        /// <param name="request">payment reauest</param>
        /// <returns>payment info modified (especially State) by the provider</returns>
        protected virtual PaymentInfo ProcessIdle(PaymentRequest request) {
            if (ReadyToPaste(request)) {
                PastePaymentInfo(request);

            } else if (request.PaymentInfo.IsDefinedTransfer && IsDefinedTransfersPage(request.Document)) {
                SelectDefinedTransferAndGoToTransferPage(request);

            } else {
                GoToTransferPage(request);
            }

            return request.PaymentInfo;
        }

        /// <summary>
        /// This method should check if payment in progress (i.e. the one that was pasted into transfer form)
        /// is now on the confirm page where user confirms or rejects/modifies the transfer.
        /// If the current page is confirm page the state should change to IN_PROGRESS_CONFIRM.
        /// If the current page is relevant transfer form the state should change to IN_PROGRESS.
        /// This method should also record actual value passed to the transfer - it may be modified by the user.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>payment info modified (especially State) by the provider</returns>
        protected abstract PaymentInfo ProcessInProgress(PaymentRequest request);

        /// <summary>
        /// This method should check if payment in progress (i.e. the one that was pasted into transfer form)
        /// is now on the confirmirmation page where user can see the confirmation of transaction.
        /// If the current page is confirmation page the state should change to PAID.
        /// If the current page is NOT confirmation page the state should change to INVALID.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>payment request with changed state</returns>
        protected virtual PaymentInfo ProcessInProgressConfirm(PaymentRequest request) {
            if (IsConfirmationPage(request)) {
                request.PaymentInfo.State = State.PAID;
            } else {
                request.PaymentInfo.State = State.INVALID;
            }
            return request.PaymentInfo;
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Checks if user is logged to the concrete provider (bank).
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>true if user is logged in or false otherwise</returns>
        protected abstract bool IsUserLogged(PaymentRequest request);

        /// <summary>
        /// Goes to the transfer page.
        /// </summary>
        /// <param name="request">payment request</param>
        protected abstract void GoToTransferPage(PaymentRequest request);

        /// <summary>
        /// Pastes payment information into HTML form.
        /// </summary>
        /// <param name="request">payment request</param>
        protected abstract void PastePaymentInfo(PaymentRequest request);

        /// <summary>
        /// Checks if the current page contains HTML form to fill with payment information.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>true if the current page contains HTML form to fill with payment information or false otherwise</returns>
        protected abstract bool ReadyToPaste(PaymentRequest request);

        /// <summary>
        /// Checks if the current page is the confirmation page saying that transaction is accepted.
        /// </summary>
        /// <param name="request">payment request</param>
        /// <returns>true if the current page is the confirmation or false otherwise</returns>
        protected abstract bool IsConfirmationPage(PaymentRequest request);
        #endregion

        #region Virtual Methods
        /// <summary>
        /// Selects defined transfer and goes to it's page.
        /// </summary>
        /// <param name="request">payment request</param>
        protected virtual void SelectDefinedTransferAndGoToTransferPage(PaymentRequest request) {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Checks if current page is the page with defined transfers list.
        /// </summary>
        /// <param name="doc">current HTML document</param>
        /// <returns>true if current page is the page with defined transfers list or false otherwise</returns>
        protected virtual bool IsDefinedTransfersPage(HTMLDocument doc) {
            return false;
        }
        #endregion
    }
}
