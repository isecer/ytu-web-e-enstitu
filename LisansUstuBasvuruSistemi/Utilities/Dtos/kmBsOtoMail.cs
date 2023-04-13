using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmBsOtoMail : BasvuruSurecOtoMail
    {
        public int gID { get; set; }
        public bool Checked { get; set; }
        public string ZamanTipAdi { get; set; }

    }
}