using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.ObsBirimler)]
    public class ObsBirimlerController : Controller
    {
        // GET: /Obs/
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return await Index(new FmObsProgramDto());
        }
        [HttpPost]
        public async Task<ActionResult> Index(FmObsProgramDto model)
        {
            var list = await ObsProgramCacheService.GetProgramsFlatAsync();
            ViewBag.Fakulteler = new SelectList(list.Select(x => new { x.FakulteKod, x.FakulteAd }).Distinct().OrderBy(o => o.FakulteAd), "FakulteKod", "FakulteAd", model.FakulteKod);
            ViewBag.Bolumler = new SelectList(list.Select(x => new { x.BolumId, x.BolumAd }).Distinct().OrderBy(o => o.BolumAd), "BolumId", "BolumAd", model.BolumId);

            // Filtreleme
            if (!string.IsNullOrWhiteSpace(model.FakulteKod))
                list = list.Where(x => x.FakulteKod == model.FakulteKod).ToList();
            if (!string.IsNullOrWhiteSpace(model.BolumId))
                list = list.Where(x => x.BolumId == model.BolumId).ToList();
            if (!string.IsNullOrWhiteSpace(model.ProgramId))
                list = list.Where(x => x.ProgramId == model.ProgramId).ToList();
            if (!model.ProgramAd.IsNullOrWhiteSpace())
            {
                var q = model.ProgramAd.Trim();

                list = list.Where(x =>
                        ContainsTrIgnoreCase(x.ProgramAd, q) ||
                        string.Equals(x.ProgramId, q, StringComparison.CurrentCultureIgnoreCase)
                    )
                    .ToList();
            }
            model.RowCount = list.Count;
            model.Programlar = list.Skip(model.StartRowIndex).Take(model.PageSize);

            // Dropdownlar
             
            return View(model);
        }
        static bool ContainsTrIgnoreCase(string source, string term)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(term))
                return false;

            // Türkçe i/İ sorunları için CurrentCulture/CompareInfo kullanıyoruz
            return CultureInfo.GetCultureInfo("tr-TR")
                .CompareInfo
                .IndexOf(source, term, CompareOptions.IgnoreCase) >= 0;
        }
    }
}