using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetJuriOneriFormuModel : MezuniyetJuriOneriFormlari
    {
        public bool IsDoktoraOrYL { get; set; }
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public bool IsTezDiliTr { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
        public Kullanicilar Danisman { get; set; }
        public List<string> AnaTabAdi { get; set; }
        public List<string> DetayTabAdi { get; set; }
        public List<string> JuriTipAdi { get; set; }
        public List<string> AdSoyad { get; set; }
        public List<string> UnvanAdi { get; set; }
        public List<string> EMail { get; set; }
        public List<int?> UniversiteID { get; set; }
        public List<string> AnabilimdaliProgramAdi { get; set; }
        public List<string> UzmanlikAlani { get; set; }
        public List<string> BilimselCalismalarAnahtarSozcukler { get; set; }
        public List<string> DilSinavAdi { get; set; }
        public List<string> DilPuani { get; set; }

        public SelectList SListUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
        public List<KrMezuniyetJuriOneriFormuJurileri> JoFormJuriList { get; set; }


        public MezuniyetJuriOneriFormuModel()
        {
            AnaTabAdi = new List<string>();
            DetayTabAdi = new List<string>();
            JuriTipAdi = new List<string>();
            AdSoyad = new List<string>();
            UnvanAdi = new List<string>();
            EMail = new List<string>();
            UniversiteID = new List<int?>();
            AnabilimdaliProgramAdi = new List<string>();
            UzmanlikAlani = new List<string>();
            BilimselCalismalarAnahtarSozcukler = new List<string>();
            DilSinavAdi = new List<string>();
            DilPuani = new List<string>();
            JoFormJuriList = new List<KrMezuniyetJuriOneriFormuJurileri>();
        }
    }
    public class KrMezuniyetJuriOneriFormuJurileri : MezuniyetJuriOneriFormuJurileri
    {

        public SelectList SlistUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
    }
}