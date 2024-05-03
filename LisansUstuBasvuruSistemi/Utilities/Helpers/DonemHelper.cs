using System;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class DonemHelper
    {
        public static EgitimOgretimDonemDetayDto ToTiAraRaporDonemBilgi(this DateTime date)
        {
            var egitimOgretimDonemDetayDto = new EgitimOgretimDonemDetayDto();
            if (date.Month <= 6)
            {
                egitimOgretimDonemDetayDto.BaslangicYil = date.Year - 1;
                egitimOgretimDonemDetayDto.DonemId = 2;
                egitimOgretimDonemDetayDto.DonemAdi = "Bahar"; 
                egitimOgretimDonemDetayDto.BaslangicTarihi = new DateTime(date.Year, 1, 1);
                egitimOgretimDonemDetayDto.BitisTarihi = new DateTime(date.Year, 7, 1).AddDays(-1);
            }
            else
            {
                egitimOgretimDonemDetayDto.BaslangicYil = date.Year;
                egitimOgretimDonemDetayDto.BaslangicTarihi = new DateTime(date.Year, 7, 1);
                egitimOgretimDonemDetayDto.BitisTarihi = new DateTime(date.Year + 1, 1, 1).AddDays(-1);
                egitimOgretimDonemDetayDto.DonemId = 1;
                egitimOgretimDonemDetayDto.DonemAdi = "Güz"; 
            }

            return egitimOgretimDonemDetayDto;
        }

        public static EgitimOgretimDonemDto ToAkademikDonemBilgi(this DateTime date)
        { 
            var egitimOgretimDonemDto = new EgitimOgretimDonemDto(); 
            if (egitimOgretimDonemDto.BaharDonemiAylar.Contains(date.Month))
            {
                egitimOgretimDonemDto.BaslangicYil = date.Year - 1;
                egitimOgretimDonemDto.DonemId = AkademikDonemEnum.BaharYariyili;
                egitimOgretimDonemDto.DonemAdi = "Bahar";
            }
            else
            {
                egitimOgretimDonemDto.BaslangicYil = date.Year;
                egitimOgretimDonemDto.DonemId = AkademikDonemEnum.GuzYariyili;
                egitimOgretimDonemDto.DonemAdi = "Güz";
            }
            return egitimOgretimDonemDto;
        }


        public static EgitimOgretimDonemDto ToDonemProjesiDonemBilgi(this DateTime date,string enstituKod)
        {
           
            var egitimOgretimDonemDto = new EgitimOgretimDonemDto
            {
                BaharDonemiAylar = DonemProjesiAyar.GetBaharDonemiIcinSecilenAyNos(enstituKod)
            };
            if (egitimOgretimDonemDto.BaharDonemiAylar.Contains(date.Month))
            {
                egitimOgretimDonemDto.BaslangicYil = date.Year - 1;
                egitimOgretimDonemDto.DonemId = AkademikDonemEnum.BaharYariyili;
                egitimOgretimDonemDto.DonemAdi = "Bahar";
            }
            else
            {
                egitimOgretimDonemDto.BaslangicYil = date.Year;
                egitimOgretimDonemDto.DonemId = AkademikDonemEnum.GuzYariyili;
                egitimOgretimDonemDto.DonemAdi = "Güz";
            }
            return egitimOgretimDonemDto;
        }

    }
}