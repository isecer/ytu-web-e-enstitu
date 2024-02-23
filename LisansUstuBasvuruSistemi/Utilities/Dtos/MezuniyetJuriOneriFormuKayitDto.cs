using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetJuriOneriFormuKayitDto : MezuniyetJuriOneriFormlari
    {
        public bool IsDoktoraOrYL { get; set; }
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public bool IsTezDiliTr { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
        public Kullanicilar Danisman { get; set; }
        public List<string> AnaTabAdi { get; set; } = new List<string>();
        public List<string> DetayTabAdi { get; set; } = new List<string>();
        public List<string> JuriTipAdi { get; set; } = new List<string>();
        public List<string> AdSoyad { get; set; } = new List<string>();
        public List<string> UnvanAdi { get; set; } = new List<string>();
        public List<string> EMail { get; set; } = new List<string>();
        public List<string> UniversiteAdi { get; set; } = new List<string>();
        public List<string> AnabilimdaliProgramAdi { get; set; } = new List<string>();
        public List<string> UzmanlikAlani { get; set; } = new List<string>();

        public SelectList SListUnvanAdi { get; set; } 
        public List<KrMezuniyetJuriOneriFormuJurileri> JoFormJuriList { get; set; } = new List<KrMezuniyetJuriOneriFormuJurileri>();
    }
    public class KrMezuniyetJuriOneriFormuJurileri : MezuniyetJuriOneriFormuJurileri
    {

        public SelectList SlistUnvanAdi { get; set; } 
    }
}