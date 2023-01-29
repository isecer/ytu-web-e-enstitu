using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.SRSalonlar)]
    public class SRSalonlarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmSalonlar { });
        }
        [HttpPost]
        public ActionResult Index(fmSalonlar model)
        {

            var q = from s in db.SRSalonlars 
                    join e in db.Enstitulers on new { s.EnstituKod} equals new { e.EnstituKod}

                    where UserIdentity.Current.EnstituKods.Contains(s.EnstituKod)
                    select new frSalonlar
                    {
                        EnstituKod = e.EnstituKod,
                        EnstituAdi = e.EnstituAd,
                        SRSalonID = s.SRSalonID,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        IslemYapanIP = s.IslemYapanIP,
                        SalonAdi = s.SalonAdi,
                       
                        SRSalonTalepTipleri = s.SRSalonTalepTipleris.ToList(),
                        Saatler = (from srs in db.SRSaatlers.Where(p => p.SRSalonID == s.SRSalonID)
                                   join gn in db.HaftaGunleris on srs.HaftaGunID equals gn.HaftaGunID
                                   group new { srs.BasSaat, srs.BitSaat, srs.HaftaGunID, HaftaGunAdi = gn.HaftaGunAdi }
                                      by new
                                      {
                                          srs.BasSaat,
                                          srs.BitSaat,

                                      } into g1
                                   select new SRSaatlerMDL
                                   {
                                       BasSaat = g1.Key.BasSaat,
                                       BitSaat = g1.Key.BitSaat,
                                       GunNos = g1.Where(p => p.BasSaat == g1.Key.BasSaat && p.BitSaat == g1.Key.BitSaat).Select(s2 => new CmbIntDto { Value = s2.HaftaGunID, Caption = s2.HaftaGunAdi }).OrderByDescending(o => o.Value > 0).ThenBy(t => t.Value.Value).ToList()
                                   }).OrderBy(t => t.GunNos.Min(m => m.Value)).ThenBy(t => t.BasSaat).ToList()
                    };

            if (model.EnstituKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (!model.SalonAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.SalonAdi.Contains(model.SalonAdi));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace())  q = q.OrderBy(model.Sort); 
            else q = q.OrderBy(o => o.SalonAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.EnstituKod = new SelectList(Management.cmbGetAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.TT = db.SRTalepTipleris.ToList();
            return View(model);
        }
        public ActionResult Kayit(int? id, string EKD, string dlgid)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            var model = new kmSalonlar();
            model.EnstituKod = _EnstituKod;
            model.SRSalonTalepTipleris = new List<SRSalonTalepTipleri>();
            if (id.HasValue)
            {

                var data = db.SRSalonlars.Where(p => p.SRSalonID == id).FirstOrDefault();
                if (data != null)
                {
                    model.SRSalonID = data.SRSalonID;
                    model.SalonAdi = data.SalonAdi;
                    model.IsAktif = data.IsAktif;
                    model.EnstituKod = data.EnstituKod;
                   

                    model.Saatler = (from s in db.SRSaatlers.Where(p => p.SRSalonID == model.SRSalonID)
                                     join gn in db.HaftaGunleris on s.HaftaGunID equals gn.HaftaGunID
                                     group new { s.BasSaat, s.BitSaat, s.HaftaGunID, HaftaGunAdi = gn.HaftaGunAdi }
                                        by new
                                        {
                                            s.BasSaat,
                                            s.BitSaat

                                        } into g1
                                     select new SRSaatlerMDL
                                     {
                                         BasSaat = g1.Key.BasSaat,
                                         BitSaat = g1.Key.BitSaat,
                                         GunNos = g1.Where(p => p.BasSaat == g1.Key.BasSaat && p.BitSaat == g1.Key.BitSaat).Select(s2 => new CmbIntDto { Value = s2.HaftaGunID, Caption = s2.HaftaGunAdi }).OrderByDescending(o => o.Value > 0).ThenBy(t => t.Value.Value).ToList()
                                     }).OrderBy(t => t.GunNos.Min(m => m.Value)).ThenBy(t => t.BasSaat).ToList();
                    model.SRSalonTalepTipleris = data.SRSalonTalepTipleris.ToList();
                }
            }
            var haftaGunleri = Management.cmbGetHaftaGunleri(false);
            ViewBag.HaftaGunleri = haftaGunleri;
            ViewBag.SRTalepTipID = Management.cmbSRTalepTipleri();
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.SelectedTTID = model.SRSalonTalepTipleris.Select(s => s.SRTalepTipID).ToList();
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.SRSalonlarKayıt)]
        public ActionResult Kayit(kmSalonlar kModel, List<int> SRTalepTipIDs, string OldID, string EKD, string dlgid = "")
        {
            SRTalepTipIDs = SRTalepTipIDs ?? new List<int>();
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            kModel.EnstituKod = _EnstituKod;

            kModel.BasSaat = kModel.BasSaat ?? new List<TimeSpan>();
            kModel.BitSaat = kModel.BitSaat ?? new List<TimeSpan>();
            kModel.HaftaGunleri = kModel.HaftaGunleri ?? new List<string>();

            var qBasSaat = kModel.BasSaat.Select((s, inx) => new { s, inx }).ToList();
            var qBitSaat = kModel.BitSaat.Select((s, inx) => new { s, inx }).ToList();
            var qHaftaGunleri = kModel.HaftaGunleri.Select((s, inx) => new { s = s.Split(',').Select(s2 => s2.ToInt().Value).ToList(), inx }).ToList();
            var qSaatler = (from qTbs in qBasSaat
                            join qTbt in qBitSaat on qTbs.inx equals qTbt.inx
                            join qhg in qHaftaGunleri on qTbs.inx equals qhg.inx
                            select new SRSaatlerMDL
                            {
                                BasSaat = qTbs.s,
                                BitSaat = qTbt.s,
                                GunNos = db.HaftaGunleris.Where(p =>  qhg.s.Contains(p.HaftaGunID)).Select(s => new CmbIntDto { Value = s.HaftaGunID, Caption = s.HaftaGunAdi }).OrderByDescending(o => o.Value > 0).ThenBy(t => t.Value.Value).ToList()
                            }).OrderBy(t => t.GunNos.Min(m => m.Value)).ThenBy(t => t.BasSaat).ToList();


            #region Kontrol
            if (kModel.SalonAdi.IsNullOrWhiteSpace())
            { 
                MmMessage.Messages.Add("Salon Adı Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SalonAdi" });
            }
            else  MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SalonAdi" });
            if (SRTalepTipIDs.Count == 0)
            {
                string msg = "En az 1 talep tipi seçmeniz gerekmektedir!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipIDs" });
            }
           
            if (qSaatler.Count == 0)
            {
                string msg = "Kayıt işlemini yapabilmeniz saat kriterlerini tanımlayınız!";
                MmMessage.Messages.Add(msg);
            }
            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                var SRSalonTalepTipleris = SRTalepTipIDs.Select(s => new SRSalonTalepTipleri
                { 
                    SRTalepTipID = s
                }).ToList();
                var SRSaatlers = new List<SRSaatler>();
                foreach (var item in qSaatler)
                {
                    foreach (var item2 in item.GunNos)
                    {
                        SRSaatlers.Add(new SRSaatler
                        { 
                            HaftaGunID = item2.Value.Value,
                            BasSaat = item.BasSaat,
                            BitSaat = item.BitSaat
                        });

                    }
                }
                if (kModel.SRSalonID <= 0)
                {
                    kModel.IsAktif = true;
                    var ydst = db.SRSalonlars.Add(new SRSalonlar
                    {
                        SalonAdi=kModel.SalonAdi,
                        EnstituKod = kModel.EnstituKod,
                        IsAktif = kModel.IsAktif,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now, 
                        SRSalonTalepTipleris= SRSalonTalepTipleris,
                        SRSaatlers= SRSaatlers
                    });
                    db.SaveChanges(); 
                }
                else
                {
                    var data = db.SRSalonlars.Where(p => p.SRSalonID == kModel.SRSalonID).First();
                    data.SalonAdi = kModel.SalonAdi;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now; 

                    var _saatler = db.SRSaatlers.Where(p => p.SRSalonID == data.SRSalonID).ToList();
                    db.SRSaatlers.RemoveRange(_saatler);

                    var srtt = db.SRSalonTalepTipleris.Where(p => p.SRSalonID == data.SRSalonID).ToList();
                    db.SRSalonTalepTipleris.RemoveRange(srtt);

                    data.SRSalonTalepTipleris = SRSalonTalepTipleris;
                    data.SRSaatlers = SRSaatlers;
                }
                
                 
               
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            
            ViewBag.MmMessage = MmMessage;
            kModel.Saatler = qSaatler;
            var haftaGunleri = Management.cmbGetHaftaGunleri(false);
            ViewBag.HaftaGunleri = haftaGunleri;
            ViewBag.SRTalepTipID = Management.cmbSRTalepTipleri();
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.SelectedTTID = SRTalepTipIDs;
            return View(kModel);
        }

        public ActionResult saatEkleKontrol(SRSaatKontrolModel model)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = true;
            model.GHaftaGunleri = model.HaftaGunleri.IsNullOrWhiteSpace() ? new List<int>() : model.HaftaGunleri.Split(',').Select(s => s.ToInt().Value).ToList();

            model.HaftaGunleriList = model.HaftaGunleriList ?? new List<string>();
            model.BasSaatList = model.BasSaatList ?? new List<TimeSpan>();
            model.BitSaatList = model.BitSaatList ?? new List<TimeSpan>();
            var _BaslangcSaati = model.BasSaatList.Select((s, inx) => new { s, inx }).ToList();
            var _TalepBitisSaati = model.BitSaatList.Select((s, inx) => new { s, inx }).ToList();
            var _GHaftaGunleriList = model.HaftaGunleriList.Select((s, inx) => new { s = s.Split(',').Select(s2 => s2.ToInt().Value).ToList(), inx }).ToList();
            var qSaatler = (from tbs in _BaslangcSaati
                            join tbt in _TalepBitisSaati on tbs.inx equals tbt.inx
                            join hgl in _GHaftaGunleriList on tbs.inx equals hgl.inx

                            select new
                            {
                                Inx = tbs.inx,
                                _TalepBaslangcSaati = tbs.s,
                                _TalepBitisSaati = tbt.s,
                                _HaftaGunleriList = hgl.s,
                            }).ToList();

            if (model.BasSaat.HasValue == false)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Başlangıç saati boş bırakılamaz!");

            }
            if (model.BitSaat.HasValue == false)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Bitiş saati boş bırakılamaz!");

            }

            if (mmMessage.IsSuccess)
            {
                if (model.BasSaat >= model.BitSaat)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Başlangıç saati bitiş saatinden büyük ya da eşit olamaz!");

                }

            }
            if (mmMessage.IsSuccess)
            {
                var varolanlar = qSaatler.Where(a => a._HaftaGunleriList.Intersect(model.GHaftaGunleri).Any() &&
                                                    (
                                                      (a._TalepBaslangcSaati == model.BasSaat || a._TalepBitisSaati == model.BitSaat) ||
                                                    (
                                                        (a._TalepBaslangcSaati < model.BasSaat && a._TalepBitisSaati > model.BasSaat) || a._TalepBaslangcSaati < model.BitSaat && a._TalepBitisSaati > model.BitSaat) ||
                                                        (a._TalepBaslangcSaati > model.BasSaat && a._TalepBaslangcSaati < model.BitSaat) || a._TalepBitisSaati > model.BasSaat && a._TalepBitisSaati < model.BitSaat)
                                                    ).ToList();
                if (varolanlar.Count > 0)
                {
                    var gunler = Management.cmbGetHaftaGunleri(false);
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Eklemeye çalıştığınız günlere ait saat aralıkları zaten bulunmaktadır!");

                    var mtc = new mailTableContent();
                    mtc.CaptTdWidth = 350;
                    mtc.GrupBasligi = "Aşağıdaki saatlerle çakışma var!";
                    var mRowModel = new List<mailTableRow>();
                    foreach (var item in varolanlar)
                    {
                        var gunlerT = gunler.Where(p => item._HaftaGunleriList.Intersect(model.GHaftaGunleri).Contains(p.Value.Value)).Select(s => s.Caption).ToList();
                        mRowModel.Add(new mailTableRow { Baslik = string.Join(",", gunlerT), Aciklama = item._TalepBaslangcSaati + " - " + item._TalepBitisSaati });

                    }
                    mtc.Detaylar = mRowModel;
                    var tavleContent = Management.RenderPartialView("Ajax", "getMailTableContent", mtc);
                    mmMessage.Messages.Add(tavleContent);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.BelgeTipleriSil)]
        public ActionResult Sil(int? id)
        {
            var data = db.SRSalonlars.Where(p => p.SRSalonID == id).FirstOrDefault(); 
            string message = "";
            bool success = true;
            if (data != null)
            {

                try
                {
                    message = "'" + data.SalonAdi + "' İsimli Salon Silindi!";
                    db.SRSaatlers.RemoveRange(data.SRSaatlers);
                    db.SRSalonTalepTipleris.RemoveRange(data.SRSalonTalepTipleris);
                    db.SRSalonlars.Remove(data);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + data.SalonAdi + "' İsimli Salon Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage(); 
                    Management.SistemBilgisiKaydet(message, "SRSalonlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Salon sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}