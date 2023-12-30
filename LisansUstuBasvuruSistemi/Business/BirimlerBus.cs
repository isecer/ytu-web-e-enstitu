using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Business
{
    public class BirimlerBus
    {
        public static List<Birimler> GetBirimler()
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.Birimlers.OrderBy(o => o.BirimAdi).ToList();

            }
        }
        public static Birimler[] GetBirimlerTreeList()
        { 
            return GetBirimler().ToOrderedList("BirimID", "UstBirimID", "BirimAdi");
        }
        public static List<CmbIntDto> CmbBirimler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Birimlers.OrderBy(o => o.BirimAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BirimID, Caption = item.BirimAdi });
                }
            }
            return dct;

        }

    }
}