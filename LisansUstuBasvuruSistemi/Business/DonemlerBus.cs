using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

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
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                donems = db.Donemlers.OrderBy(o => o.DonemID).Select(s => new CmbIntDto { Value = s.DonemID, Caption = s.DonemAdi }).ToList();
            }
            for (int i = (DateTime.Now.Year + addY); i >= 2012; i--)
            {
                lst.Add(new CmbStringDto { Value = i.ToString() + "/" + (i + 1).ToString() + "/2", Caption = i.ToString() + "/" + (i + 1).ToString() + " " + donems.First(p => p.Value == 2).Caption });
                lst.Add(new CmbStringDto { Value = i.ToString() + "/" + (i + 1).ToString() + "/1", Caption = i.ToString() + "/" + (i + 1).ToString() + " " + donems.First(p => p.Value == 1).Caption });
            }
            return lst;
        }

        public static CmbStringDto CmbGetAkademikBulundugumuzTarih(DateTime? tarih = null)
        {
            var mdl = new CmbStringDto();
            var trh = tarih?.TodateToShortDate() ?? DateTime.Now.TodateToShortDate();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var eoy = trh.ToEgitimOgretimYilBilgi();
                var sDonem = db.Donemlers.First(p => p.DonemID == eoy.Donem);
                eoy.DonemAdi = sDonem.DonemAdi;
                mdl.Value = eoy.BaslangicYili + "/" + eoy.BitisYili + "/" + eoy.Donem;
                mdl.Caption = eoy.BaslangicYili + " / " + eoy.BitisYili + " " + eoy.DonemAdi;

            }
            return mdl;
        }

       
    }
}