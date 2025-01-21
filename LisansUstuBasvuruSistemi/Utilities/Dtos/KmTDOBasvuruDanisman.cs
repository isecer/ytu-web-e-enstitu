using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmTdoBasvuruDanisman : TDOBasvuruDanisman
    {
        public bool? IsCopy;

        public new bool? IsTezDiliTr { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public SelectList SListSinav { get; set; }
        public SelectList SListSinavNot { get; set; }
        public SelectList SListTdAnabilimDali { get; set; }
        public SelectList SListTdProgram { get; set; }
        public SelectList SListTDoDanismanTalepTip { get; internal set; }
         

        
    }
}