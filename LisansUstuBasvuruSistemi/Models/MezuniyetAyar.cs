using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Models
{

    public static class MezuniyetAyar
    {

        public const string MezuniyetBasvurusuAcikmi = "Mezuniyet Başvurusu Açık";
        public const string YeniMezuniyetBasvurusundaMailGonder = "Yeni Mezuniyet Başvurusunda Mail Gönder"; 

        public static void setAyarMZ(string AyarAdi, string AyarDegeri, string EnstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.MezuniyetAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKod).FirstOrDefault();
                if (qq != null)
                {
                    qq.AyarDegeri = AyarDegeri;
                }
                else
                {
                    db.MezuniyetAyarlars.Add(new MezuniyetAyarlar { AyarAdi = AyarAdi, AyarDegeri = AyarDegeri });

                }
                db.SaveChanges();
            }

        }
        public static string getAyarMZ(this string AyarAdi, string EnstituKodu, string VarsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.MezuniyetAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKodu).FirstOrDefault();
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