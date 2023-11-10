using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MbYayinEkleViewDto
    {
        public MbYayinEkleViewDto(SelectList selectMezuniyetYayinTurId)
        {
            SelectMezuniyetYayinTurId = selectMezuniyetYayinTurId;
        }

        public int MezuniyetSurecId { get; set; }
        public SelectList SelectMezuniyetYayinTurId { get; set; }
    }
}