using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc; 
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetBasvuruDetayDto : MezuniyetBasvurulari
    {
        public IHtmlString BasvuruDurumHtml { get; set; }
        public bool IsDelete { get; set; }
        public bool GelenBasvuru { get; set; }
        public bool sonucGirisSureciAktif { get; set; }
        public bool IsYerli { get; set; }
        public string EnstituAdi { get; set; }
        public int SelectedTabIndex { get; set; }

        public string OgrenimDurumAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string BasvuruSureciTarihi { get; set; }
        public string BasvuruKayitSureciTarihi { get; set; }
        public string KullaniciTipAdi { get; set; }

        public List<MezuniyetBasvurulariYayinDto> YayinBilgileri { get; set; }

        public string MezuniyetYayinKontrolDurumAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public bool IsAnketVar { get; set; }
        public bool IsAnketDolduruldu { get; set; }
        public string AnketView { get; set; }
        public bool? EYKYaGonderildi { get; set; }
        public bool? EYKDaOnaylandi { get; set; }

        public string TezDanismanBilgiEslesen { get; set; }
        public Guid? TezDanismaniUserKey { get; set; }
        public MezuniyetBasvurulariYayinDto SelectedYayin { get; set; }


        public MezuniyetSRModel MezuniyetSRModel { get; set; }
        public List<MezuniyetBasvurulariTezDosyalariDto> MezuniyetBasvurulariTezDosyalariDtos { get; set; }
        public SrDurumSelectList MezuniyetDurumSelectList = new SrDurumSelectList();

        public SelectList SMezuniyetYayinKontrolDurum { get; set; }
        public SelectList SEYKYaGonderildi { get; set; }
        public SelectList SEYKDaOnaylandi { get; set; }
        public SelectList SIsAsilOryedek { get; set; }



        public MezuniyetBasvuruDetayDto()
        {
            YayinBilgileri = new List<MezuniyetBasvurulariYayinDto>();
            MezuniyetSRModel = new MezuniyetSRModel();


        }
    }

    public class MezuniyetSRModel
    {
        public bool IsSrEykSureAsimi { get; set; }
        public DateTime? EykIlkSrMaxTarih { get; set; }
        public List<FrTalepler> SalonRezervasyonlari { get; set; }
    }
}