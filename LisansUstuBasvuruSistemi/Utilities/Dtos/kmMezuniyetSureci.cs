using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMezuniyetSureci : MezuniyetSureci
    {
        public string OgretimYili { get; set; } 

        public List<int?> MezuniyetSureciOgrenimTipKriterId { get; set; } = new List<int?>();
        public List<int?> OgrenimTipId { get; set; } = new List<int?>();
        public List<int?> OgrenimTipKod { get; set; } = new List<int?>();
        public List<string> MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari { get; set; } = new List<string>();
        public List<string> MBasvuruEtikNotKriteri { get; set; } = new List<string>();
        public List<string> MBasvuruSeminerNotKriteri { get; set; } = new List<string>();

        public List<int?> MBasvuruToplamKrediKriteri { get; set; } = new List<int?>();
        public List<double?> MBasvuruAgnoKriteri { get; set; } = new List<double?>();
        public List<int?> MBasvuruAktsKriteri { get; set; } = new List<int?>();
        public List<int?> MbsrTalebiKacGunSonraAlabilir { get; set; } = new List<int?>();
        public List<int?> MbSinavUzatmaOgrenciTaahhutMaxGun { get; set; }
        public List<int?> MbSinavUzatmaSinavAlmaSuresiMaxGun { get; set; } = new List<int?>();
        public List<int?> MbTezTeslimSuresiGun { get; set; } = new List<int?>();


        public KmMezuniyetSureciOgrenimTipModel OgrenimTipModel { get; set; } = new KmMezuniyetSureciOgrenimTipModel();
    }
    public class KmMezuniyetSureciOgrenimTipModel
    {
        public List<KmMezuniyetSureciOgrenimTipKriterleri> OgrenimTipKriterList { get; set; }
    }
    public class KmMezuniyetSureciOgrenimTipKriterleri : MezuniyetSureciOgrenimTipKriterleri
    { 
        public SelectList SlistEtikNots { get; set; }
        public SelectList SlistSeminerNots { get; set; } 
        public string OgrenimTipAdi { get; set; }
    }
}