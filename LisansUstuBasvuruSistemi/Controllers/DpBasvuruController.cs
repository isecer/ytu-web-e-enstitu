using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using System.Collections.Generic;
using DevExpress.Data.Mask.Internal;
using LisansUstuBasvuruSistemi.WebServiceData.ObsService;
using System.IO;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using System.Xml;
using LisansUstuBasvuruSistemi.Ws_ObsService;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class DpBasvuruController : Controller
    {
        // GET: DonemProjesiBasvuru
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        [AllowAnonymous]
        public ActionResult Index(string ekd, Guid? showBasvuruUniqueId = null, Guid? isDegerlendirme = null)
        {
            if (!UserIdentity.Current.IsAuthenticated && !isDegerlendirme.HasValue) return RedirectToActionPermanent("Login", "Account");

            return Index(new FmDonemProjesiBasvuruDto() { PageSize = 50, ShowBasvuruUniqueId = showBasvuruUniqueId, IsDegerlendirme = isDegerlendirme }, ekd);
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Index(FmDonemProjesiBasvuruDto model, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            if (!UserIdentity.Current.IsAuthenticated && !model.IsDegerlendirme.HasValue) return RedirectToActionPermanent("Login", "Account");
            #region BilgiModel  
            if (!model.IsDegerlendirme.HasValue)
            {
                var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                model.IsDonemProjesiBasvurusuAcik = DonemProjesiAyar.DonemProjesiBasvuruAlimiAcik.GetAyarDp(enstituKod).ToBoolean(false);
                model.AdSoyad = kullanici.Ad + " " + kullanici.Soyad;
                model.EnstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
                model.IsYtuOgrencisi = kullanici.YtuOgrencisi && kullanici.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci;
                model.IsEnstituYetki = kullanici.EnstituKod == enstituKod;
                model.IsOgrenimSeviyeYetki = kullanici.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans;


                model.IsAktifOgrenimBasvuruVar = _entities.DonemProjesis.Any(a => a.KullaniciID == kullanici.KullaniciID && a.OgrenciNo == kullanici.OgrenciNo && a.ProgramKod==kullanici.ProgramKod);
                if (model.IsOgrenimSeviyeYetki)
                {
                    KullanicilarBus.OgrenciBilgisiGuncelleObs(kullanici.KullaniciID);
                }

            }

            #endregion


            var q =
                    from donemProjesi in _entities.DonemProjesis.Where(p => !model.IsDegerlendirme.HasValue || p.DonemProjesiBasvurus.Any(a => a.DonemProjesiJurileris.Any(a2 => a2.UniqueID == model.IsDegerlendirme)))
                    join kullanicilar in _entities.Kullanicilars on donemProjesi.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _entities.Programlars on donemProjesi.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _entities.OgrenimTipleris on new { donemProjesi.EnstituKod, donemProjesi.OgrenimTipKod } equals new { ogrenimTipleri.EnstituKod, ogrenimTipleri.OgrenimTipKod }
                    where donemProjesi.EnstituKod == enstituKod && donemProjesi.KullaniciID == (model.IsDegerlendirme.HasValue ? donemProjesi.KullaniciID : UserIdentity.Current.Id)
                    select new FrDonemProjesiBasvuruDto
                    {

                        DonemProjesiID = donemProjesi.DonemProjesiID,
                        UniqueID = donemProjesi.UniqueID,
                        BasvuruTarihi = donemProjesi.BasvuruTarihi,
                        ResimAdi = kullanicilar.ResimAdi,
                        UserKey = kullanicilar.UserKey,
                        AdSoyad = kullanicilar.Ad + " " + kullanicilar.Soyad,
                        OgrenciNo = donemProjesi.OgrenciNo,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        IsYeniBasvuruYapilabilir = donemProjesi.IsYeniBasvuruYapilabilir,
                        SonBasvuruDurum = donemProjesi.DonemProjesiBasvurus.OrderByDescending(o => o.BasvuruTarihi).Select(s => new DpBasvuruDurumDto
                        {
                            DonemProjesiID = s.DonemProjesiID,
                            DonemProjesiBasvuruID = s.DonemProjesiBasvuruID,
                            BasvuruYil = s.BasvuruYil,
                            BasvuruDonemAdi = s.Donemler.DonemAdi,
                            BasvuruTarihi = s.BasvuruTarihi,
                            IsDanismanOnay = s.IsDanismanOnay,
                            DanismanOnayAciklama = s.DanismanOnayAciklama,
                            EYKYaGonderildi = s.EYKYaGonderildi,
                            EYKYaGonderimDurumAciklamasi = s.EYKYaGonderimDurumAciklamasi,
                            EYKYaHazirlandi = s.EYKYaHazirlandi,
                            EYKDaOnaylandi = s.EYKDaOnaylandi,
                            EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi,
                            IsOyBirligiOrCoklugu = s.IsOyBirligiOrCoklugu,
                            DonemProjesiEnstituOnayDurumID = s.DonemProjesiEnstituOnayDurumID,
                            EnstituOnayAciklama = s.EnstituOnayAciklama,
                            DonemProjesiDurumID = s.DonemProjesiDurumID,
                            DonemProjesiJuriOnayDurumID = s.DonemProjesiJuriOnayDurumID,
                            IsJuriOlusturuldu = s.DonemProjesiJurileris.Any(),
                            IsSrTalebiYapildi = s.SRTalepleris.Any()

                        }).FirstOrDefault()


                    };

            model.RowCount = q.Count();
            if (model.ShowBasvuruUniqueId.HasValue)
                q = q.OrderBy(o => o.UniqueID == model.ShowBasvuruUniqueId ? 1 : 2).ThenBy(t => t.BasvuruTarihi);
            else q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            return View(model);
        }


        public ActionResult BasvuruYap(Guid? id, string ekd = "")
        {
            var mMessage = new MmMessage();
            KmDonemProjesiDto model;
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            var errorMessage = DonemProjesiBus.DonemProjesiKontrol(enstituKod, id, null);

            if (!errorMessage.Any())
            {
                var ogrenciBilgi = KullanicilarBus.OgrenciBilgisiGuncelleObs(UserIdentity.Current.Id);
                // if (ogrenciBilgi.DanismanInfo == null || ogrenciBilgi.IsDanismanHesabiBulunamadi)
                //{
                //    var msg = ogrenciBilgi.DanismanInfo == null
                //        ? "Başvuru yapabilmeniz için proje yürütücü bilginizin OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz."
                //        : $"Başvuru yapabilmeniz için proje yürütücünüzün '{ogrenciBilgi.DanismanInfo.UNVAN_AD} {ogrenciBilgi.DanismanInfo.AD} {ogrenciBilgi.DanismanInfo.SOYAD}' lisansüstü sisteminde kullanıcı hesabı oluşturması gerekmektedir.";
                //    MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, msg);
                //    return RedirectToAction("Index");
                //}
                model = DonemProjesiBus.GetDonemProjesiBasvuru(id, enstituKod);

            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, errorMessage.ToArray());
                return RedirectToAction("Index");

            }
            if (mMessage.MessageType != MsgTypeEnum.Information) mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;

            return View(model);
        }

        [HttpPost]
        public ActionResult BasvuruYap(KmDonemProjesiDto kModel, string ekd = "")
        {
            var kayitYetki = RoleNames.DonemProjesiEnstituBasvuruOnayYetkisi.InRoleCurrent();
            kModel.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            kModel.KullaniciID = kayitYetki ? kModel.KullaniciID : UserIdentity.Current.Id;
            KullanicilarBus.OgrenciBilgisiGuncelleObs(kModel.KullaniciID);
            var errprMessages = DonemProjesiBus.DonemProjesiKontrol(kModel.EnstituKod, kModel.UniqueID, null);
            if (!errprMessages.Any())
            {
                var uniqueId = DonemProjesiBus.AddOrUpdateDonemProjesi(kModel);
                return RedirectToAction("Index", new { showBasvuruUniqueId = uniqueId });
            }
            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, errprMessages.ToArray());
            return RedirectToAction("Index");
        } 
        public ActionResult Sil(Guid? uniqueId)
        {

            var mmMessage = DonemProjesiBus.DonemProjesiSilKontrol(uniqueId);

            if (mmMessage.IsSuccess)
            {
                try
                {
                    var donemProjesi = _entities.DonemProjesis.FirstOrDefault(f => f.UniqueID == uniqueId);

                    if (donemProjesi != null)
                    {
                        _entities.DonemProjesis.Remove(donemProjesi);
                        _entities.SaveChanges();
                        mmMessage.Messages.Add(donemProjesi.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Dönem Projesi başvurusu silindi!");
                        LogIslemleri.LogEkle("DonemProjesi", LogCrudType.Delete, donemProjesi.ToJson());
                    }
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Silmek istediğiniz Dönem Projesi başvurusu silinemedi!");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            return Json(new
            {
                mmMessage.IsSuccess,
                messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage)
            }, "application/json", JsonRequestBehavior.AllowGet);
        } 
        public ActionResult GetDonemProjesiBasvuruFormu(Guid donemProjesiUniqueId, Guid? donemProjesiBasvuruUniqueId)
        {


            var mMessage = new MmMessage();
            var donemProjesi = _entities.DonemProjesis.First(p => p.UniqueID == donemProjesiUniqueId);
            var donemProjesiBasvuru = donemProjesi.DonemProjesiBasvurus.FirstOrDefault(p => p.UniqueID == donemProjesiBasvuruUniqueId);
            var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(donemProjesi.KullaniciID);

            var model = donemProjesiBasvuru ?? new DonemProjesiBasvuru { DonemProjesiID = donemProjesi.DonemProjesiID };
            var view = ViewRenderHelper.RenderPartialView("DpBasvuru", "DonemProjesiBasvuruFormu", model);
            mMessage.IsSuccess = true;
            if (mMessage.MessageType != MsgTypeEnum.Information) mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = view,
                Messages = strView
            }.ToJsonResult();

        }

        [ValidateInput(false)]
        public ActionResult DonemProjesiBasvuruFormuPost(DonemProjesiBasvuru kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Dönem Projesi Başvuru Formu Oluşturma İşlemi"
            };

            var donemProjesi = _entities.DonemProjesis.First(p => p.DonemProjesiID == kModel.DonemProjesiID);
            var donemProjesiBasvuru = donemProjesi.DonemProjesiBasvurus.FirstOrDefault(p => p.DonemProjesiBasvuruID == kModel.DonemProjesiBasvuruID);
            var donemProjesiKontrolMessages = DonemProjesiBus.DonemProjesiKontrol(donemProjesi.EnstituKod, donemProjesi.UniqueID, donemProjesiBasvuru?.UniqueID);

            if (donemProjesiKontrolMessages.Any())
            {
                mMessage.Messages.AddRange(donemProjesiKontrolMessages);
            }

            if (!mMessage.Messages.Any())
            {

                var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(donemProjesi.KullaniciID);
                var ogrenci = _entities.Kullanicilars.First(p => p.KullaniciID == donemProjesi.KullaniciID);



                if (kModel.ProjeBasligi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Proje Başlığı bilgisi boş bırakılamaz.");
                }

                mMessage.MessagesDialog.Add(new MrMessage
                {
                    PropertyName = "ProjeBasligi",
                    MessageType = kModel.ProjeBasligi.IsNullOrWhiteSpace() ? MsgTypeEnum.Error : MsgTypeEnum.Success
                });
                if (kModel.ProjeOzeti.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Proje Özeti bilgisi boş bırakılamaz.");

                }

                mMessage.MessagesDialog.Add(new MrMessage
                {
                    PropertyName = "ProjeOzeti",
                    MessageType = kModel.ProjeOzeti.IsNullOrWhiteSpace() ? MsgTypeEnum.Error : MsgTypeEnum.Success
                });
                if (!kModel.IsOgrenciTaahhut)
                {
                    mMessage.Messages.Add("Taahhüt onayı veriniz.");

                }
                mMessage.MessagesDialog.Add(new MrMessage
                {
                    PropertyName = "IsOgrenciTaahhut",
                    MessageType = !kModel.IsOgrenciTaahhut ? MsgTypeEnum.Error : MsgTypeEnum.Success
                });
                if (!mMessage.Messages.Any())
                {
                    if (donemProjesiBasvuru != null)
                    {
                        donemProjesiBasvuru.ProjeBasligi = kModel.ProjeBasligi;
                        donemProjesiBasvuru.ProjeOzeti = kModel.ProjeOzeti;
                        donemProjesiBasvuru.IsOgrenciTaahhut = kModel.IsOgrenciTaahhut;
                        donemProjesiBasvuru.IslemTarihi = DateTime.Now;
                        donemProjesiBasvuru.IslemYapanIP = UserIdentity.Ip;
                        donemProjesiBasvuru.IslemYapanID = UserIdentity.Current.Id;
                        DonemProjesiBus.DonemProjesiDurumSet(donemProjesiBasvuru.DonemProjesiBasvuruID);
                        _entities.SaveChanges();
                        LogIslemleri.LogEkle("DonemProjesiBasvuru", LogCrudType.Insert, donemProjesiBasvuru.ToJson());
                    }
                    else
                    {
                        var uniqueId = Guid.NewGuid();
                        var donemBilgi = DateTime.Now.ToDonemProjesiDonemBilgi(donemProjesi.EnstituKod);
                        donemProjesi.IsYeniBasvuruYapilabilir = false;
                        donemProjesi.DonemProjesiBasvurus.Add(new DonemProjesiBasvuru
                        {
                            UniqueID = uniqueId,
                            FormKodu = uniqueId.ToString().Substring(0, 8),
                            BasvuruTarihi = DateTime.Now,
                            BasvuruYil = donemBilgi.BaslangicYil,
                            BasvuruDonemID = donemBilgi.DonemId,
                            OkuduguDonemNo = studentInfo.OkuduguDonemNo,
                            IsOgrenciTaahhut = kModel.IsOgrenciTaahhut,
                            ProjeBasligi = kModel.ProjeBasligi,
                            ProjeOzeti = kModel.ProjeOzeti,
                            DonemProjesiEnstituOnayDurumID = DonemProjesiEnstituOnayDurumEnum.KabulEdildi,
                            EnstituOnayTarihi = DateTime.Now,
                            TezDanismanID = ogrenci.DanismanID.Value,
                            DonemProjesiDurumID = DonemProjesiDurumEnum.YurutucuOnaySureci,
                            IslemTarihi = DateTime.Now,
                            IslemYapanIP = UserIdentity.Ip,
                            IslemYapanID = UserIdentity.Current.Id

                        });


                        _entities.SaveChanges();

                        var lastDonemProjesiBasvuruId = donemProjesi.DonemProjesiBasvurus.Max(o => o.DonemProjesiBasvuruID);
                        DonemProjesiBus.SendMailBasvuruBilgisi(lastDonemProjesiBasvuruId);

                    }

                    mMessage.IsSuccess = true;

                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult GetDonemProjesiDurumView(Guid? donemProjesiUniqueId)
        {
            var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.Where(p => p.DonemProjesi.UniqueID == donemProjesiUniqueId).OrderByDescending(o => o.BasvuruTarihi).Select(s => new DpBasvuruDurumDto
            {
                DonemProjesiID = s.DonemProjesiID,
                DonemProjesiBasvuruID = s.DonemProjesiBasvuruID,
                BasvuruYil = s.BasvuruYil,
                BasvuruDonemAdi = s.Donemler.DonemAdi,
                BasvuruTarihi = s.BasvuruTarihi,
                IsDanismanOnay = s.IsDanismanOnay,
                DanismanOnayAciklama = s.DanismanOnayAciklama,
                EYKYaGonderildi = s.EYKYaGonderildi,
                EYKYaGonderimDurumAciklamasi = s.EYKYaGonderimDurumAciklamasi,
                EYKYaHazirlandi = s.EYKYaHazirlandi,
                EYKDaOnaylandi = s.EYKDaOnaylandi,
                EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi,
                IsOyBirligiOrCoklugu = s.IsOyBirligiOrCoklugu,
                DonemProjesiEnstituOnayDurumID = s.DonemProjesiEnstituOnayDurumID,
                EnstituOnayAciklama = s.EnstituOnayAciklama,
                DonemProjesiDurumID = s.DonemProjesiDurumID,
                DonemProjesiJuriOnayDurumID = s.DonemProjesiJuriOnayDurumID,
                IsJuriOlusturuldu = s.DonemProjesiJurileris.Any(),
                IsSrTalebiYapildi = s.SRTalepleris.Any()

            }).FirstOrDefault() ?? new DpBasvuruDurumDto();
            var dpBasvuruDurumView = donemProjesiBasvuru.ToDpBasvuruDurumView().ToString();
            var dpBasvuruDonemView = donemProjesiBasvuru.ToDpBasvuruDonemView().ToString();
            return new { dpBasvuruDurumView, dpBasvuruDonemView }.ToJsonResult();
        }

        public ActionResult BasvuruFormuSil(Guid? uniqueId)
        {

            var mmMessage = DonemProjesiBus.DonemProjesiBasvuruFormuSilKontrol(uniqueId);

            if (mmMessage.IsSuccess)
            {
                try
                {
                    var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.FirstOrDefault(f => f.UniqueID == uniqueId);

                    if (donemProjesiBasvuru != null)
                    {


                        donemProjesiBasvuru.DonemProjesi.IsYeniBasvuruYapilabilir = true;
                        _entities.SRTalepleris.RemoveRange(donemProjesiBasvuru.SRTalepleris);
                        _entities.DonemProjesiJurileris.RemoveRange(donemProjesiBasvuru.DonemProjesiJurileris);
                        _entities.DonemProjesiBasvurus.Remove(donemProjesiBasvuru);
                        _entities.SaveChanges();
                        mmMessage.Messages.Add(donemProjesiBasvuru.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Dönem Projesi başvurusu silindi!");
                        LogIslemleri.LogEkle("DonemProjesiBasvuru", LogCrudType.Delete, donemProjesiBasvuru.ToJson());
                    }
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Silmek istediğiniz Dönem Projesi başvurusu silinemedi!");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            return Json(new
            {
                mmMessage.IsSuccess,
                messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage)
            }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetDonemProjesiJuriFormu(Guid? donemProjesiUniqueId, Guid donemProjesiBasvuruUniqueId)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };

            var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.First(f => f.UniqueID == donemProjesiBasvuruUniqueId);



            var unvanlar = UnvanlarBus.GetCmbJuriUnvanlar(true);


            var kul = donemProjesiBasvuru.DonemProjesi.Kullanicilar;


            var model = new DonemProjesiJuriFormuKayitDto
            {
                UniqueID = donemProjesiBasvuru.UniqueID,
                DonemProjesiBasvuruID = donemProjesiBasvuru.DonemProjesiBasvuruID,
                KullaniciId = donemProjesiBasvuru.DonemProjesi.KullaniciID,
                TezDanismanID = donemProjesiBasvuru.TezDanismanID,
                DonemProjesiID = donemProjesiBasvuru.DonemProjesiID,
                ProjeBasligi = donemProjesiBasvuru.ProjeBasligi,
                SListUnvanAdi = new SelectList(unvanlar, "Value", "Caption"),
            };


            var danisman = _entities.Kullanicilars.First(f => f.KullaniciID == kul.DanismanID.Value);


            model.OgrenciAdSoyad = kul.Ad + " " + kul.Soyad + " - " + donemProjesiBasvuru.DonemProjesi.OgrenciNo;

            model.JoFormJuriList = donemProjesiBasvuru.DonemProjesiJurileris.Select(s => new KrDonemProjesiJurileri
            {
                DonemProjesiJuriID = s.DonemProjesiJuriID,
                DonemProjesiBasvuruID = s.DonemProjesiBasvuruID,
                RowNum = s.RowNum,
                IsTezDanismani = s.IsTezDanismani,
                UnvanAdi = s.UnvanAdi,
                AdSoyad = s.AdSoyad,
                EMail = s.EMail,
                AnabilimdaliAdi = s.AnabilimdaliAdi,
                SlistUnvanAdi = new SelectList(unvanlar, "Value", "Caption", s.UnvanAdi),
            }).ToList();

            var joFormDanisman = model.JoFormJuriList.FirstOrDefault(f => f.IsTezDanismani);
            if (joFormDanisman != null)
            {
                joFormDanisman.AdSoyad = danisman.Ad + " " + danisman.Soyad;
                joFormDanisman.UnvanAdi = danisman.Unvanlar.UnvanAdi.ToJuriUnvanAdi();
                joFormDanisman.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                joFormDanisman.EMail = danisman.EMail;

            }
            else
            {
                model.JoFormJuriList.Insert(0, new KrDonemProjesiJurileri
                {
                    DonemProjesiBasvuruID = donemProjesiBasvuru.DonemProjesiBasvuruID,
                    RowNum = 1,
                    IsTezDanismani = true,
                    UnvanAdi = danisman.Unvanlar.UnvanAdi.ToJuriUnvanAdi(),
                    AdSoyad = danisman.Ad + " " + danisman.Soyad,
                    EMail = danisman.EMail,
                    AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi,
                    SlistUnvanAdi = new SelectList(unvanlar, "Value", "Caption", danisman.Unvanlar.UnvanAdi.ToJuriUnvanAdi()),
                });
            }



            var view = "";
            if (!mMessage.Messages.Any())
            {
                view = ViewRenderHelper.RenderPartialView("DpBasvuru", "DonemProjesiJuriFormu", model);
            }
            else { mMessage.IsSuccess = false; mMessage.MessageType = MsgTypeEnum.Warning; }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = view,
                Messages = strView
            }.ToJsonResult();
        }

        public ActionResult DonemProjesiJuriFormuPost(DonemProjesiJuriFormuKayitDto kModel)
        {
            var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.First(p => p.UniqueID == kModel.UniqueID);
            var kul = donemProjesiBasvuru.DonemProjesi.Kullanicilar;

            var enstituKod = kul.EnstituKod;

            var enstitu = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
            var enstituAdi = enstitu.EnstituAd;

            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };
            int selectedJuriNum = 0;
            bool showSinavKayitForm = false;
            var isBasvuruAcik = DonemProjesiAyar.DonemProjesiBasvuruAlimiAcik.GetAyarDp(enstituKod, "false").ToBoolean(false);




            var juriOlusturmaYetkisi = RoleNames.DonemProjesiJuriOlusturmaYetkisi.InRole();


            if (!juriOlusturmaYetkisi)
            {
                mMessage.Messages.Add("Dönem Projesi Jüri Formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!isBasvuruAcik)
            {
                mMessage.Messages.Add("Dönem Projesi Jüri Formu başvuru süreci kapalıdır. Detaylı bilgi almak için " + enstituAdi + " ile iletişime geçiniz.");
            }
            else
            {
                if (donemProjesiBasvuru.EYKDaOnaylandi == true)
                    mMessage.Messages.Add("Jüri formunuz EYK'da onaylandığından Form üzerinden herhangi bir değişiklik yapamazsınız!");
                else if (donemProjesiBasvuru.EYKYaHazirlandi == true)
                    mMessage.Messages.Add("Jüri formunuz EYK'da hazırlandığından Form üzerinden herhangi bir değişiklik yapamazsınız!");
                else if (donemProjesiBasvuru.EYKYaGonderildi == true)
                    mMessage.Messages.Add("Jüri formunuz EYK'ya gönderimi yapıldığından Form üzerinden herhangi bir değişiklik yapamazsınız!");

                if (mMessage.Messages.Count == 0)
                {

                    var rowNums = kModel.RowNum.Select((s, i) => new { RowNum = s, Inx = (i + 1) }).ToList();
                    var isTezDanismanis = kModel.IsTezDanismani.Select((s, i) => new { IsTezDanismani = s, Inx = (i + 1) }).ToList();
                    var adSoyads = kModel.AdSoyad.Select((s, i) => new { AdSoyad = s, Inx = (i + 1) }).ToList();
                    var unvanAdis = kModel.UnvanAdi.Select((s, i) => new { UnvanAdi = s, Inx = (i + 1) }).ToList();
                    var eMails = kModel.EMail.Select((s, i) => new { EMail = s.Trim(), Inx = (i + 1) }).ToList();
                    var anabilimdaliAdis = kModel.AnabilimdaliAdi.Select((s, i) => new { AnabilimdaliAdi = s, Inx = (i + 1) }).ToList();

                    var qData = (from ad in adSoyads
                                 join rwn in rowNums on ad.Inx equals rwn.Inx
                                 join td in isTezDanismanis on ad.Inx equals td.Inx
                                 join un in unvanAdis on ad.Inx equals un.Inx
                                 join em in eMails on ad.Inx equals em.Inx
                                 join abd in anabilimdaliAdis on ad.Inx equals abd.Inx

                                 select new
                                 {
                                     ad.Inx,
                                     rwn.RowNum,
                                     td.IsTezDanismani,
                                     ad.AdSoyad,
                                     AdSoyadSuccess = !ad.AdSoyad.IsNullOrWhiteSpace(),
                                     un.UnvanAdi,
                                     UnvanAdiSuccess = !un.UnvanAdi.IsNullOrWhiteSpace(),
                                     em.EMail,
                                     EMailSuccess = !em.EMail.IsNullOrWhiteSpace() && !em.EMail.ToIsValidEmail(),
                                     abd.AnabilimdaliAdi,
                                     AnabilimdaliAdiSuccess = !abd.AnabilimdaliAdi.IsNullOrWhiteSpace(),


                                 }).ToList();

                    var qGroup = (from s in qData
                                  group new { s } by new
                                  {
                                      s.Inx,
                                      s.RowNum,
                                      s.IsTezDanismani,
                                      s.AdSoyadSuccess,
                                      s.UnvanAdiSuccess,
                                      s.EMailSuccess,
                                      s.AnabilimdaliAdi,
                                      s.AnabilimdaliAdiSuccess,
                                      IsSuccessRow = s.AdSoyadSuccess && s.UnvanAdiSuccess && s.EMailSuccess && s.AnabilimdaliAdiSuccess
                                  }

                into g1
                                  select new
                                  {
                                      g1.Key.Inx,
                                      g1.Key.RowNum,
                                      g1.Key.IsTezDanismani,
                                      g1.Key.IsSuccessRow,
                                      DetayData = g1.ToList()
                                  }).AsQueryable();


                    var groupData = qGroup.ToList();

                    var rowInx = 0;
                    foreach (var item in groupData)
                    {
                        rowInx++;
                        if (rowInx > 3) rowInx = 1;

                        if (!item.IsSuccessRow)
                        {
                            var rowAd = rowInx == 1 ? "1. Jüri (Proje Yürütücüsü Bilgileri)" : $"{rowInx}. Jüri Önerisi";
                            mMessage.Messages.Add(rowAd + " kısmında hatalı veri girişleri mevcut!");
                            if (selectedJuriNum == 0) selectedJuriNum = item.RowNum;
                        }


                        foreach (var item2 in item.DetayData)
                        {
                            var adSoyadMsgType = item2.s.AdSoyadSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            var unvanAdiMsgType = item2.s.UnvanAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            var emailMsgType = item2.s.EMailSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            var anabilimdaliMsgType = item2.s.AnabilimdaliAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
                            if (item.IsSuccessRow)
                            {
                                adSoyadMsgType = MsgTypeEnum.Nothing;
                                unvanAdiMsgType = MsgTypeEnum.Nothing;
                                emailMsgType = MsgTypeEnum.Nothing;
                                anabilimdaliMsgType = MsgTypeEnum.Nothing;
                            }

                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = adSoyadMsgType, PropertyName = "AdSoyad_" + item.RowNum });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = unvanAdiMsgType, PropertyName = "UnvanAdi_" + item.RowNum });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = emailMsgType, PropertyName = "EMail_" + item.RowNum });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = anabilimdaliMsgType, PropertyName = "AnabilimdaliAdi_" + item.RowNum });

                        }

                    }
                    if (mMessage.Messages.Count == 0)
                    {
                        var kData = qData.Where(p => p.AdSoyadSuccess).ToList();
                        var varolanJurilers = donemProjesiBasvuru.DonemProjesiJurileris.ToList();
                        var isDegisiklikVar = donemProjesiBasvuru.DonemProjesiJurileris.Count != kData.Count;
                        foreach (var item in kData)
                        {
                            if (!isDegisiklikVar)
                            {

                                var rw = varolanJurilers.First(p => p.RowNum == item.RowNum);
                                if (rw.IsTezDanismani != item.IsTezDanismani || rw.AdSoyad != item.AdSoyad || rw.UnvanAdi != item.UnvanAdi ||
                                    rw.EMail != item.EMail || rw.UnvanAdi != item.UnvanAdi ||
                                    rw.AnabilimdaliAdi != item.AnabilimdaliAdi) isDegisiklikVar = true;
                            }
                        }


                        var jurilers = kData.Where(p => p.AdSoyadSuccess).Select(s =>
                                                new DonemProjesiJurileri
                                                {
                                                    UniqueID = Guid.NewGuid(),
                                                    IsTezDanismani = s.IsTezDanismani,
                                                    RowNum = s.RowNum,
                                                    UnvanAdi = s.UnvanAdi.ToUpper(),
                                                    AdSoyad = s.AdSoyad.ToUpper(),
                                                    EMail = s.EMail,
                                                    AnabilimdaliAdi = s.AnabilimdaliAdi.ToUpper()
                                                }).OrderBy(o =>
                                                o.RowNum).ToList();

                        foreach (var item in jurilers)
                        {
                            var juri = donemProjesiBasvuru.DonemProjesiJurileris.FirstOrDefault(p => p.RowNum == item.RowNum);
                            if (juri != null)
                            {
                                juri.AdSoyad = item.AdSoyad;
                                juri.UnvanAdi = item.UnvanAdi;
                                juri.EMail = item.EMail;
                                juri.AnabilimdaliAdi = item.AnabilimdaliAdi;
                                juri.IslemTarihi = DateTime.Now;
                                juri.IslemYapanIP = UserIdentity.Ip;
                                juri.IslemYapanID = UserIdentity.Current.Id;
                            }
                            else
                            {
                                donemProjesiBasvuru.DonemProjesiJurileris.Add(new DonemProjesiJurileri()
                                {
                                    UniqueID = item.UniqueID,
                                    RowNum = item.RowNum,
                                    IsTezDanismani = item.IsTezDanismani,
                                    AdSoyad = item.AdSoyad,
                                    UnvanAdi = item.UnvanAdi,
                                    EMail = item.EMail,
                                    AnabilimdaliAdi = item.AnabilimdaliAdi,
                                    IslemTarihi = DateTime.Now,
                                    IslemYapanIP = UserIdentity.Ip,
                                    IslemYapanID = UserIdentity.Current.Id
                                });
                            }
                        }
                        _entities.SaveChanges();
                        showSinavKayitForm = !donemProjesiBasvuru.SRTalepleris.Any();
                        DonemProjesiBus.DonemProjesiDurumSet(donemProjesiBasvuru.DonemProjesiBasvuruID);
                    }
                }
            }
            mMessage.IsSuccess = mMessage.Messages.Count == 0;
            if (mMessage.Messages.Count > 0)
            {
                mMessage.Title = "Dönem Projesi Jüri Formu Aşağıdaki Sebeplerden Dolayı Oluşturulamadı.";
                mMessage.IsSuccess = false;
                mMessage.MessageType = MsgTypeEnum.Warning;
            }
            return new
            {
                mMessage,
                selectedJuriNum,
                showSinavKayitForm
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.DonemProjesiEnstituBasvuruOnayYetkisi)]
        public ActionResult EnstituOnay(Guid donemProjesiBasvuruUniqueId, int? donemProjesiEnstituOnayDurumId, string enstituOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Enstitu Başvuru Onay İşlemi",
                MessageType = MsgTypeEnum.Warning

            };
            if (donemProjesiEnstituOnayDurumId.HasValue && donemProjesiEnstituOnayDurumId != DonemProjesiEnstituOnayDurumEnum.KabulEdildi && enstituOnayAciklama.IsNullOrWhiteSpace())
            {
                var donemProjesiEnstituOnayDurum = _entities.DonemProjesiEnstituOnayDurumlaris.First(f =>
                    f.DonemProjesiEnstituOnayDurumID == donemProjesiEnstituOnayDurumId);
                mmMessage.Messages.Add($"'{donemProjesiEnstituOnayDurum.EnstituOnayDurumAdi}' seçeneğini seçtiğiniz için açıklama girmelisiniz.");
            }

            if (!mmMessage.Messages.Any())
            {
                var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.First(p => p.UniqueID == donemProjesiBasvuruUniqueId);

                var durumIds = new List<int> { DonemProjesiEnstituOnayDurumEnum.RetEdildi, DonemProjesiEnstituOnayDurumEnum.IptalEdildi };
                var sendMail = durumIds.Contains(donemProjesiEnstituOnayDurumId ?? 0) && donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID != donemProjesiEnstituOnayDurumId;
                donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID = donemProjesiEnstituOnayDurumId;
                donemProjesiBasvuru.EnstituOnayAciklama = enstituOnayAciklama;
                donemProjesiBasvuru.EnstituOnayTarihi = DateTime.Now;


                _entities.SaveChanges();
                DonemProjesiBus.DonemProjesiDurumSet(donemProjesiBasvuru.DonemProjesiBasvuruID);
                mmMessage.IsSuccess = true;
                mmMessage.MessageType = MsgTypeEnum.Success;
                LogIslemleri.LogEkle("DonemProjesiBasvuru", LogCrudType.Update, donemProjesiBasvuru.ToJson());
                if (sendMail)
                    DonemProjesiBus.SendMailEnstituOnay(donemProjesiBasvuru.DonemProjesiBasvuruID);
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }


        public ActionResult DanismanOnay(Guid donemProjesiBasvuruUniqueId, bool? isDanismanOnay, string danismanOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Proje Yürütücüsü Dönem Projesi Sınavı Başvurusu Onay İşlemi",
                MessageType = MsgTypeEnum.Warning

            };
            var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.First(p => p.UniqueID == donemProjesiBasvuruUniqueId);
            if (!RoleNames.DonemProjesiDanismanBasvuruOnayYetkisi.InRoleCurrent())
            {
                mmMessage.Messages.Add("Dönem projesi sınav başvurusu Proje Yürütücüsü onayı işlemi için yetkili değilsiniz.");
            }
            else if (!DonemProjesiBus.IsYetkiliKullanici() && donemProjesiBasvuru.DonemProjesiDurumID != DonemProjesiDurumEnum.YurutucuOnaySureci)
            {
                mmMessage.Messages.Add("Yürütücü Onay sürecinde bulunmayan bir başvuru için. Yürütücü onay işlemi yapılamaz.");
            }
            else if (isDanismanOnay == false && danismanOnayAciklama.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("'Onaylamıyorum' seçeneğini seçtiğiniz için açıklama girmelisiniz.");
            }

            var showJuriOlustur = false;
            if (!mmMessage.Messages.Any())
            {
                var sendMail = isDanismanOnay.HasValue && isDanismanOnay != donemProjesiBasvuru.IsDanismanOnay;
                donemProjesiBasvuru.IsDanismanOnay = isDanismanOnay;
                donemProjesiBasvuru.DanismanOnayAciklama = danismanOnayAciklama;
                donemProjesiBasvuru.DanismanOnayTarihi = DateTime.Now;

                _entities.SaveChanges();
                showJuriOlustur = donemProjesiBasvuru.IsDanismanOnay == true && !donemProjesiBasvuru.DonemProjesiJurileris.Any();
                DonemProjesiBus.DonemProjesiDurumSet(donemProjesiBasvuru.DonemProjesiBasvuruID);
                mmMessage.IsSuccess = true;
                mmMessage.MessageType = MsgTypeEnum.Success;
                LogIslemleri.LogEkle("DonemProjesiBasvuru", LogCrudType.Update, donemProjesiBasvuru.ToJson());
                if (sendMail)
                    DonemProjesiBus.SendMailDanismanOnay(donemProjesiBasvuru.DonemProjesiBasvuruID);
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView, showJuriOlustur }, "application/json", JsonRequestBehavior.AllowGet);

        }



        public ActionResult RezervasyonAl(Guid donemProjesiBasvuruUniqueId)
        {
            var toplantiYetki = RoleNames.DonemProjesiSinaviOlusturmaYetkisi.InRoleCurrent();
            var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.First(p => p.UniqueID == donemProjesiBasvuruUniqueId);
            var model = new SrTalepleriKayitDto();
            if (!toplantiYetki && donemProjesiBasvuru.TezDanismanID != UserIdentity.Current.Id) model.YetkisizErisim = true;
            else
            {
                model.BasvuruUniqueId = donemProjesiBasvuruUniqueId;
                if (donemProjesiBasvuru.SRTalepleris.Any())
                {

                    var srTalep = donemProjesiBasvuru.SRTalepleris.First();
                    var tarih = model.IsSalonSecilsin ? srTalep.Tarih : (srTalep.Tarih.AddHours(srTalep.BasSaat.Hours).AddMinutes(srTalep.BasSaat.Minutes));

                    model.IsSalonSecilsin = srTalep.SRSalonID.HasValue;
                    model.IsOnline = srTalep.IsOnline;
                    model.SRTalepID = srTalep.SRTalepID;
                    model.SRTalepTipID = srTalep.SRTalepTipID;
                    model.EnstituKod = srTalep.EnstituKod;
                    model.TalepYapanID = srTalep.TalepYapanID;
                    model.SRSalonID = srTalep.SRSalonID;
                    model.SalonAdi = srTalep.SalonAdi;
                    model.Tarih = tarih;
                    model.HaftaGunID = srTalep.HaftaGunID;
                    model.BasSaat = srTalep.BasSaat;
                    model.BitSaat = srTalep.BitSaat;
                    model.DanismanAdi = srTalep.DanismanAdi;
                    model.EsDanismanAdi = srTalep.EsDanismanAdi;
                    model.TezOzeti = srTalep.TezOzeti;
                    model.TezOzetiHtml = srTalep.TezOzetiHtml;
                    model.SRDurumID = srTalep.SRDurumID;
                    model.SRDurumAciklamasi = srTalep.SRDurumAciklamasi;
                    model.IslemTarihi = srTalep.IslemTarihi;
                    model.IslemYapanID = srTalep.IslemYapanID;
                    model.IslemYapanIP = srTalep.IslemYapanIP;
                    model.Aciklama = srTalep.Aciklama;
                }
                else
                {
                    model.EnstituKod = donemProjesiBasvuru.DonemProjesi.EnstituKod;
                    model.SRTalepTipID = 5;
                    model.TalepYapanID = donemProjesiBasvuru.DonemProjesi.KullaniciID;
                    model.Tarih = DateTime.Now.Date;

                }
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult RezervasyonAlPost(SrTalepleriKayitDto kModel, bool isSendMail = true)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Dönem Projesi Toplantı Bilgileri",
                MessageType = MsgTypeEnum.Warning
            };
            var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.First(p => p.UniqueID == kModel.BasvuruUniqueId);
            var srTalep = donemProjesiBasvuru.SRTalepleris.FirstOrDefault();
            var toplantiTalebiYap = RoleNames.DonemProjesiSinaviOlusturmaYetkisi.InRoleCurrent();
            if (!toplantiTalebiYap) kModel.YetkisizErisim = true;


            mmMessage.DialogID = donemProjesiBasvuru.DonemProjesiBasvuruID.ToString();
            kModel.SRTalepTipID = 5;

            kModel.EnstituKod = donemProjesiBasvuru.DonemProjesi.EnstituKod;
            if (kModel.YetkisizErisim)
            {
                mmMessage.Messages.Add("Dönem Projesi Toplantı Kayıt işlemi yapmaya yetkili değilsiniz.");
            }
            else
            {
                if (donemProjesiBasvuru.DonemProjesiJurileris.Any(a => a.DonemProjesiJuriOnayDurumID.HasValue))
                {
                    mmMessage.Messages.Add("Jüri üyelerinden herhangi biri değerlendirme yaptıktan sonra Toplantı bilgileri değiştirilemez.");
                }
            }
            kModel.SRTalepID = srTalep?.SRTalepID ?? 0;

            if (!kModel.IsOnline.HasValue)
            {

                mmMessage.Messages.Add("Toplantı Şekli seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsOnline" });
            }
            else
            {
                if (kModel.IsOnline == true && !DonemProjesiAyar.SinavOnlineYapilabilsin.GetAyarDp(kModel.EnstituKod).ToBoolean(false))
                {
                    mmMessage.Messages.Add("Dönem Projesi Sınavı Online olarak yapılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsOnline" });
                }
                else if (kModel.IsOnline == false && !DonemProjesiAyar.SinavYuzyuzeYapilabilsin.GetAyarDp(kModel.EnstituKod).ToBoolean(false))
                {
                    mmMessage.Messages.Add("Dönem Projesi Sınavı Yüz Yüze olarak yapılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsOnline" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "IsOnline" });
            }
            if (mmMessage.Messages.Count == 0)
            {

                if (kModel.Tarih == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Tarih Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                else if (!DonemProjesiBus.IsYetkiliKullanici() && kModel.Tarih < DateTime.Now)
                {
                    mmMessage.Messages.Add("Toplantı tarihi bilgisi günümüz tarihten küçük olamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                if (kModel.SalonAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add(kModel.IsOnline == true ? "Toplantı katılım linkini giriniz." : "Salon adı bilgisini giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SalonAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "SalonAdi" });

                if (mmMessage.Messages.Count == 0)
                {

                    if (donemProjesiBasvuru.DonemProjesiDurumID != DonemProjesiDurumEnum.JuriSinavOlusturmaSureci &&
                        donemProjesiBasvuru.DonemProjesiDurumID != DonemProjesiDurumEnum.SinavDegerlendirmeSureci)
                    {
                        mmMessage.Messages.Add("Jüri/Sınav oluşturma sürecinde bulunmayan bir başvuru için. Toplantı oluşturma/düzenleme işlemi yapılamaz.");
                    }

                    if (donemProjesiBasvuru.DonemProjesiDurumID == DonemProjesiDurumEnum.SinavDegerlendirmeSureci && donemProjesiBasvuru.DonemProjesiJurileris.Any(a=>a.DonemProjesiJuriOnayDurumID.HasValue))
                    {
                        mmMessage.Messages.Add("Değerlendirme süreci başlamış bir sınav bilgisi düzeltilemez. Düzeltme işlemi için enstitü ile görüşünüz.");
                    }
                }
                if (mmMessage.Messages.Count == 0)
                {
                    try
                    {
                        kModel.IslemTarihi = DateTime.Now;
                        kModel.IslemYapanID = UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        var tarih = kModel.Tarih;


                        kModel.Tarih = tarih.Date;
                        kModel.HaftaGunID = (int)tarih.DayOfWeek;
                        kModel.BasSaat = kModel.IsSalonSecilsin ? kModel.BasSaat : tarih.TimeOfDay;
                        kModel.BitSaat = kModel.IsSalonSecilsin ? kModel.BitSaat : kModel.BasSaat.Add(new TimeSpan(2, 0, 0));
                        kModel.SRDurumID = SrTalepDurumEnum.Onaylandı;
                        kModel.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                        kModel.IslemTarihi = kModel.IslemTarihi;
                        kModel.IslemYapanID = kModel.IslemYapanID;
                        kModel.IslemYapanIP = kModel.IslemYapanIP;
                        kModel.SRTaleplerJuris = donemProjesiBasvuru.DonemProjesiJurileris.Select(s => new SRTaleplerJuri
                        {
                            JuriTipAdi = s.IsTezDanismani ? "Proje Yürütücüsü" : "Jüri Üyesi",
                            AnabilimdaliProgramAdi = s.AnabilimdaliAdi,
                            UniversiteAdi = "Yıldız Teknik Üniversitesi",
                            UnvanAdi = s.UnvanAdi,
                            JuriAdi = s.UnvanAdi + " " + s.AdSoyad,
                            Telefon = "",
                            Email = s.EMail,
                            IslemTarihi = DateTime.Now,
                            IslemYapanID = UserIdentity.Current.Id,
                            IslemYapanIP = UserIdentity.Ip
                        }).ToList();

                        if (!isSendMail)
                        {
                            isSendMail = srTalep == null || (srTalep.IsOnline != kModel.IsOnline || srTalep.SalonAdi != kModel.SalonAdi || srTalep.Tarih != kModel.Tarih || srTalep.BasSaat != kModel.BasSaat);
                        }

                        bool isNewRecord = false;
                        if (srTalep == null)
                        {

                            isNewRecord = true;
                            srTalep = _entities.SRTalepleris.Add(new SRTalepleri
                            {
                                UniqueID = Guid.NewGuid(),
                                DonemProjesiBasvuruID = donemProjesiBasvuru.DonemProjesiBasvuruID,
                                IsOnline = kModel.IsOnline ?? false,
                                EnstituKod = kModel.EnstituKod,
                                MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID,
                                SRTalepTipID = kModel.SRTalepTipID,
                                TalepYapanID = kModel.TalepYapanID,
                                SRSalonID = null,
                                SalonAdi = kModel.SalonAdi,
                                Tarih = kModel.Tarih,
                                HaftaGunID = kModel.HaftaGunID,
                                BasSaat = kModel.BasSaat,
                                BitSaat = kModel.BitSaat,
                                Aciklama = kModel.Aciklama,
                                SRDurumID = kModel.SRDurumID,
                                IslemTarihi = kModel.IslemTarihi,
                                IslemYapanID = kModel.IslemYapanID,
                                IslemYapanIP = kModel.IslemYapanIP

                            });

                        }
                        else
                        {

                            srTalep.DonemProjesiBasvuruID = donemProjesiBasvuru.DonemProjesiBasvuruID;
                            srTalep.SRTalepTipID = kModel.SRTalepTipID;
                            srTalep.IsOnline = kModel.IsOnline ?? false;
                            srTalep.SalonAdi = kModel.SalonAdi;
                            srTalep.TalepYapanID = kModel.TalepYapanID;
                            srTalep.SRSalonID = null;
                            srTalep.Tarih = kModel.Tarih;
                            srTalep.HaftaGunID = kModel.HaftaGunID;
                            srTalep.BasSaat = kModel.BasSaat;
                            srTalep.BitSaat = kModel.BitSaat;
                            srTalep.DanismanAdi = kModel.DanismanAdi;
                            srTalep.EsDanismanAdi = kModel.EsDanismanAdi;
                            srTalep.SRDurumID = kModel.SRDurumID;
                            srTalep.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                            srTalep.IslemTarihi = kModel.IslemTarihi;
                            srTalep.IslemYapanID = kModel.IslemYapanID;
                            srTalep.IslemYapanIP = kModel.IslemYapanIP;
                        }
                        _entities.SaveChanges();
                        DonemProjesiBus.DonemProjesiDurumSet(donemProjesiBasvuru.DonemProjesiBasvuruID);
                        LogIslemleri.LogEkle("SRTalepleri", isNewRecord ? LogCrudType.Insert : LogCrudType.Update, srTalep.ToJson());

                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = MsgTypeEnum.Success;
                        mmMessage.Messages.Add("Dönem projesi toplantı bilgisi düzenlendi.");

                        #region SendMail

                        if (isSendMail)
                        {
                            var messageModel = DonemProjesiBus.SendMailSinavBilgisi(srTalep.SRTalepID);
                            mmMessage.Messages.Add(messageModel.IsSuccess
                                ? "<br/><i class='fa fa-envelope-o'></i> <span style=font-size:10pt;'>Toplantı bilgisi Jüri üyelerine ve öğrenciye mail olarak gönderildi.</span>"
                                : "<br/><i class='fa fa-lg fa-envelope-o' style='font-size:11pt;'></i> <span style=font-size:10pt;'>Toplantı bilgisi Jüri üyelerine ve öğrenciye mail olarak gönderilemedi!</span>");
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        mmMessage.IsSuccess = false;
                        mmMessage.MessageType = MsgTypeEnum.Error;
                        mmMessage.Messages.Add("İşlem yapılırken bir hata oluştu.");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Dönem projesi toplantı bilgisi oluşturulurken bir hata oluştu! Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Kritik);
                    }

                }

            }

            return mmMessage.ToJsonResult();
        }


        public ActionResult DegerlendirmeLinkView(Guid? uniqueId)
        {
            var model = _entities.DonemProjesiJurileris.First(p => p.UniqueID == uniqueId);
            return View(model);
        }

        public ActionResult DegerlendirmeLinkiGonder(Guid donemProjesiUniqueId, Guid donemProjesiBasvuruUniqueId, Guid? uniqueId, string eMail)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Dönem Projesi Sınavı Değerlendirme Linki Gönderme İşlemi"
            };
            var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.First(p => p.DonemProjesi.UniqueID == donemProjesiUniqueId && p.UniqueID == donemProjesiBasvuruUniqueId);
            var donemProjesi = donemProjesiBasvuru.DonemProjesi;
            var degerlendirmeDuzeltmeYetki = RoleNames.DonemProjesiSinavDegerlendirmeDuzeltme.InRoleCurrent();
            if (!degerlendirmeDuzeltmeYetki && donemProjesi.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Değerlendirme Linki Göndermek İçin Yetkili Değilsiniz.");
            }
            else if (donemProjesiBasvuru.DonemProjesiDurumID != DonemProjesiDurumEnum.SinavDegerlendirmeSureci)
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Değerlendirme sürecinde bulunmayan bir başvuru için. Jüri üyelerine değerlendirme linki gönderilemez.");
            }
            else if (!degerlendirmeDuzeltmeYetki && donemProjesiBasvuru.DonemProjesiJurileris.Count == donemProjesiBasvuru.DonemProjesiJurileris.Count(c => c.DonemProjesiJuriOnayDurumID.HasValue))
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Değerlendirme işlemi tüm Jüri üyeler tarafından tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
            }
            else if (eMail.IsNullOrWhiteSpace())
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("E-Posta Giriniz");
            }
            else if (eMail.ToIsValidEmail())
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("E-Posta Formatı Uygun Değildir.");
            }
            else
            {
                if (uniqueId.HasValue)
                {
                    var uye = donemProjesiBasvuru.DonemProjesiJurileris.FirstOrDefault(p => p.UniqueID == uniqueId);
                    if (uye == null) mMessage.Messages.Add("Değerlendirme Linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı Yenileyip Tekrar Deneyiniz.");
                    else
                    {
                        uye.EMail = eMail;
                        _entities.SaveChanges();

                    }
                }
                var messages = DonemProjesiBus.SendMailDegerlendirmeLink(donemProjesiBasvuru.DonemProjesiBasvuruID, uniqueId);
                if (messages.IsSuccess)
                {
                    donemProjesiBasvuru.IsOyBirligiOrCoklugu = null;
                    donemProjesiBasvuru.DonemProjesiJuriOnayDurumID = null;
                    donemProjesiBasvuru.DonemProjesiDurumID = DonemProjesiDurumEnum.SinavDegerlendirmeSureci;
                    _entities.SaveChanges();
                    DonemProjesiBus.DonemProjesiDurumSet(donemProjesiBasvuru.DonemProjesiBasvuruID);
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Değerlendirme Linki Komite Üyesine Gönderildi.");

                }
                else
                {
                    mMessage.Messages.AddRange(messages.Messages);

                }
            }
            var strView = mMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage) : "";
            return new { mMessage.IsSuccess, messageView = strView }.ToJsonResult();
        }
        [AllowAnonymous]
        public ActionResult DonemProjesiDegerlendir(Guid? uniqueId, int? donemProjesiJuriOnayDurumId, HttpPostedFileBase intihalDosya, int? tekKaynakOrani, int? toplamKaynakOrani, string aciklama)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Dönem Projesi Değerlendirme İşlemi"
            };
            var degerlendirmeDuzeltmeYetki = RoleNames.DonemProjesiSinavDegerlendirmeDuzeltme.InRoleCurrent();
            var isRefresh = false;
            if (!uniqueId.HasValue)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme için gerekli benzersiz anahtar bilgisi boş gelmektedir.</span>");
            }
            else
            {
                var donemProjesiJuri = _entities.DonemProjesiJurileris.FirstOrDefault(p => p.UniqueID == uniqueId);

                if (donemProjesiJuri == null)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme işlemi yapmanız için size tanınan benzersiz anahtar bilgisi değişti veya bulunamadı!</span>");
                }
                else
                {
                    var donemProjesiBasvuru = donemProjesiJuri.DonemProjesiBasvuru;
                    var toplanti = donemProjesiBasvuru.SRTalepleris.First();
                    var toplantiTarihi = toplanti.Tarih.Add(toplanti.BasSaat);
                    if (donemProjesiBasvuru.EYKYaGonderildi.HasValue)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>EYK'ya gönderim işlemşi yapılan başvurularda değerlendirme işlemi yapılamaz!</span>");
                    }
                    else if (donemProjesiBasvuru.IsDanismanOnay != true)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Proje Yürütücüsü onayından geçmeyen bir Dönem Projesi değerlendirmesi yapılamaz!</span>");
                    }
                    else if (!degerlendirmeDuzeltmeYetki && DateTime.Now < toplantiTarihi)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + toplantiTarihi.ToLongDateString() + " " +
                                              $"{toplanti.BasSaat:hh\\:mm}" + "'</b> dan önce yapılamaz!</span>");
                    }
                    else if (!degerlendirmeDuzeltmeYetki && donemProjesiJuri.DonemProjesiJuriOnayDurumID.HasValue)
                    {
                        mMessage.IsSuccess = true;
                        mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme işlemini daha önceden zaten yaptınız!</span>");
                    }
                    else
                    {

                        if (!degerlendirmeDuzeltmeYetki && !donemProjesiJuriOnayDurumId.HasValue)
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme Sonucu</span>");
                        }

                        if (donemProjesiJuri.IsTezDanismani)
                        {
                            if (donemProjesiJuriOnayDurumId == DonemProjesiJuriOnayDurumEnum.Basarili)
                            {
                                var enFazlaTekKaynakOraniKriter = DonemProjesiAyar.EnFazlaTekKaynakOrani
                                    .GetAyarDp(donemProjesiBasvuru.DonemProjesi.EnstituKod, "0").ToInt();
                                var enFazlaToplamKaynakOraniKriter = DonemProjesiAyar.EnFazlaToplamKaynakOrani
                                    .GetAyarDp(donemProjesiBasvuru.DonemProjesi.EnstituKod, "0").ToInt();

                                if (!tekKaynakOrani.HasValue)
                                    mMessage.Messages.Add(
                                        "<span style='color:maroon;'>Tek kaynak oranını giriniz.</span>");
                                else if (tekKaynakOrani < 0 || tekKaynakOrani > enFazlaTekKaynakOraniKriter)
                                    mMessage.Messages.Add("<span style='color:maroon;'>Tek kaynak oranı en fazla " +
                                                          enFazlaTekKaynakOraniKriter + "% olmalıdır.</span>");
                                if (!toplamKaynakOrani.HasValue)
                                    mMessage.Messages.Add(
                                        "<span style='color:maroon;'>Toplam kaynak oranını giriniz.</span>");
                                else if (toplamKaynakOrani < 0 || toplamKaynakOrani > enFazlaToplamKaynakOraniKriter)
                                    mMessage.Messages.Add("<span style='color:maroon;'>Toplam kaynak oranı en fazla " +
                                                          enFazlaToplamKaynakOraniKriter + "% olmalıdır.</span>");

                                if (donemProjesiBasvuru.IntihalRaporuDosyaYolu.IsNullOrWhiteSpace() &&
                                    intihalDosya == null)
                                {
                                    mMessage.Messages.Add(
                                        "<span style='color:maroon;'>Intihal Dosyası seçiniz.</span>");
                                }

                                if (intihalDosya != null)
                                {
                                    var extension = Path.GetExtension(intihalDosya.FileName).Replace(".", "");
                                    var gecerliUzantilar = new List<string> { "pdf" };
                                    if (!gecerliUzantilar.Contains(extension))
                                        mMessage.Messages.Add("<span style='color:maroon;'>" + intihalDosya.FileName +
                                                              " isimli doyasının pdf formatında olması gerekmektedir. Aksi halde dosya yükleme işlemi yapılamaz.</span>");
                                    else if (intihalDosya.ContentLength > (20 * 1024 * 1024))
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>" + intihalDosya.FileName +
                                                              " isimli doyasının boyutu en fazla 20 MB büyüklüğünde olmalıdır.</span>");
                                    }
                                }
                            }
                        }

                        if (donemProjesiJuriOnayDurumId.HasValue && donemProjesiJuriOnayDurumId != DonemProjesiJuriOnayDurumEnum.Basarili && aciklama.IsNullOrWhiteSpace())
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme Açıklaması</span>");
                        }
                        if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Dönem projesi değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");
                    }

                    if (!mMessage.Messages.Any())
                    {
                        if (donemProjesiJuriOnayDurumId.HasValue)
                        {

                            if (donemProjesiJuri.IsTezDanismani)
                            {
                                var jurilerdenKatilmadiSecenekVar = donemProjesiBasvuru.DonemProjesiJurileris.Any(a => !a.IsTezDanismani && a.DonemProjesiJuriOnayDurumID.HasValue && a.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.BasarisizKatilmadi);
                                var jurilerdenKatilmadiHaricSecenekVar = donemProjesiBasvuru.DonemProjesiJurileris.Any(a => !a.IsTezDanismani && a.DonemProjesiJuriOnayDurumID.HasValue && a.DonemProjesiJuriOnayDurumID != DonemProjesiJuriOnayDurumEnum.BasarisizKatilmadi);
                                var danismanKatilmadiSecti = donemProjesiJuriOnayDurumId == DonemProjesiJuriOnayDurumEnum.BasarisizKatilmadi;


                                // Proje Yürütücüsü katılmadı seçti ve diğer jüri üyelerinden birinin katılmadı haricinde seçimi varsa değerlendirmeye izin verilmesin. Proje Yürütücüsü katıldı seçtiğinde diğer jüri üyelerinin de katılmadı seçmesi şarttır.

                                if (!danismanKatilmadiSecti && jurilerdenKatilmadiSecenekVar)
                                    mMessage.Messages.Add("<span style='color:maroon;'>Öğrenci sınav sonucu Başarısız (Katılmadı) değerlendirmesinde bulunan jüri üyeleri bulunduğundan farklı bir değerlendirme sonucu girilemez. Başarısız (Katılmadı) değerlendirmesinden farklı bir sonuç girişi yapabilmek için jüri üyelerinin sınav değerlendirmeleri kaldırılmalıdır.</span>");
                                else if (danismanKatilmadiSecti && jurilerdenKatilmadiHaricSecenekVar)
                                    mMessage.Messages.Add("<span style='color:maroon;'>Öğrenci sınav sonucu Başarısız (Katılmadı) seçeneğini seçebilmeniz için jüri üyelerinde Başarısız (Katılmadı) değerlendirmesinden farklı bir değerlendirme bulunmamalıdır. Başarısız (Katılmadı) değerlendirilmesi yapılabilmesi için jüri üyelerinin sınav değerlendirmeleri kaldırılmalıdır.</span>");

                            }
                            else
                            {
                                var juriKatilmadiSecti = donemProjesiJuriOnayDurumId == DonemProjesiJuriOnayDurumEnum.BasarisizKatilmadi;
                                var danismanKatilmadiSecti = donemProjesiBasvuru.DonemProjesiJurileris.Any(a => a.IsTezDanismani && a.DonemProjesiJuriOnayDurumID.HasValue && a.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.BasarisizKatilmadi);

                                if (!danismanKatilmadiSecti && juriKatilmadiSecti)
                                    mMessage.Messages.Add("<span style='color:maroon;'>Sınav sonucu Proje Yürütücüsü tarafından Başarısız (Katılmadı) seçilmeden jüri üyeleri sınav değerlendirmesini Başarısız (Katılmadı) olarak yapamazlar.</span>");
                                else if (danismanKatilmadiSecti && !juriKatilmadiSecti)
                                    mMessage.Messages.Add("<span style='color:maroon;'>Sınav sonucu Proje Yürütücüsü tarafından Başarısız (Katılmadı) seçildiğinden jüri üyeleri sınav değerlendirmesini sadece Başarısız (Katılmadı) olarak yapmalıdırlar.</span>");

                            }
                        }
                    }

                    if (!mMessage.Messages.Any())
                    {
                        var sendMailLink = donemProjesiJuri.IsTezDanismani && donemProjesiJuriOnayDurumId.HasValue && !donemProjesiBasvuru.DonemProjesiJurileris.Any(a => !a.IsTezDanismani && a.IsLinkGonderildi.HasValue);
                        var isDegisiklikVar = donemProjesiJuri.DonemProjesiJuriOnayDurumID != donemProjesiJuriOnayDurumId || donemProjesiJuri.Aciklama != aciklama;
                        donemProjesiJuri.DonemProjesiJuriOnayDurumID = donemProjesiJuriOnayDurumId;
                        donemProjesiJuri.Aciklama = aciklama;
                        donemProjesiJuri.DegerlendirmeIslemTarihi = DateTime.Now;
                        donemProjesiJuri.DegerlendirmeIslemYapanIP = UserIdentity.Ip;
                        donemProjesiJuri.DegerlendirmeYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;

                        if (donemProjesiJuri.IsTezDanismani && donemProjesiJuriOnayDurumId == DonemProjesiJuriOnayDurumEnum.Basarili)
                        {
                            if (intihalDosya != null)
                            {
                                if (!donemProjesiBasvuru.IntihalRaporuDosyaYolu.IsNullOrWhiteSpace())
                                    FileHelper.Delete(donemProjesiBasvuru.IntihalRaporuDosyaYolu);
                                donemProjesiBasvuru.IntihalRaporuDosyaAdi = intihalDosya.FileName.GetFileName();
                                donemProjesiBasvuru.IntihalRaporuDosyaYolu = FileHelper.SaveDonemProjesiIntihalDosya(intihalDosya);
                            }
                            donemProjesiBasvuru.TekKaynakOrani = tekKaynakOrani;
                            donemProjesiBasvuru.ToplamKaynakOrani = toplamKaynakOrani;
                        }

                        donemProjesiJuri.IslemTarihi = DateTime.Now;
                        donemProjesiJuri.IslemYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;
                        donemProjesiJuri.IslemYapanIP = UserIdentity.Ip;
                        if (isDegisiklikVar)
                        {
                            donemProjesiBasvuru.UniqueID = Guid.NewGuid();
                            var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            while (_entities.TIBasvuruAraRapors.Any(a => a.FormKodu == formKodu))
                            {
                                formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            }
                            donemProjesiBasvuru.FormKodu = formKodu;
                        }
                        _entities.SaveChanges();
                        mMessage.Messages.Add("Değerlendirme işlemi yapıldı.");
                        LogIslemleri.LogEkle("DonemProjesiJurileri", LogCrudType.Update, donemProjesiJuri.ToJson());
                        mMessage.IsSuccess = true;
                        isRefresh = true;
                        if (sendMailLink)
                        {
                            var messages = DonemProjesiBus.SendMailDegerlendirmeLink(donemProjesiJuri.DonemProjesiBasvuruID, null);
                            if (donemProjesiJuri.IsTezDanismani || degerlendirmeDuzeltmeYetki)
                            {
                                if (messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme Linki Komite Üyelerine Gönderildi.");
                                }
                                else
                                {
                                    mMessage.Messages.AddRange(messages.Messages);
                                    mMessage.Messages.Add("Değerlendirmeniz geri alınmıştır, Lütfen tekrar değerlendirme yapınız.");
                                    mMessage.IsSuccess = false;
                                    isRefresh = true;
                                    donemProjesiJuri.DonemProjesiJuriOnayDurumID = null;
                                    donemProjesiJuri.Aciklama = null;
                                    donemProjesiJuri.DegerlendirmeIslemTarihi = null;
                                    donemProjesiJuri.DegerlendirmeIslemYapanIP = null;
                                    donemProjesiJuri.DegerlendirmeYapanID = null;
                                    _entities.SaveChanges();
                                }
                            }
                        }
                        else mMessage.Messages.Add("Değerlendirme işlemi tamamlandı.");

                        var donemProjesiJurileris = donemProjesiBasvuru.DonemProjesiJurileris;
                        var isDegerlendirmeTamam = donemProjesiJurileris.All(a => a.DonemProjesiJuriOnayDurumID.HasValue);

                        if (isDegerlendirmeTamam)
                        {
                            var qGruopDegerlendirme = donemProjesiJurileris.GroupBy(g => g.DonemProjesiJuriOnayDurumID).Select(s => new { s.Key, Count = s.Count() }).OrderByDescending(o => o.Count).ToList();
                            var firstGroupDegerlendirmeData = qGruopDegerlendirme.First();

                            donemProjesiBasvuru.DonemProjesiJuriOnayDurumID = firstGroupDegerlendirmeData.Key;
                            donemProjesiBasvuru.IsOyBirligiOrCoklugu = qGruopDegerlendirme.Count == 1;

                            var messages = DonemProjesiBus.SendMailSinavSonucBilgisi(donemProjesiJuri.DonemProjesiBasvuruID);
                            if (donemProjesiJuri.IsTezDanismani || degerlendirmeDuzeltmeYetki)
                            {
                                if (messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme Sonucu Proje Yürütücüsü ve Öğrenciye Gönderildi.");

                                }
                                else
                                {
                                    mMessage.Messages.AddRange(messages.Messages);
                                    mMessage.IsSuccess = false;
                                }
                            }
                            if (messages.IsSuccess)
                            {
                                donemProjesiBasvuru.DegerlendirmeSonucMailTarihi = DateTime.Now;
                            }
                        }
                        else
                        {
                            donemProjesiBasvuru.DonemProjesiJuriOnayDurumID = null;
                            donemProjesiBasvuru.IsOyBirligiOrCoklugu = null;

                        }
                        LogIslemleri.LogEkle("DonemProjesiJurileri", LogCrudType.Update, donemProjesiJuri.ToJson());
                        LogIslemleri.LogEkle("DonemProjesiBasvuru", LogCrudType.Update, donemProjesiBasvuru.ToJson());
                        _entities.SaveChanges();
                        DonemProjesiBus.DonemProjesiDurumSet(donemProjesiJuri.DonemProjesiBasvuru.DonemProjesiBasvuruID);

                    }
                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);
            return Json(new { mMessage.IsSuccess, Messages = strView, IsRefresh = isRefresh }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult EykDurumKayit(Guid donemProjesiBasvuruUniqueId, int onayTipId, bool? onaylandi, string aciklama, DateTime? onayTarihi)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Dönem Projesi " + (onayTipId == EykTipEnum.EykDaOnaylandi ? "EYK'da onay" : (onayTipId == EykTipEnum.EykYaHazirlandi ? "EYK'ya Hazırlık" : "EYK'ya gönderim")) + " işlemi",
                MessageType = MsgTypeEnum.Warning
            };

            var donemProjesiBasvuru = _entities.DonemProjesiBasvurus.FirstOrDefault(p => p.UniqueID == donemProjesiBasvuruUniqueId);

            if (onayTipId == EykTipEnum.EykYaGonderildi && !RoleNames.DonemProjesiEykYaGonder.InRoleCurrent())
            {
                mmMessage.Messages.Add("Dönem projesi başvurularını EYK'ya gönderme yetkisine sahip değilsiniz!");
            }
            else if (onayTipId == EykTipEnum.EykYaHazirlandi && !RoleNames.DonemProjesiEykYaHazirlandi.InRoleCurrent())
            {
                mmMessage.Messages.Add("Dönem projesi başvurularında EYK'ya hazırlık yetkisine sahip değilsiniz!");
            }
            else if (onayTipId == EykTipEnum.EykDaOnaylandi && !RoleNames.DonemProjesiEykDaOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Dönem projesi başvurularında EYK'da onay yetkisine sahip değilsiniz!");
            }
            else if (donemProjesiBasvuru == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen Dönem projesi sistemde bulunamadı!");
            }
            else
            {
                var donemProjesi = donemProjesiBasvuru.DonemProjesi;
                if (onayTipId == EykTipEnum.EykDaOnaylandi)
                {
                    if (donemProjesiBasvuru.EYKYaHazirlandi != true)
                    {
                        mmMessage.Messages.Add("EYK Ya hazırlanmayan Dönem projesi üzerinde EYK Onayı işlemi yapılamaz!");
                    }
                    else if (onaylandi == true && !onayTarihi.HasValue)
                    {
                        mmMessage.Messages.Add("EYK'da onay tarihini giriniz!");
                    }
                    else if (onaylandi == false && aciklama.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'da onaylanmama sebebini giriniz!");
                    }
                    else if (donemProjesi.DonemProjesiBasvurus.Any(a => a.DonemProjesiBasvuruID > donemProjesiBasvuru.DonemProjesiBasvuruID))
                    {
                        mmMessage.Messages.Add("Yeni bir Dönem projesi başvurusu varken önceki Dönem projesi eyk onay durumu değiştirilemez!");
                    }
                }
                else if (onayTipId == EykTipEnum.EykYaGonderildi)
                {
                    if (donemProjesiBasvuru.EYKYaHazirlandi.HasValue)
                    {
                        mmMessage.Messages.Add("EYK ya hazırlama işlemi yapılan bir Dönem projesi Eyk'ya gönderim işlemi gerçekleştirilemez!");
                    }
                    else if (onaylandi == false && aciklama.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'ya gönderiminin onaylanmama sebebini giriniz!");
                    }
                }


                if (!mmMessage.Messages.Any())
                {

                    var donemProjesiBasvuruAktifEykTipId = onayTipId == EykTipEnum.EykYaGonderildi ? donemProjesiBasvuru.EYKYaGonderildi : (onayTipId == EykTipEnum.EykYaHazirlandi
                        ? donemProjesiBasvuru.EYKYaHazirlandi
                        : donemProjesiBasvuru.EYKDaOnaylandi);

                    // eyk yada eykya gönderimi onay işlemi gördü yada yeni onay durumu onaylanmadı değil ise öğrencinin aktiflik durumunu kontrol et
                    if (donemProjesiBasvuruAktifEykTipId.HasValue || onaylandi != false)
                    {
                        var obsStudentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(donemProjesi.KullaniciID);

                        if (!obsStudentInfo.KayitVar)
                        {
                            mmMessage.Messages.Add(
                                "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                        }
                        else if (donemProjesi.OgrenciNo != obsStudentInfo.OgrenciInfo.OGR_NO)
                        {
                            mmMessage.Messages.Add(
                                "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir. Onay işlemi yapılamaz.");
                        }


                        if (!mmMessage.Messages.Any() && onayTipId == EykTipEnum.EykYaGonderildi && onaylandi == true && !_entities.DonemProjesiMuafOgrencilers.Any(a => a.KullaniciID == donemProjesi.KullaniciID && a.OgrenciNo == donemProjesi.OgrenciNo))
                        {

                            var kontrolEdilecekMinDersNotlari = DonemProjesiAyar.GetKontrolEdilecekMinDersNotlari(donemProjesi.EnstituKod);
                            if (kontrolEdilecekMinDersNotlari.Any())
                            {
                                var ogrenciDonemDersleris = obsStudentInfo.TumDonemDersNotlari;


                                var filtrelenenOgrenciDersler = ogrenciDonemDersleris.Where(p => kontrolEdilecekMinDersNotlari.Any(a => a.Value == p.DersKoduNum)).ToList();

                                foreach (var itemDersNot in kontrolEdilecekMinDersNotlari)
                                {
                                    if (!filtrelenenOgrenciDersler.Any(p => p.DersKoduNum == itemDersNot.Value && HarfNotuHelper.IsHarfNotuBuyukEsit(itemDersNot.Caption, p.DersNotu)))
                                    {
                                        mmMessage.Messages.Add("Öğrenci  " + itemDersNot.Value + " kodlu ders için en az '" + itemDersNot.Caption + "'" + " notunu alması gerekmekte.");
                                    }
                                }

                                if (mmMessage.Messages.Any())
                                {
                                    mmMessage.Messages.Insert(0, "Ders yükünü tamamlayamayan öğrenci mezuniyet için EYK ya gönderilemez.");
                                }
                            }
                            var basariliKrediSayisiKriter = DonemProjesiAyar.BasariliKrediSayisi.GetAyarDp(donemProjesi.EnstituKod).ToInt();
                            if (basariliKrediSayisiKriter.HasValue && obsStudentInfo.AktifDonemDers.ToplamKredi < basariliKrediSayisiKriter.Value)
                            {
                                mmMessage.Messages.Add("Öğrenci toplam kredi sayısı " + basariliKrediSayisiKriter.Value + " krediden büyük ya da eşit olmalıdır. Mevcut kredi: " + obsStudentInfo.AktifDonemDers.ToplamKredi);

                            }

                            if (mmMessage.Messages.Any())
                                mmMessage.Messages.Add("OBS den öğrencinin derslerini kontrol ediniz.");
                        }
                    }


                }
                if (!mmMessage.Messages.Any())
                {
                    var isDegisiklikVar = false;
                    if (onayTipId == EykTipEnum.EykYaGonderildi)
                    {
                        donemProjesiBasvuru.DonemProjesi.IsYeniBasvuruYapilabilir = onaylandi == false;

                        isDegisiklikVar = donemProjesiBasvuru.EYKYaGonderildi != onaylandi || aciklama != donemProjesiBasvuru.EYKYaGonderimDurumAciklamasi;
                        donemProjesiBasvuru.EYKYaGonderimDurumAciklamasi = onaylandi == false ? aciklama : "";
                        donemProjesiBasvuru.EYKYaGonderildi = onaylandi;
                        donemProjesiBasvuru.EYKYaGonderildiIslemTarihi = DateTime.Now;
                        donemProjesiBasvuru.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                        mmMessage.Messages.Add("Form EYK ya " + (onaylandi.HasValue ? (onaylandi.Value ? "'Gönderildi'" : "'Gönderilmedi'") : "Gönderilmesi bekleniyor") + " şeklinde güncellendi...");
                    }
                    else if (onayTipId == EykTipEnum.EykYaHazirlandi)
                    {

                        donemProjesiBasvuru.EYKYaHazirlandi = onaylandi;
                        donemProjesiBasvuru.EYKYaHazirlandiIslemTarihi = DateTime.Now;
                        donemProjesiBasvuru.EYKYaHazirlandiIslemYapanID = UserIdentity.Current.Id;
                        mmMessage.Messages.Add("Form EYK ya " + (onaylandi.HasValue ? (onaylandi.Value ? "'Hazırlandı'" : "'Hazırlanmadı'") : " Hazırlanması bekleniyor") + " şeklinde güncellendi...");
                    }
                    else if (onayTipId == EykTipEnum.EykDaOnaylandi)
                    {
                        donemProjesiBasvuru.DonemProjesi.IsYeniBasvuruYapilabilir = onaylandi.HasValue;
                        isDegisiklikVar = donemProjesiBasvuru.EYKDaOnaylandi != onaylandi || aciklama != donemProjesiBasvuru.EYKDaOnaylanmadiDurumAciklamasi || donemProjesiBasvuru.EYKTarihi != onayTarihi;
                        donemProjesiBasvuru.EYKDaOnaylandi = onaylandi;
                        if (onaylandi.HasValue) { donemProjesiBasvuru.EYKTarihi = onayTarihi; }
                        donemProjesiBasvuru.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                        donemProjesiBasvuru.EYKDaOnaylandiIslemTarihi = DateTime.Now;
                        donemProjesiBasvuru.EYKDaOnaylanmadiDurumAciklamasi = onaylandi == false ? aciklama : "";

                        mmMessage.Messages.Add("Form EYK da " + (onaylandi.HasValue ? (onaylandi.Value ? "'Onaylandı'" : "'Onaylanmadı'") : "İşlem bekliyor") + " şeklinde güncellendi...");
                        var ogrenci = donemProjesi.Kullanicilar;
                        if (onaylandi == true)
                        {
                            if (ogrenci.YtuOgrencisi && ogrenci.ProgramKod == donemProjesi.ProgramKod && ogrenci.OgrenimTipKod == donemProjesi.OgrenimTipKod)
                            {
                                ogrenci.OgrenimDurumID = OgrenimDurumEnum.Mezun;
                                ogrenci.IslemTarihi = DateTime.Now;
                                ogrenci.IslemYapanID = UserIdentity.Current.Id;
                                ogrenci.IslemYapanIP = UserIdentity.Ip;
                            }
                        }
                        else
                        {
                            ogrenci.OgrenimDurumID = OgrenimDurumEnum.HalenOğrenci;
                            ogrenci.IslemTarihi = DateTime.Now;
                            ogrenci.IslemYapanID = UserIdentity.Current.Id;
                            ogrenci.IslemYapanIP = UserIdentity.Ip;
                        }
                    }
                    _entities.SaveChanges();
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    mmMessage.IsSuccess = true;

                    LogIslemleri.LogEkle("DonemProjesiBasvuru", LogCrudType.Update, donemProjesiBasvuru.ToJson());
                    DonemProjesiBus.DonemProjesiDurumSet(donemProjesiBasvuru.DonemProjesiBasvuruID);
                    if (onaylandi == false && isDegisiklikVar)
                    {
                        var eykDaOnayOrGonderim = onayTipId == EykTipEnum.EykDaOnaylandi;
                        DonemProjesiBus.SendMailEykOnaylanmadi(donemProjesiBasvuru.DonemProjesiBasvuruID, eykDaOnayOrGonderim);
                    }

                }
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView,
                mmMessage
            }.ToJsonResult();
        }


    }
}