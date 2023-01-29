using System.Collections.Generic; 
using LisansUstuBasvuruSistemi.Utilities.Enums;
namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmMulakatSonuc
    {
        public int ToplamTercihCount { get; set; }
        public int HesaplananTercihCount { get; set; }
        public int ToplamProgramCount { get; set; }
        public int HesaplananProgramCount { get; set; }
        public bool HesaplamaYapildi { get; set; }
        public bool TumProgramlarHesaplandi { get; set; }
        public int MulakatSurecineGirecekToplamBasvuru { get; set; }
        public int MulakatSurecineGirmeyecekToplamBasvuru { get; set; }
        public List<fmMsonucOranModel> OranModel { get; set; }
        public List<frMulakatSonucDetay> MulakatSonucDetay { get; set; }
        public fmMulakatSonuc()
        {
            MulakatSonucDetay = new List<frMulakatSonucDetay>();
            OranModel = new List<fmMsonucOranModel>();
            OranModel.Add(new fmMsonucOranModel { MulakatSurecineGirecek = true, AlanTipID = AlanTipi.AlanIci });
            OranModel.Add(new fmMsonucOranModel { MulakatSurecineGirecek = true, AlanTipID = AlanTipi.AlanDisi });
            OranModel.Add(new fmMsonucOranModel { MulakatSurecineGirecek = false, AlanTipID = AlanTipi.AlanIci });
            OranModel.Add(new fmMsonucOranModel { MulakatSurecineGirecek = false, AlanTipID = AlanTipi.AlanDisi });
        }
    }
    public class frMulakatSonucDetay
    {
        public int? MulakatID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int AIKota { get; set; }
        public int ADKota { get; set; }
        public int ToplamBasvuru { get; set; }
        public int AIKayitCount { get; set; }
        public int AIAsilCount { get; set; }
        public int AIYedekCount { get; set; }
        public int AIKazanamayanCount { get; set; }
        public int AISinavaGirmeyenCount { get; set; }
        public int ADKayitCount { get; set; }
        public int ADAsilCount { get; set; }
        public int ADYedekCount { get; set; }
        public int ADKazanamayanCount { get; set; }
        public int ADSinavaGirmeyenCount { get; set; }
        public bool Ucretli { get; set; }
        public double? Ucret { get; set; }
        public string KullaniciTipAdi { get; set; }

    }
}