using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.LisansustuBasvuruRapor)]
    public class RaporlarLubController : Controller
    {
        // GET: RaporlarLUB
        public ActionResult Index(string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
           
            ViewBag.BasvuruSurecID = new SelectList(LisansustuBasvuruBus.GetbasvuruSurecleri(enstituKod, BasvuruSurecTipiEnum.LisansustuBasvuru ,true), "Value", "Caption");
            return View();
        }
    }
}