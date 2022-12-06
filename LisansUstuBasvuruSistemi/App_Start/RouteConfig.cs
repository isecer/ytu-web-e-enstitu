using LisansUstuBasvuruSistemi.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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


            routes.MapRoute(
                            name: "Default",
                            url: "{EKD}/{controller}/{action}/{id}",
                            defaults: new { EKD = "fbe", controller = "home", action = "index", id = UrlParameter.Optional },
                            constraints: new { EKD = new EnstituListConstraint() }
                       );

            //routes.Add("DefaultDil",
            // new Route("{EKD}/{Culture}/{controller}/{action}/{id}",
            //                 new RouteValueDictionary(new { controller = "Home", action = "Index", id = UrlParameter.Optional }),
            //                 new MyRouteHandler()
            //     ));

            routes.MapRoute(
                               name: "NotFound",
                               url: "{EKD}/{controller}/{action}/{id}",
                               defaults: new { EKD = "fbe", controller = "AppEvent", action = "PageNotFound", id = UrlParameter.Optional }
                               );  // 404s

            routes.MapRoute(
                                   name: "Error",
                                   url: "{EKD}/{controller}/{action}/{id}",
                                   defaults: new { EKD = "fbe", controller = "AppEvent", action = "Error", id = UrlParameter.Optional }
                              );

            
 


        }
        public class MyRouteHandler : IRouteHandler
        {
            public IHttpHandler GetHttpHandler(RequestContext requestContext)
            {
                var routeData = requestContext.RouteData;
                var culture = routeData.Values["culture"].ToString();
                var controller = routeData.Values["controller"].ToString();
                routeData.Values["controller"] = culture;
                var action = routeData.Values["action"].ToString();
                routeData.Values["action"] = controller;
                var handler = new MvcHandler(requestContext);
                return handler;
            }
        }
    }
}