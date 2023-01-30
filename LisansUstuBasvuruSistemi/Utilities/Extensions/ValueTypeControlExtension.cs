using System;
using System.Collections.Generic;
using System.Text;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueTypeControlExtension
    {
        public static bool IsNumber(this object value)
        {
            double sayi;
            return double.TryParse(value.ToString(), out sayi);
        }
        public static bool IsNumber2(this object value)
        {
            return value is sbyte
                   || value is byte
                   || value is short
                   || value is ushort
                   || value is int
                   || value is uint
                   || value is long
                   || value is ulong
                   || value is float
                   || value is double
                   || value is decimal;
        }
        public static bool IsNumberX(this object value)
        {
            double Deger;
            var durum = double.TryParse(value.ToStrObj(), out Deger);
            return durum;
        }
        public static bool IsURL(this string source)
        {
            return Uri.IsWellFormedUriString(source, UriKind.RelativeOrAbsolute);
        }

        public static bool IsValidUrl(this string urlString)
        {
            if (urlString.IsNullOrWhiteSpace()) return false;
            Uri uri;
            return Uri.TryCreate(urlString, UriKind.RelativeOrAbsolute, out uri)
                   && (uri.Scheme == Uri.UriSchemeHttp
                       || uri.Scheme == Uri.UriSchemeHttps
                       || uri.Scheme == Uri.UriSchemeFtp
                       || uri.Scheme == Uri.UriSchemeMailto
                   );
        }
        public static bool IsASCII(this string value)
        {
            return Encoding.UTF8.GetByteCount(value) == value.Length;
        }
    }
}
