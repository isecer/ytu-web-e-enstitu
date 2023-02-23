using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
            if (model.AktifYeterlikSurecId.HasValue)
            {
                var surec = _context.YeterlikSurecis.First(p => p.YeterlikSurecID == model.AktifYeterlikSurecId);
                model.DonemAdi = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi;
            }
            var kullanici = _context.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            model.AdSoyad = kullanici.Ad + " " + kullanici.Soyad;
            model.EnstituAdi = _context.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
            model.IsYtuOgrencisi = kullanici.YtuOgrencisi && kullanici.OgrenimDurumID == OgrenimDurum.HalenOğrenci;
            model.IsEnstituYetki = kullanici.EnstituKod == enstituKod;
            #endregion


            var q = from yeterlikBasvuru in _context.YeterlikBasvurus.Where(p => p.KullaniciID == UserIdentity.Current.Id)
                    join yeterlikSureci in _context.YeterlikSurecis on yeterlikBasvuru.YeterlikSurecID equals yeterlikSureci.YeterlikSurecID
                    join kullanicilar in _context.Kullanicilars on yeterlikBasvuru.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _context.Programlars on yeterlikBasvuru.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _context.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod) on yeterlikBasvuru.OgrenimTipKod equals ogrenimTipleri.OgrenimTipKod
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
                        IsMuaf = yeterlikBasvuru.IsMuaf,
                        MuafAciklama = yeterlikBasvuru.MuafAciklama,
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

        [Authorize]
        public ActionResult BasvuruYap(int? yeterlikBasvuruId, string ekd = "")
        {
            var model = new KmYeterlikBasvuruDto();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
            var ogrenciBilgi = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(UserIdentity.Current.Id);
            var kul = _context.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            var mmMessage = YeterlikBus.YeterlikBasvuruKontrol(enstituKod, yeterlikBasvuruId);
            if (mmMessage.IsSuccess)
            {
                if (yeterlikBasvuruId > 0)
                {
                    var basvuru = _context.YeterlikBasvurus.First(p => p.YeterlikBasvuruID == yeterlikBasvuruId && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
                    model.YeterlikSurecID = basvuru.YeterlikSurecID;
                    model.YeterlikBasvuruID = basvuru.YeterlikBasvuruID;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = basvuru.KullaniciID;
                    model.AdSoyad = kul.Ad + " " + kul.Soyad;
                    model.OgrenciNo = basvuru.OgrenciNo;
                    model.OgrenimTipKod = basvuru.OgrenimTipKod;
                    model.KayitYil = kul.KayitYilBaslangic.Value;
                    model.KayitDonemID = kul.KayitDonemID.Value;
                    model.OkuduguDonemNo = ogrenciBilgi.OkuduguDonem.ToInt(0);
                }
                else
                {
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = UserIdentity.Current.Id;
                    model.AdSoyad = kul.Ad + " " + kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.OgrenimTipKod = kul.OgrenimTipKod.Value;
                    model.KayitYil = kul.KayitYilBaslangic.Value;
                    model.KayitDonemID = kul.KayitDonemID.Value;
                    model.OkuduguDonemNo = ogrenciBilgi.OkuduguDonem.ToInt(0);

                }
                model.OgrenimTipAdi = _context.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;
                model.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = kul.Programlar.ProgramAdi;
                ViewBag.mmMessage = mmMessage;
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult BasvuruYap(KmYeterlikBasvuruDto kModel, string ekd = "")
        {
            var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = YeterlikBus.YeterlikBasvuruKontrol(enstituKod, kModel.YeterlikBasvuruID);
            kModel.KullaniciID = kayitYetki ? kModel.KullaniciID : UserIdentity.Current.Id;
            if (mmMessage.IsSuccess)
            {
                var kullanici = _context.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                var ogrenciBilgi = Management.StudentControl(kullanici.TcKimlikNo);


                if (kModel.YeterlikBasvuruID > 0)
                {
                    var data = _context.YeterlikBasvurus.FirstOrDefault(p => p.YeterlikBasvuruID == kModel.YeterlikBasvuruID && p.KullaniciID == kModel.KullaniciID);
                    if (data == null) return Index(ekd);

                    data.OkuduguDonemNo = ogrenciBilgi.OkuduguDonem.ToInt().Value;
                    data.OgrenimTipKod = kullanici.OgrenimTipKod.Value;
                    data.OgrenciNo = kullanici.OgrenciNo;
                    data.ProgramKod = kullanici.ProgramKod;
                    data.KayitYil = kullanici.KayitYilBaslangic.Value;
                    data.KayitDonemID = kullanici.KayitDonemID.Value;
                    data.YsBasToplamKrediKriteri = ogrenciBilgi.AktifDonemDers.ToplamKredi;
                    data.YsBasSeminerNotKriteri = ogrenciBilgi.AktifDonemDers.SeminerDersNotu;
                    data.YsBasEtikNotKriteri = ogrenciBilgi.AktifDonemDers.EtikDersNotu;
                    data.TezDanismanID = kullanici.DanismanID.Value;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    _context.SaveChanges();

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
                        OkuduguDonemNo = ogrenciBilgi.OkuduguDonem.ToInt().Value,
                        OgrenimTipKod = kullanici.OgrenimTipKod.Value,
                        OgrenciNo = kullanici.OgrenciNo,
                        ProgramKod = kullanici.ProgramKod,
                        KayitYil = kullanici.KayitYilBaslangic.Value,
                        KayitDonemID = kullanici.KayitDonemID.Value,
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
            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            return RedirectToAction("Index");
        }

        public ActionResult Sil(int id)
        {
            var mmMessage = YeterlikBus.YeterlikBasvurusuSilKontrol(id);
            mmMessage.Title = "Yeterlik Başvurusu Silme İşlemi";
            if (mmMessage.IsSuccess)
            {
                var kayit = _context.YeterlikBasvurus.First(p => p.YeterlikBasvuruID == id); 
                try
                {  
                    _context.YeterlikBasvurus.Remove(kayit);
                    _context.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvurulari", IslemTipi.Delete, kayit.ToJson()); 
                    mmMessage.Messages.Add(kayit.BasvuruTarihi.ToFormatDateAndTime() + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = Msgtype.Success;
                    
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.BasvuruTarihi.ToFormatDateAndTime() + " Tarihli başvuru silinemedi."); 
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "Yeterlik/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}