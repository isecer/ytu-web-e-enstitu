using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.KmDtos
{
    public class kmSalonlar : SRSalonlar
    {


        public List<string> HaftaGunleri { get; set; }
        public List<TimeSpan> BasSaat { get; set; }
        public List<TimeSpan> BitSaat { get; set; }


        public List<SRSaatlerMDL> Saatler { get; set; }
        public object TeslimBitisSaat { get; internal set; }

        public kmSalonlar()
        {
            Saatler = new List<SRSaatlerMDL>();
        }
    }
}