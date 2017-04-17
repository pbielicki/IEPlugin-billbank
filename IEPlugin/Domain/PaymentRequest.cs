using System;
using IE = Interop.SHDocVw;
using mshtml;

namespace Billbank.IEPlugin.Domain {
    public class PaymentRequest {
        private readonly IE.WebBrowser webBrowser;
        private readonly PaymentInfo paymentInfo;
        private readonly bool userAction = false;
        private String url;

        public PaymentRequest(IE.WebBrowser webBrowser, PaymentInfo paymentInfo, bool userAction)
            : this(webBrowser, paymentInfo) {

            this.userAction = userAction;
        }

        public PaymentRequest(IE.WebBrowser webBrowser, PaymentInfo paymentInfo, String url) 
            : this(webBrowser, paymentInfo) {

            this.url = url;
        }

        public PaymentRequest(IE.WebBrowser webBrowser, PaymentInfo paymentInfo) {
            this.webBrowser = webBrowser;
            this.paymentInfo = paymentInfo;
        }

        public IE.WebBrowser WebBrowser {
            get {
                return webBrowser;
            }
        }

        public PaymentInfo PaymentInfo {
            get {
                return paymentInfo;
            }
        }

        public HTMLDocument Document {
            get {
                return webBrowser.Document as HTMLDocument;
            }
        }

        public bool UserAction {
            get {
                return this.userAction;
            }
        }

        public String Url {
            get {
                if (this.url == null) {
                    this.url = Document.url;
                }
                if (this.url != null) {
                    return this.url;
                }

                return String.Empty;
            }
        }

        internal String UrlForUpdate {
            set {
                this.url = value;
            }
        }
    }
}
