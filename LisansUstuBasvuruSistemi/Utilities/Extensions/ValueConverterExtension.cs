using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueConverterExtension
    {
        public static string toEmptyStringZero(this object obj)
        {
            string retval = "";
            if (obj != null && obj.ToString() != "0") retval = obj.ToString();
            return retval;
        }
        public static int? toNullIntZero(this object obj)
        {
            int? retval = null;
            if (obj != null && obj.ToString() != "0") retval = obj.ToString().ToInt();
            return retval;
        }
        public static int? ToInt(this string String)
        {
            int result = 0;
            return int.TryParse(String, out result) ? new int?(result) : new int?();
        }
        public static int? toIntObj(this object obj)
        {
            if (obj != null && (obj.IsNumber())) return Convert.ToInt32(obj);
            else return (int?)null;
        }
        public static double? toDoubleObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDouble(obj);
            else return (double?)null;
        }
        public static bool? toBooleanObj(this object obj)
        {
            bool dgr;
            if (obj != null && bool.TryParse(obj.ToString(), out dgr)) return Convert.ToBoolean(obj);
            else return (bool?)null;
        }
        public static bool? toIntToBooleanObj(this object obj)
        {
            var IntValue = obj.toIntObj();
            if (obj != null && IntValue.HasValue)
            {

                if (IntValue == 1) return true;
                else if (IntValue == 0) return false;
                else return (bool?)null;
            }
            else return (bool?)null;
        }
        public static decimal? toDecimalObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDecimal(obj);
            else return (decimal?)null;
        }
        public static string toStrObj(this object obj)
        {
            if (obj != null) return Convert.ToString(obj);
            else return (string)null;
        }
        public static string toStrObjEmptString(this object obj)
        {
            if (obj != null)
            {
                var Str = Convert.ToString(obj);
                return Str.Trim();
            }
            else return "";
        }
        public static decimal? ToMoney(this string moneyString)
        {
            var groupSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyGroupSeparator;
            var decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;
            return ToMoney(moneyString, decimalSeparator, groupSeparator);
        }
        public static decimal ToMoney(this string moneyString, decimal defaultValue)
        {
            var ms = ToMoney(moneyString);
            return (ms.HasValue ? ms.Value : defaultValue);
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
        public static string ToFormatDate(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            if (datetime == DateTime.MinValue) return "";
            else return datetime.Value.ToString("dd.MM.yyyy");

        }
        public static string ToFormatDate(this DateTime datetime)
        {
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("dd.MM.yyyy");

        }
        public static string ToFormatDateAndTime(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            if (datetime == DateTime.MinValue) return "";
            else return datetime.Value.ToString("dd.MM.yyyy HH:mm");

        }
        public static string ToFormatDateAndTime(this DateTime datetime)
        {
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("dd.MM.yyyy HH:mm");

        }
        public static string ToFormatTime(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            if (datetime == DateTime.MinValue) return "";
            else return datetime.Value.ToString("HH.mm");

        }
        public static string ToFormatTime(this DateTime datetime)
        {
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("HH.mm");

        }
        public static string toJsonText(this object obj)
        {  
            return JsonConvert.SerializeObject(obj); ;
        }
        public static string ToBelirtilmemis(this int? Sayi)
        {
            if (!Sayi.HasValue) return "Belirtilmemiş";
            else return Sayi.Value.ToString();

        }
        public static string ToCinsiyet(this int? Sayi)
        {
            var cins = "";
            if (!Sayi.HasValue) cins = "Belirtilmemiş";
            else if (Sayi == 1) cins = "Erkek";
            else if (Sayi == 2) cins = "Kadın";
            else cins = Sayi.Value.ToString();
            return cins;

        }
        public static string ToEvliBekar(this bool? durum)
        {
            var cins = "";
            if (!durum.HasValue) cins = "Belirtilmemiş";
            else if (durum.Value) cins = "Evli";
            else if (!durum.Value) cins = "Bekar";
            else cins = durum.Value.ToString();
            return cins;
        }
        public static string ToAsilYedek(this bool? durum)
        {
            var cins = "";
            if (!durum.HasValue) cins = "-";
            else if (durum.Value) cins = "Asil";
            else if (!durum.Value) cins = "Yedek";
            else cins = durum.Value.ToString();
            return cins;
        }
        public static string toTIDegerlendirmeSonucu(bool? IsOyBirligiOrCouklugu, bool? IsBasariliOrBasarisiz)
        {
            string ReturnSonuc = "";

            if (IsOyBirligiOrCouklugu.HasValue && IsBasariliOrBasarisiz.HasValue)
            {
                ReturnSonuc += IsOyBirligiOrCouklugu.Value ? "Oy Birliği ile" : "Oy Çokluğu ile";
                ReturnSonuc += IsBasariliOrBasarisiz.Value ? " Başarılı" : " Başarısız";

            }
            return ReturnSonuc;
        }
        public static string ToMezuniyetJuriUnvanAdi(this string UnvanAdi)
        {
            UnvanAdi = UnvanAdi.Trim().ToLower().Replace("  ", ".").Replace(". ", ".").Replace(" .", ".").Replace(" ", ".");
            var ProfUnvan = new List<string> { "PROFESÖR".ToLower(), "PROFESÖR.DR".ToLower(), "PROF.DR.".ToLower(), "Prof.".ToLower() };
            var DocUnvan = new List<string> { "DOÇENT".ToLower(), "DOÇENT.DR".ToLower(), "Doç.".ToLower() };
            var OgUyeUnvan = new List<string> { "DR.ÖĞR.ÜYE".ToLower(), "DR.ÖĞR.ÜYESİ".ToLower(), "DR.ÖĞRETİM.ÜYE".ToLower(), "DR.ÖĞRETİM.ÜYESİ".ToLower() };
            if (ProfUnvan.Any(a => a.Contains(UnvanAdi))) return "PROF.DR.";
            else if (DocUnvan.Any(a => a.Contains(UnvanAdi))) return "DOÇ.DR.";
            else if (OgUyeUnvan.Any(a => a.Contains(UnvanAdi))) return "DR.ÖĞR.ÜYE.";
            else return UnvanAdi.ToUpper();
        }

        public static string toKullaniciResim(this string ResimAdi)
        {

            var rsm = ResimAdi.IsNullOrWhiteSpace() ? ("/" + SistemAyar.KullaniciDefaultResim) : ("/" + SistemAyar.KullaniciResimYolu + "/" + ResimAdi);
            return rsm;
        }
    }
}
