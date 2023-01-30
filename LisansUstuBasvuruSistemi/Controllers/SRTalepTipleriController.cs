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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.SRTalepTipleri)]
    public class SRTalepTipleriController : Controller
    {

        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmSRTalepTipleri { });
        }
        [HttpPost]
        public ActionResult Index(fmSRTalepTipleri model)
        {

            var q = from s in db.SRTalepTipleris
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    select new frSRTalepTipleri
                    {
                        SRTalepTipID = s.SRTalepTipID,
                        IsTezSinavi = s.IsTezSinavi,
                        IstenenJuriSayisiDR = s.IstenenJuriSayisiDR,
                        IstenenJuriSayisiYL = s.IstenenJuriSayisiYL,
                        MaxCevaplanmamisTalep = s.MaxCevaplanmamisTalep,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        IslemYapanIP = s.IslemYapanIP,
                        TalepTipAdi = s.TalepTipAdi,
                        TalepTipAktifAyIds = db.SRTalepTipleriAktifAylars.Where(p => p.SRTalepTipID == s.SRTalepTipID).Select(s2 => s2.AyID).ToList(),
                        KullaniciTipIDs = db.SRTalepTipKullanicilars.Where(p => p.SRTalepTipID == s.SRTalepTipID).Select(s2 => s2.KullaniciTipID).ToList(),

                    };

            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.IsTezSinavi.HasValue) q = q.Where(p => p.IsTezSinavi == model.IsTezSinavi);
            if (!model.TalepTipAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.TalepTipAdi.Contains(model.TalepTipAdi));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.TalepTipAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            var aylar = Management.cmbAylar(false);
            ViewBag.Aylar = aylar;
            ViewBag.KullaniciTipleri = KullanicilarBus.GetCmbKullaniciTipleri(false, false);
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsTezSinavi = new SelectList(Management.cmbEvetHayirData(true), "Value", "Caption", model.IsTezSinavi);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }

        public ActionResult Kayit(int? id)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new SRTalepTipleri();  
            if (id.HasValue)
            {
                var data = db.SRTalepTipleris.Where(p => p.SRTalepTipID == id).FirstOrDefault();
                if (data != null)
                {
                    model = data;  
                }
            } 
            ViewBag.Aylars = Management.cmbAylar(false); 
            ViewBag.KullaniciTipleris = KullanicilarBus.GetCmbKullaniciTipleri(false, false); 
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(SRTalepTipleri kModel, List<int> AyIDs, List<int> KullaniciTipIDs)
        {
            AyIDs = AyIDs ?? new List<int>();
            KullaniciTipIDs = KullaniciTipIDs ?? new List<int>();
            var StAylars = AyIDs.Select(s => new SRTalepTipleriAktifAylar { AyID = s }).ToList();
            var StKullanicis = KullaniciTipIDs.Select(s => new SRTalepTipKullanicilar { KullaniciTipID = s }).ToList();
            var MmMessage = new MmMessage();
            #region Kontrol
            if (kModel.TalepTipAdi.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Talpe Tip Adı Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TalepTipAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TalepTipAdi" });
            if (KullaniciTipIDs.Count == 0)
            {
                MmMessage.Messages.Add("Kullanıcı Tipi Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AyID" });
            }
            if (AyIDs.Count == 0)
            {
                MmMessage.Messages.Add("Ay Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AyID" });
            }


            #endregion
            if (MmMessage.Messages.Count == 0)
            {
              
                if (kModel.IsTezSinavi == false) { kModel.IstenenJuriSayisiDR = null; kModel.IstenenJuriSayisiYL = null; }
                if (kModel.SRTalepTipID <= 0)
                {

                    kModel.IsAktif = true;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.IslemTarihi = DateTime.Now;
                    db.SRTalepTipleris.Add(new SRTalepTipleri
                    {
                        IsTezSinavi = kModel.IsTezSinavi,
                        IstenenJuriSayisiDR = kModel.IstenenJuriSayisiDR,
                        IstenenJuriSayisiYL = kModel.IstenenJuriSayisiYL,
                        MaxCevaplanmamisTalep = kModel.MaxCevaplanmamisTalep,
                        IsAktif = kModel.IsAktif,
                        SRTalepTipleriAktifAylars = StAylars,
                        SRTalepTipKullanicilars = StKullanicis

                    });
                    db.SaveChanges();
                }
                else
                {
                    var data = db.SRTalepTipleris.Where(p => p.SRTalepTipID == kModel.SRTalepTipID).First();
                    data.IsTezSinavi = kModel.IsTezSinavi;
                    data.IstenenJuriSayisiDR = kModel.IstenenJuriSayisiDR;
                    data.IstenenJuriSayisiYL = kModel.IstenenJuriSayisiYL;
                    data.MaxCevaplanmamisTalep = kModel.MaxCevaplanmamisTalep;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now; 
                    db.SRTalepTipKullanicilars.RemoveRange(data.SRTalepTipKullanicilars); 
                    db.SRTalepTipleriAktifAylars.RemoveRange(data.SRTalepTipleriAktifAylars);
                    data.SRTalepTipleriAktifAylars = StAylars;
                    data.SRTalepTipKullanicilars = StKullanicis;
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                kModel.SRTalepTipleriAktifAylars = StAylars;
                kModel.SRTalepTipKullanicilars = StKullanicis;
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
             
            ViewBag.MmMessage = MmMessage; 
            ViewBag.Aylars = Management.cmbAylar(false);
            ViewBag.KullaniciTipleris = KullanicilarBus.GetCmbKullaniciTipleri(false, false);
            return View(kModel);
        }
        public ActionResult Sil(int? id)
        {
            var data = db.SRTalepTipleris.Where(p => p.SRTalepTipID == id).FirstOrDefault(); 
            string message = "";
            bool success = true;
            if (data != null)
            {

                try
                {
                    message = "'" + data.TalepTipAdi + "' İsimli talep tipi Silindi!";
                    db.SRTalepTipleris.Remove(data);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + data.TalepTipAdi + "' İsimli talep tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "SRTalepTipleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Talep tipi sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}