using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueControlExtension
    {
        public static bool IsNullOrEmpty(this string String) => string.IsNullOrEmpty(String);

        public static bool IsNullOrWhiteSpace(this string String) => string.IsNullOrWhiteSpace(String);

        public static bool ToIsValidEmail(this string Email)
        {
            bool IsSuccess = !Regex.IsMatch(Email,
                @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,24}))$",
                RegexOptions.IgnoreCase);
            if (!IsSuccess) IsSuccess = !Email.IsASCII();
            return IsSuccess;
        }
        public static bool IsSpecialCharacterCheck(this string gelenStr)
        {
            var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
            return regexItem.IsMatch(gelenStr);
        }
        public static bool IsImage(this string Uzanti)
        {
            var imagesTypes = new List<string>();
            imagesTypes.Add("Png");
            imagesTypes.Add("Jpg");
            imagesTypes.Add("Bmp");
            imagesTypes.Add("Tif");
            imagesTypes.Add("Gif");
            return imagesTypes.Contains(Uzanti);
        }

    }
}
