using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData.Models;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsRestData
{
    public static class ObsRestApiService
    {
        private const string ObsUserName = "YtuObsApiMiner";
        private const string ObsPassword = "%2321%2BYtu%2B%2122%2DPro%2A%23";
        public static async Task<List<ObsServiceDonemDto>> GetDonemler()
        {
            using (var client = new HttpClient())
            {
                string apiUrl = $"https://obsservice.yildiz.edu.tr/ProlizYtuObsGenericRest/api/Genel/Donem?userName={ObsUserName}&userPass={ObsPassword}";

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseModel = JsonConvert.DeserializeObject<ObsApiResponseDonem>(jsonResponse);
                    var donems = responseModel.donem
                        .Where(t => t.TIP == "B" || t.TIP == "G")
                        .OrderByDescending(t => int.Parse(t.YIL))
                        .ThenBy(t => t.TIP == "B" ? 1 : 2)
                        .ToList();
                    foreach (var item in donems)
                    {

                        item.DonemId = item.YIL + (item.AD.Contains("Güz") ? "1" : "2");
                    }

                    return donems;
                }
                return null;
            }
        }
        public static async Task<List<CmbStringDto>> GetCmbDonemler()
        { 
            var donemler = await GetDonemler();
            return donemler.Select(s => new CmbStringDto { Caption = s.AD, Value = s.DonemId }).ToList(); 
        }
        public static async Task<ObsServiceDonemDto> GetAktifDonem()
        {
            using (var client = new HttpClient())
            {
                string apiUrl = $"https://obsservice.yildiz.edu.tr/ProlizYtuObsGenericRest/api/Genel/Donem?userName={ObsUserName}&userPass={ObsPassword}";

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseModel = JsonConvert.DeserializeObject<ObsApiResponseDonem>(jsonResponse);
                    var latestTerm = responseModel.donem
                        .Where(t => t.TIP == "B" || t.TIP == "G")
                        .OrderByDescending(t => int.Parse(t.YIL))
                        .ThenBy(t => t.TIP == "B" ? 1 : 2)
                        .FirstOrDefault();

                    if (latestTerm != null)
                    {
                        latestTerm.DonemId = latestTerm.YIL + (latestTerm.AD.Contains("Güz") ? "1" : "2");
                    }

                    return latestTerm;
                }
                return null;
            }
        }

    }


}