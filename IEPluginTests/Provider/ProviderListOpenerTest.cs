using System;
using IE = Interop.SHDocVw;
using NUnit.Framework;
using NUnit.Mocks;
using Billbank.IEPlugin;
using Billbank.IEPlugin.Domain;
using Billbank.IEPlugin.Provider;

namespace IEPluginTests.Provider {
    [TestFixture]
    class ProviderListOpenerTest {

        [Test]
        public void ShouldDoNothingOnExecute() {
            DynamicMock webBrowserMock = new DynamicMock(typeof(IE.WebBrowser));
            webBrowserMock.ExpectAndReturn("get_LocationURL", Settings.Default.ChoseBankLink + Settings.Default.PluginVersion);

            PaymentInfo info = new PaymentInfo();
            info.State = State.IDLE;
            PaymentProvider provider = GetProvider();

            PaymentInfo result = provider.Execute(new PaymentRequest(webBrowserMock.MockInstance as IE.WebBrowser, info));
            Assert.AreSame(info, result);
            webBrowserMock.Verify();
        }
        
        [Test]
        public void ShouldOpenListInNewTab() {
            DynamicMock webBrowserMock = new DynamicMock(typeof(IE.WebBrowser));
            webBrowserMock.ExpectAndReturn("get_LocationURL", Settings.Default.ChoseBankLink + "yyy");
            webBrowserMock.Expect("Navigate2", Settings.Default.ChoseBankLink + Settings.Default.PluginVersion, 0x2, "_self", null, null);

            PaymentInfo info = new PaymentInfo();
            info.State = State.IDLE;
            PaymentProvider provider = GetProvider();

            PaymentInfo result = provider.Execute(new PaymentRequest(webBrowserMock.MockInstance as IE.WebBrowser, info));
            Assert.AreSame(info, result);
            webBrowserMock.Verify();
        }

        [Test]
        public void ShouldExecute() {
            DynamicMock webBrowserMock = new DynamicMock(typeof(IE.WebBrowser));
            webBrowserMock.ExpectAndReturn("get_LocationURL", null);

            PaymentInfo info = new PaymentInfo();
            info.State = State.IDLE;
            PaymentProvider provider = GetProvider();

            PaymentInfo result = provider.Execute(new PaymentRequest(webBrowserMock.MockInstance as IE.WebBrowser, info));
            Assert.AreSame(info, result);
            webBrowserMock.Verify();
        }

        protected PaymentProvider GetProvider() {
            return ProviderListOpener.GetInstance();
        }
    }
}
