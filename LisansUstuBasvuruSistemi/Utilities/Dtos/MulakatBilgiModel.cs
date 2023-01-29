using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MulakatBilgiModel : BasvuruBilgiModel
    {
        public List<string> Programlar { get; set; }
        //public basvuru AktifBasvuru { get; set; }
        public MulakatBilgiModel()
        {
            Programlar = new List<string>();
        }
    }
}