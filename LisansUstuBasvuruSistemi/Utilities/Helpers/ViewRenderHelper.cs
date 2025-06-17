using System.IO;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class ViewRenderHelper
    {
        public static string RenderPartialView(string controllerName, string partialView, object model)
        {
            if (HttpContext.Current == null)
                HttpContext.Current = new HttpContext(
                                        new HttpRequest(null, "http://www.e-enstitu.yildiz.edu.tr", null),
                                        new HttpResponse(null));
            var context = new HttpContextWrapper(HttpContext.Current) as HttpContextBase;

            return RenderPartialView(context, controllerName, partialView, model);
            //var routes = new System.Web.Routing.RouteData();
            //routes.Values.Add("controller", controllerName);
            //var requestContext = new System.Web.Routing.RequestContext(context, routes);
            //var requiredString = requestContext.RouteData.GetRequiredString("controller");
            //var controllerFactory = ControllerBuilder.Current.GetControllerFactory();
            //var controller = controllerFactory.CreateController(requestContext, requiredString) as ControllerBase;
            //controller.ControllerContext = new ControllerContext(context, routes, controller);


            //using (var sw = new StringWriter())
            //{
            //    var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, partialView);
            //    var viewContext = new ViewContext(controller.ControllerContext,
            //        viewResult.View,
            //        new ViewDataDictionary() { Model = model },
            //        new TempDataDictionary(),
            //        sw);
            //    viewResult.View.Render(viewContext, sw);
            //    return sw.GetStringBuilder().ToString();
            //}

        }
        private static string RenderPartialView(HttpContextBase context, string controllerName, string partialView, object model)
        {
            var routes = new System.Web.Routing.RouteData();
            routes.Values.Add("controller", controllerName);

            // İstekten gelen RouteData'ları al
            foreach (var routeData in context.Request.RequestContext.RouteData.Values)
            {
                if (!routes.Values.ContainsKey(routeData.Key))
                {
                    routes.Values.Add(routeData.Key, routeData.Value);
                }
            }

            var requestContext = new System.Web.Routing.RequestContext(context, routes);
            var requiredString = requestContext.RouteData.GetRequiredString("controller");
            var controllerFactory = ControllerBuilder.Current.GetControllerFactory();
            var controller = controllerFactory.CreateController(requestContext, requiredString) as ControllerBase;
            controller.ControllerContext = new ControllerContext(context, routes, controller);

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, partialView);
                var viewContext = new ViewContext(controller.ControllerContext,
                    viewResult.View,
                    new ViewDataDictionary() { Model = model },
                    new TempDataDictionary(),
                    sw);
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