using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class UniversitelerBus
    {
        public static List<CmbIntDto> CmbGetAktifUniversiteler(bool bosSecimVar = false, bool isYtuHaric = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Universitelers.Where(p => !isYtuHaric || p.UniversiteID != GlobalSistemSetting.UniversiteYtuKod).OrderBy(o => o.Ad).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto { Value = item.UniversiteID, Caption = item.Ad + (item.KisaAd.IsNullOrWhiteSpace() ? "" : " (" + item.KisaAd + ")") }));
            }
            return dct;

        }
    }
}