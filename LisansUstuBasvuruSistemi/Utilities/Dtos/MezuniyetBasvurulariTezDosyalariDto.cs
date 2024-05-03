using System;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetBasvurulariTezDosyalariDto: MezuniyetBasvurulariTezDosyalari

    {
        public Guid UserKey { get; set; }
        public string OnayYapanTezKontrolYetkiliAdSoyad { get; set; }
    }
}