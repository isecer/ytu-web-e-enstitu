using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.YetkiGruplari)]
    public class YetkiGruplariController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        { 
            return Index(new FmYetkiGruplari());
        }
        [HttpPost]
        public ActionResult Index(FmYetkiGruplari model)
        {
            var q = from s in db.YetkiGruplaris
                    select new
                    {
                        s.YetkiGrupID,
                        s.YetkiGrupAdi,
                        YetkiSayisi = s.YetkiGrupRolleris.Count,
                        FBEYetkiliSayisi = s.Kullanicilars.Count(p => p.EnstituKod == EnstituKodlari.FenBilimleri),
                        SBEYetkiliSayisi = s.Kullanicilars.Count(p => p.EnstituKod == EnstituKodlari.SosyalBilimleri),
                        TETYetkiliSayisi = s.Kullanicilars.Count(p => p.EnstituKod == EnstituKodlari.TemizEnerjiTeknolojileri)
                    };
            if (!model.YetkiGrupAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.YetkiGrupAdi.Contains(model.YetkiGrupAdi));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(t => t.YetkiGrupAdi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrYetkiGruplari
            {
                YetkiGrupID = s.YetkiGrupID,
                YetkiGrupAdi = s.YetkiGrupAdi,
                YetkiSayisi = s.YetkiSayisi,
                FbeYetkiliSayisi = s.FBEYetkiliSayisi,
                SbeYetkiliSayisi = s.SBEYetkiliSayisi,
                TetYetkiliSayisi = s.TETYetkiliSayisi,

            }).ToArray();

            return View(model);
        }
        public ActionResult Kayit(int? id)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new YetkiGruplari();
            if (id.HasValue && id > 0)
            {
                var data = db.YetkiGruplaris.FirstOrDefault(p => p.YetkiGrupID == id);
                if (data != null) model = data;
            }

            var roles = RollerBus.GetAllRoles().ToList();
            var sRol = new List<Roller>();
            if (id.HasValue && id.Value > 0) sRol = YetkiGrupBus.GetYetkiGrupRoles(id.Value);

            var dataR = roles.Select(s => new CheckObject<Roller>
            {
                Value = s,
                Checked = sRol.Any(p => p.RolID == s.RolID)
            });
            ViewBag.Roller = dataR;
            var kategr = roles.Select(s => s.Kategori).Distinct().ToArray();
            var menuK = db.Menulers.Where(a => a.BagliMenuID == 0 && kategr.Contains(a.MenuAdi)).ToList();
            var dct = new List<CmbIntDto>();
            foreach (var item in menuK)
            {
                dct.Add(new CmbIntDto { Value = item.SiraNo.Value, Caption = item.MenuAdi });
            }
            ViewBag.cats = dct;
            ViewBag.MmMessage = MmMessage;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(YetkiGruplari model, List<int> rolId)
        {
            rolId = rolId ?? new List<int>();
            var mmMessage = new MmMessage();
            if (model.YetkiGrupAdi.IsNullOrWhiteSpace())
            {
                string msg = "Yetki Grup Adı Giriniz.";
                mmMessage.Messages.Add(msg);
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YetkiGrupAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YetkiGrupAdi" });
            //if (RolID == null || RolID.Count == 0)
            //{
            //    string msg = "Yetki Grubuna Ait Rolleri Belirleyiniz!";
            //    MmMessage.Messages.Add(msg);
            //}
            if (mmMessage.Messages.Count == 0)
            {
                model.IslemYapanID = UserIdentity.Current.Id;
                model.IslemYapanIP = UserIdentity.Ip;
                model.IslemTarihi = DateTime.Now;
                if (model.YetkiGrupID == 0)
                {

                    db.YetkiGruplaris.Add(model);
                    db.SaveChanges();
                }
                else
                {
                    var yg = db.YetkiGruplaris.First(p => p.YetkiGrupID == model.YetkiGrupID);
                    yg.YetkiGrupAdi = model.YetkiGrupAdi;
                }
                var eskiROl = db.YetkiGrupRolleris.Where(p => p.YetkiGrupID == model.YetkiGrupID).ToList();
                db.YetkiGrupRolleris.RemoveRange(eskiROl);
                foreach (var item in rolId)
                {
                    db.YetkiGrupRolleris.Add(new YetkiGrupRolleri { YetkiGrupID = model.YetkiGrupID, RolID = item });
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            var roles = RollerBus.GetAllRoles().ToList();
            var sRol = new List<int>();
            if (rolId.Count > 0) sRol = rolId;

            var dataR = roles.Select(s => new CheckObject<Roller>
            {
                Value = s,
                Checked = sRol.Any(p => p == s.RolID)
            });
            ViewBag.Roller = dataR;
            ViewBag.MmMessage = mmMessage;
            return View(model);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.YetkiGruplaris.FirstOrDefault(p => p.YetkiGrupID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.YetkiGrupAdi + "' Yetki Grubu Silindi!";
                    db.YetkiGruplaris.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.YetkiGrupAdi + "' Yetki Grubu Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "YetkiGruplari/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Yetki Grubu sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}
