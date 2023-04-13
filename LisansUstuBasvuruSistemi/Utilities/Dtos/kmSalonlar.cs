using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmSalonlar : SRSalonlar
    {


        public List<string> HaftaGunleri { get; set; }
        public List<TimeSpan> BasSaat { get; set; }
        public List<TimeSpan> BitSaat { get; set; }


        public List<SRSaatlerMDL> Saatler { get; set; }
        public object TeslimBitisSaat { get; internal set; }

        public KmSalonlar()
        {
            Saatler = new List<SRSaatlerMDL>();
        }
    }
}