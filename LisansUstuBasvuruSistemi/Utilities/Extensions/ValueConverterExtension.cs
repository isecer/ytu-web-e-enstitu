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
        public static string ToEmptyStringZero(this object obj)
        {
            var retval = "";
            if (obj != null && obj.ToString() != "0") retval = obj.ToString();
            return retval;
        }
        public static int? ToNullIntZero(this object obj)
        {
            int? retval = null;
            if (obj != null && obj.ToString() != "0") retval = obj.ToString().ToInt();
            return retval;
        }
        public static int? ToInt(this string str)
        {
            int def = 0;
            if (int.TryParse(str, out def)) return def;
            return null;
        }
        public static int? ToIntObj(this object obj)
        {
            if (obj != null && (obj.IsNumber())) return Convert.ToInt32(obj);
            return null;
        }
        public static double? ToDoubleObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDouble(obj);
            return null;
        }
        public static bool? ToBooleanObj(this object obj)
        {
            bool def = false;
            if (obj!=null && bool.TryParse(obj.ToString(), out def)) return def;
            return null;
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
            return null;
        }
        public static decimal? ToDecimalObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDecimal(obj);
            return null;
        }
        public static string ToStrObj(this object obj)
        {
            if (obj != null) return Convert.ToString(obj);
            return null;
        }
        public static string ToStrObjEmptString(this object obj)
        {
            return obj != null ? Convert.ToString(obj).Trim() : "";
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
            return ms ?? defaultValue;
        }
        public static decimal? ToMoney(this string moneyString, string decimalSeparator, string groupSeparator)
        {
            var numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
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
            return datetime == DateTime.MinValue ? "" : datetime.Value.ToString("dd.MM.yyyy");
        }
        public static string ToFormatDate(this DateTime datetime)
        {
            return datetime == DateTime.MinValue ? "" : datetime.ToString("dd.MM.yyyy");
        }
        public static string ToFormatDateAndTime(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            return datetime == DateTime.MinValue ? "" : datetime.Value.ToString("dd.MM.yyyy HH:mm");
        }
        public static string ToFormatDateAndTime(this DateTime datetime)
        {
            return datetime == DateTime.MinValue ? "" : datetime.ToString("dd.MM.yyyy HH:mm");
        }
        public static string ToFormatTime(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            return datetime == DateTime.MinValue ? "" : datetime.Value.ToString("HH.mm");
        }
        public static string ToFormatTime(this DateTime datetime)
        {
            return datetime == DateTime.MinValue ? "" : datetime.ToString("HH.mm");
        }
        public static string ToJsonText(this object obj)
        {
            return JsonConvert.SerializeObject(obj); ;
        }
        public static string ToBelirtilmemis(this int? sayi)
        {
            return !sayi.HasValue ? "Belirtilmemiş" : sayi.Value.ToString();
        }
        public static string ToCinsiyet(this int? sayi)
        {
            string cins;
            switch (sayi)
            {
                case null:
                    cins = "Belirtilmemiş";
                    break;
                case 1:
                    cins = "Erkek";
                    break;
                case 2:
                    cins = "Kadın";
                    break;
                default:
                    cins = sayi.Value.ToString();
                    break;
            }
            return cins;

        }
        public static string ToEvliBekar(this bool? durum)
        {
            string cins;
            if (!durum.HasValue) cins = "Belirtilmemiş";
            else if (durum.Value) cins = "Evli";
            else cins = "Bekar";
            return cins;
        }
        public static string ToAsilYedek(this bool? durum)
        {
            string cins;
            if (!durum.HasValue) cins = "-";
            else if (durum.Value) cins = "Asil";
            else cins = "Yedek";
            return cins;
        }
        public static string ToTiDegerlendirmeSonucu(bool? isOyBirligiOrCouklugu, bool? isBasariliOrBasarisiz)
        {
            var returnSonuc = "";

            if (isOyBirligiOrCouklugu.HasValue && isBasariliOrBasarisiz.HasValue)
            {
                returnSonuc += isOyBirligiOrCouklugu.Value ? "Oy Birliği ile" : "Oy Çokluğu ile";
                returnSonuc += isBasariliOrBasarisiz.Value ? " Başarılı" : " Başarısız";

            }
            return returnSonuc;
        }
        public static string ToMezuniyetJuriUnvanAdi(this string unvanAdi)
        {
            unvanAdi = unvanAdi.Trim().ToLower().Replace("  ", ".").Replace(". ", ".").Replace(" .", ".").Replace(" ", ".");
            var profUnvan = new List<string> { "PROFESÖR".ToLower(), "PROFESÖR.DR".ToLower(), "PROF.DR.".ToLower(), "Prof.".ToLower() };
            var docUnvan = new List<string> { "DOÇENT".ToLower(), "DOÇENT.DR".ToLower(), "Doç.".ToLower() };
            var ogUyeUnvan = new List<string> { "DR.ÖĞR.ÜYE".ToLower(), "DR.ÖĞR.ÜYESİ".ToLower(), "DR.ÖĞRETİM.ÜYE".ToLower(), "DR.ÖĞRETİM.ÜYESİ".ToLower() };
            if (profUnvan.Any(a => a.Contains(unvanAdi))) return "PROF.DR.";
            else if (docUnvan.Any(a => a.Contains(unvanAdi))) return "DOÇ.DR.";
            else if (ogUyeUnvan.Any(a => a.Contains(unvanAdi))) return "DR.ÖĞR.ÜYE.";
            else return unvanAdi.ToUpper();
        }

        public static string ToKullaniciResim(this string resimAdi)
        {
            var rsm = resimAdi.IsNullOrWhiteSpace() ? ("/" + SistemAyar.KullaniciDefaultResim) : ("/" + SistemAyar.KullaniciResimYolu + "/" + resimAdi);
            return rsm;
        }
    }
}
