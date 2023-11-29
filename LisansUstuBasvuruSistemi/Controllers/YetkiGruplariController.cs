using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.YetkiGruplari)]
    public class YetkiGruplariController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        { 
            return Index(new FmYetkiGruplari());
        }
        [HttpPost]
        public ActionResult Index(FmYetkiGruplari model)
        {
            var q = from s in _entities.YetkiGruplaris
                    select new
                    {
                        s.YetkiGrupID,
                        s.YetkiGrupAdi,
                        YetkiSayisi = s.YetkiGrupRolleris.Count,
                        FBEYetkiliSayisi = s.Kullanicilars.Count(p => p.EnstituKod == EnstituKodlariEnum.FenBilimleri),
                        SBEYetkiliSayisi = s.Kullanicilars.Count(p => p.EnstituKod == EnstituKodlariEnum.SosyalBilimleri),
                        TETYetkiliSayisi = s.Kullanicilars.Count(p => p.EnstituKod == EnstituKodlariEnum.TemizEnerjiTeknolojileri)
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
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new YetkiGruplari();
            if (id.HasValue && id > 0)
            {
                var data = _entities.YetkiGruplaris.FirstOrDefault(p => p.YetkiGrupID == id);
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
            var menuK = _entities.Menulers.Where(a => a.BagliMenuID == 0 && kategr.Contains(a.MenuAdi)).ToList();
            var dct = new List<CmbIntDto>();
            foreach (var item in menuK)
            {
                dct.Add(new CmbIntDto { Value = item.SiraNo.Value, Caption = item.MenuAdi });
            }
            ViewBag.cats = dct;
            ViewBag.MmMessage = mmMessage;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(YetkiGruplari model, List<int> rolId)
        {
            rolId = rolId ?? new List<int>();
            var mmMessage = new MmMessage();
            if (model.YetkiGrupAdi.IsNullOrWhiteSpace())
            {
                const string msg = "Yetki Grup Adı Giriniz.";
                mmMessage.Messages.Add(msg);
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YetkiGrupAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YetkiGrupAdi" });
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

                    _entities.YetkiGruplaris.Add(model);
                    _entities.SaveChanges();
                }
                else
                {
                    var yg = _entities.YetkiGruplaris.First(p => p.YetkiGrupID == model.YetkiGrupID);
                    yg.YetkiGrupAdi = model.YetkiGrupAdi;
                }
                var eskiROl = _entities.YetkiGrupRolleris.Where(p => p.YetkiGrupID == model.YetkiGrupID).ToList();
                _entities.YetkiGrupRolleris.RemoveRange(eskiROl);
                foreach (var item in rolId)
                {
                    _entities.YetkiGrupRolleris.Add(new YetkiGrupRolleri { YetkiGrupID = model.YetkiGrupID, RolID = item });
                }
                _entities.SaveChanges();
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
            var kayit = _entities.YetkiGruplaris.FirstOrDefault(p => p.YetkiGrupID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.YetkiGrupAdi + "' Yetki Grubu Silindi!";
                    _entities.YetkiGruplaris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.YetkiGrupAdi + "' Yetki Grubu Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "YetkiGruplari/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.OnemsizHata);
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
