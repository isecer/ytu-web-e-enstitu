using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData.Models;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsRestData
{
    public static class ObsProgramCacheService
    {
        private const string ObsUserName = "YtuObsApiMiner";
        private const string ObsPassword = "%2321%2BYtu%2B%2122%2DPro%2A%23";
        private const string BaseUrl = "https://obsservice.yildiz.edu.tr/ProlizYtuObsGenericRest/api/Genel/";

        private const string CacheKey = "OBS_PROGRAM_FULL_LIST";
        private static readonly MemoryCache Cache = MemoryCache.Default;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

        public static async Task<List<ObsProgramFullDto>> GetProgramsFlatAsync()
        {
            if (Cache.Contains(CacheKey))
                return Cache.Get(CacheKey) as List<ObsProgramFullDto>;

            var flatList = await FetchProgramsFromApiAsync();

            Cache.Set(CacheKey, flatList, new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.Add(CacheDuration)
            });

            return flatList;
        }

        private static async Task<List<ObsProgramFullDto>> FetchProgramsFromApiAsync()
        {
            var result = new List<ObsProgramFullDto>();

            using (var client = new HttpClient())
            {
                string fakulteUrl = $"{BaseUrl}Fakulte?userName={ObsUserName}&userPass={ObsPassword}";
                string fakulteJson = await client.GetStringAsync(fakulteUrl);
                var fakulteResponse = JsonConvert.DeserializeObject<ObsApiResponseDonem>(fakulteJson);
                var fakulteler = fakulteResponse?.fakulte ?? new List<FakulteItem>();
                var enstituKodlari = new List<string>
                {
                    "1"+EnstituKodlariEnum.FenBilimleri,  "1"+EnstituKodlariEnum.SosyalBilimleri,
                    "1"+EnstituKodlariEnum.TemizEnerjiTeknolojileri
                };
                fakulteler = fakulteler.Where(p => enstituKodlari.Contains(p.KOD)).ToList();
                foreach (var fakulte in fakulteler)
                {
                    string bolumUrl =
                        $"{BaseUrl}Bolum?userName={ObsUserName}&userPass={ObsPassword}&fakulteKod={fakulte.KOD}";
                    string bolumJson = await client.GetStringAsync(bolumUrl);
                    var bolumResponse = JsonConvert.DeserializeObject<ObsApiResponseDonem>(bolumJson);
                    var bolumler = bolumResponse?.bolum ?? new List<BolumItem>();

                    foreach (var bolum in bolumler)
                    {
                        string programUrl =
                            $"{BaseUrl}Program?userName={ObsUserName}&userPass={ObsPassword}&bolumID={bolum.ID}";
                        string programJson = await client.GetStringAsync(programUrl);
                        var programResponse = JsonConvert.DeserializeObject<ObsApiResponseDonem>(programJson);
                        var programlar = programResponse?.program ?? new List<ProgramItem>();

                        foreach (var program in programlar)
                        {
                            result.Add(new ObsProgramFullDto
                            {
                                FakulteKod = fakulte.KOD,
                                FakulteAd = fakulte.AD,
                                BolumId = bolum.ID,
                                BolumKod = bolum.KOD,
                                BolumAd = bolum.AD,
                                ProgramId = program.PROG_ID,
                                ProgramKod = program.KOD,
                                ProgramAd = program.AD,
                                ProgramTur = program.PROG_TUR,
                                ProgramTip = program.PROG_TIP,
                                NormalSure = program.NORMAL_SURE,
                                AzamiSure = program.AZAMI_SURE
                            });
                        }
                    } 
                }
            }

            return result;
        }

        public static void ClearCache()
        {
            if (Cache.Contains(CacheKey))
                Cache.Remove(CacheKey);
        }
    }
}