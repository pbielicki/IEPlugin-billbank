using System;
using System.Collections;
using IE = Interop.SHDocVw;
using mshtml;
using NUnit.Framework;
using NUnit.Mocks;

using IEPluginTests.Html;
using Billbank.IEPlugin;
using Billbank.IEPlugin.Domain;
using Billbank.IEPlugin.Provider;

namespace IEPluginTests.Provider {
    [TestFixture]
    class MBankProviderTest : BaseProviderTest {

        #region Tests for paste payment info
        [Test]
        public void ShouldPastePaymentInfoForAnyTransfer() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);
            PreparePastePaymentInfoMocks();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldPastePaymentInfoForDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocksForDefinedTransfer("<span>" + paymentInfo.DefinedTransferName + "</span>");
            PreparePastePaymentInfoMocks();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Test for IDLE state
        [Test]
        public void ShouldGoToAccountsListAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareDefinedTransfersPageMocks(false);
            PrepareAccountsListPageMocks(false);
            PrepareAnyAccountSelectedMock(false);
            PrepareAccountsListPageMocks(false);
            PrepareShouldOpenAccountsListTrueMocks();
            documentMock.Expect("set_url", MBankProvider.BASE_URL + MBankProvider.ACCOUNTS_LIST);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStatusAsInvalidAfterIdleDefinedTransferNotExists() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareDefinedTransfersPageMocks(true);
            PrepareSelectDefinedTransferAndGoToTransferPage(0);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToDefinedTransferAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareDefinedTransfersPageMocks(true);
            PrepareSelectDefinedTransferAndGoToTransferPage(2);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferAfterIdleDefinedTransferNotExists() {
            GlobalData.Instance.TestQuestionAnswer = true; 
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareDefinedTransfersPageMocks(true);
            PrepareSelectDefinedTransferAndGoToTransferPage(0);
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
            PrepareReadyToPasteFalseMocks();
            PrepareAccountsListPageMocks(false);
            PrepareAnyAccountSelectedMock(true);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToDefinedTransferListAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareDefinedTransfersPageMocks(false);
            PrepareAccountsListPageMocks(false);
            PrepareAnyAccountSelectedMock(true);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterIdleNoSupportedAccounts() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareDefinedTransfersPageMocks(false);
            PrepareAccountsListPageMocks(true);
            PrepareSelectAccountAndGoToTransferPageMocks(0);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterIdleAccountNotSelected() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareDefinedTransfersPageMocks(false);
            PrepareAccountsListPageMocks(true);
            PrepareSelectAccountAndGoToTransferPageMocks(2);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSelectSupportedAccountAfterIdleForAnyTransfer() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareAccountsListPageMocks(true);
            PrepareSelectAccountAndGoToTransferPageMocks(1);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSelectSupportedAccountAfterIdleForDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareDefinedTransfersPageMocks(false);
            PrepareAccountsListPageMocks(true);
            PrepareSelectAccountAndGoToTransferPageMocks(1);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IN_PROGRESS state
        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgressForDefinedTransferAccountNoMismatch() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareIsConfirmPageTrueMocks();
            PrepareConfirmPage("10,12", "ABCDE123409999");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS_CONFIRM;
            copy.BankAccountNo = "ABCDE123409999";
            copy.AmountPaid = "10,12";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgressForAnyTransferWrongAccountNo() {
            paymentInfo.State = State.IN_PROGRESS;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareIsConfirmPageTrueMocks();
            PrepareConfirmPage("1 230,00", "ABCDE123409999");

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            copy.BankAccountNo = "ABCDE123409999";
            copy.AmountPaid = "1230,00";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgressForDefinedTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareIsConfirmPageTrueMocks();
            PrepareConfirmPage(paymentInfo.AmountToPay, paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS_CONFIRM;
            copy.AmountPaid = copy.AmountToPay;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgressForAnyTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareIsConfirmPageTrueMocks();
            PrepareConfirmPage(paymentInfo.AmountToPay, paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS_CONFIRM;
            copy.AmountPaid = copy.AmountToPay;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgress() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteFalseMocks();
            PrepareIsConfirmPageFalseMocks();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        
        [Test]
        public void ShouldDoNothingAfterInProgressDefinedTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocksForDefinedTransfer("<span>" + paymentInfo.DefinedTransferName + "</span>");

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldDoNothingAfterInProgressAnyTransfer() {
            paymentInfo.State = State.IN_PROGRESS;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IN_PROGRESS_CONFIRM state
        [Test]
        public void ShouldSetPaymentStateAsPaidAfterInProgressConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareInProgressConfirmMocks("<p>Dyspozycja przelewu zosta³a przyjêta.</p>", false);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgressConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareInProgressConfirmMocks(null, true);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        [Test]
        public void ShouldReplaceSpecialChars() {
            Assert.AreEqual("t e()'st", provider.ReplaceSpecialChars("t|e<>\"st"));
            Assert.AreEqual("!@#$%^&*t()_+}e{ \'s:?)(t", provider.ReplaceSpecialChars("!@#$%^&*t()_+}e{|\"s:?><t"));
        }

        #region Test Fixture
        protected override AbstractProvider CreateProvider() {
            return new MBankProvider();
        }

        private void PrepareShouldOpenAccountsListTrueMocks() {
            DynamicMock frames = AddElementMock(typeof(FramesCollection));
            documentMock.ExpectAndReturn("get_frames", frames.MockInstance);
            documentMock.ExpectAndReturn("get_frames", frames.MockInstance);
            documentMock.ExpectAndReturn("get_frames", frames.MockInstance);
            documentMock.ExpectAndReturn("get_frames", frames.MockInstance);
            frames.ExpectAndReturn("get_length", 1);
            frames.ExpectAndReturn("get_length", 1);

            documentMock.ExpectAndReturn("get_url", "someUrl" + MBankProvider.ASPX);
            documentMock.ExpectAndReturn("get_url", "someUrl" + MBankProvider.ASPX);
            documentMock.ExpectAndReturn("get_url", "someUrl" + MBankProvider.ASPX);
        }

        private void PrepareSelectDefinedTransferAndGoToTransferPage(int count) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock transfersGrid = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", transfersGrid.MockInstance, "BaseDefinedTransfersList");

            PrepareGetIdListMocks(count, transfersGrid, "BaseDefinedTransfersList_grid_ctl_");

            for (int i = 0; i < count + 1; i++) {
                DynamicMock transfer = AddElementMock(typeof(IHTMLElement));
                documentMock.ExpectAndReturn("getElementById", transfer.MockInstance, "BaseDefinedTransfersList_grid_ctl_" + i + "__");
                switch (i) {
                    case 0:
                        transfer.ExpectAndReturn("get_innerHTML", "123" + paymentInfo.DefinedTransferName);
                        break;
                    default:
                        transfer.ExpectAndReturn("get_innerHTML", paymentInfo.DefinedTransferName);
                        break;
                }
            }

            if (count > 0) {
                DynamicMock transfer = AddElementMock(typeof(IHTMLElement));
                transfer.Expect("click");
                documentMock.ExpectAndReturn("getElementById", transfer.MockInstance, "BaseDefinedTransfersList_grid_ctl_1_4_exec");
            }
        }

        private void PrepareGoToTransferPageMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock window = AddElementMock(typeof(IHTMLWindow2));
            documentMock.ExpectAndReturn("get_parentWindow", window.MockInstance);
            if (paymentInfo.IsDefinedTransfer == false) {
                window.Expect("execScript", MBankProvider.ANY_TRANSFER_SCRIPT, "JScript");
            } else {
                window.Expect("execScript", MBankProvider.DEFINED_TRANSFERS_SCRIPT, "JScript");
            }
        }

        private void PrepareAnyAccountSelectedMock(bool result) {
            if (result == false) {
                documentMock.ExpectAndReturn("getElementById", null, "contextInfo");
                return;
            }
            DynamicMock context = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", context.MockInstance, "contextInfo");
            String html = MBankProvider.SPAN_BEGIN + MBankProvider.SPAN_END 
                        + MBankProvider.SPAN_BEGIN + MBankProvider.SUPPORTED_ACCOUNTS[0] + MBankProvider.SPAN_END;
            context.ExpectAndReturn("get_innerHTML", html);
            context.ExpectAndReturn("get_innerHTML", html);
            context.ExpectAndReturn("get_innerHTML", html);
        }

        private void PrepareAccountsListPageMocks(bool result) {
            if (result == false) {
                DynamicMock frames = AddElementMock(typeof(FramesCollection));
                documentMock.ExpectAndReturn("get_frames", frames.MockInstance);
                documentMock.ExpectAndReturn("get_frames", frames.MockInstance);
                frames.ExpectAndReturn("get_length", 2);
            } else {
                documentMock.ExpectAndReturn("get_frames", null);
                documentMock.ExpectAndReturn("get_frames", null);
                documentMock.ExpectAndReturn("get_url", "someFakeUrl" + MBankProvider.ACCOUNTS_LIST);
            }
        }

        private void PrepareDefinedTransfersPageMocks(bool result) {
            if (result == false) {
                DynamicMock frames = AddElementMock(typeof(FramesCollection));
                documentMock.ExpectAndReturn("get_frames", frames.MockInstance);
                documentMock.ExpectAndReturn("get_frames", frames.MockInstance);
                frames.ExpectAndReturn("get_length", 2);
            } else {
                documentMock.ExpectAndReturn("get_frames", null);
                documentMock.ExpectAndReturn("get_frames", null);
                documentMock.ExpectAndReturn("get_url", "someFakeUrl" + MBankProvider.DEFINED_TRANSFERS_LIST);
            }
        }

        private void PrepareSelectAccountAndGoToTransferPageMocks(int count) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock accountsGrid = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", accountsGrid.MockInstance, "AccountsGrid");

            PrepareGetIdListMocks(count, accountsGrid, "AccountsGrid_grid_ctl_");

            for (int i = 0; i < count + 1; i++) {
                DynamicMock account = AddElementMock(typeof(IHTMLElement));
                documentMock.ExpectAndReturn("getElementById", account.MockInstance, "AccountsGrid_grid_ctl_" + i + "__");
                switch (i) {
                    case 0:
                        account.ExpectAndReturn("get_innerHTML", "eMAX Plus");
                        break;
                    default:
                        account.ExpectAndReturn("get_innerHTML", MBankProvider.SUPPORTED_ACCOUNTS[i - 1]);
                        break;
                }
            }

            if (count == 1) {
                DynamicMock account = AddElementMock(typeof(IHTMLElement));
                account.Expect("click");
                if (paymentInfo.IsDefinedTransfer == false) {
                    documentMock.ExpectAndReturn("getElementById", account.MockInstance, "AccountsGrid_grid_ctl_1_3_A0");
                } else {
                    documentMock.ExpectAndReturn("getElementById", account.MockInstance, "AccountsGrid_grid_ctl_1_3_A2");
                }
            }
        }

        private void PrepareGetIdListMocks(int count, DynamicMock grid, String prefix) {
            ArrayList list = new ArrayList();
            DynamicMock element = AddElementMock(typeof(FakeHtmlAnchorElement));
            element.ExpectAndReturn("get_id", prefix + "0__");
            element.ExpectAndReturn("get_id", prefix + "0__");
            element.ExpectAndReturn("get_id", prefix + "0__");
            list.Add(element.MockInstance);

            for (int i = 0; i < count; i++) {
                element = AddElementMock(typeof(FakeHtmlAnchorElement));
                element.ExpectAndReturn("get_id", prefix + (i + 1) + "__");
                element.ExpectAndReturn("get_id", prefix + (i + 1) + "__");
                element.ExpectAndReturn("get_id", prefix + (i + 1) + "__");
                list.Add(element.MockInstance);
            }

            DynamicMock elements = AddElementMock(typeof(IHTMLElementCollection));
            grid.ExpectAndReturn("get_all", elements.MockInstance);
            elements.ExpectAndReturn("GetEnumerator", list.GetEnumerator());
        }

        private void PreparePastePaymentInfoMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            if (paymentInfo.IsDefinedTransfer == false) {
                PrepareInputMockById(MBankProvider.ACCOUNT_FIELD, paymentInfo.BankAccountNo, typeof(FakeHtmlInputElement));
                PrepareInputMockById(MBankProvider.TITLE_FIELD, provider.ReplaceSpecialChars(paymentInfo.Title), typeof(FakeHtmlTextAreaElement));
                PrepareInputMockById(MBankProvider.NAME_FIELD, provider.ReplaceSpecialChars(paymentInfo.BillerName), typeof(FakeHtmlInputElement));
                PrepareInputMockById(MBankProvider.ADDRESS_FIELD, provider.ReplaceSpecialChars(paymentInfo.Street), typeof(FakeHtmlInputElement));
                PrepareInputMockById(MBankProvider.CITY_FIELD, provider.ReplaceSpecialChars(paymentInfo.PostalCodeAndCity), typeof(FakeHtmlInputElement));
            } else {
                PrepareEmptyInputMockById(MBankProvider.ACCOUNT_FIELD);
                PrepareInputMockById(MBankProvider.TITLE_FIELD, provider.ReplaceSpecialChars(paymentInfo.Title), typeof(FakeHtmlTextAreaElement));
                PrepareEmptyInputMockById(MBankProvider.NAME_FIELD);
                PrepareEmptyInputMockById(MBankProvider.ADDRESS_FIELD);
                PrepareEmptyInputMockById(MBankProvider.CITY_FIELD);
            }

            PrepareInputMockById(MBankProvider.DAY_FIELD, paymentInfo.DueDateTimeToPaste.Day.ToString(), typeof(FakeHtmlInputElement));
            PrepareInputMockById(MBankProvider.MONTH_FIELD, paymentInfo.DueDateTimeToPaste.Month.ToString(), typeof(FakeHtmlInputElement));
            PrepareInputMockById(MBankProvider.YEAR_FIELD, paymentInfo.DueDateTimeToPaste.Year.ToString(), typeof(FakeHtmlInputElement));
            PrepareInputMockById(MBankProvider.AMOUNT_FIELD, paymentInfo.AmountToPay.Replace(".", ","), typeof(FakeHtmlInputElement));
        }

        private void PrepareConfirmPage(String enteredAmount, String enteredAccountNo) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock elementMock = AddElementMock(typeof(IHTMLElement));

            if (paymentInfo.IsDefinedTransfer == false) {
                documentMock.ExpectAndReturn("getElementById", elementMock.MockInstance, MBankProvider.TRANSFER_EXEC_DIV);
            } else {
                documentMock.ExpectAndReturn("getElementById", null, MBankProvider.TRANSFER_EXEC_DIV);
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
                documentMock.ExpectAndReturn("getElementById", elementMock.MockInstance, MBankProvider.DEFINED_TRANSFER_EXEC_DIV);
            }

            elementMock.ExpectAndReturn("get_innerHTML", "<span class=\"text amount\">" + enteredAmount + "</span>");
            ArrayList divChildren = new ArrayList();
            DynamicMock element = AddElementMock(typeof(IHTMLElement));
            element.ExpectAndReturn("get_innerHTML", "nothing");
            divChildren.Add(element.MockInstance);

            element = AddElementMock(typeof(IHTMLElement));
            element.ExpectAndReturn("get_innerHTML", "Rachunek odbiorcy");
            divChildren.Add(element.MockInstance);
            
            element = new DynamicMock(typeof(IHTMLElement));
            element.ExpectAndReturn("get_innerHTML", "nothing");
            divChildren.Add(element.MockInstance);
            
            element = new DynamicMock(typeof(IHTMLElement));
            element.ExpectAndReturn("get_innerHTML", enteredAccountNo);
            divChildren.Add(element.MockInstance);

            DynamicMock collection = AddElementMock(typeof(IHTMLElementCollection));
            collection.ExpectAndReturn("GetEnumerator", divChildren.GetEnumerator());
            elementMock.ExpectAndReturn("get_all", collection.MockInstance);
        }
        
        private void PrepareIsConfirmPageTrueMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock element = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", element.MockInstance, MBankProvider.MODIFY_BUTTON);
            documentMock.ExpectAndReturn("getElementById", element.MockInstance, MBankProvider.CONFIRM_BUTTON);
        }

        private void PrepareIsConfirmPageFalseMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("getElementById", null, MBankProvider.MODIFY_BUTTON);
        }

        private void PrepareReadyToPasteFalseMocks() {
            PrepareLoggedUserMocks();

            if (paymentInfo.State == State.IDLE) {
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            }

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("getElementById", null, MBankProvider.AMOUNT_FIELD);
            documentMock.ExpectAndReturn("get_url", "SomeText");
        }

        private void PrepareReadyToPasteMocksForDefinedTransfer(String mockTransferName) {
            PrepareReadyToPasteMocks(true);

            DynamicMock elementMock = this.elementMock[this.elementMock.Count - 1];
            elementMock.ExpectAndReturn("getAttribute", "hidden", "type", 1);
            documentMock.ExpectAndReturn("getElementById", elementMock.MockInstance, MBankProvider.DEFINED_TRANSFER_EXEC_DIV);
            elementMock.ExpectAndReturn("get_innerHTML", mockTransferName);
            elementMock.ExpectAndReturn("get_innerHTML", mockTransferName);
            elementMock.ExpectAndReturn("get_innerHTML", mockTransferName);
        }

        private void PrepareReadyToPasteMocks(bool mockDefinedTransferPage) {
            PrepareLoggedUserMocks();

            if (paymentInfo.State == State.IDLE) {
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            }

            DynamicMock elementMock = AddElementMock(typeof(IHTMLElement));
            elementMock.ExpectAndReturn("getAttribute", "text", "type", 1);

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("getElementById", elementMock.MockInstance, MBankProvider.AMOUNT_FIELD);
            documentMock.ExpectAndReturn("get_url", "SomeText/" + MBankProvider.ASPX);
            if (mockDefinedTransferPage == true) {
                documentMock.ExpectAndReturn("getElementById", elementMock.MockInstance, MBankProvider.DEFINED_ACCOUNT_FIELD);
            } else {
                documentMock.ExpectAndReturn("getElementById", null, MBankProvider.DEFINED_ACCOUNT_FIELD);
            }                
        }

        private void PrepareInProgressConfirmMocks(String innerHtml, bool nullDiv) {
            PrepareLoggedUserMocks();
            DynamicMock elementMock = AddElementMock(typeof(IHTMLElement));
            if (nullDiv == true) {
                documentMock.ExpectAndReturn("getElementById", null, "msg");
            } else {
                elementMock.ExpectAndReturn("get_innerHTML", innerHtml);
                documentMock.ExpectAndReturn("getElementById", elementMock.MockInstance, "msg");
            }
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
        }

        private void PrepareLoggedUserMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("get_url", MBankProvider.BASE_URL + "/");
        }
        #endregion
    }
}
