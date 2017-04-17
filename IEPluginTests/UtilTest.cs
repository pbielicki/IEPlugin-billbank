using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using IE = Interop.SHDocVw;
using mshtml;
using NUnit.Framework;
using NUnit.Mocks;
using Billbank.IEPlugin;

namespace IEPluginTests {
    [TestFixture]
    public class UtilTest {

        private DynamicMock docMock;
        private DynamicMock framesMock;
        private DynamicMock frameDocMock;
        private DynamicMock windowMock;
        private DynamicMock selectMock;

        [Test]
        public void ShouldSplitLongText() {
            String[] expected = new String[] { "Some Very", "Long Text", "For", "Testing a", "Special Fn", "in here" };
            Assert.AreEqual(expected, Util.SplitString("Some Very Long Text For Testing a Special Fn in here blah blah blah more and more", 10, 6));

            expected = new String[] { "SomeVeryLo", "ngTextForT", "estingaSpe", "cialFninhe", "reblahblah", "blahmorean", "dmore", "", "" };
            Assert.AreEqual(expected, Util.SplitString("SomeVeryLongTextForTestingaSpecialFninhereblahblahblahmoreandmore", 10, 9));

            expected = new String[] { "SomeVeryLo", "ngTextForT", "estingaSpe" };
            Assert.AreEqual(expected, Util.SplitString("SomeVeryLongTextForTestingaSpecialFninhereblahblahblahmoreandmore", 10, 3));

            expected = new String[] { "SomeVeryLo", "ngTextFor", "Testing a", "SpecialFni" };
            Assert.AreEqual(expected, Util.SplitString("SomeVeryLongTextFor Testing a SpecialFninhereblahblahblahmoreandmore", 10, 4));

            expected = new String[] { "Polskie Górnictwo Naftowe i", "Gazownictwo S.A.", "", ""};
            Assert.AreEqual(expected, Util.SplitString("Polskie Górnictwo Naftowe i Gazownictwo S.A.", 35, 4));

            expected = new String[] { "Some Text", "", "", "" };
            Assert.AreEqual(expected, Util.SplitString("Some Text", 10, 4));
        }

        [Test]
        public void ShouldCheckContainsLinkToHandle() {
            Assert.IsTrue(Util.ContainsLinkToHandle(Settings.Default.LinksInnerHtmlToHandle[1]));
            Assert.IsTrue(Util.ContainsLinkToHandle(Settings.Default.LinksInnerHtmlToHandle[Settings.Default.LinksInnerHtmlToHandle.Count - 1]));

            Assert.IsFalse(Util.ContainsLinkToHandle(""));
            Assert.IsFalse(Util.ContainsLinkToHandle(null));
            Assert.IsFalse(Util.ContainsLinkToHandle("blah blah"));
        }

        [Test]
        public void ShouldReturnFalseDueToException() {
            docMock = new DynamicMock(typeof(HTMLDocument));
            DynamicMock window = new DynamicMock(typeof(IHTMLWindow2));
            docMock.ExpectAndReturn("get_parentWindow", window.MockInstance);
            window.ExpectAndThrow("execScript", new ArgumentException(), "someScript()", "JScript");

            Assert.AreEqual(false, Util.RunJavaScript((HTMLDocument)docMock.MockInstance, "someScript()"));
        }

        [Test]
        public void ShouldSetDifferentHtmlElementsValue() {
            Dictionary<String, List<int>> expected = PrepareHtmlSelectMock();

            DynamicMock inputMock = new DynamicMock(typeof(IHTMLInputElement));
            inputMock.Expect("set_value", "inputValue");
            inputMock.Expect("set_value", "inputValue");

            DynamicMock textAreaMock = new DynamicMock(typeof(IHTMLTextAreaElement));
            textAreaMock.Expect("set_value", "textAreaValue");

            DynamicMock elementsMock = new DynamicMock(typeof(IHTMLElementCollection));
            elementsMock.ExpectAndReturn("get_length", 1);
            elementsMock.ExpectAndReturn("item", inputMock.MockInstance, null, 0);

            Util.SetElementValue(inputMock.MockInstance, "inputValue");
            Util.SetElementValue(textAreaMock.MockInstance, "textAreaValue");
            Util.SetElementValue(elementsMock.MockInstance, "inputValue");

            inputMock.Verify();
            textAreaMock.Verify();
            elementsMock.Verify();
        }

        [Test]
        public void ShouldRetrieveOptionsFromSelectElement() {
            Dictionary<String, List<int>> expected = PrepareHtmlSelectMock();
            Dictionary<String, List<int>> result = Util.GetOptionsMap(selectMock.MockInstance as IHTMLElement);
            Assert.AreEqual(10, result.Count);
            Assert.AreNotSame(expected, result);

            foreach (String key in expected.Keys) {
                Assert.AreEqual(expected[key], result[key]);
            }

            selectMock.Verify();

        }

        private Dictionary<String, List<int>> PrepareHtmlSelectMock() {
            selectMock = new DynamicMock(typeof(IHTMLElement));
            DynamicMock elementsMock = new DynamicMock(typeof(IHTMLElementCollection));

            ArrayList options = new ArrayList();
            Dictionary<String, List<int>> expected = new Dictionary<String, List<int>>();
            for (int i = 0; i < 10; i++) {
                DynamicMock optionMock = new DynamicMock(typeof(IHTMLOptionElement));
                String value = i + " " + (i * 2);
                optionMock.ExpectAndReturn("get_text", value);
                options.Add(optionMock.MockInstance);
                List<int> list = new List<int>();
                list.Add(i);
                expected.Add(value, list);
            }
            elementsMock.ExpectAndReturn("GetEnumerator", options.GetEnumerator());
            selectMock.ExpectAndReturn("get_all", elementsMock.MockInstance);
            return expected;
        }

        [Test]
        public void ShouldSetCurrentPaymentInfoInRegistry() {
            Util.SetCurrentPaymentInfo("12345678");
            Assert.AreEqual("12345678", Application.UserAppDataRegistry.GetValue(Util.CURRENT_PAYMENT));

            Util.SetCurrentPaymentInfo("ABCSEFGH");
            Assert.AreEqual("ABCSEFGH", Application.UserAppDataRegistry.GetValue(Util.CURRENT_PAYMENT));

            Util.SetCurrentPaymentInfo("");
            Assert.AreEqual("", Application.UserAppDataRegistry.GetValue(Util.CURRENT_PAYMENT));
        }

        [Test]
        public void ShouldSetUpdatedPaymentInfoInRegistry() {
            Util.SetUpdatedPaymentInfo("12345678");
            Assert.AreEqual("12345678", Application.UserAppDataRegistry.GetValue(Util.UPDATED_PAYMENT));

            Util.SetUpdatedPaymentInfo("ABCSEFGH");
            Assert.AreEqual("ABCSEFGH", Application.UserAppDataRegistry.GetValue(Util.UPDATED_PAYMENT));

            Util.SetUpdatedPaymentInfo("");
            Assert.AreEqual("", Application.UserAppDataRegistry.GetValue(Util.UPDATED_PAYMENT));
        }

        [Test]
        public void ShouldReturnProviderUrl() {
            Assert.AreEqual("http://some.bank.provider", Util.GetProviderId("http://some.bank.provider/"));
            Assert.AreEqual("http://some.bank.provider", Util.GetProviderId("http://some.bank.provider/usr"));
            Assert.AreEqual("http://some.bank.provider", Util.GetProviderId("http://some.bank.provider"));
        }

        [Test]
        public void ShouldReturnEmptyUrl() {
            Assert.AreEqual("", Util.GetProviderId("some.bank.provider"));
        }

        [Test]
        public void ShouldCallMockForUrlInNewTab() {
            object url = "http://some.url";
            object flags = 0x0800;
            object window = "_blank";
            object nullObj = null;
            DynamicMock browserMock = new DynamicMock(typeof(IE.WebBrowser));
            browserMock.Expect("Navigate2", url, flags, window, nullObj, nullObj);
            IE.WebBrowser browser = (IE.WebBrowser) browserMock.MockInstance;

            Util.OpenUrlInNewTab(browser, (string) url);
            browserMock.Verify();
        }

        [Test]
        public void ShouldCallMockForUrlInSameTab() {
            object url = "http://some.url/more";
            object flags = 0x2;
            object window = "_self";
            object nullObj = null;
            DynamicMock browserMock = new DynamicMock(typeof(IE.WebBrowser));
            browserMock.Expect("Navigate2", url, flags, window, nullObj, nullObj);
            IE.WebBrowser browser = (IE.WebBrowser)browserMock.MockInstance;

            Util.OpenUrl(browser, (string)url);
            browserMock.Verify();
        }

        [Test]
        public void ShouldReturnFramesCount() {
            framesMock = new DynamicMock(typeof(FramesCollection));
            framesMock.ExpectAndReturn("get_length", 3);
            docMock = new DynamicMock(typeof(HTMLDocument));
            docMock.ExpectAndReturn("get_frames", framesMock.MockInstance);
            docMock.ExpectAndReturn("get_frames", framesMock.MockInstance);
            Assert.AreEqual(3, Util.GetFramesCount((HTMLDocument)docMock.MockInstance));
            framesMock.Verify();
            docMock.Verify();
        }

        [Test]
        public void ShouldReturnValidFrame() {
            PrepareMocksForFrame();
            Assert.AreSame(windowMock.MockInstance, Util.GetFrame((HTMLDocument)docMock.MockInstance, 1));
            VerifyMocksForFrame();
        }

        [Test]
        public void ShouldReturnValidFrameDocument() {
            PrepareMocksForFrameDocument();
            Assert.AreSame(frameDocMock.MockInstance, Util.GetFrameDocument((HTMLDocument)docMock.MockInstance, 1));
            VerifyMocksForFrame();
        }

        private void PrepareMocksForFrameDocument() {
            PrepareMocksForFrame();
            frameDocMock = new DynamicMock(typeof(IHTMLDocument2));
            windowMock.ExpectAndReturn("get_document", frameDocMock.MockInstance);
        }


        private void PrepareMocksForFrame() {
            docMock = new DynamicMock(typeof(HTMLDocument));
            framesMock = new DynamicMock(typeof(FramesCollection));
            windowMock = new DynamicMock(typeof(HTMLWindow2));
            docMock.ExpectAndReturn("get_frames", framesMock.MockInstance, null);
            docMock.ExpectAndReturn("get_frames", framesMock.MockInstance, null);
            framesMock.ExpectAndReturn("get_length", 2, null);
            framesMock.ExpectAndReturn("item", windowMock.MockInstance, 1);
        }

        private void VerifyMocksForFrame() {
            docMock.Verify();
            framesMock.Verify();
            windowMock.Verify();
        }
    }
}
