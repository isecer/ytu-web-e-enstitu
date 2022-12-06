using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class DonemHelper
    {
        public static TarihAralikModel ToAraRaporDonemBilgi(this DateTime date)
        {
            var model = new TarihAralikModel();
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
    }
}