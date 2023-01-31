using System;
using System.Collections.Generic;
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
    

        public static string ToKullaniciResim(this string resimAdi)
        {
            var rsm = resimAdi.IsNullOrWhiteSpace() ? ("/" + SistemAyar.KullaniciDefaultResim) : ("/" + SistemAyar.KullaniciResimYolu + "/" + resimAdi);
            return rsm;
        }
    }
}
