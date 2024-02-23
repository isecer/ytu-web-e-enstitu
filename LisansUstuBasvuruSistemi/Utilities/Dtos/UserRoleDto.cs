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
        public List<Roller> EklenenRoller { get; set; }
        public List<Roller> YetkiGrupRolleri { get; set; }
        public List<Roller> TumRoller { get; set; }
        public UserRoleDto()
        {
            EklenenRoller = new List<Roller>();
            YetkiGrupRolleri = new List<Roller>();
            TumRoller = new List<Roller>();
        }
    }
}