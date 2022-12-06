using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MezuniyetYayinTurleri)]
    public class MezuniyetYayinTurleriController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();

        public ActionResult Index(string EKD)
        {
            var sEkod = Management.getSelectedEnstitu(EKD);
            return Index(new fmMezuniyetYayinTurleri { PageSize = Management.PageSize, Expand = false });
        }
        [HttpPost]
        public ActionResult Index(fmMezuniyetYayinTurleri model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from my in db.MezuniyetYayinTurleris
                    join myt in db.MezuniyetYayinTurleris on new { my.MezuniyetYayinTurID } equals new { myt.MezuniyetYayinTurID } into defmytD
                    from mytD in defmytD.DefaultIfEmpty()
                    join mbt in db.MezuniyetYayinBelgeTurleris on new { my.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)mbt.MezuniyetYayinBelgeTurID } into defmbtD
                    from mbtD in defmbtD.DefaultIfEmpty()
                    join mklt in db.MezuniyetYayinLinkTurleris on new { my.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)mklt.MezuniyetYayinLinkTurID } into defmltD
                    from mkltD in defmltD.DefaultIfEmpty()
                    join mmt in db.MezuniyetYayinMetinTurleris on new { my.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)mmt.MezuniyetYayinMetinTurID } into defmmtD
                    from mmtD in defmmtD.DefaultIfEmpty()
                    join mylt in db.MezuniyetYayinLinkTurleris on new { my.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)mylt.MezuniyetYayinLinkTurID } into defmyltD
                    from myltD in defmyltD.DefaultIfEmpty()
                    select new frMezuniyetYayinTurleri
                    {
                        MezuniyetYayinTurID = my.MezuniyetYayinTurID,
                        MezuniyetYayinBelgeTurID = my.MezuniyetYayinBelgeTurID,
                        BelgeTurAdi = mbtD != null ? mbtD.BelgeTurAdi : "",
                        BelgeZorunlu = my.BelgeZorunlu,
                        KaynakMezuniyetYayinLinkTurID = my.KaynakMezuniyetYayinLinkTurID,
                        KaynakLinkTurAdi = mkltD != null ? mkltD.LinkTurAdi : "",
                        KaynakLinkiZorunlu = my.KaynakLinkiZorunlu,
                        MezuniyetYayinMetinTurID = my.MezuniyetYayinMetinTurID,
                        MetinTurAdi = mmtD != null ? mmtD.MetinTurAdi : "",
                        MetinZorunlu = my.MetinZorunlu,
                        YayinMezuniyetYayinLinkTurID = my.YayinMezuniyetYayinLinkTurID,
                        YayinLinkTurAdi = myltD != null ? myltD.LinkTurAdi : "",
                        YayinLinkiZorunlu = my.YayinLinkiZorunlu,
                        YayinIndexTurIstensin = my.YayinIndexTurIstensin,
                        YayinKabulEdilmisMakaleIstensin = my.YayinKabulEdilmisMakaleIstensin,
                        MezuniyetYayinTurAdi = mytD != null ? mytD.MezuniyetYayinTurAdi : "",
                        TarihIstensin = my.TarihIstensin,
                        YayinDeatKurulusIstensin = my.YayinDeatKurulusIstensin,
                        YayinDergiAdiIstensin = my.YayinDergiAdiIstensin,
                        YayinMevcutDurumIstensin = my.YayinMevcutDurumIstensin,
                        YayinProjeEkibiIstensin = my.YayinProjeEkibiIstensin,
                        YayinProjeTurIstensin = my.YayinProjeTurIstensin,
                        YayinYazarlarIstensin = my.YayinYazarlarIstensin,
                        YayinYilCiltSayiIstensin = my.YayinYilCiltSayiIstensin,
                        IsTarihAraligiIstensin = my.IsTarihAraligiIstensin,
                        YayinEtkinlikAdiIstensin = my.YayinEtkinlikAdiIstensin,
                        YayinYerBilgisiIstensin = my.YayinYerBilgisiIstensin,
                        IsAktif = my.IsAktif,
                        IslemTarihi = my.IslemTarihi,
                        IslemYapanID = my.IslemYapanID,
                        IslemYapanIP = my.IslemYapanIP,
                        IslemYapan = my.Kullanicilar.Ad + " " + my.Kullanicilar.Soyad

                    };
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (!model.MezuniyetYayinTurAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.MezuniyetYayinTurAdi.Contains(model.MezuniyetYayinTurAdi));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.MezuniyetYayinTurAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }



        public ActionResult Kayit(int? id, string EKD)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new MezuniyetYayinTurleri();

            if (id.HasValue && id.Value > 0)
            {
                var data = db.MezuniyetYayinTurleris.Where(p => p.MezuniyetYayinTurID == id).FirstOrDefault();
                if (data != null)
                {
                    model = data;

                }
                else model.IsAktif = true;
            }
            else
            {
                model.BelgeZorunlu = true;
                model.KaynakLinkiZorunlu = true;
                model.MetinZorunlu = true;
                model.YayinLinkiZorunlu = true;
                model.IsAktif = true;
            }

            ViewBag.MezuniyetYayinBelgeTurID = new SelectList(Management.cmbMezuniyetYayinBelgeTurleri(true), "Value", "Caption", model.MezuniyetYayinBelgeTurID);
            ViewBag.KaynakMezuniyetYayinLinkTurID = new SelectList(Management.cmbMezuniyetYayinLinkTurleri(true, true), "Value", "Caption", model.KaynakMezuniyetYayinLinkTurID);
            ViewBag.MezuniyetYayinMetinTurID = new SelectList(Management.cmbMezuniyetYayinMetinTurleri(true), "Value", "Caption", model.MezuniyetYayinMetinTurID);
            ViewBag.YayinMezuniyetYayinLinkTurID = new SelectList(Management.cmbMezuniyetYayinLinkTurleri(false, true), "Value", "Caption", model.YayinMezuniyetYayinLinkTurID);
            ViewBag.MezuniyetYayinBelgeTurID = new SelectList(Management.cmbMezuniyetYayinBelgeTurleri(true), "Value", "Caption", model.MezuniyetYayinBelgeTurID); 
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(MezuniyetYayinTurleri kModel, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;

            #region Kontrol

            if (kModel.MezuniyetYayinTurAdi.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Yayın Türü Adı Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinTurAdi" });

            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.MezuniyetYayinBelgeTurID = kModel.MezuniyetYayinBelgeTurID;
                kModel.BelgeZorunlu = kModel.MezuniyetYayinBelgeTurID.HasValue ? kModel.BelgeZorunlu : false;
                kModel.KaynakMezuniyetYayinLinkTurID = kModel.KaynakMezuniyetYayinLinkTurID;
                kModel.KaynakLinkiZorunlu = kModel.KaynakMezuniyetYayinLinkTurID.HasValue ? kModel.KaynakLinkiZorunlu : false;
                kModel.MezuniyetYayinMetinTurID = kModel.MezuniyetYayinMetinTurID;
                kModel.MetinZorunlu = kModel.MezuniyetYayinMetinTurID.HasValue ? kModel.MetinZorunlu : false;
                kModel.YayinMezuniyetYayinLinkTurID = kModel.YayinMezuniyetYayinLinkTurID;
                kModel.YayinLinkiZorunlu = kModel.YayinMezuniyetYayinLinkTurID.HasValue ? kModel.YayinLinkiZorunlu : false;
                kModel.YayinIndexTurIstensin = kModel.YayinIndexTurIstensin;
                kModel.YayinKabulEdilmisMakaleIstensin = kModel.YayinKabulEdilmisMakaleIstensin;
                kModel.TarihIstensin = kModel.TarihIstensin;
                kModel.YayinDergiAdiIstensin = kModel.YayinDergiAdiIstensin;
                kModel.YayinProjeTurIstensin = kModel.YayinProjeTurIstensin;
                kModel.YayinYazarlarIstensin = kModel.YayinYazarlarIstensin;
                kModel.YayinYilCiltSayiIstensin = kModel.YayinYilCiltSayiIstensin;
                kModel.YayinProjeEkibiIstensin = kModel.YayinProjeEkibiIstensin;
                kModel.YayinMevcutDurumIstensin = kModel.YayinMevcutDurumIstensin;
                kModel.YayinDeatKurulusIstensin = kModel.YayinDeatKurulusIstensin;
                kModel.IsTarihAraligiIstensin = kModel.IsTarihAraligiIstensin;
                kModel.YayinEtkinlikAdiIstensin = kModel.YayinEtkinlikAdiIstensin;
                kModel.YayinYerBilgisiIstensin = kModel.YayinYerBilgisiIstensin;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.IslemTarihi = DateTime.Now;

                if (kModel.MezuniyetYayinTurID <= 0)
                {
                    kModel.IsAktif = true;
                    var myt = db.MezuniyetYayinTurleris.Add(kModel);
                    db.SaveChanges();
                }
                else
                {
                    var data = db.MezuniyetYayinTurleris.Where(p => p.MezuniyetYayinTurID == kModel.MezuniyetYayinTurID).First();
                    data.MezuniyetYayinBelgeTurID = kModel.MezuniyetYayinBelgeTurID;
                    data.BelgeZorunlu = kModel.BelgeZorunlu;
                    data.KaynakMezuniyetYayinLinkTurID = kModel.KaynakMezuniyetYayinLinkTurID;
                    data.KaynakLinkiZorunlu = kModel.KaynakLinkiZorunlu;
                    data.MezuniyetYayinMetinTurID = kModel.MezuniyetYayinMetinTurID;
                    data.MetinZorunlu = kModel.MetinZorunlu;
                    data.YayinMezuniyetYayinLinkTurID = kModel.YayinMezuniyetYayinLinkTurID;
                    data.YayinLinkiZorunlu = kModel.YayinLinkiZorunlu;
                    data.YayinIndexTurIstensin = kModel.YayinIndexTurIstensin;
                    data.YayinKabulEdilmisMakaleIstensin = kModel.YayinKabulEdilmisMakaleIstensin;
                    data.TarihIstensin = kModel.TarihIstensin;
                    data.YayinDergiAdiIstensin = kModel.YayinDergiAdiIstensin;
                    data.YayinProjeTurIstensin = kModel.YayinProjeTurIstensin;
                    data.YayinYazarlarIstensin = kModel.YayinYazarlarIstensin;
                    data.YayinYilCiltSayiIstensin = kModel.YayinYilCiltSayiIstensin;
                    data.YayinProjeEkibiIstensin = kModel.YayinProjeEkibiIstensin;
                    data.YayinMevcutDurumIstensin = kModel.YayinMevcutDurumIstensin;
                    data.YayinDeatKurulusIstensin = kModel.YayinDeatKurulusIstensin;
                    data.IsTarihAraligiIstensin = kModel.IsTarihAraligiIstensin;
                    data.YayinEtkinlikAdiIstensin = kModel.YayinEtkinlikAdiIstensin;
                    data.YayinYerBilgisiIstensin = kModel.YayinYerBilgisiIstensin;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                    data.IslemTarihi = kModel.IslemTarihi;

                } 
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

            ViewBag.MmMessage = MmMessage;
            ViewBag.MezuniyetYayinBelgeTurID = new SelectList(Management.cmbMezuniyetYayinBelgeTurleri(true), "Value", "Caption", kModel.MezuniyetYayinBelgeTurID);
            ViewBag.KaynakMezuniyetYayinLinkTurID = new SelectList(Management.cmbMezuniyetYayinLinkTurleri(true, true), "Value", "Caption", kModel.KaynakMezuniyetYayinLinkTurID);
            ViewBag.MezuniyetYayinMetinTurID = new SelectList(Management.cmbMezuniyetYayinMetinTurleri(true), "Value", "Caption", kModel.MezuniyetYayinMetinTurID);
            ViewBag.YayinMezuniyetYayinLinkTurID = new SelectList(Management.cmbMezuniyetYayinLinkTurleri(false, true), "Value", "Caption", kModel.YayinMezuniyetYayinLinkTurID);
            ViewBag.MezuniyetYayinBelgeTurID = new SelectList(Management.cmbMezuniyetYayinBelgeTurleri(true), "Value", "Caption", kModel.MezuniyetYayinBelgeTurID); 
            return View(kModel);
        }

        public ActionResult Sil(int id)
        {
            var kayit = db.MezuniyetYayinTurleris.Where(p => p.MezuniyetYayinTurID == id).FirstOrDefault();
            var ytAdi = db.MezuniyetYayinTurleris.Where(p => p.MezuniyetYayinTurID == id).First();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + ytAdi.MezuniyetYayinTurAdi + "' İsimli Yayın Türü Silindi!";
                    db.MezuniyetYayinTurleris.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + ytAdi.MezuniyetYayinTurAdi + "' İsimli Yayın Türü Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "MezuniyetYayinTurleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Yayın Türü sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }



    }
}