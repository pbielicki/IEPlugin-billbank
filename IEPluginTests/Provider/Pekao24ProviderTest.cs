using System;
using System.Collections;
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
    class Pekao24ProviderTest : BaseProviderTest {

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
        public void ShouldSetPaymentStateAsInvalidAfterInProgressWrongAccountNo() {
            paymentInfo.DefinedTransferName = "";
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks("210,03", "ABCD2384848859559");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            copy.AmountPaid = "210,03";
            copy.BankAccountNo = "ABCD2384848859559";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgressWrongPage() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock emptyCol = AddElementMock(typeof(IHTMLElementCollection));
            emptyCol.ExpectAndReturn("GetEnumerator", new ArrayList().GetEnumerator());
            documentMock.ExpectAndReturn("getElementsByName", emptyCol.MockInstance, "TextCenter");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

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
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgressAccountNoMismatch() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks("2.310,23", "ABCD2384848859559");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS_CONFIRM;
            copy.AmountPaid = "2310,23";
            copy.BankAccountNo = "ABCD2384848859559";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgress() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareConfirmPageMocks(paymentInfo.AmountToPay.Replace(".", ","), paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS_CONFIRM;
            copy.AmountPaid = paymentInfo.AmountToPay.Replace(".", ",");
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for paste payment info
        [Test]
        public void ShouldPastePaymentInfoForAnyTransfer() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldPastePaymentInfoForDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IDLE state
        [Test]
        public void ShouldSetPaymentStateAsInvalidForNotExistingDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(true);
            PrepareSelectDefinedTransfer(paymentInfo.DefinedTransferName + "123", false);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferPageNotExistingDefinedTransferHtm() {
            GlobalData.Instance.TestQuestionAnswer = true;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(true);
            PrepareSelectDefinedTransfer(paymentInfo.DefinedTransferName + "11", false);
            // trick to create correct mocks
            paymentInfo.DefinedTransferName = "";
            PrepareGoToTransferPageMocks(".htm");
            paymentInfo.DefinedTransferName = copy.DefinedTransferName;

            PaymentInfo result = provider.Execute(request);

            copy.DefinedTransferName = "";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferPageNotExistingDefinedTransferJsp() {
            GlobalData.Instance.TestQuestionAnswer = true;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(true);
            PrepareSelectDefinedTransfer(paymentInfo.DefinedTransferName + "11", false);
            // trick to create correct mocks
            paymentInfo.DefinedTransferName = "";
            PrepareGoToTransferPageMocks(".jsp");
            paymentInfo.DefinedTransferName = copy.DefinedTransferName;

            PaymentInfo result = provider.Execute(request);

            copy.DefinedTransferName = "";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSelectDefinedTransferPage() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(true);
            PrepareSelectDefinedTransfer(paymentInfo.DefinedTransferName.ToUpper(), true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToDefinedTransferListPageJspAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(false);
            PrepareGoToTransferPageMocks(".jsp");

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToDefinedTransferListPageHtmAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(false);
            PrepareGoToTransferPageMocks(".htm");

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToTransferPageAfterIdle() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareGoToTransferPageMocks(".jsp");

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldReplaceSpecialChars() {
            Assert.AreEqual("t       est", provider.ReplaceSpecialChars("t`$&\"'><est"));
        }
        #endregion

        #region Test Fixture
        protected override AbstractProvider CreateProvider() {
            return new Pekao24Provider();
        }

        private void PrepareConfirmationPageMocks(bool result) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock body = AddElementMock(typeof(IHTMLElement));
            if (result == false) {
                body.ExpectAndReturn("get_innerHTML", "<tag>something</tag>&nbsp;<MESSAGE></MESSAGE><td></td>");
            } else {
                body.ExpectAndReturn("get_innerHTML", "<tag>something</tag>&nbsp;<MESSAGE>Zlecenie Przyjêto Do Realizacji.</MESSAGE><td></td>");
            }
            documentMock.ExpectAndReturn("get_body", body.MockInstance);
        }

        private void PrepareConfirmPageMocks(String amountPaid, String enteredAccountNo) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock textCol = AddElementMock(typeof(IHTMLElementCollection));

            ArrayList textList = new ArrayList();
            DynamicMock text = AddElementMock(typeof(IHTMLElement));
            text.ExpectAndReturn("get_innerHTML", "first text");
            textList.Add(text.MockInstance);

            text = AddElementMock(typeof(IHTMLElement));
            text.ExpectAndReturn("get_innerHTML", "some text");
            textList.Add(text.MockInstance);

            text = AddElementMock(typeof(IHTMLElement));
            text.ExpectAndReturn("get_innerHTML", "  Wciœnij przycisk ZatwierdŸ, aby wykonaæ przelew.    &nbsp;");
            textList.Add(text.MockInstance);

            textCol.ExpectAndReturn("GetEnumerator", textList.GetEnumerator());
            documentMock.ExpectAndReturn("getElementsByName", textCol.MockInstance, "TextCenter");
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            ArrayList tdList = new ArrayList();
            DynamicMock td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", null);
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", " Numer Rachunku &nbsp;");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "some other text");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "&nbsp;Numer rachunku");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", " " + enteredAccountNo + "     &nbsp;");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "another text");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", " Kwota przelewu &nbsp;");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", " " + amountPaid + "&nbsp;PLN   ");
            tdList.Add(td.MockInstance);

            DynamicMock tdElements = AddElementMock(typeof(IHTMLElementCollection));
            tdElements.ExpectAndReturn("GetEnumerator", tdList.GetEnumerator());
            documentMock.ExpectAndReturn("getElementsByTagName", tdElements.MockInstance, "td");
        }

        private void PreparePastePaymentInfoMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            PrepareInputMock(Pekao24Provider.AMOUNT_A_FIELD, paymentInfo.AmountToPayDecimal, typeof(FakeHtmlInputElement));
            PrepareInputMock(Pekao24Provider.AMOUNT_B_FIELD, paymentInfo.AmountToPayFloating, typeof(FakeHtmlInputElement));
            PrepareInputMock(Pekao24Provider.DESCRIPTION_FIELD, provider.ReplaceSpecialChars(paymentInfo.Title), typeof(FakeHtmlTextAreaElement));

            if (paymentInfo.IsDefinedTransfer == false) {
                PrepareInputMock(Pekao24Provider.DESTINATION_ACCOUNT_FIELD, paymentInfo.BankAccountNo, typeof(FakeHtmlInputElement));
                PrepareInputMock(Pekao24Provider.BENEFICIARY_NAME_FIELD, provider.ReplaceSpecialChars(paymentInfo.BillerName), typeof(FakeHtmlInputElement));
                PrepareInputMock(Pekao24Provider.STREET_FIELD, provider.ReplaceSpecialChars(paymentInfo.Street), typeof(FakeHtmlInputElement));
                PrepareInputMock(Pekao24Provider.CITY_FIELD, provider.ReplaceSpecialChars(paymentInfo.City), typeof(FakeHtmlInputElement));
                PrepareInputMock(Pekao24Provider.POSTAL_CODE_FIELD, paymentInfo.PostalCode, typeof(FakeHtmlInputElement));
            }
        }

        private void PrepareSelectDefinedTransfer(String definedTransfer, bool correctName) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            ArrayList tdList = new ArrayList();

            DynamicMock mock = AddElementMock(typeof(IHTMLElement));
            mock.ExpectAndReturn("get_innerHTML", "something");
            tdList.Add(mock.MockInstance);

            mock = AddElementMock(typeof(IHTMLElement));
            mock.ExpectAndReturn("get_innerHTML", "something else");
            tdList.Add(mock.MockInstance);

            // first transfer
            AddTransfer(" 12" + definedTransfer + "12 ", tdList, false);

            // second transfer
            AddTransfer(" " + definedTransfer + " ", tdList, correctName);

            DynamicMock elements = AddElementMock(typeof(IHTMLElementCollection));
            elements.ExpectAndReturn("GetEnumerator", tdList.GetEnumerator());
            documentMock.ExpectAndReturn("getElementsByTagName", elements.MockInstance, "td");
        }

        private void AddTransfer(String definedTransfer, ArrayList tdList, bool click) {
            DynamicMock mock = AddElementMock(typeof(IHTMLElement));
            mock.ExpectAndReturn("get_innerHTML", "<INPUT name=parRadio>something</INPUT>");
            DynamicMock forms = AddElementMock(typeof(IHTMLElementCollection));
            ArrayList formList = new ArrayList();
            DynamicMock form = AddElementMock(typeof(IHTMLElement));
            if (click == true) {
                form.Expect("click");

                DynamicMock window = AddElementMock(typeof(IHTMLWindow2));
                window.Expect("execScript", "Execute()", "JScript");
                documentMock.ExpectAndReturn("get_parentWindow", window.MockInstance);
            }
            formList.Add(form.MockInstance);

            forms.ExpectAndReturn("GetEnumerator", formList.GetEnumerator());
            mock.ExpectAndReturn("get_all", forms.MockInstance);
            tdList.Add(mock.MockInstance);

            ArrayList aList = new ArrayList();
            aList.Add(AddElementMock(typeof(IHTMLElement)));
            
            mock = AddElementMock(typeof(IHTMLElement));
            mock.ExpectAndReturn("get_innerHTML", definedTransfer);
            aList.Add(mock.MockInstance);

            DynamicMock aElements = AddElementMock(typeof(IHTMLElementCollection));
            aElements.ExpectAndReturn("GetEnumerator", aList.GetEnumerator());

            mock = AddElementMock(typeof(IHTMLElement));
            mock.ExpectAndReturn("get_all", aElements.MockInstance);
            tdList.Add(mock.MockInstance);
        }

        private void PrepareDefinedTransferPageMocks(bool result) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            DynamicMock mock = AddElementMock(typeof(IHTMLElementCollection));
            if (result == false) {
                mock.ExpectAndReturn("get_length", 0);
            } else {
                mock.ExpectAndReturn("get_length", 1);
            }
            documentMock.ExpectAndReturn("getElementsByName", mock.MockInstance, Pekao24Provider.DEFINED_TRANSFERS_LIST);
        }

        private void PrepareGoToTransferPageMocks(String actionExt) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("get_url", String.Format("https://www.pekao24.pl/CurrentAccounts.{0}?subpage=93", actionExt));
            DynamicMock window = AddElementMock(typeof(IHTMLWindow2));
            if (paymentInfo.IsDefinedTransfer == false) {
                if (actionExt.Equals(".jsp")) {
                    window.Expect("execScript", Settings.Default.Pekao24NotPredefinedJspUrl, "JScript");
                } else {
                    window.Expect("execScript", Settings.Default.Pekao24NotPredefinedHtmUrl, "JScript");
                }
            } else {
                if (actionExt.Equals(".jsp")) {
                    window.Expect("execScript", Settings.Default.Pekao24PredefinedJspUrl, "JScript");
                } else {
                    window.Expect("execScript", Settings.Default.Pekao24PredefinedHtmUrl, "JScript");
                }
            }
            documentMock.ExpectAndReturn("get_parentWindow", window.MockInstance);
        }

        private void PrepareReadyToPasteMocks(bool result) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock forms = AddElementMock(typeof(IHTMLElementCollection));

            forms.ExpectAndReturn("get_length", result == true ? 1 : 0);
            if (paymentInfo.IsDefinedTransfer == false) {
                documentMock.ExpectAndReturn("getElementsByName", forms.MockInstance, Pekao24Provider.ANY_TRANSFER_FORM);
                if (result == false) {
                    return;
                }

                DynamicMock accountMock = AddElementMock(typeof(IHTMLElement));
                accountMock.ExpectAndReturn("getAttribute", "text", "type", 1);

                forms = AddElementMock(typeof(IHTMLElementCollection));
                forms.ExpectAndReturn("item", accountMock.MockInstance, Pekao24Provider.DESTINATION_ACCOUNT_FIELD, 0);
                documentMock.ExpectAndReturn("getElementsByName", forms.MockInstance, Pekao24Provider.DESTINATION_ACCOUNT_FIELD);
            } else {
                documentMock.ExpectAndReturn("getElementsByName", forms.MockInstance, Pekao24Provider.DEFINED_TRANSFER_FORM);
                if (result == false) {
                    return;
                } 
                
                forms = AddElementMock(typeof(IHTMLElementCollection));
                forms.ExpectAndReturn("get_length", 0);
                documentMock.ExpectAndReturn("getElementsByName", forms.MockInstance, Pekao24Provider.DESTINATION_ACCOUNT_FIELD);

                DynamicMock descriptionMock = AddElementMock(typeof(IHTMLElement));
                descriptionMock.ExpectAndReturn("getAttribute", "textarea", "type", 1);

                forms = AddElementMock(typeof(IHTMLElementCollection));
                forms.ExpectAndReturn("item", descriptionMock.MockInstance, Pekao24Provider.DESCRIPTION_FIELD, 0);
                documentMock.ExpectAndReturn("getElementsByName", forms.MockInstance, Pekao24Provider.DESCRIPTION_FIELD);

                PrepareIsDefinedTransferPageMocks();
            }
        }

        private void PrepareIsDefinedTransferPageMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock trElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", trElems.MockInstance, "tr");

            ArrayList trList = new ArrayList();
            DynamicMock tr = AddElementMock(typeof(IHTMLElement));
            tr.ExpectAndReturn("get_innerHTML", "some text");
            trList.Add(tr.MockInstance);

            tr = AddElementMock(typeof(IHTMLElement));
            tr.ExpectAndReturn("get_innerHTML", "<td>Nazwa Przelewu&nbsp;</td>");
            trList.Add(tr.MockInstance);

            tr = AddElementMock(typeof(IHTMLElement));
            trList.Add(tr.MockInstance);

            tr = AddElementMock(typeof(IHTMLElement));
            trList.Add(tr.MockInstance);
            DynamicMock tdElems = AddElementMock(typeof(IHTMLElementCollection));
            tr.ExpectAndReturn("get_all", tdElems.MockInstance);

            ArrayList tdList = new ArrayList();
            DynamicMock td = AddElementMock(typeof(IHTMLElement));
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerText", paymentInfo.DefinedTransferName.ToUpper());
            tdList.Add(td.MockInstance);

            tdElems.ExpectAndReturn("GetEnumerator", tdList.GetEnumerator());
            trElems.ExpectAndReturn("GetEnumerator", trList.GetEnumerator());
        }

        private void PrepareLoggedUserMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("getElementById", new DynamicMock(typeof(IHTMLElement)).MockInstance, "log-off");
        }
        #endregion
    }
}
