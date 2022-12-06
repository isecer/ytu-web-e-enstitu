using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.RmDtos
{
    public class raporLUBModel
    {
        public string EnstituAdi { get; set; }
        public string AkademikYil { get; set; }
        public string SurecTarihi { get; set; }
        public int ToplamTercihSayisi { get; set; }
        public IEnumerable<raporOtipModel> OgrenimTipleri { get; set; }
        public fmMsonucOranModel AIToplamModel { get; set; }
        public fmMsonucOranModel ADToplamModel { get; set; }
        public IEnumerable<frMulakatSonucDetay> BasvuruSonuclari { get; set; }
    }
}