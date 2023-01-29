using System.Collections.Generic;
using raporBTSayisalModel = LisansUstuBasvuruSistemi.Utilities.Dtos.raporBTSayisalModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class raporBTModel
    {
        public string EnstituAdi { get; set; }
        public string SurecTarihi { get; set; }
        public IEnumerable<raporBTSayisalModel> YilaGoreToplam { get; set; }
        public IEnumerable<raporBTSayisalModel> DetayliToplam { get; set; }
    }
}