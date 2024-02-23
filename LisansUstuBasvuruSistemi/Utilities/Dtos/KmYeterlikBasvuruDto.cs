using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{ 
    public class KmYeterlikBasvuruDto : YeterlikBasvuru
    {
        public new Guid? UniqueID { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
    }

   
}