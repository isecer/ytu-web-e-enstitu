using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models.ObsService;

namespace LisansUstuBasvuruSistemi.Controllers
{

    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TIBasvuruController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd, int? TIBasvuruID, int? KullaniciID, Guid? IsDegerlendirme = null)
        {
            if (!UserIdentity.Current.IsAuthenticated && IsDegerlendirme == null) return RedirectToActionPermanent("Login", "Account");

            return Index(new fmTIBasvuru() { TIBasvuruID = TIBasvuruID, KullaniciID = KullaniciID, IsDegerlendirme = IsDegerlendirme, PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(fmTIBasvuru model, string EKD)
        {
            if (!UserIdentity.Current.IsAuthenticated && model.IsDegerlendirme == null) return RedirectToActionPermanent("Login", "Account");

            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            #region bilgiModel
            var bbModel = new BasvuruBilgiModel();
            if (model.IsDegerlendirme == null)
            {
                bbModel.SistemBasvuruyaAcik = TIAyar.BasvurusuAcikmi.getAyarTI(_EnstituKod, "false").ToBoolean().Value;

                if (model.KullaniciID.HasValue && !RoleNames.KullaniciAdinaTezIzlemeBasvurusuYap.InRoleCurrent()) model.KullaniciID = UserIdentity.Current.Id;
                model.KullaniciID = model.KullaniciID ?? UserIdentity.Current.Id;
                var kullKayitB = Management.KullaniciKayitBilgisiGuncelle(model.KullaniciID.Value);
                var Kul = _db.Kullanicilars.Where(p => p.KullaniciID == model.KullaniciID).First();

                if (Kul.YtuOgrencisi)
                {
                    if (kullKayitB.KayitVar == false)
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Profil bilgilerinizde giriş yaptığınız YTU Lüsansüstü Öreğnci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";
                    }
                    else
                    {
                        if ((Kul.OgrenimTipKod == OgrenimTipi.Doktra || Kul.OgrenimTipKod == OgrenimTipi.ButunlesikDoktora) && Kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                        {
                            bbModel.KullaniciTipYetki = true;
                            var DonemBilgi = _db.Donemlers.Where(p => p.DonemID == Kul.KayitDonemID.Value).FirstOrDefault();
                            if (DonemBilgi != null)
                            {
                                bbModel.KayitDonemi = Kul.KayitYilBaslangic + "/" + (Kul.KayitYilBaslangic + 1);
                            }
                            if (Kul.KayitTarihi.HasValue) bbModel.KayitDonemi += " " + Kul.KayitTarihi.ToString("dd.MM.yyyy");
                            model.AktifOgrenimIcinBasvuruVar = _db.TIBasvurus.Any(a => a.KullaniciID == Kul.KullaniciID && a.OgrenciNo == Kul.OgrenciNo);
                        }
                        else
                        {
                            bbModel.KullaniciTipYetki = false;
                            bbModel.KullaniciTipYetkiYokMsj = "Tez izleme başvurusu yapılabilmesi için Doktora öğrencisi olunması gerekmektedir.";

                        }
                        if (bbModel.KullaniciTipYetki)
                        {
                            if (Kul.Programlar.AnabilimDallari.EnstituKod != _EnstituKod)
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
                    bbModel.KullaniciTipYetkiYokMsj = "Profil bilgilerinizde YTU Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için Tez İzleme başvurusu yapamazsınız. Sağ üst köşeden profil bilgilerinizi düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
                }
                if (bbModel.KullaniciTipYetki)
                {
                    var otb = _db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod && p.OgrenimTipKod == Kul.OgrenimTipKod).First();

                    bbModel.OgrenimDurumAdi = Kul.OgrenimDurumlari.OgrenimDurumAdi;
                    bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                    bbModel.AnabilimdaliAdi = Kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    bbModel.ProgramAdi = Kul.Programlar.ProgramAdi;
                    bbModel.OgrenciNo = Kul.OgrenciNo;
                }


                bbModel.Enstitü = _db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
                bbModel.Kullanici = Kul;
            }
            #endregion 
            var nowDate = DateTime.Now;
            var q = from s in _db.TIBasvurus.Where(p => model.IsDegerlendirme.HasValue ? p.TIBasvuruAraRapors.Any(a => a.TIBasvuruAraRaporKomites.Any(a2 => a2.UniqueID == model.IsDegerlendirme)) : true)
                    join e in _db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _db.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _db.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _db.Programlars on k.ProgramKod equals pr.ProgramKod
                    join ab in _db.AnabilimDallaris on k.Programlar.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in _db.Enstitulers on e.EnstituKod equals en.EnstituKod
                    join ktip in _db.KullaniciTipleris on k.KullaniciTipID equals ktip.KullaniciTipID
                    join ard in _db.TIBasvuruAraRapors on s.AktifTIBasvuruAraRaporID equals ard.TIBasvuruAraRaporID into defard
                    from Ard in defard.DefaultIfEmpty()
                    where s.EnstituKod == _EnstituKod && s.KullaniciID == (model.IsDegerlendirme.HasValue ? s.KullaniciID : model.KullaniciID.Value)
                    select new frTIBasvuru
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
                        AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        TcPasaPortNo = s.Kullanicilar.TcKimlikNo != null ? s.Kullanicilar.TcKimlikNo : s.Kullanicilar.PasaportNo,
                        OgrenciNo = s.OgrenciNo,
                        Kullanicilar = s.Kullanicilar,
                        ResimAdi = s.Kullanicilar.ResimAdi,
                        KullaniciTipID = s.Kullanicilar.KullaniciTipID,
                        KullaniciTipAdi = ktip.KullaniciTipAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifTIBasvuruAraRaporID = s.AktifTIBasvuruAraRaporID,
                        TIAraRaporAktifDonemAdi = Ard == null ? "Rapor Girişi Yapılmadı" : (Ard.DonemBaslangicYil + " / " + (Ard.DonemBaslangicYil + 1) + " " + (Ard.DonemID == 1 ? "Güz" : "Bahar")),
                        TIAraRaporRaporDurumAdi = Ard == null ? "Rapor Girişi Yapılmadı" : Ard.TIBasvuruAraRaporDurumlari.TIBasvuruAraRaporDurumAdi,
                        AraRaporSayisi = Ard == null ? (int?)null : Ard.AraRaporSayisi,
                        TIAraRaporAktifDonemID = Ard == null ? null : (Ard.DonemBaslangicYil + "" + Ard.DonemID),
                        TIAraRaporRaporDurumID = Ard == null ? 0 : Ard.TIBasvuruAraRaporDurumID,
                        IsOyBirligiOrCouklugu = Ard != null ? Ard.IsOyBirligiOrCouklugu : (bool?)null,
                        IsBasariliOrBasarisiz = Ard != null ? Ard.IsBasariliOrBasarisiz : (bool?)null

                    };

            // if (model.TIDurumID.HasValue) q = q.Where(p => p.TIDurumID == model.TIDurumID.Value);
            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            //IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.BasvuruTarihi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;

            var qdata = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToList();

            model.Data = qdata;

            ViewBag.IndexModel = IndexModel;

            ViewBag.bModel = bbModel;
            return View(model);
        }

        [Authorize]
        public ActionResult BasvuruYap(int? TIBasvuruID, int? KullaniciID = null, string EnstituKod = "", string EKD = "")
        {
            var model = new KmTIBasvuru();
            var _MmMessage = new MmMessage();
            EnstituKod = Management.getSelectedEnstitu(EKD);


            if (TIBasvuruID.HasValue || KullaniciID.HasValue)
            {
                if (KullaniciID.HasValue)
                    if (RoleNames.TIGelenBasvuruKayit.InRoleCurrent() == false)
                        KullaniciID = UserIdentity.Current.Id;
                if (TIBasvuruID.HasValue)
                {
                    var basvuru = _db.TIBasvurus.Where(p => p.TIBasvuruID == TIBasvuruID.Value).FirstOrDefault();
                    if (KullaniciID.HasValue == false) KullaniciID = basvuru.KullaniciID;
                }
            }
            else
            {
                KullaniciID = UserIdentity.Current.Id;
            }
            var studentInfo = Management.KullaniciKayitBilgisiGuncelle(KullaniciID.Value);
            var kul = _db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).FirstOrDefault();

            _MmMessage = Management.getAktifTezIzlemeSurecKontrol(EnstituKod, KullaniciID, TIBasvuruID);

            if (model.TIBasvuruID <= 0 && _MmMessage.IsSuccess)
            {
                var DanismanTC = studentInfo.OgrenciInfo.DANISMAN_TC1;
                if (!kul.DanismanID.HasValue && (DanismanTC.IsNullOrWhiteSpace() || DanismanTC.Length != 11))
                {
                    _MmMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    _MmMessage.IsSuccess = false;
                }
                else if (!kul.DanismanID.HasValue)
                {

                    _MmMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
                    _MmMessage.IsSuccess = false;
                }
                //else if (DanismanBilgi.KullaniciID == -1)
                //{
                //    _MmMessage.Messages.Add("Tez danışmanı bilginiz GSİS sisteminden alınamadı. GSİS sisteminde tez durumunuzun devam ediyor olması ve danışmanınızın tanımlı olması gerekmektedir. Başvuru yapabilmeniz için bu durumu enstitü yetkililerine bildiriniz.");
                //    _MmMessage.IsSuccess = false;
                //}

            }
            if (_MmMessage.IsSuccess)
            {
                model.KayitTarihi = kul.KayitTarihi;
                if (TIBasvuruID.HasValue)
                {
                    var Basvuru = _db.TIBasvurus.Where(p => p.TIBasvuruID == TIBasvuruID).First();
                    model.EnstituKod = Basvuru.EnstituKod;
                    model.TIBasvuruID = Basvuru.TIBasvuruID;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = Basvuru.KullaniciID;
                    model.KullaniciTipID = Basvuru.KullaniciTipID;
                    model.ResimAdi = Basvuru.ResimAdi;
                    model.Ad = Basvuru.Ad;
                    model.Soyad = Basvuru.Soyad;
                    model.OgrenciNo = Basvuru.OgrenciNo;
                    model.TcKimlikNo = Basvuru.TcKimlikNo;
                    model.PasaportNo = Basvuru.PasaportNo;
                    model.UyrukKod = Basvuru.UyrukKod;
                    model.OgrenimTipKod = Basvuru.OgrenimTipKod;



                }
                else
                {
                    model.EnstituKod = EnstituKod;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = KullaniciID.Value;
                    model.KullaniciTipID = kul.KullaniciTipID;
                    model.ResimAdi = kul.ResimAdi;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.TcKimlikNo = kul.TcKimlikNo;
                    model.PasaportNo = kul.PasaportNo;
                    model.OgrenimTipKod = kul.OgrenimTipKod.Value;

                }
                model.OgrenimTipAdi = _db.OgrenimTipleris.Where(p => p.EnstituKod == EnstituKod && p.OgrenimTipKod == kul.OgrenimTipKod).First().OgrenimTipAdi;
                var progLng = kul.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;


                ViewBag._MmMessage = _MmMessage;
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, _MmMessage.Messages.ToArray());
                return RedirectToAction("Index", new { KullaniciID = KullaniciID });
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BasvuruYap(KmTIBasvuru kModel, string EKD)
        {
            var _MmMessage = new MmMessage();


            if (RoleNames.TIGelenBasvuruKayit.InRoleCurrent() == false) { kModel.KullaniciID = UserIdentity.Current.Id; }
            _MmMessage = Management.getAktifTezIzlemeSurecKontrol(kModel.EnstituKod, kModel.KullaniciID, kModel.TIBasvuruID.toNullIntZero());

            var kullKayitB = Management.KullaniciKayitBilgisiGuncelle(kModel.KullaniciID);
            var kul = _db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).FirstOrDefault();
            kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;

            var DanismanBilgi = new Kullanicilar();
            if (kModel.TIBasvuruID <= 0 && _MmMessage.Messages.Count == 0 && DanismanBilgi.KullaniciID <= 0)
            {
                if (!kul.DanismanID.HasValue)
                {
                    _MmMessage.Messages.Add("Tez Danışmanınıza ait sistemde kullanıcı hesabı bilgisine rastlanmadı.");
                }

            }

            if (_MmMessage.Messages.Count == 0)
            {
                kModel.BasvuruSonDonemSecilecekDersKodlari = TIAyar.SonDonemKayitOlunmasiGerekenDersKodlari.getAyarTI(kModel.EnstituKod, "");
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

                var data = new TIBasvuru();
                bool IsNewRecord = false;
                if (kModel.TIBasvuruID <= 0)
                {
                    IsNewRecord = true;
                    kModel.BasvuruTarihi = DateTime.Now;

                    data = _db.TIBasvurus.Add(new TIBasvuru
                    {
                        EnstituKod = kModel.EnstituKod,
                        BasvuruSonDonemSecilecekDersKodlari = kModel.BasvuruSonDonemSecilecekDersKodlari,
                        UniqueID = Guid.NewGuid(),
                        BasvuruTarihi = kModel.BasvuruTarihi,
                        KullaniciID = kModel.KullaniciID,
                        KullaniciTipID = kModel.KullaniciTipID,
                        ResimAdi = kModel.ResimAdi,
                        Ad = kModel.Ad,
                        Soyad = kModel.Soyad,
                        UyrukKod = kModel.UyrukKod,
                        TcKimlikNo = kModel.TcKimlikNo,
                        PasaportNo = kModel.PasaportNo,
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
                    data.TezDanismanID = DanismanBilgi.KullaniciID;
                    _db.SaveChanges();
                }
                else
                {

                    data = _db.TIBasvurus.Where(p => p.TIBasvuruID == kModel.TIBasvuruID).First();
                    data.EnstituKod = kModel.EnstituKod;
                    data.BasvuruTarihi = kModel.BasvuruTarihi;
                    data.KullaniciID = kModel.KullaniciID;
                    data.KullaniciTipID = kModel.KullaniciTipID;
                    data.ResimAdi = kModel.ResimAdi;
                    data.Ad = kModel.Ad;
                    data.Soyad = kModel.Soyad;
                    data.UyrukKod = kModel.UyrukKod;
                    data.TcKimlikNo = kModel.TcKimlikNo;
                    data.PasaportNo = kModel.PasaportNo;
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
                    _db.SaveChanges();


                }
                LogIslemleri.LogEkle("TIBasvuru", IsNewRecord ? IslemTipi.Insert : IslemTipi.Update, data.ToJson());

                return RedirectToAction("Index", new { data.TIBasvuruID });
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, _MmMessage.Messages.ToArray());
            }

            ViewBag._MmMessage = _MmMessage;
            return View(kModel);
        }

        [Authorize]
        public ActionResult GetTIAraRaporFormu(int TIBasvuruID, int? TIBasvuruAraRaporID)
        {
            var Model = new TIAraRaporFormuModel();

            Model.TIBasvuruID = TIBasvuruID;
            var mMessage = new MmMessage();
            string View = "";
            var TIBasvuru = _db.TIBasvurus.Where(p => p.TIBasvuruID == TIBasvuruID).First();
            var TIBasvuruAraRapor = TIBasvuru.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID == TIBasvuruAraRaporID).FirstOrDefault();
            var DegerlendirmeYetki = RoleNames.TITezDegerlendirmeYap.InRoleCurrent() || TIBasvuru.KullaniciID == UserIdentity.Current.Id;
            var studentInfo = Management.KullaniciKayitBilgisiGuncelle(TIBasvuru.KullaniciID);
            var kul = _db.Kullanicilars.Where(p => p.KullaniciID == TIBasvuru.KullaniciID).First();
            if (!DegerlendirmeYetki)
            {
                mMessage.Messages.Add("Ara rapor formu kayıt yetkisine sahip değilsiniz.");

            }
            else if (TIBasvuruAraRapor != null && TIBasvuruAraRapor.TIBasvuruAraRaporDurumID > TIAraRaporDurumu.ToplantiBilgileriGirildi)
            {
                mMessage.Messages.Add("Komite üyelerine değerlendirme linki gönderildikten sonra rapor bilgisinde değişiklik yapamazsınız. ");
            }
            else if (!TIBasvuruAraRaporID.HasValue && !kul.DanismanID.HasValue)
            {
                var DanismanTC = studentInfo.OgrenciInfo.DANISMAN_TC1;
                if (!kul.DanismanID.HasValue && (DanismanTC.IsNullOrWhiteSpace() || DanismanTC.Length != 11))
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
                var DonemBilgi = (TIBasvuruAraRapor == null ? DateTime.Now : TIBasvuruAraRapor.RaporTarihi).ToAraRaporDonemBilgi();

                var ogrenciBilgi = Management.StudentControl(TIBasvuru.TcKimlikNo);

                var sondonemKayitolmasiGerekenDersKodlari = TIAyar.SonDonemKayitOlunmasiGerekenDersKodlari.getAyarTI(TIBasvuru.EnstituKod, "");

                var KayitYapilacakDersKodlaris = !TIBasvuruAraRaporID.HasValue ? sondonemKayitolmasiGerekenDersKodlari.Split(',').ToList() : new List<string>();
                if (KayitYapilacakDersKodlaris.Any() && KayitYapilacakDersKodlaris.Where(p => ogrenciBilgi.AktifDonemDers.DersKodNums.Any(a => a == p)).Count() != KayitYapilacakDersKodlaris.Count)
                {
                    mMessage.Messages.Add("Tez izleme raporunu başlatabilmeniz için " + DonemBilgi.DonemAdiLong + " döneminde " + sondonemKayitolmasiGerekenDersKodlari + " kodlu derslere kayıt olmanız gerekmektedir.");
                }
                else if (TIBasvuru.TIBasvuruAraRapors.Any(p => p.TIBasvuruAraRaporID != TIBasvuruAraRaporID && p.DonemBaslangicYil == DonemBilgi.BaslangicYil && p.DonemID == DonemBilgi.DonemID))
                {
                    mMessage.Messages.Add(DonemBilgi.DonemAdiLong + " döneminde zaten bir tez izleme raporu başvurunuz bulunmakta!");
                }
                else if (UserIdentity.Current.Id == TIBasvuru.KullaniciID && TIBasvuruAraRapor != null && TIBasvuruAraRapor.TIBasvuruAraRaporKomites.Any(a => a.LinkGonderenID.HasValue))
                {
                    mMessage.Messages.Add("Komite üyelerine değerlendirme linki gönderildiğinden Rapor bilgisinde değişiklik yapamazsınız. ");
                }

                if (mMessage.Messages.Count == 0)
                {

                    var Tiks = ogrenciBilgi.TezIzlJuriBilgileri.Where(p => p.TEZ_DANISMAN != "1").ToList();
                    if (Tiks.Count < 2)
                    {
                        mMessage.Messages.Add("Tik üye bilgileri OBS sisteminden alınamadı.");
                    }

                    if (mMessage.Messages.Count > 0)
                    {
                        mMessage.Messages.Add("Rapor formunu oluşturabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                    }
                    if (mMessage.Messages.Count == 0)
                    {
                        var obsTik1 = Tiks[0];
                        var obsTik2 = Tiks[1];

                        var cmbUnvanList = Management.cmbMezuniyetJofUnvanlar(true);
                        var cmbUniversiteList = Management.cmbGetAktifUniversiteler(true);

                        Model.TezBaslikTr = studentInfo.OgrenciTez.TEZ_BASLIK;
                        Model.TezBaslikEn = studentInfo.OgrenciTez.TEZ_BASLIK_ENG;
                        Model.IsTezDiliTr = studentInfo.IsTezDiliTr;
                        Model.OgrenciAdSoyad = TIBasvuru.Ad + " " + TIBasvuru.Soyad + " - " + TIBasvuru.OgrenciNo;
                        Model.OgrenciAnabilimdaliProgramAdi = TIBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " - " + TIBasvuru.Programlar.ProgramAdi;
                        if (TIBasvuruAraRapor != null)
                        {
                            Model.AraRaporSayisi = TIBasvuruAraRapor.AraRaporSayisi;
                            Model.TIBasvuruAraRaporID = TIBasvuruAraRapor.TIBasvuruAraRaporID;
                            Model.IsTezDiliTr = Model.IsTezDiliTr;
                            Model.TezBaslikEn = TIBasvuruAraRapor.TezBaslikEn;
                            Model.IsTezDiliDegisecek = TIBasvuruAraRapor.IsTezDiliDegisecek;
                            Model.YeniTezDiliTr = TIBasvuruAraRapor.YeniTezDiliTr;
                            Model.SinavAdi = TIBasvuruAraRapor.SinavAdi;
                            Model.SinavPuani = TIBasvuruAraRapor.SinavPuani;
                            Model.SinavYili = TIBasvuruAraRapor.SinavYili;
                            Model.IsTezBasligiDegisti = TIBasvuruAraRapor.IsTezBasligiDegisti;
                            Model.YeniTezBaslikTr = TIBasvuruAraRapor.YeniTezBaslikTr;
                            Model.YeniTezBaslikEn = TIBasvuruAraRapor.YeniTezBaslikEn;
                            Model.TezBasligiDegisimGerekcesi = TIBasvuruAraRapor.TezBasligiDegisimGerekcesi;
                            Model.TICalismaRaporDosyaAdi = TIBasvuruAraRapor.TICalismaRaporDosyaAdi;
                            Model.TICalismaRaporDosyaYolu = TIBasvuruAraRapor.TICalismaRaporDosyaYolu;
                            Model.IsYokDrBursiyeriVar = TIBasvuruAraRapor.IsYokDrBursiyeriVar;
                            Model.YokDrOncelikliAlan = TIBasvuruAraRapor.YokDrOncelikliAlan;
                            Model.KomiteList = TIBasvuruAraRapor.TIBasvuruAraRaporKomites.Select(s => new KrTIBasvuruAraRaporKomite
                            {
                                TIBasvuruAraRaporID = s.TIBasvuruAraRaporID,
                                TIBasvuruAraRaporKomiteID = s.TIBasvuruAraRaporKomiteID,
                                JuriTipAdi = s.JuriTipAdi,
                                UnvanAdi = s.UnvanAdi,
                                SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", s.UnvanAdi),
                                AdSoyad = s.AdSoyad,
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

                            var tD = Model.KomiteList.Where(p => p.JuriTipAdi == "TezDanismani").First();
                            if (tD.AdSoyad.ToUpper() != studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper() || tD.UnvanAdi.ToUpper() != studentInfo.OgrenciInfo.DANISMAN_UNVAN1.ToUpper())
                                mMessage.Messages.Add("Tez danışmanı bilgileri değişmiştir.<br /> Önceki Veri: " + tD.UnvanAdi + " " + tD.AdSoyad + " Yeni Veri: " + studentInfo.OgrenciInfo.DANISMAN_UNVAN1 + " " + studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1);
                            tD.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tD.UniversiteID);
                            if (tD.AdSoyad.ToUpper().Trim() != studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper().ToUpper().Trim() || tD.UnvanAdi.ToUpper() != studentInfo.OgrenciInfo.DANISMAN_UNVAN1.ToUpper())
                            {
                                tD.AdSoyad = studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper();
                                tD.UnvanAdi = studentInfo.OgrenciInfo.DANISMAN_UNVAN1.ToMezuniyetJuriUnvanAdi();
                            }
                            tD.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tD.UnvanAdi);





                            var tik1 = Model.KomiteList.Where(p => p.JuriTipAdi == "TikUyesi1").First();
                            if (tik1.AdSoyad.ToUpper() != obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper() || tik1.UnvanAdi.ToUpper() != obsTik1.TEZ_IZLEME_JURI_UNVAN.ToUpper())
                                mMessage.Messages.Add("Tik1 Üyesi bilgileri değişmiştir.<br /> Önceki Veri: " + tik1.UnvanAdi + " " + tik1.AdSoyad + " Yeni Veri: " + obsTik1.TEZ_IZLEME_JURI_UNVAN + " " + obsTik1.TEZ_IZLEME_JURI_ADSOY);
                            tik1.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tik1.UniversiteID);
                            if (tik1.AdSoyad.ToUpper() != obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper() || tik1.UnvanAdi.ToUpper() != obsTik1.TEZ_IZLEME_JURI_UNVAN.ToUpper())
                            {
                                tik1.AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper();
                                tik1.UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi();
                            }
                            tik1.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tik1.UnvanAdi);


                            var tik2 = Model.KomiteList.Where(p => p.JuriTipAdi == "TikUyesi2").First();
                            if (tik2.AdSoyad.ToUpper() != obsTik2.TEZ_IZLEME_JURI_ADSOY.ToUpper() || tik2.UnvanAdi.ToUpper() != obsTik2.TEZ_IZLEME_JURI_UNVAN.ToUpper())
                                mMessage.Messages.Add("Tik2 Üyesi bilgileri değişmiştir.<br /> Önceki Veri: " + tik2.UnvanAdi + " " + tik2.AdSoyad + " Yeni Veri: " + obsTik2.TEZ_IZLEME_JURI_UNVAN + " " + obsTik2.TEZ_IZLEME_JURI_ADSOY);

                            tik2.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tik2.UniversiteID);
                            if (tik2.AdSoyad.ToUpper() != obsTik2.TEZ_IZLEME_JURI_ADSOY.ToUpper() || tik2.UnvanAdi.ToUpper() != obsTik2.TEZ_IZLEME_JURI_UNVAN.ToUpper())
                            {
                                tik2.AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY.ToUpper();
                                tik2.UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi();

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


                            Model.AraRaporSayisi = ogrenciBilgi.AraRaporMaxNo;
                            var tdKul = _db.Kullanicilars.Where(p => p.KullaniciID == kul.DanismanID).First();
                            var TdBilgi = new KrTIBasvuruAraRaporKomite
                            {
                                JuriTipAdi = "TezDanismani",
                                UnvanAdi = studentInfo.OgrenciInfo.DANISMAN_UNVAN1.ToMezuniyetJuriUnvanAdi(),
                                AdSoyad = studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper(),
                                EMail = tdKul.EMail,
                                UniversiteID = Management.UniversiteYtuKod,
                                // AnabilimdaliProgramAdi = TezDanismani.Birimler!=null?TezDanismani.Birimler.BirimAdi:"",

                            };
                            TdBilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", TdBilgi.UnvanAdi);
                            TdBilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", TdBilgi.UniversiteID);
                            Model.KomiteList.Add(TdBilgi);


                            var Tk1Bilgi = new KrTIBasvuruAraRaporKomite
                            {
                                JuriTipAdi = "TikUyesi1",
                                AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi(),
                                EMail = obsTik1.TEZ_IZLEME_JURI_EPOSTA
                            };
                            Tk1Bilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", Tk1Bilgi.UnvanAdi);
                            Tk1Bilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", Tk1Bilgi.UniversiteID);
                            Model.KomiteList.Add(Tk1Bilgi);


                            var Tk2Bilgi = new KrTIBasvuruAraRaporKomite
                            {
                                JuriTipAdi = "TikUyesi2",
                                AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi()
                            };
                            Tk2Bilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", Tk2Bilgi.UnvanAdi);
                            Tk2Bilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", Tk2Bilgi.UniversiteID);
                            Model.KomiteList.Add(Tk2Bilgi);


                        }

                        Model.SelectedTabID = 1;

                        Model.SListUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption");
                        Model.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption");
                        Model.SListAraRaporSayisi = new SelectList(Management.cmbAraRaporSayisi(true), "Value", "Caption", Model.AraRaporSayisi);

                        mMessage.MessageType = Msgtype.Information;
                        mMessage.IsSuccess = true;
                        View = Management.RenderPartialView("TIBasvuru", "TIAraRaporFormu", Model);
                    }
                }
            }


            if (mMessage.MessageType != Msgtype.Information) mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var strView = Management.RenderPartialView("Ajax", "getMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = View,
                Messages = strView
            }.toJsonResult();

        }
        [Authorize]
        [ValidateInput(false)]
        public ActionResult TIAraRaporFormuPost(TIAraRaporFormuModel kModel, bool SaveData = false)
        {
            var mMessage = new MmMessage();
            mMessage.MessageType = Msgtype.Success;
            mMessage.Title = "Tez İzleme Rapor Formu Oluşturma İşlemi";
            bool IsYeniJO = true;
            var TIBasvuru = _db.TIBasvurus.Where(p => p.TIBasvuruID == kModel.TIBasvuruID).First();
            var TIBasvuruAraRapor = TIBasvuru.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID == kModel.TIBasvuruAraRaporID).FirstOrDefault();
            var DegerlendirmeYetki = RoleNames.TITezDegerlendirmeYap.InRoleCurrent() || TIBasvuru.KullaniciID == UserIdentity.Current.Id;
            var studentInfo = Management.KullaniciKayitBilgisiGuncelle(TIBasvuru.KullaniciID);
            var kul = _db.Kullanicilars.Where(p => p.KullaniciID == TIBasvuru.KullaniciID).First();

            if (!DegerlendirmeYetki)
            {
                mMessage.Messages.Add("Ara rapor formu kayıt yetkisine sahip değilsiniz.");
            }
            var DanismanTC = studentInfo.OgrenciInfo.DANISMAN_TC1;
            if (TIBasvuruAraRapor == null && !kul.DanismanID.HasValue && (DanismanTC.IsNullOrWhiteSpace() || DanismanTC.Length != 11))
            {
                mMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
            }
            else if (TIBasvuruAraRapor == null && !kul.DanismanID.HasValue)
            {
                mMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
            }
            else
            {
                IsYeniJO = TIBasvuruAraRapor == null;
                bool IsDegisiklikVar = false;
                var DonemBilgi = (IsYeniJO ? DateTime.Now : TIBasvuruAraRapor.RaporTarihi).ToAraRaporDonemBilgi();
                var DonemdeVerilenDersBilgileri = IsYeniJO ? Management.StudentControl(TIBasvuru.TcKimlikNo) : new StudentControl();
                var KayitYapilacakDersKodlaris = IsYeniJO ? TIAyar.SonDonemKayitOlunmasiGerekenDersKodlari.getAyarTI(TIBasvuru.EnstituKod, "").Split(',').ToList() : new List<string>();

                if (TIBasvuru.TIBasvuruAraRapors.Any(p => p.TIBasvuruAraRaporID != kModel.TIBasvuruAraRaporID && p.DonemBaslangicYil == DonemBilgi.BaslangicYil && p.DonemID == DonemBilgi.DonemID))
                {
                    mMessage.Messages.Add(DonemBilgi.DonemAdiLong + " döneminde zaten bir tez izleme raporu başvurunuz bulunmakta!");
                }
                else if (KayitYapilacakDersKodlaris.Any() && DonemdeVerilenDersBilgileri.AktifDonemDers.DersKodNums.Where(p => KayitYapilacakDersKodlaris.Any(a => a == p)).Count() != KayitYapilacakDersKodlaris.Count)
                {
                    mMessage.Messages.Add("Tez izleme raporunu başlatabilmeniz için " + DonemBilgi.DonemAdiLong + " döneminde " + TIBasvuru.BasvuruSonDonemSecilecekDersKodlari + " kodlu derslere kayıt olmanız gerekmektedir.");
                }
                else if (TIBasvuruAraRapor != null && TIBasvuruAraRapor.TIBasvuruAraRaporDurumID > TIAraRaporDurumu.ToplantiBilgileriGirildi)
                {
                    mMessage.Messages.Add("Komite üyelerine değerlendirme yaptıktan sonra rapor bilgisinde değişiklik yapamazsınız. ");
                }
                if (mMessage.Messages.Count == 0)
                {
                    bool RsSuccess = true;
                    if (kModel.AraRaporSayisi <= 0) { RsSuccess = false; mMessage.Messages.Add("Rapor Sayısını Seçiniz."); }
                    else if (TIBasvuru.TIBasvuruAraRapors.Any(p => p.TIBasvuruAraRaporID != kModel.TIBasvuruAraRaporID && p.AraRaporSayisi >= kModel.AraRaporSayisi))
                    {
                        RsSuccess = false;
                        mMessage.Messages.Add("Rapor sayısı daha önceki raporlarda girilen rapor sayısından küçük yada eşit olamaz!");
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (RsSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = "AraRaporSayisi" });

                    if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
                        mMessage.Messages.Add("Tez Başlığı Bilgisi Boş Bırakılamaz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikTr.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "TezOrjinalBasligi" });
                    if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
                        mMessage.Messages.Add("Tez Başlığı Çevirisi Bilgisi Boş Bırakılamaz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.TezBaslikEn.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "TezOrjinalBasligiCevirisi" });
                    if (kModel.IsTezDiliDegisecek)
                    {
                        if (!kModel.YeniTezDiliTr.HasValue) mMessage.Messages.Add("Yeni Tez Dili Bilgisi Boş Bırakılamaz.");
                        else
                        {
                            bool IsSuccessSinavPuan = true;
                            if (kModel.YeniTezDiliTr == false)
                            {
                                if (kModel.SinavAdi.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("Dil Sınavı Adı bilgisi boş bırakılamaz.");
                                }
                                if (kModel.SinavPuani.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("Dil Sınavı Puanı bilgisi boş bırakılamaz.");
                                    IsSuccessSinavPuan = false;
                                }
                                else
                                {
                                    var SinavPuanKontroluYap = TIAyar.SinavPuanGirisKontroluYapilsin.getAyarTI(TIBasvuru.EnstituKod, "false").ToBoolean().Value;
                                    if (SinavPuanKontroluYap)
                                    {
                                        kModel.SinavPuani = kModel.SinavPuani.Replace(" ", "").Replace(".", ",");
                                        var IsSinavPuaniSayi = kModel.SinavPuani.IsNumberX();
                                        if (!IsSinavPuaniSayi)
                                        {
                                            mMessage.Messages.Add("Dil Sınavı Puanı girişi sayıdan oluşmalıdır.");
                                            IsSuccessSinavPuan = false;
                                        }
                                        else
                                        {
                                            var PuanKriteri = TIAyar.OgrenciMinSinavPuan.getAyarTI(TIBasvuru.EnstituKod, "60").ToInt().Value;
                                            var Puan = Convert.ToDouble(kModel.SinavPuani);
                                            if (PuanKriteri > Puan || Puan > 100)
                                            {
                                                mMessage.Messages.Add("Dil Sınavı puanı girişi " + PuanKriteri + " ile 100 notları arasında olmalıdır.");
                                                IsSuccessSinavPuan = false;
                                            }
                                        }
                                    }
                                }
                                bool IsSuccessSinavYil = true;
                                if (!kModel.SinavYili.HasValue)
                                {
                                    mMessage.Messages.Add("Dil Sınavı Yılı bilgisi giriniz.");
                                    IsSuccessSinavYil = false;
                                }
                                else if (kModel.SinavYili.Value > DateTime.Now.Year)
                                {
                                    mMessage.Messages.Add("Dil Sınavı Yılı bilgisi bulunduğumuz yıldan büyük olamaz.");
                                    IsSuccessSinavYil = false;
                                }
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.SinavAdi.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "SinavAdi" });
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (IsSuccessSinavPuan ? Msgtype.Success : Msgtype.Warning), PropertyName = "SinavPuani" });
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (IsSuccessSinavYil ? Msgtype.Success : Msgtype.Warning), PropertyName = "SinavYili" });

                            }
                        }
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.YeniTezDiliTr.HasValue ? Msgtype.Success : Msgtype.Warning), PropertyName = "YeniTezDiliTr" });
                    }

                    if (kModel.IsTezBasligiDegisti)
                    {
                        if (kModel.YeniTezBaslikTr.IsNullOrWhiteSpace())
                        {
                            mMessage.Messages.Add("Yeni Tez Başlığı Bilgisi Boş Bırakılamaz.");
                        }
                        if (kModel.YeniTezBaslikEn.IsNullOrWhiteSpace())
                        {
                            mMessage.Messages.Add("Yeni Tez Başlığı Çevirisi Bilgisi Boş Bırakılamaz.");
                        }
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.YeniTezBaslikTr.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "YeniTezBasligi" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.YeniTezBaslikEn.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "YeniTezBasligiCevirisi" });

                        if (kModel.TezBasligiDegisimGerekcesi.IsNullOrWhiteSpace())
                        {
                            mMessage.Messages.Add("Tez başlığı değişim gerekçesini giriniz.");
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezBasligiDegisimGerekcesi" });
                        }
                        else mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezBasligiDegisimGerekcesi" });
                    }
                    if (kModel.Dosya == null && kModel.TICalismaRaporDosyaAdi.IsNullOrWhiteSpace()) mMessage.Messages.Add("Çalışma Raporu Dosyası Seçiniz.");
                    else if (kModel.Dosya != null && !kModel.Dosya.FileName.IsPdfFile()) mMessage.Messages.Add("Çalışma Raporu Dosyası PDF türünde olmalıdır.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = ((kModel.Dosya == null && kModel.TICalismaRaporDosyaAdi.IsNullOrWhiteSpace()) || (kModel.Dosya != null && !kModel.Dosya.FileName.IsPdfFile()) ? Msgtype.Warning : Msgtype.Success), PropertyName = "Dosya" });
                    if (!kModel.IsYokDrBursiyeriVar.HasValue)
                    {
                        mMessage.Messages.Add("100/2000 YÖK Bursiyeri Bilgisini Seçiniz");
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsYokDrBursiyeriVar" });
                    }
                    else
                    {
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsYokDrBursiyeriVar" });
                        if (kModel.IsYokDrBursiyeriVar.Value)
                            if (kModel.YokDrOncelikliAlan.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("Öncelikli Alt Alan Adı Giriniz.");
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YokDrOncelikliAlan" });
                            }
                            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YokDrOncelikliAlan" });
                    }
                }
                if (mMessage.Messages.Count > 0)
                {
                    kModel.SelectedTabID = 1;
                }
                if (mMessage.Messages.Count == 0)
                {
                    var SinavPuanKontroluYap = TIAyar.SinavPuanGirisKontroluYapilsin.getAyarTI(TIBasvuru.EnstituKod, "false").ToBoolean().Value;
                    var PuanKriteri = TIAyar.UyelerMinSinavPuan.getAyarTI(TIBasvuru.EnstituKod, "80").ToInt().Value;
                    var TabIDs = kModel.TabID.Select((s, i) => new { TabID = s, Inx = (i + 1) }).ToList();
                    var JuriTipAdis = kModel.JuriTipAdi.Select((s, i) => new { JuriTipAdi = s, Inx = (i + 1) }).ToList();
                    var AdSoyads = kModel.AdSoyad.Select((s, i) => new { AdSoyad = s, Inx = (i + 1) }).ToList();
                    var UnvanAdis = kModel.UnvanAdi.Select((s, i) => new { UnvanAdi = s, Inx = (i + 1) }).ToList();
                    var EMails = kModel.EMail.Select((s, i) => new { EMail = s.Trim(), Inx = (i + 1) }).ToList();
                    var UniversiteIDs = kModel.UniversiteID.Select((s, i) => new { UniversiteID = s, Inx = (i + 1) }).ToList();
                    var AnabilimdaliProgramAdis = kModel.AnabilimdaliProgramAdi.Select((s, i) => new { AnabilimdaliProgramAdi = s, Inx = (i + 1) }).ToList();
                    var DilSinavAdis = kModel.DilSinavAdi.Select((s, i) => new { DilSinavAdi = s, Inx = (i + 1) }).ToList();
                    var IsDilSinaviOrUniversites = kModel.IsDilSinaviOrUniversite.Select((s, i) => new { IsDilSinaviOrUniversite = s, Inx = (i + 1) }).ToList();
                    var DilPuanis = kModel.DilPuani.Select((s, i) => new { DilPuani = s, Inx = (i + 1) }).ToList();
                    var SinavTarihis = kModel.SinavTarihi.Select((s, i) => new { SinavTarihi = s, Inx = (i + 1) }).ToList();

                    var qData = (from ad in AdSoyads
                                 join at in TabIDs on ad.Inx equals at.Inx
                                 join jt in JuriTipAdis on ad.Inx equals jt.Inx
                                 join un in UnvanAdis on ad.Inx equals un.Inx
                                 join em in EMails on ad.Inx equals em.Inx
                                 join uni in UniversiteIDs on ad.Inx equals uni.Inx
                                 join abd in AnabilimdaliProgramAdis on ad.Inx equals abd.Inx
                                 join ids in IsDilSinaviOrUniversites on ad.Inx equals ids.Inx
                                 join ds in DilSinavAdis on ad.Inx equals ds.Inx
                                 join dp in DilPuanis on ad.Inx equals dp.Inx
                                 join st in SinavTarihis on ad.Inx equals st.Inx

                                 select new
                                 {
                                     ad.Inx,
                                     at.TabID,
                                     jt.JuriTipAdi,
                                     AdSoyad = ad.AdSoyad.toStrObjEmptString(),
                                     AdSoyadSuccess = !ad.AdSoyad.IsNullOrWhiteSpace(),
                                     UnvanAdi = un.UnvanAdi.toStrObjEmptString(),
                                     UnvanAdiSuccess = !un.UnvanAdi.IsNullOrWhiteSpace(),
                                     EMail = em.EMail.toStrObjEmptString(),
                                     EMailSuccess = !em.EMail.IsNullOrWhiteSpace() && !em.EMail.ToIsValidEmail(),
                                     uni.UniversiteID,
                                     UniversiteIDSuccess = uni.UniversiteID.HasValue,
                                     AnabilimdaliProgramAdi = abd.AnabilimdaliProgramAdi.toStrObjEmptString(),
                                     AnabilimdaliProgramAdiSuccess = !abd.AnabilimdaliProgramAdi.IsNullOrWhiteSpace(),
                                     IsDilSinaviOrUniversite = kModel.IsTezDiliDegisecek && kModel.YeniTezDiliTr == false ? ids.IsDilSinaviOrUniversite.ToBoolean() : null,
                                     IsDilSinaviOrUniversiteSuccess = !kModel.IsTezDiliDegisecek || kModel.YeniTezDiliTr != false || ids.IsDilSinaviOrUniversite.toBooleanObj().HasValue,
                                     DilSinavAdi = kModel.IsTezDiliDegisecek && kModel.YeniTezDiliTr == false ? ds.DilSinavAdi.toStrObjEmptString() : "",
                                     DilSinavAdiSuccess = !kModel.IsTezDiliDegisecek || kModel.YeniTezDiliTr != false || !ds.DilSinavAdi.IsNullOrWhiteSpace(),
                                     DilPuani = kModel.IsTezDiliDegisecek && kModel.YeniTezDiliTr == false && ids.IsDilSinaviOrUniversite.ToBoolean() == true ? dp.DilPuani.toStrObjEmptString() : null,
                                     DilPuaniSuccessMsg = (!kModel.IsTezDiliDegisecek || kModel.YeniTezDiliTr != false || ids.IsDilSinaviOrUniversite.ToBoolean() == false) ? "" : dp.DilPuani.IsSuccessSinavPuanUye(SinavPuanKontroluYap, PuanKriteri),
                                     SinavTarihi = kModel.IsTezDiliDegisecek && kModel.YeniTezDiliTr == false ? st.SinavTarihi.toIntObj() : null,
                                     SinavTarihiSuccess = !kModel.IsTezDiliDegisecek || kModel.YeniTezDiliTr != false || ids.IsDilSinaviOrUniversite.ToBoolean() == false || (st.SinavTarihi.toIntObj().HasValue && st.SinavTarihi.toIntObj() <= DateTime.Now.Year),

                                 }).Select(s => new
                                 {
                                     Row = s,
                                     IsSuccessRow = s.JuriTipAdi.ToTIUyeFormSuccessRow(kModel.IsTezDiliTr, s.AdSoyadSuccess, s.UnvanAdiSuccess, s.EMailSuccess, s.UniversiteIDSuccess, s.AnabilimdaliProgramAdiSuccess, s.IsDilSinaviOrUniversiteSuccess, s.DilSinavAdiSuccess, s.DilPuaniSuccessMsg.IsNullOrWhiteSpace(), s.SinavTarihiSuccess)

                                 }).ToList();

                    int ErrSelectedTabID = qData.Where(p => p.IsSuccessRow == false).OrderBy(o => o.Row.TabID).Select(s => s.Row.TabID).FirstOrDefault();
                    foreach (var item in qData)
                    {

                        if ((ErrSelectedTabID <= kModel.SelectedTabID && ErrSelectedTabID == item.Row.TabID) && !item.IsSuccessRow)
                        {
                            mMessage.Messages.Add(item.Row.JuriTipAdi + " bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            if (!item.Row.AdSoyadSuccess) mMessage.Messages.Add("Ad Soyad bilgisi");
                            if (!item.Row.UnvanAdiSuccess) mMessage.Messages.Add("Unvan bilgisi");
                            if (!item.Row.EMailSuccess) mMessage.Messages.Add("E-Posta Adresi bilgisi");
                            if (!item.Row.UniversiteIDSuccess) mMessage.Messages.Add("Üniversite bilgisi");
                            if (!item.Row.AnabilimdaliProgramAdiSuccess) mMessage.Messages.Add("Anabilim Dalı bilgisi");
                            if (!item.Row.IsDilSinaviOrUniversiteSuccess) mMessage.Messages.Add("Dil Sınavı bilgi giriş şekli");
                            if (item.Row.IsDilSinaviOrUniversiteSuccess)
                            {
                                if (!item.Row.DilSinavAdiSuccess) mMessage.Messages.Add(item.Row.IsDilSinaviOrUniversite.Value ? "Dil Sınavı Adı bilgisi" : "Üniversite Adı bilgisi");
                                if (!item.Row.SinavTarihiSuccess) mMessage.Messages.Add("Dil Sınavı Yılı bilgisi");
                                if (!item.Row.DilPuaniSuccessMsg.IsNullOrWhiteSpace()) mMessage.Messages.Add(item.Row.DilPuaniSuccessMsg);
                            }
                        }

                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.AdSoyadSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item.Row.JuriTipAdi + "AdSoyad" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.UnvanAdiSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item.Row.JuriTipAdi + "UnvanAdi" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.EMailSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item.Row.JuriTipAdi + "EMail" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.UniversiteIDSuccess ? Msgtype.Success : Msgtype.Error), PropertyName = item.Row.JuriTipAdi + "UniversiteID" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.AnabilimdaliProgramAdiSuccess ? Msgtype.Success : Msgtype.Error), PropertyName = item.Row.JuriTipAdi + "AnabilimdaliProgramAdi" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.IsDilSinaviOrUniversiteSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item.Row.JuriTipAdi + "IsDilSinaviOrUniversite" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.DilSinavAdiSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item.Row.JuriTipAdi + "DilSinavAdi" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.SinavTarihiSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item.Row.JuriTipAdi + "SinavTarihi" });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item.Row.DilPuaniSuccessMsg.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = item.Row.JuriTipAdi + "DilPuani" });

                    }
                    if (ErrSelectedTabID == 0)
                    {
                        ErrSelectedTabID = kModel.SelectedTabID;
                        if (SaveData == false)
                        {
                            kModel.SelectedTabID = kModel.SelectedTabID + 1;
                        }
                    }
                    else kModel.SelectedTabID = ErrSelectedTabID;

                    if (mMessage.Messages.Count == 0 && SaveData)
                    {
                        string DosyaYolu = "";
                        try
                        {
                            TIBasvuruAraRapor = IsYeniJO ? new TIBasvuruAraRapor() : TIBasvuruAraRapor;
                            var Unilers = _db.Universitelers.ToList();
                            foreach (var item in qData)
                            {
                                var Rw = TIBasvuruAraRapor.TIBasvuruAraRaporKomites.Where(p => p.JuriTipAdi == item.Row.JuriTipAdi).FirstOrDefault();
                                if (Rw != null)
                                {
                                    var Uni = Unilers.Where(p => p.UniversiteID == item.Row.UniversiteID).First();
                                    if (item.Row.AdSoyad.IsNullOrWhiteSpace() == false)
                                    {
                                        if (Rw.AdSoyad != item.Row.AdSoyad || Rw.UnvanAdi != item.Row.UnvanAdi || Rw.EMail != item.Row.EMail || Rw.UniversiteID != item.Row.UniversiteID || Rw.IsDilSinaviOrUniversite != item.Row.IsDilSinaviOrUniversite || Rw.DilSinavAdi != item.Row.DilSinavAdi || Rw.DilPuani != item.Row.DilPuani) IsDegisiklikVar = true;
                                        Rw.UnvanAdi = item.Row.UnvanAdi.ToUpper();
                                        Rw.AdSoyad = item.Row.AdSoyad.ToUpper();
                                        Rw.EMail = item.Row.EMail;
                                        Rw.UniversiteAdi = Uni.Ad;
                                        Rw.UniversiteID = item.Row.UniversiteID;
                                        Rw.AnabilimdaliProgramAdi = item.Row.AnabilimdaliProgramAdi;

                                        Rw.IsDilSinaviOrUniversite = kModel.IsTezDiliDegisecek == true && kModel.YeniTezDiliTr == false ? item.Row.IsDilSinaviOrUniversite : null;
                                        Rw.DilSinavAdi = kModel.IsTezDiliDegisecek == true && kModel.YeniTezDiliTr == false ? item.Row.DilSinavAdi : null;
                                        Rw.SinavTarihi = kModel.IsTezDiliDegisecek == true && kModel.YeniTezDiliTr == false && item.Row.IsDilSinaviOrUniversite == true ? item.Row.SinavTarihi : (int?)null;
                                        Rw.DilPuani = kModel.IsTezDiliDegisecek == true && kModel.YeniTezDiliTr == false && item.Row.IsDilSinaviOrUniversite == true ? item.Row.DilPuani : null;
                                        Rw.IslemTarihi = DateTime.Now;
                                        Rw.IslemYapanID = UserIdentity.Current.Id;
                                        Rw.IslemYapanIP = UserIdentity.Ip;

                                    }
                                    else _db.TIBasvuruAraRaporKomites.Remove(Rw);
                                }
                                else if (item.Row.AdSoyad.IsNullOrWhiteSpace() == false)
                                {
                                    var Uni = Unilers.Where(p => p.UniversiteID == item.Row.UniversiteID).First();
                                    TIBasvuruAraRapor.TIBasvuruAraRaporKomites.Add(
                                        new TIBasvuruAraRaporKomite
                                        {
                                            UniqueID = Guid.NewGuid(),
                                            JuriTipAdi = item.Row.JuriTipAdi,
                                            UnvanAdi = item.Row.UnvanAdi.ToUpper(),
                                            AdSoyad = item.Row.AdSoyad.ToUpper(),
                                            EMail = item.Row.EMail,
                                            UniversiteID = item.Row.UniversiteID,
                                            UniversiteAdi = Uni.Ad,
                                            AnabilimdaliProgramAdi = item.Row.AnabilimdaliProgramAdi,
                                            IsDilSinaviOrUniversite = TIBasvuruAraRapor.IsTezDiliDegisecek == true && TIBasvuruAraRapor.YeniTezDiliTr == false ? item.Row.IsDilSinaviOrUniversite : null,
                                            DilSinavAdi = kModel.IsTezDiliDegisecek == true && kModel.YeniTezDiliTr == false ? item.Row.DilSinavAdi : null,
                                            SinavTarihi = kModel.IsTezDiliDegisecek == true && kModel.YeniTezDiliTr == false && item.Row.IsDilSinaviOrUniversite == true ? item.Row.SinavTarihi : (int?)null,
                                            DilPuani = kModel.IsTezDiliDegisecek == true && kModel.YeniTezDiliTr == false && item.Row.IsDilSinaviOrUniversite == true ? item.Row.DilPuani : null,
                                            IslemTarihi = DateTime.Now,
                                            IslemYapanID = UserIdentity.Current.Id,
                                            IslemYapanIP = UserIdentity.Ip


                                        });
                                }
                            }
                            if (IsYeniJO || IsDegisiklikVar)
                            {
                                var UniqueID = Guid.NewGuid();
                                while (_db.TIBasvuruAraRapors.Any(a => a.UniqueID == UniqueID))
                                {
                                    UniqueID = Guid.NewGuid();
                                }
                                TIBasvuruAraRapor.UniqueID = UniqueID;
                                var FormKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                                while (_db.TIBasvuruAraRapors.Any(a => a.FormKodu == FormKodu))
                                {
                                    FormKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                                }
                                TIBasvuruAraRapor.FormKodu = FormKodu;
                            }
                            else
                            {
                                if (!kModel.IsTezBasligiDegisti)
                                {
                                    kModel.YeniTezBaslikTr = null;
                                    kModel.YeniTezBaslikEn = null;
                                    kModel.TezBasligiDegisimGerekcesi = null;
                                }
                                if (!kModel.IsTezDiliDegisecek || kModel.YeniTezDiliTr != false)
                                {
                                    kModel.SinavAdi = null;
                                    kModel.SinavPuani = null;
                                    kModel.SinavYili = null;
                                }
                                if (
                                    TIBasvuruAraRapor.AraRaporSayisi != kModel.AraRaporSayisi ||
                                    TIBasvuruAraRapor.TezBaslikTr != kModel.TezBaslikTr ||
                                    TIBasvuruAraRapor.TezBaslikEn != kModel.TezBaslikEn ||
                                    TIBasvuruAraRapor.YeniTezBaslikTr != kModel.YeniTezBaslikTr ||
                                    TIBasvuruAraRapor.YeniTezBaslikEn != kModel.YeniTezBaslikEn ||
                                    TIBasvuruAraRapor.IsYokDrBursiyeriVar != kModel.IsYokDrBursiyeriVar ||
                                    TIBasvuruAraRapor.YokDrOncelikliAlan != kModel.YokDrOncelikliAlan ||
                                    TIBasvuruAraRapor.IsTezDiliDegisecek != kModel.IsTezDiliDegisecek ||
                                    TIBasvuruAraRapor.YeniTezDiliTr != kModel.YeniTezDiliTr ||
                                    TIBasvuruAraRapor.SinavAdi != kModel.SinavAdi ||
                                    TIBasvuruAraRapor.SinavPuani != kModel.SinavPuani ||
                                    TIBasvuruAraRapor.SinavYili != kModel.SinavYili
                                   ) IsDegisiklikVar = true;

                            }
                            TIBasvuruAraRapor.DonemID = DonemBilgi.DonemID;
                            TIBasvuruAraRapor.AraRaporSayisi = kModel.AraRaporSayisi;
                            TIBasvuruAraRapor.TIBasvuruID = kModel.TIBasvuruID;
                            TIBasvuruAraRapor.IsTezDiliTr = kModel.IsTezDiliTr;
                            TIBasvuruAraRapor.TezBaslikTr = kModel.TezBaslikTr;
                            TIBasvuruAraRapor.TezBaslikEn = kModel.TezBaslikEn;
                            TIBasvuruAraRapor.IsTezDiliDegisecek = kModel.IsTezDiliDegisecek;
                            TIBasvuruAraRapor.YeniTezDiliTr = kModel.YeniTezDiliTr;
                            TIBasvuruAraRapor.SinavAdi = kModel.SinavAdi;
                            TIBasvuruAraRapor.SinavPuani = kModel.SinavPuani;
                            TIBasvuruAraRapor.SinavYili = kModel.SinavYili;
                            TIBasvuruAraRapor.IsTezBasligiDegisti = kModel.IsTezBasligiDegisti;
                            TIBasvuruAraRapor.YeniTezBaslikTr = kModel.YeniTezBaslikTr;
                            TIBasvuruAraRapor.YeniTezBaslikEn = kModel.YeniTezBaslikEn;
                            TIBasvuruAraRapor.TezBasligiDegisimGerekcesi = kModel.TezBasligiDegisimGerekcesi;
                            TIBasvuruAraRapor.IsYokDrBursiyeriVar = kModel.IsYokDrBursiyeriVar.Value;
                            TIBasvuruAraRapor.YokDrOncelikliAlan = kModel.YokDrOncelikliAlan;
                            TIBasvuruAraRapor.IslemTarihi = DateTime.Now;
                            TIBasvuruAraRapor.IslemYapanID = UserIdentity.Current.Id;
                            TIBasvuruAraRapor.IslemYapanIP = UserIdentity.Ip;

                            if (kModel.Dosya != null)
                            {
                                var dosyaAdi = kModel.Dosya.FileName.ToFileNameAddGuid(null, TIBasvuru.TIBasvuruID.ToString());

                                DosyaYolu = "/BasvuruDosyalari/TezIzlemeBelgeleri/" + dosyaAdi;
                                var sfilename = Server.MapPath("~" + DosyaYolu);
                                kModel.Dosya.SaveAs(sfilename);
                                if (!TIBasvuruAraRapor.TICalismaRaporDosyaAdi.IsNullOrWhiteSpace())
                                {
                                    try
                                    {

                                        var path = Server.MapPath("~" + TIBasvuruAraRapor.TICalismaRaporDosyaYolu);
                                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                                    }
                                    catch { }

                                }
                                TIBasvuruAraRapor.TICalismaRaporDosyaAdi = dosyaAdi;
                                TIBasvuruAraRapor.TICalismaRaporDosyaYolu = DosyaYolu;
                            }

                            if (IsYeniJO)
                            {
                                var Td = _db.Kullanicilars.Where(p => p.KullaniciID == kul.DanismanID).First();
                                TIBasvuruAraRapor.BasvuruSonDonemSecilecekDersKodlari = TIAyar.SonDonemKayitOlunmasiGerekenDersKodlari.getAyarTI(TIBasvuru.EnstituKod, "");
                                TIBasvuru.TezDanismanID = Td.KullaniciID;
                                TIBasvuruAraRapor.TezDanismanID = Td.KullaniciID;
                                TIBasvuruAraRapor.RaporTarihi = DateTime.Now;
                                TIBasvuruAraRapor.DonemBaslangicYil = DonemBilgi.BaslangicYil;
                                TIBasvuruAraRapor.TIBasvuruAraRaporDurumID = TIAraRaporDurumu.ToplantiBilgileriGirilmedi;
                                TIBasvuruAraRapor = _db.TIBasvuruAraRapors.Add(TIBasvuruAraRapor);
                            }

                            _db.SaveChanges();
                            LogIslemleri.LogEkle("TIBasvuruAraRapor", IsYeniJO ? IslemTipi.Insert : IslemTipi.Update, TIBasvuruAraRapor.ToJson());
                            foreach (var item in TIBasvuruAraRapor.TIBasvuruAraRaporKomites)
                            {
                                LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", IsYeniJO ? IslemTipi.Insert : IslemTipi.Update, item.ToJson());
                            }
                            mMessage.IsSuccess = true;
                            int? SRTalepID = null;
                            if (IsDegisiklikVar || IsYeniJO)
                            {
                                if (IsYeniJO)
                                {
                                    TIBasvuru.AktifTIBasvuruAraRaporID = TIBasvuruAraRapor.TIBasvuruAraRaporID;
                                    _db.SaveChanges();
                                }
                                if (IsDegisiklikVar && !IsYeniJO && TIBasvuruAraRapor.SRTalepleris.Any()) SRTalepID = TIBasvuruAraRapor.SRTalepleris.First().SRTalepID;
                                var Messages = Management.sendMailTIBilgisi(TIBasvuruAraRapor.TIBasvuruAraRaporID, SRTalepID);
                                if (SRTalepID.HasValue && mMessage.IsSuccess) mMessage.Messages.Add("<br/><i class='fa fa-lg fa-envelope-o' style='font-size:11pt;'></i> <span style=font-size:10pt;'>Rapor bilgilerinde değişiklik yapıldığı için Rapor, Toplantı bilgileri Danışman ve Öğrenciye mail olarak tekrar gönderildi!</span>");

                            }
                        }
                        catch (Exception ex)
                        {
                            if (DosyaYolu != null)
                            {
                                try
                                {
                                    var path = Server.MapPath("~" + DosyaYolu);
                                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

                                }
                                catch { }
                            }
                            var hataMsj = "Kayıt işlemi sırasında bir hata oluştu! \r\nHata:" + ex.ToExceptionMessage();
                            mMessage.Messages.Add(hataMsj);
                            Management.SistemBilgisiKaydet(hataMsj, "TIBasvuru/TIAraRaporFormuPost", BilgiTipi.Hata);
                        }


                    }

                }


            }
            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return new
            {
                mMessage,
                IsYeniJO,
                SaveData,
                kModel.SelectedTabID,
            }.toJsonResult();
        }
        [Authorize]
        public ActionResult TIAraRaporFormu()
        {

            return View();
        }
        [Authorize]

        public ActionResult RezervasyonAl(int TIBasvuruAraRaporID, int SRTalepID)
        {
            var ToplantiYetki = RoleNames.TIToplantiTalebiYap.InRoleCurrent();
            var TIAraRapor = _db.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID == TIBasvuruAraRaporID).First();
            var model = new kmSRTalep();
            if (!ToplantiYetki && TIAraRapor.TIBasvuru.TezDanismanID != UserIdentity.Current.Id) model.YetkisizErisim = true;
            else
            {
                if (TIAraRapor.SRTalepleris.Any())
                {

                    var SrTalep = TIAraRapor.SRTalepleris.First();
                    var Tarih = model.IsSalonSecilsin ? SrTalep.Tarih : (SrTalep.Tarih.AddHours(SrTalep.BasSaat.Hours).AddMinutes(SrTalep.BasSaat.Minutes));

                    model.IsSalonSecilsin = SrTalep.SRSalonID.HasValue;
                    model.IsOnline = SrTalep.IsOnline;
                    model.SRTalepID = SrTalep.SRTalepID;
                    model.SRTalepTipID = SrTalep.SRTalepTipID;
                    model.EnstituKod = SrTalep.EnstituKod;
                    model.TalepYapanID = SrTalep.TalepYapanID;
                    model.SRSalonID = SrTalep.SRSalonID;
                    model.SalonAdi = SrTalep.SalonAdi;
                    model.Tarih = Tarih;
                    model.HaftaGunID = SrTalep.HaftaGunID;
                    model.BasSaat = SrTalep.BasSaat;
                    model.BitSaat = SrTalep.BitSaat;
                    model.DanismanAdi = SrTalep.DanismanAdi;
                    model.EsDanismanAdi = SrTalep.EsDanismanAdi;
                    model.TezOzeti = SrTalep.TezOzeti;
                    model.TezOzetiHtml = SrTalep.TezOzetiHtml;
                    model.SRDurumID = SrTalep.SRDurumID;
                    model.SRDurumAciklamasi = SrTalep.SRDurumAciklamasi;
                    model.IslemTarihi = SrTalep.IslemTarihi;
                    model.IslemYapanID = SrTalep.IslemYapanID;
                    model.IslemYapanIP = SrTalep.IslemYapanIP;
                    model.Aciklama = SrTalep.Aciklama;
                    //model.SRTaleplerJuris = data.SRTaleplerJuris.ToList();
                }
                else
                {

                    model.SRTalepTipID = 3;
                    model.TalepYapanID = TIAraRapor.TIBasvuru.KullaniciID;
                    model.Tarih = DateTime.Now.Date;
                    //model.SRTaleplerJuris = TITalep.TIBasvuruAraRaporKomites.Select(s => new SRTaleplerJuri { JuriAdi = s.UnvanAdi + " " + s.AdSoyad, Telefon = "", Email = s.EMail }).ToList();  

                }
            }

            return View(model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult RezervasyonAlPost(kmSRTalep kModel, bool IsSendMail = true)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;
            mmMessage.Title = "Tez İzleme Toplantı Bilgileri";
            mmMessage.MessageType = Msgtype.Warning;
            var TIAraRapor = _db.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID == kModel.TIBasvuruAraRaporID).First();
            var SRTalep = TIAraRapor.SRTalepleris.FirstOrDefault();
            var TIToplantiTalebiYap = RoleNames.TIToplantiTalebiYap.InRoleCurrent();
            var TITezDegerlendirmeDuzeltme = RoleNames.TITezDegerlendirmeDuzeltme.InRoleCurrent();

            if (!TIToplantiTalebiYap && TIAraRapor.TIBasvuru.TezDanismanID != UserIdentity.Current.Id) kModel.YetkisizErisim = true;


            mmMessage.DialogID = TIAraRapor.TIBasvuruID.ToString();
            kModel.SRTalepTipID = 3;

            kModel.EnstituKod = TIAraRapor.TIBasvuru.EnstituKod;
            if (kModel.YetkisizErisim)
            {
                mmMessage.Messages.Add("Tez Izleme Toplantı Kayıt işlemi yapmaya yetkili değilsiniz.");
            }
            else
            {
                if (TIAraRapor.TIBasvuruAraRaporKomites.Any(a => a.IsBasarili.HasValue))
                {
                    mmMessage.Messages.Add("Komite üyelerinden herhangi biri değerlendirme yaptıktan sonra Toplantı bilgileri değiştirilemez.");
                }
            }
            kModel.SRTalepID = SRTalep == null ? 0 : SRTalep.SRTalepID;


            if (mmMessage.Messages.Count == 0)
            {

                if (kModel.Tarih == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Tarih Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                }
                else if (!TITezDegerlendirmeDuzeltme && kModel.Tarih < DateTime.Now)
                {
                    mmMessage.Messages.Add("Toplantı tarihi bilgisi günümüz tarihten küçük olamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                }
                else if (TIAraRapor.RaporTarihi.ToAraRaporDonemBilgi().BitisTarihi.Date < kModel.Tarih.Date)
                {
                    var DonemSonuTarihi = TIAraRapor.RaporTarihi.ToAraRaporDonemBilgi().BitisTarihi;
                    mmMessage.Messages.Add("Toplantı tarihi ara rapor dönem sonu tarihi olan " + DonemSonuTarihi.ToLongDateString() + " tarihten büyük olamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                }
                if (kModel.SalonAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add(kModel.IsOnline ? "Toplantı katılım linkini giriniz." : "Salon adı bilgisini giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SalonAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "SalonAdi" });


                if (mmMessage.Messages.Count == 0)
                {
                    try
                    {
                        kModel.IslemTarihi = DateTime.Now;
                        kModel.IslemYapanID = UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        var Tarih = kModel.Tarih;

                        kModel.Tarih = Tarih.Date;
                        kModel.HaftaGunID = (int)Tarih.DayOfWeek;
                        kModel.BasSaat = kModel.IsSalonSecilsin ? kModel.BasSaat.Value : Tarih.TimeOfDay;
                        kModel.BitSaat = kModel.IsSalonSecilsin ? kModel.BitSaat.Value : kModel.BasSaat.Value.Add(new TimeSpan(2, 0, 0));
                        kModel.SRDurumID = SRTalepDurum.Onaylandı;
                        kModel.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                        kModel.IslemTarihi = kModel.IslemTarihi;
                        kModel.IslemYapanID = kModel.IslemYapanID;
                        kModel.IslemYapanIP = kModel.IslemYapanIP;
                        kModel.SRTaleplerJuris = TIAraRapor.TIBasvuruAraRaporKomites.Select(s => new SRTaleplerJuri
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

                        if (!TITezDegerlendirmeDuzeltme)
                        {
                            IsSendMail = SRTalep == null || (SRTalep.IsOnline != kModel.IsOnline || SRTalep.SalonAdi != kModel.SalonAdi || SRTalep.Tarih != kModel.Tarih || SRTalep.BasSaat != kModel.BasSaat);
                        }

                        bool IsNewRecord = false;
                        if (SRTalep == null)
                        {

                            IsNewRecord = true;
                            SRTalep = _db.SRTalepleris.Add(new SRTalepleri
                            {
                                UniqueID = Guid.NewGuid(),
                                TIBasvuruAraRaporID = TIAraRapor.TIBasvuruAraRaporID,
                                IsOnline = kModel.IsOnline,
                                EnstituKod = kModel.EnstituKod,
                                MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID,
                                SRTalepTipID = kModel.SRTalepTipID,
                                TalepYapanID = kModel.TalepYapanID,
                                SRSalonID = null,
                                SalonAdi = kModel.SalonAdi,
                                Tarih = kModel.Tarih,
                                HaftaGunID = kModel.HaftaGunID,
                                BasSaat = kModel.BasSaat.Value,
                                BitSaat = kModel.BitSaat.Value,
                                Aciklama = kModel.Aciklama,
                                SRDurumID = kModel.SRDurumID,
                                IslemTarihi = kModel.IslemTarihi,
                                IslemYapanID = kModel.IslemYapanID,
                                IslemYapanIP = kModel.IslemYapanIP

                            });
                            TIAraRapor.TIBasvuruAraRaporDurumID = TIAraRaporDurumu.ToplantiBilgileriGirildi;

                        }
                        else
                        {

                            SRTalep.TIBasvuruAraRaporID = TIAraRapor.TIBasvuruAraRaporID;
                            SRTalep.SRTalepTipID = kModel.SRTalepTipID;
                            SRTalep.IsOnline = kModel.IsOnline;
                            SRTalep.SalonAdi = kModel.SalonAdi;
                            SRTalep.TalepYapanID = kModel.TalepYapanID;
                            SRTalep.SRSalonID = null;
                            SRTalep.Tarih = kModel.Tarih;
                            SRTalep.HaftaGunID = kModel.HaftaGunID;
                            SRTalep.BasSaat = kModel.BasSaat.Value;
                            SRTalep.BitSaat = kModel.BitSaat.Value;
                            SRTalep.DanismanAdi = kModel.DanismanAdi;
                            SRTalep.EsDanismanAdi = kModel.EsDanismanAdi;
                            SRTalep.SRDurumID = kModel.SRDurumID;
                            SRTalep.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                            SRTalep.IslemTarihi = kModel.IslemTarihi;
                            SRTalep.IslemYapanID = kModel.IslemYapanID;
                            SRTalep.IslemYapanIP = kModel.IslemYapanIP;
                        }
                        _db.SaveChanges();
                        LogIslemleri.LogEkle("SRTalepleri", IsNewRecord ? IslemTipi.Insert : IslemTipi.Update, SRTalep.ToJson());

                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = Msgtype.Success;
                        mmMessage.Messages.Add("Komite toplantı bilgisi düzenlendi.");

                        #region SendMail

                        if (IsSendMail)
                        {
                            var Messages = Management.sendMailTIBilgisi(null, SRTalep.SRTalepID);
                            if (Messages.IsSuccess)
                            {
                                mmMessage.Messages.Add("<br/><i class='fa fa-envelope-o'></i> <span style=font-size:10pt;'>Toplantı bilgisi Komite üyelerine ve öğrenciye mail olarak gönderildi.</span>");
                            }
                            else
                            {
                                mmMessage.Messages.Add("<br/><i class='fa fa-lg fa-envelope-o' style='font-size:11pt;'></i> <span style=font-size:10pt;'>Toplantı bilgisi Komite üyelerine ve öğrenciye mail olarak gönderilemedi!</span>");
                            }
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        mmMessage.IsSuccess = false;
                        mmMessage.MessageType = Msgtype.Error;
                        mmMessage.Messages.Add("İşlem yapılırken bir hata oluştu.");
                        Management.SistemBilgisiKaydet("Tez izleme toplantı bilgisi oluşturulurken bir hata oluştu! Hata:" + ex.ToExceptionMessage(), "TIBasvuru/RezervasyonAlPost<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.Kritik);
                    }

                }

            }

            return mmMessage.toJsonResult();
        }

        public ActionResult AraRaporDegerlendi(Guid? UniqueID, bool? IsTezIzlemeRaporuTezOnerisiUygun, bool IsDrBurs, bool? IsTezIzlemeRaporuAltAlanUygun, bool? IsBasarili, string Aciklama)
        {
            var mMessage = new MmMessage();
            mMessage.IsSuccess = false;
            mMessage.Title = "Tez İzleme Rapor Değerlendirme İşlemi";
            var DegerlendirmeDuzeltmeYetki = RoleNames.TITezDegerlendirmeDuzeltme.InRoleCurrent();
            bool IsRefresh = false;
            if (!UniqueID.HasValue)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme için gerekli benzersiz anahtar bilgisi boş gelmektedir.</span>");
            }
            else
            {
                var Komite = _db.TIBasvuruAraRaporKomites.Where(p => p.UniqueID == UniqueID).FirstOrDefault();
                if (Komite == null)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme işlemi yapmanız için size tanınan benzersiz anahtar bilgisi değişti veya bulunamadı!</span>");
                }
                else
                {
                    bool IsTezDanismani = Komite.JuriTipAdi == "TezDanismani";
                    var Donem = DateTime.Now.ToAraRaporDonemBilgi();
                    if (!DegerlendirmeDuzeltmeYetki)
                    {
                        var Toplanti = Komite.TIBasvuruAraRapor.SRTalepleris.First();

                        var ToplantiTarihi = Toplanti.Tarih.Add(Toplanti.BasSaat);
                        if (DateTime.Now < ToplantiTarihi)
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + ToplantiTarihi.ToLongDateString() + " " + string.Format("{0:hh\\:mm}", Toplanti.BasSaat) + "'</b> dan önce yapılamaz!</span>");
                        }
                        else if (Komite.IsBasarili.HasValue)
                        {
                            mMessage.IsSuccess = true;
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemini daha önceden zaten yaptınız!</span>");
                        }
                        //else if (!(Komite.TIBasvuruAraRapor.DonemBaslangicYil == Donem.BaslangicYil && Komite.TIBasvuruAraRapor.DonemID == Donem.DonemID))
                        //{
                        //    mMessage.Messages.Add("<span style='color:maroon;'>Rapor değerlendirme dönemi geçtikten sonra değerlendirme işlemi yapılamaz!</span>");
                        //}
                        else
                        {
                            if (!IsTezIzlemeRaporuTezOnerisiUygun.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu İle Tez Önerisi Uyumlu mu?</span>");
                            }
                            if (IsTezDanismani && IsDrBurs && !IsTezIzlemeRaporuAltAlanUygun.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu İle 100/2000 YÖK Bursu Alt Alan Uyumlu mu?</span>");
                            }
                            if (!IsBasarili.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporunun Değerlendirme Sonucu</span>");
                            }
                            else if (!IsBasarili.Value && Aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu Değerlendirme Açıklaması</span>");
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez izleme rapor değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");
                        }
                    }
                    else
                    {
                        var Toplanti = Komite.TIBasvuruAraRapor.SRTalepleris.First();
                        var ToplantiTarihi = Toplanti.Tarih.Add(Toplanti.BasSaat);
                        if (DateTime.Now < ToplantiTarihi)
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + ToplantiTarihi.ToLongDateString() + " " + string.Format("{0:hh\\:mm}", Toplanti.BasSaat) + "'</b> dan önce yapılamaz!</span>");
                        }
                        else
                        {
                            int DCount = 2 + (IsTezDanismani && IsDrBurs ? 1 : 0) + (IsBasarili == false ? 1 : 0);
                            int GCount = 0;
                            if (!IsTezIzlemeRaporuTezOnerisiUygun.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu İle Tez Önerisi Uyumlu mu?</span>");
                            }
                            else GCount++;
                            if (IsTezDanismani && IsDrBurs)
                            {
                                if (!IsTezIzlemeRaporuAltAlanUygun.HasValue)
                                    mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu İle 100/2000 YÖK Bursu Alt Alan Uyumlu mu?</span>");
                                else GCount++;
                            }
                            if (!IsBasarili.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporunun Değerlendirme Sonucu</span>");
                            }
                            else GCount++;
                            if (IsBasarili == false)
                            {
                                if (Aciklama.IsNullOrWhiteSpace())
                                    mMessage.Messages.Add("<span style='color:maroon;'>Tez İzleme Raporu Değerlendirme Açıklaması</span>");
                                else GCount++;
                            }
                            if (DCount == GCount || GCount == 0)
                            {
                                mMessage.Messages.Clear();
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez izleme rapor değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");
                        }
                    }
                    if (!mMessage.Messages.Any())
                    {
                        var SendMailLink = false;
                        if (IsTezDanismani && IsBasarili.HasValue && !Komite.TIBasvuruAraRapor.TIBasvuruAraRaporKomites.Any(a => a.IsLinkGonderildi.HasValue)) SendMailLink = true;
                        bool IsDegisiklikVar = Komite.IsTezIzlemeRaporuTezOnerisiUygun != IsTezIzlemeRaporuTezOnerisiUygun || Komite.IsTezIzlemeRaporuAltAlanUygun != IsTezIzlemeRaporuAltAlanUygun || Komite.IsBasarili != IsBasarili || Komite.Aciklama != Aciklama;
                        Komite.IsTezIzlemeRaporuTezOnerisiUygun = IsTezIzlemeRaporuTezOnerisiUygun;
                        Komite.IsTezIzlemeRaporuAltAlanUygun = IsTezIzlemeRaporuAltAlanUygun;
                        Komite.IsBasarili = IsBasarili;
                        Komite.Aciklama = Aciklama;
                        Komite.DegerlendirmeIslemTarihi = DateTime.Now;
                        Komite.DegerlendirmeIslemYapanIP = UserIdentity.Ip;
                        Komite.DegerlendirmeYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;

                        Komite.IslemTarihi = DateTime.Now;
                        Komite.IslemYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;
                        Komite.IslemYapanIP = UserIdentity.Ip;
                        if (IsDegisiklikVar)
                        {
                            Komite.TIBasvuruAraRapor.UniqueID = Guid.NewGuid();
                            var FormKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            while (_db.TIBasvuruAraRapors.Any(a => a.FormKodu == FormKodu))
                            {
                                FormKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            }
                            Komite.TIBasvuruAraRapor.FormKodu = FormKodu;
                        }
                        _db.SaveChanges();
                        LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", IslemTipi.Update, Komite.ToJson());
                        mMessage.IsSuccess = true;
                        if (SendMailLink)
                        {
                            var Messages = Management.sendMailTIDegerlendirmeLink(Komite.TIBasvuruAraRaporID, null, true);
                            if (IsTezDanismani || DegerlendirmeDuzeltmeYetki)
                            {
                                if (Messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme Linki Komite Üyelerine Gönderildi.");
                                }
                                else
                                {
                                    mMessage.Messages.AddRange(Messages.Messages);
                                    mMessage.Messages.Add("Değerlendirmeniz geri alınmıştır, Lütfen tekrar değerlendirme yapınız.");
                                    mMessage.IsSuccess = false;
                                    IsRefresh = true;
                                    Komite.IsTezIzlemeRaporuTezOnerisiUygun = null;
                                    Komite.IsTezIzlemeRaporuAltAlanUygun = null;
                                    Komite.IsBasarili = null;
                                    Komite.Aciklama = null;
                                    Komite.DegerlendirmeIslemTarihi = null;
                                    Komite.DegerlendirmeIslemYapanIP = null;
                                    Komite.DegerlendirmeYapanID = null;
                                    _db.SaveChanges();
                                }
                            }
                        }
                        else mMessage.Messages.Add("Değerlendirme işlemi tamamlandı.");


                        var IsDegerlendirmeTamam = !Komite.TIBasvuruAraRapor.TIBasvuruAraRaporKomites.Any(a => !a.IsBasarili.HasValue);
                        var TIBasvuruAraRapor = Komite.TIBasvuruAraRapor;
                        var TIBasvuruAraRaporKomites = TIBasvuruAraRapor.TIBasvuruAraRaporKomites;
                        if (IsDegerlendirmeTamam)
                        {

                            TIBasvuruAraRapor.TIBasvuruAraRaporDurumID = TIAraRaporDurumu.DegerlendirmeSureciTamamlandi;
                            TIBasvuruAraRapor.IsBasariliOrBasarisiz = TIBasvuruAraRaporKomites.Count(c => c.IsBasarili == true) > TIBasvuruAraRaporKomites.Count(c => c.IsBasarili == false);
                            TIBasvuruAraRapor.IsOyBirligiOrCouklugu = TIBasvuruAraRaporKomites.Count == TIBasvuruAraRaporKomites.Count(c => c.IsBasarili == TIBasvuruAraRapor.IsBasariliOrBasarisiz);

                            var Messages = Management.sendMailTIDegerlendirmeLink(Komite.TIBasvuruAraRaporID, null, false);
                            if (IsTezDanismani || DegerlendirmeDuzeltmeYetki)
                            {
                                if (Messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme Sonucu Danışman ve Öğrenciye Gönderildi.");

                                }
                                else
                                {
                                    mMessage.Messages.AddRange(Messages.Messages);
                                    mMessage.IsSuccess = false;
                                }
                            }
                            if (Messages.IsSuccess)
                            {
                                TIBasvuruAraRapor.DegerlendirmeSonucMailTarihi = DateTime.Now;
                            }
                        }
                        else
                        {
                            TIBasvuruAraRapor.IsBasariliOrBasarisiz = null;
                            TIBasvuruAraRapor.IsOyBirligiOrCouklugu = null;
                            if (TIBasvuruAraRaporKomites.Any(a => a.IsBasarili.HasValue))
                            {
                                TIBasvuruAraRapor.TIBasvuruAraRaporDurumID = TIAraRaporDurumu.DegerlendirmeSureciBaslatildi;
                            }
                            else
                            {
                                TIBasvuruAraRapor.TIBasvuruAraRaporDurumID = TIAraRaporDurumu.ToplantiBilgileriGirildi;
                            }
                        }
                        LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", IslemTipi.Update, Komite.ToJson());
                        _db.SaveChanges();
                    }
                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var strView = Management.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { mMessage.IsSuccess, Messages = strView, IsRefresh }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult DegerlendirmeLinkiGonder(int TIBasvuruID, int TIBasvuruAraRaporID, Guid? UniqueID)
        {
            var mMessage = new MmMessage();
            mMessage.IsSuccess = false;
            mMessage.Title = "Tez İzleme Raporu Değerlendirme Linki Gönderme İşlemi";
            var AraRapor = _db.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID == TIBasvuruAraRaporID).First();
            var Basvuru = AraRapor.TIBasvuru;
            var TITezDegerlendirmeDuzeltme = RoleNames.TITezDegerlendirmeDuzeltme.InRoleCurrent();
            if (!TITezDegerlendirmeDuzeltme && Basvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Değerlendirme Linki Göndermek İçin Yetkili Değilsiniz.");
            }
            else if (!TITezDegerlendirmeDuzeltme && AraRapor.TIBasvuruAraRaporKomites.Count == AraRapor.TIBasvuruAraRaporKomites.Count(c => c.IsBasarili.HasValue))
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Değerlendirme işlemi tüm Komite üyeler tarafından tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
            }
            else
            {
                if (UniqueID.HasValue)
                {
                    var Uye = AraRapor.TIBasvuruAraRaporKomites.Where(p => p.UniqueID == UniqueID).FirstOrDefault();
                    if (Uye == null) mMessage.Messages.Add("Değerlendirme Linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı Yenileyip Tekrar Deneyiniz.");

                }
                var Messages = Management.sendMailTIDegerlendirmeLink(TIBasvuruAraRaporID, UniqueID, true);
                if (Messages.IsSuccess)
                {

                    AraRapor.IsBasariliOrBasarisiz = null;
                    AraRapor.IsOyBirligiOrCouklugu = null;
                    if (AraRapor.TIBasvuruAraRaporKomites.Any(a => a.IsBasarili.HasValue))
                    {
                        AraRapor.TIBasvuruAraRaporDurumID = TIAraRaporDurumu.DegerlendirmeSureciBaslatildi;
                    }
                    else
                    {
                        AraRapor.TIBasvuruAraRaporDurumID = TIAraRaporDurumu.ToplantiBilgileriGirildi;
                    }
                    _db.SaveChanges();
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Değerlendirme Linki Komite Üyesine Gönderildi.");

                }
                else
                {
                    mMessage.Messages.AddRange(Messages.Messages);

                }
            }

            return new { mMessage, MessageType = (mMessage.IsSuccess ? "success" : "error") }.toJsonResult();
        }
     

        [Authorize]
        public ActionResult Sil(int id)
        {

            var mmMessage = Management.getTIBasvuruSilKontrol(id);

            if (mmMessage.IsSuccess)
            {
                var kayit = _db.TIBasvurus.Where(p => p.TIBasvuruID == id).FirstOrDefault();
                var tarih = kayit.BasvuruTarihi.ToString();

                bool IsAdminRemove = false;
                if (UserIdentity.Current.IsAdmin)
                {
                    if (!kayit.TIBasvuruAraRapors.Any(a => a.TIBasvuruAraRaporDurumID == TIAraRaporDurumu.DegerlendirmeSureciTamamlandi))
                    {
                        IsAdminRemove = true;
                    }
                }

                try
                {

                    mmMessage.Title = "Uyarı";
                    if (IsAdminRemove)
                    {
                        var AraRapors = kayit.TIBasvuruAraRapors.ToList();
                        foreach (var item in AraRapors)
                        {
                            _db.SRTalepleris.RemoveRange(item.SRTalepleris);
                            _db.TIBasvuruAraRaporKomites.RemoveRange(item.TIBasvuruAraRaporKomites);
                            _db.TIBasvuruAraRapors.Remove(item);
                        }
                        _db.TIBasvurus.Remove(kayit);
                    }
                    else
                    {
                        _db.TIBasvurus.Remove(kayit);
                    }
                    _db.SaveChanges();
                    LogIslemleri.LogEkle("TIBasvuru", IslemTipi.Delete, kayit.ToJson());

                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = Msgtype.Success;

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "TIBasvuru/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }

            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult DetaySil(int id, int TIBasvuruAraRaporID)
        {
            var mmMessage = new MmMessage();
            mmMessage.Title = "Rapor silme işlemi";
            var TITezDegerlendirmeYap = RoleNames.TITezDegerlendirmeYap.InRoleCurrent();
            var TITezDegerlendirmeDuzeltme = RoleNames.TITezDegerlendirmeDuzeltme.InRoleCurrent();
            var qKayit = _db.TIBasvuruAraRapors.Where(p => p.TIBasvuruID == id && p.TIBasvuruAraRaporID == TIBasvuruAraRaporID).AsQueryable();
            if (!TITezDegerlendirmeYap && !TITezDegerlendirmeDuzeltme) qKayit = qKayit.Where(p => p.TIBasvuru.KullaniciID == UserIdentity.Current.Id);
            else if (TITezDegerlendirmeYap && !TITezDegerlendirmeDuzeltme) qKayit = qKayit.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            var TIbasvuruAraRapor = qKayit.FirstOrDefault();


            if (TIbasvuruAraRapor == null)
            {
                mmMessage.Messages.Add("Silinmek istenen kayıt sistemde bulunamadı.");
            }
            else if (TIbasvuruAraRapor.SRTalepleris.Any())
            {
                mmMessage.Messages.Add(TIbasvuruAraRapor.AraRaporSayisi + ". Rapor için salon toplantı tarihi belirlendiği için silme işlemi yapılamaz.");
            }
            {
                try
                {
                    TIbasvuruAraRapor.TIBasvuru.AktifTIBasvuruAraRaporID = TIbasvuruAraRapor.TIBasvuru.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID != TIBasvuruAraRaporID).OrderByDescending(o => o.AraRaporSayisi).Select(s => s.AraRaporSayisi).FirstOrDefault().toNullIntZero();
                    _db.TIBasvuruAraRapors.Remove(TIbasvuruAraRapor);
                    _db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    LogIslemleri.LogEkle("TIbasvuruAraRapor", IslemTipi.Delete, TIbasvuruAraRapor.ToJson());
                    mmMessage.Messages.Add(TIbasvuruAraRapor.AraRaporSayisi + ". Rapor sistemden silindi.");

                }
                catch (Exception ex)
                {
                    mmMessage.Messages.Add(TIbasvuruAraRapor.AraRaporSayisi + ". Rapor sistemden silinemedi.");
                    Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "TIBasvuru/DetaySil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}