using LisansUstuBasvuruSistemi.Utilities.Extensions;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi
{
    public class LowercaseUrlAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var url = filterContext.HttpContext.Request.Url;
            var lowercaseUrl = url.ToString().ToLowerTurkish();

            if (url.ToString() != lowercaseUrl)
            {
                filterContext.Result = new RedirectResult(lowercaseUrl);
            }
        }
    }
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {            
            filters.Add(new HandleErrorAttribute());
            //filters.Add(new LowercaseUrlAttribute());
        }
    }
}