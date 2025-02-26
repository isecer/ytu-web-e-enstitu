using System;
using System.Linq;
using System.Web.Mvc;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KrTIBasvuruAraRaporKomite : TIBasvuruAraRaporKomite
    {

        public SelectList SlistUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
    }
}