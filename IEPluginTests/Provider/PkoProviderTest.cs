using System;
using System.Collections;
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
    class PkoProviderTest : BaseProviderTest {

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
            PrepareConfirmPageMocks(true, "212.37", "988384747476636363");

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "212.37";
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
            PrepareConfirmPageMocks(true, "3 222.15", "988384747476636363");

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "3222.15";
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
            PrepareConfirmPageMocks(true, "34,98", paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "34,98";
            copy.State = State.IN_PROGRESS_CONFIRM;
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
        public void ShouldPastePaymentInfoForDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfo();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldPastePaymentInfoForAnyTransferLongTitle150Chars() {
            paymentInfo.DefinedTransferName = "";
            paymentInfo.Title = "W ka¿dej chwili mo¿esz uzyskaæ szczegó³ow¹ pomoc na temat "
                                + "danej strony serwisu i jej funkcjonalnoœci. "
                                + "Wystarczy, ¿e klikniesz w przycisk \"Pomoc\".";

            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfo();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldPastePaymentInfoForAnyTransferLongName() {
            paymentInfo.DefinedTransferName = "";
            paymentInfo.BillerName = "Polskie Gornictwo Naftowe i Gazownictwo S.A";

            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfo();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldPastePaymentInfoForAnyTransferShortTitle() {
            paymentInfo.DefinedTransferName = "";
            paymentInfo.Title = "Polskie Górnictwo Naftowe";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true);
            PreparePastePaymentInfo();

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
            PreparePastePaymentInfo();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IDLE state
        [Test]
        public void ShouldGoToDefinedTransferPageAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(true, true, true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterIdleDefinedTransferNotExists() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            paymentInfo.DefinedTransferName = "1234567890";
            PrepareDefinedTransferPageMocks(true, false, false);
            paymentInfo.DefinedTransferName = copy.DefinedTransferName;

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldGoToAnyTransferPageAfterIdleDefinedTransferNotExists() {
            GlobalData.Instance.TestQuestionAnswer = true;

            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            paymentInfo.DefinedTransferName = "1234567890";
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
        public void ShouldGoToDefinedTransferListPageAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareDefinedTransferPageMocks(false, false, false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferPageAfterIdle() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldReplaceSpecialChars() {
            Assert.AreEqual("te  'st", provider.ReplaceSpecialChars("te~|\"st"));
        }
        #endregion

        #region Test Fixture
        protected override AbstractProvider CreateProvider() {
            return new PkoProvider();
        }

        private void PrepareConfirmationPageMocks(bool result) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock h3Elems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", h3Elems.MockInstance, "h3");
            ArrayList h3Table = new ArrayList();
            DynamicMock h3 = AddElementMock(typeof(IHTMLElement));
            h3.ExpectAndReturn("get_innerHTML", "some text");
            h3Table.Add(h3.MockInstance);

            h3 = AddElementMock(typeof(IHTMLElement));
            h3.ExpectAndReturn("get_innerHTML", "some other text");
            h3Table.Add(h3.MockInstance);

            if (result == true) {
                h3 = AddElementMock(typeof(IHTMLElement));
                h3.ExpectAndReturn("get_innerHTML", "Transakcja Zosta³a Przyjêta Do Realizacji.<br><br>Data wykonania przelewu to ");
                h3Table.Add(h3.MockInstance);
            }

            h3Elems.ExpectAndReturn("GetEnumerator", h3Table.GetEnumerator());
        }

        private void PrepareConfirmPageMocks(bool result, String amountPaid, String enteredAccountNo) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock docHeader = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", docHeader.MockInstance, "docheader");
            if (result == false) {
                docHeader.ExpectAndReturn("get_innerHTML", "some text");
                return;
            }

            if (paymentInfo.IsDefinedTransfer == false) {
                docHeader.ExpectAndReturn("get_innerHTML", "<p>some text</p><h1>Potwierdzenie&nbsp;Zlecenia Przelewu Jednorazowego</h1>");
            } else {
                docHeader.ExpectAndReturn("get_innerHTML", "<p>some text</p><h1>Potwierdzenie&nbsp;realizacji P³atnoœci</h1>");
            }

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock tdElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", tdElems.MockInstance, "td");
            ArrayList tdList = new ArrayList();
            DynamicMock td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "some td value");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "Numer&nbsp;Rachunku Odbiorcy");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "&nbsp;" + enteredAccountNo + "&nbsp;<table></table>");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "some other td value");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", "Kwota&nbsp;");
            tdList.Add(td.MockInstance);

            td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", amountPaid + "&nbsp;PLN");
            tdList.Add(td.MockInstance);

            tdElems.ExpectAndReturn("GetEnumerator", tdList.GetEnumerator());
        }

        private void PreparePastePaymentInfo() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            if (paymentInfo.IsDefinedTransfer == false) {
                PrepareInputMockById(PkoProvider.ACCOUNT_FIELD, paymentInfo.BankAccountNo, typeof(FakeHtmlInputElement));

                String[] billerName = Util.SplitString(provider.ReplaceSpecialChars(paymentInfo.BillerName), 35, PkoProvider.BENEFICIARY_NAME_FIELD.Length);
                for (int i = 0; i < billerName.Length; i++) {
                    PrepareInputMockById(PkoProvider.BENEFICIARY_NAME_FIELD[i], billerName[i], typeof(FakeHtmlInputElement));
                }
                PrepareInputMockById(PkoProvider.ADDRESS_1_FIELD, provider.ReplaceSpecialChars(paymentInfo.Street), typeof(FakeHtmlInputElement));
                PrepareInputMockById(PkoProvider.ADDRESS_2_FIELD, provider.ReplaceSpecialChars(paymentInfo.PostalCodeAndCity), typeof(FakeHtmlInputElement));
            }

            String[] title = Util.SplitString(provider.ReplaceSpecialChars(paymentInfo.Title), 35, PkoProvider.TITLE_FIELD.Length);
            for (int i = 0; i < title.Length; i++) {
                PrepareInputMockById(PkoProvider.TITLE_FIELD[i], title[i], typeof(FakeHtmlInputElement));
            }
            PrepareInputMockById(PkoProvider.AMOUNT_1_FIELD, paymentInfo.AmountToPayDecimal, typeof(FakeHtmlInputElement));
            PrepareInputMockById(PkoProvider.AMOUNT_2_FIELD, paymentInfo.AmountToPayFloating, typeof(FakeHtmlInputElement));
            PrepareInputMockById(PkoProvider.DATE_FIELD, paymentInfo.DueDateTimeToPaste.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), typeof(FakeHtmlInputElement));
        }

        private void PrepareGoToTransferPageMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock window = AddElementMock(typeof(IHTMLWindow2));
            documentMock.ExpectAndReturn("get_parentWindow", window.MockInstance);

            if (paymentInfo.IsDefinedTransfer == false) {
                window.Expect("execScript", "clickMenu('pay_transfer_normal')", "JScript");
            } else {
                window.Expect("execScript", "clickMenu('pay_payment_list')", "JScript");
            }
        }

        private void PrepareDefinedTransferPageMocks(bool result, bool repeat, bool transferNamesMatch) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            DynamicMock elements = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", elements.MockInstance, "h1");

            ArrayList list = new ArrayList();
            DynamicMock h1 = AddElementMock(typeof(IHTMLElement));
            h1.ExpectAndReturn("get_innerHTML", "anything");
            list.Add(h1.MockInstance);

            if (result == false) {
                elements.ExpectAndReturn("GetEnumerator", list.GetEnumerator());
                return;
            }

            h1 = AddElementMock(typeof(IHTMLElement));
            h1.ExpectAndReturn("get_innerHTML", "Lista&nbsp;P³atnoœci");
            list.Add(h1.MockInstance);
            elements.ExpectAndReturn("GetEnumerator", list.GetEnumerator());

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            DynamicMock trElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", trElems.MockInstance, "tr");
            ArrayList trList = new ArrayList();
            DynamicMock tr = AddElementMock(typeof(IHTMLElement));
            tr.ExpectAndReturn("get_innerHTML", "some text");
            trList.Add(tr.MockInstance);

            tr = AddElementMock(typeof(IHTMLElement));
            tr.ExpectAndReturn("get_innerHTML", "<th>Nazwa P³atnoœci</th><th>Dane Odbiorcy</th><th>Numer Rachunku</th><th>Tytu³</th>");
            trList.Add(tr.MockInstance);

            tr = AddElementMock(typeof(IHTMLElement));
            trList.Add(tr.MockInstance);

            AddDefinedTransferMocks(trList, "XXX" + paymentInfo.DefinedTransferName + "111", false);
            AddDefinedTransferMocks(trList, paymentInfo.DefinedTransferName, transferNamesMatch);
            if (repeat == true) {
                AddDefinedTransferMocks(trList, paymentInfo.DefinedTransferName, false);
            }
            trElems.ExpectAndReturn("GetEnumerator", trList.GetEnumerator());
        }

        private void AddDefinedTransferMocks(ArrayList trList, String definedTransfer, bool toClick) {
            DynamicMock tr = AddElementMock(typeof(IHTMLElement));
            trList.Add(tr.MockInstance);
            DynamicMock tdElems = AddElementMock(typeof(IHTMLElementCollection));
            tr.ExpectAndReturn("get_all", tdElems.MockInstance);

            ArrayList tdList = new ArrayList();
            DynamicMock td = AddElementMock(typeof(IHTMLElement));
            td.ExpectAndReturn("get_innerHTML", definedTransfer);
            tdList.Add(td.MockInstance);

            if (toClick == true) {
                for (int i = 0; i < 4; i++) {
                    tdList.Add(AddElementMock(typeof(FakeHtmlTableCellElement)).MockInstance);
                }

                DynamicMock transferTable = AddElementMock(typeof(IHTMLElementCollection));
                td = AddElementMock(typeof(IHTMLElement));
                td.ExpectAndReturn("get_all", transferTable.MockInstance);
                tdList.Add(td.MockInstance);

                ArrayList transferTableList = new ArrayList();
                transferTableList.Add(AddElementMock(typeof(IHTMLElement)).MockInstance);
                DynamicMock anchor = AddElementMock(typeof(FakeHtmlAnchorElement));
                anchor.ExpectAndReturn("get_innerHTML", "Usuñ");
                transferTableList.Add(anchor.MockInstance);

                anchor = AddElementMock(typeof(FakeHtmlAnchorElement));
                anchor.ExpectAndReturn("get_innerHTML", "ZAP£Aæ");
                anchor.Expect("click");
                transferTableList.Add(anchor.MockInstance);

                transferTable.ExpectAndReturn("GetEnumerator", transferTableList.GetEnumerator());
            }
            tdElems.ExpectAndReturn("GetEnumerator", tdList.GetEnumerator());
        }

        private void PrepareReadyToPasteMocks(bool result) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            if (result == false) {
                documentMock.ExpectAndReturn("getElementById", null, "docheader");
                return;
            }
            DynamicMock element = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", element.MockInstance, "docheader");

            if (paymentInfo.IsDefinedTransfer == false) {
                element.ExpectAndReturn("get_innerHTML", "<h1>Przelew Jednorazowy</h1>");
            } else {
                element.ExpectAndReturn("get_innerHTML", "<h1>Realizacja P³atnoœci</h1>");
            }
            element = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", element.MockInstance, PkoProvider.TITLE_FIELD[0]);
            element.ExpectAndReturn("getAttribute", "text", "type", 1);
        }

        private void PrepareLoggedUserMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("getElementById", new DynamicMock(typeof(IHTMLElement)).MockInstance, "inteligomenu");
            documentMock.ExpectAndReturn("getElementById", new DynamicMock(typeof(IHTMLElement)).MockInstance, "leftmenucontener");
        }
        #endregion
    }
}
