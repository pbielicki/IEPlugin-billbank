using System;
using NUnit.Framework;
using Billbank.IEPlugin;
using Billbank.IEPlugin.Provider;

namespace IEPluginTests.Provider {
    [TestFixture]
    class ProviderFactoryTest {

        [Test]
        public void ShouldReturnNullProvider() {
            Assert.AreSame(NullProvider.GetInstance(), ProviderFactory.GetProvider(null));

            Assert.AreSame(NullProvider.GetInstance(), ProviderFactory.GetProvider("Dummy Provider"));

            Assert.AreSame(NullProvider.GetInstance(), ProviderFactory.GetProvider("http://wp.pl"));
        }

        [Test]
        public void ShouldReturnProvider() {
            PaymentProvider provider = ProviderFactory.GetProvider(Settings.Default.ProviderId[0]);
            Assert.AreEqual(Settings.Default.ProviderClass[0], provider.GetType().FullName);

            Assert.AreSame(provider, ProviderFactory.GetProvider(Settings.Default.ProviderId[0]));
        }

        [Test]
        public void ShouldReturnAllProvidersOneByOne() {
            for (int i = 0; i < Settings.Default.ProviderId.Count; i++) {
                PaymentProvider provider = ProviderFactory.GetProvider(Settings.Default.ProviderId[i]);
                Assert.AreEqual(Settings.Default.ProviderClass[i], provider.GetType().FullName);

                Assert.AreSame(provider, ProviderFactory.GetProvider(Settings.Default.ProviderId[i]));
            }
        }

        [Test]
        public void ShouldReturnProvidersLinkOpener() {
            Assert.AreSame(ProviderListOpener.GetInstance(), ProviderFactory.GetProvider(null, true));

            Assert.AreSame(ProviderListOpener.GetInstance(), ProviderFactory.GetProvider("Dummy Provider", true));

            Assert.AreSame(ProviderListOpener.GetInstance(), ProviderFactory.GetProvider("http://wp.pl", true));
        }
    }
}
