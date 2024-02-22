using BiskaUtil;
using System;
using System.Collections.Generic;
using System.Text;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueTypeControlExtension
    {
      

        public static bool IsNumber(this object value)
        {
            double sayi;
            return double.TryParse(value.ToString(), out sayi);
        }

        public static bool IsNumberX(this object value)
        {
            double deger;
            var durum = double.TryParse(value.ToStrObj(), out deger);
            return durum;
        }
        public static bool IsAscii(this string value)
        {
            return Encoding.UTF8.GetByteCount(value) == value.Length;
        }
    }
}
