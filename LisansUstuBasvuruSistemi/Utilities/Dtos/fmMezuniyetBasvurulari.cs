using System;
using System.Collections.Generic;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMezuniyetBasvurulari : PagerModel
    {
        public List<int> SelectedMezuniyetBasvurulariIds { get; set; }=new List<int>();
        public int? SMezuniyetBID { get; set; }
        public int? STabID { get; set; }
        public bool IsSinavDegerlendirme { get; set; }
        public bool Expand { get; set; }
        public Guid? RowID { get; set; }
        public int? KullaniciID { get; set; }
        public int? UyrukKod { get; set; }
        public string EnstituKod { get; set; }
        public int? MezuniyetSurecID { get; set; }
        public int? MezuniyetSureci { get; set; }
        public string KayitDonemi { get; set; }
        public int? KullaniciTipID { get; set; }
        public int? JuriOneriFormuDurumuID { get; set; }
        public int? CiltliTezTeslimUzatmaTalepDurumuID { get; set; }
        public string CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi { get; set; }
        public string AdSoyad { get; set; }
        public string EykSayisi { get; set; }
        public int? ToplamBasvurulanProgram { get; set; }
        public int? MezuniyetYayinKontrolDurumID { get; set; }
        public bool? IsMezuniyetYayinKontrolAciklamasiVar { get; set; }
        public int? OgrenimTipKod { get; set; }
        public bool? IsTezDiliTr { get; set; }
        public string ProgramKod { get; set; }
        public int? AnabilimDaliID { get; set; }
        public string AnabilimDaliKod { get; set; }
        public int? SRDurumID { get; set; }
        public int? MezuniyetSinavDurumID { get; set; }
        public int? TDDurumID { get; set; }
        public bool? TeslimFormDurumu { get; set; }
        public int? MezuniyetDurumID { get; set; }
        public int? TezKontrolKullaniciId { get; set; }
        public DateTime? MBaslangicTarihi { get; set; }
        public DateTime? MBitisTarihi { get; set; }

        public IEnumerable<FrMezuniyetBasvurulari> Data { get; set; }
    }
    public class FrMezuniyetBasvurulari : MezuniyetBasvurulari
    {
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public string MezuniyetSurecAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int AnabilimDaliId { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; } 
        public int SurecBaslangicYil { get; set; }
        public int DonemID { get; set; }
        public DateTime BasTar { get; set; }
        public DateTime BitTar { get; set; }
        public Guid? UserKey { get; set; } 
        public string KullaniciTipAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public DateTime? GsisKayitTarihi { get; set; }
        public string MezuniyetYayinKontrolDurumAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string MezuniyetSinavDurumAdi { get; set; }
        public string SDurumClassName { get; set; }
        public string SDurumColor { get; set; }
        public bool IsNotDuzelt { get; set; }
        public bool TeslimFormDurumu { get; set; }
        public int? SRDurumID { get; set; }
        public string IslemYapan { get; set; }
        public int UzatmaSuresiGun { get; set; }
        public int MezuniyetSuresiGun { get; set; }
        public SRTalepleri SrTalebi { get; set; } 
        public bool? IsOnaylandiOrDuzeltme { get; set; }
        public bool? TezDosyasiIlkKezKontrolBekliyor { get; set; }
        public MezuniyetBasvurulariTezDosyalari MezuniyetBasvurulariTezDosyasi { get; set; }
        public string FormNo { get; set; }
        public MezuniyetJuriOneriFormlari MezuniyetJuriOneriFormu { get; set; }
        public List<string> YayinInfo { get; set; } = new List<string>();
        public List<int> MBYayinTurIDs { get; set; }

    }
}