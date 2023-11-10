using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueConverterExtension
    {
        public static string ToJsonText(this object obj)
        {
            return JsonConvert.SerializeObject(obj); ;
        }
        public static string ToStrObj(this object obj)
        {
            if (obj != null) return Convert.ToString(obj);
            else return (string)null;
        }

        public static string ToStrObjEmptString(this object obj)
        {
            if (obj != null)
            {
                var str = Convert.ToString(obj);
                return str.Trim();
            }
            else return "";
        }
        public static decimal? ToMoney(this string moneyString)
        {
            var groupSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyGroupSeparator;
            var decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;
            return ToMoney(moneyString, decimalSeparator, groupSeparator);
        }
        public static decimal? ToMoney(this string moneyString, string decimalSeparator, string groupSeparator)
        {
            char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            var moneyStr = string.Join("",
                moneyString
                    .ToCharArray()
                    .Where(p => (p.ToString() == groupSeparator || p.ToString() == decimalSeparator || numbers.Contains(p))).ToArray()
            );
            decimal def = 0;
            if (decimal.TryParse(moneyStr, out def)) return def;
            return null;
        }
        public static decimal? ToDecimalObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDecimal(obj);
            else return (decimal?)null;
        }
        public static bool? ToIntToBooleanObj(this object obj)
        {
            var intValue = obj.ToIntObj();
            if (obj != null && intValue.HasValue)
            {
                switch (intValue)
                {
                    case 1:
                        return true;
                    case 0:
                        return false;
                    default:
                        return (bool?)null;
                }
            }

            return (bool?)null;
        }
        public static bool? ToBooleanObj(this object obj)
        {
            bool dgr;
            if (obj != null && bool.TryParse(obj.ToString(), out dgr)) return Convert.ToBoolean(obj);
            return (bool?)null;
        }
        public static double? ToDoubleObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDouble(obj);
            return (double?)null;
        }
        public static int? ToIntObj(this object obj)
        {
            if (obj != null && (obj.IsNumber())) return Convert.ToInt32(obj);
            return (int?)null;
        }
        public static int ToEmptyStringToZero(this object obj)
        {
            int retval = 0;
            if (obj != null && obj.ToString().Trim() != "") retval = obj.ToString().ToInt().Value;
            return retval;
        }
        public static int? ToNullIntZero(this object obj)
        {
            int? retval = null;
            if (obj != null && obj.ToString() != "0") retval = obj.ToString().ToInt();
            return retval;
        }
        public static string ToEmptyStringZero(this object obj)
        {
            string retval = "";
            if (obj != null && obj.ToString() != "0") retval = obj.ToString();
            return retval;
        }
        #region Datetime Convert
        public static string ToFormatDate(this DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "";
            return dateTime == DateTime.MinValue ? "" : dateTime.Value.ToFormatDate();
        }
        public static string ToFormatDate(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("dd.MM.yyyy");
        }
        public static string ToFormatDateAndTime(this DateTime? datetime)
        {
            if (!datetime.HasValue || datetime == DateTime.MinValue) return "";
            return datetime.Value.ToString("dd.MM.yyyy HH:mm");
        }
        public static string ToFormatDateAndTime(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("dd.MM.yyyy HH:mm");
        }

        public static string ToFormatDateDayTime(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("dd.MM.yyyy dddd HH.mm", new CultureInfo("tr-TR"));
        }
        public static string ToFormatDateDayTime(this DateTime? dateTime)
        {
            return dateTime == null || dateTime == DateTime.MinValue ? "" : dateTime.Value.ToString("dd.MM.yyyy dddd HH.mm", new CultureInfo("tr-TR"));
        }
        public static string ToFormatDateInput(this DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "";
            return dateTime == DateTime.MinValue ? "" : dateTime.Value.ToFormatDateInput();
        }
        public static string ToFormatDateInput(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("yyyy-MM-dd");
        }
        public static string ToFormatDateAndTimeInput(this DateTime? datetime)
        {
            if (!datetime.HasValue || datetime == DateTime.MinValue) return "";
            return datetime.Value.ToString("yyyy-MM-dd HH:mm");
        }
        public static string ToFormatDateAndTimeInput(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("yyyy-MM-dd HH:mm");
        }
        public static string ToFormatTime(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            return datetime == DateTime.MinValue ? "" : datetime.Value.ToString("HH.mm");
        }
        public static string ToFormatTime(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("HH.mm");
        }
        public static DateTime TodateToShortDate(this DateTime Tarih)
        {
            var data1 = Tarih.ToDateString().ToDate().Value;
            return data1;
        }
        public static DateTime? TodateToShortDate(this DateTime? Tarih)
        {
            if (Tarih != null) return Tarih.ToDateString().ToDate().Value;
            else return null;
        }
        #endregion
       
        public static string ToAsilYedek(this bool? durum)
        {
            string cins;
            if (!durum.HasValue) cins = "-";
            else if (durum.Value) cins = "Asil";
            else cins = "Yedek";
            return cins;
        }
        public static string ToTiDegerlendirmeSonucu(bool? isOyBirligiOrCoklugu, bool? isBasariliOrBasarisiz)
        {
            var returnSonuc = "";

            if (isOyBirligiOrCoklugu.HasValue && isBasariliOrBasarisiz.HasValue)
            {
                returnSonuc += isOyBirligiOrCoklugu.Value ? "Oy Birliği ile" : "Oy Çokluğu ile";
                returnSonuc += isBasariliOrBasarisiz.Value ? " Başarılı" : " Başarısız";

            }
            return returnSonuc;
        }


        public static string ToKullaniciResim(this string resimAdi)
        {
            var rsm = resimAdi.IsNullOrWhiteSpace() ? ("/" + SistemAyar.KullaniciDefaultResim) : ("/" + SistemAyar.KullaniciResimYolu + "/" + resimAdi);
            return rsm;
        }

       
        public static DateTime ToGetBitisTarihi(this DateTime baslangicTarihi, int ay)
        {
            // İki tarih arasındaki toplam ay süresini hesaplayın
            var bitisTarihi = baslangicTarihi.AddMonths(ay);


            return bitisTarihi;

        }

    }
}
