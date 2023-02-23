using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TdoBasvuruController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();

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

            var kullaniciAdinaBasvuruYetki = RoleNames.KullaniciAdinaTezDanismanOnerisiYap.InRoleCurrent();
            var gelenBasvuruDuzeltmeYetki = RoleNames.TdoGelenBasvuruKayit.InRoleCurrent();

            if (kullaniciAdinaBasvuruYetki || gelenBasvuruDuzeltmeYetki)
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
            var kullKayitB = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(model.KullaniciID.Value);
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
                    bbModel.KullaniciTipYetkiYokMsj = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Profil bilgilerinizde giriş yaptığınız YTU Lüsansüstü Öreğnci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";
                }
                else
                {

                    if ((kul.OgrenimTipKod.IsDoktora() || kul.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans) && kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                    {
                        bbModel.KullaniciTipYetki = true;
                        var donemBilgi = _entities.Donemlers.FirstOrDefault(p => p.DonemID == kul.KayitDonemID.Value);
                        if (donemBilgi != null)
                        {
                            bbModel.KayitDonemi = kul.KayitYilBaslangic + "/" + (kul.KayitYilBaslangic + 1);
                        }
                        if (kul.KayitTarihi.HasValue) bbModel.KayitDonemi += " " + kul.KayitTarihi.ToString("dd.MM.yyyy");
                        model.AktifOgrenimIcinBasvuruVar = _entities.TDOBasvurus.Any(a => a.KullaniciID == kul.KullaniciID && a.OgrenimTipKod == kul.OgrenimTipKod && a.ProgramKod == kul.ProgramKod);
                    }
                    else
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "Tez danışmanı öneri başvurusu yapılabilmesi için Doktora,Bütünleşik Doktora veya Tezli YL öğrencisi olunması gerekmektedir.";

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
                bbModel.KullaniciTipYetkiYokMsj = "Profil bilgilerinizde YTU Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için Tez İzleme başvurusu yapamazsınız. Sağ üst köşeden profil bilgilerinizi düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
            }



            bbModel.Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
            bbModel.Kullanici = kul;

            #endregion
            var nowDate = DateTime.Now;
            var q = from s in _entities.TDOBasvurus
                    join en in _entities.Enstitulers on s.EnstituKod equals en.EnstituKod
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, en.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on pr.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join ktip in _entities.KullaniciTipleris on k.KullaniciTipID equals ktip.KullaniciTipID
                    join ard in _entities.TDOBasvuruDanismen on s.AktifTDOBasvuruDanismanID equals ard.TDOBasvuruDanismanID into defard
                    from ard in defard.DefaultIfEmpty()
                    let ardEs = _entities.TDOBasvuruEsDanismen.FirstOrDefault(p => p.TDOBasvuruDanismanID == ard.TDOBasvuruDanismanID)
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
                        AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        TcKimlikNo = s.Kullanicilar.TcKimlikNo,
                        OgrenciNo = s.OgrenciNo,
                        Kullanicilar = s.Kullanicilar,
                        ResimAdi = s.Kullanicilar.ResimAdi,
                        KullaniciTipID = s.Kullanicilar.KullaniciTipID,
                        KullaniciTipAdi = ktip.KullaniciTipAdi,
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
                        EYKYaGonderildi = ard != null ? ard.EYKYaGonderildi : null,
                        EYKDaOnaylandi = ard != null ? ard.EYKDaOnaylandi : null,
                        EsDanismanOnerisiVar = ardEs != null,
                        Es_EYKYaGonderildi = ardEs != null ? ardEs.EYKYaGonderildi : null,
                        Es_EYKDaOnaylandi = ardEs != null ? ardEs.EYKDaOnaylandi : null,


                    };

            if (model.TDOBasvuruID.HasValue) q = q.Where(p => p.TDOBasvuruID == model.TDOBasvuruID.Value);
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            //IndexModel.Toplam = model.RowCount;
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            var ps = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = ps.PageIndex;

            var qdata = q.Skip(ps.StartRowIndex).Take(model.PageSize).ToList();

            model.TdoBasvuruDtos = qdata;

            ViewBag.IndexModel = indexModel;

            ViewBag.bModel = bbModel;
            return View(model);
        }

        [Authorize]
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

            var mmMessage = TezDanismanOneriBus.GetAktifTezDanismanOneriSurecKontrol(enstituKod, kullaniciId, tdoBasvuruId);


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
                    model.KullaniciTipID = basvuru.KullaniciTipID;
                    model.ResimAdi = basvuru.ResimAdi;
                    model.Ad = basvuru.Ad;
                    model.Soyad = basvuru.Soyad;
                    model.OgrenciNo = basvuru.OgrenciNo;
                    model.TcKimlikNo = basvuru.TcKimlikNo; 
                    model.UyrukKod = basvuru.UyrukKod;
                    model.OgrenimTipKod = basvuru.OgrenimTipKod;



                }
                else
                {
                    model.EnstituKod = enstituKod;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = kullaniciId.Value;
                    model.KullaniciTipID = kul.KullaniciTipID;
                    model.ResimAdi = kul.ResimAdi;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.TcKimlikNo = kul.TcKimlikNo; 
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


        [Authorize]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BasvuruYap(KmTDOBasvuru kModel, string ekd)
        {
            if (RoleNames.TdoGelenBasvuruKayit.InRoleCurrent() == false) { kModel.KullaniciID = UserIdentity.Current.Id; }
            var mmMessage = TezDanismanOneriBus.GetAktifTezDanismanOneriSurecKontrol(kModel.EnstituKod, kModel.KullaniciID, kModel.TDOBasvuruID.ToNullIntZero()); 
           
            if (mmMessage.Messages.Count == 0)
            {
                var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID); 
                kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;
                kModel.ResimAdi = kul.ResimAdi;
                kModel.KullaniciTipID = kul.KullaniciTipID;
                kModel.KayitOgretimYiliBaslangic = kul.KayitYilBaslangic;
                kModel.KayitOgretimYiliDonemID = kul.KayitDonemID;
                kModel.KayitTarihi = kul.KayitTarihi;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;
                kModel.OgrenciNo = kul.OgrenciNo;
                kModel.OgrenimDurumID = kul.OgrenimDurumID.Value;
                kModel.ProgramKod = kul.ProgramKod;
                kModel.Ad = kul.Ad;
                kModel.Soyad = kul.Soyad; 
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
                        KullaniciTipID = kModel.KullaniciTipID,
                        ResimAdi = kModel.ResimAdi,
                        Ad = kModel.Ad,
                        Soyad = kModel.Soyad,
                        UyrukKod = kModel.UyrukKod,
                        TcKimlikNo = kModel.TcKimlikNo, 
                        OgrenciNo = kModel.OgrenciNo,
                        OgrenimDurumID = kModel.OgrenimDurumID,
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
                    TezDanismanOneriBus.ObsDanismanBasvurBilgiEslestir(data.KullaniciID, data.TDOBasvuruID);
                }
                else
                {

                    data = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == kModel.TDOBasvuruID);
                    data.EnstituKod = kModel.EnstituKod;
                    data.BasvuruTarihi = kModel.BasvuruTarihi;
                    data.KullaniciID = kModel.KullaniciID;
                    data.KullaniciTipID = kModel.KullaniciTipID;
                    data.ResimAdi = kModel.ResimAdi;
                    data.Ad = kModel.Ad;
                    data.Soyad = kModel.Soyad;
                    data.UyrukKod = kModel.UyrukKod;
                    data.TcKimlikNo = kModel.TcKimlikNo; 
                    data.OgrenciNo = kModel.OgrenciNo;
                    data.OgrenimDurumID = kModel.OgrenimDurumID;
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
                LogIslemleri.LogEkle("TDOBasvuru", isNewRecord ? IslemTipi.Insert : IslemTipi.Update, data.ToJson());

                return RedirectToAction("Index", new { data.TDOBasvuruID });
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

            ViewBag._MmMessage = mmMessage;
            return View(kModel);
        }




        [Authorize]
        public ActionResult GetTdoDanismanFormu(int tdoBasvuruId, int? tdoBasvuruDanismanId, bool? isCopy, int? tdoDanismanTalepTipId)
        {

            tdoBasvuruDanismanId = tdoBasvuruDanismanId ?? 0;
            var model = new KmTDOBasvuruDanisman() { TDOBasvuruID = tdoBasvuruId, isCopy = isCopy, TDODanismanTalepTipID = tdoDanismanTalepTipId ?? TDODanismanTalepTip.TezDanismaniOnerisi };
            var mMessage = new MmMessage();
            string view = "";
            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.FirstOrDefault(p => p.TDOBasvuruID == tdoBasvuruId && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            if (tdoBas == null) return null;
            model.OgrenciAdSoyad = tdoBas.Ad + " " + tdoBas.Soyad + "-" + tdoBas.OgrenciNo;
            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            if (tdoBasvuruDanismanId > 0 && isCopy != true && tdoBas.TDOBasvuruDanismen.Any(a => a.TDOBasvuruDanismanID == tdoBasvuruDanismanId && a.DanismanOnayladi == true))
            {
                mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
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
                    model.OgrenciAdSoyad = tdoBas.Ad + " " + tdoBas.Soyad + "-" + tdoBas.OgrenciNo;
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
                    model.TDSinavTipID = tdoBd.TDSinavTipID;
                    model.TDSinavAdi = tdoBd.TDSinavAdi;
                    model.TDSinavPuani = tdoBd.TDSinavPuani;
                    model.TDSinavYili = tdoBd.TDSinavYili;
                    model.TDOgrenciSayisiDR = tdoBd.TDOgrenciSayisiDR;
                    model.TDOgrenciSayisiYL = tdoBd.TDOgrenciSayisiYL;
                    model.TDTezSayisiDR = tdoBd.TDTezSayisiDR;
                    model.TDTezSayisiYL = tdoBd.TDTezSayisiYL;
                    if (isCopy == true)
                    {
                        model.TDOBasvuruDanismanID = 0;
                        if (model.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi)
                        {
                            model.YeniTezBaslikTr = null;
                            model.YeniTezBaslikEn = null;
                            model.VarolanTezDanismanID = null;
                        }
                        else if (model.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi)
                        {
                            model.VarolanTezDanismanID = tdoBd.TezDanismanID;
                            model.TDAnabilimDaliID = null;
                            model.TDProgramKod = null;
                            model.TezDanismanID = 0;
                        }
                        else if (model.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
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
                model.SListTDoDanismanTalepTip = new SelectList(TezDanismanOneriBus.CmbTdoDanismanTalepTip(model.TDODanismanTalepTipID > TDODanismanTalepTip.TezDanismaniOnerisi, false), "Value", "Caption", model.TDODanismanTalepTipID);
                model.SListSinav = new SelectList(Management.cmbGetAktifSinavlar(tdoBas.EnstituKod, SinavTipGrup.DilSinavlari, true), "Value", "Caption", model.SinavTipID);
                model.SListTDAnabilimDali = new SelectList(Management.cmbGetAktifAnabilimDallari(tdoBas.EnstituKod, true), "Value", "Caption", model.TDAnabilimDaliID);
                model.SListTDProgram = new SelectList(Management.cmbGetAktifProgramlar(true, model.TDAnabilimDaliID), "Value", "Caption", model.TDProgramKod);
                var dilSinavList = Management.cmbGetAktifSinavlar(tdoBas.EnstituKod, SinavTipGrup.DilSinavlari, true);
                dilSinavList.Insert(1, new CmbIntDto { Value = -1, Caption = "Yurt Dışı Doktora veya %100 İngilizce Eğitim Veren Üniversite Mezunu" });
                model.SListTDSinav = new SelectList(dilSinavList, "Value", "Caption", model.TDSinavTipID);

                if (model.SinavTipID.HasValue)
                {
                    var sinav = _entities.SinavTipleris.First(p => p.SinavTipID == model.SinavTipID);
                    if (sinav.OzelNot) model.SListSinavNot = new SelectList(Management.cmbGetSinavTipOzelNot(model.SinavTipID.Value, true), "Value", "Caption", model.SinavPuani);
                    if (model.TDSinavTipID != -1 && model.TDSinavTipID.HasValue)
                    {
                        var sinavTd = _entities.SinavTipleris.First(p => p.SinavTipID == model.TDSinavTipID);
                        if (sinavTd.OzelNot) model.SListTDSinavNot = new SelectList(Management.cmbGetSinavTipOzelNot(model.TDSinavTipID.Value, true), "Value", "Caption", model.TDSinavPuani);
                    }

                }
                mMessage.MessageType = Msgtype.Information;
                mMessage.IsSuccess = true;
                view = ViewRenderHelper.RenderPartialView("TDOBasvuru", "TDODanismanFormu", model);


            }


            if (mMessage.MessageType != Msgtype.Information) mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = view,
                Messages = strView
            }.ToJsonResult();

        }
        [ValidateInput(false)]
        public ActionResult TdoDanismanFormuPost(TDOBasvuruDanisman kModel, bool? isTezDiliTr)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                Title = "Tez Danışmanı Öneri Formu Oluşturma İşlemi"
            };
            var formYetki = RoleNames.TdoFormOlusturmaYetkisi.InRoleCurrent();
            var tdoBas = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == kModel.TDOBasvuruID && p.KullaniciID == (formYetki ? p.KullaniciID : UserIdentity.Current.Id));
            var kullKayitB = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(tdoBas.KullaniciID);


            if (!UserIdentity.Current.IsAdmin && !formYetki && tdoBas.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Tez Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            if (kModel.TDOBasvuruDanismanID > 0)
            {

                if (tdoBas.TDOBasvuruDanisman.TDODanismanTalepTipID > TDODanismanTalepTip.TezDanismaniOnerisi)
                {
                    if (tdoBas.TDOBasvuruDanisman.VarolanDanismanOnayladi == true)
                    {
                        mMessage.Messages.Add("Varolan Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                    }
                }
                else
                {
                    if (tdoBas.TDOBasvuruDanisman.DanismanOnayladi == true)
                    {
                        mMessage.Messages.Add("Tez danışmanı tarafından onaylanan danışman öneri formları düzeltilemez.");
                    }
                }
            }
            else
            {
                if (kModel.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniOnerisi && kullKayitB.KayitVar && !kullKayitB.OgrenciInfo.DANISMAN_TC1.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Obs sisteminde aktif olarak bir danışmanınız bulunmaktadır. Tekrar danışman başvurusu yapılamaz.");
                    mMessage.Messages.Add("Obs Sisteminde Gözüken Danışman: " + $"{kullKayitB.OgrenciInfo.DANISMAN_UNVAN1} {kullKayitB.OgrenciInfo.DANISMAN_AD_SOYAD1}");
                }
                if (tdoBas.TDOBasvuruDanisman != null && tdoBas.TDOBasvuruDanisman.DanismanOnayladi.HasValue)
                {
                    mMessage.Messages.Add("Süreci devam eden bir Tez danışmanı öneri formunuz bulunmaktadır. Yeni bir Tez danışmanı öneri işlemi yapamazsınız.");
                }
            }
            if (!mMessage.Messages.Any())
            {

                if (kModel.TDODanismanTalepTipID != TDODanismanTalepTip.TezDanismaniDegisikligi)
                {
                    if (!isTezDiliTr.HasValue)
                    {
                        mMessage.Messages.Add("Tez dili seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (isTezDiliTr.HasValue ? Msgtype.Success : Msgtype.Warning), PropertyName = "IsTezDiliTr" });
                }

                if (kModel.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniOnerisi)
                {
                    if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Tez başlığını türkçe olarak giriniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikTr.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "TezBaslikTr" });
                    if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Tez başlığını ingilizce olarak giriniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikEn.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "TezBaslikEn" });
                }
                if (kModel.TDODanismanTalepTipID != TDODanismanTalepTip.TezDanismaniDegisikligi && kModel.TDODanismanTalepTipID != TDODanismanTalepTip.TezDanismaniOnerisi)
                {
                    if (kModel.YeniTezBaslikTr.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Yeni Tez başlığını türkçe olarak giriniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.YeniTezBaslikTr.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "YeniTezBaslikTr" });
                    if (kModel.YeniTezBaslikEn.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Yeni Tez başlığını ingilizce olarak giriniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.YeniTezBaslikEn.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "YeniTezBaslikEn" });
                }
                if (kModel.TDODanismanTalepTipID != TDODanismanTalepTip.TezBasligiDegisikligi)
                {
                    if (kModel.TezDanismanID <= 0)
                    {
                        mMessage.Messages.Add("Tez danışmanınızı seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TezDanismanID > 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "TezDanismanID" });

                    if (kModel.TDAnabilimDaliID > 0 == false)
                    {
                        mMessage.Messages.Add("Tez Danışmanı Anabilim dalı bilgisini seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDAnabilimDaliID > 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDAnabilimDaliID" });
                    if (kModel.TDProgramKod.IsNullOrWhiteSpace())
                    {
                        mMessage.Messages.Add("Tez Danışmanı program bilgisini seçiniz.");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TDProgramKod.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDProgramKod" });
                    if (isTezDiliTr == false)
                    {
                        if (!kModel.TDSinavTipID.HasValue) mMessage.Messages.Add("Tez danışmanı yabancı dil yeterlilik sınav bilginizi seçiniz.");
                        else if (kModel.TDSinavTipID.HasValue)
                        {
                            if (kModel.TDSinavTipID == -1)
                            {
                                if (kModel.TDUniversiteAdi.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("Yurt Dışı Doktora veya %100 İngilizce Eğitim Veren Üniversite Mezunu Sınav Tipi için Üniversite Adı giriniz.");
                                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Error, PropertyName = "TDUniversiteAdi" });
                                }
                                else
                                {
                                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TDUniversiteAdi" });
                                }
                            }
                            else
                            {

                                if (!kModel.TDSinavYili.HasValue || kModel.TDSinavYili <= 0) mMessage.Messages.Add("Tez danışmanı yabancı dil yeterlilik sınav yılı bilginizi giriniz.");
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDSinavYili.HasValue && kModel.TDSinavYili > 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDSinavYili" });
                                if (!kModel.TDSinavPuani.ToDouble().HasValue)
                                    mMessage.Messages.Add("Tez danışmanı yabancı dil yeterlilik sınav puanı bilginizi giriniz.");
                                else
                                {

                                    var tdSinavPuani = kModel.TDSinavPuani.ToDouble().Value;

                                    var sinavTipKriterMin = TdoAyar.DanismanMinSinavPuanKabulKriter.GetAyarTdo(tdoBas.EnstituKod).ToDouble();
                                    if (sinavTipKriterMin != null)
                                    {
                                        var sinavPuaniUygun = (tdSinavPuani >= sinavTipKriterMin && tdSinavPuani <= 100);
                                        if (!sinavPuaniUygun)
                                        {
                                            mMessage.Messages.Add("Tez danışmanı Yabancı dil yeterlilik sınav puanı en az " + sinavTipKriterMin + " en fazla " + 100 + " olmalıdır.");
                                        }
                                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = sinavPuaniUygun ? Msgtype.Success : Msgtype.Warning, PropertyName = "TDSinavPuani" });
                                    }
                                    else
                                    {
                                        mMessage.Messages.Add("Tez danışmanı Yabancı dil sınav yeterlilik puan kriterleri tanımlı değil. Bu bilgiyi Enstitüye iletiniz.");
                                    }

                                }
                            }
                        }
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDSinavTipID.HasValue ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDSinavTipID" });
                    }
                }
                if (kModel.TDODanismanTalepTipID != TDODanismanTalepTip.TezDanismaniDegisikligi)
                {
                    if (isTezDiliTr == false)
                    {
                        if (!kModel.SinavTipID.HasValue) mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav bilgisini seçiniz.");
                        if (!kModel.SinavYili.HasValue || kModel.SinavYili <= 0) mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav yılı bilginizi giriniz.");
                        if (kModel.SinavPuani.IsNullOrWhiteSpace() || !kModel.SinavPuani.ToDouble().HasValue)
                        {
                            mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav puanı bilgisini giriniz.");
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SinavPuani" });
                        }
                        else
                        {

                            if (kModel.SinavTipID.HasValue)
                            {
                                var sinavTipi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID);
                                var sinavPuani = kModel.SinavPuani.ToDouble().Value;
                                var sinavTipKriter = sinavTipi.SinavTipleriOTNotAraliklaris.FirstOrDefault(p => p.OgrenimTipKod == tdoBas.OgrenimTipKod && p.Ingilizce == tdoBas.Programlar.Ingilizce);
                                if (sinavTipKriter != null)
                                {
                                    var sinavPuaniUygun = (sinavPuani >= sinavTipKriter.Min && sinavPuani <= sinavTipKriter.Max);
                                    if (!sinavPuaniUygun)
                                    {
                                        mMessage.Messages.Add("Öğrenci Yabancı dil yeterlilik sınav puanı en az " + sinavTipKriter.Min + " en fazla " + sinavTipKriter.Max + " olmalıdır.");
                                    }
                                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = sinavPuaniUygun ? Msgtype.Success : Msgtype.Warning, PropertyName = "SinavPuani" });
                                }
                                else
                                {
                                    mMessage.Messages.Add("Öğrenci Yabancı dil sınav yeterlilik puan kriterleri tanımlı değil. Bu bilgiyi Enstitüye iletiniz.");
                                }
                            }

                        }

                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.SinavTipID.HasValue ? Msgtype.Success : Msgtype.Warning), PropertyName = "SinavTipID" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.SinavYili.HasValue && kModel.SinavYili > 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "SinavYili" });


                        if (!kModel.TDSinavTipID.HasValue) mMessage.Messages.Add("Tez danışmanı yabancı dil yeterlilik sınav bilginizi seçiniz.");
                        else if (kModel.TDSinavTipID.HasValue)
                        {
                            if (kModel.TDSinavTipID == -1)
                            {
                                if (kModel.TDUniversiteAdi.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("Yurt Dışı Doktora veya %100 İngilizce Eğitim Veren Üniversite Mezunu Sınav Tipi için Üniversite Adı giriniz.");
                                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Error, PropertyName = "TDUniversiteAdi" });
                                }
                                else
                                {
                                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TDUniversiteAdi" });
                                }
                            }
                            else
                            {

                                if (!kModel.TDSinavYili.HasValue || kModel.TDSinavYili <= 0) mMessage.Messages.Add("Tez danışmanı yabancı dil yeterlilik sınav yılı bilginizi giriniz.");
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDSinavYili.HasValue && kModel.TDSinavYili > 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDSinavYili" });
                                if (!kModel.TDSinavPuani.ToDouble().HasValue)
                                    mMessage.Messages.Add("Tez danışmanı yabancı dil yeterlilik sınav puanı bilginizi giriniz.");
                                else
                                {

                                    var tdSinavPuani = kModel.TDSinavPuani.ToDouble().Value;

                                    var sinavTipKriterMin = TdoAyar.DanismanMinSinavPuanKabulKriter.GetAyarTdo(tdoBas.EnstituKod).ToDouble();
                                    if (sinavTipKriterMin != null)
                                    {
                                        var sinavPuaniUygun = (tdSinavPuani >= sinavTipKriterMin && tdSinavPuani <= 100);
                                        if (!sinavPuaniUygun)
                                        {
                                            mMessage.Messages.Add("Tez danışmanı Yabancı dil yeterlilik sınav puanı en az " + sinavTipKriterMin + " en fazla " + 100 + " olmalıdır.");
                                        }
                                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = sinavPuaniUygun ? Msgtype.Success : Msgtype.Warning, PropertyName = "TDSinavPuani" });
                                    }
                                    else
                                    {
                                        mMessage.Messages.Add("Tez danışmanı Yabancı dil sınav yeterlilik puan kriterleri tanımlı değil. Bu bilgiyi Enstitüye iletiniz.");
                                    }

                                }
                            }
                        }
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDSinavTipID.HasValue ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDSinavTipID" });
                    }
                }


            }
            if (!mMessage.Messages.Any())
            {


                if (kModel.TDODanismanTalepTipID != TDODanismanTalepTip.TezDanismaniOnerisi)
                {
                    var oncekiBasvuru = tdoBas.TDOBasvuruDanisman;

                    if (kModel.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                    {
                        kModel.VarolanTezDanismanID = oncekiBasvuru.TezDanismanID;
                        kModel.VarolanTDAdSoyad = oncekiBasvuru.TDAdSoyad;
                        kModel.VarolanTDUnvanAdi = oncekiBasvuru.TDUnvanAdi;
                        kModel.VarolanTDAnabilimDaliAdi = oncekiBasvuru.TDAnabilimDaliAdi;
                        kModel.VarolanTDProgramAdi = oncekiBasvuru.TDProgramAdi;
                        kModel.TezBaslikTr = oncekiBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi || oncekiBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi ? oncekiBasvuru.YeniTezBaslikTr : oncekiBasvuru.TezBaslikTr;
                        kModel.TezBaslikEn = oncekiBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi || oncekiBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi ? oncekiBasvuru.YeniTezBaslikEn : oncekiBasvuru.TezBaslikEn;
                    }
                    else if (kModel.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi)
                    {
                        kModel.VarolanTezDanismanID = oncekiBasvuru.TezDanismanID;
                        kModel.VarolanTDAdSoyad = oncekiBasvuru.TDAdSoyad;
                        kModel.VarolanTDUnvanAdi = oncekiBasvuru.TDUnvanAdi;
                        kModel.VarolanTDAnabilimDaliAdi = oncekiBasvuru.TDAnabilimDaliAdi;
                        kModel.VarolanTDProgramAdi = oncekiBasvuru.TDProgramAdi;
                        kModel.IsTezDiliTr = oncekiBasvuru.IsTezDiliTr;
                        kModel.TezBaslikTr = oncekiBasvuru.TezBaslikTr;
                        kModel.TezBaslikEn = oncekiBasvuru.TezBaslikEn;
                        kModel.YeniTezBaslikTr = oncekiBasvuru.YeniTezBaslikTr;
                        kModel.YeniTezBaslikEn = oncekiBasvuru.YeniTezBaslikEn;
                        kModel.SinavTipID = oncekiBasvuru.SinavTipID;
                        kModel.SinavAdi = oncekiBasvuru.SinavAdi;
                        kModel.SinavYili = oncekiBasvuru.SinavYili;
                        kModel.SinavPuani = oncekiBasvuru.SinavPuani;
                    }
                    else if (kModel.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi)
                    {
                        kModel.VarolanTezDanismanID = oncekiBasvuru.TezDanismanID;
                        kModel.VarolanTDAdSoyad = oncekiBasvuru.TDAdSoyad;
                        kModel.VarolanTDUnvanAdi = oncekiBasvuru.TDUnvanAdi;
                        kModel.VarolanTDAnabilimDaliAdi = oncekiBasvuru.TDAnabilimDaliAdi;
                        kModel.VarolanTDProgramAdi = oncekiBasvuru.TDProgramAdi;

                        kModel.TezDanismanID = oncekiBasvuru.TezDanismanID;
                        kModel.IsTezDiliTr = oncekiBasvuru.IsTezDiliTr;
                        kModel.TezBaslikTr = oncekiBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi || oncekiBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi ? oncekiBasvuru.YeniTezBaslikTr : oncekiBasvuru.TezBaslikTr;
                        kModel.TezBaslikEn = oncekiBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi || oncekiBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi ? oncekiBasvuru.YeniTezBaslikEn : oncekiBasvuru.TezBaslikEn;
                        kModel.TDAdSoyad = oncekiBasvuru.TDAdSoyad;
                        kModel.TDUnvanAdi = oncekiBasvuru.TDUnvanAdi;
                        kModel.TDAnabilimDaliID = oncekiBasvuru.TDAnabilimDaliID;
                        kModel.TDAnabilimDaliAdi = oncekiBasvuru.TDAnabilimDaliAdi;
                        kModel.TDProgramKod = oncekiBasvuru.TDProgramKod;
                        kModel.TDProgramAdi = oncekiBasvuru.TDProgramAdi;
                        kModel.TDOgrenciSayisiYL = oncekiBasvuru.TDOgrenciSayisiYL;
                        kModel.TDOgrenciSayisiDR = oncekiBasvuru.TDOgrenciSayisiDR;
                        kModel.TDTezSayisiYL = oncekiBasvuru.TDTezSayisiYL;
                        kModel.TDTezSayisiDR = oncekiBasvuru.TDTezSayisiDR;
                        kModel.TDOgrenciSayisiDR = oncekiBasvuru.TDOgrenciSayisiDR;
                        kModel.TDOgrenciSayisiDR = oncekiBasvuru.TDOgrenciSayisiDR;
                    }
                }


            }
            if (!mMessage.Messages.Any())
            {
                var sendMail = false;
                var isYeni = false;
                var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.TezDanismanID);
                if (isTezDiliTr == true)
                {
                    kModel.SinavAdi = null;
                    kModel.SinavPuani = null;
                    kModel.SinavYili = null;
                    kModel.TDSinavAdi = null;
                    kModel.TDSinavPuani = null;
                    kModel.TDSinavYili = null;
                    kModel.TDUniversiteAdi = null;
                }

                kModel.BasvuruTarihi = DateTime.Now;
                var donemBilgi = kModel.BasvuruTarihi.ToAraRaporDonemBilgi();
                kModel.DonemBaslangicYil = donemBilgi.BaslangicYil;
                kModel.DonemID = donemBilgi.DonemID;
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
                if (kModel.IsTezDiliTr == false)
                {
                    if (kModel.SinavTipID.HasValue) kModel.SinavAdi = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID).SinavAdi;
                    if (kModel.TDSinavTipID.HasValue)
                    {
                        kModel.TDSinavAdi = kModel.TDSinavTipID > 0 ? _entities.SinavTipleris.First(p => p.SinavTipID == kModel.TDSinavTipID).SinavAdi : "Yurt Dışı Doktora veya %100 İngilizce Eğitim Veren Üniversite Mezunu";
                    }
                }

                if (kModel.TDOBasvuruDanismanID > 0)
                {
                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);
                    if (

                        tdoBasvuruDanis.IsTezDiliTr != isTezDiliTr.Value ||
                        tdoBasvuruDanis.TezBaslikEn != kModel.TezBaslikEn ||
                        tdoBasvuruDanis.TezBaslikTr != kModel.TezBaslikTr ||
                        tdoBasvuruDanis.SinavTipID != kModel.SinavTipID ||
                        tdoBasvuruDanis.SinavPuani != kModel.SinavPuani ||
                        tdoBasvuruDanis.SinavYili != kModel.SinavYili ||
                        tdoBasvuruDanis.TezDanismanID != kModel.TezDanismanID ||
                        tdoBasvuruDanis.TDAdSoyad != kModel.TDAdSoyad ||
                        tdoBasvuruDanis.TDUnvanAdi != kModel.TDUnvanAdi ||
                        tdoBasvuruDanis.TDAnabilimDaliID != kModel.TDAnabilimDaliID ||
                        tdoBasvuruDanis.TDProgramKod != kModel.TDProgramKod ||
                        tdoBasvuruDanis.TDSinavTipID != kModel.TDSinavTipID ||
                        tdoBasvuruDanis.TDSinavPuani != kModel.TDSinavPuani ||
                        tdoBasvuruDanis.TDSinavYili != kModel.TDSinavYili ||
                        tdoBasvuruDanis.TDUniversiteAdi != kModel.TDUniversiteAdi ||
                         tdoBasvuruDanis.VarolanTezDanismanID != kModel.VarolanTezDanismanID ||
                    tdoBasvuruDanis.VarolanTDAdSoyad != kModel.VarolanTDAdSoyad ||
                    tdoBasvuruDanis.VarolanTDUnvanAdi != kModel.VarolanTDUnvanAdi ||
                    tdoBasvuruDanis.VarolanTDAnabilimDaliAdi != kModel.VarolanTDAnabilimDaliAdi ||
                    tdoBasvuruDanis.VarolanTDProgramAdi != kModel.VarolanTDProgramAdi
                    )
                    {
                        if (tdoBasvuruDanis.TezDanismanID != kModel.TezDanismanID && tdoBasvuruDanis.DanismanOnayladi == false)
                        {
                            tdoBasvuruDanis.DanismanOnayladi = null;
                            tdoBasvuruDanis.DanismanOnayTarihi = null;
                            tdoBasvuruDanis.DanismanOnaylanmadiAciklama = null;
                            tdoBasvuruDanis.TDOgrenciSayisiDR = null;
                            tdoBasvuruDanis.TDOgrenciSayisiYL = null;
                            tdoBasvuruDanis.TDTezSayisiDR = null;
                            tdoBasvuruDanis.TDTezSayisiYL = null;

                        }
                        if (UserIdentity.Current.IsAdmin && (tdoBasvuruDanis.EYKDaOnaylandi.HasValue || tdoBasvuruDanis.EYKYaGonderildi.HasValue || tdoBasvuruDanis.DanismanOnayladi.HasValue))
                        {
                            sendMail = false;
                        }
                        else sendMail = true;
                        tdoBasvuruDanis.BasvuruTarihi = kModel.BasvuruTarihi;
                        tdoBasvuruDanis.DonemBaslangicYil = kModel.DonemBaslangicYil;
                        tdoBasvuruDanis.DonemID = kModel.DonemID;
                        tdoBasvuruDanis.FormKodu = kModel.FormKodu;
                        tdoBasvuruDanis.UniqueID = kModel.UniqueID;
                        tdoBasvuruDanis.IsTezDiliTr = isTezDiliTr.Value;
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
                        tdoBasvuruDanis.TDSinavTipID = kModel.TDSinavTipID;
                        tdoBasvuruDanis.TDSinavAdi = kModel.TDSinavAdi;
                        tdoBasvuruDanis.TDSinavPuani = kModel.TDSinavPuani;
                        tdoBasvuruDanis.TDSinavYili = kModel.TDSinavYili;
                        tdoBasvuruDanis.IslemTarihi = kModel.IslemTarihi;
                        tdoBasvuruDanis.TDUniversiteAdi = kModel.TDUniversiteAdi;
                        tdoBasvuruDanis.IslemYapanID = kModel.IslemYapanID;
                        tdoBasvuruDanis.IslemYapanIP = kModel.IslemYapanIP;
                    }
                }
                else
                {

                    isYeni = true;
                    tdoBasvuruDanis = _entities.TDOBasvuruDanismen.Add(kModel);
                    sendMail = true;
                    tdoBas.AktifTDOBasvuruDanismanID = kModel.TDOBasvuruDanismanID;
                }
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", isYeni ? IslemTipi.Insert : IslemTipi.Update, tdoBasvuruDanis.ToJson());

                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    TezDanismanOneriBus.SendMailTdoBilgisi(kModel.TDOBasvuruDanismanID);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return mMessage.ToJsonResult();
        }
        [Authorize]
        public ActionResult GetProgramlar(int tdAnabilimDaliId)
        {
            var bolm = Management.cmbGetAktifProgramlar(true, tdAnabilimDaliId);
            return bolm.Select(s => new { s.Value, s.Caption }).ToJsonResult();
        }
        public ActionResult GetSinavTip(int tdoBasvuruId, int sinavTipId)
        {
            var sinav = _entities.SinavTipleris.First(p => p.SinavTipID == sinavTipId);

            var notlar = new List<CmbDoubleDto>();
            if (sinav.OzelNot) notlar = Management.cmbGetSinavTipOzelNot(sinavTipId, true);
            return new { sinav.OzelNot, Notlar = notlar }.ToJsonResult();
        }
        [Authorize]
        public ActionResult TdoDanismanFormu()
        {
            return View();
        }

        [Authorize]
        public ActionResult TDODanismanOnayPost(TDOBasvuruDanisman kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                Title = "Tez Danışmanı Öneri Formu Danışman Onay İşlemi"
            };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);
            if (!RoleNames.TdoeyKdaOnayYetkisi.InRoleCurrent() && (!formYetki || tdoBasvuruDanis.TezDanismanID != UserIdentity.Current.Id))
            {
                mMessage.Messages.Add("Danışman onayı yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruDanis.EYKYaGonderildi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'ya gönderim işlemi yapılan Danışman öneri formu üzerinden herhangi bir işlemi yapılamaz.");
            }
            else if (kModel.DanismanOnayladi == false)
            {
                if (kModel.DanismanOnaylanmadiAciklama.IsNullOrWhiteSpace()) mMessage.Messages.Add("Onaylanmama durumu için açıklama giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.DanismanOnaylanmadiAciklama.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "DanismanOnaylanmadiAciklama_" + kModel.TDOBasvuruDanismanID });
            }

            if (!mMessage.Messages.Any() && kModel.DanismanOnayladi == true)
            {
                if (!kModel.TDOgrenciSayisiYL.HasValue || kModel.TDOgrenciSayisiYL < 0) mMessage.Messages.Add("Yüksek lisans öğrenci sayısı bilgisini giriniz.");
                if (!kModel.TDTezSayisiYL.HasValue || kModel.TDTezSayisiYL < 0) mMessage.Messages.Add("Yüksek lisans tez sayısı bilgisini giriniz.");
                if (!kModel.TDOgrenciSayisiDR.HasValue || kModel.TDOgrenciSayisiDR < 0) mMessage.Messages.Add("Doktora öğrenci sayısı bilgisini giriniz.");
                if (!kModel.TDTezSayisiDR.HasValue || kModel.TDTezSayisiDR < 0) mMessage.Messages.Add("Doktora tez sayısı bilgisini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDOgrenciSayisiYL.HasValue && kModel.TDOgrenciSayisiYL >= 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDOgrenciSayisiYL_" + kModel.TDOBasvuruDanismanID });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDTezSayisiYL.HasValue && kModel.TDTezSayisiYL >= 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDTezSayisiYL_" + kModel.TDOBasvuruDanismanID });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDOgrenciSayisiDR.HasValue && kModel.TDOgrenciSayisiDR >= 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDOgrenciSayisiDR_" + kModel.TDOBasvuruDanismanID });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.TDTezSayisiDR.HasValue && kModel.TDTezSayisiDR >= 0 ? Msgtype.Success : Msgtype.Warning), PropertyName = "TDTezSayisiDR_" + kModel.TDOBasvuruDanismanID });
                if (!mMessage.Messages.Any())
                {
                    var danismanOgrenciKriterMax = TdoAyar.DanismanMaxOgrenciKayitKriter.GetAyarTdo(tdoBasvuruDanis.TDOBasvuru.EnstituKod).ToDouble();

                    if (tdoBasvuruDanis.TDOBasvuru.OgrenimTipKod == OgrenimTipi.Doktra)
                    {
                        if (kModel.TDTezSayisiDR == 0 && kModel.TDTezSayisiYL == 0)
                        {
                            mMessage.Messages.Add("Doktora Öğrenim seviyesinde danışman atama formu oluşturulabilmesi için danışman mezun yükü 0 dan büyük olmalıdır.");
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TDTezSayisiDR_" + kModel.TDOBasvuruDanismanID });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TDTezSayisiYL_" + kModel.TDOBasvuruDanismanID });
                        }
                        else
                        {
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TDTezSayisiDR_" + kModel.TDOBasvuruDanismanID });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TDTezSayisiYL_" + kModel.TDOBasvuruDanismanID });
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
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TDOgrenciSayisiDR_" + kModel.TDOBasvuruDanismanID });
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TDOgrenciSayisiYL_" + kModel.TDOBasvuruDanismanID });
                            }
                            else
                            {
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TDOgrenciSayisiDR_" + kModel.TDOBasvuruDanismanID });
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TDOgrenciSayisiYL_" + kModel.TDOBasvuruDanismanID });
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


                tdoBasvuruDanis.DanismanOnayladi = kModel.DanismanOnayladi;
                tdoBasvuruDanis.DanismanOnayTarihi = DateTime.Now;
                tdoBasvuruDanis.DanismanOnaylanmadiAciklama = kModel.DanismanOnaylanmadiAciklama;
                tdoBasvuruDanis.TDTezSayisiDR = kModel.TDTezSayisiDR;
                tdoBasvuruDanis.TDTezSayisiYL = kModel.TDTezSayisiYL;
                tdoBasvuruDanis.TDOgrenciSayisiDR = kModel.TDOgrenciSayisiDR;
                tdoBasvuruDanis.TDOgrenciSayisiYL = kModel.TDOgrenciSayisiYL;
                tdoBasvuruDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    LogIslemleri.LogEkle("TDOBasvuruDanisman", IslemTipi.Update, tdoBasvuruDanis.ToJson());
                    TezDanismanOneriBus.SendMailTdoDanismanOnay(kModel.TDOBasvuruDanismanID, kModel.DanismanOnayladi == true);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return mMessage.ToJsonResult();
        }
        [Authorize]
        public ActionResult TDOVarolanDanismanOnayPost(TDOBasvuruDanisman kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                Title = "Tez Danışmanı Öneri Formu Danışman Onay İşlemi"
            };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);
            if (!RoleNames.TdoeyKdaOnayYetkisi.InRoleCurrent() && (!formYetki || tdoBasvuruDanis.VarolanTezDanismanID != UserIdentity.Current.Id))
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
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.VarolanDanismanOnaylanmadiAciklama.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "VarolanDanismanOnaylanmadiAciklama_" + kModel.TDOBasvuruDanismanID });
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
                    LogIslemleri.LogEkle("TDOBasvuruDanisman", IslemTipi.Update, tdoBasvuruDanis.ToJson());
                    TezDanismanOneriBus.SendMailTdoDanismanOnay(kModel.TDOBasvuruDanismanID, tdoBasvuruDanis.VarolanDanismanOnayladi.Value);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return mMessage.ToJsonResult();
        }
        [Authorize]
        public ActionResult TdoEykYaGonderimPost(int tdoBasvuruDanismanId, bool? eykYaGonderildi)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                Title = "Tez Danışmanı Öneri Formu EYK'ya Gönderim İşlemi"
            };
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
            if (!RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent())
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
            if (!mMessage.Messages.Any())
            {

                tdoBasvuruDanis.EYKYaGonderildi = eykYaGonderildi;
                tdoBasvuruDanis.EYKYaGonderildiIslemTarihi = DateTime.Now;
                tdoBasvuruDanis.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("TDOBasvuruDanisman", IslemTipi.Update, tdoBasvuruDanis.ToJson());
                mMessage.IsSuccess = true;
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return mMessage.ToJsonResult();
        }
        [Authorize]
        public ActionResult TdoEykDaOnayPost(int tdoBasvuruDanismanId, bool? eykDaOnaylandi, DateTime? eykDaOnaylandiOnayTarihi, string eykDaOnaylanmadiDurumAciklamasi)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                Title = "Tez Danışmanı Öneri Formu EYK'Da Onay İşlemi"
            };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var tdoBasvuruDanis = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
            if (!RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent())
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
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Error, PropertyName = "EYKDaOnaylanmadiDurumAciklamasi_" + tdoBasvuruDanismanId });

                }
            }
            else if (eykDaOnaylandi == true)
            {
                if (!eykDaOnaylandiOnayTarihi.HasValue)
                {
                    mMessage.Messages.Add("EYK'Da onaylanma tarihini giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Error, PropertyName = "EYKDaOnaylandiOnayTarihi_" + tdoBasvuruDanismanId });

                }
            }
            if (!mMessage.Messages.Any())
            {

                var sendMail = eykDaOnaylandi.HasValue && eykDaOnaylandi != tdoBasvuruDanis.EYKDaOnaylandi;
                tdoBasvuruDanis.EYKDaOnaylandi = eykDaOnaylandi;
                if (eykDaOnaylandi == true) tdoBasvuruDanis.EYKDaOnaylandiOnayTarihi = eykDaOnaylandiOnayTarihi.Value;
                tdoBasvuruDanis.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                if (eykDaOnaylandi == false) tdoBasvuruDanis.EYKDaOnaylanmadiDurumAciklamasi = eykDaOnaylanmadiDurumAciklamasi;
                tdoBasvuruDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruDanis.IslemYapanIP = UserIdentity.Ip;
                // TDOBasvuruDanis.TDOBasvuru.Kullanicilar.DanismanID = TDOBasvuruDanis.TezDanismanID;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    LogIslemleri.LogEkle("TDOBasvuruDanisman", IslemTipi.Update, tdoBasvuruDanis.ToJson());
                    TezDanismanOneriBus.SendMailTdoEykOnay(tdoBasvuruDanismanId, eykDaOnaylandi.Value);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return mMessage.ToJsonResult();
        }


        [Authorize]
        public ActionResult GetTdoEsDanismanFormu(int tdoBasvuruDanismanId, int? tdoBasvuruEsDanismanId, bool isDegisiklikTalebi = false)
        {
            var mMessage = new MmMessage();
            var view = "";
            var model = new TDOBasvuruEsDanisman() { TDOBasvuruDanismanID = tdoBasvuruDanismanId, IsDegisiklikTalebi = isDegisiklikTalebi };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var yYetki = RoleNames.TdoeyKdaOnayYetkisi.InRoleCurrent() || RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent();
            var tdoBasvuruDanismanData = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
            if (tdoBasvuruEsDanismanId.HasValue) model = _entities.TDOBasvuruEsDanismen.FirstOrDefault(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
            if (!formYetki || (!yYetki && tdoBasvuruDanismanData.TezDanismanID != UserIdentity.Current.Id))
            {
                mMessage.Messages.Add("Tez Eş Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            else if (tdoBasvuruDanismanData.EYKDaOnaylandi != true)
            {
                mMessage.Messages.Add("EYK'da onaylanmayan Tez Danışmanı Formları için Eş Danışman Formu oluşturma işlemi yapılamaz.");
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
                if (isDegisiklikTalebi == true)
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
                view = ViewRenderHelper.RenderPartialView("TDOBasvuru", "TDOEsDanismanFormu", model);
                mMessage.IsSuccess = true;
            }
            if (mMessage.MessageType != Msgtype.Information) mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);

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
                MessageType = Msgtype.Success,
                Title = "Tez Danışmanı Öneri Formu Oluşturma İşlemi"
            };
            var formYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var yYetki = RoleNames.TdoeyKdaOnayYetkisi.InRoleCurrent() || RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent();
            var tdoBasvuruDanismanData = _entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == kModel.TDOBasvuruDanismanID);
            if (!formYetki || (!yYetki && tdoBasvuruDanismanData.TezDanismanID != UserIdentity.Current.Id))
            {
                mMessage.Messages.Add("Tez Eş Danışmanı Öneri Formu oluşturmaya yetkili değilsiniz.");
            }
            else if (tdoBasvuruDanismanData.EYKDaOnaylandi != true)
            {
                mMessage.Messages.Add("EYK'da onaylanmayan Tez Danışmanı Formları için Eş Danışman Formu oluşturma işlemi yapılamaz.");
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

                if (kModel.AdSoyad.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Ad Soyad giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.AdSoyad.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "AdSoyadX" });
                if (kModel.UnvanAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Ünvan giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.UnvanAdi.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "UnvanAdi" });
                if (kModel.UniversiteAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Üniversite giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.UniversiteAdi.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "UniversiteAdi" });
                if (kModel.AnabilimDaliAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Anabilim dalı adı giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.AnabilimDaliAdi.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "AnabilimDaliAdi" });

                if (kModel.ProgramAdi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Program adı giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.ProgramAdi.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "ProgramAdi" });
                if (kModel.EMail.IsNullOrWhiteSpace())
                {

                    mMessage.Messages.Add("EMail bilgisini giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else if (kModel.EMail.ToIsValidEmail())
                {
                    mMessage.Messages.Add("Mail formatı uygun değil.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else
                {
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EMail" });
                }
                if (kModel.Gerekce.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Gerekçe giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.Gerekce.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "Gerekce" });
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
                        tdoBasvuruEsDanis.OncekiEsDanismanAdi = kModel.OncekiEsDanismanAdi;
                        tdoBasvuruEsDanis.BasvuruTarihi = kModel.BasvuruTarihi;
                        tdoBasvuruEsDanis.AdSoyad = kModel.AdSoyad;
                        tdoBasvuruEsDanis.UnvanAdi = kModel.UnvanAdi;
                        tdoBasvuruEsDanis.UniversiteAdi = kModel.UniversiteAdi;
                        tdoBasvuruEsDanis.AnabilimDaliAdi = kModel.AnabilimDaliAdi;
                        tdoBasvuruEsDanis.ProgramAdi = kModel.ProgramAdi;
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
                    else tdoBasvuruDanismanData.TDOBasvuruEsDanismen.Add(kModel);

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
                    LogIslemleri.LogEkle("TDOBasvuruEsDanisman", insertOrUpdate ? IslemTipi.Insert : IslemTipi.Update, tdoBasvuruEsDanis.ToJson());
                    TezDanismanOneriBus.SendMailTdoEsBilgisi(tdoBasvuruEsDanis.TDOBasvuruEsDanismanID);
                }



            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return mMessage.ToJsonResult();
        }

        [Authorize]
        public ActionResult TdoEykYaGonderimPostEs(int tdoBasvuruEsDanismanId, bool? eykYaGonderildi)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                Title = "Tez Eş Danışmanı Öneri Formu EYK'ya Gönderim İşlemi"
            };
            var tdoBasvuruEsDanis =
                _entities.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
            if (!RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya gönderme yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruEsDanis.TDOBasvuruDanisman.EYKDaOnaylandi != true)
            {
                mMessage.Messages.Add("Tez danışmanı öneri formu EYK'da onaylanmadığından Tez Eş Danışman EYK'ya gönderim işlemi yapılamaz.");
            }
            else if (tdoBasvuruEsDanis.EYKDaOnaylandi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'da onaylandı işlemi yapılan Tez Eş Danışman öneri formu üzerinden EYK'ya gönderim işlemi yapılamaz.");
            }
            if (!mMessage.Messages.Any())
            {


                tdoBasvuruEsDanis.EYKYaGonderildi = eykYaGonderildi;
                tdoBasvuruEsDanis.EYKYaGonderildiIslemTarihi = DateTime.Now;
                tdoBasvuruEsDanis.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruEsDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruEsDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruEsDanis.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                LogIslemleri.LogEkle("TDOBasvuruEsDanisman", IslemTipi.Update, tdoBasvuruEsDanis.ToJson());
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return mMessage.ToJsonResult();
        }
        [Authorize]
        public ActionResult TdoEykDaOnayPostEs(int tdoBasvuruEsDanismanId, bool? eykDaOnaylandi, DateTime? eykDaOnaylandiOnayTarihi, string eykDaOnaylanmadiDurumAciklamasi)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                Title = "Tez Eş Danışmanı Öneri Formu EYK'Da Onay İşlemi"
            };
            var tdoBasvuruEsDanis = _entities.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
            if (!RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya gönderme yetkiniz bulunmamaktadır.");
            }
            else if (tdoBasvuruEsDanis.TDOBasvuruDanisman.EYKDaOnaylandi != true)
            {
                mMessage.Messages.Add("Tez danışmanı öneri formu EYK'da onaylanmadığından Tez Eş Danışman EYK'da onay işlemi yapılamaz.");
            }
            else if (tdoBasvuruEsDanis.EYKYaGonderildi != true)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'ya gönderildi işlemi yapılmayan Eş Danışman öneri formu üzerinden EYK onay işlemi yapılamaz.");
            }
            if (eykDaOnaylandi == false)
            {
                if (eykDaOnaylanmadiDurumAciklamasi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Onaylanmama durumu için açıklama giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Error, PropertyName = "EYKDaOnaylanmadiDurumAciklamasi_" + tdoBasvuruEsDanismanId });

                }
            }
            else if (eykDaOnaylandi == true)
            {
                if (!eykDaOnaylandiOnayTarihi.HasValue)
                {
                    mMessage.Messages.Add("EYK'Da onaylanma tarihini giriniz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Error, PropertyName = "EYKDaOnaylandiOnayTarihiEs_" + tdoBasvuruEsDanismanId });

                }
            }
            if (!mMessage.Messages.Any())
            {
                var sendMail = eykDaOnaylandi.HasValue && eykDaOnaylandi != tdoBasvuruEsDanis.EYKDaOnaylandi;

                tdoBasvuruEsDanis.EYKDaOnaylandi = eykDaOnaylandi;
                if (eykDaOnaylandi == true) tdoBasvuruEsDanis.EYKDaOnaylandiOnayTarihi = eykDaOnaylandiOnayTarihi.Value;
                tdoBasvuruEsDanis.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                tdoBasvuruEsDanis.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                if (eykDaOnaylandi == false) tdoBasvuruEsDanis.EYKDaOnaylanmadiDurumAciklamasi = eykDaOnaylanmadiDurumAciklamasi;
                tdoBasvuruEsDanis.IslemTarihi = DateTime.Now;
                tdoBasvuruEsDanis.IslemYapanID = UserIdentity.Current.Id;
                tdoBasvuruEsDanis.IslemYapanIP = UserIdentity.Ip;
                // TDOBasvuruDanis.TDOBasvuru.Kullanicilar.DanismanID = TDOBasvuruDanis.TezDanismanID;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                if (sendMail)
                {
                    LogIslemleri.LogEkle("TDOBasvuruEsDanisman", IslemTipi.Update, tdoBasvuruEsDanis.ToJson());
                    TezDanismanOneriBus.SendMailTdoEsEykOnay(tdoBasvuruEsDanismanId, eykDaOnaylandi.Value);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return mMessage.ToJsonResult();
        }
        [Authorize]
        public ActionResult DetaySil(int id, int tdoBasvuruDanismanId)
        {
            var mmMessage = new MmMessage
            {
                Title = "Danışman Öneri Formu silme işlemi"
            };
            var tdoDanismanOnayYetkisi = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
            var tdoeyKyaGonderimYetkisi = RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent();
            var qKayit = _entities.TDOBasvuruDanismen.Where(p => p.TDOBasvuruID == id && p.TDOBasvuruDanismanID == tdoBasvuruDanismanId).AsQueryable();
            if (!tdoDanismanOnayYetkisi && !tdoeyKyaGonderimYetkisi) qKayit = qKayit.Where(p => p.TDOBasvuru.KullaniciID == UserIdentity.Current.Id);
            else if (tdoDanismanOnayYetkisi && !tdoeyKyaGonderimYetkisi) qKayit = qKayit.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            var tdoBasvuruDanisman = qKayit.FirstOrDefault();


            if (tdoBasvuruDanisman == null)
            {
                mmMessage.Messages.Add("Silinmek istenen kayıt sistemde bulunamadı.");
            }
            else if (tdoBasvuruDanisman.VarolanDanismanOnayladi.HasValue || tdoBasvuruDanisman.DanismanOnayladi.HasValue)
            {
                mmMessage.Messages.Add("Silmek istediğiniz danışman öneri formu danışman tarafından işlemi gördüğünden silme işlemi yapılamaz.");
            }
            else
            {
                try
                {
                    tdoBasvuruDanisman.TDOBasvuru.AktifTDOBasvuruDanismanID = tdoBasvuruDanisman.TDOBasvuru.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID != tdoBasvuruDanismanId).OrderByDescending(o => o.TDOBasvuruDanismanID).Select(s => s.TDOBasvuruDanismanID).FirstOrDefault().ToNullIntZero();
                    _entities.TDOBasvuruDanismen.Remove(tdoBasvuruDanisman);
                    _entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    LogIslemleri.LogEkle("TDOBasvuruDanisman", IslemTipi.Delete, tdoBasvuruDanisman.ToJson());
                    mmMessage.Messages.Add(tdoBasvuruDanisman.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Danışman Öneri Formu sistemden silindi.");

                }
                catch (Exception ex)
                {
                    mmMessage.Messages.Add(tdoBasvuruDanisman.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Danışman Öneri Formu sistemden silinemedi.");
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "TDOBasvuru/DetaySil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult DetaySilEs(int id, Guid uniqueID)
        {
            var mmMessage = new MmMessage
            {
                Title = "Danışman Eş Öneri Formu silme işlemi"
            };
            var tdoeyKdaOnayYetkisi = RoleNames.TdoeyKdaOnayYetkisi.InRoleCurrent();
            var tdoeyKyaGonderimYetkisi = RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent();
            var tdoEsDanisman = _entities.TDOBasvuruEsDanismen.FirstOrDefault(p => p.UniqueID == uniqueID);


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
                    LogIslemleri.LogEkle("TDOBasvuruEsDanisman", IslemTipi.Delete, tdoEsDanisman.ToJson());
                    mmMessage.Messages.Add(tdoEsDanisman.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Eş Danışman Öneri Formu sistemden silindi.");

                }
                catch (Exception ex)
                {
                    mmMessage.Messages.Add(tdoEsDanisman.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Eş Danışman Öneri Formu sistemden silinemedi.");
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "TDOBasvuru/DetaySilEs<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult Sil(int id)
        {
            var mmMessage = TezDanismanOneriBus.GetTdoBasvuruSilKontrol(id);

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
                    LogIslemleri.LogEkle("TDOBasvuru", IslemTipi.Delete, kayit.ToJson());

                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = Msgtype.Success;

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "TDOBasvuru/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}