using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Kotalar)]
    public class KotalarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmKotalar { PageSize = 15 });
        }

        [HttpPost]
        public ActionResult Index(fmKotalar model, bool export = false)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var enstList = db.Enstitulers.Where(p => p.IsAktif).ToList();


            var q = from k in db.Kotalars.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru)
                    join ot in db.OgrenimTipleris on new { k.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                    join s in db.Programlars on k.ProgramKod equals s.ProgramKod
                    join e in db.AnabilimDallaris on new { s.AnabilimDaliID } equals new { e.AnabilimDaliID }
                    join at in db.AlesTipleris on new { s.AlesTipID } equals new { at.AlesTipID }
                    join atL in db.AlesTipleris on new { at.AlesTipID } equals new { atL.AlesTipID }
                    join enst in db.Enstitulers on new { k.EnstituKod } equals new { enst.EnstituKod }
                    where EnstKods.Contains(enst.EnstituKod)
                    select new frKotalar
                    {
                        KotaID = k.KotaID,
                        OgrenimTipKod = k.OgrenimTipKod,
                        OgrenimTipAdi = ot.OgrenimTipAdi,
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
                        AlesTipAdi = atL.AlesTipAdi,
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


            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();





            var IndexModel = new MIndexBilgi();


            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            //ViewBag.EnstituKod2 = new SelectList(Management.cmbGetYetkiliEnstituler( true), "Value", "Caption", model.EnstituKod);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.MulakatSurecineGirecek = new SelectList(Management.cmbEvetHayirData(true), "Value", "Caption", model.MulakatSurecineGirecek);
            ViewBag.IsAlesYerineDosyaNotuIstensin = new SelectList(Management.cmbEvetHayirData(true), "Value", "Caption", model.IsAlesYerineDosyaNotuIstensin);
            return View(model);
        }

        public ActionResult Kayit(int? id, string EKD)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
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

            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", tEnstKod);
            ViewBag.AnabilimDaliKod = new SelectList(Management.cmbGetYetkiliProgramAnabilimDallari(true, tEnstKod), "Value", "Caption", AnabilimDaliKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetKullaniciProgramlari(UserIdentity.Current.Id, tEnstKod, AnabilimDaliKod, true), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(tEnstKod, true), "Value", "Caption", model.OgrenimTipKod);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(kmKotalar kModel, string EnstituKod, string AnabilimDaliKod)
        {
            var MmMessage = new MmMessage();
            #region Kontrol
            if (EnstituKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Enstitü seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (AnabilimDaliKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Anabilim Dalı seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnabilimDaliKod" });
            if (kModel.ProgramKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Program seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
            if (kModel.OgrenimTipKod <= 0)
            {
                MmMessage.Messages.Add("Öğrenim Tipi seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });

            if (kModel.OrtakKota == false)
            {
                if (!kModel.AlanIciKota.HasValue)
                {
                    MmMessage.Messages.Add("Alaniçi Kota Bilgisini Giriniz!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AlanIciKota" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AlanIciKota" });
                if (!kModel.AlanDisiKota.HasValue)
                {
                    MmMessage.Messages.Add("Alandışı Kota Bilgisini Giriniz!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AlanDisiKota" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AlanDisiKota" });
            }
            else
            {
                if (!kModel.OrtakKotaSayisi.HasValue)
                {
                    MmMessage.Messages.Add("Ortak Kota Sayısı Bilgisini Giriniz!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OrtakKotaSayisi" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OrtakKotaSayisi" });
            }

            if (MmMessage.Messages.Count == 0)
            {

                var kt = db.Kotalars.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru && p.KotaID != kModel.KotaID && p.ProgramKod == kModel.ProgramKod && p.OgrenimTipKod == kModel.OgrenimTipKod).FirstOrDefault();
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
                        BasvuruSurecTipID = BasvuruSurecTipi.LisansustuBasvuru,
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
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", EnstituKod);
            ViewBag.AnabilimDaliKod = new SelectList(Management.cmbGetYetkiliProgramAnabilimDallari(true, EnstituKod), "Value", "Caption", AnabilimDaliKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetKullaniciProgramlari(UserIdentity.Current.Id, EnstituKod, AnabilimDaliKod, true), "Value", "Caption", kModel.ProgramKod);

            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(kModel.EnstituKod, true), "Value", "Caption", kModel.OgrenimTipKod);
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
                    var qdata = Management.getWsKotalar(EnstituKod, BasvuruSurecTipi.LisansustuBasvuru);
                    if (qdata.Count > 0)
                    {
                        var kotalar = db.Kotalars.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru && p.EnstituKod == EnstituKod).ToList();

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
                        Management.SistemBilgisiKaydet(message.Replace("<br/>", "\r\n"), "Kotalar/getWsData", BilgiTipi.Bilgi);
                    }
                    else
                    {
                        success = false;
                        message = "Çekilecek bir kota bilgisi bulunamadı!";
                        Management.SistemBilgisiKaydet(message, "Kotalar/getWsData", BilgiTipi.Bilgi);
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "Kota bilgisi güncellenirken bir hata oluştu! Hata: " + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Kotalar/getWsData", BilgiTipi.Kritik);
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
                    Management.SistemBilgisiKaydet(message, "Kotalar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
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
