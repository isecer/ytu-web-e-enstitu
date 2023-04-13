using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MailBsonucModel : RprBasvuruSonucModel
    {
        public int KullaniciID { get; set; }
        public int BasvuruID { get; set; }
        public string EgitimOgretimYili { get; set; }
        public string EnstituAdi { get; set; }
        public string WebAdresi { get; set; }
        public string Link { get; set; }
        public string Email { get; set; }
    }
}