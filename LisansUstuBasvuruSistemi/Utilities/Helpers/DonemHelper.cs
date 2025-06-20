using System;
using Entities.Entities;
using System.Collections.Generic;
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
                var year = date.Year;
                //1. ay ve 3. ay dahilinde ise bir önceki yıl dönem başlangıç yılı olmalı
                if (date.Month >= 1 && date.Month <= 3) year = date.Year - 1;
                egitimOgretimDonemDto.BaslangicYil = year;
                egitimOgretimDonemDto.DonemId = AkademikDonemEnum.GuzYariyili;
                egitimOgretimDonemDto.DonemAdi = "Güz";
            }
            return egitimOgretimDonemDto;
        }
        public static EgitimOgretimDonemDto ToKsAkademikDonemBilgi(this DateTime date)
        { 
            var donemId = KayitSilmeAyar
                .GetDonemId(date, null).Value;
            var egitimOgretimDonemDto = new EgitimOgretimDonemDto();
            if (donemId == AkademikDonemEnum.BaharYariyili)
            {
                egitimOgretimDonemDto.BaslangicYil = date.Year - 1;
                egitimOgretimDonemDto.DonemId = AkademikDonemEnum.BaharYariyili;
                egitimOgretimDonemDto.DonemAdi = "Bahar";
            }
            else
            {
                var year = date.Year;
                //1. ay ve 3. ay dahilinde ise bir önceki yıl dönem başlangıç yılı olmalı
                if (date.Month >= 1 && date.Month <= 3) year = date.Year - 1;
                egitimOgretimDonemDto.BaslangicYil = year;
                egitimOgretimDonemDto.DonemId = AkademikDonemEnum.GuzYariyili;
                egitimOgretimDonemDto.DonemAdi = "Güz";
            }
            return egitimOgretimDonemDto;
        }


        public static List<CmbStringDto> GetCmbAkademikTarih(string baslangicDonem, string bitisDonem)
        {
            var lst = new List<CmbStringDto>();
            List<CmbIntDto> donems;

            // Get terms from database
            using (var entities = new LubsDbEntities())
            {
                donems = entities.Donemlers
                    .OrderBy(o => o.DonemID)
                    .Select(s => new CmbIntDto { Value = s.DonemID, Caption = s.DonemAdi })
                    .ToList();
            }

            // Parse start and end periods
            int baslangicYil = int.Parse(baslangicDonem.Substring(0, 4));
            int baslangicDonemId = int.Parse(baslangicDonem.Substring(4));
            int bitisYil = int.Parse(bitisDonem.Substring(0, 4));
            int bitisDonemId = int.Parse(bitisDonem.Substring(4));

            // Create year-term pairs and sort them
            var donemler = new List<KeyValuePair<int, int>>();

            for (int yil = baslangicYil; yil <= bitisYil; yil++)
            {
                if (yil == baslangicYil && yil == bitisYil)
                {
                    // If same year, add terms between start and end period
                    for (int donemId = baslangicDonemId; donemId <= bitisDonemId; donemId++)
                    {
                        donemler.Add(new KeyValuePair<int, int>(yil, donemId));
                    }
                }
                else if (yil == baslangicYil)
                {
                    // For start year, add terms from start period to end of year
                    for (int donemId = baslangicDonemId; donemId <= 2; donemId++)
                    {
                        donemler.Add(new KeyValuePair<int, int>(yil, donemId));
                    }
                }
                else if (yil == bitisYil)
                {
                    // For end year, add terms from start of year to end period
                    for (int donemId = 1; donemId <= bitisDonemId; donemId++)
                    {
                        donemler.Add(new KeyValuePair<int, int>(yil, donemId));
                    }
                }
                else
                {
                    // For years in between, add both terms
                    donemler.Add(new KeyValuePair<int, int>(yil, 1));
                    donemler.Add(new KeyValuePair<int, int>(yil, 2));
                }
            }

            // Sort periods in descending order and create result list
            foreach (var donem in donemler.OrderByDescending(d => d.Key).ThenByDescending(d => d.Value))
            {
                var donemAdi = donems.First(p => p.Value == donem.Value).Caption;
                lst.Add(new CmbStringDto
                {
                    Value = $"{donem.Key}{donem.Value}",
                    Caption = $"{donem.Key}/{donem.Key + 1} {donemAdi}"
                });
            }

            return lst;
        }


        public static int GetDonemFark(this string baslangicDonem, string aktifDonem)
        {
            int baslangicYil = int.Parse(baslangicDonem.Substring(0, 4));
            int baslangicDonemNo = int.Parse(baslangicDonem.Substring(4, 1));

            int aktifYil = int.Parse(aktifDonem.Substring(0, 4));
            int aktifDonemNo = int.Parse(aktifDonem.Substring(4, 1));

            return ((aktifYil - baslangicYil) * 2) + (aktifDonemNo - baslangicDonemNo);
        }
    }
}