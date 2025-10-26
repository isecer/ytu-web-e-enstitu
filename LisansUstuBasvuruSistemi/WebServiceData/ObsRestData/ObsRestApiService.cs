using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
namespace LisansUstuBasvuruSistemi.WebServiceData.ObsRestData
{
    public static class ObsRestApiService
    {
        private const string ObsUserName = "YtuObsApiMiner";
        private const string ObsPassword = "%2321%2BYtu%2B%2122%2DPro%2A%23";
        private const string BaseUrl = "https://obsservice.yildiz.edu.tr/ProlizYtuObsGenericRest/api/Genel/";

        #region Donem İşlemleri
        public static async Task<List<ObsServiceDonemDto>> GetDonemler()
        {
            using (var client = new HttpClient())
            {
                string apiUrl = $"{BaseUrl}Donem?userName={ObsUserName}&userPass={ObsPassword}";
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
            return donemler.Select(s => new CmbStringDto
            {
                Caption = s.AD,
                Value = s.DonemId
            }).ToList();
        }

        public static async Task<ObsServiceDonemDto> GetAktifDonem()
        {
            using (var client = new HttpClient())
            {
                string apiUrl = $"{BaseUrl}Donem?userName={ObsUserName}&userPass={ObsPassword}";
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
        #endregion

        #region Fakülte / Bölüm / Program
        public static async Task<List<FakulteItem>> GetFakulteler()
        {
            using (var client = new HttpClient())
            {
                string apiUrl = $"{BaseUrl}Fakulte?userName={ObsUserName}&userPass={ObsPassword}";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ObsApiResponseDonem>(jsonResponse);
                    return result.fakulte ?? new List<FakulteItem>();
                }

                return new List<FakulteItem>();
            }
        }

        public static async Task<List<BolumItem>> GetBolumler(string fakulteKod)
        {
            using (var client = new HttpClient())
            {
                string apiUrl = $"{BaseUrl}Bolum?userName={ObsUserName}&userPass={ObsPassword}&fakulteKod={fakulteKod}";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ObsApiResponseDonem>(jsonResponse);
                    return result.bolum ?? new List<BolumItem>();
                }

                return new List<BolumItem>();
            }
        }

        public static async Task<List<ProgramItem>> GetProgramlar(string bolumId)
        {
            using (var client = new HttpClient())
            {
                string apiUrl = $"{BaseUrl}Program?userName={ObsUserName}&userPass={ObsPassword}&bolumID={bolumId}";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ObsApiResponseDonem>(jsonResponse);
                    return result.program ?? new List<ProgramItem>();
                }

                return new List<ProgramItem>();
            }
        }
        #endregion
    }

}