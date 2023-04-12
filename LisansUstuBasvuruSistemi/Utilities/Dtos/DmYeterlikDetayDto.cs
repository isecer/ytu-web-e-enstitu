using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class DmYeterlikDetayDto : YeterlikBasvuru
    { 
        public bool? IsKomiteOrJuri { get; set; }
        public Guid? IsDegerlendirme { get; set; }
        public string KayitDonemi { get; set; }
        public string ResimAdi { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int AnabilimDaliID { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string DanismanAdi { get; set; }
        public int AbdYeterlikKomiteUyeCount { get; set; }
        public YeterlikSurecOgrenimTipleri YeterlikSurecOgrenimTipleri { get; set; } 
        public List<DmYeterlikKomite> DmYeterlikKomites { get; set; } 
    }
     
    public sealed class DmYeterlikKomite : YeterlikBasvuruKomiteler
    {
        public string UnvanAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
    }
    public sealed class KmYeterlikJuriModel : YeterlikBasvuru
    {
        public int SelectedTabId { get; set; }
        public List<int> TabIds { get; set; }
        public List<Guid> UniqueIDs { get; set; }
        public List<string> JuriTipAdis { get; set; }
        public List<int?> UniversiteIDs { get; set; }
        public List<string> AnabilimDaliAdis { get; set; }
        public List<string> AdSoyads { get; set; }
        public List<string> UnvanAdis { get; set; }
        public List<string> EMails { get; set; }
        public List<bool?> IsAsilOrYedeks { get; set; }

        public SelectList SelectListUndan;
        public SelectList SelectListUniversite;
        public SelectList SelectListAnabilimDali;
        public KmYeterlikJuriModel()
        {
            SelectedTabId = 1;
            YeterlikBasvuruJuriUyeleris = new List<YeterlikBasvuruJuriUyeleri>();
        }
    }

    public class KmYeterlikJuriDegisiklik : YeterlikBasvuruJuriUyeleri
    {
        public bool? EMailOrJuriDegisiklik{ get; set; }
        public Guid? YeniJuriUniqueID { get; set; }
        public SelectList SelectListYedekJuriler;
    }
    public class KmYeterlikKomiteDegisiklik : YeterlikBasvuruKomiteler
    {
        public int AnabilimDaliID { get; set; }
        public string UnvanAdi { get; set; }
        public string AdSoyad { get; set; }
        public int? YeniKomiteID { get; set; }
        public bool isDegiseBilecekKomiteVar { get; set; }
        public SelectList SelectListYeniKomiteler;
    }
}