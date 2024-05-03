using System;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public class CreditCardModel
    {
        public string HolderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpireMonth { get; set; }
        public string ExpireYear { get; set; }
        public string Cv2 { get; set; }
        public int? MaximumTipId { get; set; }
        public int? Taksit { get; set; }
    }
    public class ThreeDHelper
    {
        public static string PrepareForm(string actionUrl, NameValueCollection collection)
        {
            const string formId = "PaymentForm";
            StringBuilder strForm = new StringBuilder();
            strForm.Append("<form id=\"" + formId + "\" name=\"" + formId + "\" action=\"" + actionUrl + "\" method=\"POST\">");

            foreach (string key in collection)
            {
                strForm.Append("<input type=\"hidden\" name=\"" + key + "\" value=\"" + collection[key] + "\">");
            }

            strForm.Append("</form>");
            StringBuilder strScript = new StringBuilder();
            strScript.Append("<script>");
            strScript.Append("var v" + formId + " = document." + formId + ";");
            strScript.Append("v" + formId + ".submit();");
            strScript.Append("</script>");

            return strForm + strScript.ToString();
        }

        public static string ConvertSha1(string text)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] inputbytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));

            return Convert.ToBase64String(inputbytes);
        }

        public static string CreateRandomValue(int length, bool charactersB, bool charactersS, bool isNumbers, bool specialCharacters)
        {
            const string characters_b = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string characters_s = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string special_characters = "-_*+/";
            var allowedChars = String.Empty;

            if (charactersB)
                allowedChars += characters_b;

            if (charactersS)
                allowedChars += characters_s;

            if (isNumbers)
                allowedChars += numbers;

            if (specialCharacters)
                allowedChars += special_characters;

            var chars = new char[length];
            var rd = new Random();

            for (int i = 0; i < length; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }

        public static NameValueCollection ValueCollection(CreditCardModel cardModel = null, string siparisNo = "", string defaultUrl = "", string amount = "")
        {
            if (cardModel == null)
                cardModel = new CreditCardModel
                {
                    CardNumber = "4546711234567894",
                    ExpireMonth = "12",
                    ExpireYear = "18",
                    Cv2 = "000",
                    HolderName = "Test"

                };

            var processType = "Auth";//İşlem tipi
            var clientId = "190300000";//Mağaza numarası
            var storeKey = "123456";//Mağaza anahtarı
            var storeType = "3d_pay_hosting";//SMS onaylı ödeme modeli 3DPay olarak adlandırılıyor.
            var successUrl = defaultUrl + "OnlineOdeme/Success";//Başarılı Url
            var unsuccessUrl = defaultUrl + "OnlineOdeme/UnSuccess";//Hata Url
            var randomKey = ThreeDHelper.CreateRandomValue(10, false, false, true, false);
            var installment = "";//Taksit
            var orderNumber = siparisNo ?? ThreeDHelper.CreateRandomValue(8, false, false, true, false);//Sipariş numarası
            var currencyCode = "949"; //TL ISO code | EURO "978" | Dolar "840"
            var languageCode = "tr";// veya "en"
            var cardType = "1"; //Kart Ailesi Visa 1 | MasterCard 2 | Amex 3
            var orderAmount = amount;//Decimal seperator nokta olmalı!

            //Güvenlik amaçlı olarak birleştirip şifreliyoruz. Banka decode edip bilgilerin doğruluğunu kontrol ediyor. Alanların sırasına dikkat etmeliyiz.
            var hashFormat = clientId + orderNumber + orderAmount + successUrl + unsuccessUrl + processType + installment + randomKey + storeKey;
            var paymentCollection = new NameValueCollection
            {
                //Mağaza bilgileri
                { "hash", ThreeDHelper.ConvertSha1(hashFormat) },
                { "clientid", clientId },
                { "storetype", storeType },
                { "rnd", randomKey },
                { "okUrl", successUrl },
                { "failUrl", unsuccessUrl },
                { "islemtipi", processType },
                { "refreshtime", "0" },
                //Ödeme bilgileri
                { "currency", currencyCode },
                { "lang", languageCode },
                { "amount", orderAmount },
                { "oid", orderNumber },
                //Kredi kart bilgileri
                { "pan", cardModel.CardNumber },
                { "cardHolderName", cardModel.HolderName },
                { "cv2", cardModel.Cv2 },
                { "Ecom_Payment_Card_ExpDate_Year", cardModel.ExpireYear },
                { "Ecom_Payment_Card_ExpDate_Month", cardModel.ExpireMonth },
                { "taksit", installment },
                { "cartType", cardType }
            };

            return paymentCollection;
        }
    }

}