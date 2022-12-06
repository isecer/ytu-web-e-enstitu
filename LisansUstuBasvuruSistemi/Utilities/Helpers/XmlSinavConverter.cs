using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using Newtonsoft.Json.Linq;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class XmlSinavConverter
    {
        public static SinavSonucAlesXmlModel toSinavSonucAlesXmlModel(this string obj)
        {
            var xml = new XmlDocument();
            xml.LoadXml(obj);
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xml);
            var jobject = JObject.Parse(jsonString);
            var output = jobject.Children<JProperty>().Select(prop => prop.Value.ToObject<SinavSonucAlesXmlModel>()).FirstOrDefault();

            return output;
        }
        public static double? toSinavSonucAlesMaxNot(this List<int> AlesTips, string xmlstring)
        {
            var sonuclar = xmlstring.toSinavSonucAlesXmlModel();
            var maxNot = new Dictionary<int, double>();
            if (AlesTips.Any(a => a == AlesTipBilgi.Sayısal)) maxNot.Add(AlesTipBilgi.Sayısal, sonuclar.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
            if (AlesTips.Any(a => a == AlesTipBilgi.Sözel)) maxNot.Add(AlesTipBilgi.Sözel, sonuclar.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
            if (AlesTips.Any(a => a == AlesTipBilgi.EşitAğırlık)) maxNot.Add(AlesTipBilgi.EşitAğırlık, sonuclar.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
            return maxNot.Select(s => s.Value).Max();
        }
        public static SinavSonucDilXmlModel toSinavSonucDilXmlModel(this string obj)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(obj);
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xml);
                var jobject = JObject.Parse(jsonString);
                var output = jobject.Children<JProperty>().Select(prop => prop.Value.ToObject<SinavSonucDilXmlModel>()).FirstOrDefault();
                return output;
            }
            catch
            {

                return Newtonsoft.Json.JsonConvert.DeserializeObject<SinavSonucDilXmlModel>(obj);


            }


        }
        public static double? toSinavSonucDil(this string obj, int BasvuruID = 0)
        {
            return obj.toSinavSonucDilXmlModel().PUAN.ToDouble();

        }
    }
}