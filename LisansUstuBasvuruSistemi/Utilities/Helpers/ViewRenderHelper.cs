using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; 
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class ViewRenderHelper
    {
        public static string RenderPartialView(string controllerName, string partialView, object model)
        { 
            if (HttpContext.Current == null)
                HttpContext.Current = new HttpContext(
                                        new HttpRequest(null, "http://www.lisansustu.yildiz.edu.tr", null),
                                        new HttpResponse(null));
            var context = new HttpContextWrapper(System.Web.HttpContext.Current) as HttpContextBase;
            var routes = new System.Web.Routing.RouteData();
            routes.Values.Add("controller", controllerName);
            var requestContext = new System.Web.Routing.RequestContext(context, routes);
            string requiredString = requestContext.RouteData.GetRequiredString("controller");
            var controllerFactory = ControllerBuilder.Current.GetControllerFactory();
            var controller = controllerFactory.CreateController(requestContext, requiredString) as ControllerBase;
            controller.ControllerContext = new ControllerContext(context, routes, controller);
            var ViewData = new ViewDataDictionary();
            var TempData = new TempDataDictionary();
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, partialView);
                var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
           
        } 
        public static IHtmlString ToRenderPartialViewHtml(this object model, string controllerName, string partialView)
        {
            var strView = RenderPartialView(controllerName, partialView, model);
            return new HtmlString(strView);
        }
      
    }
}