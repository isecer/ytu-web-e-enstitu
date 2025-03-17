using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TdoBasvuruController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();

        // GET: TDOBasvuru
        public ActionResult Index(string ekd, int? tdoBasvuruId, int? kullaniciId)
        {

            return Index(new FmTdoBasvuruDto() { TDOBasvuruID = tdoBasvuruId, KullaniciID = kullaniciId, PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTdoBasvuruDto model, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            #region bilgiModel
            var bbModel = new IndexPageInfoDto
            {
                SistemBasvuruyaAcik = TdoAyar.BasvurusuAcikmi.GetAyarTdo(enstituKod, "false").ToBoolean(false)
            };

            var gelenBasvuruDuzeltmeYetki = RoleNames.TdoGelenBasvuruKayit.InRoleCurrent();

            if (gelenBasvuruDuzeltmeYetki)
            {
                if (model.TDOBasvuruID.HasValue || model.KullaniciID.HasValue)
                {
                    if (!model.KullaniciID.HasValue) model.KullaniciID = _entities.TDOBasvurus.Where(p => p.TDOBasvuruID == model.TDOBasvuruID).Select(s => s.KullaniciID).FirstOrDefault();
                }
                else model.KullaniciID = UserIdentity.Current.Id;
            }
            else
            {
                model.KullaniciID = UserIdentity.Current.Id;
            }
            var kullKayitB = KullanicilarBus.OgrenciBilgisiGuncelleObs(model.KullaniciID.Value);
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == model.KullaniciID);

            if (kul.YtuOgrencisi)
            {
                var otb = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);

                bbModel.OgrenimDurumAdi = kul.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                bbModel.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                bbModel.ProgramAdi = kul.Programlar.ProgramAdi;
                bbModel.OgrenciNo = kul.OgrenciNo;

                if (kullKayitB.KayitVar == false)
                {
                    bbModel.KullaniciTipYetki = false;
                    bbModel.KullaniciTipYetkiYokMsj = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Hesap bilgilerinizde bulundna YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";
                }
                else
                {

                    if ((kul.OgrenimTipKod.IsDoktora() || kul.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans) && kul.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci)
                    {
                        bbModel.KullaniciTipYetki = true;
                        var donemBilgi = _entities.Donemlers.FirstOrDefault(p => p.DonemID == kul.KayitDonemID.Value);
                        if (donemBilgi != null)
                        {
                            bbModel.KayitDonemi = kul.KayitYilBaslangic + "/" + (kul.KayitYilBaslangic + 1);
                        }
                        if (kul.KayitTarihi.HasValue) bbModel.KayitDonemi += " " + kul.KayitTarihi.ToFormatDate();
                        model.AktifOgrenimIcinBasvuruVar = _entities.TDOBasvurus.Any(a => a.KullaniciID == kul.KullaniciID && a.OgrenimTipKod == kul.OgrenimTipKod && a.ProgramKod == kul.ProgramKod && a.OgrenciNo == kul.OgrenciNo);
                    }
                    else
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "Tez danışmanı öneri başvurusu yapılabilmesi için Doktora veya Tezli Yl seviyesinde öğrenim görmeniz gerekmektedir.";

                    }
                }
                if (bbModel.KullaniciTipYetki)
                {
                    if (kul.Programlar.AnabilimDallari.EnstituKod != enstituKod)
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "Kayıtlı olduğunuz program ve başvuru yapmaya çalıştığınız enstitü birbiri ile uyuşmamaktadır. Doğru enstitü sayfasından başvuru yaptığınızdan emin olunuz.";
                    }
                }
            }
            else
            {
                bbModel.KullaniciTipYetki = false;
                bbModel.KullaniciTipYetkiYokMsj = "Tez İzleme başvurusu yapabilmek için hesap bilgilerinizde YTÜ Lisansüstü öğrencisi olduğunuza dair bilgilerin eksiksiz olarak doldurulması gerekmektedir. Profilinizi güncellemek ve başvurunuzu yeniden denemek için sağ üst köşedeki 'Hesap bilgilerini düzenle' butonuna tıklayarak 'YTÜ Lisansüstü Öğrencisi Misiniz?' sorusunu cevaplayınız.";
            }



            bbModel.Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
            bbModel.Kullanici = kul;

            #endregion 
            var q = from s in _entities.TDOBasvurus
                    join en in _entities.Enstitulers on s.EnstituKod equals en.EnstituKod
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, en.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on pr.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join ktip in _entities.KullaniciTipleris on k.KullaniciTipID equals ktip.KullaniciTipID
                    join ard in _entities.TDOBasvuruDanismen on s.AktifTDOBasvuruDanismanID equals ard.TDOBasvuruDanismanID into defard
                    from ard in defard.DefaultIfEmpty()
                    let ardEs = s.TDOBasvuruDanismen.SelectMany(sm => sm.TDOBasvuruEsDanismen).OrderByDescending(oe => oe.TDOBasvuruEsDanismanID).FirstOrDefault(p => p.TDOBasvuruDanismanID == ard.TDOBasvuruDanismanID)
                    where s.EnstituKod == enstituKod && s.KullaniciID == model.KullaniciID
                    select new FrTdoBasvuruDto
                    {
                        TDOBasvuruID = s.TDOBasvuruID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        KullaniciID = s.KullaniciID,
                        UserKey = k.UserKey,
                        AdSoyad = k.Ad + " " + k.Soyad,
                        TcKimlikNo = k.TcKimlikNo,
                        OgrenciNo = s.OgrenciNo,
                        ResimAdi = k.ResimAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifTDOBasvuruDanismanID = s.AktifTDOBasvuruDanismanID,
                        TDOBasvuruDanisman = ard,
                        AktifDonemID = ard == null ? null : (ard.DonemBaslangicYil + "" + ard.DonemID),
                        AktifDonemAdi = ard == null ? "Danışman Önerisi Yok" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),
                        VarolanTezDanismanID = ard != null ? ard.VarolanTezDanismanID : null,
                        VarolanDanismanOnayladi = ard != null ? ard.VarolanDanismanOnayladi : null,

                        DanismanOnayladi = ard != null ? ard.DanismanOnayladi : null,
                        EYKYaHazirlandi = ard != null ? ard.EYKYaHazirlandi : null,
                        EYKYaGonderildi = ard != null ? ard.EYKYaGonderildi : null,
                        EYKDaOnaylandi = ard != null ? ard.EYKDaOnaylandi : null,
                        EsDanismanOnerisiVar = ardEs != null,
                        Es_EYKYaGonderildi = ardEs != null ? ardEs.EYKYaGonderildi : null,
                        Es_EYKYaHazirlandi = ardEs != null ? ardEs.EYKYaHazirlandi : null,
                        Es_EYKDaOnaylandi = ardEs != null ? ardEs.EYKDaOnaylandi : null,


                    };

            if (model.TDOBasvuruID.HasValue) q = q.Where(p => p.TDOBasvuruID == model.TDOBasvuruID.Value);
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            model.TdoBasvuruDtos = qdata;
            ViewBag.IndexModel = indexModel;
            ViewBag.bModel = bbModel;
            return View(model);
        }

        public ActionResult BasvuruYap(int? tdoBasvuruId, int? kullaniciId = null, string ekd = "")
        {
            var model = new KmTDOBasvuru();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);


            if (tdoBasvuruId.HasValue || kullaniciId.HasValue)
            {
                if (kullaniciId.HasValue)
                    if (RoleNames.TdoGelenBasvuruKayit.InRoleCurrent() == false)
                        kullaniciId = UserIdentity.Current.Id;
                if (tdoBasvuruId.HasValue)
                {
                    var basvuru = _entities.TDOBasvurus.FirstOrDefault(p => p.TDOBasvuruID == tdoBasvuruId.Value);
                    if (kullaniciId.HasValue == false) kullaniciId = basvuru.KullaniciID;
                }
            }
            else
            {
                kullaniciId = UserIdentity.Current.Id;
            }

            var kul = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kullaniciId);
            model.AdSoyad = kul.Ad + " " + kul.Soyad;
            var mmMessage = TdoBus.GetAktifTezDanismanOneriSurecKontrol(enstituKod, kullaniciId, tdoBasvuruId);


            if (mmMessage.IsSuccess)
            {
                model.KayitTarihi = kul.KayitTarihi;
                if (tdoBasvuruId.HasValue)
                {
                    var basvuru = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId);
                    model.EnstituKod = basvuru.EnstituKod;
                    model.TDOBasvuruID = basvuru.TDOBasvuruID;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = basvuru.KullaniciID;
                    model.OgrenimTipKod = basvuru.OgrenimTipKod;



                }
                else
                {
                    model.EnstituKod = enstituKod;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = kullaniciId.Value;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.OgrenimTipKod = kul.OgrenimTipKod.Value;

                }
                if (kul.OgrenimTipKod.HasValue)
                {
                    model.OgrenimTipAdi = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;
                    var progLng = kul.Programlar;
                    model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = progLng.ProgramAdi;
                }

                ViewBag._MmMessage = mmMessage;
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
                return RedirectToAction("Index", new { KullaniciID = kullaniciId });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BasvuruYap(KmTDOBasvuru kModel, string ekd)
        {
            if (RoleNames.TdoGelenBasvuruKayit.InRoleCurrent() == false) { kModel.KullaniciID = UserIdentity.Current.Id; }
            var mmMessage = TdoBus.GetAktifTezDanismanOneriSurecKontrol(kModel.EnstituKod, kModel.KullaniciID, kModel.TDOBasvuruID.ToNullIntZero());

            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
            kModel.AdSoyad = kul.Ad + " " + kul.Soyad;
            if (mmMessage.Messages.Count == 0)
            {
                kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;

                kModel.KayitOgretimYiliBaslangic = kul.KayitYilBaslangic;
                kModel.KayitOgretimYiliDonemID = kul.KayitDonemID;
                kModel.KayitTarihi = kul.KayitTarihi;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;
                kModel.OgrenciNo = kul.OgrenciNo;
                kModel.ProgramKod = kul.ProgramKod;
                TDOBasvuru data;
                var isNewRecord = false;
                if (kModel.TDOBasvuruID <= 0)
                {
                    isNewRecord = true;
                    kModel.BasvuruTarihi = DateTime.Now;

                    data = _entities.TDOBasvurus.Add(new TDOBasvuru
                    {
                        EnstituKod = kModel.EnstituKod,
                        UniqueID = Guid.NewGuid(),
                        BasvuruTarihi = kModel.BasvuruTarihi,
                        KullaniciID = kModel.KullaniciID,
                        OgrenciNo = kModel.OgrenciNo,
                        OgrenimTipKod = kModel.OgrenimTipKod,
                        ProgramKod = kModel.ProgramKod,
                        KayitOgretimYiliBaslangic = kModel.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = kModel.KayitOgretimYiliDonemID,
                        KayitTarihi = kModel.KayitTarihi,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip

                    });
                    _entities.SaveChanges();
                    TdoBus.ObsDanismanBasvuruBilgiEslestir(data.KullaniciID, data.TDOBasvuruID);

                }
                else
                {

                    data = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == kModel.TDOBasvuruID);
                    data.EnstituKod = kModel.EnstituKod;
                    data.BasvuruTarihi = kModel.BasvuruTarihi;
                    data.KullaniciID = kModel.KullaniciID;
                    data.OgrenciNo = kModel.OgrenciNo;
                    data.OgrenimTipKod = kModel.OgrenimTipKod;
                    data.ProgramKod = kModel.ProgramKod;
                    data.KayitOgretimYiliBaslangic = kModel.KayitOgretimYiliBaslangic;
                    data.KayitOgretimYiliDonemID = kModel.KayitOgretimYiliDonemID;
                    data.KayitTarihi = kModel.KayitTarihi;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    _entities.SaveChanges();


                }
                LogIslemleri.LogEkle("TdoBasvuru", isNewRecord ? LogCrudType.Insert : LogCrudType.Update, data.ToJson());

                return RedirectToAction("Index", new { data.TDOBasvuruID });
            }

            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());

            ViewBag._MmMessage = mmMessage;
            return View(kModel);
        }

        public ActionResult GetProgramlar(int tdAnabilimDaliId)
        {
            var bolm = ProgramlarBus.CmbGetAktifProgramlar(true, tdAnabilimDaliId);
            return bolm.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }
        public ActionResult GetSinavTip(int tdoBasvuruId, int sinavTipId)
        {
            var sinav = _entities.SinavTipleris.First(p => p.SinavTipID == sinavTipId);

            var notlar = new List<CmbDoubleDto>();
            if (sinav.OzelNot) notlar = SinavTipleriBus.CmbGetSinavTipOzelNot(sinavTipId, true);
            return new { sinav.OzelNot, Notlar = notlar }.ToJsonResult();
        }
        public ActionResult GetTdoYeniDanismanFormu(int tdoBasvuruId, int? tdoBasvuruDanismanId, bool? isCopy, int? tdoDanismanTalepTipId)
        {

            var model = new KmTdoBasvuruDanisman() { TDOBasvuruID = tdoBasvuruId, IsCopy = isCopy, TDODanismanTalepTipID = tdoDanismanTalepTipId ?? TdoDanismanTalepTipEnum.TezDanismaniOnerisi };
            var mMessage = new MmMessage()
            {
                Title = "Tez Danışmanı Öneri İşlemi"
            };
            string view = "";
            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.FirstOrDefault(p => p.TDOBasvuruID == tdoBasvuruId && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            if (tdoBas == null) return null;
            var ogrenci = tdoBas.Kullanicilar;
            model.OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + "-" + tdoBas.OgrenciNo;
            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            if (tdoBasvuruDanismanId > 0 && isCopy != true && tdoBas.TDOBasvuruDanismen.Any(a => a.TDOBasvuruDanismanID == tdoBasvuruDanismanId && a.DanismanOnayladi == true))
            {
                mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
            }

            if (!mMessage.Messages.Any() && !(tdoBasvuruDanismanId > 0))
            {
                var msgs = TijBus.IsAktifDevamEdenTijMessage(tdoBas.KullaniciID, tdoBas.OgrenciNo);
                mMessage.Messages.AddRange(msgs);

            }

            if (!mMessage.Messages.Any())
            {
                if (tdoBasvuruDanismanId > 0)
                {
                    var tdoBd = tdoBas.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    model.TDOBasvuruDanismanID = tdoBasvuruDanismanId.Value;
                    model.TDOBasvuruID = tdoBd.TDOBasvuruID;
                    model.UniqueID = tdoBd.UniqueID;
                    model.FormKodu = tdoBd.FormKodu;
                    model.OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + "-" + tdoBas.OgrenciNo;
                    model.YeniTezBaslikTr = tdoBd.YeniTezBaslikTr;
                    model.YeniTezBaslikEn = tdoBd.YeniTezBaslikEn;
                    model.IsTezDiliTr = tdoBd.IsTezDiliTr;
                    model.TezBaslikTr = tdoBd.TezBaslikTr;
                    model.TezBaslikEn = tdoBd.TezBaslikEn;
                    model.SinavTipID = tdoBd.SinavTipID;
                    model.SinavAdi = tdoBd.SinavAdi;
                    model.SinavPuani = tdoBd.SinavPuani;
                    model.SinavYili = tdoBd.SinavYili;
                    model.VarolanTezDanismanID = tdoBd.VarolanTezDanismanID;
                    model.TezDanismanID = tdoBd.TezDanismanID;
                    model.TDAdSoyad = tdoBd.TDAdSoyad;
                    model.TDUnvanAdi = tdoBd.TDUnvanAdi;
                    model.TDAnabilimDaliID = tdoBd.TDAnabilimDaliID;
                    model.TDAnabilimDaliAdi = tdoBd.TDAnabilimDaliAdi;
                    model.TDProgramKod = tdoBd.TDProgramKod;
                    model.TDProgramAdi = tdoBd.TDProgramAdi;
                    model.TDOgrenciSayisiDR = tdoBd.TDOgrenciSayisiDR;
                    model.TDOgrenciSayisiYL = tdoBd.TDOgrenciSayisiYL;
                    model.TDTezSayisiDR = tdoBd.TDTezSayisiDR;
                    model.TDTezSayisiYL = tdoBd.TDTezSayisiYL;
                    if (isCopy == true)
                    {
                        model.TDOBasvuruDanismanID = 0;
                        if (model.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezBasligiDegisikligi)
                        {
                            model.YeniTezBaslikTr = null;
                            model.YeniTezBaslikEn = null;
                            model.VarolanTezDanismanID = null;
                        }
                        else if (model.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi)
                        {
                            model.VarolanTezDanismanID = tdoBd.TezDanismanID;
                            model.TDAnabilimDaliID = null;
                            model.TDProgramKod = null;
                            model.TezDanismanID = 0;
                        }
                        else if (model.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi)
                        {
                            model.YeniTezBaslikTr = null;
                            model.YeniTezBaslikEn = null;
                            model.VarolanTezDanismanID = tdoBd.TezDanismanID;
                            model.TezDanismanID = 0;
                            model.TDAnabilimDaliID = null;
                            model.TDProgramKod = null;
                        }
                    }

                }
                model.SListTDoDanismanTalepTip = new SelectList(TdoBus.CmbTdoDanismanTalepTip(model.TDODanismanTalepTipID > TdoDanismanTalepTipEnum.TezDanismaniOnerisi, false), "Value", "Caption", model.TDODanismanTalepTipID);
                model.SListSinav = new SelectList(SinavTipleriBus.CmbGetAktifSinavlar(tdoBas.EnstituKod, SinavTipGrupEnum.DilSinavlari, true), "Value", "Caption", model.SinavTipID);
                model.SListTdAnabilimDali = new SelectList(AnabilimDallariBus.CmbGetAktifAnabilimDallari(tdoBas.EnstituKod, true), "Value", "Caption", model.TDAnabilimDaliID);
                model.SListTdProgram = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(true, model.TDAnabilimDaliID), "Value", "Caption", model.TDProgramKod);

                if (model.SinavTipID.HasValue)
                {
                    var sinav = _entities.SinavTipleris.First(p => p.SinavTipID == model.SinavTipID);
                    if (sinav.OzelNot) model.SListSinavNot = new SelectList(SinavTipleriBus.CmbGetSinavTipOzelNot(model.SinavTipID.Value, true), "Value", "Caption", model.SinavPuani);


                }
                mMessage.MessageType = MsgTypeEnum.Information;
                mMessage.IsSuccess = true;
                view = ViewRenderHelper.RenderPartialView("TdoBasvuru", "TdoYeniDanismanFormu", model);


            }


            if (mMessage.MessageType != MsgTypeEnum.Information) mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = view,
                Messages = strView
            }.ToJsonResult();

        }

        public ActionResult TdoYeniDanismanFormuPost(TDOBasvuruDanisman kModel, bool? isTezDiliTr)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı Öneri İşlemi"
            };
            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == kModel.TDOBasvuruID && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));

            KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBas.KullaniciID);


            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            if (kModel.TDOBasvuruDanismanID > 0)
            {
                if (tdoBas.TDOBasvuruDanisman.VarolanDanismanOnayladi == true)
                {
                    mMessage.Messages.Add(
                        "Varolan Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                }
                else if (tdoBas.TDOBasvuruDanisman.DanismanOnayladi == true)
                {
                    mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                }
            }

            if (!mMessage.Messages.Any())
            {
                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(tdoBas.OgrenciNo);
                if (!ogrenciBilgi.KayitVar)
                {
                    mMessage.Messages.Add(tdoBas.OgrenciNo + " öğrenci numaranıza ait OBS isteminde aktif bir öğrenim bilgisine rastlanmadı. " + ogrenciBilgi.HataMsj);
                }
            }
            if (!mMessage.Messages.Any())
            {

                if (!isTezDiliTr.HasValue)
                {
                    mMessage.Messages.Add("Tez dilini seçiniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (isTezDiliTr.HasValue ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "IsTezDiliTr" });
                var tezBaslikMaxLength = tdoBas.Enstituler.TezBaslikMaxLength;
                if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Tez başlığını türkçe olarak giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezBaslikTr" });
                }
                else if (tezBaslikMaxLength.HasValue && kModel.TezBaslikTr.Length > tezBaslikMaxLength)
                {
                    mMessage.Messages.Add($"Tez başlığı türkçe bilgisi için '{tezBaslikMaxLength}' karakter ile sınırlandırılmış karakter uzunluğunu aştınız!");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezBaslikTr" });
                }
                else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezBaslikTr" });
                if (tezBaslikMaxLength.HasValue && kModel.TezBaslikEn.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Tez başlığını türkçe olarak giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezBaslikEn" });
                }
                else if (tezBaslikMaxLength.HasValue && kModel.TezBaslikEn.Length > tezBaslikMaxLength)
                {
                    mMessage.Messages.Add($"Tez başlığı ingilizce bilgisi için '{tezBaslikMaxLength}' karakter ile sınırlandırılmış karakter uzunluğunu aştınız!");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezBaslikEn" });
                }
                else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezBaslikEn" });

                if (isTezDiliTr == false)
                {
                    if (!kModel.SinavTipID.HasValue)
                        mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav bilgisini seçiniz.");
                    if (!kModel.SinavYili.HasValue || kModel.SinavYili <= 0)
                        mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav yılı bilginizi giriniz.");
                    if (kModel.SinavPuani.IsNullOrWhiteSpace() || !kModel.SinavPuani.ToDouble().HasValue)
                    {
                        mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav puanı bilgisini giriniz.");
                        mMessage.MessagesDialog.Add(new MrMessage
                        { MessageType = MsgTypeEnum.Warning, PropertyName = "SinavPuani" });
                    }
                    else
                    {

                        if (kModel.SinavTipID.HasValue)
                        {
                            var sinavTipi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID);
                            kModel.SinavAdi = sinavTipi.SinavAdi;
                            var sinavPuani = kModel.SinavPuani.ToDouble(0);
                            var sinavTipKriter = sinavTipi.SinavTipleriOTNotAraliklaris.FirstOrDefault(p =>
                               p.OgrenimTipKod == tdoBas.OgrenimTipKod && p.Ingilizce == tdoBas.Programlar.Ingilizce);
                            if (sinavTipKriter != null)
                            {
                                var sinavPuaniUygun =
                                    (sinavPuani >= sinavTipKriter.Min && sinavPuani <= sinavTipKriter.Max);
                                if (!sinavPuaniUygun)
                                {
                                    mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav puanı en az " +
                                                          sinavTipKriter.Min + " en fazla " + sinavTipKriter.Max +
                                                          " olmalıdır.");
                                }

                                mMessage.MessagesDialog.Add(new MrMessage
                                {
                                    MessageType = sinavPuaniUygun ? MsgTypeEnum.Success : MsgTypeEnum.Warning,
                                    PropertyName = "SinavPuani"
                                });
                            }
                            else
                            {
                                mMessage.Messages.Add(
                                    "Öğrenci Yabancı dil sınav yeterlilik puan kriterleri tanımlı değil. Bu bilgiyi Enstitüye iletiniz.");
                            }
                        }

                    }

                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = (kModel.SinavTipID.HasValue ? MsgTypeEnum.Success : MsgTypeEnum.Warning),
                        PropertyName = "SinavTipID"
                    });
                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = (kModel.SinavYili.HasValue && kModel.SinavYili > 0
                            ? MsgTypeEnum.Success
                            : MsgTypeEnum.Warning),
                        PropertyName = "SinavYili"
                    });
                }
            }

            if (kModel.TezDanismanID <= 0)
            {
                mMessage.Messages.Add("Tez danışmanınızı seçiniz.");
            }
            if (kModel.TDAnabilimDaliID <= 0)
            {
                mMessage.Messages.Add("Tez Danışmanı Anabilim dalı bilgisini seçiniz.");
            }
            mMessage.MessagesDialog.Add(new MrMessage
            {
                MessageType = (kModel.TDAnabilimDaliID > 0 ? MsgTypeEnum.Success : MsgTypeEnum.Warning),
                PropertyName = "TDAnabilimDaliID"
            });
            if (kModel.TDProgramKod.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("Tez Danışmanı program bilgisini seçiniz.");
            }
            mMessage.MessagesDialog.Add(new MrMessage
            {
                MessageType = (!kModel.TDProgramKod.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning),
                PropertyName = "TDProgramKod"
            });


            if (!mMessage.Messages.Any() && kModel.TDOBasvuruDanismanID <= 0)
            {
                var msgs = TijBus.IsAktifDevamEdenTijMessage(tdoBas.KullaniciID, tdoBas.OgrenciNo);
                mMessage.Messages.AddRange(msgs);
            }



            if (!mMessage.Messages.Any())
            {
                kModel.TDODanismanTalepTipID = TdoDanismanTalepTipEnum.TezDanismaniOnerisi;
                kModel.IsTezDiliTr = isTezDiliTr.Value;

                if (isTezDiliTr == true)
                {
                    kModel.SinavTipID = null;
                    kModel.SinavAdi = null;
                    kModel.SinavPuani = null;
                    kModel.SinavYili = null;
                }

                kModel.BasvuruTarihi = DateTime.Now;
                var donemBilgi = kModel.BasvuruTarihi.ToAkademikDonemBilgi();
                kModel.DonemBaslangicYil = donemBilgi.BaslangicYil;
                kModel.DonemID = donemBilgi.DonemId;
                var danisman = _entities.Kullanicilars.First(f => f.KullaniciID == kModel.TezDanismanID);
                kModel.TDAnabilimDaliID = kModel.TDAnabilimDaliID;
                var program = _entities.Programlars.First(p => p.ProgramKod == kModel.TDProgramKod);
                kModel.TDAnabilimDaliAdi = program.AnabilimDallari.AnabilimDaliAdi;
                kModel.TDProgramAdi = program.ProgramAdi;
                kModel.TDAdSoyad = danisman.Ad + " " + danisman.Soyad;
                kModel.TDUnvanAdi = danisman.Unvanlar.UnvanAdi;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                while (_entities.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
                {
                    formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                }
                kModel.UniqueID = Guid.NewGuid();
                kModel.FormKodu = formKodu;
                TDOBasvuruDanisman tdoBasvuruDanis;

                if (isTezDiliTr == false)
                {
                    if (kModel.SinavTipID.HasValue) kModel.SinavAdi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID).SinavAdi;

                }
                if (kModel.TDOBasvuruDanismanID > 0)
                {
                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);
                    tdoBasvuruDanis.DanismanOnayladi = null;
                    tdoBasvuruDanis.DanismanOnayTarihi = null;
                    tdoBasvuruDanis.DanismanOnaylanmadiAciklama = null;
                    tdoBasvuruDanis.BasvuruTarihi = kModel.BasvuruTarihi;
                    tdoBasvuruDanis.DonemBaslangicYil = kModel.DonemBaslangicYil;
                    tdoBasvuruDanis.DonemID = kModel.DonemID;
                    tdoBasvuruDanis.FormKodu = kModel.FormKodu;
                    tdoBasvuruDanis.UniqueID = kModel.UniqueID;
                    tdoBasvuruDanis.IsTezDiliTr = kModel.IsTezDiliTr;
                    tdoBasvuruDanis.TezBaslikTr = kModel.TezBaslikTr;
                    tdoBasvuruDanis.TezBaslikEn = kModel.TezBaslikEn;
                    tdoBasvuruDanis.SinavTipID = kModel.SinavTipID;
                    tdoBasvuruDanis.SinavAdi = kModel.SinavAdi;
                    tdoBasvuruDanis.SinavPuani = kModel.SinavPuani;
                    tdoBasvuruDanis.SinavYili = kModel.SinavYili;
                    tdoBasvuruDanis.TezDanismanID = kModel.TezDanismanID;
                    tdoBasvuruDanis.TDAdSoyad = kModel.TDAdSoyad;
                    tdoBasvuruDanis.TDUnvanAdi = kModel.TDUnvanAdi;
                    tdoBasvuruDanis.TDAnabilimDaliID = kModel.TDAnabilimDaliID;
                    tdoBasvuruDanis.TDAnabilimDaliAdi = kModel.TDAnabilimDaliAdi;
                    tdoBasvuruDanis.TDProgramKod = kModel.TDProgramKod;
                    tdoBasvuruDanis.TDProgramAdi = kModel.TDProgramAdi;
                    tdoBasvuruDanis.IslemTarihi = kModel.IslemTarihi;
                    tdoBasvuruDanis.IslemYapanID = kModel.IslemYapanID;
                    tdoBasvuruDanis.IslemYapanIP = kModel.IslemYapanIP;
                }
                else
                {

                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.Add(kModel);
                    tdoBas.AktifTDOBasvuruDanismanID = kModel.TDOBasvuruDanismanID;
                }
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", kModel.TDOBasvuruDanismanID > 0 ? LogCrudType.Update : LogCrudType.Insert, tdoBasvuruDanis.ToJson());

                mMessage.IsSuccess = true;
                TdoBus.SendMailTdoBilgisi(kModel.TDOBasvuruDanismanID);

            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult GetTdoDanismanDegisiklikFormu(int tdoBasvuruId, int tdoBasvuruDanismanId)
        {
            var model = new KmTdoBasvuruDanisman() { TDOBasvuruID = tdoBasvuruId };
            var mMessage = new MmMessage()
            {
                Title = "Tez Danışmanı Değişikliği İşlemi"
            };
            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            var ogrenci = tdoBas.Kullanicilar;
            model.OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + "-" + tdoBas.OgrenciNo;

            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            if (tdoBasvuruDanismanId > 0)
            {
                if (tdoBas.TDOBasvuruDanisman.VarolanDanismanOnayladi == true)
                {
                    mMessage.Messages.Add(
                        "Varolan Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                }
                else if (tdoBas.TDOBasvuruDanisman.DanismanOnayladi == true)
                {
                    mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                }
            }
            else
            {

                if (!mMessage.Messages.Any())
                {
                    var msgs = TijBus.IsAktifDevamEdenTijMessage(tdoBas.KullaniciID, tdoBas.OgrenciNo);
                    mMessage.Messages.AddRange(msgs);

                }

                if (!mMessage.Messages.Any())
                {
                    if (MezuniyetBus.IsMezuniyetBasvuruVar(ogrenci.KullaniciID, ogrenci.OgrenciNo))
                    {
                        mMessage.Messages.Add("Aktif olarak devam eden bir mezuniyet başvurunuz bulunmakta. Tez Danışmanı Değişikliği işlemi yapamazsınız.");
                    }
                }
            }

            if (!mMessage.Messages.Any() && !(tdoBasvuruDanismanId > 0))
            {
                var msgs = TijBus.IsAktifDevamEdenTijMessage(tdoBas.KullaniciID, tdoBas.OgrenciNo);
                mMessage.Messages.AddRange(msgs);

            }
            if (!mMessage.Messages.Any())
            {
                var ogrenciData = KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBas.KullaniciID);

                var isNew = tdoBasvuruDanismanId <= 0;
                if (tdoBasvuruDanismanId <= 0) tdoBasvuruDanismanId = tdoBas.AktifTDOBasvuruDanismanID ?? 0;
                var tdoBd = tdoBas.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);

                model.TDOBasvuruID = tdoBd.TDOBasvuruID;
                model.IsTezDiliTr = tdoBd.IsYeniTezDiliTr ?? tdoBd.IsTezDiliTr;
                model.UniqueID = tdoBd.UniqueID;
                model.OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + "-" + tdoBas.OgrenciNo;
                model.VarolanTezDanismanID = tdoBd.TezDanismanID;
                if (isNew)
                {
                    model.TezDanismanID = 0;
                    model.TDAnabilimDaliID = null;
                    model.TDProgramKod = null;
                    model.TezBaslikTr = tdoBd.TezBaslikTr;
                    model.TezBaslikEn = tdoBd.TezBaslikEn;
                    if (model.TezBaslikTr.IsNullOrWhiteSpace())
                    {
                        model.TezBaslikTr = ogrenciData.OgrenciTez.TEZ_BASLIK;
                    }

                    if (model.TezBaslikEn.IsNullOrWhiteSpace())
                    {
                        model.TezBaslikEn = ogrenciData.OgrenciTez.TEZ_BASLIK_ENG;
                    }
                     
                }
                else
                {
                    model.TezBaslikTr = tdoBd.TezBaslikTr;
                    model.TezBaslikEn = tdoBd.TezBaslikEn;

                    model.TDOBasvuruDanismanID = tdoBasvuruDanismanId;
                    model.SinavTipID = tdoBd.SinavTipID;
                    model.SinavPuani = tdoBd.SinavPuani;
                    model.SinavYili = tdoBd.SinavYili;
                    model.TDAnabilimDaliID = tdoBd.TDAnabilimDaliID;
                    model.TDProgramKod = tdoBd.TDProgramKod;
                    var danisman = _entities.Kullanicilars.First(f => f.KullaniciID == tdoBd.TezDanismanID);
                    model.TezDanismanID = tdoBd.TezDanismanID;
                    model.TDAdSoyad = danisman.Ad + " " + danisman.Soyad;

                }
                if (isNew || !tdoBd.DanismanOnayladi.HasValue || tdoBd.DanismanOnayladi == false) // yeni kayıt veya danışman onayı yok veya ret ise son tez başlığı getirilsin
                {
                    var sonTezBaslik = TdoBus.GetSonTezBaslik(tdoBas.OgrenciNo);//tez başlığı değişikliklerini diğer modülerde kontrol et eğer değişiklik varsa son değişiklik alınacak
                    if (sonTezBaslik != null)
                    {
                        model.IsTezDiliTr = sonTezBaslik.IsTezDiliTr;
                        model.TezBaslikTr = sonTezBaslik.TezBaslikTr;
                        model.TezBaslikEn = sonTezBaslik.TezBaslikEn;
                    }
                }
                model.SListSinav = new SelectList(SinavTipleriBus.CmbGetAktifSinavlar(tdoBas.EnstituKod, SinavTipGrupEnum.DilSinavlari, true), "Value", "Caption", model.SinavTipID);
                model.SListTdAnabilimDali = new SelectList(AnabilimDallariBus.CmbGetAktifAnabilimDallari(tdoBas.EnstituKod, true), "Value", "Caption", model.TDAnabilimDaliID);
                model.SListTdProgram = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(true, model.TDAnabilimDaliID), "Value", "Caption", model.TDProgramKod);

                if (model.SinavTipID.HasValue)
                {
                    var sinav = _entities.SinavTipleris.First(p => p.SinavTipID == model.SinavTipID);
                    if (sinav.OzelNot) model.SListSinavNot = new SelectList(SinavTipleriBus.CmbGetSinavTipOzelNot(model.SinavTipID.Value, true), "Value", "Caption", model.SinavPuani);

                }
                mMessage.MessageType = MsgTypeEnum.Information;
                mMessage.IsSuccess = true;
            }


            if (mMessage.MessageType != MsgTypeEnum.Information) mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = mMessage.IsSuccess ? ViewRenderHelper.RenderPartialView("TdoBasvuru", "TdoDanismanDegisiklikFormu", model) : "",
                Messages = strView
            }.ToJsonResult();
        }

        public ActionResult TdoDanismanDegisiklikFormuPost(TDOBasvuruDanisman kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı Değişikliği İşlemi"
            };
            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == kModel.TDOBasvuruID && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            var oncekiBasvuru = tdoBas.TDOBasvuruDanismen.Where(p => p.EYKDaOnaylandi == true && p.TDOBasvuruDanismanID != kModel.TDOBasvuruDanismanID).OrderByDescending(o => o.TDOBasvuruDanismanID).First();
            var isTezDiliTr = oncekiBasvuru.IsYeniTezDiliTr ?? oncekiBasvuru.IsTezDiliTr;
            var ogrenciData = KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBas.KullaniciID);


            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            if (kModel.TDOBasvuruDanismanID > 0)
            {
                if (tdoBas.TDOBasvuruDanisman.VarolanDanismanOnayladi == true)
                {
                    mMessage.Messages.Add(
                        "Varolan Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                }
                else if (tdoBas.TDOBasvuruDanisman.DanismanOnayladi == true)
                {
                    mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                }
            }
            else
            {
                if (!mMessage.Messages.Any())
                {
                    if (MezuniyetBus.IsMezuniyetBasvuruVar(tdoBas.KullaniciID, tdoBas.OgrenciNo))
                    {
                        mMessage.Messages.Add("Aktif olarak devam eden bir mezuniyet başvurunuz bulunmakta. Tez danışman değişikliği işlemi yapamazsınız.");
                    }
                }
            }

            if (!mMessage.Messages.Any())
            {
                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(tdoBas.OgrenciNo);
                if (!ogrenciBilgi.KayitVar)
                {
                    mMessage.Messages.Add(tdoBas.OgrenciNo + " öğrenci numaranıza ait OBS isteminde aktif bir öğrenim bilgisine rastlanmadı. " + ogrenciBilgi.HataMsj);
                }
            }
            if (!mMessage.Messages.Any())
            {
                if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Varolan Tez başlığını türkçe olarak giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikTr.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TezBaslikTr" });
                if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Varolan Tez başlığını ingilizce olarak giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikEn.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TezBaslikEn" });


                if (kModel.TezDanismanID <= 0)
                {
                    mMessage.Messages.Add("Yeni Tez danışmanınızı seçiniz.");
                }

                if (kModel.TezDanismanID == oncekiBasvuru.TezDanismanID)
                {
                    mMessage.Messages.Add("Varolan danışman ile yeni danışman  aynı kişi olamaz!");
                }
                else
                {

                    if (kModel.TDAnabilimDaliID > 0 == false)
                    {
                        mMessage.Messages.Add("Tez Danışmanı Anabilim dalı bilgisini seçiniz.");
                    }

                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = (kModel.TDAnabilimDaliID > 0 ? MsgTypeEnum.Success : MsgTypeEnum.Warning),
                        PropertyName = "TDAnabilimDaliID"
                    });
                    if (kModel.TDProgramKod.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Tez Danışmanı program bilgisini seçiniz.");
                    }

                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = (!kModel.TDProgramKod.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning),
                        PropertyName = "TDProgramKod"
                    });

                }
            }


            if (!mMessage.Messages.Any())
            {
                var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.TezDanismanID);

                kModel.BasvuruTarihi = DateTime.Now;
                var donemBilgi = kModel.BasvuruTarihi.ToAkademikDonemBilgi();
                var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                while (_entities.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
                {
                    formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                }
                kModel.TDODanismanTalepTipID = TdoDanismanTalepTipEnum.TezDanismaniDegisikligi;
                kModel.UniqueID = Guid.NewGuid();
                kModel.FormKodu = formKodu;
                kModel.VarolanTezDanismanID = oncekiBasvuru.TezDanismanID;
                kModel.VarolanTDAdSoyad = oncekiBasvuru.TDAdSoyad;
                kModel.VarolanTDUnvanAdi = oncekiBasvuru.TDUnvanAdi;
                kModel.VarolanTDAnabilimDaliAdi = oncekiBasvuru.TDAnabilimDaliAdi;
                kModel.VarolanTDProgramAdi = oncekiBasvuru.TDProgramAdi;
                kModel.IsTezDiliTr = isTezDiliTr;
                kModel.TezBaslikTr = ogrenciData.OgrenciTez.TEZ_BASLIK;
                kModel.TezBaslikEn = ogrenciData.OgrenciTez.TEZ_BASLIK_ENG;
                kModel.YeniTezBaslikTr = ogrenciData.OgrenciTez.TEZ_BASLIK;
                kModel.YeniTezBaslikEn = ogrenciData.OgrenciTez.TEZ_BASLIK_ENG;
                kModel.SinavTipID = oncekiBasvuru.SinavTipID;
                kModel.SinavAdi = oncekiBasvuru.SinavAdi;
                kModel.SinavYili = oncekiBasvuru.SinavYili;
                kModel.SinavPuani = oncekiBasvuru.SinavPuani;
                kModel.DonemBaslangicYil = donemBilgi.BaslangicYil;
                kModel.DonemID = donemBilgi.DonemId;
                kModel.TDAdSoyad = danisman.Ad + " " + danisman.Soyad;
                kModel.TDUnvanAdi = danisman.Unvanlar.UnvanAdi;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (isTezDiliTr)
                {
                    kModel.SinavAdi = null;
                    kModel.SinavPuani = null;
                    kModel.SinavYili = null;
                }

                TDOBasvuruDanisman tdoBasvuruDanis;
                kModel.TDProgramAdi = _entities.Programlars.First(p => p.ProgramKod == kModel.TDProgramKod).ProgramAdi;
                kModel.TDAnabilimDaliAdi = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == kModel.TDAnabilimDaliID).AnabilimDaliAdi;
                if (!isTezDiliTr)
                {
                    if (kModel.SinavTipID.HasValue) kModel.SinavAdi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID).SinavAdi;

                }

                if (kModel.TDOBasvuruDanismanID > 0)
                {
                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);
                    tdoBasvuruDanis.BasvuruTarihi = kModel.BasvuruTarihi;
                    tdoBasvuruDanis.DonemBaslangicYil = kModel.DonemBaslangicYil;
                    tdoBasvuruDanis.DonemID = kModel.DonemID;
                    tdoBasvuruDanis.FormKodu = kModel.FormKodu;
                    tdoBasvuruDanis.UniqueID = kModel.UniqueID;
                    tdoBasvuruDanis.IsTezDiliTr = isTezDiliTr;
                    tdoBasvuruDanis.TezBaslikTr = kModel.TezBaslikTr;
                    tdoBasvuruDanis.TezBaslikEn = kModel.TezBaslikEn;
                    tdoBasvuruDanis.SinavTipID = kModel.SinavTipID;
                    tdoBasvuruDanis.SinavAdi = kModel.SinavAdi;
                    tdoBasvuruDanis.SinavPuani = kModel.SinavPuani;
                    tdoBasvuruDanis.SinavYili = kModel.SinavYili;
                    tdoBasvuruDanis.VarolanTezDanismanID = kModel.VarolanTezDanismanID;
                    tdoBasvuruDanis.VarolanTDAdSoyad = kModel.VarolanTDAdSoyad;
                    tdoBasvuruDanis.VarolanTDUnvanAdi = kModel.VarolanTDUnvanAdi;
                    tdoBasvuruDanis.VarolanTDAnabilimDaliAdi = kModel.VarolanTDAnabilimDaliAdi;
                    tdoBasvuruDanis.VarolanTDProgramAdi = kModel.VarolanTDProgramAdi;
                    tdoBasvuruDanis.TezDanismanID = kModel.TezDanismanID;
                    tdoBasvuruDanis.TDAdSoyad = kModel.TDAdSoyad;
                    tdoBasvuruDanis.TDUnvanAdi = kModel.TDUnvanAdi;
                    tdoBasvuruDanis.TDAnabilimDaliID = kModel.TDAnabilimDaliID;
                    tdoBasvuruDanis.TDAnabilimDaliAdi = kModel.TDAnabilimDaliAdi;
                    tdoBasvuruDanis.TDProgramKod = kModel.TDProgramKod;
                    tdoBasvuruDanis.TDProgramAdi = kModel.TDProgramAdi;
                    tdoBasvuruDanis.IslemTarihi = kModel.IslemTarihi;
                    tdoBasvuruDanis.IslemYapanID = kModel.IslemYapanID;
                    tdoBasvuruDanis.IslemYapanIP = kModel.IslemYapanIP;
                    tdoBasvuruDanis.DanismanOnayladi = null;
                    tdoBasvuruDanis.DanismanOnayTarihi = null;
                    tdoBasvuruDanis.DanismanOnaylanmadiAciklama = null;
                    tdoBasvuruDanis.TDOgrenciSayisiDR = null;
                    tdoBasvuruDanis.TDOgrenciSayisiYL = null;
                    tdoBasvuruDanis.TDTezSayisiDR = null;
                    tdoBasvuruDanis.TDTezSayisiYL = null;
                }
                else
                {

                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.Add(kModel);
                    tdoBas.AktifTDOBasvuruDanismanID = kModel.TDOBasvuruDanismanID;
                }
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", kModel.TDOBasvuruDanismanID > 0 ? LogCrudType.Update : LogCrudType.Insert, tdoBasvuruDanis.ToJson());

                mMessage.IsSuccess = true;

                TdoBus.SendMailTdoBilgisi(kModel.TDOBasvuruDanismanID);
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }


        public ActionResult GetTdoDilBaslikDegisiklikFormu(int tdoBasvuruId, int tdoBasvuruDanismanId)
        {

            var model = new KmTdoBasvuruDanisman()
            {
                TDOBasvuruID = tdoBasvuruId
            };
            var mMessage = new MmMessage()
            {
                Title = "Tez Dili, Tez Başlığı Değişikliği İşlemi"
            };

            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            var ogrenci = tdoBas.Kullanicilar;
            model.OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + "-" + tdoBas.OgrenciNo;
            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez dil/başlık öneri formu oluşturmaya yetkili değilsiniz.");
            }
            if (tdoBasvuruDanismanId > 0 && tdoBas.TDOBasvuruDanisman.DanismanOnayladi == true)
            {
                mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
            }

            if (!mMessage.Messages.Any() && !(tdoBasvuruDanismanId > 0))
            {
                var msgs = TijBus.IsAktifDevamEdenTijMessage(tdoBas.KullaniciID, tdoBas.OgrenciNo);
                mMessage.Messages.AddRange(msgs);
                if (!mMessage.Messages.Any())
                {
                    if (MezuniyetBus.IsMezuniyetBasvuruVar(ogrenci.KullaniciID, ogrenci.OgrenciNo))
                    {
                        mMessage.Messages.Add("Aktif olarak devam eden bir mezuniyet başvurunuz bulunmakta. Tez Dili, Tez Başlığı Değişikliği işlemi yapamazsınız.");
                    }
                }
            }

            if (!mMessage.Messages.Any())
            {
                var isNew = tdoBasvuruDanismanId <= 0;
                if (isNew) tdoBasvuruDanismanId = tdoBas.AktifTDOBasvuruDanismanID ?? 0;
                var tdoBd = tdoBas.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                model.IsTezDiliTr = tdoBd.IsTezDiliTr;
                model.TezBaslikTr = tdoBd.TezBaslikTr;
                model.TezBaslikEn = tdoBd.TezBaslikEn;

                model.TDOBasvuruID = tdoBd.TDOBasvuruID;
                model.OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + "-" + tdoBas.OgrenciNo;
                model.SinavTipID = tdoBd.SinavTipID;
                model.SinavAdi = tdoBd.SinavAdi;
                model.SinavPuani = tdoBd.SinavPuani;
                model.SinavYili = tdoBd.SinavYili;
                model.TDAdSoyad = tdoBd.TDAdSoyad;
                model.TDUnvanAdi = tdoBd.TDUnvanAdi;
                model.TDAnabilimDaliAdi = tdoBd.TDAnabilimDaliAdi;
                model.TDProgramAdi = tdoBd.TDProgramAdi;

                model.IsYeniTezDiliTr = tdoBd.IsYeniTezDiliTr;
                if (!isNew)
                {
                    model.TDOBasvuruDanismanID = tdoBd.TDOBasvuruDanismanID;
                    model.YeniTezBaslikTr = tdoBd.YeniTezBaslikTr;
                    model.YeniTezBaslikEn = tdoBd.YeniTezBaslikEn;
                }
                else
                {
                    var oncekiBasvuru = tdoBas.TDOBasvuruDanismen.Where(p => p.EYKDaOnaylandi == true && p.TDOBasvuruDanismanID != tdoBasvuruDanismanId).OrderByDescending(o => o.TDOBasvuruDanismanID).FirstOrDefault();
                    if (oncekiBasvuru != null && (oncekiBasvuru.IsYeniTezDiliTr == false || oncekiBasvuru.IsTezDiliTr == false))
                    {
                        model.SinavTipID = oncekiBasvuru.SinavTipID;
                        model.SinavAdi = oncekiBasvuru.SinavAdi;
                        model.SinavPuani = oncekiBasvuru.SinavPuani;
                        model.SinavYili = oncekiBasvuru.SinavYili;
                    }
                }
                if (isNew || !tdoBd.DanismanOnayladi.HasValue || tdoBd.DanismanOnayladi == false) // yeni kayıt veya danışman onayı yok veya ret ise son tez başlığı getirilsin
                {
                    var sonTezBaslik = TdoBus.GetSonTezBaslik(tdoBas.OgrenciNo);//tez başlığı değişikliklerini diğer modülerde kontrol et eğer değişiklik varsa son değişiklik alınacak
                    if (sonTezBaslik != null)
                    {
                        model.IsTezDiliTr = sonTezBaslik.IsTezDiliTr;
                        model.TezBaslikTr = sonTezBaslik.TezBaslikTr;
                        model.TezBaslikEn = sonTezBaslik.TezBaslikEn;
                    } 
                }


                model.SListTDoDanismanTalepTip = new SelectList(TdoBus.CmbTdoDanismanTalepTip(model.TDODanismanTalepTipID > TdoDanismanTalepTipEnum.TezDanismaniOnerisi, false), "Value", "Caption", model.TDODanismanTalepTipID);
                model.SListSinav = new SelectList(SinavTipleriBus.CmbGetAktifSinavlar(tdoBas.EnstituKod, SinavTipGrupEnum.DilSinavlari, true), "Value", "Caption", model.SinavTipID);
                model.SListTdAnabilimDali = new SelectList(AnabilimDallariBus.CmbGetAktifAnabilimDallari(tdoBas.EnstituKod, true), "Value", "Caption", model.TDAnabilimDaliID);
                model.SListTdProgram = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(true, model.TDAnabilimDaliID), "Value", "Caption", model.TDProgramKod);
                if (model.SinavTipID.HasValue)
                {
                    var sinav = _entities.SinavTipleris.First(p => p.SinavTipID == model.SinavTipID);
                    if (sinav.OzelNot) model.SListSinavNot = new SelectList(SinavTipleriBus.CmbGetSinavTipOzelNot(model.SinavTipID.Value, true), "Value", "Caption", model.SinavPuani);
                }
                mMessage.MessageType = MsgTypeEnum.Information;
                mMessage.IsSuccess = true;
            }
            if (mMessage.MessageType != MsgTypeEnum.Information) mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);
            return new
            {
                mMessage.IsSuccess,
                Content = mMessage.IsSuccess ? ViewRenderHelper.RenderPartialView("TdoBasvuru", "TdoDilBaslikDegisiklikFormu", model) : "",
                Messages = strView
            }.ToJsonResult();
        }


        public ActionResult TdoDilBaslikDegisiklikFormuPost(TDOBasvuruDanisman kModel, bool yenTezDiliDegisecekmi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Dili, Tez Başlığı Değişikliği İşlemi"
            };
            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == kModel.TDOBasvuruID && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            var oncekiBasvuru = tdoBas.TDOBasvuruDanismen.Where(p => p.EYKDaOnaylandi == true && p.TDOBasvuruDanismanID != kModel.TDOBasvuruDanismanID).OrderByDescending(o => o.TDOBasvuruDanismanID).First();
            if (yenTezDiliDegisecekmi == false) kModel.IsYeniTezDiliTr = null;
            var isOncekiTezDiliTr = oncekiBasvuru.IsYeniTezDiliTr ?? oncekiBasvuru.IsTezDiliTr;
            var isTezDiliTr = yenTezDiliDegisecekmi ? kModel.IsYeniTezDiliTr == true : isOncekiTezDiliTr;


            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            if (kModel.TDOBasvuruDanismanID > 0 && tdoBas.TDOBasvuruDanisman.DanismanOnayladi == true)
            {
                mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
            }

            if (!mMessage.Messages.Any())
            {
                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(tdoBas.OgrenciNo);
                if (!ogrenciBilgi.KayitVar)
                {
                    mMessage.Messages.Add(tdoBas.OgrenciNo + " öğrenci numaranıza ait OBS isteminde aktif bir öğrenim bilgisine rastlanmadı. " + ogrenciBilgi.HataMsj);
                }
            }
            if (!mMessage.Messages.Any())
            {
                if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Varolan Tez başlığını türkçe olarak giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikTr.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TezBaslikTr" });
                if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Varolan Tez başlığını ingilizce olarak giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikEn.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TezBaslikEn" });


                if (yenTezDiliDegisecekmi)
                {
                    if (!kModel.IsYeniTezDiliTr.HasValue)
                    {
                        mMessage.Messages.Add("Yeni tez dilini seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.IsYeniTezDiliTr.HasValue ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "IsYeniTezDiliTr" });
                }

                var tezBaslikMaxLength = tdoBas.Enstituler.TezBaslikMaxLength;
                if (kModel.YeniTezBaslikTr.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Yeni Tez başlığını türkçe olarak giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YeniTezBaslikTr" });
                }
                else if (tezBaslikMaxLength.HasValue && kModel.YeniTezBaslikTr.Length > tezBaslikMaxLength)
                {
                    mMessage.Messages.Add($"Yeni Tez başlığı türkçe bilgisi için '{tezBaslikMaxLength}' karakter ile sınırlandırılmış karakter uzunluğunu aştınız!");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YeniTezBaslikTr" });
                }
                else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YeniTezBaslikTr" });

                if (kModel.YeniTezBaslikEn.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Yeni Tez başlığını ingilizce olarak giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YeniTezBaslikEn" });
                }
                else if (tezBaslikMaxLength.HasValue && kModel.YeniTezBaslikEn.Length > tezBaslikMaxLength)
                {
                    mMessage.Messages.Add($"Yeni Tez başlığı ingilizce bilgisi için '{tezBaslikMaxLength}' karakter ile sınırlandırılmış karakter uzunluğunu aştınız!");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YeniTezBaslikEn" });
                }
                else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YeniTezBaslikEn" });
                if (isTezDiliTr == false)
                {
                    if (!kModel.SinavTipID.HasValue) mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav bilgisini seçiniz.");
                    if (!kModel.SinavYili.HasValue || kModel.SinavYili <= 0) mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav yılı bilginizi giriniz.");
                    if (kModel.SinavPuani.IsNullOrWhiteSpace() || !kModel.SinavPuani.ToDouble().HasValue)
                    {
                        mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav puanı bilgisini giriniz.");
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SinavPuani" });
                    }
                    else
                    {

                        if (kModel.SinavTipID.HasValue)
                        {
                            var sinavTipi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID);
                            kModel.SinavAdi = sinavTipi.SinavAdi;
                            var sinavPuani = kModel.SinavPuani.ToDouble(0);
                            var sinavTipKriter = sinavTipi.SinavTipleriOTNotAraliklaris.FirstOrDefault(p => p.OgrenimTipKod == tdoBas.OgrenimTipKod && p.Ingilizce == tdoBas.Programlar.Ingilizce);
                            if (sinavTipKriter != null)
                            {
                                var sinavPuaniUygun = (sinavPuani >= sinavTipKriter.Min && sinavPuani <= sinavTipKriter.Max);
                                if (!sinavPuaniUygun)
                                {
                                    mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav puanı en az " + sinavTipKriter.Min + " en fazla " + sinavTipKriter.Max + " olmalıdır.");
                                }
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = sinavPuaniUygun ? MsgTypeEnum.Success : MsgTypeEnum.Warning, PropertyName = "SinavPuani" });
                            }
                            else
                            {
                                mMessage.Messages.Add("Öğrenci Yabancı dil sınav yeterlilik puan kriterleri tanımlı değil. Bu bilgiyi Enstitüye iletiniz.");
                            }
                        }

                    }

                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.SinavTipID.HasValue ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "SinavTipID" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.SinavYili.HasValue && kModel.SinavYili > 0 ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "SinavYili" });

                }



            }
            if (!mMessage.Messages.Any() && yenTezDiliDegisecekmi)
            {
                if (isOncekiTezDiliTr == kModel.IsYeniTezDiliTr)
                {
                    mMessage.Messages.Add("Değiştirmek istediğiniz tez dili mevcut tez dili ile aynı olamaz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (isOncekiTezDiliTr != kModel.IsYeniTezDiliTr ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "IsYeniTezDiliTr" });

            }

            if (!mMessage.Messages.Any())
            {
                kModel.TDODanismanTalepTipID = TdoDanismanTalepTipEnum.TezBasligiDegisikligi;
                kModel.TezDanismanID = oncekiBasvuru.TezDanismanID;
                kModel.IsTezDiliTr = isOncekiTezDiliTr;
                //if (oncekiBasvuru.TDODanismanTalepTipID == TdoDanismanTalepTip.TezBasligiDegisikligi ||
                //    oncekiBasvuru.TDODanismanTalepTipID == TdoDanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                //{
                //    kModel.TezBaslikTr = oncekiBasvuru.YeniTezBaslikTr;
                //    kModel.TezBaslikEn = oncekiBasvuru.YeniTezBaslikEn;
                //}
                //else
                //{
                //    kModel.TezBaslikTr = oncekiBasvuru.TezBaslikTr;
                //    kModel.TezBaslikEn = oncekiBasvuru.TezBaslikEn;
                //}

                kModel.TDAnabilimDaliID = oncekiBasvuru.TDAnabilimDaliID;
                kModel.TDAnabilimDaliAdi = oncekiBasvuru.TDAnabilimDaliAdi;
                kModel.TDProgramKod = oncekiBasvuru.TDProgramKod;
                kModel.TDProgramAdi = oncekiBasvuru.TDProgramAdi;


                var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.TezDanismanID);
                if (isTezDiliTr)
                {
                    kModel.SinavTipID = null;
                    kModel.SinavAdi = null;
                    kModel.SinavPuani = null;
                    kModel.SinavYili = null;
                }

                kModel.BasvuruTarihi = DateTime.Now;
                var donemBilgi = kModel.BasvuruTarihi.ToAkademikDonemBilgi();
                kModel.DonemBaslangicYil = donemBilgi.BaslangicYil;
                kModel.DonemID = donemBilgi.DonemId;
                kModel.TDAdSoyad = danisman.Ad + " " + danisman.Soyad;
                kModel.TDUnvanAdi = danisman.Unvanlar.UnvanAdi;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                while (_entities.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
                {
                    formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                }
                kModel.UniqueID = Guid.NewGuid();
                kModel.FormKodu = formKodu;
                TDOBasvuruDanisman tdoBasvuruDanis;
                if (kModel.IsTezDiliTr == false)
                {
                    if (kModel.SinavTipID.HasValue) kModel.SinavAdi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID).SinavAdi;

                }
                if (kModel.TDOBasvuruDanismanID > 0)
                {
                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);

                    tdoBasvuruDanis.DanismanOnayladi = null;
                    tdoBasvuruDanis.DanismanOnayTarihi = null;
                    tdoBasvuruDanis.DanismanOnaylanmadiAciklama = null;
                    tdoBasvuruDanis.BasvuruTarihi = kModel.BasvuruTarihi;
                    tdoBasvuruDanis.DonemBaslangicYil = kModel.DonemBaslangicYil;
                    tdoBasvuruDanis.DonemID = kModel.DonemID;
                    tdoBasvuruDanis.FormKodu = kModel.FormKodu;
                    tdoBasvuruDanis.UniqueID = kModel.UniqueID;
                    tdoBasvuruDanis.IsTezDiliTr = kModel.IsTezDiliTr;
                    tdoBasvuruDanis.TezBaslikTr = kModel.TezBaslikTr;
                    tdoBasvuruDanis.TezBaslikEn = kModel.TezBaslikEn;
                    tdoBasvuruDanis.IsYeniTezDiliTr = kModel.IsYeniTezDiliTr;
                    tdoBasvuruDanis.YeniTezBaslikTr = kModel.YeniTezBaslikTr;
                    tdoBasvuruDanis.YeniTezBaslikEn = kModel.YeniTezBaslikEn;
                    tdoBasvuruDanis.SinavTipID = kModel.SinavTipID;
                    tdoBasvuruDanis.SinavAdi = kModel.SinavAdi;
                    tdoBasvuruDanis.SinavPuani = kModel.SinavPuani;
                    tdoBasvuruDanis.SinavYili = kModel.SinavYili;
                    tdoBasvuruDanis.TezDanismanID = kModel.TezDanismanID;
                    tdoBasvuruDanis.TDAdSoyad = kModel.TDAdSoyad;
                    tdoBasvuruDanis.TDUnvanAdi = kModel.TDUnvanAdi;
                    tdoBasvuruDanis.TDAnabilimDaliID = kModel.TDAnabilimDaliID;
                    tdoBasvuruDanis.TDAnabilimDaliAdi = kModel.TDAnabilimDaliAdi;
                    tdoBasvuruDanis.TDProgramKod = kModel.TDProgramKod;
                    tdoBasvuruDanis.TDProgramAdi = kModel.TDProgramAdi;
                    tdoBasvuruDanis.IslemTarihi = kModel.IslemTarihi;
                    tdoBasvuruDanis.IslemYapanID = kModel.IslemYapanID;
                    tdoBasvuruDanis.IslemYapanIP = kModel.IslemYapanIP;

                }
                else
                {

                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.Add(kModel);
                    tdoBas.AktifTDOBasvuruDanismanID = kModel.TDOBasvuruDanismanID;
                }
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", kModel.TDOBasvuruDanismanID > 0 ? LogCrudType.Update : LogCrudType.Insert, tdoBasvuruDanis.ToJson());

                mMessage.IsSuccess = true;
                TdoBus.SendMailTdoBilgisi(kModel.TDOBasvuruDanismanID);

            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }


        public ActionResult GetTdoDanismanBaslikDilDegisiklikFormu(int tdoBasvuruId, int tdoBasvuruDanismanId)
        {

            var model = new KmTdoBasvuruDanisman() { TDOBasvuruID = tdoBasvuruId };
            var mMessage = new MmMessage()
            {
                Title = "Tez Danışmanı, Tez Dili, Tez Başlığı Değişikliği İşlemi"
            };

            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            var ogrenci = tdoBas.Kullanicilar;
            model.OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + "-" + tdoBas.OgrenciNo;
            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez dil/başlık öneri formu oluşturmaya yetkili değilsiniz.");
            }
            if (tdoBasvuruDanismanId > 0 && tdoBas.TDOBasvuruDanisman.DanismanOnayladi == true)
            {
                mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
            }
            if (!mMessage.Messages.Any() && !(tdoBasvuruDanismanId > 0))
            {
                var msgs = TijBus.IsAktifDevamEdenTijMessage(tdoBas.KullaniciID, tdoBas.OgrenciNo);
                mMessage.Messages.AddRange(msgs);
                if (!mMessage.Messages.Any())
                {
                    if (MezuniyetBus.IsMezuniyetBasvuruVar(ogrenci.KullaniciID, ogrenci.OgrenciNo))
                    {
                        mMessage.Messages.Add("Aktif olarak devam eden bir mezuniyet başvurunuz bulunmakta. Tez Danışmanı, Tez Dili, Tez Başlığı Değişikliği işlemi yapamazsınız.");
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {
                var isNew = tdoBasvuruDanismanId <= 0;
                if (tdoBasvuruDanismanId <= 0) tdoBasvuruDanismanId = tdoBas.AktifTDOBasvuruDanismanID ?? 0;
                var tdoBd = tdoBas.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);

                model.TDOBasvuruID = tdoBd.TDOBasvuruID;
                model.OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + "-" + tdoBas.OgrenciNo;
                model.SinavTipID = tdoBd.SinavTipID;
                model.SinavAdi = tdoBd.SinavAdi;
                model.SinavPuani = tdoBd.SinavPuani;
                model.SinavYili = tdoBd.SinavYili;
                model.TDAdSoyad = tdoBd.TDAdSoyad;
                model.TDUnvanAdi = tdoBd.TDUnvanAdi;
                model.TDAnabilimDaliAdi = tdoBd.TDAnabilimDaliAdi;
                model.IsTezDiliTr = tdoBd.IsTezDiliTr;
                model.TezBaslikTr = tdoBd.TezBaslikTr;
                model.TezBaslikEn = tdoBd.TezBaslikEn;
                model.IsYeniTezDiliTr = tdoBd.IsYeniTezDiliTr;
                if (!isNew)
                {
                    model.YeniTezBaslikTr = tdoBd.YeniTezBaslikTr;
                    model.YeniTezBaslikEn = tdoBd.YeniTezBaslikEn;
                    model.TDOBasvuruDanismanID = tdoBd.TDOBasvuruDanismanID;

                    model.TezDanismanID = tdoBd.TezDanismanID;
                    model.TDAnabilimDaliID = tdoBd.TDAnabilimDaliID;
                    model.TDProgramKod = tdoBd.TDProgramKod;

                }
                else
                {
                   
                    var oncekiBasvuru = tdoBas.TDOBasvuruDanismen.Where(p => p.EYKDaOnaylandi == true && p.TDOBasvuruDanismanID != tdoBasvuruDanismanId).OrderByDescending(o => o.TDOBasvuruDanismanID).FirstOrDefault();
                    if (oncekiBasvuru != null && (oncekiBasvuru.IsYeniTezDiliTr == false || oncekiBasvuru.IsTezDiliTr == false))
                    {
                        model.SinavTipID = oncekiBasvuru.SinavTipID;
                        model.SinavAdi = oncekiBasvuru.SinavAdi;
                        model.SinavPuani = oncekiBasvuru.SinavPuani;
                        model.SinavYili = oncekiBasvuru.SinavYili;
                    }
                }
                if (isNew || !tdoBd.DanismanOnayladi.HasValue || tdoBd.DanismanOnayladi == false) // yeni kayıt veya danışman onayı yok veya ret ise son tez başlığı getirilsin
                {
                    var sonTezBaslik = TdoBus.GetSonTezBaslik(tdoBas.OgrenciNo);//tez başlığı değişikliklerini diğer modülerde kontrol et eğer değişiklik varsa son değişiklik alınacak
                    if (sonTezBaslik != null)
                    {
                        model.IsTezDiliTr = sonTezBaslik.IsTezDiliTr;
                        model.TezBaslikTr = sonTezBaslik.TezBaslikTr;
                        model.TezBaslikEn = sonTezBaslik.TezBaslikEn;
                    }
                }

                model.SListTDoDanismanTalepTip = new SelectList(TdoBus.CmbTdoDanismanTalepTip(model.TDODanismanTalepTipID > TdoDanismanTalepTipEnum.TezDanismaniOnerisi, false), "Value", "Caption", model.TDODanismanTalepTipID);
                model.SListSinav = new SelectList(SinavTipleriBus.CmbGetAktifSinavlar(tdoBas.EnstituKod, SinavTipGrupEnum.DilSinavlari, true), "Value", "Caption", model.SinavTipID);
                model.SListTdAnabilimDali = new SelectList(AnabilimDallariBus.CmbGetAktifAnabilimDallari(tdoBas.EnstituKod, true), "Value", "Caption", model.TDAnabilimDaliID);
                model.SListTdProgram = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(true, model.TDAnabilimDaliID), "Value", "Caption", model.TDProgramKod);

                if (model.SinavTipID.HasValue)
                {
                    var sinav = _entities.SinavTipleris.First(p => p.SinavTipID == model.SinavTipID);
                    if (sinav.OzelNot) model.SListSinavNot = new SelectList(SinavTipleriBus.CmbGetSinavTipOzelNot(model.SinavTipID.Value, true), "Value", "Caption", model.SinavPuani);

                }
                mMessage.MessageType = MsgTypeEnum.Information;
                mMessage.IsSuccess = true;
            }
            if (mMessage.MessageType != MsgTypeEnum.Information) mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = mMessage.IsSuccess ? ViewRenderHelper.RenderPartialView("TdoBasvuru", "TdoDanismanBaslikDilDegisiklikFormu", model) : "",
                Messages = strView
            }.ToJsonResult();
        }


        public ActionResult TdoDanismanBaslikDilDegisiklikFormuPost(TDOBasvuruDanisman kModel, bool yenTezDiliDegisecekmi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı, Tez Dili, Tez Başlığı Değişikliği İşlemi"
            };
            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == kModel.TDOBasvuruID && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            var oncekiBasvuru = tdoBas.TDOBasvuruDanismen.Where(p => p.EYKDaOnaylandi == true && p.TDOBasvuruDanismanID != kModel.TDOBasvuruDanismanID).OrderByDescending(o => o.TDOBasvuruDanismanID).First();
            KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBas.KullaniciID);
            if (yenTezDiliDegisecekmi == false) kModel.IsYeniTezDiliTr = null;
            var isOncekiTezDiliTr = oncekiBasvuru.IsYeniTezDiliTr ?? oncekiBasvuru.IsTezDiliTr;
            var isTezDiliTr = yenTezDiliDegisecekmi ? kModel.IsYeniTezDiliTr == true : isOncekiTezDiliTr;


            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }

            if (!mMessage.Messages.Any())
            {
                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(tdoBas.OgrenciNo);
                if (!ogrenciBilgi.KayitVar)
                {
                    mMessage.Messages.Add(tdoBas.OgrenciNo + " öğrenci numaranıza ait OBS isteminde aktif bir öğrenim bilgisine rastlanmadı. " + ogrenciBilgi.HataMsj);
                }
            }
            if (!mMessage.Messages.Any() && kModel.TDOBasvuruDanismanID > 0)
            {
                if (tdoBas.TDOBasvuruDanisman.VarolanDanismanOnayladi == true)
                {
                    mMessage.Messages.Add(
                        "Varolan Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                }
                else if (tdoBas.TDOBasvuruDanisman.DanismanOnayladi == true)
                {
                    mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                }
            }
            if (!mMessage.Messages.Any())
            {
                if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Varolan Tez başlığını türkçe olarak giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikTr.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TezBaslikTr" });
                if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Varolan Tez başlığını ingilizce olarak giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikEn.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TezBaslikEn" });

                if (yenTezDiliDegisecekmi)
                {
                    if (!kModel.IsYeniTezDiliTr.HasValue)
                    {
                        mMessage.Messages.Add("Yeni tez dilini seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.IsYeniTezDiliTr.HasValue ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "IsYeniTezDiliTr" });
                }

                var tezBaslikMaxLength = tdoBas.Enstituler.TezBaslikMaxLength;
                if (kModel.YeniTezBaslikTr.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Yeni Tez başlığını türkçe olarak giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YeniTezBaslikTr" });
                }
                else if (tezBaslikMaxLength.HasValue && kModel.YeniTezBaslikTr.Length > tezBaslikMaxLength)
                {
                    mMessage.Messages.Add($"Yeni Tez başlığı türkçe bilgisi için '{tezBaslikMaxLength}' karakter ile sınırlandırılmış karakter uzunluğunu aştınız!");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YeniTezBaslikTr" });
                }
                else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YeniTezBaslikTr" });

                if (kModel.YeniTezBaslikEn.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Yeni Tez başlığını ingilizce olarak giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YeniTezBaslikEn" });
                }
                else if (tezBaslikMaxLength.HasValue && kModel.YeniTezBaslikEn.Length > tezBaslikMaxLength)
                {
                    mMessage.Messages.Add($"Yeni Tez başlığı ingilizce bilgisi için '{tezBaslikMaxLength}' karakter ile sınırlandırılmış karakter uzunluğunu aştınız!");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YeniTezBaslikEn" });
                }
                else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YeniTezBaslikEn" }); if (isTezDiliTr == false)
                {
                    if (!kModel.SinavTipID.HasValue)
                        mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav bilgisini seçiniz.");
                    if (!kModel.SinavYili.HasValue || kModel.SinavYili <= 0)
                        mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav yılı bilginizi giriniz.");
                    if (kModel.SinavPuani.IsNullOrWhiteSpace() || !kModel.SinavPuani.ToDouble().HasValue)
                    {
                        mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav puanı bilgisini giriniz.");
                        mMessage.MessagesDialog.Add(new MrMessage
                        { MessageType = MsgTypeEnum.Warning, PropertyName = "SinavPuani" });
                    }
                    else
                    {

                        if (kModel.SinavTipID.HasValue)
                        {
                            var sinavTipi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID);
                            kModel.SinavAdi = sinavTipi.SinavAdi;
                            var sinavPuani = kModel.SinavPuani.ToDouble(0);
                            var sinavTipKriter = sinavTipi.SinavTipleriOTNotAraliklaris.FirstOrDefault(p =>
                                p.OgrenimTipKod == tdoBas.OgrenimTipKod && p.Ingilizce == tdoBas.Programlar.Ingilizce);
                            if (sinavTipKriter != null)
                            {
                                var sinavPuaniUygun =
                                    (sinavPuani >= sinavTipKriter.Min && sinavPuani <= sinavTipKriter.Max);
                                if (!sinavPuaniUygun)
                                {
                                    mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav puanı en az " +
                                                          sinavTipKriter.Min + " en fazla " + sinavTipKriter.Max +
                                                          " olmalıdır.");
                                }

                                mMessage.MessagesDialog.Add(new MrMessage
                                {
                                    MessageType = sinavPuaniUygun ? MsgTypeEnum.Success : MsgTypeEnum.Warning,
                                    PropertyName = "SinavPuani"
                                });
                            }
                            else
                            {
                                mMessage.Messages.Add(
                                    "Öğrenci Yabancı dil sınav yeterlilik puan kriterleri tanımlı değil. Bu bilgiyi Enstitüye iletiniz.");
                            }
                        }

                    }

                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = (kModel.SinavTipID.HasValue ? MsgTypeEnum.Success : MsgTypeEnum.Warning),
                        PropertyName = "SinavTipID"
                    });
                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = (kModel.SinavYili.HasValue && kModel.SinavYili > 0
                            ? MsgTypeEnum.Success
                            : MsgTypeEnum.Warning),
                        PropertyName = "SinavYili"
                    });
                }
            }
            if (!mMessage.Messages.Any() && yenTezDiliDegisecekmi)
            {
                if (isOncekiTezDiliTr == kModel.IsYeniTezDiliTr)
                {
                    mMessage.Messages.Add("Değiştirmek istediğiniz tez dili mevcut tez dili ile aynı olamaz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (isOncekiTezDiliTr != kModel.IsYeniTezDiliTr ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "IsYeniTezDiliTr" });

            }

            if (!mMessage.Messages.Any())
            {
                if (kModel.TezDanismanID <= 0)
                {
                    mMessage.Messages.Add("Yeni tez danışmanınızı seçiniz.");
                }
                else if (kModel.TezDanismanID == oncekiBasvuru.TezDanismanID)
                {
                    mMessage.Messages.Add("Varolan danışman ile yeni danışman aynı kişi olamaz!");
                }
                else
                {

                    if (kModel.TDAnabilimDaliID > 0 == false)
                    {
                        mMessage.Messages.Add("Tez danışmanı Anabilim dalı bilgisini seçiniz.");
                    }

                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = (kModel.TDAnabilimDaliID > 0 ? MsgTypeEnum.Success : MsgTypeEnum.Warning),
                        PropertyName = "TDAnabilimDaliID"
                    });
                    if (kModel.TDProgramKod.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Tez danışmanı program bilgisini seçiniz.");
                    }

                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = (!kModel.TDProgramKod.IsNullOrWhiteSpace()
                            ? MsgTypeEnum.Success
                            : MsgTypeEnum.Warning),
                        PropertyName = "TDProgramKod"
                    });

                }
            }

            if (!mMessage.Messages.Any())
            {
                kModel.TDODanismanTalepTipID = TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi;
                kModel.VarolanTezDanismanID = oncekiBasvuru.TezDanismanID;
                kModel.VarolanTDAdSoyad = oncekiBasvuru.TDAdSoyad;
                kModel.VarolanTDUnvanAdi = oncekiBasvuru.TDUnvanAdi;
                kModel.VarolanTDAnabilimDaliAdi = oncekiBasvuru.TDAnabilimDaliAdi;
                kModel.VarolanTDProgramAdi = oncekiBasvuru.TDProgramAdi;
                kModel.TezDanismanID = kModel.TezDanismanID;
                kModel.IsTezDiliTr = isOncekiTezDiliTr;

                //if (oncekiBasvuru.TDODanismanTalepTipID == TdoDanismanTalepTip.TezBasligiDegisikligi ||
                //    oncekiBasvuru.TDODanismanTalepTipID == TdoDanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                //{
                //    kModel.TezBaslikTr = oncekiBasvuru.YeniTezBaslikTr;
                //    kModel.TezBaslikEn = oncekiBasvuru.YeniTezBaslikEn;
                //}
                //else
                //{
                //    kModel.TezBaslikTr = oncekiBasvuru.TezBaslikTr;
                //    kModel.TezBaslikEn = oncekiBasvuru.TezBaslikEn;
                //}




                var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.TezDanismanID);
                if (isTezDiliTr)
                {
                    kModel.SinavTipID = null;
                    kModel.SinavAdi = null;
                    kModel.SinavPuani = null;
                    kModel.SinavYili = null;
                }

                kModel.BasvuruTarihi = DateTime.Now;
                var donemBilgi = kModel.BasvuruTarihi.ToAkademikDonemBilgi();
                kModel.DonemBaslangicYil = donemBilgi.BaslangicYil;
                kModel.DonemID = donemBilgi.DonemId;
                kModel.TDAdSoyad = danisman.Ad + " " + danisman.Soyad;
                kModel.TDUnvanAdi = danisman.Unvanlar.UnvanAdi;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                while (_entities.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
                {
                    formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                }
                kModel.UniqueID = Guid.NewGuid();
                kModel.FormKodu = formKodu;
                TDOBasvuruDanisman tdoBasvuruDanis;

                kModel.TDProgramAdi = _entities.Programlars.First(p => p.ProgramKod == kModel.TDProgramKod).ProgramAdi;
                kModel.TDAnabilimDaliAdi = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == kModel.TDAnabilimDaliID).AnabilimDaliAdi;
                if (isTezDiliTr == false)
                {
                    if (kModel.SinavTipID.HasValue) kModel.SinavAdi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID).SinavAdi;
                }
                if (kModel.TDOBasvuruDanismanID > 0)
                {
                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);

                    tdoBasvuruDanis.DanismanOnayladi = null;
                    tdoBasvuruDanis.DanismanOnayTarihi = null;
                    tdoBasvuruDanis.DanismanOnaylanmadiAciklama = null;
                    tdoBasvuruDanis.BasvuruTarihi = kModel.BasvuruTarihi;
                    tdoBasvuruDanis.DonemBaslangicYil = kModel.DonemBaslangicYil;
                    tdoBasvuruDanis.DonemID = kModel.DonemID;
                    tdoBasvuruDanis.FormKodu = kModel.FormKodu;
                    tdoBasvuruDanis.UniqueID = kModel.UniqueID;
                    tdoBasvuruDanis.IsTezDiliTr = kModel.IsTezDiliTr;
                    tdoBasvuruDanis.TezBaslikTr = kModel.TezBaslikTr;
                    tdoBasvuruDanis.TezBaslikEn = kModel.TezBaslikEn;
                    tdoBasvuruDanis.IsYeniTezDiliTr = kModel.IsYeniTezDiliTr;
                    tdoBasvuruDanis.YeniTezBaslikTr = kModel.YeniTezBaslikTr;
                    tdoBasvuruDanis.YeniTezBaslikEn = kModel.YeniTezBaslikEn;
                    tdoBasvuruDanis.SinavTipID = kModel.SinavTipID;
                    tdoBasvuruDanis.SinavAdi = kModel.SinavAdi;
                    tdoBasvuruDanis.SinavPuani = kModel.SinavPuani;
                    tdoBasvuruDanis.SinavYili = kModel.SinavYili;
                    tdoBasvuruDanis.VarolanTezDanismanID = kModel.VarolanTezDanismanID;
                    tdoBasvuruDanis.VarolanTDAdSoyad = kModel.VarolanTDAdSoyad;
                    tdoBasvuruDanis.VarolanTDUnvanAdi = kModel.VarolanTDUnvanAdi;
                    tdoBasvuruDanis.VarolanTDAnabilimDaliAdi = kModel.VarolanTDAnabilimDaliAdi;
                    tdoBasvuruDanis.VarolanTDProgramAdi = kModel.VarolanTDProgramAdi;
                    tdoBasvuruDanis.TezDanismanID = kModel.TezDanismanID;
                    tdoBasvuruDanis.TDAdSoyad = kModel.TDAdSoyad;
                    tdoBasvuruDanis.TDUnvanAdi = kModel.TDUnvanAdi;
                    tdoBasvuruDanis.TDAnabilimDaliID = kModel.TDAnabilimDaliID;
                    tdoBasvuruDanis.TDAnabilimDaliAdi = kModel.TDAnabilimDaliAdi;
                    tdoBasvuruDanis.TDProgramKod = kModel.TDProgramKod;
                    tdoBasvuruDanis.TDProgramAdi = kModel.TDProgramAdi;
                    tdoBasvuruDanis.IslemTarihi = kModel.IslemTarihi;
                    tdoBasvuruDanis.IslemYapanID = kModel.IslemYapanID;
                    tdoBasvuruDanis.IslemYapanIP = kModel.IslemYapanIP;
                }
                else
                {

                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.Add(kModel);
                    tdoBas.AktifTDOBasvuruDanismanID = kModel.TDOBasvuruDanismanID;
                }
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", kModel.TDOBasvuruDanismanID > 0 ? LogCrudType.Update : LogCrudType.Insert, tdoBasvuruDanis.ToJson());

                mMessage.IsSuccess = true;
                TdoBus.SendMailTdoBilgisi(kModel.TDOBasvuruDanismanID);

            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult TDODanismanOnayPost(TDOBasvuruDanisman kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı Öneri Formu Danışman Onay İşlemi"
            };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.FirstOrDefault(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);
            if (tdoBasvuruDanis == null)
            {
                mMessage.Messages.Add("Onaylanmak istenen başvuru bulunamadı. Başvurunun öğrenci tarafından silinme/vazgeçilme ihtimaline karşı ekranı yenileyip tekrar deneyiniz.");
                return mMessage.ToJsonResult();
            }
            if (!RoleNames.TdoEykdaOnayYetkisi.InRoleCurrent() && (!formYetki || tdoBasvuruDanis.TezDanismanID != UserIdentity.Current.Id))
            {
                mMessage.Messages.Add("Danışman onayı yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruDanis.EYKYaGonderildi == true)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'ya gönderim işlemi yapılan Danışman öneri formu üzerinden herhangi bir işlemi yapılamaz.");
            }
            else if (kModel.DanismanOnayladi == false)
            {
                if (kModel.DanismanOnaylanmadiAciklama.IsNullOrWhiteSpace()) mMessage.Messages.Add("Onaylanmama durumu için açıklama giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.DanismanOnaylanmadiAciklama.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "DanismanOnaylanmadiAciklama_" + kModel.TDOBasvuruDanismanID });
            }

            if (!mMessage.Messages.Any() && kModel.DanismanOnayladi == true)
            {

                if (kModel.TDProgramKod.IsNullOrWhiteSpace()) mMessage.Messages.Add("Programınızı seçiniz.");
                if (!kModel.TDOgrenciSayisiYL.HasValue || kModel.TDOgrenciSayisiYL < 0) mMessage.Messages.Add("Yüksek lisans öğrenci sayısı bilgisini giriniz.");
                if (!kModel.TDTezSayisiYL.HasValue || kModel.TDTezSayisiYL < 0) mMessage.Messages.Add("Yüksek lisans tez sayısı bilgisini giriniz.");
                if (!kModel.TDOgrenciSayisiDR.HasValue || kModel.TDOgrenciSayisiDR < 0) mMessage.Messages.Add("Doktora öğrenci sayısı bilgisini giriniz.");
                if (!kModel.TDTezSayisiDR.HasValue || kModel.TDTezSayisiDR < 0) mMessage.Messages.Add("Doktora tez sayısı bilgisini giriniz.");
                if (tdoBasvuruDanis.TDODanismanTalepTipID != TdoDanismanTalepTipEnum.TezDanismaniOnerisi && kModel.Gerekce.IsNullOrWhiteSpace()) mMessage.Messages.Add("Gerekçe giriniz.");

                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TDProgramKod.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TDProgramKod_" + kModel.TDOBasvuruDanismanID });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDOgrenciSayisiYL.HasValue && kModel.TDOgrenciSayisiYL >= 0 ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TDOgrenciSayisiYL_" + kModel.TDOBasvuruDanismanID });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDTezSayisiYL.HasValue && kModel.TDTezSayisiYL >= 0 ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TDTezSayisiYL_" + kModel.TDOBasvuruDanismanID });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDOgrenciSayisiDR.HasValue && kModel.TDOgrenciSayisiDR >= 0 ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TDOgrenciSayisiDR_" + kModel.TDOBasvuruDanismanID });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDTezSayisiDR.HasValue && kModel.TDTezSayisiDR >= 0 ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TDTezSayisiDR_" + kModel.TDOBasvuruDanismanID });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.Gerekce.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "DanismanGerekce_" + kModel.TDOBasvuruDanismanID });
                if (!mMessage.Messages.Any())
                {
                    var danismanOgrenciKriterMax = TdoAyar.DanismanMaxOgrenciKayitKriter.GetAyarTdo(tdoBasvuruDanis.TDOBasvuru.EnstituKod).ToDouble();

                    if (tdoBasvuruDanis.TDOBasvuru.OgrenimTipKod == OgrenimTipi.Doktra)
                    {
                        if (kModel.TDTezSayisiDR == 0 && kModel.TDTezSayisiYL == 0)
                        {
                            mMessage.Messages.Add("Doktora Öğrenim seviyesinde danışman atama formu oluşturulabilmesi için danışman mezun yükü 0 dan büyük olmalıdır.");
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TDTezSayisiDR_" + kModel.TDOBasvuruDanismanID });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TDTezSayisiYL_" + kModel.TDOBasvuruDanismanID });
                        }
                        else
                        {
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TDTezSayisiDR_" + kModel.TDOBasvuruDanismanID });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TDTezSayisiYL_" + kModel.TDOBasvuruDanismanID });
                        }
                    }
                    if (!mMessage.Messages.Any())
                    {
                        if (danismanOgrenciKriterMax != null)
                        {
                            var danismanOgrenciKriterMaxSaglaniyor = danismanOgrenciKriterMax >= (kModel.TDOgrenciSayisiDR + kModel.TDOgrenciSayisiYL);
                            if (!danismanOgrenciKriterMaxSaglaniyor)
                            {
                                mMessage.Messages.Add("Danışmanın yüksek lisans ve doktora kayıtlı öğrenci sayısı toplamı maksimum " + danismanOgrenciKriterMax + " olabilir.");
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TDOgrenciSayisiDR_" + kModel.TDOBasvuruDanismanID });
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TDOgrenciSayisiYL_" + kModel.TDOBasvuruDanismanID });
                            }
                            else
                            {
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TDOgrenciSayisiDR_" + kModel.TDOBasvuruDanismanID });
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TDOgrenciSayisiYL_" + kModel.TDOBasvuruDanismanID });
                            }
                        }
                        else
                        {
                            mMessage.Messages.Add("Danışman makisimum kayıtlı öğrenci sayısı kriteri bilgisi tanımlı değil. Bu bilgiyi Enstitüye iletiniz.");

                        }
                    }

                }
            }

            if (!mMessage.Messages.Any())
            {
                if (tdoBasvuruDanis.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniOnerisi)
                    kModel.Gerekce = "";
                var sendMail = false;
                if (tdoBasvuruDanis.DanismanOnayladi != kModel.DanismanOnayladi)
                {
                    var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                    while (_entities.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
                    {
                        formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                    }
                    tdoBasvuruDanis.UniqueID = Guid.NewGuid();
                    tdoBasvuruDanis.FormKodu = formKodu;
                    if (kModel.DanismanOnayladi.HasValue) sendMail = true;
                }
                if (tdoBasvuruDanis.EYKYaGonderildi == false)
                {
                    var program = _entities.Programlars.First(p => p.ProgramKod == kModel.TDProgramKod);
                    sendMail = (
                        tdoBasvuruDanis.TDProgramKod != program.ProgramKod ||
                        tdoBasvuruDanis.TDProgramAdi != program.ProgramAdi ||
                        tdoBasvuruDanis.TDAnabilimDaliID != program.AnabilimDaliID ||
                        tdoBasvuruDanis.TDAnabilimDaliAdi != program.AnabilimDallari.AnabilimDaliAdi ||
                        tdoBasvuruDanis.TDTezSayisiDR != kModel.TDTezSayisiDR ||
                        tdoBasvuruDanis.TDTezSayisiYL != kModel.TDTezSayisiYL ||
                        tdoBasvuruDanis.TDOgrenciSayisiDR != kModel.TDOgrenciSayisiDR ||
                        tdoBasvuruDanis.TDOgrenciSayisiYL != kModel.TDOgrenciSayisiYL
                    );
                    if (sendMail) tdoBasvuruDanis.EYKYaGonderildi = null;

                }
                if (kModel.DanismanOnayladi.HasValue)
                {
                    var program = _entities.Programlars.First(p => p.ProgramKod == kModel.TDProgramKod);
                    tdoBasvuruDanis.TDProgramKod = program.ProgramKod;
                    tdoBasvuruDanis.TDProgramAdi = program.ProgramAdi;
                    tdoBasvuruDanis.TDAnabilimDaliID = program.AnabilimDaliID;
                    tdoBasvuruDanis.TDAnabilimDaliAdi = program.AnabilimDallari.AnabilimDaliAdi;



                }

                tdoBasvuruDanis.DanismanOnayladi = kModel.DanismanOnayladi;
                tdoBasvuruDanis.DanismanOnayTarihi = DateTime.Now;
                tdoBasvuruDanis.DanismanOnaylanmadiAciklama = kModel.DanismanOnaylanmadiAciklama;
                tdoBasvuruDanis.TDTezSayisiDR = kModel.TDTezSayisiDR;
                tdoBasvuruDanis.TDTezSayisiYL = kModel.TDTezSayisiYL;
                tdoBasvuruDanis.TDOgrenciSayisiDR = kModel.TDOgrenciSayisiDR;
                tdoBasvuruDanis.TDOgrenciSayisiYL = kModel.TDOgrenciSayisiYL;
                tdoBasvuruDanis.Gerekce = kModel.Gerekce;
                tdoBasvuruDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", LogCrudType.Update, tdoBasvuruDanis.ToJson());
                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    TdoBus.SendMailTdoDanismanOnay(kModel.TDOBasvuruDanismanID, kModel.DanismanOnayladi == true);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult TDOVarolanDanismanOnayPost(TDOBasvuruDanisman kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı Öneri Formu Danışman Onay İşlemi"
            };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);
            if (!RoleNames.TdoEykdaOnayYetkisi.InRoleCurrent() && (!formYetki || tdoBasvuruDanis.VarolanTezDanismanID != UserIdentity.Current.Id))
            {
                mMessage.Messages.Add("Danışman onayı yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruDanis.DanismanOnayladi.HasValue)
            {
                mMessage.Messages.Add("Yeni danışman tarafından onay işlemi yapılan danışman değişiklik formu üzerinden herhangi bir işlemi yapılamaz.");
            }
            else if (kModel.DanismanOnayladi == false)
            {
                if (kModel.VarolanDanismanOnaylanmadiAciklama.IsNullOrWhiteSpace()) mMessage.Messages.Add("Onaylanmama durumu için açıklama giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.VarolanDanismanOnaylanmadiAciklama.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "VarolanDanismanOnaylanmadiAciklama_" + kModel.TDOBasvuruDanismanID });
            }
            if (!mMessage.Messages.Any())
            {
                var sendMail = false;
                if (tdoBasvuruDanis.DanismanOnayladi != kModel.DanismanOnayladi)
                {
                    var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                    while (_entities.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
                    {
                        formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                    }
                    tdoBasvuruDanis.UniqueID = Guid.NewGuid();
                    tdoBasvuruDanis.FormKodu = formKodu;
                    if (kModel.DanismanOnayladi.HasValue) sendMail = true;
                }


                tdoBasvuruDanis.VarolanDanismanOnayladi = kModel.VarolanDanismanOnayladi;
                tdoBasvuruDanis.VarolanDanismanOnayTarihi = DateTime.Now;
                tdoBasvuruDanis.VarolanDanismanOnaylanmadiAciklama = kModel.VarolanDanismanOnaylanmadiAciklama;
                tdoBasvuruDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    LogIslemleri.LogEkle("TDOBasvuruDanisman", LogCrudType.Update, tdoBasvuruDanis.ToJson());
                    TdoBus.SendMailTdoDanismanOnay(kModel.TDOBasvuruDanismanID, tdoBasvuruDanis.VarolanDanismanOnayladi.Value);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult TdoEykYaGonderimPost(int tdoBasvuruDanismanId, bool? eykYaGonderildi, string eykYaGonderimDurumAciklamasi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı Öneri Formu EYK'ya Gönderim İşlemi"
            };
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
            if (!RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya gönderme yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruDanis.DanismanOnayladi != true)
            {
                mMessage.Messages.Add("Danışman tarafından onaylandı işlemi yapılmayan Danışman öneri formu üzerinden EYK'ya gönderim işlemi yapılamaz.");
            }
            else if (tdoBasvuruDanis.EYKDaOnaylandi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'da onaylandı işlemi yapılan Danışman öneri formu üzerinden EYK'ya gönderim işlemi yapılamaz.");
            }
            else if (eykYaGonderildi == false && eykYaGonderimDurumAciklamasi.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("EYK'ya gönderilmeme sebebi açıklaması giriniz.");
            }
            if (!mMessage.Messages.Any())
            {
                if (tdoBasvuruDanis.EYKYaGonderildi.HasValue || eykYaGonderildi != false)
                {
                    // eykya gönderimi onay işlemi gördü yada yeni onay durumu onaylanmadı değil ise öğrencinin aktiflik durumunu kontrol et
                    var ogrenciObsBilgi =
                        KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBasvuruDanis.TDOBasvuru.KullaniciID);

                    if (!ogrenciObsBilgi.KayitVar)
                    {
                        mMessage.Messages.Add(
                            "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                    }
                    else if (tdoBasvuruDanis.TDOBasvuru.OgrenciNo != ogrenciObsBilgi.OgrenciInfo.OGR_NO)
                    {
                        mMessage.Messages.Add(
                            "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir.");
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {
                var sendMail = tdoBasvuruDanis.EYKYaGonderildi != eykYaGonderildi && eykYaGonderildi == false;
                tdoBasvuruDanis.EYKYaGonderildi = eykYaGonderildi;
                tdoBasvuruDanis.EYKYaGonderildiIslemTarihi = DateTime.Now;
                tdoBasvuruDanis.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.EYKYaGonderimDurumAciklamasi =
                    tdoBasvuruDanis.EYKYaGonderildi == false ? eykYaGonderimDurumAciklamasi : "";
                tdoBasvuruDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", LogCrudType.Update, tdoBasvuruDanis.ToJson());
                if (sendMail)
                {
                    TdoBus.SendMailTdoEykYaGonderimRet(tdoBasvuruDanismanId);
                }
                mMessage.IsSuccess = true;
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult TdoEykYaHazirlaPost(int tdoBasvuruDanismanId, bool? eykYaHazirlandi, string eykYaHazirlandiAciklamasi = "")
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı Öneri Formu EYK'ya Hazırlama İşlemi"
            };
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
            if (!RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya hazırlama yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruDanis.EYKYaGonderildi != true)
            {
                mMessage.Messages.Add("Enstitü tarafından Eyk'ya gönderim işlemi yapılmayan Danışman öneri formu üzerinden EYK'ya hazırlandı işlemi yapılamaz.");
            }
            else if (tdoBasvuruDanis.EYKDaOnaylandi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'da onaylandı işlemi yapılan Danışman öneri formu üzerinden EYK'ya hazırlandı işlemi yapılamaz.");
            }

            if (!mMessage.Messages.Any())
            {
                if (tdoBasvuruDanis.EYKYaGonderildi.HasValue || eykYaHazirlandi != false)
                {
                    // eykya gönderimi onay işlemi gördü yada yeni onay durumu onaylanmadı değil ise öğrencinin aktiflik durumunu kontrol et
                    var ogrenciObsBilgi =
                        KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBasvuruDanis.TDOBasvuru.KullaniciID);

                    if (!ogrenciObsBilgi.KayitVar)
                    {
                        mMessage.Messages.Add(
                            "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                    }
                    else if (tdoBasvuruDanis.TDOBasvuru.OgrenciNo != ogrenciObsBilgi.OgrenciInfo.OGR_NO)
                    {
                        mMessage.Messages.Add(
                            "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir.");
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {
                tdoBasvuruDanis.EYKYaHazirlandiAciklamasi = eykYaHazirlandiAciklamasi;
                tdoBasvuruDanis.EYKYaHazirlandi = eykYaHazirlandi;
                tdoBasvuruDanis.EYKYaHazirlandiIslemTarihi = DateTime.Now;
                tdoBasvuruDanis.EYKYaHazirlandiIslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", LogCrudType.Update, tdoBasvuruDanis.ToJson());
                mMessage.IsSuccess = true;
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }
        public ActionResult TdoEykDaOnayPost(int tdoBasvuruDanismanId, bool? eykDaOnaylandi, DateTime? eykDaOnaylandiOnayTarihi, string eykDaOnaylanmadiDurumAciklamasi, bool isBaslikGuncellensin, bool isYeniTezBasligiGozuksun, string tezBaslikTr, string tezBaslikEn)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı Öneri Formu EYK'Da Onay İşlemi"
            };
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
            if (!RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'da onay yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruDanis.EYKYaGonderildi != true)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'ya gönderildi işlemi yapılmayan Danışman öneri formu üzerinden EYK onay işlemi yapılamaz.");
            }
            if (eykDaOnaylandi == false)
            {
                if (eykDaOnaylanmadiDurumAciklamasi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Onaylanmama durumu için açıklama giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Error, PropertyName = "EYKDaOnaylanmadiDurumAciklamasi_" + tdoBasvuruDanismanId });

                }
            }
            else if (eykDaOnaylandi == true)
            {
                if (!eykDaOnaylandiOnayTarihi.HasValue)
                {
                    mMessage.Messages.Add("EYK'Da onaylanma tarihini giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Error, PropertyName = "EYKDaOnaylandiOnayTarihi_" + tdoBasvuruDanismanId });

                }

                if (isBaslikGuncellensin)
                {
                    if (tezBaslikTr.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add((isYeniTezBasligiGozuksun ? "Yeni " : "") + "Tez Başlığı Türkçe bilgisi boş bırakılamaz");
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Error, PropertyName = "TezBaslikTr_" + tdoBasvuruDanismanId });

                    }
                    if (tezBaslikEn.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add((isYeniTezBasligiGozuksun ? "Yeni " : "") + "Tez Başlığı İngilizce bilgisi boş bırakılamaz");
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Error, PropertyName = "TezBaslikEn_" + tdoBasvuruDanismanId });

                    }
                }
            }
            if (!mMessage.Messages.Any())
            {
                if (tdoBasvuruDanis.EYKDaOnaylandi.HasValue || eykDaOnaylandi != false)
                {
                    // eyk'da onay işlemi gördü yada yeni onay durumu onaylanmadı değil ise öğrencinin aktiflik durumunu kontrol et
                    var ogrenciObsBilgi =
                        KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBasvuruDanis.TDOBasvuru.KullaniciID);

                    if (!ogrenciObsBilgi.KayitVar)
                    {
                        mMessage.Messages.Add(
                            "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                    }
                    else if (tdoBasvuruDanis.TDOBasvuru.OgrenciNo != ogrenciObsBilgi.OgrenciInfo.OGR_NO)
                    {
                        mMessage.Messages.Add(
                            "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir.");
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {

                var sendMail = eykDaOnaylandi.HasValue && eykDaOnaylandi != tdoBasvuruDanis.EYKDaOnaylandi;
                tdoBasvuruDanis.EYKDaOnaylandi = eykDaOnaylandi;
                if (eykDaOnaylandi == true) tdoBasvuruDanis.EYKDaOnaylandiOnayTarihi = eykDaOnaylandiOnayTarihi.Value;
                tdoBasvuruDanis.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                if (eykDaOnaylandi == false)
                {

                    if (tdoBasvuruDanis.EYKDaOnaylanmadiDurumAciklamasi == null || eykDaOnaylanmadiDurumAciklamasi.Trim() != tdoBasvuruDanis.EYKDaOnaylanmadiDurumAciklamasi.Trim()) sendMail = true;
                    tdoBasvuruDanis.EYKDaOnaylanmadiDurumAciklamasi = eykDaOnaylanmadiDurumAciklamasi;
                }

                if (isBaslikGuncellensin)
                {
                    if (isYeniTezBasligiGozuksun)
                    {
                        tdoBasvuruDanis.YeniTezBaslikTr = tezBaslikTr;
                        tdoBasvuruDanis.YeniTezBaslikEn = tezBaslikEn;
                    }
                    else
                    {
                        tdoBasvuruDanis.TezBaslikTr = tezBaslikTr;
                        tdoBasvuruDanis.TezBaslikEn = tezBaslikEn;
                    }
                }

                tdoBasvuruDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemYapanIP = UserIdentity.Ip;
                // TDOBasvuruDanis.TDOBasvuru.Kullanicilar.DanismanID = TDOBasvuruDanis.TezDanismanID;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    LogIslemleri.LogEkle("TDOBasvuruDanisman", LogCrudType.Update, tdoBasvuruDanis.ToJson());
                    TdoBus.SendMailTdoEykOnay(tdoBasvuruDanismanId, eykDaOnaylandi.Value);
                }
                mMessage.Messages.Add("Eyk da onay durum bilgisi güncellendi" + (sendMail ? " ve ilgili kişilere mail gönderildi." : "."));
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }


        bool IslemYapilabilir(bool? eykYaGonderildi, bool? eykYaHazirlandi, bool? eykDaOnaylandi)
        {
            // EYKYaGonderildi false ise, diğer aşamalara bakılmaksızın işlem yapılabilir
            if (eykYaGonderildi == false)
                return true;

            // EYKYaGonderildi true veya null ise, EYKYaHazirlandi kontrol edilir
            // EYKYaHazirlandi false ise işlem yapılabilir
            if (eykYaHazirlandi == false)
                return true;

            // EYKYaHazirlandi true veya null ise, EYKDaOnaylandi kontrol edilir
            // EYKDaOnaylandi dolu (true veya false) ise işlem yapılabilir
            if (eykDaOnaylandi.HasValue)
                return true;

            // Yukarıdaki koşullar sağlanmadıysa işlem yapılamaz
            return false;
        }
        public ActionResult GetTdoEsDanismanFormu(int tdoBasvuruDanismanId, int? tdoBasvuruEsDanismanId, bool isDegisiklikTalebi = false)
        {
            var mMessage = new MmMessage();
            var view = "";
            var model = new TDOBasvuruEsDanisman() { TDOBasvuruDanismanID = tdoBasvuruDanismanId, IsDegisiklikTalebi = isDegisiklikTalebi };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var yYetki = RoleNames.TdoEykdaOnayYetkisi.InRoleCurrent() || RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent();
            var tdoBasvuruDanismanData = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
            if (tdoBasvuruEsDanismanId.HasValue) model = _entities.TDOBasvuruEsDanismen.FirstOrDefault(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);

            if (!formYetki || (!yYetki && tdoBasvuruDanismanData.TezDanismanID != UserIdentity.Current.Id))
            {
                mMessage.Messages.Add("Tez Eş Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            else if (!IslemYapilabilir(
                         tdoBasvuruDanismanData.EYKYaGonderildi,
                         tdoBasvuruDanismanData.EYKYaHazirlandi,
                         tdoBasvuruDanismanData.EYKDaOnaylandi
                     ))
            {
                mMessage.Messages.Add("EYK'süreci devam eden danışman formu bulunduğundan eş danışman öneri ya da düzeltme işlemi yapılamaz.");
            }
            else if (tdoBasvuruEsDanismanId.HasValue)
            {
                var esDanismanFormu = tdoBasvuruDanismanData.TDOBasvuruEsDanismen.First();
                if (esDanismanFormu.EYKDaOnaylandi == true)
                {
                    mMessage.Messages.Add("EYK'da onaylanan Eş danışman öneri formu üzerinde değişiklik yapılamaz.");
                }
                else if (esDanismanFormu.EYKYaGonderildi == true && esDanismanFormu.EYKDaOnaylandi != false)
                {
                    mMessage.Messages.Add("EYK'ya gönderilen Eş danışman öneri formu üzerinde değişiklik yapılamaz.");
                }

            }

            if (!mMessage.Messages.Any())
            {
                if (isDegisiklikTalebi)
                {
                    var lastEsDanisman = tdoBasvuruDanismanData.TDOBasvuru.TDOBasvuruDanismen.Where(p => p.TDOBasvuruEsDanismen.Any()).OrderByDescending(o => o.TDOBasvuruDanismanID).Select(s => s.TDOBasvuruEsDanismen.OrderByDescending(o => o.TDOBasvuruEsDanismanID).FirstOrDefault()).FirstOrDefault();
                    if (lastEsDanisman != null)
                    {
                        model.OncekiEsDanismanAdi = lastEsDanisman.UnvanAdi + " " + lastEsDanisman.AdSoyad;
                    }
                }
                if (tdoBasvuruEsDanismanId.HasValue)
                {
                    model = tdoBasvuruDanismanData.TDOBasvuruEsDanismen.First();
                }
                if (model == null) model = new TDOBasvuruEsDanisman { TDOBasvuruDanismanID = tdoBasvuruDanismanId, IsDegisiklikTalebi = isDegisiklikTalebi };
                view = ViewRenderHelper.RenderPartialView("TdoBasvuru", "TdoEsDanismanFormu", model);
                mMessage.IsSuccess = true;
            }
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
        public ActionResult TdoEsDanismanFormuPost(TDOBasvuruEsDanisman kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Eş Danışmanı Öneri Formu Oluşturma İşlemi"
            };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var yYetki = RoleNames.TdoEykdaOnayYetkisi.InRoleCurrent() || RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent();
            var tdoBasvuruDanismanData = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);

            if (!formYetki || (!yYetki && tdoBasvuruDanismanData.TezDanismanID != UserIdentity.Current.Id))
            {
                mMessage.Messages.Add("Tez Eş Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            else if (!IslemYapilabilir(
                         tdoBasvuruDanismanData.EYKYaGonderildi,
                         tdoBasvuruDanismanData.EYKYaHazirlandi,
                         tdoBasvuruDanismanData.EYKDaOnaylandi
                     ))
            {
                mMessage.Messages.Add("EYK'süreci devam eden danışman formu bulunduğundan eş danışman öneri ya da düzeltme işlemi yapılamaz.");
            }
            else if (kModel.TDOBasvuruEsDanismanID > 0)
            {
                var esDanismanFormu = tdoBasvuruDanismanData.TDOBasvuruEsDanismen.First(p => p.UniqueID == kModel.UniqueID);
                if (esDanismanFormu.EYKDaOnaylandi == true)
                {
                    mMessage.Messages.Add("EYK'da onaylanan Eş danışman öneri formu üzerinde değişiklik yapılamaz.");
                }
                else if (esDanismanFormu.EYKYaGonderildi == true)
                {
                    mMessage.Messages.Add("EYK'ya gönderilen Eş danışman öneri formu üzerinde değişiklik yapılamaz.");
                }
            }

            if (!mMessage.Messages.Any())
            {
                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(tdoBasvuruDanismanData.TDOBasvuru.OgrenciNo);
                if (!ogrenciBilgi.KayitVar)
                {
                    mMessage.Messages.Add(tdoBasvuruDanismanData.TDOBasvuru.OgrenciNo + " öğrenci numaranıza ait OBS isteminde aktif bir öğrenim bilgisine rastlanmadı. " + ogrenciBilgi.HataMsj);
                }
                else if (ogrenciBilgi.DanismanInfo == null && kModel.TDOBasvuruEsDanismanID <= 0)
                {
                    mMessage.Messages.Add(tdoBasvuruDanismanData.TDOBasvuru.OgrenciNo + " öğrenci numarasına ait OBS isteminde aktif bir danışman bilgisine rastlanmadı. Eş danışman önerisi yapılabilmesi için OBS sisteminde danışman bilgisinin tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                }
            }
            if (!mMessage.Messages.Any())
            {

                if (kModel.AdSoyad.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Ad Soyad giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.AdSoyad.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "AdSoyadX" });
                if (kModel.UnvanAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Ünvan giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.UnvanAdi.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "UnvanAdi" });
                if (kModel.UniversiteAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Üniversite giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.UniversiteAdi.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "UniversiteAdi" });
                if (kModel.AnabilimDaliAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Anabilim dalı adı giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.AnabilimDaliAdi.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "AnabilimDaliAdi" });

                if (kModel.ProgramAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Program adı giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.ProgramAdi.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "ProgramAdi" });
                if (kModel.EMail.IsNullOrWhiteSpace())
                {

                    mMessage.Messages.Add("EMail bilgisini giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EMail" });
                }
                else if (!kModel.EMail.ToIsValidEmail())
                {
                    mMessage.Messages.Add("Mail formatı uygun değil.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EMail" });
                }
                else
                {
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EMail" });
                }
                if (kModel.Gerekce.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Gerekçe giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.Gerekce.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "Gerekce" });
            }

            if (!mMessage.Messages.Any())
            {
                var ogrenciObsBilgi = KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBasvuruDanismanData.TDOBasvuru.KullaniciID);
                if (!ogrenciObsBilgi.IsDanismanHesabiBulunamadi)
                {
                    if (ogrenciObsBilgi.DanismanInfo.E_POSTA1.ToLower().Trim() == tdoBasvuruDanismanData.Kullanicilar.EMail.ToLower().Trim())
                    {
                        kModel.TDAdSoyad = ogrenciObsBilgi.DanismanInfo.AD + " " + ogrenciObsBilgi.DanismanInfo.SOYAD;
                        kModel.TDUnvanAdi = ogrenciObsBilgi.DanismanInfo.UNVAN_AD;
                        kModel.TDAnabilimDaliAdi = ogrenciObsBilgi.DanismanInfo.ANABILIMDALI_AD;
                        kModel.TDProgramAdi = ogrenciObsBilgi.DanismanInfo.PROGRAM_AD;
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {
                var sendMail = false;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.IslemTarihi = DateTime.Now;
                kModel.BasvuruTarihi = DateTime.Now;
                kModel.TDOBasvuruDanismanID = tdoBasvuruDanismanData.TDOBasvuruDanismanID;
                var insertOrUpdate = false;
                TDOBasvuruEsDanisman tdoBasvuruEsDanis;
                var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                while (_entities.TDOBasvuruEsDanismen.Any(a => a.FormKodu == formKodu))
                {
                    formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                }
                kModel.UniqueID = Guid.NewGuid();
                kModel.FormKodu = formKodu;
                if (kModel.TDOBasvuruEsDanismanID > 0)
                {
                    tdoBasvuruEsDanis = tdoBasvuruDanismanData.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == kModel.TDOBasvuruEsDanismanID);
                    if (tdoBasvuruEsDanis.AdSoyad != kModel.AdSoyad || tdoBasvuruEsDanis.UnvanAdi != kModel.UnvanAdi || tdoBasvuruEsDanis.UniversiteAdi != kModel.UniversiteAdi
                        || tdoBasvuruEsDanis.AnabilimDaliAdi != kModel.AnabilimDaliAdi || tdoBasvuruEsDanis.ProgramAdi != kModel.ProgramAdi || tdoBasvuruEsDanis.EMail != kModel.EMail || tdoBasvuruEsDanis.Gerekce != kModel.Gerekce)

                    {
                        sendMail = true;

                    }
                    tdoBasvuruEsDanis.OncekiEsDanismanAdi = kModel.OncekiEsDanismanAdi;
                    tdoBasvuruEsDanis.BasvuruTarihi = kModel.BasvuruTarihi;
                    tdoBasvuruEsDanis.AdSoyad = kModel.AdSoyad;
                    tdoBasvuruEsDanis.UnvanAdi = kModel.UnvanAdi;
                    tdoBasvuruEsDanis.UniversiteAdi = kModel.UniversiteAdi;
                    tdoBasvuruEsDanis.AnabilimDaliAdi = kModel.AnabilimDaliAdi;
                    tdoBasvuruEsDanis.ProgramAdi = kModel.ProgramAdi;
                    tdoBasvuruEsDanis.TDAdSoyad = kModel.TDAdSoyad;
                    tdoBasvuruEsDanis.TDUnvanAdi = kModel.TDUnvanAdi;
                    tdoBasvuruEsDanis.TDAnabilimDaliAdi = kModel.TDAnabilimDaliAdi;
                    tdoBasvuruEsDanis.TDProgramAdi = kModel.TDProgramAdi;
                    tdoBasvuruEsDanis.EMail = kModel.EMail;
                    tdoBasvuruEsDanis.Gerekce = kModel.Gerekce;
                    tdoBasvuruEsDanis.IslemTarihi = kModel.IslemTarihi;
                    tdoBasvuruEsDanis.IslemYapanID = kModel.IslemYapanID;
                    tdoBasvuruEsDanis.IslemYapanIP = kModel.IslemYapanIP;
                    tdoBasvuruEsDanis.FormKodu = kModel.FormKodu;
                    tdoBasvuruEsDanis.UniqueID = kModel.UniqueID;
                    tdoBasvuruEsDanis.EYKYaGonderildi = null;
                    tdoBasvuruEsDanis.EYKYaGonderildiIslemYapanID = null;
                    tdoBasvuruEsDanis.EYKYaGonderildiIslemTarihi = null;
                    tdoBasvuruEsDanis.EYKDaOnaylandi = null;
                    tdoBasvuruEsDanis.EYKDaOnaylandiIslemYapanID = null;
                    tdoBasvuruEsDanis.EYKDaOnaylandiOnayTarihi = null;

                }
                else
                {
                    sendMail = true;
                    insertOrUpdate = true;
                    tdoBasvuruEsDanis = _entities.TDOBasvuruEsDanismen.Add(kModel);
                }
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    LogIslemleri.LogEkle("TDOBasvuruEsDanisman", insertOrUpdate ? LogCrudType.Insert : LogCrudType.Update, tdoBasvuruEsDanis.ToJson());
                    TdoBus.SendMailTdoEsBilgisi(tdoBasvuruEsDanis.TDOBasvuruEsDanismanID);
                }



            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }


        public ActionResult TdoEykYaGonderimPostEs(int tdoBasvuruEsDanismanId, bool? eykYaGonderildi, string eykYaGonderimDurumAciklamasi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Eş Danışmanı Öneri Formu EYK'ya Gönderim İşlemi"
            };

            var esDanismanTalepTipIds = new List<int>() { TdoDanismanTalepTipEnum.TezDanismaniOnerisi, TdoDanismanTalepTipEnum.TezDanismaniDegisikligi, TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi };
            var tdoBasvuruEsDanis =
                _entities.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
            if (!RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya gönderme yetkiniz bulunmamaktadır.");
            }
            else if (esDanismanTalepTipIds.Contains(tdoBasvuruEsDanis.TDOBasvuruDanisman.TDODanismanTalepTipID) && tdoBasvuruEsDanis.TDOBasvuruDanisman.EYKDaOnaylandi != true)
            {
                mMessage.Messages.Add("Tez danışmanı öneri formu EYK'da onaylanmadığından Tez Eş Danışman EYK'ya gönderim işlemi yapılamaz.");
            }
            else if (tdoBasvuruEsDanis.EYKYaHazirlandi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'ya hazırlanma işlemi yapılan Tez Eş Danışman öneri formu üzerinden EYK'ya gönderim işlemi yapılamaz.");
            }
            else if (eykYaGonderildi == false && eykYaGonderimDurumAciklamasi.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("EYK'ya gönderilmeme sebebi açıklaması giriniz.");
            }
            if (!mMessage.Messages.Any())
            {


                tdoBasvuruEsDanis.EYKYaGonderildi = eykYaGonderildi;
                tdoBasvuruEsDanis.EYKYaGonderildiIslemTarihi = DateTime.Now;
                tdoBasvuruEsDanis.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruEsDanis.EYKYaGonderimDurumAciklamasi = tdoBasvuruEsDanis.EYKYaGonderildi == false ? eykYaGonderimDurumAciklamasi : "";
                tdoBasvuruEsDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruEsDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruEsDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                LogIslemleri.LogEkle("TDOBasvuruEsDanisman", LogCrudType.Update, tdoBasvuruEsDanis.ToJson());
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }
        public ActionResult TdoEykYaHazirlaPostEs(int tdoBasvuruEsDanismanId, bool? eykYaHazirlandi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Danışmanı Öneri Formu EYK'ya Hazırlama İşlemi"
            };
            var tdoBasvuruEsDanis = _entities.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
            if (!RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya hazırlama yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruEsDanis.EYKYaGonderildi != true)
            {
                mMessage.Messages.Add("Enstitü tarafından Eyk'ya gönderim işlemi yapılmayan Eş Danışman öneri formu üzerinden EYK'ya hazırlandı işlemi yapılamaz.");
            }
            else if (tdoBasvuruEsDanis.EYKDaOnaylandi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'da onaylandı işlemi yapılan Eş Danışman öneri formu üzerinden EYK'ya hazırlandı işlemi yapılamaz.");
            }

            if (!mMessage.Messages.Any())
            {
                if (tdoBasvuruEsDanis.EYKYaGonderildi.HasValue || eykYaHazirlandi != false)
                {
                    // eykya gönderimi onay işlemi gördü yada yeni onay durumu onaylanmadı değil ise öğrencinin aktiflik durumunu kontrol et
                    var ogrenciObsBilgi =
                        KullanicilarBus.OgrenciBilgisiGuncelleObs(tdoBasvuruEsDanis.TDOBasvuruDanisman.TDOBasvuru.KullaniciID);

                    if (!ogrenciObsBilgi.KayitVar)
                    {
                        mMessage.Messages.Add(
                            "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                    }
                    else if (tdoBasvuruEsDanis.TDOBasvuruDanisman.TDOBasvuru.OgrenciNo != ogrenciObsBilgi.OgrenciInfo.OGR_NO)
                    {
                        mMessage.Messages.Add(
                            "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir.");
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {

                tdoBasvuruEsDanis.EYKYaHazirlandi = eykYaHazirlandi;
                tdoBasvuruEsDanis.EYKYaHazirlandiIslemTarihi = DateTime.Now;
                tdoBasvuruEsDanis.EYKYaHazirlandiIslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruEsDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruEsDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruEsDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruEsDanisman", LogCrudType.Update, tdoBasvuruEsDanis.ToJson());
                mMessage.IsSuccess = true;
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }


        public ActionResult TdoEykDaOnayPostEs(int tdoBasvuruEsDanismanId, bool? eykDaOnaylandi, DateTime? eykDaOnaylandiOnayTarihi, string eykDaOnaylanmadiDurumAciklamasi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Eş Danışmanı Öneri Formu EYK'Da Onay İşlemi"
            };
            var tdoBasvuruEsDanis = _entities.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
            if (!RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya gönderme yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruEsDanis.TDOBasvuruDanisman.EYKDaOnaylandi != true)
            {
                mMessage.Messages.Add("Tez danışmanı öneri formu EYK'da onaylanmadığından Tez Eş Danışman EYK'da onay işlemi yapılamaz.");
            }
            else if (tdoBasvuruEsDanis.EYKYaHazirlandi != true)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'ya hazırlanma işlemi yapılmayan Eş Danışman öneri formu üzerinden EYK onay işlemi yapılamaz.");
            }
            if (eykDaOnaylandi == false)
            {
                if (eykDaOnaylanmadiDurumAciklamasi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Onaylanmama durumu için açıklama giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Error, PropertyName = "EYKDaOnaylanmadiDurumAciklamasi_" + tdoBasvuruEsDanismanId });

                }
            }
            else if (eykDaOnaylandi == true)
            {
                if (!eykDaOnaylandiOnayTarihi.HasValue)
                {
                    mMessage.Messages.Add("EYK'Da onaylanma tarihini giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Error, PropertyName = "EYKDaOnaylandiOnayTarihiEs_" + tdoBasvuruEsDanismanId });

                }
            }
            if (!mMessage.Messages.Any())
            {
                var sendMail = eykDaOnaylandi.HasValue && eykDaOnaylandi != tdoBasvuruEsDanis.EYKDaOnaylandi;
                tdoBasvuruEsDanis.EYKDaOnaylandi = eykDaOnaylandi;
                if (eykDaOnaylandi == true) tdoBasvuruEsDanis.EYKDaOnaylandiOnayTarihi = eykDaOnaylandiOnayTarihi.Value;
                tdoBasvuruEsDanis.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                if (eykDaOnaylandi == false)
                {
                    if (eykDaOnaylanmadiDurumAciklamasi.Trim() != tdoBasvuruEsDanis.EYKDaOnaylanmadiDurumAciklamasi.Trim()) sendMail = true;
                    tdoBasvuruEsDanis.EYKDaOnaylanmadiDurumAciklamasi = eykDaOnaylanmadiDurumAciklamasi;
                }
                tdoBasvuruEsDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruEsDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruEsDanis.IslemYapanIP = UserIdentity.Ip;
                // TDOBasvuruDanis.TDOBasvuru.Kullanicilar.DanismanID = TDOBasvuruDanis.TezDanismanID;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    LogIslemleri.LogEkle("TDOBasvuruEsDanisman", LogCrudType.Update, tdoBasvuruEsDanis.ToJson());
                    TdoBus.SendMailTdoEsEykOnay(tdoBasvuruEsDanismanId, eykDaOnaylandi.Value);
                }
                mMessage.Messages.Add("Eyk da onay durum bilgisi güncellendi" + (sendMail ? " ve ilgili kişilere mail gönderildi." : "."));
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }


        public ActionResult GetTdoDanismans(string term)
        {
            //17-Doç.42-Dr Prof.Dr, 73-Dr. Öğr. Üye ,5-arş gör dr, 66-öğr gör dr,
            var danismanUnvanIDs = new List<int>() { 17, 42, 73 };
            var danismanlar = _entities.Kullanicilars.Where(p => p.KullaniciTipID == KullaniciTipiEnum.AkademikPersonel && danismanUnvanIDs.Contains(p.UnvanID ?? 0) && (p.Ad + " " + p.Soyad).StartsWith(term)).OrderBy(o => o.Ad).ThenBy(t => t.Soyad).Take(25).Select(s => new
            {
                id = s.KullaniciID,
                AdSoyad = s.Ad + " " + s.Soyad,
                text = s.Ad + " " + s.Soyad,
                s.Birimler.BirimAdi,
                s.Unvanlar.UnvanAdi

            }).ToList();

            return danismanlar.ToJsonResult();
        }
        public ActionResult DetaySil(int id, int tdoBasvuruDanismanId)
        {
            var mmMessage = new MmMessage
            {
                Title = "Tez Danışman/Dil/Başlık öneri başvurusu silme işlemi"
            };
            var tdoDanismanOnayYetkisi = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var tdoeyKyaGonderimYetkisi = RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent();
            var qKayit = _entities.TDOBasvuruDanismen.Where(p => p.TDOBasvuruID == id && p.TDOBasvuruDanismanID == tdoBasvuruDanismanId).AsQueryable();
            if (!tdoDanismanOnayYetkisi && !tdoeyKyaGonderimYetkisi) qKayit = qKayit.Where(p => p.TDOBasvuru.KullaniciID == UserIdentity.Current.Id);
            else if (tdoDanismanOnayYetkisi && !tdoeyKyaGonderimYetkisi) qKayit = qKayit.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            var tdoBasvuruDanisman = qKayit.FirstOrDefault();


            if (tdoBasvuruDanisman == null)
            {
                mmMessage.Messages.Add("Silinmek istenen kayıt sistemde bulunamadı.");
            }
            else if (tdoBasvuruDanismanId != tdoBasvuruDanisman.TDOBasvuru.AktifTDOBasvuruDanismanID && (tdoBasvuruDanisman.VarolanDanismanOnayladi.HasValue || tdoBasvuruDanisman.DanismanOnayladi.HasValue))
            {
                mmMessage.Messages.Add("Silmek istediğiniz danışman öneri formu danışman tarafından işlemi gördüğünden silme işlemi yapılamaz.");
            }
            else
            {
                try
                {
                    tdoBasvuruDanisman.TDOBasvuru.AktifTDOBasvuruDanismanID = tdoBasvuruDanisman.TDOBasvuru.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID != tdoBasvuruDanismanId).OrderByDescending(o => o.TDOBasvuruDanismanID).Select(s => s.TDOBasvuruDanismanID).FirstOrDefault().ToNullIntZero();

                    if (tdoBasvuruDanisman.IsObsData && tdoBasvuruDanisman.TDOBasvuruEsDanismen.All(a => a.IsObsData))
                    {
                        _entities.TDOBasvuruEsDanismen.RemoveRange(tdoBasvuruDanisman.TDOBasvuruEsDanismen);
                    }
                    _entities.TDOBasvuruDanismen.Remove(tdoBasvuruDanisman);
                    _entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    LogIslemleri.LogEkle("TDOBasvuruDanisman", LogCrudType.Delete, tdoBasvuruDanisman.ToJson());
                    mmMessage.Messages.Add(tdoBasvuruDanisman.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Danışman Öneri Formu sistemden silindi.");

                }
                catch (Exception ex)
                {
                    mmMessage.Messages.Add(tdoBasvuruDanisman.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Danışman Öneri Formu sistemden silinemedi.");
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult DetaySilEs(int id, Guid uniqueId)
        {
            var mmMessage = new MmMessage
            {
                Title = "Tez Eş Danışman öneri başvurusu silme işlemi"
            };
            var tdoeyKdaOnayYetkisi = RoleNames.TdoEykdaOnayYetkisi.InRoleCurrent();
            var tdoeyKyaGonderimYetkisi = RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent();
            var tdoEsDanisman = _entities.TDOBasvuruEsDanismen.FirstOrDefault(p => p.UniqueID == uniqueId);


            if (!(tdoeyKdaOnayYetkisi || tdoeyKyaGonderimYetkisi))
            {
                mmMessage.Messages.Add("Eş danışman bilgisini silmeye yetkili değilsiniz.");
            }
            if (tdoEsDanisman == null)
            {
                mmMessage.Messages.Add("Silinmek istenen kayıt sistemde bulunamadı.");
            }
            else if (tdoEsDanisman.EYKYaGonderildi.HasValue)
            {
                mmMessage.Messages.Add("Silmek istediğiniz eş danışman öneri formu eyk'ya gönderim işlemi gördüğünden silinemez.");
            }
            else
            {
                try
                {
                    _entities.TDOBasvuruEsDanismen.Remove(tdoEsDanisman);
                    _entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    LogIslemleri.LogEkle("TDOBasvuruEsDanisman", LogCrudType.Delete, tdoEsDanisman.ToJson());
                    mmMessage.Messages.Add(tdoEsDanisman.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Eş Danışman Öneri Formu sistemden silindi.");

                }
                catch (Exception ex)
                {
                    mmMessage.Messages.Add(tdoEsDanisman.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Eş Danışman Öneri Formu sistemden silinemedi.");
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Sil(int id)
        {
            var mmMessage = TdoBus.GetTdoBasvuruSilKontrol(id);

            if (mmMessage.IsSuccess)
            {
                var kayit = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == id);

                var isAdminRemove = false;
                if (UserIdentity.Current.IsAdmin)
                {
                    if (kayit.TDOBasvuruDanismen.All(a => a.EYKDaOnaylandi != true))
                    {
                        isAdminRemove = true;
                    }
                }

                try
                {

                    mmMessage.Title = "Uyarı";
                    if (isAdminRemove)
                    {
                        var araRapors = kayit.TDOBasvuruDanismen.ToList();
                        foreach (var item in araRapors)
                        {
                            if (item.TDOBasvuruEsDanismen.Any()) _entities.TDOBasvuruEsDanismen.RemoveRange(item.TDOBasvuruEsDanismen);
                            _entities.TDOBasvuruDanismen.Remove(item);
                        }
                        _entities.TDOBasvurus.Remove(kayit);
                    }
                    else
                    {
                        _entities.TDOBasvurus.Remove(kayit);
                    }
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("TdoBasvuru", LogCrudType.Delete, kayit.ToJson());

                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}