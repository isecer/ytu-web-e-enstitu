using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetBasvurulariTezDosyalariDto: MezuniyetBasvurulariTezDosyalari

    {
        public Guid UserKey { get; set; }
        public string OnayYapanTezKontrolYetkiliAdSoyad { get; set; }
    }
}