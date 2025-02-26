using System;
using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using static System.String;

namespace LisansUstuBasvuruSistemi.Business
{
    public class DonemlerBus
    {
        public static List<CmbStringDto> GetCmbAkademikTarih(bool bosSecimVar = false, int? eklenecekYil = null)
        {
            var lst = new List<CmbStringDto>();
            List<CmbIntDto> donems;
            if (bosSecimVar) lst.Add(new CmbStringDto { Value = "", Caption = "" });
            var addY = eklenecekYil ?? 1;
            using (var entities = new LubsDbEntities())
            {
                donems = entities.Donemlers.OrderBy(o => o.DonemID).Select(s => new CmbIntDto { Value = s.DonemID, Caption = s.DonemAdi }).ToList();
            }
            for (int i = (DateTime.Now.Year + addY); i >= 2012; i--)
            {
                lst.Add(new CmbStringDto { Value = i + "/" + (i + 1) + "/2", Caption = i + "/" + (i + 1) + " " + donems.First(p => p.Value == 2).Caption });
                lst.Add(new CmbStringDto { Value = i + "/" + (i + 1) + "/1", Caption = i + "/" + (i + 1) + " " + donems.First(p => p.Value == 1).Caption });
            }
            return lst;
        }
        public static List<CmbStringDto> GetCmbAkademikDonemler(int? seciliYil = null)
        {
            var lst = new List<CmbStringDto>();
            List<CmbIntDto> donems; 
            using (var entities = new LubsDbEntities())
            {
                donems = entities.Donemlers.OrderBy(o => o.DonemID).Select(s => new CmbIntDto { Value = s.DonemID, Caption = s.DonemAdi }).ToList();
            }

            var hasSeciliYil = false;
            for (int i = (DateTime.Now.Year); i >= DateTime.Now.Year - 30; i--)
            {
                if (i == seciliYil) hasSeciliYil = true;
                lst.Add(new CmbStringDto { Value = i + "/" + (i + 1) + "/2", Caption = i + "/" + (i + 1) + " " + donems.First(p => p.Value == 2).Caption });
                lst.Add(new CmbStringDto { Value = i + "/" + (i + 1) + "/1", Caption = i + "/" + (i + 1) + " " + donems.First(p => p.Value == 1).Caption });
            }

            if (seciliYil.HasValue && !hasSeciliYil)
            {
                lst.Insert(0, new CmbStringDto { Value = seciliYil.Value + "/" + (seciliYil + 1) + "/2", Caption = seciliYil.Value + "/" + (seciliYil.Value + 1) + " " + donems.First(p => p.Value == 2).Caption });
                lst.Insert(0, new CmbStringDto { Value = seciliYil.Value + "/" + (seciliYil + 1) + "/1", Caption = seciliYil.Value + "/" + (seciliYil.Value + 1) + " " + donems.First(p => p.Value == 1).Caption });
            }
            return lst;
        }
        public static CmbStringDto CmbGetAkademikBulundugumuzTarih(DateTime? tarih = null)
        {
            var mdl = new CmbStringDto();
            var trh = tarih?.TodateToShortDate() ?? DateTime.Now.TodateToShortDate();
            using (var entities = new LubsDbEntities())
            {
                var eoy = trh.ToAkademikDonemBilgi();
                var sDonem = entities.Donemlers.First(p => p.DonemID == eoy.DonemId);
                eoy.DonemAdi = sDonem.DonemAdi;
                mdl.Value = eoy.BaslangicYil + "/" + eoy.BitisYil + "/" + eoy.DonemId;
                mdl.Caption = eoy.BaslangicYil + " / " + eoy.BitisYil + " " + eoy.DonemAdi;

            }
            return mdl;
        }
     

    }
}