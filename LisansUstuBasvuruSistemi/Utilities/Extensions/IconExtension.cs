using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class IconExtension
    {
        public static HtmlString ToOnayDurumIconHtml(this bool? isOnaylandi)
        {
            string iconString;
            if (!isOnaylandi.HasValue) iconString = "<i class='fa fa-clock-o'></i>";
            else if (isOnaylandi.Value) iconString = "<i class='fa fa-thumbs-o-up' style='color:green;'></i>";
            else iconString = "<i class='fa fa-thumbs-o-down' style='color:maroon;'></i>";
            return new HtmlString(iconString);
        }
     
        public static HtmlString ToOnayDurumIconLgHtml(this bool? isOnaylandi)
        {
            string iconString;
            if (!isOnaylandi.HasValue) iconString = "<i class='fa fa-clock-o fa-lg'></i>";
            else if (isOnaylandi.Value) iconString = "<i class='fa fa-thumbs-o-up fa-lg' style='color:green;'></i>";
            else iconString = "<i class='fa fa-thumbs-o-down fa-lg' style='color:maroon;'></i>";
            return new HtmlString(iconString);
        }
        public static HtmlString ToEnstituOnayDurumIDIconLgHtml(this int? enstituBasvuruOnayDurumId)
        {
            string iconString;
            if (!enstituBasvuruOnayDurumId.HasValue) iconString = "<i class='fa fa-clock-o fa-lg'></i>";
            else if (enstituBasvuruOnayDurumId==DonemProjesiEnstituOnayDurumEnum.KabulEdildi) iconString = "<i class='fa fa-thumbs-o-up fa-lg' style='color:green;'></i>";
            else iconString = "<i class='fa fa-thumbs-o-down fa-lg' style='color:maroon;'></i>";
            return new HtmlString(iconString);
        }
    }
}