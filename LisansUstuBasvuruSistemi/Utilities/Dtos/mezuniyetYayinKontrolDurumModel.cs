using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class mezuniyetYayinKontrolDurumModel : MezuniyetYayinKontrolDurumlari
    {
        public string DurumAdi { get; set; }
    }
}