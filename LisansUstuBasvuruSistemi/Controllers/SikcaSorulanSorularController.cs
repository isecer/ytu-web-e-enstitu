using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    public class SikcaSorulanSorularController : Controller
    {
        // GET: SikcaSorulanSorular 
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var ssS = db.SikcaSorulanSorulars.Where(p => p.EnstituKod == _EnstituKod .ToLower());
            var yetki = RoleNames.SSSKayit.InRoleCurrent();
            if (yetki == false) ssS = ssS.Where(p => p.IsAktif); 
            return View(ssS.FirstOrDefault());
        }
        [Authorize(Roles = RoleNames.SSSKayit)]
        public ActionResult Kayit(string EKD)
        {
            var mdl = new SikcaSorulanSorular();
            var MmMessage = new MmMessage();
            var _EnstituKod = Management.getSelectedEnstitu(EKD);

            var sss = db.SikcaSorulanSorulars.Where(p => p.EnstituKod == _EnstituKod .ToLower()).FirstOrDefault();
            if (sss != null)
            {
                if (UserIdentity.Current.EnstituKods.Contains(sss.EnstituKod))
                {
                    mdl = sss;
                }
                else
                {
                    MmMessage.Messages.Add("Bu enstitü için işlem yapmaya yetkili değilsiniz!");
                    MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
                    return RedirectToAction("Index", new { EKD = EKD });
                }
            }
            

            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(false), "Value", "Caption", mdl.EnstituKod ?? _EnstituKod);
            ViewBag.MmMessage = MmMessage;
            return View(mdl);
        }
        [HttpPost]
        [ValidateInput(false)]
        [Authorize(Roles = RoleNames.SSSKayit)]
        public ActionResult Kayit(SikcaSorulanSorular kmodel)
        {
            var MmMessage = new MmMessage();
            if (kmodel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Sıkça sorulan soruların yayınlanacağı Enstitüyü Seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kmodel.Aciklama.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Sıkça sorulan sorular için açıklamaları giriniz!");
            }
            if (MmMessage.Messages.Count == 0)
            {
                kmodel.IslemTarihi = DateTime.Now;
                kmodel.IslemYapanID = UserIdentity.Current.Id;
                kmodel.IslemYapanIP = UserIdentity.Ip;
                if (db.SikcaSorulanSorulars.Any(a => a.EnstituKod == kmodel.EnstituKod) == false)
                {
                    db.SikcaSorulanSorulars.Add(kmodel);
                }
                else
                {
                    var sss = db.SikcaSorulanSorulars.Where(a => a.EnstituKod == kmodel.EnstituKod).First();
                    sss.EnstituKod = kmodel.EnstituKod; 
                    sss.Aciklama = kmodel.Aciklama;
                    sss.AciklamaHtml = kmodel.AciklamaHtml;
                    sss.IsAktif = kmodel.IsAktif;
                    sss.IslemTarihi = DateTime.Now;
                    sss.IslemYapanID = UserIdentity.Current.Id;
                    sss.IslemYapanIP = UserIdentity.Ip;

                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(false), "Value", "Caption", kmodel.EnstituKod);
            ViewBag.MmMessage = MmMessage;
            return View(kmodel);
        }
    }
}