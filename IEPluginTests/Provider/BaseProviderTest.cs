using System;
using System.Collections;
using System.Collections.Generic;
using IE = Interop.SHDocVw;
using mshtml;
using NUnit.Framework;
using NUnit.Mocks;

using IEPluginTests.Html;
using Billbank.IEPlugin;
using Billbank.IEPlugin.Domain;
using Billbank.IEPlugin.Provider;

namespace IEPluginTests.Provider {
    abstract class BaseProviderTest {

        protected IE.WebBrowser webBrowser;
        protected PaymentInfo paymentInfo;
        protected PaymentRequest request;
        protected AbstractProvider provider;
        protected DynamicMock browserMock;
        protected DynamicMock documentMock;
        protected List<DynamicMock> elementMock;

        #region Test Fixture
        [SetUp]
        public virtual void SetUp() {
            GlobalData.Instance.TestMode = true;
            Settings.Default.ReloadCount = 0;

            browserMock = new DynamicMock(typeof(IE.WebBrowser));
            documentMock = new DynamicMock(typeof(HTMLDocument));
            elementMock = new List<DynamicMock>();

            webBrowser = browserMock.MockInstance as IE.WebBrowser;
            paymentInfo = new PaymentInfo();
            paymentInfo.Amount = "123.12";
            paymentInfo.AmountToPay = "120.00";
            paymentInfo.BankAccountNo = "PL 1234567980";
            paymentInfo.BillerName = "Some Biller Name`~!@#$%^&*_={}[];\"'<>|\\() of the ~`!@#$%^&*()_+{}|:\"<>?=[]\\'";
            paymentInfo.City = "CityName`~!@#$%^&*_={}[];\"'<>|\\() of the ~`!@#$%^&*()_+{}|:\"<>?=[]\\'";
            paymentInfo.Currency = "PLN";
            paymentInfo.DefinedTransferName = "Defined Transfer";
            paymentInfo.DueDate = "20.03.2010";
            paymentInfo.Id = "12345";
            paymentInfo.PostalCode = "12098";
            paymentInfo.State = State.IDLE;
            paymentInfo.Street = "Some Street name`~!@#$%^&*_={}[];\"'<>|\\() of the ~`!@#$%^&*()_+{}|:\"<>?=[]\\'";
            paymentInfo.Title = "Title `~!@#$%^&*_={}[];\"'<>|\\() of the ~`!@#$%^&*()_+{}|:\"<>?=[]\\'payment";

            request = new PaymentRequest(webBrowser, paymentInfo);
            provider = CreateProvider();
        }

        [TearDown]
        public void TearDown() {
            GlobalData.Instance.TestQuestionAnswer = false;
            browserMock.Verify();
            documentMock.Verify();
            foreach (DynamicMock mock in elementMock) {
                mock.Verify();
            }
        }
        #endregion

        protected abstract AbstractProvider CreateProvider();

        #region Auxiliary methods
        protected DynamicMock AddElementMock(Type type) {
            DynamicMock mock = new DynamicMock(type);
            elementMock.Add(mock);
            return mock;
        }

        protected void PrepareEmptyInputMockById(String fieldId) {
            documentMock.ExpectAndReturn("getElementById", null, fieldId);
        }

        /// <param name="args">Additional (optional) arguments used for FakeHtmlSelectElement type 
        /// <see cref="PrepareInput"/>
        /// </param>
        protected void PrepareInputMockById(String fieldId, String value, Type type, params bool[] args) {
            DynamicMock input = AddElementMock(type);
            PrepareInput(input, value, type, args);
            documentMock.ExpectAndReturn("getElementById", input.MockInstance, fieldId);
        }

        protected void PrepareEmptyInputMock(String fieldName) {
            DynamicMock fields = AddElementMock(typeof(IHTMLElementCollection));
            fields.ExpectAndReturn("get_length", 0);
            documentMock.ExpectAndReturn("getElementsByName", fields.MockInstance, fieldName);
        }

        /// <param name="args">Additional (optional) arguments used for FakeHtmlSelectElement type 
        /// <see cref="PrepareInput"/>
        /// </param>
        protected void PrepareInputMock(String fieldName, String value, Type type, params bool[] args) {
            DynamicMock fields = AddElementMock(typeof(IHTMLElementCollection));
            fields.ExpectAndReturn("get_length", 1);
            DynamicMock input = AddElementMock(type);
            PrepareInput(input, value, type, args);

            fields.ExpectAndReturn("item", input.MockInstance, null, 0);
            documentMock.ExpectAndReturn("getElementsByName", fields.MockInstance, fieldName);
        }

        /// <param name="args">Additional (optional) arguments used for FakeHtmlSelectElement type:<br/>
        /// args[0] - repeat value in the Select element?<br/>
        /// args[1] - value in the Select will be selected?
        /// </param>
        private void PrepareInput(DynamicMock input, String value, Type type, params bool[] args) {
            if (type == typeof(FakeHtmlSelectElement)) {
                DynamicMock options = AddElementMock(typeof(IHTMLElementCollection));
                input.ExpectAndReturn("get_all", options.MockInstance);

                ArrayList optionsList = new ArrayList();
                DynamicMock option = AddElementMock(typeof(FakeHtmlOptionElement));
                option.ExpectAndReturn("get_text", value + "123");
                optionsList.Add(option.MockInstance);

                option = AddElementMock(typeof(FakeHtmlOptionElement));
                option.ExpectAndReturn("get_text", "456" + value);
                optionsList.Add(option.MockInstance);

                option = AddElementMock(typeof(FakeHtmlOptionElement));
                option.ExpectAndReturn("get_text", value);
                optionsList.Add(option.MockInstance);

                if (args.Length > 0 && args[0] == true) {
                    option = AddElementMock(typeof(FakeHtmlOptionElement));
                    option.ExpectAndReturn("get_text", value);
                    optionsList.Add(option.MockInstance);
                }
                options.ExpectAndReturn("GetEnumerator", optionsList.GetEnumerator());

                // select this option?
                if (args.Length == 0 || (args.Length > 1 && args[1] == true)) {
                    input.Expect("set_selectedIndex", 2);
                }
            } else {
                input.Expect("set_value", value);
            }
        }
        #endregion
    }
}
