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
    class InteligoProviderTest : BaseProviderTest {

        #region Tests for paste payment info
        [Test]
        public void ShouldPastePaymentInfoForAnyTransferLongName() {
            paymentInfo.DefinedTransferName = "";
            paymentInfo.BillerName = "Polskie Gornictwo Naftowe i Gazownictwo S.A.";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldPastePaymentInfoForAnyTransferLongTitle() {
            paymentInfo.DefinedTransferName = "";
            paymentInfo.Title = "W ka¿dej chwili mo¿esz uzyskaæ szczegó³ow¹ pomoc na temat "
                    + "danej strony serwisu i jej funkcjonalnoœci. "
                    + "Wystarczy, ¿e klikniesz w przycisk \"Pomoc\".";

            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfoMocks();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

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
        public void ShouldGoToDefinedTransferAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(true, true, true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldGoToDefinedTransferListAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(false, false, false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStatusAsInvalidAfterIdleDefinedTransferNotExists() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            paymentInfo.DefinedTransferName = "SomeImaginaryName";
            PrepareDefinedTransferPageMocks(true, false, false);
            paymentInfo.DefinedTransferName = copy.DefinedTransferName;

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferAfterIdleDefinedTransferNotExists() {
            GlobalData.Instance.TestQuestionAnswer = true;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            paymentInfo.DefinedTransferName = "SomeImaginaryName";
            PrepareDefinedTransferPageMocks(true, false, false);

            paymentInfo.DefinedTransferName = "";
            PrepareGoToTransferPageMocks();
            paymentInfo.DefinedTransferName = copy.DefinedTransferName;

            PaymentInfo result = provider.Execute(request);

            copy.DefinedTransferName = "";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldGoToAnyTransferAfterIdle() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IN_PROGRESS state
        [Test]
        public void ShouldSetPaymentStatusAsInProgressConfirmAfterInProgressAccountNoMismatch() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareIsConfirmPageMocks("Szczegó³y realizowanej p³atnoœci. Aby zatwierdziæ, kliknij OK.");
            PrepareConfirmPageMocks("12,09", "PW1928437456565");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS_CONFIRM;
            copy.BankAccountNo = "PW1928437456565";
            copy.AmountPaid = "12,09";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStatusAsInProgressConfirmAfterInProgress() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareIsConfirmPageMocks("Szczegó³y realizowanej p³atnoœci. Aby zatwierdziæ, kliknij OK.");
            PrepareConfirmPageMocks(paymentInfo.AmountToPay, paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS_CONFIRM;
            copy.AmountPaid = paymentInfo.AmountToPay;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldSetPaymentStatusAsInvalidAfterInProgressWrongAccountNo() {
            paymentInfo.State = State.IN_PROGRESS;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareIsConfirmPageMocks("Szczegó³y Twojego przelewu jednorazowego. Aby zatwierdziæ, kliknij OK.");
            PrepareConfirmPageMocks(paymentInfo.AmountToPay, "AMC09999182828282");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            copy.AmountPaid = paymentInfo.AmountToPay;
            copy.BankAccountNo = "AMC09999182828282";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStatusAsInvalidAfterInProgress() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareIsConfirmPageMocks("wrong text");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldDoNothingAfterInProgressForDefinedTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldDoNothingAfterInProgressForAnyTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IN_PROGRESS_CONFIRM state
        [Test]
        public void ShouldSetPaymentStatusAsInvalidAfterInProgressConfirmForAnyTransfer() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareIsConfirmationPageMocks("");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStatusAsInvalidAfterInProgressConfirmForDefinedTransfer() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareIsConfirmationPageMocks("");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStatusAsPaidAfterInProgressConfirmForAnyTransfer() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareIsConfirmationPageMocks("Twój przelew zostanie zrealizowany w dniu");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStatusAsPaidAfterInProgressConfirmForDefinedTransfer() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareIsConfirmationPageMocks("Twoja p³atnoœæ zostanie zrealizowana w dniu");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldReplaceSpecialChars() {
            Assert.AreEqual("t e<> 'st", provider.ReplaceSpecialChars("t|e<>~\"st"));
            Assert.AreEqual("!@#$%^&*t()_+}e{ 's:?> <t", provider.ReplaceSpecialChars("!@#$%^&*t()_+}e{|'s:?>~<t"));
        }
        #endregion

        #region Test Fixture
        protected override AbstractProvider CreateProvider() {
            return new InteligoProvider();
        }

        private void PreparePastePaymentInfoMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            if (paymentInfo.IsDefinedTransfer == false) {
                DynamicMock accountField = AddElementMock(typeof(FakeHtmlInputElement));
                accountField.Expect("set_value", paymentInfo.BankAccountNo);
                documentMock.ExpectAndReturn("getElementById", accountField.MockInstance, InteligoProvider.ACCOUNT_FIELD);

                String[] billerName = Util.SplitString(provider.ReplaceSpecialChars(paymentInfo.BillerName), 35, InteligoProvider.BENEFICIARY_NAME_FIELD.Length);
                for (int i = 0; i < billerName.Length; i++) {
                    PrepareInputMock(InteligoProvider.BENEFICIARY_NAME_FIELD[i], billerName[i], typeof(FakeHtmlInputElement));
                }
                PrepareInputMock(InteligoProvider.ADDRESS_1_FIELD, provider.ReplaceSpecialChars(paymentInfo.Street), typeof(FakeHtmlInputElement));
                PrepareInputMock(InteligoProvider.ADDRESS_2_FIELD, provider.ReplaceSpecialChars(paymentInfo.PostalCodeAndCity), typeof(FakeHtmlInputElement));
            } else {
                documentMock.ExpectAndReturn("getElementById", null, InteligoProvider.ACCOUNT_FIELD);
                PrepareEmptyInputMock(InteligoProvider.BENEFICIARY_NAME_FIELD[0]);
                PrepareEmptyInputMock(InteligoProvider.BENEFICIARY_NAME_FIELD[1]);
                PrepareEmptyInputMock(InteligoProvider.ADDRESS_1_FIELD);
                PrepareEmptyInputMock(InteligoProvider.ADDRESS_2_FIELD);
            }

            String[] title = Util.SplitString(provider.ReplaceSpecialChars(paymentInfo.Title), 35, InteligoProvider.TITLE_FIELD.Length);
            for (int i = 0; i < title.Length; i++) {
                PrepareInputMock(InteligoProvider.TITLE_FIELD[i], title[i], typeof(FakeHtmlInputElement));
            }
            PrepareInputMock(InteligoProvider.AMOUNT_1_FIELD, paymentInfo.AmountToPayDecimal, typeof(FakeHtmlInputElement));
            PrepareInputMock(InteligoProvider.AMOUNT_2_FIELD, paymentInfo.AmountToPayFloating, typeof(FakeHtmlInputElement));
            PrepareInputMock(InteligoProvider.YEAR_FIELD, paymentInfo.DueDateTimeToPaste.Year.ToString(), typeof(FakeHtmlSelectElement));
            PrepareInputMock(InteligoProvider.MONTH_FIELD, paymentInfo.DueDateTimeToPaste.Month.ToString(), typeof(FakeHtmlSelectElement));
            PrepareInputMock(InteligoProvider.DAY_FIELD, paymentInfo.DueDateTimeToPaste.Day.ToString(), typeof(FakeHtmlSelectElement));
        }

        private void PrepareGoToTransferPageMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock window = AddElementMock(typeof(IHTMLWindow2));
            documentMock.ExpectAndReturn("get_parentWindow", window.MockInstance);
            if (paymentInfo.IsDefinedTransfer == false) {
                window.Expect("execScript", "clickMenu('onetime_transfer')", "JScript");
            } else {
                window.Expect("execScript", "clickMenu('payments')", "JScript");
            }
        }

        private void PrepareDefinedTransferPageMocks(bool result, bool repeat, bool transferNamesMatch) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock body = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("get_body", body.MockInstance);

            if (result == false) {
                body.ExpectAndReturn("get_innerHTML", "<table>some body text</table>");
                return;
            }
            body.ExpectAndReturn("get_innerHTML", "<td><p>twoje ustalone p³atnoœci dostêpne przez www.</p></td>");

            // SelectDefinedTransferAndGoToTransferPage
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock tdElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", tdElems.MockInstance, "td");
            ArrayList tdList = new ArrayList();
            DynamicMock td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_className", "someClass");
            td.ExpectAndReturn("get_className", "someClass");
            tdList.Add(td.MockInstance);

            AddDefinedTransferMock(tdList, "tableField2c", true, transferNamesMatch);
            if (repeat == true) {
                AddDefinedTransferMock(tdList, "tableField1c", false, false);
            }

            tdElems.ExpectAndReturn("GetEnumerator", tdList.GetEnumerator());
        }

        private void AddDefinedTransferMock(ArrayList tdList, String className, bool repeatClass, bool transferNamesMatch) {
            DynamicMock td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_className", className);
            if (repeatClass == true) {
                td.ExpectAndReturn("get_className", className);
            }
            td.ExpectAndReturn("get_innerHTML", "&nbsp;" + paymentInfo.DefinedTransferName + "<BR>");
            tdList.Add(td.MockInstance);

            for (int i = 0; i < 3; i++) {
                td = AddElementMock(typeof(IHTMLElement));
                tdList.Add(td.MockInstance);
            }

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "<TABLE><TR><TD>Something</TD></TR></TABLE>");

            if (transferNamesMatch == true) {
                DynamicMock table = AddElementMock(typeof(IHTMLElementCollection));
                td.ExpectAndReturn("get_all", table.MockInstance);

                ArrayList tableList = new ArrayList();
                DynamicMock anchor = AddElementMock(typeof(FakeHtmlAnchorElement));
                anchor.ExpectAndReturn("get_innerHTML", "Wykonaj");
                tableList.Add(anchor.MockInstance);

                anchor = AddElementMock(typeof(FakeHtmlAnchorElement));
                anchor.ExpectAndReturn("get_innerHTML", "Zap³aæ");
                anchor.Expect("click");
                tableList.Add(anchor.MockInstance);
                table.ExpectAndReturn("GetEnumerator", tableList.GetEnumerator());
            }
            tdList.Add(td.MockInstance);
        }

        private void PrepareConfirmPageMocks(String amountPaid, String enteredAccountNo) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock elements = new DynamicMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", elements.MockInstance, "td");

            ArrayList tdList = new ArrayList();
            DynamicMock elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", "anything");
            elementMock.ExpectAndReturn("get_innerHTML", "anything");
            tdList.Add(elementMock.MockInstance);

            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", "Numer rachunku odbiorcy");
            elementMock.ExpectAndReturn("get_innerHTML", "Numer rachunku odbiorcy");
            tdList.Add(elementMock.MockInstance);

            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", enteredAccountNo);
            tdList.Add(elementMock.MockInstance);

            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", "anything 2");
            elementMock.ExpectAndReturn("get_innerHTML", "anything 2");
            tdList.Add(elementMock.MockInstance);

            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", "Kwota");
            tdList.Add(elementMock.MockInstance);

            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", amountPaid + " PLN");
            elementMock.ExpectAndReturn("get_innerHTML", amountPaid + " PLN");
            tdList.Add(elementMock.MockInstance);

            elements.ExpectAndReturn("GetEnumerator", tdList.GetEnumerator());
        }

        private void PrepareIsConfirmPageMocks(String pText) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            ArrayList pList = new ArrayList();
            DynamicMock elementMock = new DynamicMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", null);
            pList.Add(elementMock.MockInstance);

            elementMock = new DynamicMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", "some text");
            elementMock.ExpectAndReturn("get_innerHTML", "some text");
            pList.Add(elementMock.MockInstance);

            elementMock = new DynamicMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", pText);
            pList.Add(elementMock.MockInstance);

            DynamicMock elements = new DynamicMock(typeof(IHTMLElementCollection));
            elements.ExpectAndReturn("GetEnumerator", pList.GetEnumerator());
            documentMock.ExpectAndReturn("getElementsByTagName", elements.MockInstance, "p");
        }

        private void PrepareReadyToPasteMocks(bool returnValue) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock elements = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByName", elements.MockInstance, "title_1");
            if (returnValue == false) {
                elements.ExpectAndReturn("get_length", new Random().Next(2, int.MaxValue));
                return;
            }
            elements.ExpectAndReturn("get_length", 1);
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            documentMock.ExpectAndReturn("getElementsByName", elements.MockInstance, "title_1");
            DynamicMock elementMock = AddElementMock(typeof(IHTMLElement));
            elements.ExpectAndReturn("item", elementMock.MockInstance, "title_1", 0);
            elementMock.ExpectAndReturn("getAttribute", "text", "type", 1);

            ArrayList tdList = new ArrayList();
            if (paymentInfo.IsDefinedTransfer) {
                DynamicMock element = AddElementMock(typeof(IHTMLElement));
                element.ExpectAndReturn("get_innerHTML", "some text");
                tdList.Add(element.MockInstance);

                element = AddElementMock(typeof(IHTMLElement));
                element.ExpectAndReturn("get_innerHTML", "Twoja nazwa p³atnoœci");
                tdList.Add(element.MockInstance);

                element = AddElementMock(typeof(IHTMLElement));
                element.ExpectAndReturn("get_innerHTML", paymentInfo.DefinedTransferName);
                tdList.Add(element.MockInstance);
            }

            elements = AddElementMock(typeof(IHTMLElementCollection));
            elements.ExpectAndReturn("GetEnumerator", tdList.GetEnumerator());
            documentMock.ExpectAndReturn("getElementsByTagName", elements.MockInstance, "td");
        }

        private void PrepareIsConfirmationPageMocks(String pText) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            ArrayList pList = new ArrayList();
            DynamicMock elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", null);
            pList.Add(elementMock.MockInstance);

            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", "some text");
            elementMock.ExpectAndReturn("get_innerHTML", "some text");
            pList.Add(elementMock.MockInstance);

            elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("get_innerHTML", pText + " another addition");
            elementMock.ExpectAndReturn("get_innerHTML", pText + " another addition");
            pList.Add(elementMock.MockInstance);

            DynamicMock elements = AddElementMock(typeof(IHTMLElementCollection));
            elements.ExpectAndReturn("GetEnumerator", pList.GetEnumerator());
            documentMock.ExpectAndReturn("getElementsByTagName", elements.MockInstance, "p");

        }

        private void PrepareLoggedUserMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("getElementById", new DynamicMock(typeof(IHTMLElement)).MockInstance, "koniec");
        }
        #endregion
    }
}
