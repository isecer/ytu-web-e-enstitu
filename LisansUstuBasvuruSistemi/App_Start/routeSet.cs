using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using LisansUstuBasvuruSistemi.Models; 
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.App_Start
{
    public class RouteSet
    {
        public interface IRouteConstraint
        {
            bool Match(HttpContextBase httpContext, Route route, string parameterName,
                RouteValueDictionary values, RouteDirection routeDirection);
        }
    }
    public class EnstituListConstraint : IRouteConstraint
    {
        public EnstituListConstraint(params string[] values)
        {
            //var Enstitulers = Management.GetEnstituler();
            //values = Enstitulers.Where(p => p.IsAktif).Select(s => s.EnstituKisaAd.ToLower()).ToArray();
            values = new List<string> { "fbe", "sbe" }.ToArray();
            this._values = values;
        }

        private readonly string[] _values;

        public bool Match(HttpContextBase httpContext,
            Route route,
            string ekd,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            // Get the value called "parameterName" from the 
            // RouteValueDictionary called "value"
            string value = values[ekd].ToString().ToLower();

            // Return true is the list of allowed values contains 
            // this value.
            return _values.Contains(value);
        }
    }
    //public class CultureListConstraint : IRouteConstraint
    //{
    //    public CultureListConstraint(params string[] values)
    //    {
    //        var SistemDilleris = Management.GetDiller();
    //        values = SistemDilleris.Select(s => s.DilKodu.ToLower()).ToArray();

    //        this._values = values;
    //    }

    //    private string[] _values;

    //    public bool Match(HttpContextBase httpContext,
    //        Route route,
           
    //        RouteValueDictionary values,
    //        RouteDirection routeDirection)
    //    {
    //        // Get the value called "parameterName" from the 
    //        // RouteValueDictionary called "value"
    //        string value = values[Culture].ToString().ToLower();

    //        // Return true is the list of allowed values contains 
    //        // this value.
    //        return _values.Contains(value);
    //    }
    //}

}