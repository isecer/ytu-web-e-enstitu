using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMezuniyetSureci : MezuniyetSureci
    {
        public string OgretimYili { get; set; } 

        public List<int?> MezuniyetSureciOgrenimTipKriterId { get; set; } = new List<int?>();
        public List<int?> OgrenimTipId { get; set; } = new List<int?>();
        public List<int?> OgrenimTipKod { get; set; } = new List<int?>();
        public List<int> AktifDonemMaxKriteri { get; set; } = new List<int>();
        public List<string> AktifDonemDersKodKriteri { get; set; } = new List<string>();
        public List<string> AktifDonemEtikNotKriteri { get; set; } = new List<string>();
        public List<string> AktifDonemSeminerNotKriteri { get; set; } = new List<string>();

        public List<int?> AktifDonemToplamKrediKriteri { get; set; } = new List<int?>();
        public List<double?> AktifDonemAgnoKriteri { get; set; } = new List<double?>();
        public List<int?> AktifDonemAktsKriteri { get; set; } = new List<int?>();

        public List<int?> ToplamKaynakOraniKriteri { get; set; } = new List<int?>();
        public List<int?> TekKaynakOraniKriteri { get; set; } = new List<int?>();
        public List<int?> SinavKacGunSonraAlabilir { get; set; } = new List<int?>();
        public List<int?> SinavEnGecKacGunSonraAlabilir { get; set; } = new List<int?>(); 
        public List<int?> SinavUzatmaOgrenciTaahhutMaxGun { get; set; } = new List<int?>();
        public List<int?> SinavUzatmaSinavAlmaSuresiMaxGun { get; set; } = new List<int?>();
        public List<int?> TezTeslimSuresiGun { get; set; } = new List<int?>();


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