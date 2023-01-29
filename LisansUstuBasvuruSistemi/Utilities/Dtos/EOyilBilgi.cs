using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class EOyilBilgi
    {

        public int BaslangicYili { get; set; }
        public int BitisYili { get; set; }

        public int Donem { get; set; }
        public string DonemAdi { get; set; }
    }
    public class TarihAralikModel
    {
        public int BaslangicYil { get; set; }
        public int BitisYil { get { return (this.BaslangicYil > 0 ? this.BaslangicYil + 1 : 0); } }
        public int DonemID { get; set; }
        public string DonemAdi { get; set; }
        public string DonemAdiEn { get; set; }
        public string DonemAdiLong { get { return (this.BaslangicYil + " - " + (this.BaslangicYil + 1) + " " + this.DonemAdi); } }
        public string DonemAdiEnLong { get { return (this.BaslangicYil + " - " + (this.BaslangicYil + 1) + " " + this.DonemAdiEn); } }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
    }
}