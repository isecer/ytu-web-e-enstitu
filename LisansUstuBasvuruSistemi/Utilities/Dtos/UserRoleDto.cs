using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class UserRoleDto
    {

        public int? YetkiGrupID { get; set; }
        public string YetkiGrupAdi { get; set; }
        public List<Roller> EklenenRoller { get; set; } = new List<Roller>();
        public List<Roller> YetkiGrupRolleri { get; set; } = new List<Roller>();
        public List<Roller> TumRoller { get; set; } = new List<Roller>();
    }
}