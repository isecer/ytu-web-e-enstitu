using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using BiskaUtil;
using System.Web.UI.WebControls;
using System.IO;
using System.Web.UI;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Mesajlar)]
    public class MesajlarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            var enstituKod = Management.getSelectedEnstitu(EKD);
            return Index(new fmMesajlar() { PageSize = 10, Expand = true, EnstituKod = enstituKod }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmMesajlar model, string EKD, bool export = false)
        {

            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.Mesajlars.Where(p => EnstKods.Contains(p.EnstituKod) && p.UstMesajID.HasValue == false)
                    join ens in db.Enstitulers on new { s.EnstituKod } equals new { ens.EnstituKod }
                    join mk in db.MesajKategorileris on s.MesajKategoriID equals mk.MesajKategoriID
                    join k in db.Kullanicilars on s.KullaniciID equals k.KullaniciID into defK
                    from kul in defK.DefaultIfEmpty()
                    where s.Silindi == false
                    select new
                    {
                        s.EnstituKod,
                        ens.EnstituAd,
                        s.MesajKategoriID,
                        mk.KategoriAdi,
                        s.MesajID,
                        s.UstMesajID,
                        Tarih = s.SonMesajTarihi,
                        s.Konu,
                        Email = kul != null ? kul.EMail : s.Email,
                        s.Aciklama,
                        s.AciklamaHtml,
                        s.AdSoyad,
                        ResimAdi = kul != null ? kul.ResimAdi : null,
                        s.IslemYapanIP,
                        EkSayisi = s.ToplamEkSayisi,
                        s.IsAktif,
                        s.KullaniciID
                    };



            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.MesajKategoriID.HasValue) q = q.Where(p => p.MesajKategoriID == model.MesajKategoriID.Value);
            if (!model.Konu.IsNullOrWhiteSpace()) q = q.Where(p => p.Konu.Contains(model.Konu) || p.Aciklama.Contains(model.Konu));
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.Email.Contains(model.AdSoyad));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.IsDosyaEkDurum.HasValue) q = q.Where(p => model.IsDosyaEkDurum.Value ? p.EkSayisi > 0 : p.EkSayisi == 0);
            if (model.Tarih.HasValue)
            {
                var trih = model.Tarih.Value.TodateToShortDate();
                q = q.Where(p => p.Tarih == trih);

            }
            if (model.MesajYili.HasValue) q = q.Where(p => p.Tarih.Year == model.MesajYili);
            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = model.IsAktif == true ? model.RowCount : q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = model.IsAktif == false ? model.RowCount : (IndexModel.Toplam - IndexModel.Aktif);
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.Tarih);


            if (export && model.RowCount > 0)
            {
                var MesajIDs = q.Select(s => s.MesajID).ToList();
                GridView gv = new GridView();

                if (model.MesajKategoriID.HasValue && model.MesajKategoriID == 37)
                {
                    var data = GetMesajDetails(MesajIDs).Select(s => new
                    {
                        s.GrupNo,
                        GelenGiden = s.GidenGelen,
                        s.KategoriAdi,
                        s.KullaniciTipAdi,
                        s.AdSoyad,
                        s.Konu,
                        Mesaj = s.Aciklama,
                        s.Tarih,
                        s.OgrenciNo,
                        s.KayitDonemAdi,
                        KayitTarihi = s.KayitTarihi.HasValue ? s.KayitTarihi.ToString("dd.MM.yyyy") : "",
                        s.OgrenimTipAdi,
                        s.AnabilimdaliAdi,
                        s.ProgramAdi
                    }).ToList();
                    gv.DataSource = data;
                    gv.DataBind();
                }
                else
                {
                    var data = GetMesajDetails(MesajIDs).Select(s => new
                    {
                        s.GrupNo,
                        GelenGiden = s.GidenGelen,
                        s.KategoriAdi,
                        s.AdSoyad,
                        s.Konu,
                        Mesaj = s.Aciklama,
                        s.Tarih,

                    }).ToList();
                    gv.DataSource = data;
                    gv.DataBind();
                }
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_GelenMesajListesi_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");


            }
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new frMesajlar
            {
                EnstituAdi = s.EnstituAd,
                EnstituKod = s.EnstituKod,
                MesajKategoriID = s.MesajKategoriID,
                KategoriAdi = s.KategoriAdi,
                MesajID = s.MesajID,
                Konu = s.Konu,
                Email = s.Email,
                Aciklama = s.Aciklama,
                AciklamaHtml = s.AciklamaHtml,
                Tarih = s.Tarih,
                AdSoyad = s.AdSoyad,
                ResimAdi = s.ResimAdi,
                EkSayisi = s.EkSayisi,
                KullaniciID = s.KullaniciID ?? 0,
                IslemYapanIP = s.IslemYapanIP,
                IsAktif = s.IsAktif
            }).ToList();
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.MesajKategoriID = new SelectList(Management.cmbGetMesajKategorileri(model.EnstituKod, true), "Value", "Caption", model.MesajKategoriID);
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAcikKapaliData(true), "Value", "Caption", model.IsAktif);
            ViewBag.IsDosyaEkDurum = new SelectList(Management.cmbDosyaEkiDurumData(true), "Value", "Caption", model.IsDosyaEkDurum);
            ViewBag.MesajYili = new SelectList(Management.cmbGetMesajYillari(model.EnstituKod, true), "Value", "Caption", model.MesajYili);

            return View(model);
        }
        public ActionResult GetAcikMsjCount(string EnstituKod)
        {
            var model = Management.GetCevaplanmamisMesajCount(EnstituKod);
            return new { mCount = model.Value.Value, HtmlContent = model.Caption }.toJsonResult();
        }
        public ActionResult DurumKayit(int id, bool IsAktif, bool? MainFilter)
        {
            var kayit = db.Mesajlars.Where(p => p.MesajID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            try
            {
                message = "'" + kayit.Konu + "' Konulu Mesaj durumu " + (!IsAktif ? "Açık" : "Kapalı") + " olarak İşaretlendi";
                kayit.IsAktif = IsAktif;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                success = false;
                message = "'" + kayit.Konu + "'Konulu Mesaj Durumu Güncellenemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                Management.SistemBilgisiKaydet(message, "Mesajlar/DurumKayit<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult Sil(int id)
        {
            var mSilYetki = RoleNames.MesajlarSil.InRoleCurrent();
            string message = "";
            bool success = true;

            if (mSilYetki)
            {
                var kayit = db.Mesajlars.Where(p => p.MesajID == id).FirstOrDefault();
                if (kayit != null)
                {
                    try
                    {
                        message = "'" + kayit.Konu + "' Konulu Mesaj Silindi!";
                        kayit.Silindi = true;
                        var mails = db.GonderilenMaillers.Where(p => p.MesajID == kayit.MesajID).ToList();
                        foreach (var item in mails)
                        {
                            item.Silindi = true;
                        }
                        db.SaveChanges();

                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = "'" + kayit.Konu + "' Başlıklı Konu! <br/> Bilgi:" + ex.ToExceptionMessage();
                        Management.SistemBilgisiKaydet(message, "Mesajlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                    }
                }
                else
                {
                    success = false;
                    message = "Silmek istediğiniz Mesaj sistemde bulunamadı!";
                }
            }
            else
            {
                success = false;
                message = "Mesaj silmeye yetkiniz bulunmuyor!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }


        public ActionResult getMesajDetay(int MesajID)
        {
            var mesaj = db.Mesajlars.Where(p => p.UstMesajID.HasValue == false && p.MesajID == MesajID).Select(s => new frMesajlar
            {
                MesajKategoriID = s.MesajKategoriID,
                MesajID = s.MesajID,
                Konu = s.Konu,
                KullaniciID = s.KullaniciID,
                Email = s.Kullanicilar != null ? s.Kullanicilar.EMail : s.Email,
                Aciklama = s.Aciklama,
                AciklamaHtml = s.AciklamaHtml,
                Tarih = s.Tarih,
                AdSoyad = s.AdSoyad,
                ResimAdi = s.Kullanicilar != null ? s.Kullanicilar.ResimAdi : null,
                IslemYapanIP = s.IslemYapanIP,
                MesajEkleris = s.MesajEkleris.ToList()
            }).First();

            var groupMesajs = db.Mesajlars.Where(p => p.UstMesajID == mesaj.MesajID).ToList().Select(s => new SubMessages
            {
                MesajID = s.MesajID,
                KullaniciID = s.KullaniciID ?? 0,
                EMail = s.KullaniciID.HasValue ? s.Kullanicilar.EMail : s.Email,
                AdSoyad = s.AdSoyad,
                Tarih = s.Tarih,
                Icerik = s.AciklamaHtml,
                ResimYolu = s.KullaniciID.HasValue ? s.Kullanicilar.ResimAdi : "",
                IslemYapanIP = s.IslemYapanIP,
                Gonderilenler = new List<GonderilenMailKullanicilar>(),
                Ekler = s.MesajEkleris.ToList()


            }).ToList();
            groupMesajs.Add(new SubMessages
            {
                MesajID = mesaj.MesajID,
                KullaniciID = mesaj.KullaniciID ?? 0,
                EMail = mesaj.Email,
                AdSoyad = mesaj.AdSoyad,
                Tarih = mesaj.Tarih,
                Icerik = mesaj.Aciklama,
                ResimYolu = mesaj.ResimAdi,
                IslemYapanIP = mesaj.IslemYapanIP,
                Gonderilenler = new List<GonderilenMailKullanicilar>(),
                Ekler = mesaj.MesajEkleris.Select(s => new MesajEkleri { EkAdi = s.EkAdi, EkDosyaYolu = s.EkDosyaYolu }).ToList(),
            });
            var gMesajs = db.GonderilenMaillers.Where(p => p.MesajID == mesaj.MesajID).ToList();
            foreach (var item in gMesajs)
            {
                var kul = item.Kullanicilar;
                groupMesajs.Add(new SubMessages
                {
                    MesajID = item.MesajID.Value,
                    KullaniciID = kul.KullaniciID,
                    EMail = kul.EMail,
                    AdSoyad = kul.Ad + " " + kul.Soyad,
                    Tarih = item.Tarih,
                    Ekler = item.GonderilenMailEkleris.Select(s2 => new MesajEkleri { MesajEkiID = 1, EkAdi = s2.EkAdi, EkDosyaYolu = s2.EkDosyaYolu }).ToList(),
                    Icerik = item.AciklamaHtml,
                    ResimYolu = kul.ResimAdi,
                    Gonderilenler = item.GonderilenMailKullanicilars.ToList(),
                    IslemYapanIP = item.IslemYapanIP,

                });
            }
            mesaj.SubMesajList = groupMesajs.OrderByDescending(o => o.Tarih).ToList();
            return View(mesaj);
        }
        public List<frMesajlar> GetMesajDetails(List<int> MesajID)
        {
            var MesajList = new List<frMesajlar>();
            var mesajLar = (from s in db.Mesajlars.Where(p => p.UstMesajID.HasValue == false && MesajID.Contains(p.MesajID))
                            join mk in db.MesajKategorileris on new { s.MesajKategoriID } equals new { mk.MesajKategoriID }
                            join k in db.Kullanicilars on s.KullaniciID equals k.KullaniciID into defk
                            from K in defk.DefaultIfEmpty()
                            join kt in db.KullaniciTipleris on K.KullaniciTipID equals kt.KullaniciTipID into defkt
                            from Kt in defkt.DefaultIfEmpty()
                            join kd in db.Donemlers on K.KayitDonemID equals kd.DonemID into defkd
                            from Kd in defkd.DefaultIfEmpty()
                            join ot in db.OgrenimTipleris on K.OgrenimTipKod equals ot.OgrenimTipKod into defot
                            from Ot in defot.DefaultIfEmpty()
                            join pr in db.Programlars on K.ProgramKod equals pr.ProgramKod into defpr
                            from Pr in defpr.DefaultIfEmpty()
                            join ab in db.AnabilimDallaris on Pr.AnabilimDaliKod equals ab.AnabilimDaliKod into defab
                            from Ab in defab.DefaultIfEmpty()
                            select new frMesajlar
                            {
                                GidenGelen = "Gelen Mesaj",
                                MesajKategoriID = s.MesajKategoriID,
                                KategoriAdi = mk.KategoriAdi,
                                MesajID = s.MesajID,
                                Konu = s.Konu,
                                Aciklama = s.Aciklama,
                                Tarih = s.Tarih,
                                KullaniciTipAdi = Kt != null ? Kt.KullaniciTipAdi : "",
                                AdSoyad = s.AdSoyad,
                                OgrenciNo = K.OgrenciNo,
                                KayitDonemAdi = K.KayitYilBaslangic.HasValue ? (K.KayitYilBaslangic + "/" + (K.KayitYilBaslangic + 1) + " " + Kd.DonemAdi) : "",
                                KayitTarihi = K.KayitTarihi,
                                OgrenimTipAdi = Ot != null ? Ot.OgrenimTipAdi : "",
                                AnabilimdaliAdi = Ab != null ? Ab.AnabilimDaliAdi : "",
                                ProgramAdi = Pr != null ? Pr.ProgramAdi : "",

                            }).ToList();
            var altMesaj = (from s in db.Mesajlars.Where(p => MesajID.Contains(p.UstMesajID ?? 0))
                            join k in db.Kullanicilars on s.KullaniciID equals k.KullaniciID into defk
                            from K in defk.DefaultIfEmpty()
                            join kt in db.KullaniciTipleris on K.KullaniciTipID equals kt.KullaniciTipID into defkt
                            from Kt in defkt.DefaultIfEmpty()
                            join kd in db.Donemlers on K.KayitDonemID equals kd.DonemID into defkd
                            from Kd in defkd.DefaultIfEmpty()
                            join ot in db.OgrenimTipleris on K.OgrenimTipKod equals ot.OgrenimTipKod into defot
                            from Ot in defot.DefaultIfEmpty()
                            join pr in db.Programlars on K.ProgramKod equals pr.ProgramKod into defpr
                            from Pr in defpr.DefaultIfEmpty()
                            join ab in db.AnabilimDallaris on Pr.AnabilimDaliKod equals ab.AnabilimDaliKod into defab
                            from Ab in defab.DefaultIfEmpty()
                            select new frMesajlar
                            {
                                GidenGelen = "Gelen Mesaj",
                                MesajID = s.MesajID,
                                Konu = s.Konu,
                                Aciklama = s.Aciklama,
                                Tarih = s.Tarih,
                                KullaniciTipAdi = Kt != null ? Kt.KullaniciTipAdi : "",
                                AdSoyad = s.AdSoyad,
                                OgrenciNo = K.OgrenciNo,
                                KayitDonemAdi = K.KayitYilBaslangic.HasValue ? (K.KayitYilBaslangic + "/" + (K.KayitYilBaslangic + 1) + " " + Kd.DonemAdi) : "",
                                KayitTarihi = K.KayitTarihi,
                                OgrenimTipAdi = Ot != null ? Ot.OgrenimTipAdi : "",
                                AnabilimdaliAdi = Ab != null ? Ab.AnabilimDaliAdi : "",
                                ProgramAdi = Pr != null ? Pr.ProgramAdi : "",

                            }).ToList();

            altMesaj.AddRange(db.GonderilenMaillers.Where(p => MesajID.Contains(p.MesajID ?? 0)).Select(s => new frMesajlar
            {
                GidenGelen = "Giden Mail",
                MesajID = s.MesajID.Value,
                Tarih = s.Tarih,
                AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                Aciklama = s.Aciklama,

            }).ToList());
            int inx = 1;
            foreach (var item in mesajLar)
            {
                item.GrupNo = inx;
                MesajList.Add(item);
                var Secilenler = altMesaj.Where(p => p.MesajID == item.MesajID || p.UstMesajID == item.MesajID).ToList();
                foreach (var item2 in Secilenler.OrderByDescending(o => o.Tarih))
                {
                    MesajList.Add(new frMesajlar
                    {
                        GidenGelen = item2.GidenGelen,
                        GrupNo = item.GrupNo,
                        KategoriAdi = item.KategoriAdi,
                        Konu = item.Konu,
                        Email = item2.Email,
                        Aciklama = item2.Aciklama,
                        Tarih = item2.Tarih,
                        KullaniciTipAdi = item2.KullaniciTipAdi,
                        AdSoyad = item2.AdSoyad,
                        OgrenciNo = item2.OgrenciNo,
                        KayitDonemAdi = item2.KayitDonemAdi,
                        KayitTarihi = item2.KayitTarihi,
                        OgrenimTipAdi = item2.OgrenimTipAdi,
                        AnabilimdaliAdi = item2.AnabilimdaliAdi,
                        ProgramAdi = item2.ProgramAdi

                    });
                }
                inx++;

            }
            return MesajList;

        }

    }
}