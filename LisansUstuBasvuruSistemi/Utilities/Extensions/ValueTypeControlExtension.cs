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
            double Deger;
            var durum = double.TryParse(value.ToStrObj(), out Deger);
            return durum;
        }
        public static bool IsASCII(this string value)
        {
            return Encoding.UTF8.GetByteCount(value) == value.Length;
        }
    }
}
