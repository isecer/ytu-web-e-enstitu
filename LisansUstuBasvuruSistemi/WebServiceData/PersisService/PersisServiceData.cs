using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.WebServiceData.PersisService
{
    public class PersisServiceData
    {
        public static PersisServiceModel GetWsPersisOe(string term)
        {
            var cl = new Ws_Persis.Service1SoapClient("Service1Soap");

            var data = cl.irfan_veri("irfan", "irfan123", term);
            var dataPers = (PersisServiceModel)JsonConvert.DeserializeObject(data, typeof(PersisServiceModel));

            return dataPers;
        }

    }
}