using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.RmDtos
{
    public class raporBTModel
    {
        public string EnstituAdi { get; set; }
        public string SurecTarihi { get; set; }
        public IEnumerable<raporBTSayisalModel> YilaGoreToplam { get; set; }
        public IEnumerable<raporBTSayisalModel> DetayliToplam { get; set; }
    }
}