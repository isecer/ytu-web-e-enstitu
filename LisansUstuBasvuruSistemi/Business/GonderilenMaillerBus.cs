using System.Collections.Generic;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Business
{
    public class GonderilenMaillerBus
    {
        public static List<CmbBoolDto> GetCmbMailEkKontrol(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Eki Olanlar" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Eki Olmayanlar" });

            return dct;

        }
    }
}