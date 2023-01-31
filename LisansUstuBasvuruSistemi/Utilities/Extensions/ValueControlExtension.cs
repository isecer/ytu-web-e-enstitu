using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueControlExtension
    {
        public static bool ToIsValidEmail(this string email)
        {
            var isSuccess = !Regex.IsMatch(email,
                @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,24}))$",
                RegexOptions.IgnoreCase);
            if (!isSuccess) isSuccess = !email.IsASCII();
            return isSuccess;
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
