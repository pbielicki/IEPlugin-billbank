using System;
using IE = Interop.SHDocVw;
using NUnit.Framework;
using NUnit.Mocks;
using Billbank.IEPlugin.Domain;
using Billbank.IEPlugin.Provider;

namespace IEPluginTests.Provider {
    [TestFixture]
    class NullProviderTest {

        [Test]
        public void ShouldExecute() {
            DynamicMock webBrowserMock = new DynamicMock(typeof(IE.WebBrowser));

            PaymentInfo info = new PaymentInfo();
            info.State = State.IDLE;
            PaymentProvider provider = GetProvider();

            PaymentInfo result = provider.Execute(new PaymentRequest(webBrowserMock.MockInstance as IE.WebBrowser, info));
            Assert.AreSame(info, result);
            webBrowserMock.Verify();
        }

        protected PaymentProvider GetProvider() {
            return NullProvider.GetInstance();
        }
    }
}
