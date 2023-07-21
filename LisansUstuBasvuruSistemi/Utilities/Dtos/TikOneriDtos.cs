using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using DevExpress.Printing.Native;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmTikBasvuru : PagerOption
    {
        public string EnstituKod { get; set; }
        public TikBasvuruKontrolBilgi BasvuruKontrolBilgi { get; set; }
        public int? KullaniciID { get; set; }
        public IEnumerable<FrTijBasvuru> Data { get; set; }

        public FmTikBasvuru()
        {
            BasvuruKontrolBilgi = new TikBasvuruKontrolBilgi();
        }
    }

    public class FrTijBasvuru : TijBasvuru
    {
        public string AdSoyad { get; set; }
        public string ResimAdi { get; set; }
        public string DurumColor { get; set; }
    }

    public class TikBasvuruKontrolBilgi
    {
        public string EnstituAdi { get; set; }
        public bool AktifOgrenimIcinBasvuruVar { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenimTipAdiProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public bool IsBasvuruAcik { get; set; }
        public bool IsOgrenci { get; set; }
        public bool IsDanisman { get; set; }
        public string Aciklama { get; set; }
    }
    public class TijOneriFormuKayitDto : TijBasvuruOneri
    { 
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }  
        public List<string> AnaTabAdi { get; set; }
        public List<string> DetayTabAdi { get; set; }
        public List<string> JuriTipAdi { get; set; }
        public List<string> AdSoyad { get; set; }
        public List<string> UnvanAdi { get; set; }
        public List<string> EMail { get; set; }
        public List<int?> UniversiteID { get; set; }
        public List<string> AnabilimdaliProgramAdi { get; set; } 

        public SelectList SListUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
        public List<KrTijOneriFormuJurileri> JoFormJuriList { get; set; }


        public TijOneriFormuKayitDto()
        {
            AnaTabAdi = new List<string>();
            DetayTabAdi = new List<string>();
            JuriTipAdi = new List<string>();
            AdSoyad = new List<string>();
            UnvanAdi = new List<string>();
            EMail = new List<string>();
            UniversiteID = new List<int?>();
            AnabilimdaliProgramAdi = new List<string>(); 
            JoFormJuriList = new List<KrTijOneriFormuJurileri>();
        }
    }
    public class KrTijOneriFormuJurileri : TijBasvuruOneriJuriler
    {

        public SelectList SlistUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
    }
}