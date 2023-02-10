using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Duyurular)]
    public class DuyurularController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmDuyurularDto() { PageSize = 15 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmDuyurularDto model, string ekd)
        { 
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.Duyurulars
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    join ens in _entities.Enstitulers on new { s.EnstituKod } equals new { ens.EnstituKod }
                    // where ens.Enstituler.EnstituKisaAd.Contains(EKD)
                    where enstKods.Contains(s.EnstituKod)
                    select new
                    {

                        s.EnstituKod,
                        ens.EnstituAd,
                        s.DuyuruID,
                        s.Tarih,
                        s.Baslik,
                        s.Aciklama,
                        s.AciklamaHtml,
                        DuyuruYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP,
                        EkSayisi = s.DuyuruEkleris.Count,
                        Ekler = s.DuyuruEkleris,
                        s.IsAktif,
                        s.AnaSayfadaGozuksun,
                        s.AnaSayfaPopupAc,
                        s.MezuniyetBasvuruPopupAc,
                        s.BasvuruPopupAc,
                        s.TIBasvuruPopupAc,
                        s.TDOBasvuruPopupAc,
                        s.TalepYaparkenPopupAc,
                        s.YayinSonTarih,
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.Baslik.IsNullOrWhiteSpace()) q = q.Where(p => p.Baslik.Contains(model.Baslik));
            if (!model.Aciklama.IsNullOrWhiteSpace()) q = q.Where(p => p.Aciklama.Contains(model.Aciklama));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.Tarih.HasValue)
            {
                var trih = model.Tarih.Value.TodateToShortDate();
                q = q.Where(p => p.Tarih == trih);

            }
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.Tarih);
            model.DuyurularDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrDuyurularDto
            {
                EnstituAdi = s.EnstituAd,
                EnstituKod = s.EnstituKod,
                DuyuruID = s.DuyuruID,
                Baslik = s.Baslik,
                Aciklama = s.Aciklama,
                AciklamaHtml = s.AciklamaHtml,
                Tarih = s.Tarih,
                DuyuruYapan = s.DuyuruYapan,
                IslemYapanIP = s.IslemYapanIP,
                EkSayisi = s.EkSayisi,
                DuyuruEkleris = s.Ekler,
                IsAktif = s.IsAktif,
                AnaSayfadaGozuksun = s.AnaSayfadaGozuksun,
                AnaSayfaPopupAc = s.AnaSayfaPopupAc,
                BasvuruPopupAc = s.BasvuruPopupAc,
                TIBasvuruPopupAc = s.TIBasvuruPopupAc,
                TDOBasvuruPopupAc = s.TDOBasvuruPopupAc,
                MezuniyetBasvuruPopupAc = s.MezuniyetBasvuruPopupAc,
                TalepYaparkenPopupAc = s.TalepYaparkenPopupAc,
                YayinSonTarih = s.YayinSonTarih
            }).ToList();
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string ekd)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new Duyurular();
            if (id > 0)
            {
                var data = _entities.Duyurulars.FirstOrDefault(p => p.DuyuruID == id);
                if (data != null) model = data;
            }
            string sEnstituKod = "";
            if (enstKods.Count == 1)
            {
                sEnstituKod = enstKods.First();
            }
            else sEnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod ?? sEnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(Duyurular kModel, List<string> dosyaEkiAdi, List<HttpPostedFileBase> dosyaEki, List<int?> duyuruDosyaEkId)
        {
            var mmMessage = new MmMessage();
            duyuruDosyaEkId = duyuruDosyaEkId ?? new List<int?>();
            dosyaEkiAdi = dosyaEkiAdi ?? new List<string>();
            dosyaEki = dosyaEki ?? new List<HttpPostedFileBase>();
            var qDosyaEkAdi = dosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = dosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDuyuruDosyaEkId = duyuruDosyaEkId.Select((s, inx) => new { s, inx }).ToList();
            var qDosyalar = (from ekGirilenAd in qDosyaEkAdi
                             join eklenenEk in qDosyaEki on ekGirilenAd.inx equals eklenenEk.inx
                             select new { ekGirilenAd.inx, DosyaEkAdi = ekGirilenAd.s, Dosya = eklenenEk.s }).ToList();

            var qVarolanlar = (from s in qDosyaEkAdi
                               join sid in qDuyuruDosyaEkId on s.inx equals sid.inx
                               select new { s.inx, DosyaEkAdi = s.s, DuyuruDosyaEkID = sid.s });
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Duyurunun Yayınlanacağı Enstitüyü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });

            }

            if (kModel.Tarih == DateTime.MinValue)
            { 
                mmMessage.Messages.Add("Geçerli Bir Tarih Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
            }
            else
            {
                if (kModel.YayinSonTarih.HasValue)
                {
                    if (kModel.YayinSonTarih.Value <= kModel.Tarih)
                    { 
                        mmMessage.Messages.Add("Duyurunun yayınlanacağı son tarih Duyuru tarihinden tarihten küçük ya da eşit olamaz! ");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YayinSonTarih" });
                    }
                }
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Tarih" });
            }
            if (kModel.Baslik.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Başlık Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Baslik" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Baslik" });

            if (kModel.Aciklama.IsNullOrWhiteSpace() && kModel.AciklamaHtml.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Aciklama Giriniz.");
            }

            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = kModel.Aciklama ?? "";
                if (kModel.DuyuruID <= 0)
                {
                    kModel.IsAktif = true;
                    var eklenen = _entities.Duyurulars.Add(kModel);

                    foreach (var item in qDosyalar)
                    {
                        string dosyaYolu = "/DuyuruDosyaları/" + item.DosyaEkAdi.ToFileNameAddGuid(item.Dosya.FileName.GetFileExtension());
                        item.Dosya.SaveAs(Server.MapPath("~" + dosyaYolu));

                        _entities.DuyuruEkleris.Add(new DuyuruEkleri
                        {
                            DuyuruID = eklenen.DuyuruID,
                            DosyaEkAdi = item.DosyaEkAdi,
                            DosyaYolu = dosyaYolu
                        });
                    }
                }
                else
                {
                    var data = _entities.Duyurulars.First(p => p.DuyuruID == kModel.DuyuruID);
                    data.Baslik = kModel.Baslik;
                    data.Aciklama = kModel.Aciklama;
                    data.AciklamaHtml = kModel.AciklamaHtml;
                    data.Tarih = kModel.Tarih;
                    data.YayinSonTarih = kModel.YayinSonTarih;
                    data.AnaSayfadaGozuksun = kModel.AnaSayfadaGozuksun;
                    data.AnaSayfaPopupAc = kModel.AnaSayfaPopupAc;
                    data.BasvuruPopupAc = kModel.BasvuruPopupAc;
                    data.MezuniyetBasvuruPopupAc = kModel.MezuniyetBasvuruPopupAc;
                    data.TalepYaparkenPopupAc = kModel.TalepYaparkenPopupAc;
                    data.TDOBasvuruPopupAc = kModel.TDOBasvuruPopupAc;
                    data.TIBasvuruPopupAc = kModel.TIBasvuruPopupAc;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;

                    var silinenDuyuruEkleri = _entities.DuyuruEkleris.Where(p => duyuruDosyaEkId.Contains(p.DuyuruDosyaEkID) == false && p.DuyuruID == data.DuyuruID).ToList();
                    var varolanDuyuruEkleri = _entities.DuyuruEkleris.Where(p => duyuruDosyaEkId.Contains(p.DuyuruDosyaEkID) && p.DuyuruID == data.DuyuruID).ToList();
                    foreach (var item in varolanDuyuruEkleri)
                    {
                        var qd = qVarolanlar.FirstOrDefault(p => p.DuyuruDosyaEkID == item.DuyuruDosyaEkID);
                        if (qd != null)
                        {
                            item.DosyaEkAdi = qd.DosyaEkAdi;
                        }
                    }
                    _entities.DuyuruEkleris.RemoveRange(silinenDuyuruEkleri);
                    foreach (var item in qDosyalar)
                    {
                        string dosyaYolu = "/DuyuruDosyaları/" + item.Dosya.FileName.ToFileNameAddGuid();
                        item.Dosya.SaveAs(Server.MapPath("~" + dosyaYolu));

                        _entities.DuyuruEkleris.Add(new DuyuruEkleri
                        {
                            DuyuruID = data.DuyuruID,
                            DosyaEkAdi = item.DosyaEkAdi,
                            DosyaYolu = dosyaYolu
                        });
                    }
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.MmMessage = mmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.Duyurulars.FirstOrDefault(p => p.DuyuruID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.Baslik + "' Başlıklı Duyuru Silindi!";
                    var dosyalar = kayit.DuyuruEkleris.ToList();

                    _entities.Duyurulars.Remove(kayit);
                    _entities.SaveChanges();
                    foreach (var item in dosyalar)
                    {
                        System.IO.File.Delete(Server.MapPath("~" + item.DosyaYolu));
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Baslik + "' Başlıklı Duyuru! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Duyurular/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Duyuru sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        } 
        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }
    }
}
