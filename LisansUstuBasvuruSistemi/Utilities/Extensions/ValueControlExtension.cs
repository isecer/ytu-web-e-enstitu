using System;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueControlExtension
    {
        public static bool ToIsValidEmail(this string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        public static bool ToIsValidateTckn(this string tcKimlikNo)
        {

            if (tcKimlikNo.Length != 11)
                return false;

            // TC Kimlik No'nun ilk hanesi 0 olamaz.
            if (tcKimlikNo[0] == '0')
                return false;

            // TC Kimlik No'nun ilk 10 hanesi sayı olmalıdır.
            for (int i = 0; i < 10; i++)
            {
                if (!char.IsDigit(tcKimlikNo[i]))
                    return false;
            }

            // TC Kimlik No'nun 11. hanesi, TC Kimlik No'nun 1. ile 10. hanelerinin toplamının 10'a bölümünden kalan olmalıdır.
            int sum = 0;
            for (int i = 0; i < 10; i++)
            {
                sum += int.Parse(tcKimlikNo[i].ToString());
            }

            int digit11 = sum % 10;
            if (digit11 != int.Parse(tcKimlikNo[10].ToString()))
                return false;

            return true;
        }
        public static ObsDonemDto ParseObsDonem(this string donem)
        {
            var donemModel = new ObsDonemDto();

            var parts = donem.Split(' ');

            if (parts.Length == 2)
            {
                var yillar = parts[0].Split('-');
                if (yillar.Length != 2)
                    throw new ArgumentException("Obs öğrenci dönem bilgisinde Geçersiz yıl formatı.");
                var ilkYil = yillar[0].Trim().ToInt();
                var ikinciYil = yillar[1].Trim().ToInt();

                if (!ilkYil.HasValue || !ikinciYil.HasValue)
                    throw new ArgumentException("Obs öğrenci dönem bilgisinde Geçersiz yıl formatı.");

                donemModel.BaslangicYil = ilkYil.Value;
                donemModel.BitisYil = ikinciYil.Value;

                var donemStr = parts[1].Trim();
                donemModel.DonemAdi = donemStr;

                donemModel.DonemNo = donemStr.Equals("Güz", StringComparison.OrdinalIgnoreCase) ? 1 :
                    donemStr.Equals("Bahar", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

                if (donemModel.DonemNo == 0)
                {
                    throw new ArgumentException("Obs öğrenci dönem bilgisinde Geçersiz dönem formatı.");
                }


            }
            else
            {
                throw new ArgumentException("Obs öğrenci dönem bilgisinde Geçersiz dönem formatı.");
            }

            return donemModel;
        }

        public static string IlkHarfiBuyut(this string str)
        {
            // Cümlenin her bir kelimesini alıyoruz
            TextInfo textInfo = new CultureInfo("tr-TR", false).TextInfo;
            return textInfo.ToTitleCase(str.ToLower());
        }
        //public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

        //public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        //public static bool ToIsValidEmail(this string email)
        //{
        //    var isSuccess = !Regex.IsMatch(email,
        //        @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
        //        @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,24}))$",
        //        RegexOptions.IgnoreCase);
        //    if (!isSuccess) isSuccess = !email.IsASCII();
        //    return isSuccess;
        //}
        //public static bool IsSpecialCharacterCheck(this string gelenStr)
        //{
        //    var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
        //    return regexItem.IsMatch(gelenStr);
        //}
        //public static bool IsImage(this string uzanti)
        //{
        //    var imagesTypes = new List<string>
        //    {
        //        "Png",
        //        "Jpg",
        //        "Bmp",
        //        "Tif",
        //        "Gif"
        //    };
        //    return imagesTypes.Contains(uzanti);
        //}

    }
}
