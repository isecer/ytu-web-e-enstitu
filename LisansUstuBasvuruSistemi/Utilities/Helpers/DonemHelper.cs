using System;
using System.Collections.Generic;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class DonemHelper
    {
        public static EgitimOgretimDonemDetayDto ToTiAraRaporDonemBilgi(this DateTime date)
        {
            var model = new EgitimOgretimDonemDetayDto();
            if (date.Month <= 6)
            {
                model.BaslangicYil = date.Year - 1;
                model.DonemId = 2;
                model.DonemAdi = "Bahar"; 
                model.BaslangicTarihi = new DateTime(date.Year, 1, 1);
                model.BitisTarihi = new DateTime(date.Year, 7, 1).AddDays(-1);
            }
            else
            {
                model.BaslangicYil = date.Year;
                model.BaslangicTarihi = new DateTime(date.Year, 7, 1);
                model.BitisTarihi = new DateTime(date.Year + 1, 1, 1).AddDays(-1);
                model.DonemId = 1;
                model.DonemAdi = "Güz"; 
            }

            return model;
        }

        public static EgitimOgretimDonemDto ToAkademikDonemBilgi(this DateTime date)
        { 
            var returnModel = new EgitimOgretimDonemDto(); 
            if (returnModel.BaharDonemiAylar.Contains(date.Month))
            {
                returnModel.BaslangicYil = date.Year - 1;
                returnModel.DonemId = AkademikDonemEnum.BaharYariyili;
                returnModel.DonemAdi = "Bahar";
            }
            else
            {
                returnModel.BaslangicYil = date.Year;
                returnModel.DonemId = AkademikDonemEnum.GuzYariyili;
                returnModel.DonemAdi = "Güz";
            }
            return returnModel;
        }

     
    }
}