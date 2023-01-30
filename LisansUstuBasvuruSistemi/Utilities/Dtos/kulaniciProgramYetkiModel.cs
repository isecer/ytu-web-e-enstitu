using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KulaniciProgramYetkiModel
    {
        public int KullaniciProgramID { get; set; }
        public bool YetkiVar { get; set; }
        public string EnstituKisaAd { get; set; }
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }

        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }

    }
}