using System;
using System.Collections.Generic;
using System.Linq;
using System.Web; 
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmDonemProjesiMuafOgrenciler: PagerModel
    {
        public string AdSoyad { get; set; }
        public IEnumerable<FrDonemProjesiMuafOgrenciler> DonemProjesiMuafOgrencilers { get; set; }
    }

    public class FrDonemProjesiMuafOgrenciler : DonemProjesiMuafOgrenciler
    {
        public string AdSoyad { get; set; } 
    }
}