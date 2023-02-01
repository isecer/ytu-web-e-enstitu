using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Business
{
    public class TaleplerBus
    {
        public static List<CmbIntDto> GetCmbTalepTipleriSurec(int talepSurecId, int talepTipId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var surec = db.TalepSurecleris.FirstOrDefault(p => p.TalepSurecID == talepSurecId);
                if (surec != null)
                {
                    var talepTipIDs = surec.TalepSureciTalepTipleris.Select(s => s.TalepTipID).ToList();
                    var data = db.TalepTipleris.Where(p => (p.TalepTipID == talepTipId || talepTipIDs.Contains(p.TalepTipID))).OrderBy(o => o.TalepTipID).ToList();
                    foreach (var item in data)
                    {
                        dct.Add(new CmbIntDto { Value = item.TalepTipID, Caption = item.TalepTipAdi });
                    }
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbArGorStatuleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.TalepArGorStatuleris.OrderBy(o => o.TalepArGorStatuID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepArGorStatuID, Caption = item.StatuAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbTalepDurumlari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.TalepDurumlaris.OrderBy(o => o.TalepDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepDurumID, Caption = item.TalepDurumAdi });
                }
            }
            return dct;
        }

      

        public static List<CmbIntDto> GetCmbTalepTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.TalepTipleris.OrderBy(o => o.TalepTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbTalepSurecleri(string enstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.TalepSurecleris.Where(p => p.EnstituKod == enstituKod)
                    orderby s.BaslangicTarihi descending
                    select new
                    {
                        s.TalepSurecID,
                        s.BaslangicTarihi,
                        s.BitisTarihi
                    }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.TalepSurecID, Caption = item.BaslangicTarihi.ToDateString() + " - " + item.BitisTarihi.ToDateString() });
                }
            }
            return lst;
        }

        public static List<TalepDurumlari> GetTalepDurumList()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.TalepDurumlaris.OrderBy(o => o.TalepDurumID).ToList();
                return data;

            }

        }
    }
}