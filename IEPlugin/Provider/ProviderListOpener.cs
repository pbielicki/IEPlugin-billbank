using System;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class ProviderListOpener : PaymentProvider {
        private static readonly ProviderListOpener INSTANCE = new ProviderListOpener();
        internal static readonly String URL = String.Format("{0}{1}", Settings.Default.ChoseBankLink, Settings.Default.PluginVersion);

        public static ProviderListOpener GetInstance() {
            return INSTANCE;
        }

        private ProviderListOpener() {
        }

        public PaymentInfo Execute(PaymentRequest request) {
            String url = request.WebBrowser.LocationURL;
            String providersListUrl = Settings.Default.ChoseBankLink;
            if (url != null && url.Equals(providersListUrl) == false) {
                Util.OpenUrl(request.WebBrowser, URL);
            }

            return request.PaymentInfo;
        }
    }
}
