using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    public class SikcaSorulanSorularController : Controller
    {
        // GET: SikcaSorulanSorular 
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var ssS = _entities.SikcaSorulanSorulars.Where(p => p.EnstituKod == enstituKod .ToLower());
            var yetki = RoleNames.SssKayit.InRoleCurrent();
            if (yetki == false) ssS = ssS.Where(p => p.IsAktif); 
            return View(ssS.FirstOrDefault());
        }
        [Authorize(Roles = RoleNames.SssKayit)]
        public ActionResult Kayit(string ekd)
        {
            var mdl = new SikcaSorulanSorular();
            var mmMessage = new MmMessage();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            var sss = _entities.SikcaSorulanSorulars.FirstOrDefault(p => p.EnstituKod == enstituKod .ToLower());
            if (sss != null)
            {
                if (UserIdentity.Current.EnstituKods.Contains(sss.EnstituKod))
                {
                    mdl = sss;
                }
                else
                {
                    mmMessage.Messages.Add("Bu enstitü için işlem yapmaya yetkili değilsiniz!");
                    MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
                    return RedirectToAction("Index", new { EKD = ekd });
                }
            }
            

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(false), "Value", "Caption", mdl.EnstituKod ?? enstituKod);
            ViewBag.MmMessage = mmMessage;
            return View(mdl);
        }
        [HttpPost]
        [ValidateInput(false)]
        [Authorize(Roles = RoleNames.SssKayit)]
        public ActionResult Kayit(SikcaSorulanSorular kmodel)
        {
            var mmMessage = new MmMessage();
            if (kmodel.EnstituKod.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Sıkça sorulan soruların yayınlanacağı Enstitüyü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });
            if (kmodel.Aciklama.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Sıkça sorulan sorular için açıklamaları giriniz!");
            }
            if (mmMessage.Messages.Count == 0)
            {
                kmodel.IslemTarihi = DateTime.Now;
                kmodel.IslemYapanID = UserIdentity.Current.Id;
                kmodel.IslemYapanIP = UserIdentity.Ip;
                if (_entities.SikcaSorulanSorulars.Any(a => a.EnstituKod == kmodel.EnstituKod) == false)
                {
                    _entities.SikcaSorulanSorulars.Add(kmodel);
                }
                else
                {
                    var sss = _entities.SikcaSorulanSorulars.First(a => a.EnstituKod == kmodel.EnstituKod);
                    sss.EnstituKod = kmodel.EnstituKod; 
                    sss.Aciklama = kmodel.Aciklama;
                    sss.AciklamaHtml = kmodel.AciklamaHtml;
                    sss.IsAktif = kmodel.IsAktif;
                    sss.IslemTarihi = DateTime.Now;
                    sss.IslemYapanID = UserIdentity.Current.Id;
                    sss.IslemYapanIP = UserIdentity.Ip;

                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(false), "Value", "Caption", kmodel.EnstituKod);
            ViewBag.MmMessage = mmMessage;
            return View(kmodel);
        }
    }
}