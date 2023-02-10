using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class raporLUBModel
    {
        public string EnstituAdi { get; set; }
        public string AkademikYil { get; set; }
        public string SurecTarihi { get; set; }
        public int ToplamTercihSayisi { get; set; }
        public IEnumerable<raporOtipModel> OgrenimTipleri { get; set; }
        public FmMsonucOranModel AIToplamModel { get; set; }
        public FmMsonucOranModel ADToplamModel { get; set; }
        public IEnumerable<FrMulakatSonucDetay> BasvuruSonuclari { get; set; }
    }
}