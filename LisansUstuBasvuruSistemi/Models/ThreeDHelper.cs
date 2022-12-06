using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace LisansUstuBasvuruSistemi.Models
{
    public class CreditCardModel
    {
        public string HolderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpireMonth { get; set; }
        public string ExpireYear { get; set; }
        public string CV2 { get; set; }
        public int? MaximumTipID { get; set; }
        public int? Taksit { get; set; }
    }
    public class ThreeDHelper
    {
        public static string PrepareForm(string actionUrl, NameValueCollection collection)
        {
            string formID = "PaymentForm";
            StringBuilder strForm = new StringBuilder();
            strForm.Append("<form id=\"" + formID + "\" name=\"" + formID + "\" action=\"" + actionUrl + "\" method=\"POST\">");

            foreach (string key in collection)
            {
                strForm.Append("<input type=\"hidden\" name=\"" + key + "\" value=\"" + collection[key] + "\">");
            }

            strForm.Append("</form>");
            StringBuilder strScript = new StringBuilder();
            strScript.Append("<script>");
            strScript.Append("var v" + formID + " = document." + formID + ";");
            strScript.Append("v" + formID + ".submit();");
            strScript.Append("</script>");

            return strForm.ToString() + strScript.ToString();
        }

        public static string ConvertSHA1(string text)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] inputbytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));

            return Convert.ToBase64String(inputbytes);
        }

        public static string CreateRandomValue(int Length, bool CharactersB, bool CharactersS, bool Numbers, bool SpecialCharacters)
        {
            string characters_b = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string characters_s = "abcdefghijklmnopqrstuvwxyz";
            string numbers = "0123456789";
            string special_characters = "-_*+/";
            string allowedChars = String.Empty;

            if (CharactersB)
                allowedChars += characters_b;

            if (CharactersS)
                allowedChars += characters_s;

            if (Numbers)
                allowedChars += numbers;

            if (SpecialCharacters)
                allowedChars += special_characters;

            char[] chars = new char[Length];
            Random rd = new Random();

            for (int i = 0; i < Length; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }

        public static NameValueCollection valueCollection(CreditCardModel cardModel = null, string SiparisNo = "", string DefaultUrl = "", string Amount = "")
        {
            if (cardModel == null)
                cardModel = new CreditCardModel
                {
                    CardNumber = "4546711234567894",
                    ExpireMonth = "12",
                    ExpireYear = "18",
                    CV2 = "000",
                    HolderName = "Test"

                };

            string processType = "Auth";//İşlem tipi
            string clientId = "190300000";//Mağaza numarası
            string storeKey = "123456";//Mağaza anahtarı
            string storeType = "3d_pay_hosting";//SMS onaylı ödeme modeli 3DPay olarak adlandırılıyor.
            string successUrl = DefaultUrl + "OnlineOdeme/Success";//Başarılı Url
            string unsuccessUrl = DefaultUrl + "OnlineOdeme/UnSuccess";//Hata Url
            string randomKey = ThreeDHelper.CreateRandomValue(10, false, false, true, false);
            string installment = "";//Taksit
            string orderNumber = SiparisNo ?? ThreeDHelper.CreateRandomValue(8, false, false, true, false);//Sipariş numarası
            string currencyCode = "949"; //TL ISO code | EURO "978" | Dolar "840"
            string languageCode = "tr";// veya "en"
            string cardType = "1"; //Kart Ailesi Visa 1 | MasterCard 2 | Amex 3
            string orderAmount = Amount;//Decimal seperator nokta olmalı!

            //Güvenlik amaçlı olarak birleştirip şifreliyoruz. Banka decode edip bilgilerin doğruluğunu kontrol ediyor. Alanların sırasına dikkat etmeliyiz.
            string hashFormat = clientId + orderNumber + orderAmount + successUrl + unsuccessUrl + processType + installment + randomKey + storeKey;
            var paymentCollection = new NameValueCollection();

            //Mağaza bilgileri
            paymentCollection.Add("hash", ThreeDHelper.ConvertSHA1(hashFormat));
            paymentCollection.Add("clientid", clientId);
            paymentCollection.Add("storetype", storeType);
            paymentCollection.Add("rnd", randomKey);
            paymentCollection.Add("okUrl", successUrl);
            paymentCollection.Add("failUrl", unsuccessUrl);
            paymentCollection.Add("islemtipi", processType);
            paymentCollection.Add("refreshtime", "0");
            //Ödeme bilgileri
            paymentCollection.Add("currency", currencyCode);
            paymentCollection.Add("lang", languageCode);
            paymentCollection.Add("amount", orderAmount);
            paymentCollection.Add("oid", orderNumber);
            //Kredi kart bilgileri
            paymentCollection.Add("pan", cardModel.CardNumber);
            paymentCollection.Add("cardHolderName", cardModel.HolderName);
            paymentCollection.Add("cv2", cardModel.CV2);
            paymentCollection.Add("Ecom_Payment_Card_ExpDate_Year", cardModel.ExpireYear);
            paymentCollection.Add("Ecom_Payment_Card_ExpDate_Month", cardModel.ExpireMonth);
            paymentCollection.Add("taksit", installment);
            paymentCollection.Add("cartType", cardType);

            return paymentCollection;
        }
    }

}