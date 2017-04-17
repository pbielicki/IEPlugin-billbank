using System;
using System.Reflection;
using System.Collections.Generic;
using Billbank.IEPlugin;

namespace Billbank.IEPlugin.Provider {
    public class ProviderFactory {

        private static Dictionary<String, PaymentProvider> providers = new Dictionary<String, PaymentProvider>();

        public static PaymentProvider GetProvider(String providerId) {
            return GetProvider(providerId, false);
        }

        /// <summary>
        /// Gets appropriate provider for given provider ID. Check Settings.settings file to see the mapping.
        /// In fact the ID of the provider is the base URL.
        /// Different default provider is returned depending on the action origin i.e. for user initiated
        /// action it is Billbank.IEPlugin.Provider.ProviderListOpener whilst for any other event it is
        /// Billbank.IEPlugin.Provider.NullProvider.
        /// </summary>
        /// <param name="providerId">payment provider's ID</param>
        /// <param name="userAction">sets the user initiated action flag</param>
        /// <returns>provider object</returns>
        public static PaymentProvider GetProvider(String providerId, bool userAction) {
            if (providerId == null) {
                return GetDefaultProvider(userAction);
            }

            PaymentProvider result = null;
            if (providers.ContainsKey(providerId)) {
                return providers[providerId];
            }

            int idx = Settings.Default.ProviderId.IndexOf(providerId);
            if (idx >= 0) {
                try {
                    Type type = Type.GetType(Settings.Default.ProviderClass[idx]);
                    result = (PaymentProvider)Activator.CreateInstance(type);
                    providers[providerId] = result;
                    return result;
                } catch {
                    return GetDefaultProvider(userAction);
                }
            }

            return GetDefaultProvider(userAction);
        }

        private static PaymentProvider GetDefaultProvider(bool userAction) {
            if (userAction == true) {
                return ProviderListOpener.GetInstance();
            }
            return NullProvider.GetInstance();
        }
    }
}
