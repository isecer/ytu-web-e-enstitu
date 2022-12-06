using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.KmDtos
{
    public class KmTDOBasvuruDanisman : TDOBasvuruDanisman
    {
        public bool? IsTezDiliTr { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public SelectList SListSinav { get; set; }
        public SelectList SListSinavNot { get; set; }
        public SelectList SListTDAnabilimDali { get; set; }
        public SelectList SListTDProgram { get; set; }
        public SelectList SListTDSinav { get; set; }
        public SelectList SListTDSinavNot { get; set; }
    }
}