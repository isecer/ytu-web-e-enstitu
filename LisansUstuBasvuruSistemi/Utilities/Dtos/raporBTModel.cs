using System.Collections.Generic;
using raporBTSayisalModel = LisansUstuBasvuruSistemi.Utilities.Dtos.RaporBTSayisalModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RaporBTModel
    {
        public string EnstituAdi { get; set; }
        public string SurecTarihi { get; set; }
        public IEnumerable<RaporBTSayisalModel> YilaGoreToplam { get; set; }
        public IEnumerable<RaporBTSayisalModel> DetayliToplam { get; set; }
    }
}