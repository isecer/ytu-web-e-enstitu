using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RwMulakatJuri : MulakatJuri
    {
        public string AsilYedek { get; set; }
    }
}