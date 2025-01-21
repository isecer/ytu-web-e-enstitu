using BiskaUtil;
using Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.WebServiceData.ObsService;

namespace LisansUstuBasvuruSistemi.Controllers
{

    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TiBasvuruController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string ekd, int? tiBasvuruId, int? kullaniciId, Guid? isDegerlendirme = null)
        {
            if (!UserIdentity.Current.IsAuthenticated && isDegerlendirme == null) return RedirectToActionPermanent("Login", "Account");

            return Index(new FmTiBasvuru() { TIBasvuruID = tiBasvuruId, KullaniciID = kullaniciId, IsDegerlendirme = isDegerlendirme, PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTiBasvuru model, string ekd)
        {
            if (!UserIdentity.Current.IsAuthenticated && model.IsDegerlendirme == null) return RedirectToActionPermanent("Login", "Account");

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            #region bilgiModel
            var bbModel = new IndexPageInfoDto();
            if (model.IsDegerlendirme == null)
            {
                bbModel.SistemBasvuruyaAcik = TiAyar.TiBasvurusuAcikmi.GetAyarTi(enstituKod, "false").ToBoolean(false);

                if (model.KullaniciID.HasValue) model.KullaniciID = UserIdentity.Current.Id;
                model.KullaniciID = model.KullaniciID ?? UserIdentity.Current.Id;
                var kullKayitB = KullanicilarBus.OgrenciBilgisiGuncelleObs(model.KullaniciID.Value);
                var kul = _entities.Kullanicilars.First(p => p.KullaniciID == model.KullaniciID);

                if (kul.YtuOgrencisi)
                {
                    if (kullKayitB.KayitVar == false)
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Hesap bilgilerinizde bulunan YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";
                    }
                    else
                    {
                        if (kul.OgrenimTipKod.IsDoktora() && kul.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci)
                        {
                            bbModel.KullaniciTipYetki = true;
                            var donemBilgi = _entities.Donemlers.FirstOrDefault(p => p.DonemID == kul.KayitDonemID.Value);
                            if (donemBilgi != null)
                            {
                                bbModel.KayitDonemi = kul.KayitYilBaslangic + "/" + (kul.KayitYilBaslangic + 1);
                            }
                            if (kul.KayitTarihi.HasValue) bbModel.KayitDonemi += " " + kul.KayitTarihi.ToFormatDate();
                            model.AktifOgrenimIcinBasvuruVar = _entities.TIBasvurus.Any(a => a.KullaniciID == kul.KullaniciID && a.OgrenciNo == kul.OgrenciNo && a.ProgramKod == kul.ProgramKod);
                        }
                        else
                        {
                            bbModel.KullaniciTipYetki = false;
                            bbModel.KullaniciTipYetkiYokMsj = "Tez izleme ara rapor başvurusu yapılabilmesi için Doktora öğrencisi olunması gerekmektedir.";

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
                }
                else
                {
                    bbModel.KullaniciTipYetki = false;
                    bbModel.KullaniciTipYetkiYokMsj = "Hesap bilgilerinizde YTÜ Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için Tez İzleme başvurusu yapamazsınız. Sağ üst köşeden hesap bilgilerini düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
                }
                if (bbModel.KullaniciTipYetki)
                {
                    var otb = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);

                    bbModel.OgrenimDurumAdi = kul.OgrenimDurumlari.OgrenimDurumAdi;
                    bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                    bbModel.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    bbModel.ProgramAdi = kul.Programlar.ProgramAdi;
                    bbModel.OgrenciNo = kul.OgrenciNo;
                }


                bbModel.Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
                bbModel.Kullanici = kul;
            }
            #endregion

            var q = from s in _entities.TIBasvurus.Where(p => !model.IsDegerlendirme.HasValue || p.TIBasvuruAraRapors.Any(a => a.TIBasvuruAraRaporKomites.Any(a2 => a2.UniqueID == model.IsDegerlendirme)))
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on s.Programlar.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in _entities.Enstitulers on e.EnstituKod equals en.EnstituKod
                    join ard in _entities.TIBasvuruAraRapors on s.AktifTIBasvuruAraRaporID equals ard.TIBasvuruAraRaporID into defard
                    from ard in defard.DefaultIfEmpty()
                    where s.EnstituKod == enstituKod && s.KullaniciID == (model.IsDegerlendirme.HasValue ? s.KullaniciID : model.KullaniciID.Value)
                    select new FrTiBasvuru
                    {
                        TIBasvuruID = s.TIBasvuruID,
                        TezDanismanID = s.TezDanismanID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        KullaniciID = s.KullaniciID,
                        UserKey = k.UserKey,
                        AdSoyad = k.Ad + " " + k.Soyad,
                        OgrenciNo = s.OgrenciNo,
                        ResimAdi = k.ResimAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifTIBasvuruAraRaporID = s.AktifTIBasvuruAraRaporID,
                        TiAraRaporAktifDonemAdi = ard == null ? "Rapor Girişi Yapılmadı" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),
                        TIAraRaporRaporDurumAdi = ard == null ? "Rapor Girişi Yapılmadı" : ard.TIBasvuruAraRaporDurumlari.TIBasvuruAraRaporDurumAdi,
                        AraRaporSayisi = ard == null ? (int?)null : ard.AraRaporSayisi,
                        TIAraRaporAktifDonemID = ard == null ? null : (ard.DonemBaslangicYil + "" + ard.DonemID),
                        TIAraRaporRaporDurumID = ard == null ? 0 : ard.TIBasvuruAraRaporDurumID,
                        IsOyBirligiOrCoklugu = ard != null ? ard.IsOyBirligiOrCoklugu : null,
                        IsBasariliOrBasarisiz = ard != null ? ard.IsBasariliOrBasarisiz : null

                    };

            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.IndexModel = indexModel;

            ViewBag.bModel = bbModel;
            return View(model);
        }

        [Authorize]
        public ActionResult BasvuruYap(int? tiBasvuruId, int? kullaniciId, string ekd)
        {
            var model = new KmTiBasvuru();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);


            if (tiBasvuruId.HasValue || kullaniciId.HasValue)
            {
                if (kullaniciId.HasValue)
                    if (RoleNames.TiGelenBasvuruKayit.InRoleCurrent() == false)
                        kullaniciId = UserIdentity.Current.Id;
                if (tiBasvuruId.HasValue)
                {
                    var basvuru = _entities.TIBasvurus.First(p => p.TIBasvuruID == tiBasvuruId.Value);
                    if (kullaniciId.HasValue == false) kullaniciId = basvuru.KullaniciID;
                }
            }
            else
            {
                kullaniciId = UserIdentity.Current.Id;
            }
            var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(kullaniciId.Value);
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);

            var mmMessage = TiBus.GetAktifTezIzlemeSurecKontrol(enstituKod, kullaniciId, tiBasvuruId);

            if (model.TIBasvuruID <= 0 && mmMessage.IsSuccess)
            {
                var danismanTc = studentInfo.OgrenciInfo.DANISMAN_TC1;
                if (!kul.DanismanID.HasValue && (danismanTc.IsNullOrWhiteSpace() || danismanTc.Length != 11))
                {
                    mmMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    mmMessage.IsSuccess = false;
                }
                else if (!kul.DanismanID.HasValue)
                {

                    mmMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
                    mmMessage.IsSuccess = false;
                }
            }
            if (mmMessage.IsSuccess)
            {
                model.KayitTarihi = kul.KayitTarihi;
                if (tiBasvuruId.HasValue)
                {
                    var basvuru = _entities.TIBasvurus.First(p => p.TIBasvuruID == tiBasvuruId);
                    model.EnstituKod = basvuru.EnstituKod;
                    model.TIBasvuruID = basvuru.TIBasvuruID;
                    model.BasvuruTarihi = DateTime.Now;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.KullaniciID = basvuru.KullaniciID;
                    model.OgrenciNo = basvuru.OgrenciNo;
                    model.OgrenimTipKod = basvuru.OgrenimTipKod;



                }
                else
                {
                    model.EnstituKod = enstituKod;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = kullaniciId.Value;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.OgrenimTipKod = kul.OgrenimTipKod.Value;

                }
                model.OgrenimTipAdi = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;
                var progLng = kul.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;


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
        public ActionResult BasvuruYap(KmTiBasvuru kModel, string ekd)
        {
            if (RoleNames.TiGelenBasvuruKayit.InRoleCurrent() == false) { kModel.KullaniciID = UserIdentity.Current.Id; }
            var mmMessage = TiBus.GetAktifTezIzlemeSurecKontrol(kModel.EnstituKod, kModel.KullaniciID, kModel.TIBasvuruID.ToNullIntZero());

            KullanicilarBus.OgrenciBilgisiGuncelleObs(kModel.KullaniciID);
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
            kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;

            var danismanBilgi = new Kullanicilar();
            if (kModel.TIBasvuruID <= 0 && mmMessage.Messages.Count == 0 && danismanBilgi.KullaniciID <= 0)
            {
                if (!kul.DanismanID.HasValue)
                {
                    mmMessage.Messages.Add("Tez Danışmanınıza ait sistemde kullanıcı hesabı bilgisine rastlanmadı.");
                }

            }

            if (mmMessage.Messages.Count == 0)
            {
                kModel.BasvuruSonDonemSecilecekDersKodlari = TiAyar.TiSonDonemKayitOlunmasiGerekenDersKodlari.GetAyarTi(kModel.EnstituKod);
                kModel.KayitOgretimYiliBaslangic = kul.KayitYilBaslangic;
                kModel.KayitOgretimYiliDonemID = kul.KayitDonemID;
                kModel.KayitTarihi = kul.KayitTarihi;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;
                kModel.OgrenciNo = kul.OgrenciNo;
                kModel.ProgramKod = kul.ProgramKod;

                TIBasvuru data;
                var isNewRecord = false;
                if (kModel.TIBasvuruID <= 0)
                {
                    isNewRecord = true;
                    kModel.BasvuruTarihi = DateTime.Now;

                    data = _entities.TIBasvurus.Add(new TIBasvuru
                    {
                        EnstituKod = kModel.EnstituKod,
                        BasvuruSonDonemSecilecekDersKodlari = kModel.BasvuruSonDonemSecilecekDersKodlari,
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
                    data.TezDanismanID = danismanBilgi.KullaniciID;
                    _entities.SaveChanges();
                }
                else
                {

                    data = _entities.TIBasvurus.First(p => p.TIBasvuruID == kModel.TIBasvuruID);
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
                LogIslemleri.LogEkle("TiBasvuru", isNewRecord ? LogCrudType.Insert : LogCrudType.Update, data.ToJson());

                return RedirectToAction("Index", new { data.TIBasvuruID });
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

            ViewBag._MmMessage = mmMessage;
            return View(kModel);
        }

        [Authorize]
        public ActionResult GetTiAraRaporFormu(int tiBasvuruId, int? tiBasvuruAraRaporId)
        {
            var model = new TiAraRaporFormuModel
            {
                TIBasvuruID = tiBasvuruId
            };

            var mMessage = new MmMessage();
            string view = "";
            var tiBasvuru = _entities.TIBasvurus.First(p => p.TIBasvuruID == tiBasvuruId);
            var tiBasvuruAraRapor = tiBasvuru.TIBasvuruAraRapors.FirstOrDefault(p => p.TIBasvuruAraRaporID == tiBasvuruAraRaporId);
            var degerlendirmeYetki = RoleNames.TiTezDegerlendirmeYap.InRoleCurrent() || tiBasvuru.KullaniciID == UserIdentity.Current.Id;
            var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(tiBasvuru.KullaniciID);
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == tiBasvuru.KullaniciID);
            if (!degerlendirmeYetki)
            {
                mMessage.Messages.Add("Ara rapor formu kayıt yetkisine sahip değilsiniz.");

            }
            else if (tiBasvuruAraRapor != null && tiBasvuruAraRapor.TIBasvuruAraRaporDurumID > TiAraRaporDurumuEnum.ToplantiBilgileriGirildi)
            {
                mMessage.Messages.Add("Komite üyelerine değerlendirme linki gönderildikten sonra rapor bilgisinde değişiklik yapamazsınız. ");
            }
            else if (tiBasvuruAraRapor == null && TiAyar.TiBasvurusuAcikmi.GetAyarTi(tiBasvuru.EnstituKod).ToBoolean() == false)
            {
                mMessage.Messages.Add("Ara rapor giriş işelmemleri kapalıdır. ");
            }
            else if (!tiBasvuruAraRaporId.HasValue && !kul.DanismanID.HasValue)
            {
                var danismanTc = studentInfo.OgrenciInfo.DANISMAN_TC1;
                if (!kul.DanismanID.HasValue && (danismanTc.IsNullOrWhiteSpace() || danismanTc.Length != 11))
                {
                    mMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                }
                else if (!kul.DanismanID.HasValue)
                {
                    mMessage.Messages.Add("Tez danışmanı bilginiz OBS sisteminden boş gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin OBS sisteminde tanımlı olması gerekmektedir.");

                }
                else mMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
            }
            else
            {
                var donemBilgi = (tiBasvuruAraRapor?.RaporTarihi ?? DateTime.Now).ToTiAraRaporDonemBilgi(tiBasvuru.EnstituKod);
                var donemId = donemBilgi.BaslangicTarihi.Year.ToString() + donemBilgi.DonemId;
                var ogrenciInfo = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo, donemId);

                if (ogrenciInfo.Hata)
                {
                    mMessage.Messages.Add("Öğrenci kimlik numarası bilgisi ile OBS sisteminden öğrenci bilgisi kontrol edilirken bir hata oluştu. " + ogrenciInfo.HataMsj);
                }
                else
                {
                    var sondonemKayitolmasiGerekenDersKodlari = TiAyar.TiSonDonemKayitOlunmasiGerekenDersKodlari.GetAyarTi(tiBasvuru.EnstituKod);

                    var kayitYapilacakDersKodlaris = !tiBasvuruAraRaporId.HasValue ? sondonemKayitolmasiGerekenDersKodlari.Split(',').Where(p => p.Trim() != "").ToList().Select(s => s.Trim()).ToList() : new List<string>();
                    if (kayitYapilacakDersKodlaris.Any() && kayitYapilacakDersKodlaris.Count(p => ogrenciInfo.AktifDonemDers.DersKodNums.Any(a => a == p)) != kayitYapilacakDersKodlaris.Count)
                    {
                        mMessage.Messages.Add("Tez izleme raporunu başlatabilmeniz için " + donemBilgi.DonemAdiLong + " döneminde " + sondonemKayitolmasiGerekenDersKodlari + " kodlu derslere kayıt olmanız gerekmektedir.");
                    }
                    else if (tiBasvuru.TIBasvuruAraRapors.Any(p => p.TIBasvuruAraRaporID != tiBasvuruAraRaporId && p.DonemBaslangicYil == donemBilgi.BaslangicYil && p.DonemID == donemBilgi.DonemId))
                    {
                        mMessage.Messages.Add(donemBilgi.DonemAdiLong + " döneminde zaten bir tez izleme raporu başvurunuz bulunmakta!");
                    }
                    else if (UserIdentity.Current.Id == tiBasvuru.KullaniciID && tiBasvuruAraRapor != null && tiBasvuruAraRapor.TIBasvuruAraRaporKomites.Any(a => a.LinkGonderenID.HasValue))
                    {
                        mMessage.Messages.Add("Komite üyelerine değerlendirme linki gönderildiğinden Rapor bilgisinde değişiklik yapamazsınız. ");
                    }
                }



                if (mMessage.Messages.Count == 0)
                {
                    if (ogrenciInfo.Hata)
                    {
                        mMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu! " + ogrenciInfo.HataMsj);
                    }



                    var tiks = ogrenciInfo.TezIzlJuriBilgileri.Where(p => p.TEZ_DANISMAN != "1").ToList();
                    if (tiks.Count < 2)
                    {
                        mMessage.Messages.Add("Tik üye bilgileri OBS sisteminden alınamadı.");
                    }
                    else if (ogrenciInfo.TezIzlJuriBilgileri.All(p => p.TEZ_DANISMAN != "1"))
                    {
                        mMessage.Messages.Add("Tik üye bilgileri tez danışmanı bilgisine rastlanmadı.");
                    }
                    if (mMessage.Messages.Count > 0)
                    {
                        mMessage.Messages.Add("Rapor formunu oluşturabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                    }
                    if (mMessage.Messages.Count == 0)
                    {
                        foreach (var item in tiks)
                        {
                            item.TEZ_IZLEME_JURI_ADSOY = item.TEZ_IZLEME_JURI_ADSOY.ToUpper().Trim();
                            item.TEZ_IZLEME_JURI_UNVAN = item.TEZ_IZLEME_JURI_UNVAN.ToUpper().Trim().ToJuriUnvanAdi();
                        }
                        var obsTik1 = tiks[0];
                        var obsTik2 = tiks[1];

                        var cmbUnvanList = UnvanlarBus.GetCmbJuriUnvanlar(true);
                        var cmbUniversiteList = UniversitelerBus.CmbGetAktifUniversiteler(true);

                        model.TezBaslikTr = studentInfo.OgrenciTez.TEZ_BASLIK;
                        model.TezBaslikEn = studentInfo.OgrenciTez.TEZ_BASLIK_ENG;
                        model.IsTezDiliTr = studentInfo.IsTezDiliTr;
                        studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1 = studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper().Trim();
                        studentInfo.OgrenciInfo.DANISMAN_UNVAN1 = studentInfo.OgrenciInfo.DANISMAN_UNVAN1.ToUpper().Trim().ToJuriUnvanAdi();

                        model.OgrenciAdSoyad = kul.Ad + " " + kul.Soyad + " - " + tiBasvuru.OgrenciNo;
                        model.OgrenciAnabilimdaliProgramAdi = tiBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " - " + tiBasvuru.Programlar.ProgramAdi;
                        if (tiBasvuruAraRapor != null)
                        {
                            model.AraRaporSayisi = tiBasvuruAraRapor.AraRaporSayisi;
                            model.TIBasvuruAraRaporID = tiBasvuruAraRapor.TIBasvuruAraRaporID;
                            model.IsTezDiliTr = model.IsTezDiliTr;
                            model.TezBaslikEn = tiBasvuruAraRapor.TezBaslikEn; 
                            model.IsTezBasligiDegisti = tiBasvuruAraRapor.IsTezBasligiDegisti;
                            model.YeniTezBaslikTr = tiBasvuruAraRapor.YeniTezBaslikTr;
                            model.YeniTezBaslikEn = tiBasvuruAraRapor.YeniTezBaslikEn;
                            model.TezBasligiDegisimGerekcesi = tiBasvuruAraRapor.TezBasligiDegisimGerekcesi;
                            model.TICalismaRaporDosyaAdi = tiBasvuruAraRapor.TICalismaRaporDosyaAdi;
                            model.TICalismaRaporDosyaYolu = tiBasvuruAraRapor.TICalismaRaporDosyaYolu;
                            model.IsYokDrBursiyeriVar = tiBasvuruAraRapor.IsYokDrBursiyeriVar;
                            model.YokDrOncelikliAlan = tiBasvuruAraRapor.YokDrOncelikliAlan;
                            model.KomiteList = tiBasvuruAraRapor.TIBasvuruAraRaporKomites.ToList().Select(s => new KrTIBasvuruAraRaporKomite
                            {
                                TIBasvuruAraRaporID = s.TIBasvuruAraRaporID,
                                TIBasvuruAraRaporKomiteID = s.TIBasvuruAraRaporKomiteID,
                                JuriTipAdi = s.JuriTipAdi,
                                UnvanAdi = s.UnvanAdi.ToUpper().Trim(),
                                SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", s.UnvanAdi),
                                AdSoyad = s.AdSoyad.ToUpper().Trim(),
                                EMail = s.EMail,
                                UniversiteID = s.UniversiteID,
                                IsDilSinaviOrUniversite = s.IsDilSinaviOrUniversite,
                                DilSinavAdi = s.DilSinavAdi,
                                DilPuani = s.DilPuani,
                                SinavTarihi = s.SinavTarihi,
                                SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", s.UniversiteID),
                                UniversiteAdi = s.UniversiteAdi,
                                AnabilimdaliProgramAdi = s.AnabilimdaliProgramAdi
                            }).ToList();
                            var tD = model.KomiteList.First(p => p.JuriTipAdi == "TezDanismani");
                            if (tD.AdSoyad != studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1 || tD.UnvanAdi != studentInfo.OgrenciInfo.DANISMAN_UNVAN1)
                                mMessage.Messages.Add("Tez danışmanı bilgileri değişmiştir.<br /> Önceki Veri: " + tD.UnvanAdi + " " + tD.AdSoyad + " Yeni Veri: " + studentInfo.OgrenciInfo.DANISMAN_UNVAN1 + " " + studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1);
                            tD.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tD.UniversiteID);
                            if (tD.AdSoyad != studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1 || tD.UnvanAdi != studentInfo.OgrenciInfo.DANISMAN_UNVAN1)
                            {
                                tD.AdSoyad = studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1;
                                tD.UnvanAdi = studentInfo.OgrenciInfo.DANISMAN_UNVAN1;

                            }

                            tD.UniversiteID = GlobalSistemSetting.UniversiteYtuKod;
                            tD.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tD.UnvanAdi);





                            var tik1 = model.KomiteList.First(p => p.JuriTipAdi == "TikUyesi1");
                            if (tik1.AdSoyad != obsTik1.TEZ_IZLEME_JURI_ADSOY || tik1.UnvanAdi != obsTik1.TEZ_IZLEME_JURI_UNVAN)
                                mMessage.Messages.Add("Tik1 Üyesi bilgileri değişmiştir.<br /> Önceki Veri: " + tik1.UnvanAdi + " " + tik1.AdSoyad + " Yeni Veri: " + obsTik1.TEZ_IZLEME_JURI_UNVAN + " " + obsTik1.TEZ_IZLEME_JURI_ADSOY);
                            tik1.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tik1.UniversiteID);
                            if (tik1.AdSoyad != obsTik1.TEZ_IZLEME_JURI_ADSOY || tik1.UnvanAdi != obsTik1.TEZ_IZLEME_JURI_UNVAN)
                            {
                                tik1.AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY;
                                tik1.UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN;
                                tik1.UniversiteID = obsTik1.TEZ_IZLEME_JURI_UNIVER.ToLower().Contains("yıldız teknik")
                                    ? GlobalSistemSetting.UniversiteYtuKod
                                    : (int?)null;
                                tik1.EMail = obsTik1.TEZ_IZLEME_JURI_EPOSTA.Trim();
                                tik1.AnabilimdaliProgramAdi = obsTik1.TEZ_IZLEME_JURI_ANABLMDAL;

                            }
                            tik1.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tik1.UnvanAdi);


                            var tik2 = model.KomiteList.First(p => p.JuriTipAdi == "TikUyesi2");
                            if (tik2.AdSoyad != obsTik2.TEZ_IZLEME_JURI_ADSOY || tik2.UnvanAdi != obsTik2.TEZ_IZLEME_JURI_UNVAN)
                                mMessage.Messages.Add("Tik2 Üyesi bilgileri değişmiştir.<br /> Önceki Veri: " + tik2.UnvanAdi + " " + tik2.AdSoyad + " Yeni Veri: " + obsTik2.TEZ_IZLEME_JURI_UNVAN + " " + obsTik2.TEZ_IZLEME_JURI_ADSOY);

                            tik2.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tik2.UniversiteID);
                            if (tik2.AdSoyad != obsTik2.TEZ_IZLEME_JURI_ADSOY || tik2.UnvanAdi != obsTik2.TEZ_IZLEME_JURI_UNVAN)
                            {
                                tik2.AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY;
                                tik2.UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN;
                                tik2.UniversiteID = obsTik2.TEZ_IZLEME_JURI_UNIVER.ToLower().Contains("yıldız teknik")
                                    ? GlobalSistemSetting.UniversiteYtuKod
                                    : (int?)null;
                                tik2.EMail = obsTik2.TEZ_IZLEME_JURI_EPOSTA.Trim();
                                tik2.AnabilimdaliProgramAdi = obsTik2.TEZ_IZLEME_JURI_ANABLMDAL;


                            }
                            tik2.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tik2.UnvanAdi);


                            if (mMessage.Messages.Count > 0)
                            {
                                mMessage.Title = "Komite Üye Bilgilerinde Değişikliğe Rastlandı!";
                                mMessage.Messages.Add("<span style='color:maroon;'>Yukarıdaki değişikliklerin formunuza yansıması için Kayıt işlemini tamamlayınız.</span>");

                            }
                        }
                        else
                        {


                            model.AraRaporSayisi = ogrenciInfo.AraRaporMaxNo;
                            var tdKul = _entities.Kullanicilars.First(p => p.KullaniciID == kul.DanismanID);
                            var tdBilgi = new KrTIBasvuruAraRaporKomite
                            {
                                JuriTipAdi = "TezDanismani",
                                UnvanAdi = studentInfo.OgrenciInfo.DANISMAN_UNVAN1,
                                AdSoyad = studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1,
                                EMail = tdKul.EMail,
                                UniversiteID = GlobalSistemSetting.UniversiteYtuKod,
                                // AnabilimdaliProgramAdi = TezDanismani.Birimler!=null?TezDanismani.Birimler.BirimAdi:"",

                            };
                            tdBilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tdBilgi.UnvanAdi);
                            tdBilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tdBilgi.UniversiteID);
                            model.KomiteList.Add(tdBilgi);


                            var tk1Bilgi = new KrTIBasvuruAraRaporKomite
                            {
                                JuriTipAdi = "TikUyesi1",
                                AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN,
                                EMail = obsTik1.TEZ_IZLEME_JURI_EPOSTA
                            };
                            tk1Bilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tk1Bilgi.UnvanAdi);
                            tk1Bilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tk1Bilgi.UniversiteID);
                            model.KomiteList.Add(tk1Bilgi);


                            var tk2Bilgi = new KrTIBasvuruAraRaporKomite
                            {
                                JuriTipAdi = "TikUyesi2",
                                AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN
                            };
                            tk2Bilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tk2Bilgi.UnvanAdi);
                            tk2Bilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tk2Bilgi.UniversiteID);
                            model.KomiteList.Add(tk2Bilgi);


                        }

                        model.SelectedTabId = 1;
                        var donemSelectedValue = (tiBasvuruAraRapor != null
                            ? (tiBasvuruAraRapor.DonemBaslangicYil + "" + tiBasvuruAraRapor.DonemID)
                            : (donemBilgi.BaslangicYil + "" + donemBilgi.DonemId));
                        model.SListDonemSecim =
                            new SelectList(TiBus.CmbTiDonemListeBasvuru(tiBasvuru.EnstituKod), "Value", "Caption", donemSelectedValue);
                        model.SListUnvanAdi = new SelectList(cmbUnvanList);
                        model.SListUniversiteId = new SelectList(cmbUniversiteList, "Value", "Caption");
                        model.SListAraRaporSayisi = new SelectList(TiBus.CmbAraRaporSayisi(true), "Value", "Caption", model.AraRaporSayisi);

                        mMessage.MessageType = MsgTypeEnum.Information;
                        mMessage.IsSuccess = true;
                        view = ViewRenderHelper.RenderPartialView("TiBasvuru", "TiAraRaporFormu", model);
                    }
                }
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
        [Authorize]
        [ValidateInput(false)]
        public ActionResult TiAraRaporFormuPost(TiAraRaporFormuModel kModel, string donemSecim, bool saveData = false)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez İzleme Rapor Formu Oluşturma İşlemi"
            };

            bool isYeniJo = true;
            var tiBasvuru = _entities.TIBasvurus.First(p => p.TIBasvuruID == kModel.TIBasvuruID);
            var tiBasvuruAraRapor = tiBasvuru.TIBasvuruAraRapors.FirstOrDefault(p => p.TIBasvuruAraRaporID == kModel.TIBasvuruAraRaporID);
            var degerlendirmeYetki = RoleNames.TiTezDegerlendirmeYap.InRoleCurrent() || tiBasvuru.KullaniciID == UserIdentity.Current.Id;
            var enstituYetki = RoleNames.TiGelenBasvuruKayit.InRole();
            var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(tiBasvuru.KullaniciID);
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == tiBasvuru.KullaniciID);
            int? baslangicYil = null;
            int? donemId = null;
            if (enstituYetki && !donemSecim.IsNullOrWhiteSpace())
            {
                baslangicYil = donemSecim.Substring(0, 4).ToInt(0);
                donemId = donemSecim.Substring(4, 1).ToInt(0);

            }

            if (!degerlendirmeYetki)
            {
                mMessage.Messages.Add("Ara rapor formu kayıt yetkisine sahip değilsiniz.");
            }
            var danismanTc = studentInfo.OgrenciInfo.DANISMAN_TC1;
            if (tiBasvuruAraRapor == null && !kul.DanismanID.HasValue && (danismanTc.IsNullOrWhiteSpace() || danismanTc.Length != 11))
            {
                mMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
            }
            else if (tiBasvuruAraRapor == null && !kul.DanismanID.HasValue)
            {
                mMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
            }
            else
            {
                isYeniJo = tiBasvuruAraRapor == null;
                bool isDegisiklikVar = false;
                var donemBilgi = (isYeniJo ? DateTime.Now : tiBasvuruAraRapor.RaporTarihi).ToTiAraRaporDonemBilgi(tiBasvuru.EnstituKod);
                var obsDonemId = donemBilgi.BaslangicTarihi.Year.ToString() + donemBilgi.DonemId;
                var donemdeVerilenDersBilgileri = isYeniJo ? KullanicilarBus.OgrenciKontrol(kul.OgrenciNo, obsDonemId) : new StudentControl();
                var kayitYapilacakDersKodlaris = isYeniJo ? TiAyar.TiSonDonemKayitOlunmasiGerekenDersKodlari.GetAyarTi(tiBasvuru.EnstituKod).Split(',').Where(p => p.Trim() != "").Select(s => s.Trim()).ToList() : new List<string>();

                if (tiBasvuru.TIBasvuruAraRapors.Any(p => p.TIBasvuruAraRaporID != kModel.TIBasvuruAraRaporID && p.DonemBaslangicYil == donemBilgi.BaslangicYil && p.DonemID == donemBilgi.DonemId))
                {
                    mMessage.Messages.Add(donemBilgi.DonemAdiLong + " döneminde zaten bir tez izleme raporu başvurunuz bulunmakta!");
                }
                else if (kayitYapilacakDersKodlaris.Any() && donemdeVerilenDersBilgileri.AktifDonemDers.DersKodNums.Count(p => kayitYapilacakDersKodlaris.Any(a => a == p)) != kayitYapilacakDersKodlaris.Count)
                {
                    mMessage.Messages.Add("Tez izleme raporunu başlatabilmeniz için " + donemBilgi.DonemAdiLong + " döneminde " + tiBasvuru.BasvuruSonDonemSecilecekDersKodlari + " kodlu derslere kayıt olmanız gerekmektedir.");
                }
                else if (tiBasvuruAraRapor != null && tiBasvuruAraRapor.TIBasvuruAraRaporDurumID > TiAraRaporDurumuEnum.ToplantiBilgileriGirildi)
                {
                    mMessage.Messages.Add("Komite üyelerine değerlendirme yaptıktan sonra rapor bilgisinde değişiklik yapamazsınız. ");
                }
                if (mMessage.Messages.Count == 0)
                {
                    bool rsSuccess = true;
                    if (kModel.AraRaporSayisi <= 0) { rsSuccess = false; mMessage.Messages.Add("Rapor Sayısını Seçiniz."); }
                    else if (tiBasvuru.TIBasvuruAraRapors.Any(p => p.TIBasvuruAraRaporID != kModel.TIBasvuruAraRaporID && p.AraRaporSayisi >= kModel.AraRaporSayisi))
                    {
                        rsSuccess = false;
                        mMessage.Messages.Add("Rapor sayısı daha önceki raporlarda girilen rapor sayısından küçük yada eşit olamaz!");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (rsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "AraRaporSayisi" });

                    if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
                        mMessage.Messages.Add("Tez Başlığı Bilgisi Boş Bırakılamaz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikTr.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TezBaslikTr" });
                    if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
                        mMessage.Messages.Add("Tez Başlığı Çevirisi Bilgisi Boş Bırakılamaz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikEn.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "TezBaslikEn" });
               

                    if (kModel.Dosya == null && kModel.TICalismaRaporDosyaAdi.IsNullOrWhiteSpace()) mMessage.Messages.Add("Çalışma Raporu Dosyası Seçiniz.");
                    else if (kModel.Dosya != null && !kModel.Dosya.FileName.IsPdfFile()) mMessage.Messages.Add("Çalışma Raporu Dosyası PDF türünde olmalıdır.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = ((kModel.Dosya == null && kModel.TICalismaRaporDosyaAdi.IsNullOrWhiteSpace()) || (kModel.Dosya != null && !kModel.Dosya.FileName.IsPdfFile()) ? MsgTypeEnum.Warning : MsgTypeEnum.Success), PropertyName = "Dosya" });
                    if (!kModel.IsYokDrBursiyeriVar.HasValue)
                    {
                        mMessage.Messages.Add("100/2000 YÖK Bursiyeri Bilgisini Seçiniz");
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsYokDrBursiyeriVar" });
                    }
                    else
                    {
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IsYokDrBursiyeriVar" });
                        if (kModel.IsYokDrBursiyeriVar.Value)
                            if (kModel.YokDrOncelikliAlan.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("Öncelikli Alt Alan Adı Giriniz.");
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YokDrOncelikliAlan" });
                            }
                            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YokDrOncelikliAlan" });
                    }
                }
                if (mMessage.Messages.Count > 0)
                {
                    kModel.SelectedTabId = 1;
                }
                if (mMessage.Messages.Count == 0)
                {
                      var tabIDs = kModel.TabId.Select((s, i) => new { TabID = s, Inx = (i + 1) }).ToList();
                    var juriTipAdis = kModel.JuriTipAdi.Select((s, i) => new { JuriTipAdi = s, Inx = (i + 1) }).ToList();
                    var adSoyads = kModel.AdSoyad.Select((s, i) => new { AdSoyad = s, Inx = (i + 1) }).ToList();
                    var unvanAdis = kModel.UnvanAdi.Select((s, i) => new { UnvanAdi = s, Inx = (i + 1) }).ToList();
                    var eMails = kModel.EMail.Select((s, i) => new { EMail = s.Trim(), Inx = (i + 1) }).ToList();
                    var universiteIDs = kModel.UniversiteId.Select((s, i) => new { UniversiteID = s, Inx = (i + 1) }).ToList();
                    var anabilimdaliProgramAdis = kModel.AnabilimdaliProgramAdi.Select((s, i) => new { AnabilimdaliProgramAdi = s, Inx = (i + 1) }).ToList();
                    
                    var qData = (from ad in adSoyads
                                 join at in tabIDs on ad.Inx equals at.Inx
                                 join jt in juriTipAdis on ad.Inx equals jt.Inx
                                 join un in unvanAdis on ad.Inx equals un.Inx
                                 join em in eMails on ad.Inx equals em.Inx
                                 join uni in universiteIDs on ad.Inx equals uni.Inx
                                 join abd in anabilimdaliProgramAdis on ad.Inx equals abd.Inx 

                                 select new
                                 {
                                     ad.Inx,
                                     at.TabID,
                                     jt.JuriTipAdi,
                                     AdSoyad = ad.AdSoyad.ToStrObjEmptString(),
                                     AdSoyadSuccess = !ad.AdSoyad.IsNullOrWhiteSpace(),
                                     UnvanAdi = un.UnvanAdi.ToStrObjEmptString(),
                                     UnvanAdiSuccess = !un.UnvanAdi.IsNullOrWhiteSpace(),
                                     EMail = em.EMail.ToStrObjEmptString(),
                                     EMailSuccess = !em.EMail.IsNullOrWhiteSpace() && em.EMail.ToIsValidEmail(),
                                     uni.UniversiteID,
                                     UniversiteIDSuccess = uni.UniversiteID.HasValue,
                                     AnabilimdaliProgramAdi = abd.AnabilimdaliProgramAdi.ToStrObjEmptString(),
                                     AnabilimdaliProgramAdiSuccess = !abd.AnabilimdaliProgramAdi.IsNullOrWhiteSpace()
                                 
                                 }).Select(s => new
                                 {
                                     Row = s,
                                     IsSuccessRow = s.JuriTipAdi.ToTiUyeFormSuccessRow(kModel.IsTezDiliTr, s.AdSoyadSuccess, s.UnvanAdiSuccess, s.EMailSuccess, s.UniversiteIDSuccess, s.AnabilimdaliProgramAdiSuccess)

                                 }).ToList();

                    int errSelectedTabId = qData.Where(p => p.IsSuccessRow == false).OrderBy(o => o.Row.TabID).Select(s => s.Row.TabID).FirstOrDefault();
                    foreach (var item in qData)
                    {

                        if ((errSelectedTabId <= kModel.SelectedTabId && errSelectedTabId == item.Row.TabID) && !item.IsSuccessRow)
                        {
                            mMessage.Messages.Add(item.Row.JuriTipAdi + " bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            if (!item.Row.AdSoyadSuccess) mMessage.Messages.Add("Ad Soyad bilgisi");
                            if (!item.Row.UnvanAdiSuccess) mMessage.Messages.Add("Unvan bilgisi");
                            if (!item.Row.EMailSuccess) mMessage.Messages.Add("E-Posta Adresi bilgisi");
                            if (!item.Row.UniversiteIDSuccess) mMessage.Messages.Add("Üniversite bilgisi");
                            if (!item.Row.AnabilimdaliProgramAdiSuccess) mMessage.Messages.Add("Anabilim Dalı bilgisi");
                          
                        }

                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.AdSoyadSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item.Row.JuriTipAdi + "AdSoyad" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.UnvanAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item.Row.JuriTipAdi + "UnvanAdi" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.EMailSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item.Row.JuriTipAdi + "EMail" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.UniversiteIDSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error), PropertyName = item.Row.JuriTipAdi + "UniversiteID" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.AnabilimdaliProgramAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error), PropertyName = item.Row.JuriTipAdi + "AnabilimdaliProgramAdi" });
                       
                    }
                    if (errSelectedTabId == 0)
                    {
                        if (saveData == false)
                        {
                            kModel.SelectedTabId += 1;
                        }
                    }
                    else kModel.SelectedTabId = errSelectedTabId;

                    if (mMessage.Messages.Count == 0 && saveData)
                    {
                        try
                        {
                            tiBasvuruAraRapor = isYeniJo ? new TIBasvuruAraRapor() : tiBasvuruAraRapor;
                            var unilers = _entities.Universitelers.ToList();
                            foreach (var item in qData)
                            {
                                var rw = tiBasvuruAraRapor.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.JuriTipAdi == item.Row.JuriTipAdi);
                                if (rw != null)
                                {
                                    var uni = unilers.First(p => p.UniversiteID == item.Row.UniversiteID);
                                    if (item.Row.AdSoyad.IsNullOrWhiteSpace() == false)
                                    {
                                        if (rw.AdSoyad != item.Row.AdSoyad || rw.UnvanAdi != item.Row.UnvanAdi || rw.EMail != item.Row.EMail || rw.UniversiteID != item.Row.UniversiteID) isDegisiklikVar = true;
                                        rw.UnvanAdi = item.Row.UnvanAdi.ToUpper();
                                        rw.AdSoyad = item.Row.AdSoyad.ToUpper();
                                        rw.EMail = item.Row.EMail;
                                        rw.UniversiteAdi = uni.Ad;
                                        rw.UniversiteID = item.Row.UniversiteID;
                                        rw.AnabilimdaliProgramAdi = item.Row.AnabilimdaliProgramAdi; 
                                        rw.IslemTarihi = DateTime.Now;
                                        rw.IslemYapanID = UserIdentity.Current.Id;
                                        rw.IslemYapanIP = UserIdentity.Ip;

                                    }
                                    else _entities.TIBasvuruAraRaporKomites.Remove(rw);
                                }
                                else if (item.Row.AdSoyad.IsNullOrWhiteSpace() == false)
                                {
                                    var uni = unilers.First(p => p.UniversiteID == item.Row.UniversiteID);
                                    tiBasvuruAraRapor.TIBasvuruAraRaporKomites.Add(
                                        new TIBasvuruAraRaporKomite
                                        {
                                            UniqueID = Guid.NewGuid(),
                                            JuriTipAdi = item.Row.JuriTipAdi,
                                            UnvanAdi = item.Row.UnvanAdi.ToUpper(),
                                            AdSoyad = item.Row.AdSoyad.ToUpper(),
                                            EMail = item.Row.EMail,
                                            UniversiteID = item.Row.UniversiteID,
                                            UniversiteAdi = uni.Ad,
                                            AnabilimdaliProgramAdi = item.Row.AnabilimdaliProgramAdi, 
                                            IslemTarihi = DateTime.Now,
                                            IslemYapanID = UserIdentity.Current.Id,
                                            IslemYapanIP = UserIdentity.Ip


                                        });
                                }
                            }
                            if (isYeniJo || isDegisiklikVar)
                            {
                                var uniqueId = Guid.NewGuid();
                                while (_entities.TIBasvuruAraRapors.Any(a => a.UniqueID == uniqueId))
                                {
                                    uniqueId = Guid.NewGuid();
                                }
                                tiBasvuruAraRapor.UniqueID = uniqueId;
                                var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                                while (_entities.TIBasvuruAraRapors.Any(a => a.FormKodu == formKodu))
                                {
                                    formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                                }
                                tiBasvuruAraRapor.FormKodu = formKodu;
                            }
                            else
                            { 
                                if (
                                    tiBasvuruAraRapor.AraRaporSayisi != kModel.AraRaporSayisi ||
                                    tiBasvuruAraRapor.TezBaslikTr != kModel.TezBaslikTr ||
                                    tiBasvuruAraRapor.TezBaslikEn != kModel.TezBaslikEn || 
                                    tiBasvuruAraRapor.IsYokDrBursiyeriVar != kModel.IsYokDrBursiyeriVar ||
                                    tiBasvuruAraRapor.YokDrOncelikliAlan != kModel.YokDrOncelikliAlan 
                                    
                                   ) isDegisiklikVar = true;

                            }

                            tiBasvuruAraRapor.DonemID = donemId ?? donemBilgi.DonemId;
                            tiBasvuruAraRapor.AraRaporSayisi = kModel.AraRaporSayisi;
                            tiBasvuruAraRapor.TIBasvuruID = kModel.TIBasvuruID;
                            tiBasvuruAraRapor.IsTezDiliTr = kModel.IsTezDiliTr;
                            tiBasvuruAraRapor.TezBaslikTr = kModel.TezBaslikTr;
                            tiBasvuruAraRapor.TezBaslikEn = kModel.TezBaslikEn;   
                            tiBasvuruAraRapor.IsYokDrBursiyeriVar = kModel.IsYokDrBursiyeriVar.Value;
                            tiBasvuruAraRapor.YokDrOncelikliAlan = kModel.YokDrOncelikliAlan;
                            tiBasvuruAraRapor.IslemTarihi = DateTime.Now;
                            tiBasvuruAraRapor.IslemYapanID = UserIdentity.Current.Id;
                            tiBasvuruAraRapor.IslemYapanIP = UserIdentity.Ip;

                            if (kModel.Dosya != null)
                            {
                                FileHelper.Delete(tiBasvuruAraRapor.TICalismaRaporDosyaYolu);
                                tiBasvuruAraRapor.TICalismaRaporDosyaAdi = kModel.Dosya.FileName.GetFileName();
                                tiBasvuruAraRapor.TICalismaRaporDosyaYolu = FileHelper.SaveTiAraRaporDosya(kModel.Dosya);
                            }
                            tiBasvuruAraRapor.DonemBaslangicYil = (baslangicYil ?? donemBilgi.BaslangicYil);
                            tiBasvuruAraRapor.DonemID = (donemId ?? donemBilgi.DonemId);
                            if (isYeniJo)
                            {
                                var td = _entities.Kullanicilars.First(p => p.KullaniciID == kul.DanismanID);
                                tiBasvuruAraRapor.BasvuruSonDonemSecilecekDersKodlari = TiAyar.TiSonDonemKayitOlunmasiGerekenDersKodlari.GetAyarTi(tiBasvuru.EnstituKod);
                                tiBasvuru.TezDanismanID = td.KullaniciID;
                                tiBasvuruAraRapor.TezDanismanID = td.KullaniciID;
                                tiBasvuruAraRapor.RaporTarihi = DateTime.Now;
                                tiBasvuruAraRapor.TIBasvuruAraRaporDurumID = TiAraRaporDurumuEnum.ToplantiBilgileriGirilmedi;
                                tiBasvuruAraRapor = _entities.TIBasvuruAraRapors.Add(tiBasvuruAraRapor);
                            }


                            _entities.SaveChanges();
                            LogIslemleri.LogEkle("TIBasvuruAraRapor", isYeniJo ? LogCrudType.Insert : LogCrudType.Update, tiBasvuruAraRapor.ToJson());
                            foreach (var item in tiBasvuruAraRapor.TIBasvuruAraRaporKomites)
                            {
                                LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", isYeniJo ? LogCrudType.Insert : LogCrudType.Update, item.ToJson());
                            }
                            mMessage.IsSuccess = true;
                            int? srTalepId = null;
                            if (isDegisiklikVar || isYeniJo)
                            {
                                if (isYeniJo)
                                {
                                    tiBasvuru.AktifTIBasvuruAraRaporID = tiBasvuruAraRapor.TIBasvuruAraRaporID;
                                    _entities.SaveChanges();
                                }
                                if (isDegisiklikVar && !isYeniJo && tiBasvuruAraRapor.SRTalepleris.Any()) srTalepId = tiBasvuruAraRapor.SRTalepleris.First().SRTalepID;
                                TiBus.SendMailTiBilgisi(tiBasvuruAraRapor.TIBasvuruAraRaporID, srTalepId);
                                if (srTalepId.HasValue && mMessage.IsSuccess) mMessage.Messages.Add("<br/><i class='fa fa-lg fa-envelope-o' style='font-size:11pt;'></i> <span style=font-size:10pt;'>Rapor bilgilerinde değişiklik yapıldığı için Rapor, Toplantı bilgileri Danışman ve Öğrenciye mail olarak tekrar gönderildi!</span>");

                            }
                        }
                        catch (Exception ex)
                        {
                            var hataMsj = "Kayıt işlemi sırasında bir hata oluştu! \r\nHata:" + ex.ToExceptionMessage();
                            mMessage.Messages.Add(hataMsj);
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(hataMsj, ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                        }


                    }

                }


            }
            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return new
            {
                mMessage,
                IsYeniJO = isYeniJo,
                SaveData = saveData,
                SelectedTabID = kModel.SelectedTabId,
            }.ToJsonResult();
        }
        [Authorize]
        public ActionResult TiAraRaporFormu()
        {

            return View();
        }
        [Authorize]

        public ActionResult RezervasyonAl(int tiBasvuruAraRaporId)
        {
            var toplantiYetki = RoleNames.TiToplantiTalebiYap.InRoleCurrent();
            var tiAraRapor = _entities.TIBasvuruAraRapors.First(p => p.TIBasvuruAraRaporID == tiBasvuruAraRaporId);
            var model = new SrTalepleriKayitDto();
            if (!toplantiYetki && tiAraRapor.TIBasvuru.TezDanismanID != UserIdentity.Current.Id) model.YetkisizErisim = true;
            else
            {
                if (tiAraRapor.SRTalepleris.Any())
                {

                    var srTalep = tiAraRapor.SRTalepleris.First();
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
                    //model.SRTaleplerJuris = data.SRTaleplerJuris.ToList();
                }
                else
                {
                    model.EnstituKod = tiAraRapor.TIBasvuru.EnstituKod;
                    model.SRTalepTipID = 3;
                    model.TalepYapanID = tiAraRapor.TIBasvuru.KullaniciID;
                    model.Tarih = DateTime.Now.Date;
                    //model.SRTaleplerJuris = TITalep.TIBasvuruAraRaporKomites.Select(s => new SRTaleplerJuri { JuriAdi = s.UnvanAdi + " " + s.AdSoyad, Telefon = "", Email = s.EMail }).ToList();  

                }
            }

            return View(model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult RezervasyonAlPost(SrTalepleriKayitDto kModel, bool isSendMail = true)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez İzleme Toplantı Bilgileri",
                MessageType = MsgTypeEnum.Warning
            };
            var tiAraRapor = _entities.TIBasvuruAraRapors.First(p => p.TIBasvuruAraRaporID == kModel.TIBasvuruAraRaporID);
            var srTalep = tiAraRapor.SRTalepleris.FirstOrDefault();
            var tiToplantiTalebiYap = RoleNames.TiToplantiTalebiYap.InRoleCurrent();
            var tiTezDegerlendirmeDuzeltme = RoleNames.TiTezDegerlendirmeDuzeltme.InRoleCurrent();

            if (!tiToplantiTalebiYap && tiAraRapor.TIBasvuru.TezDanismanID != UserIdentity.Current.Id) kModel.YetkisizErisim = true;


            mmMessage.DialogID = tiAraRapor.TIBasvuruID.ToString();
            kModel.SRTalepTipID = 3;

            kModel.EnstituKod = tiAraRapor.TIBasvuru.EnstituKod;
            if (kModel.YetkisizErisim)
            {
                mmMessage.Messages.Add("Tez Izleme Toplantı Kayıt işlemi yapmaya yetkili değilsiniz.");
            }
            else
            {
                if (tiAraRapor.TIBasvuruAraRaporKomites.Any(a => a.IsBasarili.HasValue))
                {
                    mmMessage.Messages.Add("Komite üyelerinden herhangi biri değerlendirme yaptıktan sonra Toplantı bilgileri değiştirilemez.");
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
                if (kModel.IsOnline == true && !TiAyar.TiAraRaporSinaviOnlineYapilabilsin
                        .GetAyarTi(kModel.EnstituKod).ToBoolean(false))
                {
                    mmMessage.Messages.Add("Tez İzleme Sınavı Online olarak yapılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsOnline" });
                }
                else if (kModel.IsOnline == false && !TiAyar.TiAraRaporSinaviYuzYuzeYapilabilsin
                             .GetAyarTi(kModel.EnstituKod).ToBoolean(false))
                {
                    mmMessage.Messages.Add("Tez İzleme Sınavı Yüz Yüze olarak yapılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsOnline" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "IsOnline" });
            }
            if (mmMessage.Messages.Count == 0)
            {
                var adminYetki = RoleNames.TiTezDegerlendirmeDuzeltme.InRole();

                if (kModel.Tarih == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Tarih Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                else if (!tiTezDegerlendirmeDuzeltme && kModel.Tarih < DateTime.Now)
                {
                    mmMessage.Messages.Add("Toplantı tarihi bilgisi günümüz tarihten küçük olamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                else if (!adminYetki && tiAraRapor.RaporTarihi.ToTiAraRaporDonemBilgi(tiAraRapor.TIBasvuru.EnstituKod).BitisTarihi.Date < kModel.Tarih.Date)
                {
                    var donemSonuTarihi = tiAraRapor.RaporTarihi.ToTiAraRaporDonemBilgi(tiAraRapor.TIBasvuru.EnstituKod).BitisTarihi;
                    mmMessage.Messages.Add("Toplantı tarihi ara rapor dönem sonu tarihi olan " + donemSonuTarihi.ToLongDateString() + " tarihten büyük olamaz.");
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
                        kModel.SRTaleplerJuris = tiAraRapor.TIBasvuruAraRaporKomites.Select(s => new SRTaleplerJuri
                        {
                            JuriTipAdi = s.JuriTipAdi,
                            AnabilimdaliProgramAdi = s.AnabilimdaliProgramAdi,
                            UniversiteAdi = s.UniversiteAdi,
                            UnvanAdi = s.UnvanAdi,
                            JuriAdi = s.UnvanAdi + " " + s.AdSoyad,
                            Telefon = "",
                            Email = s.EMail,
                            IslemTarihi = DateTime.Now,
                            IslemYapanID = UserIdentity.Current.Id,
                            IslemYapanIP = UserIdentity.Ip
                        }).ToList();

                        if (!tiTezDegerlendirmeDuzeltme)
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
                                TIBasvuruAraRaporID = tiAraRapor.TIBasvuruAraRaporID,
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
                            tiAraRapor.TIBasvuruAraRaporDurumID = TiAraRaporDurumuEnum.ToplantiBilgileriGirildi;

                        }
                        else
                        {

                            srTalep.TIBasvuruAraRaporID = tiAraRapor.TIBasvuruAraRaporID;
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
                        LogIslemleri.LogEkle("SRTalepleri", isNewRecord ? LogCrudType.Insert : LogCrudType.Update, srTalep.ToJson());

                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = MsgTypeEnum.Success;
                        mmMessage.Messages.Add("Komite toplantı bilgisi düzenlendi.");

                        #region SendMail

                        if (isSendMail)
                        {
                            var messages = TiBus.SendMailTiBilgisi(null, srTalep.SRTalepID);
                            mmMessage.Messages.Add(messages.IsSuccess
                                ? "<br/><i class='fa fa-envelope-o'></i> <span style=font-size:10pt;'>Toplantı bilgisi Komite üyelerine ve öğrenciye mail olarak gönderildi.</span>"
                                : "<br/><i class='fa fa-lg fa-envelope-o' style='font-size:11pt;'></i> <span style=font-size:10pt;'>Toplantı bilgisi Komite üyelerine ve öğrenciye mail olarak gönderilemedi!</span>");
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        mmMessage.IsSuccess = false;
                        mmMessage.MessageType = MsgTypeEnum.Error;
                        mmMessage.Messages.Add("İşlem yapılırken bir hata oluştu.");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Tez izleme toplantı bilgisi oluşturulurken bir hata oluştu! Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Kritik);
                    }

                }

            }

            return mmMessage.ToJsonResult();
        }

        public ActionResult AraRaporDegerlendi(Guid? uniqueId, bool? isTezIzlemeRaporuTezOnerisiUygun, bool isDrBurs, bool? isTezIzlemeRaporuAltAlanUygun, bool? isTezBasligiDegisti,
           string yeniTezBaslikTr, string yeniTezBaslikEn, string tezBasligiDegisimGerekcesi, bool? isBasarili, string aciklama)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez İzleme Rapor Değerlendirme İşlemi"
            };
            var degerlendirmeDuzeltmeYetki = RoleNames.TiTezDegerlendirmeDuzeltme.InRoleCurrent();
            var isRefresh = false;
            if (!uniqueId.HasValue)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme için gerekli benzersiz anahtar bilgisi boş gelmektedir.</span>");
            }
            else
            {
                var komite = _entities.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.UniqueID == uniqueId);
                if (komite == null)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme işlemi yapmanız için size tanınan benzersiz anahtar bilgisi değişti veya bulunamadı!</span>");
                }
                else
                {
                    var isTezDanismani = komite.JuriTipAdi == "TezDanismani";
                    if (!degerlendirmeDuzeltmeYetki)
                    {
                        var toplanti = komite.TIBasvuruAraRapor.SRTalepleris.First();

                        var toplantiTarihi = toplanti.Tarih.Add(toplanti.BasSaat);
                        if (DateTime.Now < toplantiTarihi)
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + toplantiTarihi.ToLongDateString() + " " +
                                                  $"{toplanti.BasSaat:hh\\:mm}" + "'</b> dan önce yapılamaz!</span>");
                        }
                        else if (komite.IsBasarili.HasValue)
                        {
                            mMessage.IsSuccess = true;
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemini daha önceden zaten yaptınız!</span>");
                        }
                        else
                        {
                            if (!isTezIzlemeRaporuTezOnerisiUygun.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu İle Tez Önerisi Uyumlu mu?</span>");
                            }
                            if (isTezDanismani && isDrBurs && !isTezIzlemeRaporuAltAlanUygun.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu İle 100/2000 YÖK Bursu Alt Alan Uyumlu mu?</span>");
                            }
                            if (isTezDanismani && isTezBasligiDegisti==true)
                            {
                                if (yeniTezBaslikTr.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı Bilgisi Boş Bırakılamaz.</span>");
                                }
                                if (yeniTezBaslikEn.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı Çevirisi Bilgisi Boş Bırakılamaz.</span>");
                                }
                            }
                            if (!isBasarili.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporunun Değerlendirme Sonucu</span>");
                            }
                            else if (!isBasarili.Value && aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu Değerlendirme Açıklaması</span>");
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez izleme rapor değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");
                        }
                    }
                    else
                    {
                        var toplanti = komite.TIBasvuruAraRapor.SRTalepleris.First();
                        var toplantiTarihi = toplanti.Tarih.Add(toplanti.BasSaat);
                        if (DateTime.Now < toplantiTarihi)
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + toplantiTarihi.ToLongDateString() + " " +
                                                  $"{toplanti.BasSaat:hh\\:mm}" + "'</b> dan önce yapılamaz!</span>");
                        }
                        else
                        {
                            var dCount = 4 + (isTezDanismani && isDrBurs ? 1 : 0) + (isBasarili == false ? 1 : 0);
                            var gCount = 0;
                            if (!isTezIzlemeRaporuTezOnerisiUygun.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu İle Tez Önerisi Uyumlu mu?</span>");
                            }
                            else gCount++;
                            if (isTezDanismani && isDrBurs)
                            {
                                if (!isTezIzlemeRaporuAltAlanUygun.HasValue)
                                    mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu İle 100/2000 YÖK Bursu Alt Alan Uyumlu mu?</span>");
                                else gCount++;
                            }
                            if (isTezDanismani && isTezBasligiDegisti==true)
                            {
                                if (yeniTezBaslikTr.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı Bilgisi Boş Bırakılamaz.</span>");
                                }
                                else gCount++;
                                if (yeniTezBaslikEn.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı Çevirisi Bilgisi Boş Bırakılamaz.</span>");
                                }
                                else gCount++;
                            }
                            if (!isBasarili.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporunun Değerlendirme Sonucu</span>");
                            }
                            else gCount++;
                            if (isBasarili == false)
                            {
                                if (aciklama.IsNullOrWhiteSpace())
                                    mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu Değerlendirme Açıklaması</span>");
                                else gCount++;
                            }
                            if (dCount == gCount || gCount == 0)
                            {
                                mMessage.Messages.Clear();
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez izleme rapor değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");
                        }
                    }
                    if (!mMessage.Messages.Any())
                    {
                        var sendMailLink = isTezDanismani && isBasarili.HasValue && !komite.TIBasvuruAraRapor.TIBasvuruAraRaporKomites.Any(a => a.IsLinkGonderildi.HasValue);
                        var isDegisiklikVar = komite.IsTezIzlemeRaporuTezOnerisiUygun != isTezIzlemeRaporuTezOnerisiUygun || komite.IsTezIzlemeRaporuAltAlanUygun != isTezIzlemeRaporuAltAlanUygun || komite.IsBasarili != isBasarili || komite.Aciklama != aciklama;
                        komite.IsTezIzlemeRaporuTezOnerisiUygun = isTezIzlemeRaporuTezOnerisiUygun;
                        komite.IsTezIzlemeRaporuAltAlanUygun = isTezIzlemeRaporuAltAlanUygun;
                        komite.IsBasarili = isBasarili;
                        komite.Aciklama = aciklama;
                        komite.DegerlendirmeIslemTarihi = DateTime.Now;
                        komite.DegerlendirmeIslemYapanIP = UserIdentity.Ip;
                        komite.DegerlendirmeYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;

                        komite.IslemTarihi = DateTime.Now;
                        komite.IslemYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;
                        komite.IslemYapanIP = UserIdentity.Ip;
                        if (isDegisiklikVar)
                        {
                            komite.TIBasvuruAraRapor.UniqueID = Guid.NewGuid();
                            var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            while (_entities.TIBasvuruAraRapors.Any(a => a.FormKodu == formKodu))
                            {
                                formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            }
                            komite.TIBasvuruAraRapor.FormKodu = formKodu;
                        }

                        if (isTezDanismani)
                        {
                            var araRapor = komite.TIBasvuruAraRapor;
                            araRapor.IsTezBasligiDegisti = isTezBasligiDegisti==true;
                            araRapor.YeniTezBaslikTr = yeniTezBaslikTr;
                            araRapor.YeniTezBaslikEn = yeniTezBaslikEn;
                            araRapor.TezBasligiDegisimGerekcesi = tezBasligiDegisimGerekcesi;

                        }
                        _entities.SaveChanges();
                        LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", LogCrudType.Update, komite.ToJson());
                        mMessage.IsSuccess = true;
                        if (sendMailLink)
                        {
                            var messages = TiBus.SendMailTiDegerlendirmeLink(komite.TIBasvuruAraRaporID, null, true);
                            if (isTezDanismani || degerlendirmeDuzeltmeYetki)
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
                                    komite.IsTezIzlemeRaporuTezOnerisiUygun = null;
                                    komite.IsTezIzlemeRaporuAltAlanUygun = null;
                                    komite.IsBasarili = null;
                                    komite.Aciklama = null;
                                    komite.DegerlendirmeIslemTarihi = null;
                                    komite.DegerlendirmeIslemYapanIP = null;
                                    komite.DegerlendirmeYapanID = null;
                                    _entities.SaveChanges();
                                }
                            }
                        }
                        else mMessage.Messages.Add("Değerlendirme işlemi tamamlandı.");


                        var isDegerlendirmeTamam = komite.TIBasvuruAraRapor.TIBasvuruAraRaporKomites.All(a => a.IsBasarili.HasValue);
                        var tiBasvuruAraRapor = komite.TIBasvuruAraRapor;
                        var tiBasvuruAraRaporKomites = tiBasvuruAraRapor.TIBasvuruAraRaporKomites;
                        if (isDegerlendirmeTamam)
                        {

                            tiBasvuruAraRapor.TIBasvuruAraRaporDurumID = TiAraRaporDurumuEnum.DegerlendirmeSureciTamamlandi;
                            tiBasvuruAraRapor.IsBasariliOrBasarisiz = tiBasvuruAraRaporKomites.Count(c => c.IsBasarili == true) > tiBasvuruAraRaporKomites.Count(c => c.IsBasarili == false);
                            tiBasvuruAraRapor.IsOyBirligiOrCoklugu = tiBasvuruAraRaporKomites.Count == tiBasvuruAraRaporKomites.Count(c => c.IsBasarili == tiBasvuruAraRapor.IsBasariliOrBasarisiz);

                            var messages = TiBus.SendMailTiDegerlendirmeLink(komite.TIBasvuruAraRaporID, null, false);
                            if (isTezDanismani || degerlendirmeDuzeltmeYetki)
                            {
                                if (messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme Sonucu Danışman ve Öğrenciye Gönderildi.");

                                }
                                else
                                {
                                    mMessage.Messages.AddRange(messages.Messages);
                                    mMessage.IsSuccess = false;
                                }
                            }
                            if (messages.IsSuccess)
                            {
                                tiBasvuruAraRapor.DegerlendirmeSonucMailTarihi = DateTime.Now;
                            }
                        }
                        else
                        {
                            tiBasvuruAraRapor.IsBasariliOrBasarisiz = null;
                            tiBasvuruAraRapor.IsOyBirligiOrCoklugu = null;
                            if (tiBasvuruAraRaporKomites.Any(a => a.IsBasarili.HasValue))
                            {
                                tiBasvuruAraRapor.TIBasvuruAraRaporDurumID = TiAraRaporDurumuEnum.DegerlendirmeSureciBaslatildi;
                            }
                            else
                            {
                                tiBasvuruAraRapor.TIBasvuruAraRaporDurumID = TiAraRaporDurumuEnum.ToplantiBilgileriGirildi;
                            }
                        }
                        LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", LogCrudType.Update, komite.ToJson());
                        _entities.SaveChanges();
                    }
                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);
            return Json(new { mMessage.IsSuccess, Messages = strView, IsRefresh = isRefresh }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]



        [Authorize]
        public ActionResult Sil(int id)
        {

            var mmMessage = TiBus.GetTiBasvuruSilKontrol(id);

            if (mmMessage.IsSuccess)
            {
                var kayit = _entities.TIBasvurus.First(p => p.TIBasvuruID == id);
                var tarih = kayit.BasvuruTarihi.ToString();

                bool isAdminRemove = false;
                if (UserIdentity.Current.IsAdmin)
                {
                    if (kayit.TIBasvuruAraRapors.All(a => a.TIBasvuruAraRaporDurumID != TiAraRaporDurumuEnum.DegerlendirmeSureciTamamlandi))
                    {
                        isAdminRemove = true;
                    }
                }

                try
                {

                    mmMessage.Title = "Uyarı";
                    if (isAdminRemove)
                    {
                        var araRapors = kayit.TIBasvuruAraRapors.ToList();
                        foreach (var item in araRapors)
                        {
                            _entities.SRTalepleris.RemoveRange(item.SRTalepleris);
                            _entities.TIBasvuruAraRaporKomites.RemoveRange(item.TIBasvuruAraRaporKomites);
                            _entities.TIBasvuruAraRapors.Remove(item);
                        }
                        _entities.TIBasvurus.Remove(kayit);
                    }
                    else
                    {
                        _entities.TIBasvurus.Remove(kayit);
                    }
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("TiBasvuru", LogCrudType.Delete, kayit.ToJson());

                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult DetaySil(int id, int tiBasvuruAraRaporId)
        {
            var mmMessage = new MmMessage
            {
                Title = "Rapor silme işlemi"
            };
            var tiTezDegerlendirmeYap = RoleNames.TiTezDegerlendirmeYap.InRoleCurrent();
            var tiTezDegerlendirmeDuzeltme = RoleNames.TiTezDegerlendirmeDuzeltme.InRoleCurrent();
            var qKayit = _entities.TIBasvuruAraRapors.Where(p => p.TIBasvuruID == id && p.TIBasvuruAraRaporID == tiBasvuruAraRaporId).AsQueryable();
            if (!tiTezDegerlendirmeYap && !tiTezDegerlendirmeDuzeltme) qKayit = qKayit.Where(p => p.TIBasvuru.KullaniciID == UserIdentity.Current.Id);
            else if (tiTezDegerlendirmeYap && !tiTezDegerlendirmeDuzeltme) qKayit = qKayit.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            var araRapor = qKayit.FirstOrDefault();


            if (araRapor == null)
            {
                mmMessage.Messages.Add("Silinmek istenen kayıt sistemde bulunamadı.");
            }
            else if (araRapor.TIBasvuruAraRaporDurumID > TiAraRaporDurumuEnum.ToplantiBilgileriGirildi)
            {
                mmMessage.Messages.Add("Komite üyelerine değerlendirme yaptıktan sonra rapor bilgisi silinemez. ");
            }
            else
            {
                try
                {
                    _entities.SRTalepleris.RemoveRange(araRapor.SRTalepleris);
                    _entities.TIBasvuruAraRaporKomites.RemoveRange(araRapor.TIBasvuruAraRaporKomites);
                    araRapor.TIBasvuru.AktifTIBasvuruAraRaporID = araRapor.TIBasvuru.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID != tiBasvuruAraRaporId).OrderByDescending(o => o.AraRaporSayisi).Select(s => s.TIBasvuruAraRaporID).FirstOrDefault().ToNullIntZero();
                    _entities.TIBasvuruAraRapors.Remove(araRapor);
                    _entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    LogIslemleri.LogEkle("TIbasvuruAraRapor", LogCrudType.Delete, araRapor.ToJson());
                    mmMessage.Messages.Add(araRapor.AraRaporSayisi + ". Rapor sistemden silindi.");

                }
                catch (Exception ex)
                {
                    mmMessage.Messages.Add(araRapor.AraRaporSayisi + ". Rapor sistemden silinemedi.");
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult DegerlendirmeLinkView(Guid? uniqueId)
        {
            var model = _entities.TIBasvuruAraRaporKomites.First(p => p.UniqueID == uniqueId);
            return View(model);
        }
        [Authorize]
        public ActionResult DegerlendirmeLinkiGonder(int tiBasvuruId, int tiBasvuruAraRaporId, Guid? uniqueId, string eMail)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez İzleme Raporu Değerlendirme Linki Gönderme İşlemi"
            };
            var araRapor = _entities.TIBasvuruAraRapors.First(p => p.TIBasvuruID == tiBasvuruId && p.TIBasvuruAraRaporID == tiBasvuruAraRaporId);
            var basvuru = araRapor.TIBasvuru;
            var tiTezDegerlendirmeDuzeltme = RoleNames.TiTezDegerlendirmeDuzeltme.InRoleCurrent();
            if (!tiTezDegerlendirmeDuzeltme && basvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Değerlendirme Linki Göndermek İçin Yetkili Değilsiniz.");
            }
            else if (!tiTezDegerlendirmeDuzeltme && araRapor.TIBasvuruAraRaporKomites.Count == araRapor.TIBasvuruAraRaporKomites.Count(c => c.IsBasarili.HasValue))
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Değerlendirme işlemi tüm Komite üyeler tarafından tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
            }
            else if (eMail.IsNullOrWhiteSpace())
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("E-Posta Giriniz");
            }
            else if (!eMail.ToIsValidEmail())
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("E-Posta Formatı Uygun Değildir.");
            }
            else
            {
                if (uniqueId.HasValue)
                {
                    var uye = araRapor.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.UniqueID == uniqueId);
                    if (uye == null) mMessage.Messages.Add("Değerlendirme Linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı Yenileyip Tekrar Deneyiniz.");
                    else
                    {
                        uye.EMail = eMail;
                        _entities.SaveChanges();

                    }
                }
                var messages = TiBus.SendMailTiDegerlendirmeLink(tiBasvuruAraRaporId, uniqueId, true);
                if (messages.IsSuccess)
                {

                    araRapor.IsBasariliOrBasarisiz = null;
                    araRapor.IsOyBirligiOrCoklugu = null;
                    if (araRapor.TIBasvuruAraRaporKomites.Any(a => a.IsBasarili.HasValue))
                    {
                        araRapor.TIBasvuruAraRaporDurumID = TiAraRaporDurumuEnum.DegerlendirmeSureciBaslatildi;
                    }
                    else
                    {
                        araRapor.TIBasvuruAraRaporDurumID = TiAraRaporDurumuEnum.ToplantiBilgileriGirildi;
                    }
                    _entities.SaveChanges();
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


    }
}