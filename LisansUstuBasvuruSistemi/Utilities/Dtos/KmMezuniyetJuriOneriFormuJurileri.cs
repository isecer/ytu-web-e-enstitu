using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMezuniyetJuriOneriFormuJurileri : MezuniyetJuriOneriFormuJurileri
    {

        public SelectList SlistUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
    }
}