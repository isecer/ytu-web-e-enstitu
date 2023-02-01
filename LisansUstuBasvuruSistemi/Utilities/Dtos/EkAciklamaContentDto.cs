using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class EkAciklamaContentDto
    {
        public string Baslik { get; set; }
        public List<CmbStringDto> Detay { get; set; }
        public EkAciklamaContentDto()
        {
            Detay = new List<CmbStringDto>();
        }
    }
}