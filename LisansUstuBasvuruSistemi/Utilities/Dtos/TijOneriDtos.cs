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
    public class FmTijBasvuru : PagerOption
    {
        public Guid? SelectedBasvuruUniqueId { get; set; }
        public string EnstituKod { get; set; }
        public TijBasvuruKontrolBilgi BasvuruKontrolBilgi { get; set; }
        public string AktifTijDonemId { get; set; }
        public int? AnabilimDaliID { get; set; }
        public int? AktifDurumID { get; set; }
        public  int? TijFormTipID { get; set; }
        public int? KullaniciID { get; set; }
        public string AdSoyad { get; set; }
        public IEnumerable<FrTijBasvuru> Data { get; set; }

        public FmTijBasvuru()
        {
            BasvuruKontrolBilgi = new TijBasvuruKontrolBilgi();
        }
    }

    public class FrTijBasvuru : TijBasvuru
    {
        public bool KayitVar { get; set; }
        public string AdSoyad { get; set; }
        public string ResimAdi { get; set; }
        public string DurumColor { get; set; }
        public TijBasvuruOneriDetayDto SonBasvuru { get; set; }
    }

    public class TijBasvuruKontrolBilgi
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
        public bool IsIlkOneri { get; set; }
        public int KullaniciId { get; set; }
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public List<int> RowNum { get; set; }
        public List<bool> IsTezDanismani { get; set; }
        public List<bool> IsYtuIciJuri { get; set; }
        public List<string> AdSoyad { get; set; }
        public List<string> UnvanAdi { get; set; }
        public List<string> EMail { get; set; }
        public List<int?> UniversiteID { get; set; }
        public List<string> AnabilimdaliAdi { get; set; }
         
        public SelectList SListTijDegisiklikTip { get; set; }
        public SelectList SListTijFormTip { get; set; }


        public SelectList SListUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
        public List<KrTijOneriFormuJurileri> JoFormJuriList { get; set; }


        public TijOneriFormuKayitDto()
        { 
            IsTezDanismani = new List<bool>();
            IsYtuIciJuri = new List<bool>();
            AdSoyad = new List<string>();
            UnvanAdi = new List<string>();
            EMail = new List<string>();
            UniversiteID = new List<int?>();
            AnabilimdaliAdi = new List<string>(); 
            JoFormJuriList = new List<KrTijOneriFormuJurileri>();


        }
    }
    public class KrTijOneriFormuJurileri : TijBasvuruOneriJuriler
    { 
        public SelectList SlistUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
    }
}