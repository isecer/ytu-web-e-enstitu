using System;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class DonemHelper
    {
        public static EgitimOgretimDonemDetayDto ToTiAraRaporDonemBilgi(this DateTime date, string enstituKod)
        {
            var egitimOgretimDonemDetayDto = new EgitimOgretimDonemDetayDto();

            //var secilenAylars = TiAyar.GetBaharDonemiIcinSecilenAyNos(enstituKod);


            //if (secilenAylars.Any())
            //{
            //    if (secilenAylars.Contains(date.Month))
            //    {
            //        egitimOgretimDonemDetayDto.BaslangicYil = date.Year - 1;
            //        egitimOgretimDonemDetayDto.DonemId = AkademikDonemEnum.BaharYariyili;
            //        egitimOgretimDonemDetayDto.DonemAdi = "Bahar";

            //        egitimOgretimDonemDetayDto.BaslangicTarihi = new DateTime(date.Year, secilenAylars.Min(), 1);
            //        egitimOgretimDonemDetayDto.BitisTarihi = new DateTime(date.Year, secilenAylars.Max() + 1, 1).AddDays(-1);
            //    }
            //    else
            //    {
            //        egitimOgretimDonemDetayDto.BaslangicYil = date.Year;
            //        egitimOgretimDonemDetayDto.DonemId = AkademikDonemEnum.GuzYariyili;
            //        egitimOgretimDonemDetayDto.DonemAdi = "Güz";
            //        egitimOgretimDonemDetayDto.BaslangicTarihi = new DateTime(date.Year, secilenAylars.Max() + 1, 1);
            //        egitimOgretimDonemDetayDto.BitisTarihi = new DateTime(date.Year + 1, secilenAylars.Min(), 1).AddDays(-1);
            //    }
            //}

            if (date.Month <= 6)
            {
                egitimOgretimDonemDetayDto.BaslangicYil = date.Year - 1;
                egitimOgretimDonemDetayDto.DonemId = AkademikDonemEnum.BaharYariyili;
                egitimOgretimDonemDetayDto.DonemAdi = "Bahar";
                egitimOgretimDonemDetayDto.BaslangicTarihi = new DateTime(date.Year, 1, 1);
                egitimOgretimDonemDetayDto.BitisTarihi = new DateTime(date.Year, 7, 1).AddDays(-1);
            }
            else
            {
                egitimOgretimDonemDetayDto.BaslangicYil = date.Year;
                egitimOgretimDonemDetayDto.BaslangicTarihi = new DateTime(date.Year, 7, 1);
                egitimOgretimDonemDetayDto.BitisTarihi = new DateTime(date.Year + 1, 1, 1).AddDays(-1);
                egitimOgretimDonemDetayDto.DonemId = AkademikDonemEnum.GuzYariyili;
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


        public static EgitimOgretimDonemDto ToDonemProjesiDonemBilgi(this DateTime date, string enstituKod)
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