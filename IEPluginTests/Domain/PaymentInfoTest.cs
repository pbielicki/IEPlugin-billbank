using System;
using System.Windows.Forms;
using NUnit.Mocks;
using NUnit.Framework;
using Billbank.IEPlugin.Domain;

namespace IEPluginTests.Domain {
    [TestFixture]
    class PaymentInfoTest {

        private PaymentInfo BuildDefaultPaymentInfo() {
            PaymentInfo info = new PaymentInfo();
            info.Amount = "10,23";
            info.AmountToPay = "11,24";
            info.AmountPaid = "11.10";
            info.BankAccountNo = "DE123456778900";
            info.BillerName = "Biller Name";
            info.City = "[Some Very Long Name of The\\City]";
            info.Currency = "PLN";
            info.DefinedTransferName = "EMPTY";
            info.DueDate = "13.10.2009";
            info.Id = "200566";
            info.PostalCode = "80-111";
            info.Street = "[[Original]][[Street]][[Name]]";
            info.Title = "SomeTitle";
            info.State = State.IN_PROGRESS;

            return info;
        }

        [Test]
        public void ShouldCreateValidPaymentInfoWithEmptyBankAccountNo() {
            PaymentInfo info = BuildDefaultPaymentInfo();
            info.BankAccountNo = "";

            Assert.AreEqual(State.IN_PROGRESS, info.State);
            Assert.AreEqual("", info.BankAccountNo);
            Assert.AreEqual("PL", info.CountryCode);
        }

        [Test]
        public void ShouldClonePaymentInfo() {
            PaymentInfo info = BuildDefaultPaymentInfo();
            PaymentInfo clone = info.Clone();

            Assert.AreNotSame(info, clone);
            Assert.AreEqual(info.ToString(), clone.ToString());
        }

        [Test]
        public void ShouldSplitAmountToDecimalAndFloatingPart() {
            PaymentInfo info = new PaymentInfo();
            info.AmountToPay = "10,12";
            Assert.AreEqual("10", info.AmountToPayDecimal);
            Assert.AreEqual("12", info.AmountToPayFloating);

            info.AmountToPay = "100.01";
            Assert.AreEqual("100", info.AmountToPayDecimal);
            Assert.AreEqual("01", info.AmountToPayFloating);

            info.AmountToPay = "23456,3";
            Assert.AreEqual("23456", info.AmountToPayDecimal);
            Assert.AreEqual("30", info.AmountToPayFloating);

            info.AmountToPay = "1287.6";
            Assert.AreEqual("1287", info.AmountToPayDecimal);
            Assert.AreEqual("60", info.AmountToPayFloating);

            info.AmountToPay = "0,45";
            Assert.AreEqual("0", info.AmountToPayDecimal);
            Assert.AreEqual("45", info.AmountToPayFloating);

            info.AmountToPay = "10,99";
            Assert.AreEqual("10", info.AmountToPayDecimal);
            Assert.AreEqual("99", info.AmountToPayFloating);
        }

        [Test]
        public void ShouldEscapeHtmlCharactersFromAccountNumber() {
            PaymentInfo info = new PaymentInfo();
            info.BankAccountNo = "10&nbsp;20 50&nbsp;6070&nbsp;90 10";

            Assert.AreEqual("10205060709010", info.BankAccountNo);
        }

        [Test]
        public void ShouldReturnPostalCodeAndCity() {
            PaymentInfo info = new PaymentInfo();
            info.City = "City name";
            info.PostalCode = "";
            Assert.AreEqual("City name", info.PostalCodeAndCity);

            info.City = "City name";
            info.PostalCode = "12-123";
            Assert.AreEqual("12-123 City name", info.PostalCodeAndCity);
        }

        [Test]
        public void ShouldConvertIntToFloatingString() {
            PaymentInfo info = new PaymentInfo();
            info.AmountToPayInt = 123456;
            Assert.AreEqual("1234.56", info.AmountToPay);

            info.AmountToPayInt = 5;
            Assert.AreEqual("0.05", info.AmountToPay);

            info.AmountToPayInt = 17;
            Assert.AreEqual("0.17", info.AmountToPay);

            info.AmountToPayInt = 1200;
            Assert.AreEqual("12.00", info.AmountToPay);

            info.AmountToPayInt = 0;
            Assert.AreEqual("0.00", info.AmountToPay);
        }

        [Test]
        public void ShouldRemoveSpacesFromBankAccountNo() {
            PaymentInfo info = new PaymentInfo();
            info.BankAccountNo = " PL 40 1140 2004 1234 3102 4567 3413 ";

            Assert.AreEqual("40114020041234310245673413", info.BankAccountNo);
        }

        [Test]
        public void ShouldNotRemoveSpecialCharacters() {
            PaymentInfo info = new PaymentInfo();
            info.Title = "!@#$%^&*t()_+}e{|\"s:?><t";

            Assert.AreEqual("!@#$%^&*t()_+}e{|\"s:?><t", info.Title);
        }

        [Test]
        public void ShouldCreateValidDateTime() {
            PaymentInfo info = new PaymentInfo();
            info.DueDate = "13.10.2009";
            Assert.AreEqual(13, info.DueDateTime.Day);
            Assert.AreEqual(10, info.DueDateTime.Month);
            Assert.AreEqual(2009, info.DueDateTime.Year);

            info.DueDate = "02.05.2011";
            Assert.AreEqual(2, info.DueDateTime.Day);
            Assert.AreEqual(5, info.DueDateTime.Month);
            Assert.AreEqual(2011, info.DueDateTime.Year);
        }

        [Test]
        public void ShouldCreateValidAmounts() {
            PaymentInfo info = new PaymentInfo();
            info.AmountToPay = "1300,23";
            info.AmountPaid = "12.45";
            Assert.AreEqual(130023, info.AmountToPayInt);
            Assert.AreEqual(1245, info.AmountPaidInt);

            info = new PaymentInfo();
            info.AmountToPay = "900.73";
            info.AmountPaid = "243,45";
            Assert.AreEqual(90073, info.AmountToPayInt);
            Assert.AreEqual(24345, info.AmountPaidInt);

            info = new PaymentInfo();
            info.AmountToPay = "";
            info.AmountPaid = "";
            Assert.AreEqual(0, info.AmountToPayInt);
            Assert.AreEqual(0, info.AmountPaidInt);        
        }

        [Test]
        public void ShouldCreateStringFromPaymentInfo() {
            PaymentInfo info = BuildDefaultPaymentInfo();

            Assert.AreEqual("[200566][DE123456778900][10,23][11,24][11.10][SomeTitle][\\[\\[Original\\]\\]\\[\\[Street\\]\\]\\[\\[Name\\]\\]]"
                + "[80-111][\\[Some Very Long Name of The\\\\City\\]][Biller Name][13.10.2009][PLN][EMPTY][IN_PROGRESS]", info.ToString());

            PaymentInfo newOne = PaymentInfo.ValueOf(info.ToString());
            Assert.AreEqual(info.CountryCode, newOne.CountryCode);
            Assert.AreEqual(info.Street, newOne.Street);
            Assert.AreEqual(info.City, newOne.City);
            Assert.AreEqual(newOne.ToString(), info.ToString());
            Assert.AreEqual(State.IN_PROGRESS, info.State);
            Assert.AreEqual(State.IN_PROGRESS, newOne.State);
            Assert.IsTrue(newOne.IsDefinedTransfer);
        }

        [Test]
        public void ShouldCreateStringFromNotFullPaymentInfo() {
            PaymentInfo info = new PaymentInfo();
            info.AmountToPay = "11,24";
            info.BankAccountNo = "123456778900";
            info.BillerName = "Biller Name";
            info.Currency = "PLN";
            info.DueDate = "13.10.2009";
            info.Id = "200566";
            info.PostalCode = "80-111";
            info.Title = "SomeTitle";

            Assert.AreEqual("[200566][PL123456778900][][11,24][][SomeTitle][]"
                + "[80-111][][Biller Name][13.10.2009][PLN][][INVALID]", info.ToString());

            PaymentInfo newOne = PaymentInfo.ValueOf(info.ToString());
            Assert.AreEqual("", newOne.Street);
            Assert.AreEqual("", newOne.City);
            Assert.AreEqual(newOne.ToString(), info.ToString());
            Assert.AreEqual(State.INVALID, newOne.State);
            Assert.IsFalse(newOne.IsDefinedTransfer);
            Assert.IsFalse(info.IsDefinedTransfer);
        }
        
        [Test]
        public void ShouldCreatePaymentInfoFromString() {
            String infoAsString = "[098765][PL92837465][120,23][211,24][200.10][Title][\\[\\[Original\\]\\]\\[\\[Street\\]\\]\\[\\[Name\\]\\]]"
                + "[66111][\\[Some Very Long Name of The\\\\City\\]][Biller][13.10.2010][EUR][CASH_FLOW][INVALID]";
            PaymentInfo info = PaymentInfo.ValueOf(infoAsString);

            Assert.AreEqual("PL", info.CountryCode);
            Assert.AreEqual("098765", info.Id);
            Assert.AreEqual("92837465", info.BankAccountNo);
            Assert.AreEqual("120,23", info.Amount);
            Assert.AreEqual("211,24", info.AmountToPay);
            Assert.AreEqual(21124, info.AmountToPayInt);
            Assert.AreEqual("200.10", info.AmountPaid);
            Assert.AreEqual(20010, info.AmountPaidInt);
            Assert.AreEqual("Title", info.Title);
            Assert.AreEqual("[[Original]][[Street]][[Name]]", info.Street);
            Assert.AreEqual("66111", info.PostalCode);
            Assert.AreEqual("[Some Very Long Name of The\\City]", info.City);
            Assert.AreEqual("Biller", info.BillerName);
            Assert.AreEqual("EUR", info.Currency);
            Assert.AreEqual("CASH_FLOW", info.DefinedTransferName);
            Assert.AreEqual(State.INVALID, info.State);

            Assert.AreEqual(infoAsString, info.ToString());
        }

        [Test]
        public void ShouldCreateEmptyPaymentInfoFromString() {
            String infoAsString = "[098765][FR1234567890][][][][][][][][][][][][IN_PROGRESS_CONFIRM]";
            PaymentInfo info = PaymentInfo.ValueOf(infoAsString);

            Assert.AreEqual("098765", info.Id);
            Assert.AreEqual("1234567890", info.BankAccountNo);
            Assert.AreEqual("FR", info.CountryCode);
            Assert.AreEqual("", info.Amount);
            Assert.AreEqual("", info.AmountToPay);
            Assert.AreEqual("", info.Title);
            Assert.AreEqual("", info.Street);
            Assert.AreEqual("", info.PostalCode);
            Assert.AreEqual("", info.City);
            Assert.AreEqual("", info.BillerName);
            Assert.AreEqual("", info.Currency);
            Assert.AreEqual("", info.DefinedTransferName);
            Assert.AreEqual(infoAsString, info.ToString());
            Assert.AreEqual(State.IN_PROGRESS_CONFIRM, info.State);
        }

        [Test]
        public void ShouldCreateCorrectStates() {
            String infoAsString = "[123][ES0927465][][][][][][][][][][][][PAID]";
            PaymentInfo info = PaymentInfo.ValueOf(infoAsString);
            Assert.AreEqual(State.PAID, info.State);
            Assert.AreEqual(infoAsString, info.ToString());

            infoAsString = "[123][ES0927465][][][][][][][][][][][][IDLE]";
            info = PaymentInfo.ValueOf(infoAsString);
            Assert.AreEqual(State.IDLE, info.State);
            Assert.AreEqual(infoAsString, info.ToString());

            infoAsString = "[123][ES0927465][][][][][][][][][][][][IN_PROGRESS]";
            info = PaymentInfo.ValueOf(infoAsString);
            Assert.AreEqual(State.IN_PROGRESS, info.State);
            Assert.AreEqual(infoAsString, info.ToString());

            infoAsString = "[123][ES0927465][][][][][][][][][][][][IN_PROGRESS_CONFIRM]";
            info = PaymentInfo.ValueOf(infoAsString);
            Assert.AreEqual(State.IN_PROGRESS_CONFIRM, info.State);
            Assert.AreEqual(infoAsString, info.ToString());

            infoAsString = "[123][ES0927465][][][][][][][][][][][][INVALID]";
            info = PaymentInfo.ValueOf(infoAsString);
            Assert.AreEqual(State.INVALID, info.State);
            Assert.AreEqual(infoAsString, info.ToString());        
        }

        [Test]
        public void ShouldCreateNullPaymentInfoFromString() {
            String infoAsString = "[098765]";
            PaymentInfo info = PaymentInfo.ValueOf(infoAsString);
            Assert.IsNull(info.Id);
            Assert.AreEqual("[][][][][][][][][][][][][][INVALID]", info.ToString());
            Assert.AreEqual(State.INVALID, info.State);
        }
    }
}
