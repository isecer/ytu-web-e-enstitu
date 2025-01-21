using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class TiAraRaporFormuModel : TIBasvuruAraRapor
    {
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public new bool? IsYokDrBursiyeriVar { get; set; }
        public SelectList SListAraRaporSayisi { get; set; }
        public int SelectedTabId { get; set; }
        public List<int> TabId { get; set; }
        public List<string> AnaTabAdi { get; set; }
        public List<string> JuriTipAdi { get; set; }
        public List<string> AdSoyad { get; set; }
        public List<string> UnvanAdi { get; set; }
        public List<string> EMail { get; set; }
        public List<int?> UniversiteId { get; set; }
        public List<string> AnabilimdaliProgramAdi { get; set; } 
        public SelectList SListDonemSecim { get; set; }
        public SelectList SListUnvanAdi { get; set; }
        public SelectList SListUniversiteId { get; set; }
        public List<KrTIBasvuruAraRaporKomite> KomiteList { get; set; }

        public HttpPostedFileBase Dosya { get; set; }

        public TiAraRaporFormuModel()
        {
            AnaTabAdi = new List<string>();
            JuriTipAdi = new List<string>();
            AdSoyad = new List<string>();
            UnvanAdi = new List<string>();
            EMail = new List<string>();
            UniversiteId = new List<int?>();
            AnabilimdaliProgramAdi = new List<string>(); 
            KomiteList = new List<KrTIBasvuruAraRaporKomite>();
        }
    }
}