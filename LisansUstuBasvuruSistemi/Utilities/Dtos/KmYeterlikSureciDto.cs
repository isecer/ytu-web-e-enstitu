using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmYeterlikSureciDto : YeterlikSureci
    {
        public string OgretimYili { get; set; }
        public List<int> YeterlikSurecOgrenimTipID { get; set; }
        public List<int> OgrenimTipID { get; set; }
        public List<int> OgrenimTipKod { get; set; }
        public List<int?> YsMaxBasvuruDonemNo { get; set; }
        public List<string> YsBasEtikNotKriteri { get; set; }
        public List<string> YsBasSeminerNotKriteri { get; set; }
        public List<int?> YsBasToplamKrediKriteri { get; set; }
        public List<KmYeterlikSureciOgrenimTipKriterleri> KmYeterlikSureciOgrenimTipKriterleris { get; set; }
    }
    public class KmYeterlikSureciOgrenimTipKriterleri : YeterlikSurecOgrenimTipleri
    {
        public bool IsSelected { get; set; }
        public string OgrenimTipAdi { get; set; }
    }
}