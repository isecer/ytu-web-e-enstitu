using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class TIAraRaporFormuModel : TIBasvuruAraRapor
    {
        public string DilKodu { get; set; }
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public bool? IsYokDrBursiyeriVar { get; set; }
        public SelectList SListAraRaporSayisi { get; set; }
        public int SelectedTabID { get; set; }
        public List<int> TabID { get; set; }
        public List<string> AnaTabAdi { get; set; }
        public List<string> JuriTipAdi { get; set; }
        public List<string> AdSoyad { get; set; }
        public List<string> UnvanAdi { get; set; }
        public List<string> EMail { get; set; }
        public List<int?> UniversiteID { get; set; }
        public List<string> AnabilimdaliProgramAdi { get; set; }
        public List<string> DilSinavAdi { get; set; }
        public List<string> IsDilSinaviOrUniversite { get; set; }
        public List<string> DilPuani { get; set; }
        public List<string> SinavTarihi { get; set; }

        public SelectList SListUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
        public List<KrTIBasvuruAraRaporKomite> KomiteList { get; set; }

        public HttpPostedFileBase Dosya { get; set; }

        public TIAraRaporFormuModel()
        {
            AnaTabAdi = new List<string>();
            JuriTipAdi = new List<string>();
            AdSoyad = new List<string>();
            UnvanAdi = new List<string>();
            EMail = new List<string>();
            UniversiteID = new List<int?>();
            AnabilimdaliProgramAdi = new List<string>();
            DilSinavAdi = new List<string>();
            DilPuani = new List<string>();
            SinavTarihi = new List<string>();
            KomiteList = new List<KrTIBasvuruAraRaporKomite>();
        }
    }
}