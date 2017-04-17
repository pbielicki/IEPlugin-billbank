using System;
using System.Windows.Forms;
using IE = Interop.SHDocVw;
using mshtml;
using NUnit.Mocks;
using NUnit.Framework;
using Billbank.IEPlugin.Domain;

namespace IEPluginTests.Domain {
    [TestFixture]
    class PaymentRequestTest {

        [Test]
        public void ShouldReturnDefaultOption() {
            DynamicMock webBrowser = new DynamicMock(typeof(IE.WebBrowser));
            DynamicMock documentMock = new DynamicMock(typeof(HTMLDocument));
            webBrowser.ExpectAndReturn("get_Document", documentMock.MockInstance);
            documentMock.ExpectAndReturn("get_url", "http://someLink/");
            PaymentRequest request = new PaymentRequest(webBrowser.MockInstance as IE.WebBrowser, PaymentInfo.ValueOf(""));

            Assert.AreEqual("http://someLink/", request.Url);
            Assert.AreEqual("http://someLink/", request.Url);
        }
    }
}
