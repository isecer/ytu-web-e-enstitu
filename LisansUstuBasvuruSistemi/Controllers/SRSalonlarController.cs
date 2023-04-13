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
    [Authorize(Roles = RoleNames.SrSalonlar)]
    public class SrSalonlarController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmSalonlar { });
        }
        [HttpPost]
        public ActionResult Index(FmSalonlar model)
        {

            var q = from s in _entities.SRSalonlars 
                    join e in _entities.Enstitulers on new { s.EnstituKod} equals new { e.EnstituKod}

                    where UserIdentity.Current.EnstituKods.Contains(s.EnstituKod)
                    select new FrSalonlar
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
                       
                        SrSalonTalepTipleris = s.SRSalonTalepTipleris.ToList(),
                        Saatler = (from srs in _entities.SRSaatlers.Where(p => p.SRSalonID == s.SRSalonID)
                                   join gn in _entities.HaftaGunleris on srs.HaftaGunID equals gn.HaftaGunID
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
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.SalonAdi); 
            model.FrSalonlars = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.TT = _entities.SRTalepTipleris.ToList();
            return View(model);
        }
        public ActionResult Kayit(int? id, string ekd, string dlgid)
        {
            
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var model = new KmSalonlar
            {
                EnstituKod = enstituKod,
                SRSalonTalepTipleris = new List<SRSalonTalepTipleri>()
            };
            if (id.HasValue)
            {

                var data = _entities.SRSalonlars.FirstOrDefault(p => p.SRSalonID == id);
                if (data != null)
                {
                    model.SRSalonID = data.SRSalonID;
                    model.SalonAdi = data.SalonAdi;
                    model.IsAktif = data.IsAktif;
                    model.EnstituKod = data.EnstituKod;
                   

                    model.Saatler = (from s in _entities.SRSaatlers.Where(p => p.SRSalonID == model.SRSalonID)
                                     join gn in _entities.HaftaGunleris on s.HaftaGunID equals gn.HaftaGunID
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
            var haftaGunleri = SrTalepleriBus.GetCmbHaftaGunleri(false);
            ViewBag.HaftaGunleri = haftaGunleri;
            ViewBag.SRTalepTipID = SrTalepleriBus.GetCmbSrTalepTipleri();
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.SelectedTTID = model.SRSalonTalepTipleris.Select(s => s.SRTalepTipID).ToList();
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.SrSalonlarKayıt)]
        public ActionResult Kayit(KmSalonlar kModel, List<int> srTalepTipIDs, string oldId, string ekd, string dlgid = "")
        {
            srTalepTipIDs = srTalepTipIDs ?? new List<int>();
            
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            kModel.EnstituKod = enstituKod;

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
                                GunNos = _entities.HaftaGunleris.Where(p =>  qhg.s.Contains(p.HaftaGunID)).Select(s => new CmbIntDto { Value = s.HaftaGunID, Caption = s.HaftaGunAdi }).OrderByDescending(o => o.Value > 0).ThenBy(t => t.Value.Value).ToList()
                            }).OrderBy(t => t.GunNos.Min(m => m.Value)).ThenBy(t => t.BasSaat).ToList();


            #region Kontrol
            if (kModel.SalonAdi.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Salon Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SalonAdi" });
            }
            else  mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SalonAdi" });
            if (srTalepTipIDs.Count == 0)
            { 
                mmMessage.Messages.Add("En az 1 talep tipi seçmeniz gerekmektedir!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipIDs" });
            }
           
            if (qSaatler.Count == 0)
            { 
                mmMessage.Messages.Add("Kayıt işlemini yapabilmeniz saat kriterlerini tanımlayınız!");
            }
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                var srSalonTalepTipleris = srTalepTipIDs.Select(s => new SRSalonTalepTipleri
                { 
                    SRTalepTipID = s
                }).ToList();
                var srSaatlers = new List<SRSaatler>();
                foreach (var item in qSaatler)
                {
                    foreach (var item2 in item.GunNos)
                    {
                        srSaatlers.Add(new SRSaatler
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
                    var ydst = _entities.SRSalonlars.Add(new SRSalonlar
                    {
                        SalonAdi=kModel.SalonAdi,
                        EnstituKod = kModel.EnstituKod,
                        IsAktif = kModel.IsAktif,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now, 
                        SRSalonTalepTipleris= srSalonTalepTipleris,
                        SRSaatlers= srSaatlers
                    });
                    _entities.SaveChanges(); 
                }
                else
                {
                    var data = _entities.SRSalonlars.First(p => p.SRSalonID == kModel.SRSalonID);
                    data.SalonAdi = kModel.SalonAdi;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now; 

                    var saatler = _entities.SRSaatlers.Where(p => p.SRSalonID == data.SRSalonID).ToList();
                    _entities.SRSaatlers.RemoveRange(saatler);

                    var srtt = _entities.SRSalonTalepTipleris.Where(p => p.SRSalonID == data.SRSalonID).ToList();
                    _entities.SRSalonTalepTipleris.RemoveRange(srtt);

                    data.SRSalonTalepTipleris = srSalonTalepTipleris;
                    data.SRSaatlers = srSaatlers;
                }
                
                 
               
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            
            ViewBag.MmMessage = mmMessage;
            kModel.Saatler = qSaatler;
            var haftaGunleri = SrTalepleriBus.GetCmbHaftaGunleri(false);
            ViewBag.HaftaGunleri = haftaGunleri;
            ViewBag.SRTalepTipID = SrTalepleriBus.GetCmbSrTalepTipleri();
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.SelectedTTID = srTalepTipIDs;
            return View(kModel);
        }

        public ActionResult SaatEkleKontrol(SRSaatKontrolModel model)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = true
            };
            model.GHaftaGunleri = model.HaftaGunleri.IsNullOrWhiteSpace() ? new List<int>() : model.HaftaGunleri.Split(',').Select(s => s.ToInt().Value).ToList();

            model.HaftaGunleriList = model.HaftaGunleriList ?? new List<string>();
            model.BasSaatList = model.BasSaatList ?? new List<TimeSpan>();
            model.BitSaatList = model.BitSaatList ?? new List<TimeSpan>();
            var baslangcSaati = model.BasSaatList.Select((s, inx) => new { s, inx }).ToList();
            var talepBitisSaati = model.BitSaatList.Select((s, inx) => new { s, inx }).ToList();
            var gHaftaGunleriList = model.HaftaGunleriList.Select((s, inx) => new { s = s.Split(',').Select(s2 => s2.ToInt().Value).ToList(), inx }).ToList();
            var qSaatler = (from tbs in baslangcSaati
                            join tbt in talepBitisSaati on tbs.inx equals tbt.inx
                            join hgl in gHaftaGunleriList on tbs.inx equals hgl.inx

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
                    var gunler = SrTalepleriBus.GetCmbHaftaGunleri(false);
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Eklemeye çalıştığınız günlere ait saat aralıkları zaten bulunmaktadır!");

                    var mtc = new MailTableContentDto
                    {
                        CaptTdWidth = 350,
                        GrupBasligi = "Aşağıdaki saatlerle çakışma var!"
                    };
                    var mRowModel = new List<MailTableRowDto>();
                    foreach (var item in varolanlar)
                    {
                        var gunlerT = gunler.Where(p => item._HaftaGunleriList.Intersect(model.GHaftaGunleri).Contains(p.Value.Value)).Select(s => s.Caption).ToList();
                        mRowModel.Add(new MailTableRowDto { Baslik = string.Join(",", gunlerT), Aciklama = item._TalepBaslangcSaati + " - " + item._TalepBitisSaati });

                    }
                    mtc.Detaylar = mRowModel;
                    var tavleContent = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtc);
                    mmMessage.Messages.Add(tavleContent);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.BelgeTipleriSil)]
        public ActionResult Sil(int? id)
        {
            var data = _entities.SRSalonlars.FirstOrDefault(p => p.SRSalonID == id); 
            string message = "";
            bool success = true;
            if (data != null)
            {

                try
                {
                    message = "'" + data.SalonAdi + "' İsimli Salon Silindi!";
                    _entities.SRSaatlers.RemoveRange(data.SRSaatlers);
                    _entities.SRSalonTalepTipleris.RemoveRange(data.SRSalonTalepTipleris);
                    _entities.SRSalonlars.Remove(data);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + data.SalonAdi + "' İsimli Salon Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage(); 
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "SRSalonlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
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