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
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemData;


namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.SrOzelTanimlar)]
    public class SrOzelTanimlarController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmOzelTanimlar());
        }
        [HttpPost]
        public ActionResult Index(FmOzelTanimlar model)
        {
            var q = from s in _entities.SROzelTanimlars
                    join a in _entities.Aylars on s.Ay equals a.AyID into def1
                    from def in def1.DefaultIfEmpty()
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    join ott in _entities.SROzelTanimTipleris on s.SROzelTanimTipID equals ott.SROzelTanimTipID
                    join sln in _entities.SRSalonlars on s.SRSalonID equals sln.SRSalonID into defs
                    from defSln in defs.DefaultIfEmpty()
                    join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID into deft
                    from defTt in deft.DefaultIfEmpty()

                    select new FrOzelTanimlar
                    {
                        SROzelTanimID = s.SROzelTanimID,
                        SROzelTanimTipID = s.SROzelTanimTipID,
                        SROzelTanimTipAdi = ott.SROzelTanimTipAdi,
                        TalepTipAdi = defTt != null ? defTt.TalepTipAdi : "",
                        SRSalonID = s.SRSalonID,
                        SalonAdi = s.SRSalonID.HasValue ? defSln.SalonAdi : "", 
                        Tarih = s.Tarih,
                        Ay = s.Ay,
                        AyAdi = def != null ? def.AyAdi : "",
                        Gun = s.Gun,
                        BasTarih = s.BasTarih,
                        BitTarih = s.BitTarih,
                        Aciklama = s.Aciklama,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        IslemYapanIP = s.IslemYapanIP
                    };

            if (model.SROzelTanimTipID.HasValue) q = q.Where(p => p.SROzelTanimTipID == model.SROzelTanimTipID);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.Aciklama.IsNullOrWhiteSpace() == false) q = q.Where(p => p.Aciklama == model.Aciklama);
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace())
            {
                if (model.Sort.Contains("TTarih"))
                {
                    q = model.Sort.Contains("DESC") == false ? q.OrderBy(o => o.Tarih).ThenBy(t => t.BasTarih).ThenBy(t => t.Ay).ThenBy(t => t.Gun) : q.OrderByDescending(o => o.Tarih).ThenByDescending(t => t.BasTarih).ThenByDescending(t => t.Ay).ThenByDescending(t => t.Gun);
                }
                else
                {
                    q = q.OrderBy(model.Sort);
                }

            }
            else q = q.OrderBy(o => o.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit ? 0 : o.SROzelTanimTipID);
            model.FrOzelTanimlars = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.SROzelTanimTipID = new SelectList(SrTalepleriBus.GetCmbOzelTanimTipleri(true), "Value", "Caption", model.SROzelTanimTipID);
            return View(model);
        }
        public ActionResult Kayit(int? id)
        {

            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;

            var model = new SROzelTanimlar
            {
                Tarih = DateTime.Now
            }; 
            if (id.HasValue)
            {
                model = _entities.SROzelTanimlars.FirstOrDefault(p => p.SROzelTanimID == id); 

            } 
            ViewBag.SROzelTanimTipID = new SelectList(SrTalepleriBus.GetCmbOzelTanimTipleri(true), "Value", "Caption", model.SROzelTanimTipID);
            ViewBag.Ay = new SelectList(SrTalepleriBus.GetCmbAylar(true), "Value", "Caption", model.Ay);
            ViewBag.SRTalepTipID = new SelectList(SrTalepleriBus.GetCmbTalepTipleri(true), "Value", "Caption", model.SRTalepTipID);
 
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(SROzelTanimlar kModel, List<TimeSpan?> basSaat, List<TimeSpan?> bitSaat, DateTime? basTarih2, DateTime? bitTarih2, List<int> haftaGunIDs, string oldId)
        {
            haftaGunIDs = haftaGunIDs ?? new List<int>();
            var mmMessage = new MmMessage();
             
            #region Kontrol 

            if (kModel.SROzelTanimTipID <= 0)
            {
                mmMessage.Messages.Add("Özel tanım tipini seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SROzelTanimTipID" });
            }
            else if (kModel.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit)
            {
                if (kModel.Ay.HasValue == false)
                {
                    mmMessage.Messages.Add("Ay seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Ay" });
                }
                if (kModel.Gun.HasValue == false)
                {
                    mmMessage.Messages.Add("Gün seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Gun" });
                }
            }
            else if (kModel.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen)
            {
                if (kModel.BasTarih.HasValue == false)
                {
                    mmMessage.Messages.Add("Başlangıç Tarihi seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BasTarih" });
                }
                if (kModel.BitTarih.HasValue == false)
                {
                    mmMessage.Messages.Add("Bitiş Tarihi seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitTarih" });
                }
                if (kModel.BasTarih.HasValue && kModel.BitTarih.HasValue)
                {
                    if (kModel.BasTarih > kModel.BitTarih)
                    {
                        mmMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BasTarih" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitTarih" });
                    }
                }
            }
            if (kModel.Aciklama.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Açıklama giriniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Aciklama" });
            }
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                if (kModel.SROzelTanimID <= 0)
                {
                    kModel.IsAktif = true;
                    var insertM = new SROzelTanimlar
                    {
                        SROzelTanimTipID = kModel.SROzelTanimTipID
                    };
                    if (kModel.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit)
                    {
                        insertM.Tarih = null;
                        insertM.SRSalonID = null;
                        insertM.SRTalepTipID = null;
                        insertM.Ay = kModel.Ay;
                        insertM.Gun = kModel.Gun;
                        insertM.BasTarih = null;
                        insertM.BitTarih = null;

                    }
                    else if (kModel.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen)
                    {
                        insertM.Tarih = null;
                        insertM.SRSalonID = null;
                        insertM.SRTalepTipID = null;
                        insertM.Ay = null;
                        insertM.Gun = null;
                        insertM.BasTarih = kModel.BasTarih;
                        insertM.BitTarih = kModel.BitTarih;

                    }
                    insertM.Aciklama = kModel.Aciklama;
                    insertM.IsAktif = kModel.IsAktif;
                    insertM.IslemYapanID = UserIdentity.Current.Id;
                    insertM.IslemYapanIP = UserIdentity.Ip;
                    insertM.IslemTarihi = DateTime.Now;
                    var ydst = _entities.SROzelTanimlars.Add(insertM);
                    _entities.SaveChanges();
                    kModel.SROzelTanimID = ydst.SROzelTanimID;
                }
                else
                {
                    var data = _entities.SROzelTanimlars.First(p => p.SROzelTanimID == kModel.SROzelTanimID);
                    data.SROzelTanimTipID = kModel.SROzelTanimTipID;
                    if (kModel.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit)
                    {
                        data.Tarih = null;
                        data.SRSalonID = null;
                        data.SRTalepTipID = null;
                        data.Ay = kModel.Ay;
                        data.Gun = kModel.Gun;
                        data.BasTarih = null;
                        data.BitTarih = null;

                    }
                    else if (kModel.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen)
                    {
                        data.Tarih = null;
                        data.SRSalonID = null;
                        data.SRTalepTipID = null;
                        data.Ay = null;
                        data.Gun = null;
                        data.BasTarih = kModel.BasTarih;
                        data.BitTarih = kModel.BitTarih;
                    }

                    data.IsAktif = kModel.IsAktif;
                    data.Aciklama = kModel.Aciklama;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;

                }

                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.MmMessage = mmMessage; 
            ViewBag.Ay = new SelectList(SrTalepleriBus.GetCmbAylar(true), "Value", "Caption", kModel.Ay);
            ViewBag.SROzelTanimTipID = new SelectList(SrTalepleriBus.GetCmbOzelTanimTipleri(true), "Value", "Caption", kModel.SROzelTanimTipID);
            ViewBag.SRTalepTipID = new SelectList(SrTalepleriBus.GetCmbTalepTipleri(true), "Value", "Caption", kModel.SRTalepTipID);
            var hGunler = SrTalepleriBus.GetCmbHaftaGunleri(false);
            ViewBag.HaftaGunleri = hGunler;
            ViewBag.hGSecilenler = haftaGunIDs;
            return View(kModel);
        }

        public ActionResult GetDetail(int id)
        {
            var q = (from s in _entities.SROzelTanimlars
                     join a in _entities.Aylars on s.Ay equals a.AyID into def1
                     from def in def1.DefaultIfEmpty()
                     join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                     join ott in _entities.SROzelTanimTipleris on s.SROzelTanimTipID equals ott.SROzelTanimTipID
                     join sln in _entities.SRSalonlars on s.SRSalonID equals sln.SRSalonID into defs
                     from defSln in defs.DefaultIfEmpty()
                     join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID into deft
                     from defTt in deft.DefaultIfEmpty()
                     where s.SROzelTanimID == id
                     select new FrOzelTanimlar
                     {
                         SROzelTanimID = s.SROzelTanimID,
                         SROzelTanimTipID = s.SROzelTanimTipID,
                         SROzelTanimTipAdi = ott.SROzelTanimTipAdi,
                         TalepTipAdi = defTt != null ? defTt.TalepTipAdi : "",
                         SRSalonID = s.SRSalonID,
                         SalonAdi = s.SRSalonID.HasValue ? defSln.SalonAdi : "", 
                         Tarih = s.Tarih,
                         Ay = s.Ay,
                         AyAdi = def != null ? def.AyAdi : "",
                         Gun = s.Gun,
                         BasTarih = s.BasTarih,
                         BitTarih = s.BitTarih,
                         Aciklama = s.Aciklama,
                         IsAktif = s.IsAktif,
                         IslemTarihi = s.IslemTarihi,
                         IslemYapanID = s.IslemYapanID,
                         IslemYapan = k.Ad + " " + k.Soyad,
                         IslemYapanIP = s.IslemYapanIP
                     }).FirstOrDefault();
            ViewBag.HaftaGunleri = _entities.HaftaGunleris.ToList();
            return View(q);
        }

        public ActionResult GetSaatList(int srSalonId, int srTalepTipId, DateTime tarih, int? srOzelTanimId)
        {
            var data = SrTalepleriBus.GetSalonBosSaatler(srSalonId, srTalepTipId, tarih, null, srOzelTanimId);
            var hcb = ViewRenderHelper.RenderPartialView("SROzelTanimlar", "getSaatlerView", data);
            return new { Deger = hcb }.ToJsonResult();
        }
        public ActionResult GetSaatlerView(SRSalonSaatlerModel model)
        {
            return View(model);
        }
        public ActionResult Sil(int id)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var kayit = _entities.SROzelTanimlars.FirstOrDefault(p => p.SROzelTanimID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.Aciklama + "' Açıklamalı özel tanım sistemden silindi!";
                    _entities.SROzelTanimlars.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Aciklama + "' Açıklamalı özel tanım Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Özel Tanım sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}