using System;
using System.Collections;
using System.Threading;
using System.Globalization;
using mshtml;
using IE = Interop.SHDocVw;
using NUnit.Framework;
using NUnit.Mocks;

using IEPluginTests.Html;
using Billbank.IEPlugin;
using Billbank.IEPlugin.Domain;
using Billbank.IEPlugin.Provider;

namespace IEPluginTests.Provider {
    [TestFixture]
    class BzWbkProviderTest : BaseProviderTest {

        #region Tests for IN_PROGRESS_CONFIRM state
        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgressConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareConfirmationPageMocks(false);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsPaidAfterInProgressConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareConfirmationPageMocks(true);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IN_PROGRESS state
        [Test]
        public void ShouldDoNothingAfterInProgress() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgress() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks(false, null, null);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgressWrongAccountNo() {
            paymentInfo.State = State.IN_PROGRESS;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks(true, "1 212.37", "988384747476636363");
            PrepareSecondConfirmPageMocks(false);

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "1212.37";
            copy.BankAccountNo = "988384747476636363";
            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgressAccountNoMismatch() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks(true, "22.15", "988384747476636363");

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "22.15";
            copy.BankAccountNo = "988384747476636363";
            copy.State = State.IN_PROGRESS_CONFIRM;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgressForAnyTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks(true, "3 004,98", paymentInfo.BankAccountNo);
            PrepareSecondConfirmPageMocks(true);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS_CONFIRM;
            copy.AmountPaid = "3004,98";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressAfterInProgressForAnyTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks(true, "34,98", paymentInfo.BankAccountNo);
            PrepareSecondConfirmPageMocks(false);

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "34,98";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgressForDefinedTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks(true, "10,23", paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "10,23";
            copy.State = State.IN_PROGRESS_CONFIRM;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for paste payment info
        [Test]
        public void ShouldGoToTransferPageOnPastePaymentInfoForNotSelectedAnyTransfer() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks(false, true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentSateAsInvalidAfterIdleNoDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            paymentInfo.DefinedTransferName = "XYZ123";
            PreparePastePaymentInfoMocks(false, false);
            paymentInfo.DefinedTransferName = copy.DefinedTransferName;

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldPastePaymentInfoForDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks(true, false);

            PaymentInfo result = provider.Execute(request);
            Thread.Sleep(400);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldPastePaymentInfoForAnyTransferAsDefinedTransferNotFound() {
            GlobalData.Instance.TestQuestionAnswer = true;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            paymentInfo.DefinedTransferName = "XYZ123";
            PreparePastePaymentInfoMocks(false, false);
            paymentInfo.DefinedTransferName = "";
            PreparePastePaymentInfoMocks(true, false);
            paymentInfo.DefinedTransferName = copy.DefinedTransferName;

            PaymentInfo result = provider.Execute(request);
            Thread.Sleep(400);

            copy.DefinedTransferName = "";
            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldPastePaymentInfoForAnyTransferLongName() {
            paymentInfo.DefinedTransferName = "";
            paymentInfo.BillerName = "some Very Very long and annoying name of the biller";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks(true, false);

            PaymentInfo result = provider.Execute(request);
            Thread.Sleep(400);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldPastePaymentInfoForAnyTransfer() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks(true, false);

            PaymentInfo result = provider.Execute(request);
            Thread.Sleep(400);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IDLE state
        [Test]
        public void ShouldGoToTransferPageAfterIdleAnyTransfer() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldGoToTransferPageAfterIdleDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldReplaceSpecialChars() {
            Assert.AreEqual("te                           st", provider.ReplaceSpecialChars("te~`!@#$%^&*()_+{}|:\"<>?=[]\\'st"));
        }
        #endregion

        #region Test Fixture
        protected override AbstractProvider CreateProvider() {
            return new BzWbkProvider();
        }

        private void PrepareSecondConfirmPageMocks(bool result) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            if (result == false) {
                documentMock.ExpectAndReturn("getElementById", null, "authorization_response");
                return;
            }
            DynamicMock smsCodeField = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", smsCodeField.MockInstance, "authorization_response");
            smsCodeField.ExpectAndReturn("getAttribute", "text", "type", 1);
        }

        private void PrepareConfirmationPageMocks(bool result) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock liElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", liElems.MockInstance, "li");

            ArrayList liList = new ArrayList();
            if (result == true) {
                DynamicMock li = AddElementMock(typeof(IHTMLElement));
                li.ExpectAndReturn("get_className", "some class");
                liList.Add(li.MockInstance);

                li = AddElementMock(typeof(IHTMLElement));
                li.ExpectAndReturn("get_className", "selected-last-step");
                li.ExpectAndReturn("get_innerHTML", "<span><strong>3</strong>&nbsp;KONIEC&nbsp;</span>");
                liList.Add(li.MockInstance);
            }

            liElems.ExpectAndReturn("GetEnumerator", liList.GetEnumerator());
        }

        private void PrepareConfirmPageMocks(bool result, String amountPaid, String enteredAccountNo) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock liElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", liElems.MockInstance, "li");

            ArrayList liList = new ArrayList();
            if (result == true) {
                DynamicMock li = AddElementMock(typeof(IHTMLElement));
                li.ExpectAndReturn("get_className", "some class");
                liList.Add(li.MockInstance);

                li = AddElementMock(typeof(IHTMLElement));
                li.ExpectAndReturn("get_className", "selected-step");
                li.ExpectAndReturn("get_innerHTML", "<span><strong>2</strong>POTWIERDZENIE</span>");
                liList.Add(li.MockInstance);
            }

            liElems.ExpectAndReturn("GetEnumerator", liList.GetEnumerator());

            if (result == true) {
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
                DynamicMock spanElems = AddElementMock(typeof(IHTMLElementCollection));
                documentMock.ExpectAndReturn("getElementsByTagName", spanElems.MockInstance, "span");

                ArrayList spanList = new ArrayList();
                DynamicMock span = AddElementMock(typeof(IHTMLElement));
                span.ExpectAndReturn("get_innerHTML", "some text");
                spanList.Add(span.MockInstance);

                span = AddElementMock(typeof(IHTMLElement));
                span.ExpectAndReturn("get_innerHTML", "Numer Rachunku:&nbsp;");
                spanList.Add(span.MockInstance);

                span = AddElementMock(typeof(IHTMLElement));
                span.ExpectAndReturn("get_innerHTML", enteredAccountNo + "&nbsp;");
                spanList.Add(span.MockInstance);

                span = AddElementMock(typeof(IHTMLElement));
                span.ExpectAndReturn("get_innerHTML", "some text");
                spanList.Add(span.MockInstance);

                span = AddElementMock(typeof(IHTMLElement));
                span.ExpectAndReturn("get_innerHTML", "Kwota:&nbsp;");
                spanList.Add(span.MockInstance);

                span = AddElementMock(typeof(IHTMLElement));
                span.ExpectAndReturn("get_innerHTML", amountPaid + "&nbsp;PLN");
                spanList.Add(span.MockInstance);

                spanElems.ExpectAndReturn("GetEnumerator", spanList.GetEnumerator());
            }
        }

        private void PreparePastePaymentInfoMocks(bool fullPath, bool goToTransferPage) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            if (paymentInfo.IsDefinedTransfer == false) {
                PrepareIsAnyTransferSelectedMocks(goToTransferPage == false);
                if (goToTransferPage == true) {
                    PrepareGoToTransferPageMocks();
                    return;
                }

                PrepareInputMock(BzWbkProvider.ACCOUNT_FIELD, paymentInfo.BankAccountNo, typeof(FakeHtmlInputElement));
                if (paymentInfo.BillerName.Length > 32) {
                    PrepareInputMock(BzWbkProvider.NAME_FIELD, provider.ReplaceSpecialChars(paymentInfo.BillerName).Substring(0, 32), typeof(FakeHtmlInputElement));
                } else {
                    PrepareInputMock(BzWbkProvider.NAME_FIELD, provider.ReplaceSpecialChars(paymentInfo.BillerName), typeof(FakeHtmlInputElement));
                }
                PrepareInputMock(BzWbkProvider.STREET_FIELD, provider.ReplaceSpecialChars(paymentInfo.Street), typeof(FakeHtmlInputElement));
                PrepareInputMock(BzWbkProvider.CITY_FIELD, provider.ReplaceSpecialChars(paymentInfo.City), typeof(FakeHtmlInputElement));
                PrepareInputMock(BzWbkProvider.POSTAL_CODE_FIELD, paymentInfo.PostalCode, typeof(FakeHtmlInputElement));
            } else {
                PrepareInputMock(BzWbkProvider.DEFINED_TRANSFER_FIELD, paymentInfo.DefinedTransferName, typeof(FakeHtmlSelectElement), fullPath, fullPath);
                if (fullPath == false) {
                    return;
                }
            }
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            
            PrepareInputMock(BzWbkProvider.AMOUNT_FIELD, paymentInfo.AmountToPay.Replace(".", ","), typeof(FakeHtmlInputElement));
            PrepareInputMock(BzWbkProvider.TITLE_FIELD, provider.ReplaceSpecialChars(paymentInfo.Title), typeof(FakeHtmlTextAreaElement));
            PrepareInputMock(BzWbkProvider.DATE_FIELD, paymentInfo.DueDateTimeToPaste.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), typeof(FakeHtmlInputElement));
        }

        private void PrepareIsAnyTransferSelectedMocks(bool result) {
            DynamicMock col = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByName", col.MockInstance, BzWbkProvider.DEFINED_TRANSFER_FIELD);
            DynamicMock select = AddElementMock(typeof(FakeHtmlSelectElement));
            col.ExpectAndReturn("item", select.MockInstance, 0, null);
            if (result == false) {
                select.ExpectAndReturn("get_selectedIndex", 1);
            } else {
                select.ExpectAndReturn("get_selectedIndex", 0);
                DynamicMock option = AddElementMock(typeof(FakeHtmlOptionElement));
                select.ExpectAndReturn("item", option.MockInstance, 0, null);
                option.ExpectAndReturn("get_text", "WYBIERZ");
            }
        }

        private void PrepareGoToTransferPageMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            DynamicMock menuTransfers = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", menuTransfers.MockInstance, "menu_transfers");

            DynamicMock aElems = AddElementMock(typeof(IHTMLElementCollection));
            menuTransfers.ExpectAndReturn("get_all", aElems.MockInstance);
            ArrayList aList = new ArrayList();
            DynamicMock a = AddElementMock(typeof(IHTMLElement));
            a.ExpectAndReturn("get_innerHTML", "some text");
            aList.Add(a.MockInstance);

            a = AddElementMock(typeof(IHTMLElement));
            a.ExpectAndReturn("get_innerHTML", "&nbsp;PRZELEWY");
            a.Expect("click");
            aList.Add(a.MockInstance);

            aElems.ExpectAndReturn("GetEnumerator", aList.GetEnumerator());
        }

        private void PrepareReadyToPasteMocks(bool result) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            DynamicMock col = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", col.MockInstance, "h2");
            ArrayList elements = new ArrayList();
            DynamicMock h2 = AddElementMock(typeof(IHTMLElement));
            h2.ExpectAndReturn("get_innerHTML", "anything");
            elements.Add(h2.MockInstance);

            h2 = AddElementMock(typeof(IHTMLElement));
            h2.ExpectAndReturn("get_innerHTML", "Przelew Krajowy Na Rachunek&nbsp;obcy");
            elements.Add(h2.MockInstance);
            col.ExpectAndReturn("GetEnumerator", elements.GetEnumerator());

            DynamicMock menuTransfers = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", menuTransfers.MockInstance, "menu_transfers");
            if (result == false) {
                menuTransfers.ExpectAndReturn("get_className", null);
                if (paymentInfo.IsDefinedTransfer == true && paymentInfo.State == State.IDLE) {
                   browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
                }
                return;
            } else {
                menuTransfers.ExpectAndReturn("get_className", "selected");
            }

            DynamicMock liElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", liElems.MockInstance, "li");
            ArrayList liList = new ArrayList();
            DynamicMock li = AddElementMock(typeof(IHTMLElement));
            li.ExpectAndReturn("get_innerHTML", "<p>Dane</p>");
            li.ExpectAndReturn("get_className", "first-step-empty");
            liList.Add(li.MockInstance);

            li = AddElementMock(typeof(IHTMLElement));
            li.ExpectAndReturn("get_innerHTML", "<p>Dane</p>");
            li.ExpectAndReturn("get_className", "first-step");
            liList.Add(li.MockInstance);

            liElems.ExpectAndReturn("GetEnumerator", liList.GetEnumerator());
        }

        private void PrepareLoggedUserMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("get_url", "https://bank/url/centrum24-web/");

            DynamicMock aElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", aElems.MockInstance, "a");

            ArrayList aList = new ArrayList();
            DynamicMock a = AddElementMock(typeof(IHTMLElement));
            a.ExpectAndReturn("get_innerHTML", "some text");
            aList.Add(a.MockInstance);

            a = AddElementMock(typeof(IHTMLElement));
            a.ExpectAndReturn("get_innerHTML", "&nbsp;WyLoguj");
            aList.Add(a.MockInstance);

            aElems.ExpectAndReturn("GetEnumerator", aList.GetEnumerator());
        }
        #endregion
    }
}
