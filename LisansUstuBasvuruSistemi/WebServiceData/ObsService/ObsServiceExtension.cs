using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsService
{
    public static class ObsServiceExtension
    {
        public static int? ToOgrenimTipKod(this string ogrenimSeviyeId)
        {
            switch (ogrenimSeviyeId)
            {
                case "2":
                    return OgrenimTipi.TezliYuksekLisans;
                case "3":
                    return OgrenimTipi.Doktra;
                case "4":
                    return OgrenimTipi.TezsizYuksekLisans; 
                case "5":
                    return OgrenimTipi.SanattaYeterlilik; 
                case "8":
                    return OgrenimTipi.ButunlesikDoktora; 
                default: return null;
            }
        }
    }
}