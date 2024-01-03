using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class MezuniyetBus
    {
        // tezli Yl öğrencilerinin mezuniyet başvurusu için belli bir dönem okuyu ondan sonra başvurabilirler 31-03-2016 tarihi itibari ile 4 dönem okumaları gerekir.
        public static DateTime MezuniyetDonemKontrolKriterBasTar = new DateTime(2016, 03, 31);

        public static int? GetMezuniyetAktifSurecId(string enstituKod, int? mezuniyetSurecId = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.MezuniyetSurecis.Where(p =>
                    (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif &&
                    (p.EnstituKod == enstituKod) && p.MezuniyetSurecID ==
                    (mezuniyetSurecId ?? p.MezuniyetSurecID));
                var qBf = bf.FirstOrDefault();
                int? id = null;
                if (qBf != null) id = qBf.MezuniyetSurecID;
                return id;
            }
        }

        public static bool IsSurecAktif(int mezuniyetSurecId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                return db.MezuniyetSurecis.Any(p =>
                    (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif &&
                    p.MezuniyetSurecID == mezuniyetSurecId);

            }
        }
        public static void TezDosyasiKontrolYetkilisiAta(int mezuniyetBasvurulariId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var basvuru = db.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                if (!MezuniyetAyar.TezDosyasiYuklendigindeSorumluyaAta.GetAyarMz(basvuru.MezuniyetSureci.EnstituKod).ToBoolean(false)) return;

                //Dosya yüklendiğinde Kullanıcı atansa bile varolan kullanıcı yetki grubu ve aktiflik durumunu kontrol et eğer aktif bir tez kontrol yetkilisi var ise yeni kullanıcı atamaya izin verme
                if (basvuru.TezKontrolKullaniciID.HasValue && db.Kullanicilars.Any(a => a.KullaniciID == basvuru.TezKontrolKullaniciID && a.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId && a.IsAktif)) return;


                var groupToplamAtamaList =
                    (from kul in db.Kullanicilars.Where(p =>
                            p.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId && p.IsAktif &&
                            p.EnstituKod == basvuru.MezuniyetSureci.EnstituKod)
                     join mez in db.MezuniyetBasvurularis.Where(p =>
                                 p.TezKontrolKullaniciID.HasValue &&
                                 p.MezuniyetSureci.EnstituKod == basvuru.MezuniyetSureci.EnstituKod) on kul
                                 .KullaniciID
                             equals mez.TezKontrolKullaniciID into defMez
                     from mezBas in defMez.DefaultIfEmpty()
                     group new { kul.KullaniciID, mezBas.MezuniyetSurecID, IsAtandi = mezBas != null } by new { kul.KullaniciID }
                        into g1
                     select new
                     {
                         g1.Key.KullaniciID,
                         //SurecAtamaCount = g1.Count(c => c.MezuniyetSurecID == basvuru.MezuniyetSurecID),
                         ToplamAtamaCount = g1.Count(c => c.IsAtandi)
                     }).OrderBy(o => o.ToplamAtamaCount).ThenBy(t => Guid.NewGuid()).ToList();
                var enAzAtanan = groupToplamAtamaList.FirstOrDefault();
                if (enAzAtanan == null) return;
                basvuru.TezKontrolKullaniciID = enAzAtanan.KullaniciID;


                db.SaveChanges();
            }
        }

        public static MmMessage MezuniyetBasvurusuSilKontrol(int mezuniyetBasvurulariId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
                var basvuru =
                    db.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.MezuniyetSureci.EnstituKod) && kayitYetki &&
                        basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                    }
                    else if (!IsSurecAktif(basvuru.MezuniyetSurecID) && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                    }
                    else if (kayitYetki == false &&
                             basvuru.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.Taslak)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Taslak Harici Başvurular Silinemez.");
                    }
                }
            }

            return msg;
        }

        public static MmMessage MezuniyetBasvuruKriterKontrol(string enstituKod, int? mezuniyetBasvurulariId = null)
        {
            var mMessage = new MmMessage();
            var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();

            using (var entities = new LisansustuBasvuruSistemiEntities())
            {


                if (mezuniyetBasvurulariId > 0)
                {
                    var basvuru = entities.MezuniyetBasvurularis.FirstOrDefault(p =>
                        p.MezuniyetBasvurulariID == mezuniyetBasvurulariId.Value);
                    if (basvuru == null)
                    {
                        mMessage.Messages.Add("Düzenlenmek istenen mezuniyet başvurusu sistemde bulunamadı.");
                        return mMessage;
                    }

                    //Önce Kayıt yetkisi varsa ona göre kontrol et
                    if (kayitYetki)
                    {
                        if (UserIdentity.Current.IsAdmin == false && !IsSurecAktif(basvuru.MezuniyetSurecID))
                        {
                            mMessage.Messages.Add(
                                "Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");
                            return mMessage;
                        }

                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.MezuniyetSureci.EnstituKod))
                        {
                            mMessage.Messages.Add("Başvurunun ait olduğu enstitü yetkiniz bulunmamaktadır.");
                            return mMessage;
                        }

                        mMessage.IsSuccess = true;
                        return mMessage;
                    }

                    //kayıt yetkisi yoksa ona göre kontrol et
                    if (basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        mMessage.Messages.Add("Bu İşlem için yetkili değilsiniz.");
                        return mMessage;
                    }

                    if (basvuru.IsDanismanOnay == true)
                    {
                        mMessage.Messages.Add("Danışman tarafından onaylanan başvuruda düzenleme işlemi yapamazsınız!");
                        return mMessage;
                    }

                    if (!IsSurecAktif(basvuru.MezuniyetSurecID))
                    {
                        mMessage.Messages.Add(
                            "Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");
                        return mMessage;
                    }

                    mMessage.IsSuccess = true;
                    return mMessage;
                }

                // bu kısımda özel yetki kontrolü yok, yeni bir başvuru için kontroller yapılır ve yeni başvurular öğrenci adına yapılamaz,ancak öğrenci hesabına geçerek yapılabilir.

                var kul = entities.Kullanicilars.First(f => f.KullaniciID == UserIdentity.Current.Id);
                var aktifBasvuruVar = entities.MezuniyetBasvurularis.Any(p =>
                    kul.ProgramKod == p.ProgramKod &&
                    kul.OgrenciNo == p.OgrenciNo &&
                    kul.OgrenimTipKod == p.OgrenimTipKod &&
                    p.KullaniciID == kul.KullaniciID &&
                    p.MezuniyetBasvurulariID != mezuniyetBasvurulariId &&
                    (p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi ||
                     p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi));

                if (aktifBasvuruVar)
                {
                    mMessage.Messages.Add(
                        "Okuduğunuz program için zaten bir başvurunuz bulunmakta. Tekrar başvuru yapamazsınız.");
                    return mMessage;
                }

                var mezuniyetSurecId = GetMezuniyetAktifSurecId(enstituKod);
                if (!mezuniyetSurecId.HasValue)
                {
                    mMessage.Messages.Add("Başvuru süreci kapalı");
                    return mMessage;
                }

                if (!kul.KullaniciTipleri.BasvuruYapabilir)
                {
                    mMessage.Messages.Add(kul.KullaniciTipleri.KullaniciTipAdi +
                                          " hesap türleri için başvuru işlemleri kapalıdır.");
                    return mMessage;
                }

                if (!kul.YtuOgrencisi)
                {
                    mMessage.Messages.Add(
                        "Mezuniyet başvurusu yapabilmeniz için hesap bilginizi düzelterek YTÜ öğrencisi olduğunuzu belirtiniz.");
                    return mMessage;
                }

                if (kul.OgrenimDurumID != OgrenimDurumEnum.HalenOğrenci)
                {
                    mMessage.Messages.Add(
                        "Mezuniyet Başvuru işlemini yapabilmeniz için profil kısmındaki öğrenim bilgilerinizde bulunan Öğrenim durumunuzun Halen öğrenci olarak seçilmesi gerekmektedir. (Not: özel öğrenciler bu sistem üzerinden başvuru yapamazlar.)");
                    return mMessage;
                }

                if (kul.Programlar.AnabilimDallari.EnstituKod != enstituKod)
                {
                    mMessage.Messages.Add(
                        "Okuduğunuz program enstitüsü ile başvuru yapmaya çalıştığınız enstitü farklıdır. Okuduğunuz enstitü sayfasından başvuru yapmaya çalıştığınızdan emin olunuz!");
                    return mMessage;
                }

                //if (kul.KayitDonemID.HasValue == false || !kul.KayitYilBaslangic.HasValue ||
                //    !kul.OkuduguDonemNo.HasValue)
                //{
                //    mMessage.Messages.Add(
                //        "Öğrenci bilgilerinizde kayıt donemi, okuduğunuz dönem verileri eksik olduğundan eksik başvuru yapamazsınız.");
                //    return mMessage;
                //}

                var isMezuniyetYonetmelikOgrenimTipUygun = entities.MezuniyetSureciYonetmelikleris.Any(p =>
                    p.MezuniyetSurecID == mezuniyetSurecId
                    && p.MezuniyetSureciYonetmelikleriOTs.Any(a =>
                        a.OgrenimTipKod == kul.OgrenimTipKod &&
                        a.IsGecerli));
                if (!isMezuniyetYonetmelikOgrenimTipUygun)
                {
                    var otsAdi = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == kul.OgrenimTipKod)
                        .OgrenimTipAdi;
                    mMessage.Messages.Add(otsAdi +
                                          " Öğrenim seviyesinde okuyan öğrenciler mezuniyet başvurusu yapamazlar.");
                    return mMessage;
                }




                var isOgrenciSureckriterlerindenMuaf = entities.MezuniyetSureciKriterMuafOgrencilers.Any(a =>
                    a.MezuniyetSurecID == mezuniyetSurecId.Value &&
                    a.KullaniciID == kul.KullaniciID);
                if (!isOgrenciSureckriterlerindenMuaf)
                {
                    var basvuruKriterleri = entities.MezuniyetSureciOgrenimTipKriterleris.First(p =>
                        p.MezuniyetSurecID == mezuniyetSurecId.Value &&
                        p.OgrenimTipKod == kul.OgrenimTipKod);

                    var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kul.TcKimlikNo);

                    //düzenlenecek max dönem kriterleri süreç e eklenebilecek ve bu kriteri açanlar başvuru yapamayacak 
                    //if (kul.KayitTarihi > MezuniyetDonemKontrolKriterBasTar && kul.OkuduguDonemNo.Value < 4)
                    //{
                    //    mMessage.Messages.Add("Tezli yuksek lisans öğrenim seviyesi okuyan öğrencilerin mezuniyet başvurusu için en az 4 dönem okumaları gerekmektedir.");
                    //    return mMessage;
                    //}

                    var subMessages = new List<string>();
                    if (ogrenciBilgi.OkuduguDonemNo > basvuruKriterleri.AktifDonemMaxKriteri)
                    {
                        subMessages.Add("Aktif okuma dönemi " + basvuruKriterleri.AktifDonemMaxKriteri + ". dönemden daha büyük olanlar Mezuniyet başvurusu yapamazlar.");
                    }

                    var basvuruSonDonemSecilecekDersKodlari = basvuruKriterleri
                        .AktifDonemDersKodKriteri.Split(',')
                        .Where(p => !p.IsNullOrWhiteSpace()).ToList();

                    if (basvuruSonDonemSecilecekDersKodlari.Any() &&
                       ogrenciBilgi.AktifDonemDers.DersKodNums.Count(p =>
                           basvuruSonDonemSecilecekDersKodlari.Any(a => a == p)) !=
                       basvuruSonDonemSecilecekDersKodlari.Count)
                    {
                        subMessages.Add(string.Join(", ", basvuruSonDonemSecilecekDersKodlari) +
                                        " kodlu derslere son dönemde kayıt yaptırmanız gerekmektedi.");
                    }

                    if (basvuruKriterleri.AktifDonemToplamKrediKriteri >
                        ogrenciBilgi.AktifDonemDers.ToplamKredi)
                    {
                        subMessages.Add("Toplam Kredi sayınız " +
                                        basvuruKriterleri.AktifDonemToplamKrediKriteri +
                                        " krediden büyük ya da eşit olmalıdır. Mevcut Kredi: " +
                                        ogrenciBilgi.AktifDonemDers.ToplamKredi);

                    }

                    if (!basvuruKriterleri.AktifDonemEtikNotKriteri.IsNullOrWhiteSpace() &&
                        !YeterlikBus.IsHarfNotuBuyukEsit(basvuruKriterleri.AktifDonemEtikNotKriteri,
                            ogrenciBilgi.AktifDonemDers.EtikDersNotu))
                    {
                        subMessages.Add("Etik dersi için ders notunuzun " + basvuruKriterleri.AktifDonemEtikNotKriteri +
                                        " veya daha üstü bir not olması gerekmektedir.");
                    }

                    if (!basvuruKriterleri.AktifDonemSeminerNotKriteri.IsNullOrWhiteSpace() &&
                        !YeterlikBus.IsHarfNotuBuyukEsit(basvuruKriterleri.AktifDonemSeminerNotKriteri,
                            ogrenciBilgi.AktifDonemDers.SeminerDersNotu))
                    {
                        subMessages.Add("Seminer dersi için ders notunuzun " + basvuruKriterleri.AktifDonemSeminerNotKriteri +
                                        " veya daha üstü bir not olması gerekmektedir.");
                    }

                    if (basvuruKriterleri.AktifDonemAgnoKriteri > ogrenciBilgi.AktifDonemDers.Agno)
                    {
                        subMessages.Add("Ortalamanız " + basvuruKriterleri.AktifDonemAgnoKriteri +
                                        " ortalamasından büyük ya da eşit olmalıdır. Mevcut Ortalama: " +
                                        ogrenciBilgi.AktifDonemDers.Agno.ToString("n2"));

                    }

                    if (basvuruKriterleri.AktifDonemAktsKriteri >
                        ogrenciBilgi.AktifDonemDers.ToplamAkts)
                    {
                        subMessages.Add("Akts toplamınız " + basvuruKriterleri.AktifDonemAktsKriteri +
                                        " akts'den büyük ya da eşit olmalıdır. Mevcut Akts: " +
                                        ogrenciBilgi.AktifDonemDers.ToplamAkts);

                    }

                    if (subMessages.Any())
                    {
                        mMessage.Messages.Add("Mezuniyet başvurunuz aşağıdaki sebeplerden dolayı başlatılamadı.");
                        mMessage.Messages.AddRange(subMessages);
                        return mMessage;
                    }

                }

                mMessage.IsSuccess = true;
                return mMessage;
            }

        }
        public static KmMezuniyetBasvuru GetMezuniyetBasvuruBilgi(int mezuniyetBasvurulariId)
        {
            var model = new KmMezuniyetBasvuru();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.MezuniyetBasvurularis.Include("MezuniyetYayinKontrolDurumlari").First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                var kul = db.Kullanicilars.First(p => p.KullaniciID == basvuru.KullaniciID);

                #region BasvuruBilgi
                model.EnstituKod = basvuru.MezuniyetSureci.EnstituKod;
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                if (kul.KullaniciTipID != basvuru.KullaniciTipID)
                {
                    model.KullaniciTipID = kul.KullaniciTipID;
                    model.ResimAdi = kul.ResimAdi;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.TcKimlikNo = kul.TcKimlikNo;


                }
                else
                {
                    model.KullaniciTipID = basvuru.KullaniciTipID;
                    model.ResimAdi = basvuru.ResimAdi;
                    model.Ad = basvuru.Ad;
                    model.Soyad = basvuru.Soyad;
                    model.TcKimlikNo = basvuru.TcKimlikNo;
                    model.UyrukKod = basvuru.UyrukKod;
                }
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.First(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.IsTezDiliTr = basvuru.IsTezDiliTr;
                model.TezBaslikTr = basvuru.TezBaslikTr;
                model.TezBaslikEn = basvuru.TezBaslikEn;
                model.TezDanismanAdi = basvuru.TezDanismanAdi;
                model.TezDanismanUnvani = basvuru.TezDanismanUnvani;
                model.TezEsDanismanAdi = basvuru.TezEsDanismanAdi;
                model.TezEsDanismanUnvani = basvuru.TezEsDanismanUnvani;
                model.TezEsDanismanEMail = basvuru.TezEsDanismanEMail;
                model.TezOzet = basvuru.TezOzet;
                model.OzetAnahtarKelimeler = basvuru.OzetAnahtarKelimeler;
                model.TezOzetHtml = basvuru.TezOzetHtml;
                model.TezAbstract = basvuru.TezAbstract;
                model.TezAbstractHtml = basvuru.TezAbstractHtml;
                model.AbstractAnahtarKelimeler = basvuru.AbstractAnahtarKelimeler;
                model.DanismanImzaliFormDosyaAdi = basvuru.DanismanImzaliFormDosyaAdi;
                model.DanismanImzaliFormDosyaYolu = basvuru.DanismanImzaliFormDosyaYolu;
                model.IslemTarihi = DateTime.Now;
                model.IslemYapanID = UserIdentity.Current.Id;
                model.IslemYapanIP = UserIdentity.Ip;
                #endregion

                var yayins = (from qs in db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId)
                              join s in db.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                              from klD in defkl.DefaultIfEmpty()
                              join inx in db.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                              select new MezuniyetBasvurulariYayinDto
                              {
                                  Yayinlanmis = qs.Yayinlanmis,
                                  MezuniyetBasvurulariYayinID = qs.MezuniyetBasvurulariYayinID,
                                  YayinBasligi = qs.YayinBasligi,
                                  MezuniyetYayinTarih = qs.MezuniyetYayinTarih,
                                  MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                                  MezuniyetYayinTurID = qs.MezuniyetYayinTurID,
                                  MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                                  MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                                  MezuniyetYayinBelgeAdi = ybD != null ? qs.MezuniyetYayinBelgeAdi : "",
                                  MezuniyetYayinBelgeDosyaYolu = ybD != null ? qs.MezuniyetYayinBelgeDosyaYolu : "",
                                  MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                                  MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                                  MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                                  MezuniyetYayinKaynakLinkIsUrl = klkD != null ? klkD.IsUrl : false,
                                  MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = ymD != null ? qs.MezuniyetYayinMetniBelgeAdi : "",
                                  MezuniyetYayinMetniBelgeYolu = qs.MezuniyetYayinMetniBelgeYolu,
                                  MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                                  MezuniyetYayinLinkIsUrl = klD != null ? klD.IsUrl : false,
                                  MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                                  MezuniyetYayinKaynakLinki = qs.MezuniyetYayinKaynakLinki,
                                  MezuniyetYayinLinki = qs.MezuniyetYayinLinki,
                                  MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                                  MezuniyetYayinIndexTurAdi = inxD != null ? inxD.IndexTurAdi : "",
                                  MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                                  MezuniyetYayinKabulEdilmisMakaleAdi = qs.MezuniyetYayinKabulEdilmisMakaleAdi,
                                  MezuniyetYayinKabulEdilmisMakaleDosyaYolu = qs.MezuniyetYayinKabulEdilmisMakaleDosyaYolu,
                                  YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                                  ProjeDeatKurulus = qs.ProjeDeatKurulus,
                                  YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                                  DergiAdi = qs.DergiAdi,
                                  YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                                  IsProjeTamamlandiOrDevamEdiyor = qs.IsProjeTamamlandiOrDevamEdiyor,
                                  YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                                  ProjeEkibi = qs.ProjeEkibi,
                                  YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                                  ProjeTurAdi = qs.MezuniyetYayinProjeTurID.HasValue ? qs.MezuniyetYayinProjeTurleri.ProjeTurAdi : "",
                                  YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                                  YazarAdi = qs.YazarAdi,
                                  YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                                  YilCiltSayiSS = qs.YilCiltSayiSS,
                                  IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                                  TarihAraligi = qs.TarihAraligi


                              }).ToList();

                model.MezuniyetBasvuruYayinlari = yayins;
                if (basvuru.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi) model.Onaylandi = true;

            }
            return model;

        }

        public static MezuniyetBasvuruDetayDto GetMezuniyetBasvuruDetayBilgi(int mezuniyetBasvurulariId, int? mezuniyetBasvurulariYayinId = null, int? showDetayYayinId = null)
        {
            var model = new MezuniyetBasvuruDetayDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var basvuru = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

                var bsurec = basvuru.MezuniyetSureci;
                var bSurecOtKriter = bsurec.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == basvuru.OgrenimTipKod);
                var enstitu = db.Enstitulers.First(p => p.EnstituKod == bsurec.EnstituKod);

                var eslesenDanisman = db.Kullanicilars.FirstOrDefault(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0));
                if (eslesenDanisman != null)
                {
                    model.TezDanismaniUserKey = eslesenDanisman.UserKey;
                    model.TezDanismanBilgiEslesen = eslesenDanisman.Unvanlar.UnvanAdi + " " + eslesenDanisman.Ad + " " + eslesenDanisman.Soyad;
                }
                else
                {
                    model.TezDanismanBilgiEslesen = "Sistemde eşleşen tez danışmanı bulunamadı.";
                }
                model.MezuniyetSinavDurumID = basvuru.MezuniyetSinavDurumID;
                model.TezKontrolKullaniciID = basvuru.TezKontrolKullaniciID;

                model.RowID = basvuru.RowID;
                if (model.TezKontrolKullaniciID != null)
                {
                    var tezAtananKullanici =
                        db.Kullanicilars.FirstOrDefault(f => model.TezKontrolKullaniciID.HasValue && f.KullaniciID == model.TezKontrolKullaniciID);
                    model.TezKontrolYetkiliUserKey = tezAtananKullanici.UserKey;
                    model.TezKontrolYetkilisiAdSoyad = tezAtananKullanici.Ad + " " + tezAtananKullanici.Soyad;
                }
                model.MezuniyetBasvurulariTezDosyalariDtos = basvuru.MezuniyetBasvurulariTezDosyalaris.Select(s => new MezuniyetBasvurulariTezDosyalariDto
                {
                    MezuniyetBasvurulariTezDosyaID = s.MezuniyetBasvurulariTezDosyaID,
                    RowID = s.RowID,
                    SiraNo = s.SiraNo,
                    MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                    TezDosyaAdi = s.TezDosyaAdi,
                    TezDosyaYolu = s.TezDosyaYolu,
                    IsOnaylandiOrDuzeltme = s.IsOnaylandiOrDuzeltme,
                    Aciklama = s.Aciklama,
                    YuklemeTarihi = s.YuklemeTarihi,
                    OnayTarihi = s.OnayTarihi,
                    OnayYapanID = s.OnayYapanID,
                }).ToList();
                var onayYapanIDs = model.MezuniyetBasvurulariTezDosyalariDtos.Where(p => p.OnayYapanID.HasValue).Select(s => s.OnayYapanID).ToList();
                var kuls = db.Kullanicilars.Where(p => onayYapanIDs.Contains(p.KullaniciID)).ToList();
                foreach (var item in model.MezuniyetBasvurulariTezDosyalariDtos.Where(p => p.OnayYapanID.HasValue))
                {
                    var kul = kuls.First(p => p.KullaniciID == item.OnayYapanID);
                    item.UserKey = kul.UserKey;
                    item.OnayYapanTezKontrolYetkiliAdSoyad = kul.Ad + " " + kul.Soyad;

                }
                model.TezDanismanID = basvuru.TezDanismanID;
                model.IsAnketVar = bsurec.AnketID.HasValue;
                model.IsAnketDolduruldu = basvuru.AnketCevaplaris.Any();
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.KullaniciTipID = basvuru.KullaniciTipID;
                model.ResimAdi = basvuru.ResimAdi;
                model.Ad = basvuru.Ad;
                model.Soyad = basvuru.Soyad;
                model.TcKimlikNo = basvuru.TcKimlikNo;
                model.UyrukKod = basvuru.UyrukKod;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.First(p => p.EnstituKod == bsurec.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.IsTezDiliTr = basvuru.IsTezDiliTr;
                model.TezBaslikTr = basvuru.TezBaslikTr;
                model.TezBaslikEn = basvuru.TezBaslikEn;
                model.IsDanismanOnay = basvuru.IsDanismanOnay;
                model.DanismanOnayTarihi = basvuru.DanismanOnayTarihi;
                model.DanismanOnayAciklama = basvuru.DanismanOnayAciklama;
                model.TezDanismanUnvani = basvuru.TezDanismanUnvani;
                model.TezDanismanAdi = basvuru.TezDanismanUnvani + " " + basvuru.TezDanismanAdi;
                model.TezEsDanismanUnvani = basvuru.TezEsDanismanUnvani;
                if (!basvuru.TezEsDanismanUnvani.IsNullOrWhiteSpace())
                    model.TezEsDanismanAdi = basvuru.TezEsDanismanUnvani + " " + basvuru.TezEsDanismanAdi;
                model.TezEsDanismanEMail = basvuru.TezEsDanismanEMail;
                model.TezOzet = basvuru.TezOzet;
                model.OzetAnahtarKelimeler = basvuru.OzetAnahtarKelimeler;
                model.TezOzetHtml = basvuru.TezOzetHtml;
                model.TezAbstract = basvuru.TezAbstract;
                model.TezAbstractHtml = basvuru.TezAbstractHtml;
                model.AbstractAnahtarKelimeler = basvuru.AbstractAnahtarKelimeler;
                model.DanismanImzaliFormDosyaYolu = basvuru.DanismanImzaliFormDosyaYolu;
                model.DanismanImzaliFormDosyaAdi = basvuru.DanismanImzaliFormDosyaAdi;
                model.IsYerli = basvuru.KullaniciTipleri.Yerli;
                model.EnstituAdi = enstitu.EnstituAd;
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSureci = basvuru.MezuniyetSureci;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;

                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;
                var nowDate = DateTime.Now;
                model.BasvuruSureciTarihi = bsurec.BaslangicYil + "/" + bsurec.BitisYil + " " + db.Donemlers.First(p => p.DonemID == bsurec.DonemID).DonemAdi + " (" + bsurec.BaslangicTarihi.ToFormatDate() + "-" + bsurec.BitisTarihi.ToFormatDate() + ")";
                model.SonucGirisSureciAktif = bsurec.BaslangicTarihi <= nowDate && bsurec.BitisTarihi >= nowDate;
                model.IsMezunOldu = basvuru.IsMezunOldu;
                model.MezuniyetTarihi = basvuru.MezuniyetTarihi;
                model.EYKTarihi = basvuru.EYKTarihi;
                model.TezTeslimSonTarih = basvuru.TezTeslimSonTarih;
                model.MezuniyetJuriOneriFormlaris = db.MezuniyetJuriOneriFormlaris.Include("MezuniyetJuriOneriFormuJurileris").Where(p => p.MezuniyetBasvurulariID == basvuru.MezuniyetBasvurulariID).ToList();
                model.MezuniyetBasvurulariTezTeslimFormlaris = basvuru.MezuniyetBasvurulariTezTeslimFormlaris;

                model.EykYaGonderildi = model.MezuniyetJuriOneriFormlaris.Select(s => s.EYKYaGonderildi).FirstOrDefault();
                model.EykDaOnaylandi = model.MezuniyetJuriOneriFormlaris.Select(s => s.EYKDaOnaylandi).FirstOrDefault();
                var yayins = (from qs in db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId)
                              join s in db.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                              from klD in defkl.DefaultIfEmpty()
                              join inx in db.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                              select new MezuniyetBasvurulariYayinDto
                              {
                                  MezuniyetYayinTurID = qs.MezuniyetYayinTurID,
                                  ShowDetayYayinID = showDetayYayinId,
                                  MezuniyetBasvurulariYayinID = qs.MezuniyetBasvurulariYayinID,
                                  MezuniyetBasvurulariID = qs.MezuniyetBasvurulariID,
                                  DanismanIsmiVar = qs.DanismanIsmiVar,
                                  TezIcerikUyumuVar = qs.TezIcerikUyumuVar,
                                  Onaylandi = qs.Onaylandi,
                                  RetAciklamasi = qs.RetAciklamasi,
                                  YayinBasligi = qs.YayinBasligi,
                                  Yayinlanmis = qs.Yayinlanmis,
                                  MezuniyetYayinTarih = qs.MezuniyetYayinTarih,
                                  MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                                  MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                                  MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                                  MezuniyetYayinBelgeAdi = ybD != null ? qs.MezuniyetYayinBelgeAdi : "",
                                  MezuniyetYayinBelgeDosyaYolu = ybD != null ? qs.MezuniyetYayinBelgeDosyaYolu : "",
                                  MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                                  MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                                  MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                                  MezuniyetYayinKaynakLinkIsUrl = klkD != null && klkD.IsUrl,
                                  MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = ymD != null ? qs.MezuniyetYayinMetniBelgeAdi : "",
                                  MezuniyetYayinMetniBelgeYolu = qs.MezuniyetYayinMetniBelgeYolu,
                                  MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                                  MezuniyetYayinLinkIsUrl = klD != null && klD.IsUrl,
                                  MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                                  MezuniyetYayinKaynakLinki = qs.MezuniyetYayinKaynakLinki,
                                  MezuniyetYayinLinki = qs.MezuniyetYayinLinki,
                                  MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                                  MezuniyetYayinIndexTurAdi = inxD != null ? inxD.IndexTurAdi : "",
                                  MezuniyetYayinIndexTurID = qs.MezuniyetYayinIndexTurID,
                                  YayinIndexTurleri = db.MezuniyetYayinIndexTurleris.ToList(),
                                  MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                                  MezuniyetYayinKabulEdilmisMakaleAdi = qs.MezuniyetYayinKabulEdilmisMakaleAdi,
                                  MezuniyetYayinKabulEdilmisMakaleDosyaYolu = qs.MezuniyetYayinKabulEdilmisMakaleDosyaYolu,
                                  YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                                  ProjeDeatKurulus = qs.ProjeDeatKurulus,
                                  YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                                  DergiAdi = qs.DergiAdi,
                                  YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                                  IsProjeTamamlandiOrDevamEdiyor = qs.IsProjeTamamlandiOrDevamEdiyor,
                                  YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                                  ProjeEkibi = qs.ProjeEkibi,
                                  YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                                  MezuniyetYayinProjeTurID = qs.MezuniyetYayinProjeTurID,
                                  ProjeTurAdi = qs.MezuniyetYayinProjeTurID.HasValue ? qs.MezuniyetYayinProjeTurleri.ProjeTurAdi : "",
                                  YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                                  YazarAdi = qs.YazarAdi,
                                  YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                                  YilCiltSayiSS = qs.YilCiltSayiSS,
                                  IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                                  TarihAraligi = qs.TarihAraligi,
                                  YayinYerBilgisiIstensin = s.YayinYerBilgisiIstensin,
                                  YerBilgisi = qs.YerBilgisi,
                                  YayinEtkinlikAdiIstensin = s.YayinEtkinlikAdiIstensin,
                                  EtkinlikAdi = qs.EtkinlikAdi


                              });

                if (mezuniyetBasvurulariYayinId.HasValue)
                {
                    model.SelectedYayin = yayins.First(p => p.MezuniyetBasvurulariYayinID == mezuniyetBasvurulariYayinId);
                }
                else
                {
                    model.YayinBilgileri = yayins.ToList();
                }

                #region SalonRezervasyonlari 
                model.MezuniyetSrModel.SalonRezervasyonlari = (from s in db.SRTalepleris
                                                               join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                                                               join mb in db.MezuniyetBasvurularis on s.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                                                               join sal in db.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                                                               from defSl in def1.DefaultIfEmpty()
                                                               join hg in db.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                                                               join d in db.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                                                               join sd in db.MezuniyetSinavDurumlaris on (s.MezuniyetSinavDurumID ?? MezuniyetSinavDurumEnum.SonucGirilmedi) equals sd.MezuniyetSinavDurumID into def2
                                                               from defSd in def2.DefaultIfEmpty()
                                                               join sdj in db.MezuniyetSinavDurumlaris on (s.JuriSonucMezuniyetSinavDurumID ?? MezuniyetSinavDurumEnum.SonucGirilmedi) equals sdj.MezuniyetSinavDurumID into def3
                                                               from defsdj in def3.DefaultIfEmpty()
                                                               let jof = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault()
                                                               where s.MezuniyetBasvurulariID == basvuru.MezuniyetBasvurulariID
                                                               select new FrTalepler
                                                               {
                                                                   SRTalepID = s.SRTalepID,
                                                                   UniqueID = s.UniqueID,
                                                                   TalepYapanID = s.TalepYapanID,
                                                                   TalepTipAdi = tt.TalepTipAdi,
                                                                   SRTalepTipID = s.SRTalepTipID,
                                                                   SRSalonID = s.SRSalonID,
                                                                   SalonAdi = s.SRSalonID.HasValue ? defSl.SalonAdi : s.SalonAdi,
                                                                   DegerlendirmeSonucMailTarihi = s.DegerlendirmeSonucMailTarihi,

                                                                   Tarih = s.Tarih,
                                                                   HaftaGunID = s.HaftaGunID,
                                                                   HaftaGunAdi = hg.HaftaGunAdi,
                                                                   BasSaat = s.BasSaat,
                                                                   BitSaat = s.BitSaat,
                                                                   MezuniyetSinavDurumID = s.MezuniyetSinavDurumID,
                                                                   MezuniyetSinavDurumIslemTarihi = s.MezuniyetSinavDurumIslemTarihi,
                                                                   MezuniyetSinavDurumIslemYapanID = s.MezuniyetSinavDurumIslemYapanID,
                                                                   SDurumAdi = defSd != null ? defSd.MezuniyetSinavDurumAdi : "",
                                                                   SDurumListeAdi = defSd != null ? defSd.MezuniyetSinavDurumAdi : "",
                                                                   SClassName = defSd != null ? defSd.ClassName : "",
                                                                   SColor = defSd != null ? defSd.Color : "",
                                                                   SRDurumID = s.SRDurumID,
                                                                   DurumAdi = d.DurumAdi,
                                                                   DurumListeAdi = d.DurumAdi,
                                                                   ClassName = d.ClassName,
                                                                   Color = d.Color,
                                                                   SRDurumAciklamasi = s.SRDurumAciklamasi,
                                                                   JuriSonucMezuniyetSinavDurumID = s.JuriSonucMezuniyetSinavDurumID,
                                                                   IsOyBirligiOrCoklugu = s.IsOyBirligiOrCoklugu,
                                                                   RSBaslatildiMailGonderimTarihi = s.RSBaslatildiMailGonderimTarihi,
                                                                   JuriSonucMezuniyetSinavDurumAdi = defsdj.MezuniyetSinavDurumAdi,
                                                                   IslemTarihi = s.IslemTarihi,
                                                                   IslemYapanID = s.IslemYapanID,
                                                                   IslemYapanIP = s.IslemYapanIP,
                                                                   IsTezBasligiDegisti = s.IsTezBasligiDegisti,
                                                                   IsTezDiliTr = mb.IsTezDiliTr == true,
                                                                   TezBaslikTr = jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikTr : mb.TezBaslikTr,
                                                                   TezBaslikEn = jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikEn : mb.TezBaslikEn,
                                                                   YeniTezBaslikTr = s.YeniTezBaslikTr,
                                                                   YeniTezBaslikEn = s.YeniTezBaslikEn,
                                                                   SRTaleplerJuris = s.SRTaleplerJuris.ToList(),
                                                                   IsSonSrTalebi = !mb.SRTalepleris.Any(a => a.SRTalepID > s.SRTalepID),
                                                                   UzatmaSonrasiOgrenciTaahhutSonTarih = s.UzatmaSonrasiOgrenciTaahhutSonTarih,
                                                                   UzatmaTaahhutSonTarih = s.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ?
                                                                                                          s.UzatmaSonrasiOgrenciTaahhutSonTarih ?? (DbFunctions.AddDays(s.Tarih, bSurecOtKriter.SinavUzatmaOgrenciTaahhutMaxGun).Value)
                                                                                                          : DateTime.Now,

                                                                   UzatmaSonrasiYeniSinavTalebiSonTarih = s.UzatmaSonrasiYeniSinavTalebiSonTarih,
                                                                   UzatmaSonSrTarih = s.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ?
                                                                                                s.UzatmaSonrasiYeniSinavTalebiSonTarih ?? DbFunctions.AddDays(s.Tarih, bSurecOtKriter.SinavUzatmaSinavAlmaSuresiMaxGun).Value
                                                                                                : DateTime.Now,
                                                                   TezTeslimSonTarih = model.TezTeslimSonTarih ?? DbFunctions.AddDays(s.Tarih, bSurecOtKriter.TezTeslimSuresiGun).Value,

                                                                   IsOgrenciUzatmaSonrasiOnay = s.IsOgrenciUzatmaSonrasiOnay,
                                                                   OgrenciOnayTarihi = s.OgrenciOnayTarihi,
                                                                   IsDanismanUzatmaSonrasiOnay = s.IsDanismanUzatmaSonrasiOnay,
                                                                   DanismanOnayTarihi = s.DanismanOnayTarihi,
                                                                   DanismanUzatmaSonrasiOnayAciklama = s.DanismanUzatmaSonrasiOnayAciklama,
                                                                   IsYokDrBursiyeriVar = s.IsYokDrBursiyeriVar,
                                                                   YokDrOncelikliAlan = s.YokDrOncelikliAlan

                                                               }).OrderByDescending(o => o.SRTalepID).ToList();
                foreach (var item in model.MezuniyetSrModel.SalonRezervasyonlari)
                {
                    var haricSinavDurumId = new List<int>();
                    if (model.MezuniyetSrModel.SalonRezervasyonlari.Any(a => a.SRTalepID < item.SRTalepID && a.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma))
                        haricSinavDurumId.Add(MezuniyetSinavDurumEnum.Uzatma);
                    item.SrDurumSelectList.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe(), "Value", "Caption", item.SRDurumID);
                    item.SrDurumSelectList.MezuniyetSinavDurumID = new SelectList(MezuniyetBus.GetCmbMzSinavDurumListe(false, haricSinavDurumId), "Value", "Caption", item.MezuniyetSinavDurumID);

                }
                model.MezuniyetDurumSelectList.IsMezunOldu = new SelectList(MezuniyetBus.GetCmbMezuniyetDurum(), "Value", "Caption", model.IsMezunOldu);
                model.MezuniyetSrModel.EykIlkSrMaxTarih = model.EYKTarihi.HasValue ? (model.TezTeslimSonTarih ?? model.EYKTarihi.Value.AddDays(bSurecOtKriter.TezTeslimSuresiGun)) : (DateTime?)null;
                model.MezuniyetSrModel.IsSrEykSureAsimi = model.EYKTarihi.HasValue && model.MezuniyetSrModel.EykIlkSrMaxTarih < DateTime.Now.Date;


                #endregion

                if (model.IsAnketDolduruldu == false)
                {
                    if (bsurec.AnketID.HasValue)
                    {
                        if (!db.AnketCevaplaris.Any(a => a.MezuniyetBasvurulariID == mezuniyetBasvurulariId))
                        {

                            var anketSorulari = (from bsa in db.Ankets.Where(p => p.AnketID == bsurec.AnketID)
                                                 join aso in db.AnketSorus on bsa.AnketID equals aso.AnketID
                                                 join sb in db.AnketCevaplaris.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId && p.Basvurular.KullaniciID == basvuru.KullaniciID) on aso.AnketSoruID equals sb.AnketSoruID into def1
                                                 from sbc in def1.DefaultIfEmpty()
                                                 select new
                                                 {
                                                     aso.AnketSoruID,
                                                     AnketSoruSecenekID = sbc != null ? sbc.AnketSoruSecenekID : null,
                                                     aso.IsTabloVeriGirisi,
                                                     aso.IsTabloVeriMaxSatir,
                                                     Aciklama = sbc != null ? sbc.EkAciklama : "",
                                                     aso.SiraNo,
                                                     aso.SoruAdi,
                                                     Secenekler = aso.AnketSoruSeceneks.Select(ss => new
                                                     {
                                                         ss.AnketSoruSecenekID,
                                                         ss.AnketSoruID,
                                                         ss.SiraNo,
                                                         ss.IsYaziOrSayi,
                                                         ss.IsEkAciklamaGir,
                                                         ss.SecenekAdi
                                                     }).OrderBy(o => o.SiraNo).ToList()


                                                 }).OrderBy(o => o.SiraNo).ToList();
                            var modelAnk = new KmAnketlerCevap
                            {
                                RowID = basvuru.RowID.ToString(),
                                AnketTipID = 4,
                                BasvuruSurecID = bsurec.MezuniyetSurecID,
                                AnketID = bsurec.AnketID.Value,
                                JsonStringData = anketSorulari.ToJson()
                            };
                            foreach (var item in anketSorulari)
                            {
                                modelAnk.AnketCevapModel.Add(new AnketCevapDto
                                {
                                    SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                                    SoruBilgi = new FrAnketDetayDto { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama, IsTabloVeriGirisi = item.IsTabloVeriGirisi, IsTabloVeriMaxSatir = item.IsTabloVeriMaxSatir },
                                    SoruSecenek = item.Secenekler.Select(s => new FrAnketSecenekDetayDto { AnketSoruSecenekID = s.AnketSoruSecenekID, SiraNo = s.SiraNo, IsEkAciklamaGir = s.IsEkAciklamaGir, SecenekAdi = s.SecenekAdi }).ToList(),
                                    SelectListSoruSecenek = new SelectList(item.Secenekler.ToList(), "AnketSoruSecenekID", "SecenekAdi", item.AnketSoruSecenekID)
                                });
                            }

                            model.AnketView = ViewRenderHelper.RenderPartialView("Ajax", "getAnket", modelAnk);
                        }
                    }
                }

                var bdurum = basvuru.MezuniyetYayinKontrolDurumlari;
                model.MezuniyetYayinKontrolDurumAdi = bdurum.MezuniyetYayinKontrolDurumAdi;
                model.DurumClassName = bdurum.ClassName;
                model.DurumColor = bdurum.Color;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
            }
            return model;

        }
        public static MmMessage TezKontrol(KmMezuniyetBasvuru kModel)
        {
            var mmMessage = new MmMessage();
            if (!kModel.IsTezDiliTr.HasValue)
            {
                mmMessage.Messages.Add("Tez dilini seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsTezDiliTr" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IsTezDiliTr" });
            if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Başlığını Türkçe Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezBaslikTr" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezBaslikTr" });
            if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Başlığını İngilizce Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezBaslikEn" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezBaslikEn" });

            if (kModel.TezDanismanUnvani.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Danışman Unvanı Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezDanismanUnvani" });
            }
            if (kModel.TezDanismanAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Danışman Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezDanismanAdi" });
            }
            if (!kModel.TezDanismanAdi.IsNullOrWhiteSpace() && !kModel.TezDanismanUnvani.IsNullOrWhiteSpace())
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezDanismanAdi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezDanismanUnvani" });
            }


            if (!kModel.TezEsDanismanAdi.IsNullOrWhiteSpace() || !kModel.TezEsDanismanUnvani.IsNullOrWhiteSpace() || !kModel.TezEsDanismanEMail.IsNullOrWhiteSpace())
            {
                if (kModel.TezEsDanismanUnvani.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Tez Eş Danışman Unvanı Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezEsDanismanUnvani" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezEsDanismanUnvani" });

                if (kModel.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Tez Eş Danışman Adı Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezEsDanismanAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezEsDanismanAdi" });
                if (kModel.TezEsDanismanEMail.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Eş Danışman E-Posta Bilgisini Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezEsDanismanEMail" });

                }
                else if (kModel.TezEsDanismanEMail.ToIsValidEmail())
                {
                    mmMessage.Messages.Add("Lütfen E-Posta Adres Tekrarını Uygun Formatta Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezEsDanismanEMail" });

                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezEsDanismanEMail" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "TezEsDanismanAdi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "TezEsDanismanUnvani" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "TezEsDanismanEMail" });
            }
            if (kModel.TezOzet.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özetini Türkçe Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezOzet" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezOzet" });
            if (kModel.OzetAnahtarKelimeler.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özeti Anahtar Kelimelerini Türkçe Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OzetAnahtarKelimeler" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OzetAnahtarKelimeler" });
            if (kModel.TezAbstract.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özetini İngilizce Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezAbstract" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezAbstract" });
            if (kModel.AbstractAnahtarKelimeler.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özeti Anahtar Kelimelerini İngilizce Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AbstractAnahtarKelimeler" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "AbstractAnahtarKelimeler" });

            return mmMessage;
        }

        public static MezuniyetSureciYonetmelikleri GetMezuniyetAktifYonetmelik(int mezuniyetSurecId, int kullaniciId, int? mezuniyetBasvurulariId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                decimal baslangic;
                if (mezuniyetBasvurulariId > 0)
                {
                    var mBasvuru = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    baslangic = Convert.ToDecimal(mBasvuru.KayitOgretimYiliBaslangic + "," + mBasvuru.KayitOgretimYiliDonemID.Value);
                }
                else
                {
                    var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                    baslangic = Convert.ToDecimal(kul.KayitYilBaslangic + "," + kul.KayitDonemID.Value);
                }
                var kriter = db.MezuniyetSureciYonetmelikleris.Include("MezuniyetSureciYonetmelikleriOTs").Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList().First(f =>
                    f.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeOncesi ?
                        (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) >= baslangic)
                        :
                        (f.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeSonrasi ?
                            (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic)
                            :
                            (
                                (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic && Convert.ToDecimal(f.BaslangicYilB + "," + f.DonemIDB) >= baslangic)
                            )
                        )

                );
                return kriter;
            }
        }
        public static List<MezuniyetSureciYonetmelikleriOT> GetMezuniyetAktifOgrenimTipiYayinBilgileri(int mezuniyetSurecId, int kullaniciId, int mezuniyetBasvurulariId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                decimal baslangic;
                int ogrenimTipKod;
                if (mezuniyetBasvurulariId > 0)
                {
                    var mBasvuru = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    baslangic = Convert.ToDecimal(mBasvuru.KayitOgretimYiliBaslangic + "," + mBasvuru.KayitOgretimYiliDonemID.Value);
                    ogrenimTipKod = mBasvuru.OgrenimTipKod;
                }
                else
                {
                    var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                    baslangic = Convert.ToDecimal(kul.KayitYilBaslangic + "," + kul.KayitDonemID.Value);
                    ogrenimTipKod = kul.OgrenimTipKod.Value;
                }
                var kriter = db.MezuniyetSureciYonetmelikleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList().First(f =>
                    f.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeOncesi ?
                        (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) >= baslangic)
                        :
                        (f.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeSonrasi ?
                            (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic)
                            :
                            (
                                (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic && Convert.ToDecimal(f.BaslangicYilB + "," + f.DonemIDB) >= baslangic)
                            )
                        )

                );
                var ots = kriter.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == ogrenimTipKod).ToList();
                return ots;
            }
        }
        public static MmMessage YayinKontrol(KmMezuniyetBasvuru kModel)
        {
            var mmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                //var kriterSecim=db.MezuniyetSureciYonetmelikleris.Where(p=>p.BaslangicYil)
                var kriter = GetMezuniyetAktifYonetmelik(kModel.MezuniyetSurecID, kModel.KullaniciID, kModel.MezuniyetBasvurulariID);
                var yturAds = db.MezuniyetYayinTurleris.ToList();
                var kul = db.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                var kriterDetay = (from s in kriter.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == kul.OgrenimTipKod).ToList()
                                   join yta in yturAds on s.MezuniyetYayinTurID equals yta.MezuniyetYayinTurID
                                   group new { s.MezuniyetYayinTurID, yta.MezuniyetYayinTurAdi, s.OgrenimTipKod, s.IsGecerli, s.IsZorunlu, s.GrupKodu } by new { s.IsZorunlu, IsGrup = s.GrupKodu.IsNullOrWhiteSpace() == false, s.GrupKodu } into g1
                                   select new
                                   {
                                       g1.Key.IsGrup,
                                       g1.Key.GrupKodu,
                                       g1.Key.IsZorunlu,
                                       data = g1.ToList()
                                   }).ToList();


                var qYbaslik = kModel._YayinBasligi.Select((s, inx) => new { YayinBasligi = s, Index = inx }).ToList();
                var qYtarih = kModel._MezuniyetYayinTarih.Select((s, inx) => new { MezuniyetYayinTarih = s, Index = inx }).ToList();
                var qMytId = kModel._MezuniyetYayinTurID.Select((s, inx) => new { MezuniyetYayinTurID = s, Index = inx }).ToList();
                var qMybelge = kModel._MezuniyetYayinBelgesiAdi.Select((s, inx) => new { MezuniyetYayinBelgesiAdi = s, Index = inx }).ToList();
                var qMkLink = kModel._MezuniyetYayinKaynakLinki.Select((s, inx) => new { MezuniyetYayinKaynakLinki = s, Index = inx }).ToList();
                var qMbelge = kModel._YayinMetniBelgesiAdi.Select((s, inx) => new { YayinMetniBelgesiAdi = s, Index = inx }).ToList();
                var qMyLink = kModel._MezuniyetYayinLinki.Select((s, inx) => new { MezuniyetYayinLinki = s, Index = inx }).ToList();
                var qIndex = kModel._MezuniyetYayinIndexTurID.Select((s, inx) => new { MezuniyetYayinIndexTurID = s, Index = inx }).ToList();

                var qYayins = (from b in qYbaslik
                               join myt in qMytId on b.Index equals myt.Index
                               join yt in qYtarih on b.Index equals yt.Index
                               join yta in yturAds on myt.MezuniyetYayinTurID equals yta.MezuniyetYayinTurID
                               join myb in qMybelge on b.Index equals myb.Index
                               join mkl in qMkLink on b.Index equals mkl.Index
                               join mb in qMbelge on b.Index equals mb.Index
                               join myl in qMyLink on b.Index equals myl.Index
                               join mI in qIndex on b.Index equals mI.Index
                               select new
                               {
                                   b.Index,
                                   myt.MezuniyetYayinTurID,
                                   yta.MezuniyetYayinTurAdi,
                                   yt.MezuniyetYayinTarih,
                                   myb.MezuniyetYayinBelgesiAdi,
                                   mkl.MezuniyetYayinKaynakLinki,
                                   mb.YayinMetniBelgesiAdi,
                                   myl.MezuniyetYayinLinki,
                                   mI.MezuniyetYayinIndexTurID
                               }).ToList();
                if (kriterDetay.Any(p => p.IsZorunlu) == false && qYbaslik.Count > 0)
                {
                    mmMessage.Messages.Add("Mezuniyet başvurunuz için herhangi bir yayın bilgisi istenmemektedir. Yayın bilgisi ekleyemezsiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinTurID" });
                }
                else
                {
                    foreach (var item in kriterDetay)
                    {
                        if (item.IsZorunlu)
                        {
                            if (item.IsGrup)
                            {
                                var contains = item.data.Select(s => new { s.MezuniyetYayinTurID, s.MezuniyetYayinTurAdi }).ToList();

                                if (!qYayins.Any(a => contains.Any(a2 => a2.MezuniyetYayinTurID == a.MezuniyetYayinTurID)) && kModel.YayinBilgisi == null)
                                {
                                    mmMessage.Messages.Add(string.Join(", ", contains.Select(s => s.MezuniyetYayinTurAdi)) + " Yayın Türlerinden birinin eklenmesi gerekmektedir.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                                else if (qYayins.Count(a => contains.Any(a2 => a2.MezuniyetYayinTurID == a.MezuniyetYayinTurID)) > 1)
                                {
                                    mmMessage.Messages.Add(string.Join(", ", contains.Select(s => s.MezuniyetYayinTurAdi)) + " Yayın Türlerinden sadece biri eklenebilir.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                            }
                            else
                            {
                                foreach (var item2 in item.data)
                                {
                                    if (qYayins.All(a => item2.MezuniyetYayinTurID != a.MezuniyetYayinTurID) && kModel.YayinBilgisi == null)
                                    {

                                        mmMessage.Messages.Add(item2.MezuniyetYayinTurAdi + " Yayın türünün eklenmesi gerekmektedir.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinTurID" });
                                    }

                                    else if (qYayins.Count(a => item2.MezuniyetYayinTurID == a.MezuniyetYayinTurID) > 1)
                                    {
                                        mmMessage.Messages.Add(item2.MezuniyetYayinTurAdi + " Yayın Türünden sadece 1 adet eklenebilir.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinTurID" });
                                    }
                                }

                            }
                        }
                        else
                        {
                            foreach (var itemY in qYayins)
                            {
                                if (item.data.Any(a => a.MezuniyetYayinTurID == itemY.MezuniyetYayinTurID && a.IsZorunlu == false))
                                {
                                    mmMessage.Messages.Add(itemY.MezuniyetYayinTurAdi + " Yayın kabul edilmemektedir. Bu Yayın bilgisini eklenemez.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                            }

                            if (mmMessage.Messages.Any())
                            {
                                var zorunlular = kriterDetay.Where(p => p.IsZorunlu).SelectMany(s => s.data)
                                       .Select(s => s.MezuniyetYayinTurAdi).ToList();
                                if (zorunlular.Any()) mmMessage.Messages.Add("Zorunlu istenen yayın türleri: " + string.Join(", ", zorunlular));
                                //    var zorunluOlmayanlar = kriterDetay.Where(p => !p.IsZorunlu).SelectMany(s => s.data).Where(p => p.IsGecerli)
                                //        .Select(s => s.MezuniyetYayinTurAdi).ToList();
                                //    if (zorunluOlmayanlar.Any()) mmMessage.Messages.Add("Zorunlu olmayan yayın türleri: " + string.Join(", ", zorunluOlmayanlar));
                                //    var kabulEdilmeyenler = kriterDetay.Where(p => !p.IsZorunlu).SelectMany(s => s.data).Where(p => !p.IsGecerli)
                                //        .Select(s => s.MezuniyetYayinTurAdi).ToList();
                                //    if (kabulEdilmeyenler.Any()) mmMessage.Messages.Add("Kabul edilmeyen yayın türleri: " + string.Join(", ", kabulEdilmeyenler));
                            }
                        }
                    }
                }

            }
            return mmMessage;
        }
        public static KmMezuniyetSureciOgrenimTipModel GetMezuniyetOgrenimTipKriterleri(string enstituKod, int mezuniyetSurecId)
        {
            var model = new KmMezuniyetSureciOgrenimTipModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ogrenimTipleri = db.OgrenimTipleris
                    .Where(p => p.EnstituKod == enstituKod && p.IsMezuniyetBasvurusuYapabilir).ToList();

                var sonMezuniyetSurecId = db.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod && p.MezuniyetSurecID != mezuniyetSurecId)
                    .OrderByDescending(t => t.MezuniyetSurecID).Select(s => s.MezuniyetSurecID).FirstOrDefault();
                var sonMezuniyetOgrenimTipleri = db.MezuniyetSurecis
                    .Where(p => p.MezuniyetSurecID == sonMezuniyetSurecId).SelectMany(s => s.MezuniyetSureciOgrenimTipKriterleris).ToList();

                var mezuniyetOgrenimTipleri = db.MezuniyetSureciOgrenimTipKriterleris
                    .Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList();



                model.OgrenimTipKriterList = (from ogrenimTipi in ogrenimTipleri
                                              join surecOgrenimTip in mezuniyetOgrenimTipleri on ogrenimTipi.OgrenimTipID equals surecOgrenimTip
                                                  .OgrenimTipID into defSurecOgrenimTip
                                              from surecOgrenimTipi in defSurecOgrenimTip.DefaultIfEmpty()
                                              join sonSurecOgrenimTip in sonMezuniyetOgrenimTipleri on ogrenimTipi.OgrenimTipID equals
                                                  sonSurecOgrenimTip.OgrenimTipID into defSonSureciOgrenimTip
                                              from sonSurecOgrenimTipi in defSonSureciOgrenimTip.DefaultIfEmpty()
                                              select new KmMezuniyetSureciOgrenimTipKriterleri
                                              {
                                                  MezuniyetSureciOgrenimTipKriterID = surecOgrenimTipi?.MezuniyetSureciOgrenimTipKriterID ?? 0,
                                                  OgrenimTipKod = ogrenimTipi.OgrenimTipKod,
                                                  OgrenimTipID = ogrenimTipi.OgrenimTipID,
                                                  OgrenimTipAdi = ogrenimTipi.OgrenimTipAdi,
                                                  AktifDonemMaxKriteri = surecOgrenimTipi?.AktifDonemMaxKriteri ?? (sonSurecOgrenimTipi?.AktifDonemMaxKriteri ?? 0),
                                                  AktifDonemDersKodKriteri = surecOgrenimTipi != null
                                                      ? surecOgrenimTipi.AktifDonemDersKodKriteri
                                                      : sonSurecOgrenimTipi?.AktifDonemDersKodKriteri ?? "",
                                                  AktifDonemEtikNotKriteri = surecOgrenimTipi != null
                                                      ? surecOgrenimTipi.AktifDonemEtikNotKriteri
                                                      : sonSurecOgrenimTipi?.AktifDonemEtikNotKriteri ?? "",
                                                  AktifDonemSeminerNotKriteri = surecOgrenimTipi != null
                                                      ? surecOgrenimTipi.AktifDonemSeminerNotKriteri
                                                      : sonSurecOgrenimTipi?.AktifDonemSeminerNotKriteri ?? "",
                                                  AktifDonemToplamKrediKriteri = surecOgrenimTipi?.AktifDonemToplamKrediKriteri ??
                                                                               sonSurecOgrenimTipi?.AktifDonemToplamKrediKriteri ?? 0,
                                                  AktifDonemAgnoKriteri = surecOgrenimTipi?.AktifDonemAgnoKriteri ??
                                                                        sonSurecOgrenimTipi?.AktifDonemAgnoKriteri ?? 0,
                                                  AktifDonemAktsKriteri = surecOgrenimTipi?.AktifDonemAktsKriteri ??
                                                                        sonSurecOgrenimTipi?.AktifDonemAktsKriteri ?? 0,
                                                  SinavUzatmaOgrenciTaahhutMaxGun = surecOgrenimTipi?.SinavUzatmaOgrenciTaahhutMaxGun ??
                                                                                      sonSurecOgrenimTipi?.SinavUzatmaOgrenciTaahhutMaxGun ?? 0,
                                                  SinavUzatmaSinavAlmaSuresiMaxGun = surecOgrenimTipi?.SinavUzatmaSinavAlmaSuresiMaxGun ??
                                                                                       sonSurecOgrenimTipi?.SinavUzatmaSinavAlmaSuresiMaxGun ?? 0,
                                                  TezTeslimSuresiGun = surecOgrenimTipi?.TezTeslimSuresiGun ??
                                                                         sonSurecOgrenimTipi?.TezTeslimSuresiGun ?? 0,
                                                  SinavKacGunSonraAlabilir = surecOgrenimTipi?.SinavKacGunSonraAlabilir ??
                                                                                  sonSurecOgrenimTipi?.SinavKacGunSonraAlabilir ?? 0,


                                              }).ToList();

                foreach (var item in model.OgrenimTipKriterList)
                {
                    item.SlistEtikNots = new SelectList(YeterlikBus.NotDegerleri, item.AktifDonemEtikNotKriteri);
                    item.SlistSeminerNots = new SelectList(YeterlikBus.NotDegerleri, item.AktifDonemSeminerNotKriteri);
                }
            }
            return model;
        }
        public static MezuniyetBasvurulariYayinDto GetYayinBilgisi(int mezuniyetSurecId, int mezuniyetYayinTurId)
        {
            MezuniyetBasvurulariYayinDto mdl;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                mdl = (from s in db.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId && p.MezuniyetYayinTurID == mezuniyetYayinTurId)
                       join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                       join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                       from ybD in defyb.DefaultIfEmpty()
                       join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                       from klkD in defklk.DefaultIfEmpty()
                       join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                       from ymD in defym.DefaultIfEmpty()
                       join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                       from klD in defkl.DefaultIfEmpty()
                       join inx in db.MezuniyetYayinIndexTurleris on new { s.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                       from inxD in definx.DefaultIfEmpty()
                       select new MezuniyetBasvurulariYayinDto
                       {
                           MezuniyetYayinTurID = s.MezuniyetYayinTurID,
                           MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                           MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                           MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                           MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                           MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                           MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                           MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                           MezuniyetYayinKaynakLinkIsUrl = klD != null && klD.IsUrl,
                           MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                           MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                           MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                           MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                           MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                           MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                           MezuniyetYayinLinkIsUrl = klD != null && klD.IsUrl,
                           MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                           MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                           MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                           YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                           YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                           YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                           YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                           YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                           YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                           YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                           IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                           YayinEtkinlikAdiIstensin = s.YayinEtkinlikAdiIstensin,
                           YayinYerBilgisiIstensin = s.YayinYerBilgisiIstensin,
                       }).First();
                mdl.YayinIndexTurleri = db.MezuniyetYayinIndexTurleris.ToList();
                mdl.MezuniyetYayinProjeTurleris = db.MezuniyetYayinProjeTurleris.ToList();
            }
            return mdl;
        }
        public static List<MezuniyetYayinKontrolDurumlari> GetMezuniyetYayinDurumListe(List<int> selectedBDurumId = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif);
                if (selectedBDurumId != null) qdata = qdata.Where(p => selectedBDurumId.Contains(p.MezuniyetYayinKontrolDurumID)).OrderBy(o => o.MezuniyetYayinKontrolDurumID);
                var data = qdata.ToList();
                return data;

            }

        }
        public static bool IsMakaleYayinDurumIsteniyor(this int yayinTurId)
        {
            return new List<int> { 5, 4 }.Contains(yayinTurId);
        }

        public static IHtmlString ToMezuniyetDurum(this FrMezuniyetBasvurulari model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Mezuniyet", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayBasvuru(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t1_Basvuru");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayEykSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t2_EYKSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetaySinavSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t3_SinavSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayTezKontrolSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t4_TezKontrolSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayMezuniyetSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t5_MezuniyetSureci");
            return pagerString;
        }

        public static bool ToJoFormSuccessRow(this string juriTipAdi, bool tezDiliTr, bool adSoyadSuccess, bool unvanAdiSuccess, bool eMailSuccess, bool universiteAdiSuccess, bool uzmanlikAlaniSuccess)
        {
            bool retVal;
            if (new List<string> { "YtuIciJuri4", "YtuDisiJuri4" }.Contains(juriTipAdi))
            {
                if (tezDiliTr)
                {
                    retVal = (
                                (
                                    adSoyadSuccess &&
                                    unvanAdiSuccess &&
                                    eMailSuccess &&
                                    universiteAdiSuccess &&
                                    uzmanlikAlaniSuccess
                                 )
                                 ||
                                 (
                                     !adSoyadSuccess &&
                                     !unvanAdiSuccess &&
                                     !eMailSuccess &&
                                     !universiteAdiSuccess &&
                                     !uzmanlikAlaniSuccess

                                 )
                            );
                }
                else
                {
                    retVal = (
                               (
                                   adSoyadSuccess &&
                                   unvanAdiSuccess &&
                                   eMailSuccess &&
                                   universiteAdiSuccess &&
                                   uzmanlikAlaniSuccess
                                )
                                ||
                                (
                                    !adSoyadSuccess &&
                                    !unvanAdiSuccess &&
                                    !eMailSuccess &&
                                    !universiteAdiSuccess &&
                                    !uzmanlikAlaniSuccess
                                )
                           );
                }

            }
            else
            {
                if (tezDiliTr)
                {
                    retVal = (
                                  adSoyadSuccess &&
                                  unvanAdiSuccess &&
                                  eMailSuccess &&
                                  universiteAdiSuccess &&
                                  uzmanlikAlaniSuccess
                              );
                }
                else
                {
                    retVal = (
                                 adSoyadSuccess &&
                                 unvanAdiSuccess &&
                                 eMailSuccess &&
                                 universiteAdiSuccess &&
                                 uzmanlikAlaniSuccess
                             );
                }
            }
            return retVal;
        }


        public static List<CmbIntDto> GetCmbMezuniyetSurecleri(string enstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            orderby s.BaslangicTarihi descending
                            select new
                            {
                                s.MezuniyetSurecID,
                                s.BaslangicYil,
                                s.BitisYil,
                                s.SiraNo,
                                d.DonemAdi,
                                s.BaslangicTarihi,
                                s.BitisTarihi
                            }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.MezuniyetSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " " + item.SiraNo + " (" + item.BaslangicTarihi.ToFormatDate() + " - " + item.BitisTarihi.ToFormatDate() + ")") });
                }
            }
            return lst;
        }
        public static List<CmbIntDto> GetCmbMezuniyetSurecGroup(string enstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            select new
                            {
                                s.DonemID,
                                s.BaslangicYil,
                                s.BitisYil,
                                d.DonemAdi
                            }).Distinct().OrderByDescending(o => o.BaslangicYil).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = (item.BaslangicYil + "" + item.DonemID).ToInt().Value, Caption = (item.BaslangicYil + "/" + (item.BitisYil) + " " + item.DonemAdi) });
                }
            }
            return lst;
        }
        public static List<CmbStringDto> GetCmbMezuniyetKayitDonemleri(string enstituKod, int? mezuniyetSurecId = null, bool bosSecimVar = false)
        {
            var lst = new List<CmbStringDto>();
            if (bosSecimVar) lst.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var qData = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                             join bsv in db.MezuniyetBasvurularis on s.MezuniyetSurecID equals bsv.MezuniyetSurecID
                             join d in db.Donemlers on bsv.KayitOgretimYiliDonemID equals d.DonemID
                             orderby s.BaslangicTarihi descending
                             select new
                             {
                                 s.MezuniyetSurecID,
                                 bsv.KayitOgretimYiliBaslangic,
                                 bsv.KayitOgretimYiliDonemID,
                                 d.DonemAdi
                             }).AsQueryable();
                if (mezuniyetSurecId.HasValue) qData = qData.Where(p => p.MezuniyetSurecID == mezuniyetSurecId.Value);
                var dataDst = qData.Select(s => new { s.KayitOgretimYiliBaslangic, s.KayitOgretimYiliDonemID, s.DonemAdi }).Distinct().OrderByDescending(o => o.KayitOgretimYiliBaslangic).ThenBy(t => t.KayitOgretimYiliDonemID).ToList();
                foreach (var item in dataDst)
                {
                    lst.Add(new CmbStringDto { Value = item.KayitOgretimYiliBaslangic + "_" + item.KayitOgretimYiliDonemID, Caption = (item.KayitOgretimYiliBaslangic + "/" + (item.KayitOgretimYiliBaslangic + 1) + " " + item.DonemAdi) });
                }
            }
            return lst;
        }
        public static List<CmbBoolDto> GetCmbTezDili(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Türkçe" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "İngilizce" });

            return dct;
        }

        public static List<Kullanicilar> GetAktifTezKontrolSorumlulari(string enstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Kullanicilars.Where(p => p.IsAktif && p.EnstituKod == enstituKod && p.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId).OrderBy(o => o.Ad).ThenBy(t => t.Soyad).ToList();
                return data;
            }
        }
        public static List<CmbIntDto> GetCmbAktifTezKontrolSorumlulari(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            var data = GetAktifTezKontrolSorumlulari(enstituKod);
            foreach (var item in data)
            {
                dct.Add(new CmbIntDto { Value = item.KullaniciID, Caption = item.Ad + " " + item.Soyad });
            }

            return dct;

        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinDurum(bool bosSecimVar = false, bool tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif && (tumu || p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinKontrolDurumID, Caption = item.MezuniyetYayinKontrolDurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> GetCmbMzSinavDurumListe(bool bosSecimVar = false, List<int> haricSinavDurumIds = null)
        {
            var dct = new List<CmbIntDto>();
            haricSinavDurumIds = haricSinavDurumIds ?? new List<int>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetSinavDurumlaris.Where(p => !haricSinavDurumIds.Contains(p.MezuniyetSinavDurumID)).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetSinavDurumID, Caption = item.MezuniyetSinavDurumAdi });
                }
            }
            return dct;
        }
        public static List<CmbBoolDto> GetCmbTeslimFormDurumu(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Teslim Formu Oluşturuldu" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Teslim Formu Oluşturulmadı" });

            return dct;
        }
        public static List<CmbBoolDto> GetCmbMezuniyetDurum(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = null, Caption = "Sonuç Girilmedi" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Mezun Oldu" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Mezun Olamadı" });

            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetDurumId(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = -1, Caption = "" });
            dct.Add(new CmbIntDto { Value = null, Caption = "Sonuç Girilmedi" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Mezun Oldu" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Mezun Olamadı" });
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetSurecYayinTurleri(int mezuniyetSurecId, int kullaniciId, int mezuniyetBasvurulariId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kriter = MezuniyetBus.GetMezuniyetAktifOgrenimTipiYayinBilgileri(mezuniyetSurecId, kullaniciId, mezuniyetBasvurulariId);
                var mezuniyetYayinTurIDs = kriter.Where(p => p.IsGecerli).Select(s => s.MezuniyetYayinTurID).Distinct().ToList();

                var qdata = db.MezuniyetYayinTurleris.AsQueryable();
                if (mezuniyetYayinTurIDs.Count > 0) qdata = qdata.Where(p => mezuniyetYayinTurIDs.Contains(p.MezuniyetYayinTurID));
                var data = qdata.OrderBy(o => o.MezuniyetYayinTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinTurID, Caption = item.MezuniyetYayinTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinDurumListe(bool bosSecimVar = false, bool tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinKontrolDurumlaris.Where(p => (tumu || p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinKontrolDurumID, Caption = item.MezuniyetYayinKontrolDurumAdi });
                }
            }

            return dct;

        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinBelgeTurleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinBelgeTurleris.OrderBy(o => o.BelgeTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinBelgeTurID, Caption = item.BelgeTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinLinkTurleri(bool isKaynakOrYayin, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinLinkTurleris.Where(p => p.IsKaynakOrYayin == isKaynakOrYayin).OrderBy(o => o.LinkTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinLinkTurID, Caption = item.LinkTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinMetinTurleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinMetinTurleris.OrderBy(o => o.MetinTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinMetinTurID, Caption = item.MetinTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbJuriOneriFormuDurumu(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Form oluşturulmadı" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Form oluşturuldu" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Eyk'Ya Gonderimi Onaylandi" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Eyk'Ya Gonderimi Onaylanmadi" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "Eyk'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "Eyk'Da Onaylanmadı" });

            return dct;

        }
        public static List<CmbIntDto> GetCmbTezDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 2, Caption = "İşlem Bekleyenler" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Düzeltme Talep Edildi" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Onaylananlar" });

            return dct;

        }

        public static MmMessage SendMailBasvuruDanismanOnay(int mezuniyetBasvurulariId)
        {
            return MailSenderMezuniyet.SendMailBasvuruDanismanOnay(mezuniyetBasvurulariId);
        }
        public static MmMessage SendMailBasvuruDurum(int mezuniyetBasvurulariId)
        {
            return MailSenderMezuniyet.SendMailBasvuruDurum(mezuniyetBasvurulariId);
        }
        public static MmMessage SendMailJuriOneriFormuOnay(int mezuniyetJuriOneriFormId)
        {
            return MailSenderMezuniyet.SendMailJuriOneriFormuOnay(mezuniyetJuriOneriFormId);
        }
        public static MmMessage SendMailMezuniyetDegerlendirmeLink(int srTalepId, Guid? uniqueId = null, bool isLinkOrSonuc = false, bool isYeniLink = true, string eMail = "")
        {
            return MailSenderMezuniyet.SendMailMezuniyetDegerlendirmeLink(srTalepId, uniqueId, isLinkOrSonuc, isYeniLink, eMail);
        }
        public static MmMessage SendMailMezuniyetSinavYerBilgisi(int srTalepId, bool isOnaylandi)
        {
            return MailSenderMezuniyet.SendMailMezuniyetSinavYerBilgisi(srTalepId, isOnaylandi);
        }
        public static MmMessage SendMailMezuniyetSinavSonucu(int srTalepId, int mezuniyetSinavDurumId)
        {
            return MailSenderMezuniyet.SendMailMezuniyetSinavSonucu(srTalepId, mezuniyetSinavDurumId);
        }
        public static MmMessage SendMailMezuniyetTezSablonKontrol(int mezuniyetBasvurulariTezDosyaId, int sablonTipId, string aciklama = "")
        {
            return MailSenderMezuniyet.SendMailMezuniyetTezSablonKontrol(mezuniyetBasvurulariTezDosyaId, sablonTipId, aciklama);
        }
    }
}