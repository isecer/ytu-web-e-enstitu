using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RprModelProgramAgnoKriterBilgi
    {
        public string AgnoAlimTipi { get; set; }
        public List<CmbStringDto> OgretimKriterleri { get; set; }
        public RprModelProgramAgnoKriterBilgi()
        {
            OgretimKriterleri = new List<CmbStringDto>();
        }
    }
}