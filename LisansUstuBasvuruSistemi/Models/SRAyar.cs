using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Models
{
    public static class SRAyar
    {
        public const string SalonRezervasyonTalebiAcikmi = "Salon Rezervasyon İşlemi Açık";
        public const string SRIslemlerindeMailGonder = "Salon Rezervasyon İşlemlerinde Mail Gönder";

        public static void setAyarSR(string AyarAdi, string AyarDegeri, string EnstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.SRAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKod).FirstOrDefault();
                if (qq != null)
                {
                    qq.AyarDegeri = AyarDegeri;
                }
                else
                {
                    db.Ayarlars.Add(new Ayarlar { AyarAdi = AyarAdi, AyarDegeri = AyarDegeri });

                }
                db.SaveChanges();
            }
        }
        public static string getAyarSR(this string AyarAdi, string EnstituKodu, string VarsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.SRAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKodu).FirstOrDefault();
                if (qq != null)
                {
                    return qq.AyarDegeri;
                }
                else
                {
                    return VarsayilanDeger;

                }
            }
        }
    }
}