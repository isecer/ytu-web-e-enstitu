using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; 
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmDekontBilgi
    {
        public int KullaniciID { get; set; }
        public string UniqueID { get; set; }
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public string AdSoyad { get; set; }
        public int KullaniciTipID { get; set; }
        public string TcPasaportNo { get; set; }
        public bool KullaniciAktif { get; set; }
        public bool GirisAktif { get; set; }

        public kontenjanProgramBilgiModel ProgramBilgi { get; set; }

    }
}