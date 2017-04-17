using System;
using IE = Interop.SHDocVw;
using NUnit.Framework;
using NUnit.Mocks;
using Billbank.IEPlugin;
using Billbank.IEPlugin.Domain;
using Billbank.IEPlugin.Provider;

namespace IEPluginTests.Provider {
    [TestFixture]
    class AbstractProviderTest {

        private IE.WebBrowser webBrowser;
        private PaymentInfo paymentInfo;

        [SetUp]
        public void SetUp() {
            GlobalData.Instance.TestMode = true;
            Settings.Default.ReloadCount = 0;

            DynamicMock mock = new DynamicMock(typeof(IE.WebBrowser));
            webBrowser = mock.MockInstance as IE.WebBrowser;

            paymentInfo = new PaymentInfo();
            paymentInfo.BankAccountNo = "AB01923837465";
            paymentInfo.State = State.IDLE;
        }

        [Test]
        public void ShouldOpenProvidersListPage() {
            DummyNotLoggedProvider provider = new DummyNotLoggedProvider();
            paymentInfo.State = State.IDLE;

            DynamicMock webBrowserMock = new DynamicMock(typeof(IE.WebBrowser));
            webBrowserMock.ExpectAndReturn("get_LocationURL", Settings.Default.ChoseBankLink + "yyy");
            webBrowserMock.Expect("Navigate2", Settings.Default.ChoseBankLink + Settings.Default.PluginVersion, 0x2, "_self", null, null);

            PaymentRequest request = new PaymentRequest(webBrowserMock.MockInstance as IE.WebBrowser, paymentInfo, true);
            PaymentInfo result = provider.Execute(request);
            Assert.AreSame(paymentInfo, result);

            webBrowserMock.Verify();
        }

        [Test]
        public void ShouldExecuteInProgressConfirm() {
            DummyLoggedProvider provider = new DummyLoggedProvider();
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;

            PaymentRequest request = new PaymentRequest(webBrowser, paymentInfo);
            PaymentInfo result = provider.Execute(request);
            Assert.AreSame(result, request.PaymentInfo);
            Assert.AreEqual("101.00", result.Amount);
            Assert.AreEqual("Test Biller2", result.BillerName);
            Assert.AreEqual("67890", result.PostalCode);
            Assert.AreEqual(State.PAID, result.State);

            paymentInfo.State = State.IN_PROGRESS_CONFIRM;

            provider.cancel = true;

            result = provider.Execute(request);
            Assert.AreSame(result, request.PaymentInfo);
            Assert.AreEqual("101.00", result.Amount);
            Assert.AreEqual("Test Biller2", result.BillerName);
            Assert.AreEqual("67890", result.PostalCode);
            Assert.AreEqual(State.INVALID, result.State);
        }

        [Test]
        public void ShouldExecuteWithInvalidInfo() {
            AbstractProvider provider = new DummyLoggedProvider();
            paymentInfo.State = State.INVALID;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PaymentInfo result = provider.Execute(new PaymentRequest(webBrowser, paymentInfo));

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldExecuteWithPaidInfo() {
            AbstractProvider provider = new DummyLoggedProvider();
            paymentInfo.State = State.PAID;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PaymentInfo result = provider.Execute(new PaymentRequest(webBrowser, paymentInfo));

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldExecuteIdle() {
            AbstractProvider provider = new DummyLoggedProvider();
            PaymentRequest request = new PaymentRequest(webBrowser, paymentInfo);
            PaymentInfo result = provider.Execute(request);
            Assert.AreSame(result, request.PaymentInfo);
            Assert.AreEqual("100.00", result.Amount);
            Assert.AreEqual("Test Biller", result.BillerName);
            Assert.AreEqual("Transfer", result.DefinedTransferName);
            Assert.AreEqual("12345", result.PostalCode);
            Assert.AreEqual(State.IN_PROGRESS, result.State);
        }

        private void ExecuteInProgress(bool changeAccountNo, String definedTransfer, State state) {
            DummyLoggedProvider provider = new DummyLoggedProvider();
            ShouldExecuteIdle();

            provider.changeAccountNo = changeAccountNo;
            if (definedTransfer.Length == 0) {
                paymentInfo.DefinedTransferName = definedTransfer;
            }
            PaymentRequest request = new PaymentRequest(webBrowser, paymentInfo);
            PaymentInfo result = provider.Execute(request);

            Assert.AreEqual("200.00", result.Amount);
            Assert.AreEqual("Test Biller1", result.BillerName);
            Assert.AreEqual(definedTransfer, result.DefinedTransferName);
            Assert.AreEqual("12345", result.PostalCode);
            Assert.AreEqual(state, result.State);
        }

        [Test]
        public void ShouldExecuteInProgressAnyTransferCorrectBankAccountNo() {
            ExecuteInProgress(false, "", State.IN_PROGRESS_CONFIRM);
        }

        [Test]
        public void ShouldExecuteInProgressAnyTransferIncorrectBankAccountNo() {
            ExecuteInProgress(true, "", State.INVALID);
        }


        [Test]
        public void ShouldExecuteInProgressDefinedTransferCorrectBankAccountNo() {
            ExecuteInProgress(false, "Transfer", State.IN_PROGRESS_CONFIRM);
        }

        [Test]
        public void ShouldExecuteInProgressDefinedTransferIncorrectBankAccountNo() {
            ExecuteInProgress(true, "Transfer", State.IN_PROGRESS_CONFIRM);
        }

        [Test]
        public void ShouldIgnoreExecutionAsUserNotLogged() {
            AbstractProvider provider = new DummyNotLoggedProvider();

            PaymentInfo result = provider.Execute(new PaymentRequest(webBrowser, paymentInfo));
            Assert.AreEqual(result.ToString(), paymentInfo.ToString());
        }

        [Test]
        public void ShouldIgnoreExecutionAsMaxReloadReached() {
            AbstractProvider provider = new DummyLoggedProvider();
            Settings.Default.ReloadCount = Settings.Default.MaxReload + 1;

            PaymentInfo result = provider.Execute(new PaymentRequest(webBrowser, paymentInfo));
            Assert.AreEqual(result.ToString(), paymentInfo.ToString());
        }

        /// <summary>
        /// Auxiliary test classes
        /// </summary>
        class DummyLoggedProvider : AbstractProvider {

            internal bool changeAccountNo;
            internal bool cancel;

            protected override PaymentInfo ProcessIdle(PaymentRequest request) {
                request.PaymentInfo.Amount = "100.00";
                request.PaymentInfo.BillerName = "Test Biller";
                request.PaymentInfo.DefinedTransferName = "Transfer";
                request.PaymentInfo.PostalCode = "12345";
                request.PaymentInfo.State = State.IN_PROGRESS;
                return request.PaymentInfo;
            }

            protected override PaymentInfo ProcessInProgress(PaymentRequest request) {
                request.PaymentInfo.Amount = "200.00";
                request.PaymentInfo.BillerName = "Test Biller1";
                request.PaymentInfo.PostalCode = "12345";
                if (changeAccountNo == true) {
                    request.PaymentInfo.BankAccountNo = "123049586748930";
                }
                request.PaymentInfo.State = State.IN_PROGRESS_CONFIRM;
                return request.PaymentInfo;
            }

            protected override PaymentInfo ProcessInProgressConfirm(PaymentRequest request) {
                request.PaymentInfo.Amount = "101.00";
                request.PaymentInfo.BillerName = "Test Biller2";
                request.PaymentInfo.PostalCode = "67890";
                if (changeAccountNo == true) {
                    request.PaymentInfo.BankAccountNo = "123049586748930";
                }

                if (cancel == true) {
                    request.PaymentInfo.State = State.INVALID;
                } else {
                    request.PaymentInfo.State = State.PAID;
                }
                return request.PaymentInfo;
            }

            protected override bool IsUserLogged(PaymentRequest request) {
                return true;
            }

            protected override void GoToTransferPage(PaymentRequest request) {
                throw new Exception("The method or operation is not implemented.");
            }

            protected override bool ReadyToPaste(PaymentRequest request) {
                throw new Exception("The method or operation is not implemented.");
            }

            protected override void PastePaymentInfo(PaymentRequest request) {
                throw new Exception("The method or operation is not implemented.");
            }

            protected override bool IsConfirmationPage(PaymentRequest request) {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        class DummyNotLoggedProvider : DummyLoggedProvider {
            protected override bool IsUserLogged(PaymentRequest request) {
                return false;
            }
        }
    }
}
