using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc; 
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetBasvuruDetayDto : MezuniyetBasvurulari
    {
        public IHtmlString BasvuruDurumHtml { get; set; }
        public bool IsDelete { get; set; }
        public bool GelenBasvuru { get; set; }
        public bool SonucGirisSureciAktif { get; set; } 
        public bool IsYerli { get; set; }
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public int SelectedTabIndex { get; set; }

        public string OgrenimDurumAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int AnabilimdaliID { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string BasvuruSureciTarihi { get; set; }
        public string BasvuruKayitSureciTarihi { get; set; }
        public string KullaniciTipAdi { get; set; }

        public List<MezuniyetBasvurulariYayinDto> YayinBilgileri { get; set; } = new List<MezuniyetBasvurulariYayinDto>();

        public bool IsToplamKaynakOraniGirisiYapilacak { get; set; }
        public bool IsTekKaynakOraniGirisiYapilacak { get; set; }
        public string MezuniyetYayinKontrolDurumAdi { get; set; }
        public string MezuniyetYayinKontrolDurumOnayYapanKullaniciAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public bool IsAnketVar { get; set; }
        public bool IsAnketDolduruldu { get; set; }
        public string AnketView { get; set; }
        public bool? EykYaGonderildi { get; set; }
        public bool? EykDaOnaylandi { get; set; }

        public string TezDanismanBilgiEslesen { get; set; }
        public string GuncellenebilirBasvuruDanismanAdi { get; set; }
        public Guid? TezDanismaniUserKey { get; set; }
        public string TezKontrolYetkilisiAdSoyad { get; set; }
        public Guid? TezKontrolYetkiliUserKey { get; set; }
        public DateTime? SelectedTezTeslimSonTarih { get; set; }
        public DateTime? DefaultTezTeslimSonTarih { get; set; }
        public DateTime? DefaultMaxTezTeslimSonTarih { get; set; }
        public DateTime? AktifTezTeslimSonTarih { get; set; }

        public MezuniyetBasvurulariYayinDto SelectedYayin { get; set; }


        public MezuniyetSrModel MezuniyetSrModel { get; set; } = new MezuniyetSrModel();
        public List<MezuniyetBasvurulariTezDosyalariDto> MezuniyetBasvurulariTezDosyalariDtos { get; set; }
        public SrDurumSelectList MezuniyetDurumSelectList = new SrDurumSelectList();
       

        public SelectList SMezuniyetYayinKontrolDurum { get; set; }
        public SelectList SeykYaGonderildi { get; set; }
        public SelectList SeykDaOnaylandi { get; set; }
        public SelectList SIsAsilOryedek { get; set; }
        public SelectList OgrenciProgramList { get; set; }
    }

    public class MezuniyetSrModel
    {
        public bool IsSrEykSureAsimi { get; set; }
        public DateTime? EykIlkSrMaxTarih { get; set; }
        public List<FrTalepler> SalonRezervasyonlari { get; set; }
    }
}