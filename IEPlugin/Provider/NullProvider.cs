using System;
using System.Windows.Forms;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public class NullProvider : PaymentProvider {

        private static readonly NullProvider INSTANCE = new NullProvider();

        public static NullProvider GetInstance() {
            return INSTANCE;
        }

        private NullProvider() {
        }

        public PaymentInfo Execute(PaymentRequest request) {
            return request.PaymentInfo;
        }
    }
}
