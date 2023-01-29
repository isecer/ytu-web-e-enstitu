using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class msUrecDetay : MezuniyetSureci
    {
        public int SelectedTabIndex { get; set; }
        public bool IsDelete { get; set; }
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string DonemAdi { get; set; }
        public MIndexBilgi ToplamBasvuruBilgisi { get; set; }
        public msUrecDetay()
        {
        }
    }
}