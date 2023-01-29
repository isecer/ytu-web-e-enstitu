using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class ekAciklamaContent
    {
        public string Baslik { get; set; }
        public List<CmbStringDto> Detay { get; set; }
        public ekAciklamaContent()
        {
            Detay = new List<CmbStringDto>();
        }
    }
}