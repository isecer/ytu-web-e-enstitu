using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ResultExtension
    {
        public static JsonResult ToJsonResult(this object obj)
        {
            var jsr = new JsonResult
            {
                ContentEncoding = System.Text.Encoding.UTF8,
                ContentType = "application/json",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = obj
            };
            return jsr;
        }
        //public static JsonResult ToJsonResult(this object obj)
        //{
        //    var jsr = new JsonResult
        //    {
        //        ContentEncoding = System.Text.Encoding.UTF8,
        //        ContentType = "application/json",
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Data = obj
        //    };
        //    return jsr;
        //}
    }
}