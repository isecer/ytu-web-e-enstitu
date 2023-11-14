using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmTiAraRaporFormuModel : TIBasvuruAraRapor
    {
        public string DilKodu { get; set; }
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public new bool? IsYokDrBursiyeriVar { get; set; }
        public SelectList SListAraRaporSayisi { get; set; }
        public int SelectedTabId { get; set; }
        public List<int> TabId { get; set; }
        public List<string> AnaTabAdi { get; set; } = new List<string>();
        public List<string> JuriTipAdi { get; set; } = new List<string>();
        public List<string> AdSoyad { get; set; } = new List<string>();
        public List<string> UnvanAdi { get; set; } = new List<string>();
        public List<string> EMail { get; set; } = new List<string>();
        public List<int?> UniversiteId { get; set; } = new List<int?>();
        public List<string> AnabilimdaliProgramAdi { get; set; } = new List<string>();
        public List<string> DilSinavAdi { get; set; } = new List<string>();
        public List<string> IsDilSinaviOrUniversite { get; set; }
        public List<string> DilPuani { get; set; } = new List<string>();
        public List<string> SinavTarihi { get; set; } = new List<string>();

        public List<KrTIBasvuruAraRaporKomite> KomiteList { get; set; } = new List<KrTIBasvuruAraRaporKomite>();

        public HttpPostedFileBase Dosya { get; set; }
    }
    public class KrTIBasvuruAraRaporKomite : TIBasvuruAraRaporKomite
    {

        public SelectList SlistUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
    }
}