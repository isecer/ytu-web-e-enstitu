using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{

    public class LoginAjaxDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public string ReturnUrl { get; set; }
        public string ReturnUrlChanged { get; set; }
        public string KayitliEnstituAdi { get; set; }
        public string CurrentEnstituAdi { get; set; }
        public string NewGuid { get; set; }
        public string NewSrc { get; set; }
    }
}