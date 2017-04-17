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
    class MilleniumProviderTest : BaseProviderTest {

        private String url;

        [SetUp]
        public override void SetUp() {
            url = null;
            base.SetUp();
        }

        #region Tests for IN_PROGRESS_CONFIRM state
        [Test]
        public void ShouldDoNothingAfterInProgessConfirmForConfirmCodePage() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareLoggedUserMocks();
            PrepareConfirmPageMocks(false, null, null);
            PrepareCodeConfirmPageMocks(true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldDoNothingAfterInProgessConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareLoggedUserMocks();
            PrepareConfirmPageMocks(true, null, null, false);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgessConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareLoggedUserMocks();
            PrepareConfirmPageMocks(false, null, null);
            PrepareCodeConfirmPageMocks(false);
            PrepareConfirmationPageMocks(false);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsPaidAfterInProgessConfirm() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareLoggedUserMocks();
            PrepareConfirmPageMocks(false, null, null);
            PrepareCodeConfirmPageMocks(false);
            PrepareConfirmationPageMocks(true, 1);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsPaidAfterInProgessConfirmForSecondMessage() {
            paymentInfo.State = State.IN_PROGRESS_CONFIRM;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareLoggedUserMocks();
            PrepareConfirmPageMocks(false, null, null);
            PrepareCodeConfirmPageMocks(false);
            PrepareConfirmationPageMocks(true, 2);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.PAID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IN_PROGRESS state
        [Test]
        public void ShouldDoNothingAfterInProgess() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true, true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgess() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false, false);
            PrepareConfirmPageMocks(true, paymentInfo.AmountToPay, paymentInfo.BankAccountNo);

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = paymentInfo.AmountToPay;
            copy.State = State.IN_PROGRESS_CONFIRM;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgess() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false, false);
            PrepareConfirmPageMocks(false, null, null);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidAfterInProgessWrongAccountNo() {
            paymentInfo.DefinedTransferName = "";
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false, false);
            PrepareConfirmPageMocks(true, "23,45", "ABCl30980803");

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "23,45";
            copy.BankAccountNo = "ABCl30980803";
            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInProgressConfirmAfterInProgessAccountNoMismatch() {
            paymentInfo.State = State.IN_PROGRESS;
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false, false);
            PrepareConfirmPageMocks(true, "1 000.23", "ABCl30980803");

            PaymentInfo result = provider.Execute(request);

            copy.AmountPaid = "1000.23";
            copy.BankAccountNo = "ABCl30980803";
            copy.State = State.IN_PROGRESS_CONFIRM;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        #endregion

        #region Tests for paste payment information
        [Test]
        public void ShouldPastePaymentInfoForDefinedTransfer() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true, true);
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
            PrepareReadyToPasteMocks(true, true);
            PreparePastePaymentInfoMocks();

            PaymentInfo result = provider.Execute(request);

            copy.State = State.IN_PROGRESS;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        #region Tests for IDLE state
        [Test]
        public void ShouldGoToAnyTransferAfterIdleAlreadyOnTransferPage() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());

            url = "http://TRDomestic.qz";

            PrepareReadyToPasteMocks(false, false);
            PrepareGoToTransferPageMocks(true);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSetPaymentStateAsInvalidOnTransferPageAfterNoDefinedTransferFound() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false, false);
            PrepareDefinedTransfersPageMocks(true, 0);

            PaymentInfo result = provider.Execute(request);

            copy.State = State.INVALID;
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferOnTransferPageAfterNoDefinedTransferFound() {
            GlobalData.Instance.TestQuestionAnswer = true;

            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            url = "http://osobiste/.qz";
            request = new PaymentRequest(request.WebBrowser, request.PaymentInfo, url);
            PrepareReadyToPasteMocks(false, false);
            PrepareDefinedTransfersPageMocks(true, 0);

            paymentInfo.DefinedTransferName = "";
            PrepareGoToTransferPageMocks();
            paymentInfo.DefinedTransferName = copy.DefinedTransferName;

            PaymentInfo result = provider.Execute(request);

            copy.DefinedTransferName = "";
            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSelectDefinedTransferOnTransferPageAfterIdle1() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false, false);
            PrepareDefinedTransfersPageMocks(true, 1);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSelectDefinedTransferOnTransferPageAfterIdleSomeTransferAlreadySelected() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true, false);
            PrepareDefinedTransfersPageMocks(true, 3);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldSelectDefinedTransferOnTransferPageAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false, false);
            PrepareDefinedTransfersPageMocks(true, 2);

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToDefinedTransferAfterIdle() {
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            url = "http://firmy/.qz";
            request = new PaymentRequest(request.WebBrowser, request.PaymentInfo, url);
            PrepareReadyToPasteMocks(false, false);
            PrepareDefinedTransfersPageMocks(false, -1);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferAfterIdleDefinedTransferAlreadyChosen() {
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            url = "http://demofirmy/.qz";
            request = new PaymentRequest(request.WebBrowser, request.PaymentInfo, url);
            PrepareReadyToPasteMocks(false, false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferAfterIdleSomeTransferAlreadySelected() {
            url = String.Empty;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(true, false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }

        [Test]
        public void ShouldGoToAnyTransferAfterIdle() {
            url = String.Empty;
            paymentInfo.DefinedTransferName = "";
            PaymentInfo copy = PaymentInfo.ValueOf(paymentInfo.ToString());
            PrepareReadyToPasteMocks(false, false);
            PrepareGoToTransferPageMocks();

            PaymentInfo result = provider.Execute(request);

            Assert.AreNotSame(copy, result);
            Assert.AreEqual(copy.ToString(), result.ToString());
        }
        #endregion

        [Test]
        public void ShouldReplaceSpecialChars() {
            Assert.AreEqual("t            ()()   ()  est", provider.ReplaceSpecialChars("t`~!@#$%^&*_={}[];\"'<>|\\est"));
        }

        #region Test Fixture
        protected override AbstractProvider CreateProvider() {
            return new MilleniumProvider();
        }

        private void PrepareConfirmationPageMocks(bool result, params int[] variant) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock canvas = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", canvas.MockInstance, "canvas");
            if (result == false) {
                canvas.ExpectAndReturn("get_innerHTML", "some text");
                return;
            }

            if (variant[0] == 1) {
                canvas.ExpectAndReturn("get_innerHTML", "<p>some html</p><div>Dyspozycja Przelewu zosta³a przyjêta i zostanie wykonana<br>Zgodnie z zasadami Obowi¹zuj¹cymi W Banku.</div>");
            } else {
                canvas.ExpectAndReturn("get_innerHTML", "<p>some html</p><div>Zlecenie&nbsp;zosta³o Wys³ane.</div>");
            }
        }

        private void PrepareCodeConfirmPageMocks(bool result) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock codes = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByName", codes.MockInstance, "Code");
            if (result == false) {
                codes.ExpectAndReturn("get_length", 0);
            } else {
                codes.ExpectAndReturn("get_length", 1);
            }
        }

        private void PrepareConfirmPageMocks(bool result, string amountPaid, string enteredAccountNo, params bool[] extended) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            if (result == false) {
                documentMock.ExpectAndReturn("getElementById", null, "f_Beneficiary");
                return;
            }
            documentMock.ExpectAndReturn("getElementById", AddElementMock(typeof(IHTMLElement)).MockInstance, "f_Beneficiary");
            documentMock.ExpectAndReturn("getElementById", null, MilleniumProvider.BENEFICIARY_LIST);

            if (extended.Length == 0) {
                AddConfirmDiv("f_DestinationAccount", enteredAccountNo);
                AddConfirmDiv("f_Amount", amountPaid + " PLN");
            }
        }

        private void AddConfirmDiv(String divId, String value) {
            DynamicMock div = AddElementMock(typeof(IHTMLElement));
            documentMock.ExpectAndReturn("getElementById", div.MockInstance, divId);

            DynamicMock divElems = AddElementMock(typeof(IHTMLElementCollection));
            div.ExpectAndReturn("get_all", divElems.MockInstance);

            ArrayList divList = new ArrayList();
            DynamicMock d = AddElementMock(typeof(IHTMLElement));
            divList.Add(d.MockInstance);

            d = AddElementMock(typeof(IHTMLElement));
            d.ExpectAndReturn("get_innerText", "&nbsp;" + value);
            divList.Add(d.MockInstance);

            divElems.ExpectAndReturn("GetEnumerator", divList.GetEnumerator());
        }
        
        private void PreparePastePaymentInfoMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            if (paymentInfo.IsDefinedTransfer == false) {
                DynamicMock radioElems = AddElementMock(typeof(IHTMLElementCollection));
                documentMock.ExpectAndReturn("getElementsByTagName", radioElems.MockInstance, "input");

                ArrayList radioList = new ArrayList();
                DynamicMock radio = AddElementMock(typeof(FakeHtmlInputElement));
                radio.ExpectAndReturn("get_value", "some value");
                radioList.Add(radio.MockInstance);

                radio = AddElementMock(typeof(FakeHtmlInputElement));
                radio.ExpectAndReturn("get_value", "AccountNumberFullDestinationInput");
                radio.Expect("click");
                radioList.Add(radio.MockInstance);

                radioElems.ExpectAndReturn("GetEnumerator", radioList.GetEnumerator());

                PrepareInputMockById(MilleniumProvider.ACCOUNT_FIELD, paymentInfo.BankAccountNo, typeof(FakeHtmlInputElement));
                PrepareInputMockById(MilleniumProvider.NAME_FIELD, provider.ReplaceSpecialChars(paymentInfo.BillerName), typeof(FakeHtmlInputElement));
                PrepareInputMockById(MilleniumProvider.ADDRESS_FIELD, provider.ReplaceSpecialChars(paymentInfo.Street), typeof(FakeHtmlInputElement));
                PrepareInputMockById(MilleniumProvider.POSTAL_CODE_FIELD, provider.ReplaceSpecialChars(paymentInfo.PostalCodeAndCity), typeof(FakeHtmlInputElement));
            }
            PrepareInputMockById(MilleniumProvider.DESCRIPTION_FIELD, provider.ReplaceSpecialChars(paymentInfo.Title), typeof(FakeHtmlTextAreaElement));
            PrepareInputMock(MilleniumProvider.AMOUNT_INT_FIELD, paymentInfo.AmountToPayDecimal, typeof(FakeHtmlInputElement));
            PrepareInputMock(MilleniumProvider.AMOUNT_DEC_FIELD, paymentInfo.AmountToPayFloating, typeof(FakeHtmlInputElement));
            PrepareInputMockById(MilleniumProvider.AMOUNT_FIELD, paymentInfo.AmountToPay.Replace(".", ","), typeof(FakeHtmlInputElement));
            PrepareInputMock(MilleniumProvider.YEAR_FIELD, paymentInfo.DueDateTimeToPaste.Year.ToString(), typeof(FakeHtmlSelectElement));
            PrepareInputMock(MilleniumProvider.MONTH_FIELD, Util.GetMonth(paymentInfo.DueDateTimeToPaste.Month), typeof(FakeHtmlSelectElement));
            PrepareInputMock(MilleniumProvider.DAY_FIELD, paymentInfo.DueDateTimeToPaste.Day.ToString(), typeof(FakeHtmlSelectElement));
        }

        private void PrepareDefinedTransfersPageMocks(bool result, int definedTransfersCount) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock divElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", divElems.MockInstance, "div");

            ArrayList divList = new ArrayList();
            DynamicMock div = AddElementMock(typeof(IHTMLElement));
            div.ExpectAndReturn("get_innerHTML", "some text");
            divList.Add(div.MockInstance);

            if (result == true) {
                div = AddElementMock(typeof(IHTMLElement));
                div.ExpectAndReturn("get_innerHTML", "Przelew&nbsp;Krajowy | Przelew SORB");
                divList.Add(div.MockInstance);

                documentMock.ExpectAndReturn("getElementById", AddElementMock(typeof(IHTMLElement)).MockInstance, MilleniumProvider.BENEFICIARY_LIST);
            }
            divElems.ExpectAndReturn("GetEnumerator", divList.GetEnumerator());

            if (result == true) {
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
                switch (definedTransfersCount) {
                    case 1:
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName + MilleniumProvider.TRUSTED_POSTFIX, typeof(FakeHtmlSelectElement), false, false);
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName + MilleniumProvider.TRUSTED_POSTFIX, typeof(FakeHtmlSelectElement), false, true);
                        break;
                    case 2:
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName + " - dummy", typeof(FakeHtmlSelectElement), false, false);
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName + " - dummy", typeof(FakeHtmlSelectElement), false, false);
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName + MilleniumProvider.NORMAL_POSTFIX, typeof(FakeHtmlSelectElement), false, true);
                        break;

                    case 3:
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName, typeof(FakeHtmlSelectElement), true, true);
                        break;

                    default:
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName + " - dummy", typeof(FakeHtmlSelectElement), false, false);
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName + " - dummy", typeof(FakeHtmlSelectElement), false, false);
                        PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, paymentInfo.DefinedTransferName + " - dummy", typeof(FakeHtmlSelectElement), false, false);
                        break;
                }
            }
        }

        private void PrepareGoToTransferPageMocks() {
            PrepareGoToTransferPageMocks(false);
        }

        private void PrepareGoToTransferPageMocks(bool transferPage) {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            if (transferPage == true) {
                PrepareInputMockById(MilleniumProvider.BENEFICIARY_LIST, "Wybierz", typeof(FakeHtmlSelectElement), false, true);
                //request.UrlForUpdate = "http://TRDomestic.qz";
                return;
            } else {
                documentMock.ExpectAndReturn("getElementById", null, MilleniumProvider.BENEFICIARY_LIST);
            }
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            DynamicMock location = AddElementMock(typeof(HTMLLocation));
            documentMock.ExpectAndReturn("get_location", location.MockInstance);

            if (url == null) {
                url = "";
            }
            if (url.Contains("/osobiste")) {
                location.Expect("set_pathname", MilleniumProvider.TRANSFER_PAGE_URL);
            } else if (url.Contains("/firmy")) {
                location.Expect("set_pathname", MilleniumProvider.TRANSFER_PAGE_ENT_URL);
            } else if (url.Contains("/demofirmy")) {
                location.Expect("set_pathname", MilleniumProvider.TRANSFER_PAGE_DEMO_ENT_URL);
            } else {
                location.Expect("set_pathname", MilleniumProvider.TRANSFER_PAGE_DEMO_URL);
            }
        }

        private void PrepareReadyToPasteMocks(bool firstCheck, bool secondCheck) {
            PrepareLoggedUserMocks();

            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            PrepareTransferPageMocks(firstCheck, secondCheck);
        }

        private void PrepareTransferPageMocks(bool firstCheck, bool secondCheck) {
            DynamicMock divElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", divElems.MockInstance, "div");

            ArrayList divList = new ArrayList();
            DynamicMock div = AddElementMock(typeof(IHTMLElement));
            div.ExpectAndReturn("get_innerHTML", "some text");
            divList.Add(div.MockInstance);

            if (firstCheck == true) {
                div = AddElementMock(typeof(IHTMLElement));
                div.ExpectAndReturn("get_innerHTML", "Przelew&nbsp;Krajowy | przelew SORB");
                divList.Add(div.MockInstance);

                DynamicMock select = AddElementMock(typeof(FakeHtmlSelectElement));
                documentMock.ExpectAndReturn("getElementById", select.MockInstance, MilleniumProvider.BENEFICIARY_LIST);
                DynamicMock beneficiary = AddElementMock(typeof(IHTMLElementCollection));
                beneficiary.ExpectAndReturn("get_length", 1);
                documentMock.ExpectAndReturn("getElementsByName", beneficiary.MockInstance, MilleniumProvider.DAY_FIELD);
                if (secondCheck == true && paymentInfo.IsDefinedTransfer == false) {
                    select.ExpectAndReturn("get_selectedIndex", 0);
                } else if (secondCheck == true) {
                    select.ExpectAndReturn("get_selectedIndex", 1);
                    DynamicMock item = AddElementMock(typeof(FakeHtmlOptionElement));
                    select.ExpectAndReturn("item", item.MockInstance, 1, null);
                    if ((DateTime.Now.Millisecond % 2) == 0) {
                        item.ExpectAndReturn("get_text", paymentInfo.DefinedTransferName + MilleniumProvider.NORMAL_POSTFIX);
                    } else {
                        item.ExpectAndReturn("get_text", paymentInfo.DefinedTransferName + MilleniumProvider.TRUSTED_POSTFIX);
                    }

                } else if (paymentInfo.IsDefinedTransfer == false) {
                    select.ExpectAndReturn("get_selectedIndex", 1);
                } else {
                    select.ExpectAndReturn("get_selectedIndex", 2);
                    DynamicMock item = AddElementMock(typeof(FakeHtmlOptionElement));
                    select.ExpectAndReturn("item", item.MockInstance, 2, null);
                    item.ExpectAndReturn("get_text", " ");
                }
            }
            divElems.ExpectAndReturn("GetEnumerator", divList.GetEnumerator());
        }

        private void PrepareLoggedUserMocks() {
            browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);

            if (url == null || url.Contains(".qz") == false) {
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            } else if (url.EndsWith("TRDomestic.qz")) {
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
                documentMock.ExpectAndReturn("get_url", url);
            }
            if (url == String.Empty) {
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
                browserMock.ExpectAndReturn("get_Document", documentMock.MockInstance);
            }

            DynamicMock aElems = AddElementMock(typeof(IHTMLElementCollection));
            documentMock.ExpectAndReturn("getElementsByTagName", aElems.MockInstance, "a");

            ArrayList aList = new ArrayList();
            DynamicMock a = AddElementMock(typeof(IHTMLElement));
            a.ExpectAndReturn("get_innerHTML", "some text");
            aList.Add(a.MockInstance);

            a = AddElementMock(typeof(IHTMLElement));
            a.ExpectAndReturn("get_innerHTML", "WYLOGUJ&nbsp;");
            aList.Add(a.MockInstance);

            aElems.ExpectAndReturn("GetEnumerator", aList.GetEnumerator());
        }
        #endregion
    }
}
