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
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Programlar)]
    public class ProgramlarController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmProgramlar { PageSize = 15 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmProgramlar model, string ekd)
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.Programlars
                    join sl in _entities.Programlars on new { s.ProgramKod } equals new { sl.ProgramKod } into defP
                    from slP in defP.DefaultIfEmpty()
                    join e in _entities.AnabilimDallaris on s.AnabilimDaliID equals e.AnabilimDaliID
                    join enst in _entities.Enstitulers on new { e.EnstituKod } equals new { enst.EnstituKod }
                    where enstKods.Contains(enst.EnstituKod)
                    select new FrProgramlar
                    {
                        AnabilimDaliID = s.AnabilimDaliID,
                        EnstituKod = enst.EnstituKod,
                        EnstituAd = enst.EnstituAd,
                        AnabilimDaliKod = e.AnabilimDaliKod,
                        AnabilimDaliAdi = e.AnabilimDaliAdi,
                        ProgramKod = s.ProgramKod,
                        ProgramAdi = slP != null ? slP.ProgramAdi : "",
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad

                    };
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.ProgramAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.ProgramAdi.Contains(model.ProgramAdi) || p.AnabilimDaliAdi.Contains(model.ProgramAdi) || p.ProgramKod == model.ProgramAdi);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.AnabilimDaliAdi).ThenBy(o => o.ProgramAdi);
            model.data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(string id, string ekd)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var model = new Programlar();
            if (id.IsNullOrWhiteSpace() == false)
            {
                var data = _entities.Programlars.FirstOrDefault(p => p.ProgramKod == id);
                if (data != null)
                {
                    model = data;
                    enstituKod = data.AnabilimDallari.EnstituKod;


                }
            }

            ViewBag.AnabilimDaliID = new SelectList(Management.CmbGetYetkiliAnabilimDallari(true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.Diller2 = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.KullaniciID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.OgrenciBolumID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");

            var roleName = new List<string>() { RoleNames.Programlar };
            var kuls = UserBus.GetRoluOlanKullanicilar(roleName, enstituKod);
            var rolls = KullanicilarBus.GetProgramYetkisiOlanKullanicilar(kuls, model.ProgramKod);
            ViewBag.KullaniciIDs = rolls.Where(p => p.Checked == true).Select(s => s.Value.KullaniciID).ToList();

            var oBolIds = _entities.BolumEslestirs.Where(p => p.ProgramKod == model.ProgramKod).Select(s => s.OgrenciBolumID).ToList();

            ViewBag.OldID = model.ProgramKod;
            ViewBag.EnstituKod = enstituKod;
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(Programlar kModel, string enstituKod, string oldId, List<int> kullaniciId)
        {
            var mmMessage = new MmMessage();
            string id = oldId.IsNullOrWhiteSpace() ? kModel.ProgramKod : oldId;
            #region Kontrol 

            if (kullaniciId == null) kullaniciId = new List<int>();


            if (id.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Program kodu boş bırakılamaz ve 0 dan büyük bir değer olmalıdır!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "ProgramKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "ProgramKod" });
            if (kModel.AnabilimDaliID <= 0)
            {
                mmMessage.Messages.Add("Anabilim Dalı seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnabilimDaliID" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "AnabilimDaliID" });
            if (kModel.ProgramKod.IsNullOrWhiteSpace() && oldId.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Kayıt işlemini yapabilmeni için Kod kısmını doldurmanız gerekmektedir!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "ProgramKod" });
            }
            if (kModel.ProgramAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Program adı boş bırakılamaz!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "ProgramAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "ProgramAdi" });
            if (mmMessage.Messages.Count == 0)
            {
                int newOrEd = oldId.IsNullOrWhiteSpace() ? 1 : 0;
                var cnt = _entities.Programlars.Count(p => p.ProgramKod == id) + newOrEd;
                if (cnt > 1)
                {
                    mmMessage.Messages.Add("Tanımlamak istediğiniz kod daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "ProgramKod" });
                }
            }


            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                var bolm = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == kModel.AnabilimDaliID);
                if (oldId.IsNullOrWhiteSpace())
                {
                    var prg = _entities.Programlars.Add(new Programlar
                    {
                        AnabilimDaliID = kModel.AnabilimDaliID,
                        AnabilimDaliKod = bolm.AnabilimDaliKod,
                        ProgramKod = id,
                        ProgramAdi = kModel.ProgramAdi,
                        IsAktif = true,

                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now
                    });
                    _entities.SaveChanges();
                    id = prg.ProgramKod;
                }
                else
                {
                    var data = _entities.Programlars.First(p => p.ProgramKod == id);
                    data.AnabilimDaliID = kModel.AnabilimDaliID;
                    data.AnabilimDaliKod = bolm.AnabilimDaliKod;
                    data.ProgramAdi = kModel.ProgramAdi;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;

                }

                var onceki = _entities.KullaniciProgramlaris.Where(p => p.ProgramKod == kModel.ProgramKod).ToList();
                _entities.KullaniciProgramlaris.RemoveRange(onceki);
                foreach (var item in kullaniciId)
                {
                    _entities.KullaniciProgramlaris.Add(new KullaniciProgramlari
                    {
                        KullaniciID = item,
                        ProgramKod = id
                    });


                }

                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }



            ViewBag.EnstituKod = enstituKod;
            ViewBag.MmMessage = mmMessage;
            ViewBag.OldID = oldId;
            ViewBag.AnabilimDaliID = new SelectList(Management.CmbGetYetkiliAnabilimDallari(true), "Value", "Caption", kModel.AnabilimDaliID);
            ViewBag.KullaniciID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.OgrenciBolumID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");

            ViewBag.KullaniciIDs = kullaniciId;

            return View(kModel);
        }
        public ActionResult ProgramKullanicilarYetki(int anabilimDaliId, string programKod)
        {
            var abd = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == anabilimDaliId);
            var roleName = new List<string>() { RoleNames.Programlar };
            var kuls = UserBus.GetRoluOlanKullanicilar(roleName, abd.EnstituKod);
            var rolls = KullanicilarBus.GetProgramYetkisiOlanKullanicilar(kuls, programKod).Select(s => new { Value = s.Value.KullaniciID, Caption = (s.Value.Ad + " " + s.Value.Soyad + " [" + s.Value.KullaniciAdi + "]") }).ToList();
            return Json(rolls, "application/json", JsonRequestBehavior.AllowGet);
        }



        public ActionResult Sil(string id)
        {
            var kayit = _entities.Programlars.FirstOrDefault(p => p.ProgramKod == id);
            var pAdi = _entities.Programlars.First(p => p.ProgramKod == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + pAdi.ProgramAdi + "' İsimli Program Silindi!";
                    _entities.Programlars.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + pAdi.ProgramAdi + "' İsimli Program Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Programlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Program sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
