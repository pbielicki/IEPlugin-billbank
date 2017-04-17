using System;
using Billbank.IEPlugin.Domain;

namespace Billbank.IEPlugin.Provider {
    public interface PaymentProvider {
        PaymentInfo Execute(PaymentRequest request);
    }
}
