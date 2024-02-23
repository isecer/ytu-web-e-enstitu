using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Business
{
    public class BirimlerBus
    {
        public static List<Birimler> GetBirimler()
        {

            using (var entities = new LubsDbEntities())
            {
                return entities.Birimlers.OrderBy(o => o.BirimAdi).ToList();

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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.Birimlers.OrderBy(o => o.BirimAdi).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto { Value = item.BirimID, Caption = item.BirimAdi }));
            }
            return dct;

        }

    }
}