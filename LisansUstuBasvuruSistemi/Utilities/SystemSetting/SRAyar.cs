using LisansUstuBasvuruSistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.SystemSetting
{
    public static class SrAyar
    {
        public const string SalonRezervasyonTalebiAcikmi = "Salon Rezervasyon İşlemi Açık";
        public const string SrIslemlerindeMailGonder = "Salon Rezervasyon İşlemlerinde Mail Gönder";

        public static void SetAyarSr(string ayarAdi, string ayarDegeri, string enstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var srAyar = db.SRAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKod);
                if (srAyar != null)
                {
                    srAyar.AyarDegeri = ayarDegeri;
                }
                else
                {
                    db.Ayarlars.Add(new Ayarlar { AyarAdi = ayarAdi, AyarDegeri = ayarDegeri });

                }
                db.SaveChanges();
            }
        }
        public static string GetAyarSr(this string ayarAdi, string enstituKodu, string varsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var srAyar = db.SRAyarlars.FirstOrDefault(p => p.AyarAdi == ayarAdi && p.EnstituKod == enstituKodu);
                if (srAyar != null)
                {
                    return srAyar.AyarDegeri;
                }
                else
                {
                    return varsayilanDeger;

                }
            }
        }
    }
}