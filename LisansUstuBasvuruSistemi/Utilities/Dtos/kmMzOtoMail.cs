using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMzOtoMail : MezuniyetSurecOtoMail
    {
        public int gID { get; set; }
        public bool Checked { get; set; }
        public string ZamanTipAdi { get; set; }
        public string Aciklama { get; set; }

    }
}