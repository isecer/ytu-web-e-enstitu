using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetYayinKontrolDurumDto : MezuniyetYayinKontrolDurumlari
    {
        public string DurumAdi { get; set; }
    }
}