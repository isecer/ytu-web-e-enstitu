using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    public class YeterlikController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _context = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {

            return Index(new FmYeterlikBasvuruDto { PageSize = 40 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmYeterlikBasvuruDto model, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            #region BilgiModel 
            model.AktifYeterlikSurecId = YeterlikBus.GetYeterlikAktifSurecId(enstituKod);

            var kullanici = _context.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            model.AdSoyad = kullanici.Ad + " " + kullanici.Soyad;
            model.EnstituAdi = _context.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
            if (model.AktifYeterlikSurecId.HasValue)
            {
                var surec = _context.YeterlikSurecis.First(p => p.YeterlikSurecID == model.AktifYeterlikSurecId);
                model.DonemAdi = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi;

                model.IsYtuOgrencisi = kullanici.YtuOgrencisi && kullanici.OgrenimDurumID == OgrenimDurum.HalenOğrenci;
                model.IsEnstituYetki = kullanici.EnstituKod == enstituKod;

                model.IsOgrenimSeviyeYetki = surec.YeterlikSurecOgrenimTipleris.Any(a => a.OgrenimTipKod == kullanici.OgrenimTipKod);
                model.OgrenimTipAdis = string.Join(", ", surec.YeterlikSurecOgrenimTipleris.Select(s => s.OgrenimTipleri.OgrenimTipAdi).ToList());
            }

            #endregion


            var q = from yeterlikBasvuru in _context.YeterlikBasvurus.Where(p => p.KullaniciID == UserIdentity.Current.Id)
                    join yeterlikSureci in _context.YeterlikSurecis on yeterlikBasvuru.YeterlikSurecID equals yeterlikSureci.YeterlikSurecID
                    join kullanicilar in _context.Kullanicilars on yeterlikBasvuru.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _context.Programlars on yeterlikBasvuru.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _context.OgrenimTipleris on yeterlikBasvuru.OgrenimTipID equals ogrenimTipleri.OgrenimTipID
                    select new FrYeterlikBasvuruDto
                    {
                        YeterlikBasvuruID = yeterlikBasvuru.YeterlikBasvuruID,
                        UniqueID = yeterlikBasvuru.UniqueID,
                        BasvuruTarihi = yeterlikBasvuru.BasvuruTarihi,
                        ResimAdi = kullanicilar.ResimAdi,
                        KullaniciID = yeterlikBasvuru.KullaniciID,
                        AdSoyad = kullanicilar.Ad + " " + kullanici.Soyad,
                        OgrenciNo = yeterlikBasvuru.OgrenciNo,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        OkuduguDonemNo = yeterlikBasvuru.OkuduguDonemNo,
                        IsOnaylandi = yeterlikBasvuru.IsOnaylandi,
                        OnayAciklama = yeterlikBasvuru.OnayAciklama,
                        IsBasarili = yeterlikBasvuru.IsBasarili,

                    };
            model.RowCount = q.Count();
            //IndexModel.Toplam = model.RowCount;
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            var ps = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = ps.PageIndex;
            model.Data = q.Skip(ps.StartRowIndex).Take(model.PageSize).ToList();
            return View(model);
        }


        public ActionResult BasvuruYap(Guid? id, string ekd = "")
        {
            var model = new KmYeterlikBasvuruDto();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
            var errorMessage = YeterlikBus.YeterlikBasvuruKontrol(enstituKod, id);
            if (!errorMessage.Any())
            {
                if (id.HasValue)
                {
                    var basvuru = _context.YeterlikBasvurus.First(p => p.UniqueID == id && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
                    var ogrenimTip = _context.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == basvuru.Kullanicilar.OgrenimTipKod);
                    var ogrenciBilgi = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(basvuru.KullaniciID);

                    model.UniqueID = basvuru.UniqueID;
                    model.YeterlikSurecID = basvuru.YeterlikSurecID;
                    model.YeterlikBasvuruID = basvuru.YeterlikBasvuruID;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = basvuru.KullaniciID;
                    model.AdSoyad = basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad;
                    model.OgrenciNo = basvuru.OgrenciNo;
                    model.OgrenimTipID = basvuru.OgrenimTipID;
                    model.KayitYil = basvuru.Kullanicilar.KayitYilBaslangic.Value;
                    model.KayitDonemID = basvuru.Kullanicilar.KayitDonemID.Value;
                    model.OkuduguDonemNo = ogrenciBilgi.OkuduguDonemNo;
                    model.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                    model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                }
                else
                {
                    var ogrenciBilgi = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(UserIdentity.Current.Id);
                    var kul = _context.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                    var ogrenimTip = _context.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = UserIdentity.Current.Id;
                    model.AdSoyad = kul.Ad + " " + kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.OgrenimTipID = ogrenimTip.OgrenimTipID;
                    model.KayitYil = kul.KayitYilBaslangic.Value;
                    model.KayitDonemID = kul.KayitDonemID.Value;
                    model.OkuduguDonemNo = ogrenciBilgi.OkuduguDonemNo;
                    model.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                    model.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = kul.Programlar.ProgramAdi;
                } 
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, errorMessage.ToArray());
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult BasvuruYap(KmYeterlikBasvuruDto kModel, string ekd = "")
        {
            var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var errprMessages = YeterlikBus.YeterlikBasvuruKontrol(enstituKod, kModel.UniqueID);
            kModel.KullaniciID = kayitYetki ? kModel.KullaniciID : UserIdentity.Current.Id;
            if (!errprMessages.Any())
            {
                var kullanici = _context.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                var ogrenciBilgi = Management.StudentControl(kullanici.TcKimlikNo);
                var ogrenimTip = _context.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);


                if (kModel.UniqueID.HasValue)
                {
                    var data = _context.YeterlikBasvurus.FirstOrDefault(p => p.UniqueID == kModel.UniqueID && p.KullaniciID == kModel.KullaniciID);
                    if (data == null) return RedirectToAction("Index");

                    data.OkuduguDonemNo = ogrenciBilgi.OkuduguDonemNo;
                    data.OgrenimTipID = ogrenimTip.OgrenimTipID;
                    data.OgrenciNo = kullanici.OgrenciNo;
                    data.ProgramKod = kullanici.ProgramKod;
                    data.KayitYil = kullanici.KayitYilBaslangic.Value;
                    data.KayitDonemID = kullanici.KayitDonemID.Value;
                    data.KayitTarihi = kullanici.KayitTarihi.Value;
                    data.YsBasToplamKrediKriteri = ogrenciBilgi.AktifDonemDers.ToplamKredi;
                    data.YsBasSeminerNotKriteri = ogrenciBilgi.AktifDonemDers.SeminerDersNotu;
                    data.YsBasEtikNotKriteri = ogrenciBilgi.AktifDonemDers.EtikDersNotu;
                    data.TezDanismanID = kullanici.DanismanID.Value;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    _context.SaveChanges(); 
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Update, data.ToJson());

                }
                else
                {
                    var yeterlikSurecId = YeterlikBus.GetYeterlikAktifSurecId(enstituKod);
                    _context.YeterlikBasvurus.Add(new YeterlikBasvuru
                    {
                        UniqueID = Guid.NewGuid(),
                        YeterlikSurecID = yeterlikSurecId.Value,
                        BasvuruTarihi = DateTime.Now,
                        KullaniciID = UserIdentity.Current.Id,
                        OkuduguDonemNo = ogrenciBilgi.OkuduguDonemNo,
                        OgrenimTipID = ogrenimTip.OgrenimTipID,
                        OgrenciNo = kullanici.OgrenciNo,
                        ProgramKod = kullanici.ProgramKod,
                        KayitYil = kullanici.KayitYilBaslangic.Value,
                        KayitDonemID = kullanici.KayitDonemID.Value,
                        KayitTarihi = kullanici.KayitTarihi.Value,
                        YsBasToplamKrediKriteri = ogrenciBilgi.AktifDonemDers.ToplamKredi,
                        YsBasSeminerNotKriteri = ogrenciBilgi.AktifDonemDers.SeminerDersNotu,
                        YsBasEtikNotKriteri = ogrenciBilgi.AktifDonemDers.EtikDersNotu,
                        TezDanismanID = kullanici.DanismanID.Value,
                        IslemTarihi = DateTime.Now,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemYapanID = UserIdentity.Current.Id

                    });
                    _context.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, errprMessages.ToArray());
            return RedirectToAction("Index");
        }

        public ActionResult GetDetail(Guid id)
        {
            var basvuru = _context.YeterlikBasvurus.Select(s => new DmYeterlikDetayDto
            {
                UniqueID = s.UniqueID,
                YeterlikSurecID = s.YeterlikSurecID,
                ResimAdi = s.Kullanicilar.ResimAdi,
                AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                OgrenciNo = s.OgrenciNo,
                ProgramAdi = s.Programlar.ProgramAdi,
                AnabilimdaliAdi = s.Programlar.AnabilimDallari.AnabilimDaliAdi,
                OgrenimTipAdi = s.OgrenimTipleri.OgrenimTipAdi,
                OkuduguDonemNo = s.OkuduguDonemNo,
                TezDanismanID = s.TezDanismanID,
                KayitDonemi = s.KayitYil + "/" + (s.KayitYil + 1) + " " + s.Donemler.DonemAdi,
                IsOnaylandi = s.IsOnaylandi,
                OnayTarihi = s.OnayTarihi,
                OnayAciklama = s.OnayAciklama,
                IsBasarili = s.IsBasarili,
            }).First(p => p.UniqueID == id);
            var danisman = _context.Kullanicilars.First(p => p.KullaniciID == basvuru.TezDanismanID);
            basvuru.DanismanAdi = danisman.Unvanlar.UnvanAdi + " " + danisman.Ad + " " + danisman.Soyad;
            return View(basvuru);
        }

        public ActionResult Sil(Guid uniqueId)
        {
            var kayit = _context.YeterlikBasvurus.First(p => p.UniqueID == uniqueId);

            var mmMessage = YeterlikBus.YeterlikBasvurusuSilKontrol(kayit.YeterlikBasvuruID);
            mmMessage.Title = "Yeterlik Başvurusu Silme İşlemi";
            if (mmMessage.IsSuccess)
            {
                try
                {
                    _context.YeterlikBasvurus.Remove(kayit);
                    _context.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Delete, kayit.ToJson());
                    mmMessage.Messages.Add(kayit.BasvuruTarihi.ToFormatDateAndTime() + " Tarihli başvuru silindi.");
                    mmMessage.IsSuccess = true;

                }
                catch (Exception ex)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.BasvuruTarihi.ToFormatDateAndTime() + " Tarihli başvuru silinemedi.");
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "Yeterlik/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }

            }

            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}