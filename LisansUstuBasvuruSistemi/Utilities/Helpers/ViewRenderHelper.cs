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
            //try
            //{


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
            //}
            //catch (Exception ex)
            //{
            //    SistemBilgisiKaydet("View Render Edilirken Bir Hata Oluştu!\r\nViewPath:" + controllerName + "/" + partialView + " \r\nhata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipi.Hata);
            //    return "";
            //}
        }
        public static string RenderPartialViewx(string controllerName, string partialView, object model)
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
            var strView = RenderPartialViewx(controllerName, partialView, model);
            return new HtmlString(strView);
        }

        public static IHtmlString ToMezuniyetDurum(this FrMezuniyetBasvurulari model)
        {


            var PagerString = model.ToRenderPartialViewHtml("Mezuniyet", "BasvuruDurumView");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetayBasvuru(this MezuniyetBasvuruDetayDto model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t1_Basvuru");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetayEYKSureci(this MezuniyetBasvuruDetayDto model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t2_EYKSureci");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetaySinavSureci(this MezuniyetBasvuruDetayDto model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t3_SinavSureci");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetayTezKontrolSureci(this MezuniyetBasvuruDetayDto model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t4_TezKontrolSureci");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetayMezuniyetSureci(this MezuniyetBasvuruDetayDto model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t5_MezuniyetSureci");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
    }
}