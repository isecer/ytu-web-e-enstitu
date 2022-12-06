using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Models
{

    public static class TDOAyar
    {

        public const string BasvurusuAcikmi = "Başvuru Alımı Açık";
        public const string DanismanMaxOgrenciKayitKriter = "Danışman YL + DR maksimum kayıtlı öğrenci sayısı";
        public const string DanismanMinSinavPuanKabulKriter = "Danışman için Dil Sınavı kabulü min puan";

        public static void setAyarTDO(string AyarAdi, string AyarDegeri, string EnstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.TDOAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKod).FirstOrDefault();
                if (qq != null)
                {
                    qq.AyarDegeri = AyarDegeri;
                }
                else
                {
                    db.TDOAyarlars.Add(new TDOAyarlar { AyarAdi = AyarAdi, AyarDegeri = AyarDegeri });

                }
                db.SaveChanges();
            } 
        }
        public static string getAyarTDO(this string AyarAdi, string EnstituKodu, string VarsayilanDeger = "")
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qq = db.TDOAyarlars.Where(p => p.AyarAdi == AyarAdi && p.EnstituKod == EnstituKodu).FirstOrDefault();
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