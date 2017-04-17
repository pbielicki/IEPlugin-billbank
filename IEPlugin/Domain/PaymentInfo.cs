using System;
using System.Text;
using System.Globalization;
using System.Windows.Forms;

namespace Billbank.IEPlugin.Domain {
    public class PaymentInfo {

        private static readonly String DEFAULT_COUNTRY_CODE = "PL";

        private String id;
        private String bankAccountNo;
        private String countryCode;
        private String amount;
        private String amountToPay;
        private int amountToPayInt;
        private String amountToPayDecimal;
        private String amountToPayFloating;
        private String amountPaid;
        private int amountPaidInt;
        private String title;
        private String street;
        private String postalCode;
        private String city;
        private String billerName;
        private String dueDate;
        private DateTime dueDateTime;
        private String currency;
        private String definedTransferName;
        private State state = State.INVALID;

        #region Factory ValueOf / ToString region
        public static PaymentInfo ValueOf(HtmlDocument doc, String id) {
            PaymentInfo info = new PaymentInfo();
            if (doc == null || id == null) {
                return info;
            }
            try {
                info.Id = id;
                info.BankAccountNo = doc.GetElementById(String.Format("bankAccountNo[{0}]", id)).GetAttribute("value");
                info.Amount = doc.GetElementById(String.Format("amount[{0}]", id)).GetAttribute("value");
                info.AmountToPay = doc.GetElementById(String.Format("amountToPay[{0}]", id)).GetAttribute("value");
                info.Title = doc.GetElementById(String.Format("title[{0}]", id)).GetAttribute("value");
                info.Street = doc.GetElementById(String.Format("street[{0}]", id)).GetAttribute("value");
                info.PostalCode = doc.GetElementById(String.Format("postCode[{0}]", id)).GetAttribute("value");
                info.City = doc.GetElementById(String.Format("city[{0}]", id)).GetAttribute("value");
                info.BillerName = doc.GetElementById(String.Format("billerName[{0}]", id)).GetAttribute("value");
                info.DueDate = doc.GetElementById(String.Format("dueDate[{0}]", id)).GetAttribute("value");
                info.Currency = doc.GetElementById(String.Format("currency[{0}]", id)).GetAttribute("value");
                info.DefinedTransferName = doc.GetElementById(String.Format("definedTransferName[{0}]", id)).GetAttribute("value");
                info.State = State.IDLE;
            } catch {
                return new PaymentInfo();
            }
            return info;
        }

        public static PaymentInfo ValueOf(String s) {
            PaymentInfo info = new PaymentInfo();
            if (s == null) {
                return info;
            }
            try {
                int[] idx = new int[] { 0 };
                info.Id = GetNextField(s, idx);
                String iban = GetNextField(s, idx).ToUpper();
                info.BankAccountNo = GetAccountNoFromIban(iban);
                info.CountryCode = GetCountryCodeFromIban(iban);
                info.Amount = GetNextField(s, idx);
                info.AmountToPay = GetNextField(s, idx);
                info.AmountPaid = GetNextField(s, idx);
                info.Title = GetNextField(s, idx);
                info.Street = GetNextField(s, idx);
                info.PostalCode = GetNextField(s, idx);
                info.City = GetNextField(s, idx);
                info.BillerName = GetNextField(s, idx);
                info.DueDate = GetNextField(s, idx);
                info.Currency = GetNextField(s, idx);
                info.DefinedTransferName = GetNextField(s, idx);
                switch (GetNextField(s, idx)) {
                    case "IN_PROGRESS":
                        info.State = State.IN_PROGRESS;
                        break;
                    case "IN_PROGRESS_CONFIRM":
                        info.State = State.IN_PROGRESS_CONFIRM;
                        break;
                    case "IDLE":
                        info.State = State.IDLE;
                        break;
                    case "PAID":
                        info.State = State.PAID;
                        break;
                    default:
                        info.State = State.INVALID;
                        break;
                }
            } catch {
                return new PaymentInfo();
            }
            return info;
        }

        public override String ToString() {
            StringBuilder sb = new StringBuilder();

            String bankAccount = String.Format("{0}{1}", CountryCode, BankAccountNo);
            Append(id, sb).Append(bankAccount, sb).Append(amount, sb).Append(amountToPay, sb)
                .Append(amountPaid, sb).Append(title, sb).Append(street, sb).Append(postalCode, sb)
                .Append(city, sb).Append(billerName, sb).Append(dueDate, sb).Append(currency, sb)
                .Append(definedTransferName, sb).Append(state.ToString(), sb);

            return sb.ToString();
        }

        public PaymentInfo Clone() {
            return PaymentInfo.ValueOf(this.ToString());
        }
        #endregion

        #region Auxiliary methods
        private static String GetNextField(String s, int[] fromIndex) {
            fromIndex[0] = s.IndexOf("[", fromIndex[0]);
            int len = s.IndexOf("]", fromIndex[0]) - fromIndex[0];
            if (s.IndexOf("][", fromIndex[0]) > -1) {
                len = s.IndexOf("][", fromIndex[0]) - fromIndex[0];
            }
            String result = Unescape(s.Substring(fromIndex[0] + 1, len - 1));
            fromIndex[0] += len;
            return result;
        }

        private static String GetAccountNoFromIban(String iban) {
            if (iban.Trim().Length == 0) {
                return "";
            }
            iban = iban.Trim().Replace(" ", "").Replace("&nbsp;", "");
            if (iban[0] >= 'A' && iban[0] <= 'Z' && iban[1] >= 'A' && iban[1] <= 'Z') {
                return iban.Substring(2, iban.Length - 2);
            }
            return iban;
        }

        private static String GetCountryCodeFromIban(String iban) {
            if (iban.Length > 1 && iban[0] >= 'A' && iban[0] <= 'Z' && iban[1] >= 'A' && iban[1] <= 'Z') {
                return iban.Substring(0, 2);
            }
            return DEFAULT_COUNTRY_CODE;
        }

        private static String Escape(String s) {
            if (s == null) {
                return "";
            }
            return s.Replace("\\", "\\\\").Replace("[", "\\[").Replace("]", "\\]");
        }

        private static String Unescape(String s) {
            return s.Replace("\\]", "]").Replace("\\[", "[").Replace("\\\\", "\\");
        }

        private PaymentInfo Append(String s, StringBuilder sb) {
            sb.Append("[").Append(Escape(s)).Append("]");
            return this;
        }
        #endregion

        #region Properties
        public String Id {
            get {
                return this.id;
            }
            set {
                this.id = value;
            }
        }

        public String BankAccountNo {
            get {
                return this.bankAccountNo;
            }
            set {
                this.bankAccountNo = GetAccountNoFromIban(value);
                this.countryCode = GetCountryCodeFromIban(value);
            }
        }

        public String CountryCode {
            get {
                return this.countryCode;
            }
            set {
                this.countryCode = value;
            }
        }

        public String Amount {
            get {
                return this.amount;
            }
            set {
                this.amount = value;
            }
        }

        public String AmountToPay {
            get {
                return amountToPay;
            }
            set {
                this.amountToPay = value;
                try {
                    this.amountToPayInt = (int)Math.Round(double.Parse(value.Replace(",", "."), CultureInfo.InvariantCulture) * 100d);
                    int tmp = amountToPayInt / 100;
                    this.amountToPayDecimal = tmp.ToString();

                    tmp = amountToPayInt - (tmp * 100);
                    this.amountToPayFloating = tmp.ToString("00");
                } catch {
                    this.amountToPayInt = 0;
                }
            }
        }

        public int AmountToPayInt {
            get {
                return this.amountToPayInt;
            }
            set {
                this.amountToPayInt = value;
                double val = (double)value;
                AmountToPay = (val / 100d).ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        public String AmountToPayDecimal {
            get {
                return this.amountToPayDecimal;
            }
        }

        public String AmountToPayFloating {
            get {
                return this.amountToPayFloating;
            }
        }

        public String AmountPaid {
            get {
                return this.amountPaid;
            }
            set {
                this.amountPaid = value;
                try {
                    this.amountPaidInt = (int)Math.Round(double.Parse(value.Replace(",", "."), CultureInfo.InvariantCulture) * 100d);
                } catch {
                    this.amountPaidInt = 0;
                }
            }
        }

        public int AmountPaidInt {
            get {
                return this.amountPaidInt;
            }
        }

        public String Title {
            get {
                return this.title;
            }
            set {
                this.title = value;
            }
        }

        public String Street {
            get {
                return this.street;
            }
            set {
                this.street = value;
            }
        }

        public String PostalCode {
            get {
                return this.postalCode;
            }
            set {
                this.postalCode = value;
            }
        }

        public String City {
            get {
                return this.city;
            }
            set {
                this.city = value;
            }
        }

        public String PostalCodeAndCity {
            get {
                String tmp = this.city;
                if (this.postalCode.Trim().Length > 0) {
                    tmp = String.Format("{0} {1}", this.postalCode, this.city);
                }
                return tmp;
            }
        }

        public String BillerName {
            get {
                return this.billerName;
            }
            set {
                this.billerName = value;
            }
        }

        public String DueDate {
            set {
                this.dueDate = value;
                try {
                    if (value != null) {
                        this.dueDateTime = DateTime.ParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                    }
                } catch (FormatException) {
                    this.dueDateTime = DateTime.MinValue;
                }
            }
        }
        
        public DateTime DueDateTime {
            get {
                return this.dueDateTime;
            }
        }

        public DateTime DueDateTimeToPaste {
            get {
                return DateTime.Today;
            }
        }

        public String Currency {
            get {
                return this.currency;
            }
            set {
                this.currency = value;
            }
        }

        public String DefinedTransferName {
            get {
                return this.definedTransferName;
            }
            set {
                this.definedTransferName = value;
            }
        }

        public bool IsDefinedTransfer {
            get {
                return this.definedTransferName != null && this.definedTransferName.Trim().Length > 0;
            }
        }

        public State State {
            get {
                return this.state;
            }
            set {
                this.state = value;
            }
        }
        #endregion
    }
}