using System.Web.Mvc;
using System.Web.Routing;

namespace LisansUstuBasvuruSistemi
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });
            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "file",
                url: "file/index",
                defaults: new { controller = "File", action = "Index", filePath = UrlParameter.Optional }
            );
           
            routes.MapRoute(
                name: "Default",
                url: "{EKD}/{controller}/{action}/{id}",
                defaults: new { EKD = "fbe", controller = "home", action = "index", id = UrlParameter.Optional },
                constraints: new { EKD = new EnstituListConstraint() }
            );
          
            routes.MapRoute(
                name: "NotFound",
                url: "{EKD}/{controller}/{action}/{id}",
                defaults: new { EKD = "fbe", controller = "AppEvent", action = "PageNotFound", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Error",
                url: "{EKD}/{controller}/{action}/{id}",
                defaults: new { EKD = "fbe", controller = "AppEvent", action = "Error", id = UrlParameter.Optional }
            );
        }
    }
}