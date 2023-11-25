using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetBasvurulariTezDosyalariDto: MezuniyetBasvurulariTezDosyalari

    {
        public string TezKontrolYetkiliAdSoyad { get; set; }
        public string OnayYapanTezKontrolYetkiliAdSoyad { get; set; }
    }
}