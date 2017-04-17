using System;
using System.Collections;
using System.Globalization;
using mshtml;
using IE = Interop.SHDocVw;
using NUnit.Framework;
using NUnit.Mocks;

using IEPluginTests.Html;
using Billbank.IEPlugin.Domain;
using Billbank.IEPlugin.Provider;

namespace IEPluginTests.Provider {
    [TestFixture]
    class IngProviderTest : BaseProviderTest {

        #region Tests for IN_PROGRESS_CONFIRM state
        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgressConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareConfirmationPageMocks(false, null, null);
            PrepareReadyToPasteMocks(false);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateUnchangedAfterInProgressConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareConfirmationPageMocks(false, null, null);
            PrepareReadyToPasteMocks(true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsPaidAfterInProgressConfirmModifiedForAnyTransfer() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareConfirmationPageMocks(true, "2 110,1", paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            copy.AmountPaid = "2110.1";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsPaidAfterInProgressConfirmModifiedForDefinedTransfer() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareConfirmationPageMocks(true, "1 010,13", paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            copy.AmountPaid = "1010.13";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgressConfirmWrongAccountNo() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareConfirmationPageMocks(true, paymentInfo.AmountToPay, "18273764643636363");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            copy.BankAccountNo = "18273764643636363";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsPaidAfterInProgressConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareConfirmationPageMocks(true, paymentInfo.AmountToPay, paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            copy.AmountPaid = paymentInfo.AmountToPay;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for paste payment info
        [Test]
        public void ShouldPastePaymentData() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareLoggedUserMocks();
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks();

            PaymentInfo result = provider.Execute(request);

            System.Threading.Thread.Sleep(400);

            copy.State = State.IN_PROGRESS_CONFIRM;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IDLE state
        [Test]
        public void ShouldGoToTransferPageAfterIdleForDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareLoggedUserMocks();
            PrepareReadyToPasteMocks(false);
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToTransferPageAfterIdleForAnyTransfer() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareLoggedUserMocks();
            PrepareReadyToPasteMocks(false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldReplaceSpecialChars() {
            Assert.AreEqual("t e   st ", provider.ReplaceSpecialChars("t|e<>\"st'"));
        }
        #endregion

        #region Test Fixture
        protected override AbstractProvider CreateProvider() {
            return new IngProvider();
        }

        private void PrepareConfirmationPageMocks(bool result, String amountPaid, String enteredAccountNo) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            if (result == false) {
                documentMock.ExpectAndReturn("getElementById", null, "confirm");
                return;
            }

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock form = AddElementMock(typeof(FakeHtmlFormElement));
            documentMock.ExpectAndReturn("getElementById", form.MockInstance, "confirm");
            documentMock.ExpectAndReturn("getElementById", form.MockInstance, "confirm");

            String html = String.Format("<p><div>some text here</div></p><td>Przelew z rachunku " 
                + "XXXXXXXXXXXXXXXXXX na rachunek {0}{1} na kwotê {2}{3} zosta³ YYYYYYYYYYY<td>"
                + "<p>some other texts here</p>", 
                enteredAccountNo,
                paymentInfo.IsDefinedTransfer ? "," : "",
                amountPaid.Replace(",", "."),
                paymentInfo.IsDefinedTransfer ? " PLN" : "");

            form.ExpectAndReturn("get_innerHTML", html);
        }

        private void PreparePastePaymentInfoMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            PrepareInputMockById(IngProvider.CREDIT_ACCOUNT_FIELD, paymentInfo.BankAccountNo, typeof(FakeHtmlInputElement));
            PrepareInputMockById(IngProvider.RECIPIENT_FIELD, 
                        String.Format("{0}\n{1}\n{2}", 
                            provider.ReplaceSpecialChars(paymentInfo.BillerName), 
                            provider.ReplaceSpecialChars(paymentInfo.Street), 
                            provider.ReplaceSpecialChars(paymentInfo.PostalCodeAndCity).Trim()), 
                        typeof(FakeHtmlTextAreaElement));

            PrepareInputMockById(IngProvider.AMOUNT_FIELD, paymentInfo.AmountToPay.Replace(".", ","), typeof(FakeHtmlInputElement));
            PrepareInputMockById(IngProvider.TITLE_FIELD, provider.ReplaceSpecialChars(paymentInfo.Title), typeof(FakeHtmlTextAreaElement));
            PrepareInputMockById(IngProvider.DATE_TRANSFER_FIELD, paymentInfo.DueDateTimeToPaste.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), typeof(FakeHtmlInputElement));
        }

        private void PrepareGoToTransferPageMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.Expect("set_url", "https://ssl.bsk.com.pl/bskonl/transaction/transfer/pln/newtransfer.html?formRefresh=false");
        }

        private void PrepareReadyToPasteMocks(bool result) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            if (result == false) {
                documentMock.ExpectAndReturn("getElementById", null, "newtransfer");
            } else {
                documentMock.ExpectAndReturn("getElementById", AddElementMock(typeof(IHTMLElement)).MockInstance, "newtransfer");
            }
        }

        private void PrepareLoggedUserMocks() {
            request = new PaymentRequest(request.WebBrowser, request.PaymentInfo, "correctUrl.html?forceSth=false");

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock div = AddElementMock(typeof(IHTMLElement));
            DynamicMock elements = AddElementMock(typeof(IHTMLElementCollection));
            ArrayList list = new ArrayList();
            DynamicMock elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_className", "none");
            list.Add(elementMock.MockInstance);

            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_className", null);
            list.Add(elementMock.MockInstance);
            
            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_className", "logout");
            elementMock.ExpectAndReturn("get_innerHTML", "Wyjœcie");
            list.Add(elementMock.MockInstance);

            elements.ExpectAndReturn("GetEnumerator", list.GetEnumerator());
            div.ExpectAndReturn("get_all", elements.MockInstance);
            documentMock.ExpectAndReturn("getElementById", div.MockInstance, "toplinks");
        }
        #endregion
    }
}
