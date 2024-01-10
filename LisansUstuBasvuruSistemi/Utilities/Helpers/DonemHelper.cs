using System;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class DonemHelper
    {
        public static EgitimOgretimDonemDetayDto ToAraRaporDonemBilgi(this DateTime date)
        {
            var model = new EgitimOgretimDonemDetayDto();
            if (date.Month <= 6)
            {
                model.BaslangicYil = date.Year - 1;
                model.DonemID = 2;
                model.DonemAdi = "Bahar";
                model.DonemAdiEn = "Spring";
                model.BaslangicTarihi = new DateTime(date.Year, 1, 1);
                model.BitisTarihi = new DateTime(date.Year, 7, 1).AddDays(-1);
            }
            else
            {
                model.BaslangicYil = date.Year;
                model.BaslangicTarihi = new DateTime(date.Year, 7, 1);
                model.BitisTarihi = new DateTime(date.Year + 1, 1, 1).AddDays(-1);
                model.DonemID = 1;
                model.DonemAdi = "Güz";
                model.DonemAdiEn = "Fall";
            }

            return model;
        }
        public static EgitimOgretimDonemDto ToEgitimOgretimYilBilgi(this DateTime datetime)
        {

            var mdl = new EgitimOgretimDonemDto();
            var nowYear = datetime.Year;
            if (datetime.Month >= 2 && datetime.Month <= 8)
            {
                mdl.Donem = 2;
            }
            else
            {
                mdl.BaslangicYili = datetime.Year;
                mdl.BitisYili = datetime.Year + 1;
                mdl.Donem = 1;
            }
            if (datetime.Month <= 8)
            {
                mdl.BaslangicYili = nowYear - 1;
                mdl.BitisYili = nowYear;
            }
            else
            {
                mdl.BaslangicYili = nowYear;
                mdl.BitisYili = nowYear + 1;
            }
            return mdl;
        }
    }
}