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
    [Authorize(Roles = RoleNames.SrTalepTipleri)]
    public class SrTalepTipleriController : Controller
    {

        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmSrTalepTipleri { });
        }
        [HttpPost]
        public ActionResult Index(FmSrTalepTipleri model)
        {

            var q = from s in _entities.SRTalepTipleris
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    select new FrSrTalepTipleri
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
                        TalepTipAktifAyIds = _entities.SRTalepTipleriAktifAylars.Where(p => p.SRTalepTipID == s.SRTalepTipID).Select(s2 => s2.AyID).ToList(),
                        KullaniciTipIDs = _entities.SRTalepTipKullanicilars.Where(p => p.SRTalepTipID == s.SRTalepTipID).Select(s2 => s2.KullaniciTipID).ToList(),

                    };

            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.IsTezSinavi.HasValue) q = q.Where(p => p.IsTezSinavi == model.IsTezSinavi);
            if (!model.TalepTipAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.TalepTipAdi.Contains(model.TalepTipAdi));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.TalepTipAdi); 
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            var aylar = SrTalepleriBus.GetCmbAylar(false);
            ViewBag.Aylar = aylar;
            ViewBag.KullaniciTipleri = KullanicilarBus.GetCmbKullaniciTipleri(false, false);
            ViewBag.IndexModel = indexModel;
            ViewBag.IsTezSinavi = new SelectList(ComboData.GetCmbEvetHayirData(true), "Value", "Caption", model.IsTezSinavi);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }

        public ActionResult Kayit(int? id)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new SRTalepTipleri();  
            if (id.HasValue)
            {
                var data = _entities.SRTalepTipleris.FirstOrDefault(p => p.SRTalepTipID == id);
                if (data != null)
                {
                    model = data;  
                }
            } 
            ViewBag.Aylars = SrTalepleriBus.GetCmbAylar(false); 
            ViewBag.KullaniciTipleris = KullanicilarBus.GetCmbKullaniciTipleri(false, false); 
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(SRTalepTipleri kModel, List<int> ayIDs, List<int> kullaniciTipIDs)
        {
            ayIDs = ayIDs ?? new List<int>();
            kullaniciTipIDs = kullaniciTipIDs ?? new List<int>();
            var stAylars = ayIDs.Select(s => new SRTalepTipleriAktifAylar { AyID = s }).ToList();
            var stKullanicis = kullaniciTipIDs.Select(s => new SRTalepTipKullanicilar { KullaniciTipID = s }).ToList();
            var mmMessage = new MmMessage();
            #region Kontrol
            if (kModel.TalepTipAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Talpe Tip Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TalepTipAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TalepTipAdi" });
            if (kullaniciTipIDs.Count == 0)
            {
                mmMessage.Messages.Add("Kullanıcı Tipi Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AyID" });
            }
            if (ayIDs.Count == 0)
            {
                mmMessage.Messages.Add("Ay Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AyID" });
            }


            #endregion
            if (mmMessage.Messages.Count == 0)
            {
              
                if (kModel.IsTezSinavi == false) { kModel.IstenenJuriSayisiDR = null; kModel.IstenenJuriSayisiYL = null; }
                if (kModel.SRTalepTipID <= 0)
                {

                    kModel.IsAktif = true;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.IslemTarihi = DateTime.Now;
                    _entities.SRTalepTipleris.Add(new SRTalepTipleri
                    {
                        IsTezSinavi = kModel.IsTezSinavi,
                        IstenenJuriSayisiDR = kModel.IstenenJuriSayisiDR,
                        IstenenJuriSayisiYL = kModel.IstenenJuriSayisiYL,
                        MaxCevaplanmamisTalep = kModel.MaxCevaplanmamisTalep,
                        IsAktif = kModel.IsAktif,
                        SRTalepTipleriAktifAylars = stAylars,
                        SRTalepTipKullanicilars = stKullanicis

                    });
                    _entities.SaveChanges();
                }
                else
                {
                    var data = _entities.SRTalepTipleris.First(p => p.SRTalepTipID == kModel.SRTalepTipID);
                    data.IsTezSinavi = kModel.IsTezSinavi;
                    data.IstenenJuriSayisiDR = kModel.IstenenJuriSayisiDR;
                    data.IstenenJuriSayisiYL = kModel.IstenenJuriSayisiYL;
                    data.MaxCevaplanmamisTalep = kModel.MaxCevaplanmamisTalep;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now; 
                    _entities.SRTalepTipKullanicilars.RemoveRange(data.SRTalepTipKullanicilars); 
                    _entities.SRTalepTipleriAktifAylars.RemoveRange(data.SRTalepTipleriAktifAylars);
                    data.SRTalepTipleriAktifAylars = stAylars;
                    data.SRTalepTipKullanicilars = stKullanicis;
                }

                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                kModel.SRTalepTipleriAktifAylars = stAylars;
                kModel.SRTalepTipKullanicilars = stKullanicis;
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
             
            ViewBag.MmMessage = mmMessage; 
            ViewBag.Aylars = SrTalepleriBus.GetCmbAylar(false);
            ViewBag.KullaniciTipleris = KullanicilarBus.GetCmbKullaniciTipleri(false, false);
            return View(kModel);
        }
        public ActionResult Sil(int? id)
        {
            var data = _entities.SRTalepTipleris.FirstOrDefault(p => p.SRTalepTipID == id); 
            string message = "";
            bool success = true;
            if (data != null)
            {

                try
                {
                    message = "'" + data.TalepTipAdi + "' İsimli talep tipi Silindi!";
                    _entities.SRTalepTipleris.Remove(data);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + data.TalepTipAdi + "' İsimli talep tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "SRTalepTipleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Talep tipi sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}