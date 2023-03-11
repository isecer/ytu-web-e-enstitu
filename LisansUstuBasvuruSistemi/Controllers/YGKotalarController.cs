using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.YgKotalar)]
    public class YGKotalarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmKotalarDto { PageSize = 15 });
        }

        [HttpPost]
        public ActionResult Index(FmKotalarDto model, bool export = false)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var enstList = db.Enstitulers.Where(p => p.IsAktif).ToList();


            var q = from k in db.Kotalars.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                    join ot in db.OgrenimTipleris on new { k.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                    join otl in db.OgrenimTipleris on ot.OgrenimTipID equals otl.OgrenimTipID
                    join s in db.Programlars on k.ProgramKod equals s.ProgramKod
                    join e in db.AnabilimDallaris on s.AnabilimDaliID equals e.AnabilimDaliID
                    join at in db.AlesTipleris on s.AlesTipID equals at.AlesTipID
                    join enst in db.Enstitulers on k.EnstituKod equals enst.EnstituKod
                    where EnstKods.Contains(enst.EnstituKod)
                    select new FrKotalarDto
                    {
                        KotaID = k.KotaID,
                        OgrenimTipKod = k.OgrenimTipKod,
                        OgrenimTipAdi = otl.OgrenimTipAdi,
                        OrtakKota = k.OrtakKota,
                        OrtakKotaSayisi = k.OrtakKotaSayisi,
                        AlanIciKota = k.AlanIciKota,
                        AlanDisiKota = k.AlanDisiKota,
                        MinAles = k.MinAles,
                        MinAgno = k.MinAGNO,
                        EnstituKod = enst.EnstituKod,
                        EnstituAd = enst.EnstituAd,
                        AnabilimDaliKod = e.AnabilimDaliKod,
                        AnabilimDaliAdi = e.AnabilimDaliAdi,
                        AlesTipID = s.AlesTipID,
                        AlesTipAdi = at.AlesTipAdi,
                        ProgramKod = s.ProgramKod,
                        ProgramAdi = s.ProgramAdi,
                        Ingilizce = s.Ingilizce,
                        MulakatSurecineGirecek = (k.MulakatSurecineGirecek ?? ot.MulakatSurecineGirecek),
                        IsAlesYerineDosyaNotuIstensin = enst.EnstituKod == EnstituKodlari.FenBilimleri || ot.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans ? false : (k.IsAlesYerineDosyaNotuIstensin == true),
                        IsAktif = s.IsAktif,
                        IslemTarihi = k.IslemTarihi,
                        IslemYapanID = k.IslemYapanID,
                        IslemYapanIP = k.IslemYapanIP,
                        IslemYapan = k.Kullanicilar.Ad + " " + k.Kullanicilar.Soyad,


                        KullaniciTipAdi = s.KullaniciTipleri.KullaniciTipAdi,

                    };
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod.Value);
            if (model.MulakatSurecineGirecek.HasValue) q = q.Where(p => p.MulakatSurecineGirecek == model.MulakatSurecineGirecek.Value);
            if (model.IsAlesYerineDosyaNotuIstensin.HasValue) q = q.Where(p => p.IsAlesYerineDosyaNotuIstensin == model.IsAlesYerineDosyaNotuIstensin.Value);
            if (!model.ProgramAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.ProgramAdi.Contains(model.ProgramAdi) || p.AnabilimDaliAdi.Contains(model.ProgramAdi));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.AnabilimDaliAdi).ThenBy(o => o.ProgramAdi);

            if (export && model.RowCount > 0)
            {
                GridView gv = new GridView();
                var qExp = q.AsQueryable();

                gv.DataSource = qExp.Select(s => new
                {
                    s.EnstituAd,
                    s.AnabilimDaliKod,
                    s.AnabilimDaliAdi,
                    s.ProgramKod,
                    s.ProgramAdi,
                    Mulakat = s.MulakatSurecineGirecek == true ? "Var" : "Yok",
                    AlesYerineDosyaNotu = s.IsAlesYerineDosyaNotuIstensin == true ? "Var" : "Yok",
                    s.OgrenimTipAdi,
                    s.AlesTipAdi,
                    s.OrtakKotaSayisi,
                    s.AlanIciKota,
                    s.AlanDisiKota
                }).ToList();
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_KotaListesi_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");
            } 
            model.KotalarDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray(); 
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            }; 
            ViewBag.IndexModel = indexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            //ViewBag.EnstituKod2 = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.MulakatSurecineGirecek = new SelectList(ComboData.GetCmbEvetHayirData(true), "Value", "Caption", model.MulakatSurecineGirecek);
            ViewBag.IsAlesYerineDosyaNotuIstensin = new SelectList(ComboData.GetCmbEvetHayirData(true), "Value", "Caption", model.IsAlesYerineDosyaNotuIstensin);
            return View(model);
        }

        public ActionResult Kayit(int? id, string EKD, string dlgid)
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var model = new kmKotalar();

            string AnabilimDaliKod = "";
            if (id.HasValue)
            {
                var data = db.Kotalars.Where(p => p.KotaID == id).FirstOrDefault();
                if (data != null)
                {
                    var ot = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == data.OgrenimTipKod).First();
                    _EnstituKod = data.Programlar.AnabilimDallari.EnstituKod;
                    AnabilimDaliKod = data.Programlar.AnabilimDaliKod;
                    model.EnstituKod = data.EnstituKod;
                    model.KotaID = data.KotaID;
                    model.ProgramKod = data.ProgramKod;
                    model.MulakatSurecineGirecek = data.MulakatSurecineGirecek;
                    model.OgrenimTipKod = data.OgrenimTipKod;
                    model.AlanIciKota = data.AlanIciKota;
                    model.AlanDisiKota = data.AlanDisiKota;
                    model.OrtakKota = data.OrtakKota;
                    model.OrtakKotaSayisi = data.OrtakKotaSayisi;
                    model.MinAles = data.MinAles;
                    model.MinAGNO = data.MinAGNO;
                    model.IsAlesYerineDosyaNotuIstensin = data.IsAlesYerineDosyaNotuIstensin;
                    model.IslemYapanID = UserIdentity.Current.Id;
                    model.IslemYapanIP = UserIdentity.Ip;
                    model.IslemTarihi = DateTime.Now;
                }
            }
            model.MulakatSurecineGirecek = model.MulakatSurecineGirecek ?? true;
            model.IsAlesYerineDosyaNotuIstensin = model.IsAlesYerineDosyaNotuIstensin ?? false;
            var tEnstKod = model.EnstituKod.IsNullOrWhiteSpace() == false ? model.EnstituKod : _EnstituKod;

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", tEnstKod);
            ViewBag.AnabilimDaliKod = new SelectList(Management.cmbGetYetkiliProgramAnabilimDallari(true, tEnstKod), "Value", "Caption", AnabilimDaliKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetKullaniciProgramlari(UserIdentity.Current.Id, tEnstKod, AnabilimDaliKod, true), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(tEnstKod, true), "Value", "Caption", model.OgrenimTipKod);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(kmKotalar kModel, string EnstituKod, string AnabilimDaliKod, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            #region Kontrol
            if (EnstituKod.IsNullOrWhiteSpace())
            {
                EnstituKod = "////";
                string msg = "Enstitü seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (AnabilimDaliKod.IsNullOrWhiteSpace())
            {
                string msg = "Anabilim Dalı seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnabilimDaliKod" });
            if (kModel.ProgramKod.IsNullOrWhiteSpace())
            {
                string msg = "Program seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
            if (kModel.OgrenimTipKod <= 0)
            {
                string msg = "Öğrenim Tipi seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });

            if (kModel.OrtakKota == false)
            {
                if (!kModel.AlanIciKota.HasValue)
                {
                    string msg = "Alaniçi Kota Bilgisini Giriniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AlanIciKota" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AlanIciKota" });
                if (!kModel.AlanDisiKota.HasValue)
                {
                    string msg = "Alandışı Kota Bilgisini Giriniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AlanDisiKota" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AlanDisiKota" });
            }
            else
            {
                if (!kModel.OrtakKotaSayisi.HasValue)
                {
                    string msg = "Ortak Kota Sayısı Bilgisini Giriniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OrtakKotaSayisi" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OrtakKotaSayisi" });
            }

            if (MmMessage.Messages.Count == 0)
            {

                var kt = db.Kotalars.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru && p.KotaID != kModel.KotaID && p.ProgramKod == kModel.ProgramKod && p.OgrenimTipKod == kModel.OgrenimTipKod).FirstOrDefault();
                if (kt != null)
                {
                    var programAdi = db.Programlars.Where(p => p.ProgramKod == kModel.ProgramKod).First().ProgramAdi;
                    var OgrenimTipAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.OgrenimTipKod == kModel.OgrenimTipKod).First().OgrenimTipAdi;
                    string msg = "Tanımlamak istediğiniz '" + programAdi + "' programının '" + OgrenimTipAdi + "' öğrenim tipine ait zaten kota tanımlanmış durumdadır, tekrar tanımlanamaz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                }
            }


            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                var ot = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == kModel.OgrenimTipKod).First();
                if (kModel.OrtakKota) { kModel.AlanIciKota = 0; kModel.AlanDisiKota = 0; }
                else kModel.OrtakKotaSayisi = null;

                if (ot.OgrenimTipKod != OgrenimTipi.TezliYuksekLisans || kModel.MulakatSurecineGirecek == true)
                {
                    kModel.MulakatSurecineGirecek = null;
                }
                if (kModel.EnstituKod == EnstituKodlari.FenBilimleri || kModel.IsAlesYerineDosyaNotuIstensin == false) kModel.IsAlesYerineDosyaNotuIstensin = null;
                if (kModel.KotaID <= 0)
                {

                    var Kota = db.Kotalars.Add(new Kotalar
                    {
                        ProgramKod = kModel.ProgramKod,
                        EnstituKod = kModel.EnstituKod,
                        OgrenimTipKod = kModel.OgrenimTipKod,
                        AlanIciKota = kModel.AlanIciKota.Value,
                        AlanDisiKota = kModel.AlanDisiKota.Value,
                        OrtakKota = kModel.OrtakKota,
                        OrtakKotaSayisi = kModel.OrtakKotaSayisi,
                        MinAles = 0,
                        MinAGNO = 0,
                        MulakatSurecineGirecek = kModel.MulakatSurecineGirecek,
                        IsAlesYerineDosyaNotuIstensin = kModel.IsAlesYerineDosyaNotuIstensin,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now
                    });
                }
                else
                {
                    var data = db.Kotalars.Where(p => p.KotaID == kModel.KotaID).First();
                    data.EnstituKod = kModel.EnstituKod;
                    data.ProgramKod = kModel.ProgramKod;
                    data.OgrenimTipKod = kModel.OgrenimTipKod;
                    data.AlanIciKota = kModel.AlanIciKota.Value;
                    data.AlanDisiKota = kModel.AlanDisiKota.Value;
                    data.OrtakKota = kModel.OrtakKota;
                    data.OrtakKotaSayisi = kModel.OrtakKotaSayisi;
                    data.IsAlesYerineDosyaNotuIstensin = kModel.IsAlesYerineDosyaNotuIstensin;
                    //data.MinAles = kModel.MinAles.Value;
                    //data.MinAGNO = kModel.MinAGNO.Value;
                    data.MulakatSurecineGirecek = kModel.MulakatSurecineGirecek;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

            kModel.MulakatSurecineGirecek = kModel.MulakatSurecineGirecek ?? true;
            kModel.IsAlesYerineDosyaNotuIstensin = kModel.IsAlesYerineDosyaNotuIstensin ?? false;
            ViewBag.MmMessage = MmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", EnstituKod);
            ViewBag.AnabilimDaliKod = new SelectList(Management.cmbGetYetkiliProgramAnabilimDallari(true, EnstituKod), "Value", "Caption", AnabilimDaliKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetKullaniciProgramlari(UserIdentity.Current.Id, EnstituKod, AnabilimDaliKod, true), "Value", "Caption", kModel.ProgramKod);

            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(kModel.EnstituKod, true), "Value", "Caption", kModel.OgrenimTipKod);
            return View(kModel);
        }

        public ActionResult getWsData(string EnstituKod)
        {
            var userEnst = UserIdentity.Current.EnstituKods.ToList();
            string message = "";
            bool success = true;
            if (userEnst.Contains(EnstituKod))
            {
                try
                {

                    var programlar = db.Programlars.Where(p => p.AnabilimDallari.EnstituKod == EnstituKod).Select(s => s.ProgramKod).ToList();
                    var qdata = Management.getWsKotalar(EnstituKod, BasvuruSurecTipi.YatayGecisBasvuru);
                    if (qdata.Count > 0)
                    {
                        var kotalar = db.Kotalars.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru && p.EnstituKod == EnstituKod).ToList();

                        var olmayanlar = qdata.Where(p => programlar.Contains(p.ProgramKod) == false).Select(s => s.ProgramKod).Distinct().ToList();
                        var newK = qdata.Where(p => programlar.Contains(p.ProgramKod)).ToList();
                        db.Kotalars.RemoveRange(kotalar);
                        db.Kotalars.AddRange(newK);
                        db.SaveChanges();
                        message = kotalar.Count + " Havuz kotası silindi, " + newK.Count + " Kota havuza aktarıldı!";
                        if (olmayanlar.Count > 0)
                        {
                            message += "<br/>Tanımsız program kodları mevcut! <br/>Program Kod: " + string.Join("<br/>Program Kod: ", olmayanlar);
                        }
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message.Replace("<br/>", "\r\n"), "Kotalar/getWsData", LogType.Bilgi);
                    }
                    else
                    {
                        success = false;
                        message = "Çekilecek bir kota bilgisi bulunamadı!";
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Kotalar/getWsData", LogType.Bilgi);
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "Kota bilgisi güncellenirken bir hata oluştu! Hata: " + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Kotalar/getWsData", LogType.Kritik);
                }
            }
            else
            {
                message = "Seçili enstitü için kota bilgisi güncellemeye yetkili değilsiniz!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.Kotalars.Where(p => p.KotaID == id).FirstOrDefault();
            var PAdi = db.Programlars.Where(p => p.ProgramKod == kayit.ProgramKod).First();
            var otL = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == kayit.OgrenimTipKod && p.EnstituKod == kayit.EnstituKod).First();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + PAdi.ProgramAdi + "' İsimli Programının " + otL.OgrenimTipAdi + " öğrenim tipine ait kota bilgisi silindi!";
                    db.Kotalars.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + PAdi.ProgramAdi + "' İsimli Programının " + otL.OgrenimTipAdi + " öğrenim tipine ait kota bilgisi silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Kotalar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Program kotası sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }


    }
}
