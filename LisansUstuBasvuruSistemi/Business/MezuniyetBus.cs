using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using DevExpress.XtraReports.UI;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Raporlar.Genel;
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


        public static int? GetMezuniyetAktifSurecId(string enstituKod, int? mezuniyetSurecId = null)
        {
            using (var entities = new LubsDbEntities())
            {

                var nowDate = DateTime.Now;
                var mezuniyetSureci = entities.MezuniyetSurecis.FirstOrDefault(p => (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif &&
                    p.EnstituKod == enstituKod && p.MezuniyetSurecID == (mezuniyetSurecId ?? p.MezuniyetSurecID));
                return mezuniyetSureci?.MezuniyetSurecID;
            }
        }

        public static bool IsSurecAktif(int mezuniyetSurecId)
        {
            using (var entities = new LubsDbEntities())
            {

                var nowDate = DateTime.Now;
                return entities.MezuniyetSurecis.Any(p =>
                    (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif &&
                    p.MezuniyetSurecID == mezuniyetSurecId);

            }
        }
        public static bool IsMezuniyetBasvuruVar(int kullaniciId, string ogrenciNo)
        {
            using (var entities = new LubsDbEntities())
            {
                return entities.MezuniyetBasvurularis.Any(p => p.KullaniciID == kullaniciId && p.OgrenciNo == ogrenciNo && p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi);

            }
        }
        public static void TezDosyasiKontrolYetkilisiAta(int mezuniyetBasvurulariId)
        {
            using (var entities = new LubsDbEntities())
            {
                var basvuru = entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                if (!MezuniyetAyar.MezuniyetBasvurusunuTezSorumlusunaAta.GetAyar(basvuru.MezuniyetSureci.EnstituKod).ToBoolean(false)) return;




                //Dosya yüklendiğinde Kullanıcı atansa bile varolan kullanıcı yetki grubu ve aktiflik durumunu kontrol et eğer aktif bir tez kontrol yetkilisi var ise yeni kullanıcı atamaya izin verme
                if (basvuru.TezKontrolKullaniciID.HasValue && entities.Kullanicilars.Any(a => a.KullaniciID == basvuru.TezKontrolKullaniciID && a.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId && a.IsAktif)) return;



                var isTezDosyasiIlgiliSorumluyaAta = MezuniyetAyar.MezuniyetBasvurusunuIlgiliTezSorumlusunaAta.GetAyar(basvuru.MezuniyetSureci.EnstituKod).ToBoolean(false);

                var isTezSorumluAtamaHesaplamasiDonemselYap = MezuniyetAyar.TezSorumluAtamaHesaplamasiDonemselYap.GetAyar(basvuru.MezuniyetSureci.EnstituKod).ToBoolean(false);
                var nowDate = DateTime.Now;

                //Yüklenen tezin ait olduğu programda yetkisi olanlar öncelikli, sonrasında hiç program yetkisi olmayanlar öncelikli, sonrasında count sayısına göre öncelikli, sonrasında random atama. Program yetkisi var ve gelen programda yetkisi yoksa hiç sıralamaya dahil edilmeyecek.
                var groupToplamAtamaList =
                    (from kul in entities.Kullanicilars.Where(p =>
                            p.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId && p.IsAktif && (!p.IzinBaslamaTarihi.HasValue || p.IzinBaslamaTarihi > nowDate || p.IzinBitisTarihi < nowDate)
                            && p.EnstituKod == basvuru.MezuniyetSureci.EnstituKod)
                     join mez in entities.MezuniyetBasvurularis.Where(p =>
                             p.TezKontrolKullaniciID.HasValue &&
                             p.MezuniyetSureci.EnstituKod == basvuru.MezuniyetSureci.EnstituKod &&
                             p.MezuniyetSurecID == (isTezSorumluAtamaHesaplamasiDonemselYap ? basvuru.MezuniyetSurecID : p.MezuniyetSurecID))
                         on kul.KullaniciID equals mez.TezKontrolKullaniciID into defMez
                     from mezBas in defMez.DefaultIfEmpty()
                     group new { kul.KullaniciID, mezBas.MezuniyetSurecID, IsAtandi = mezBas != null } by new
                     {
                         kul.KullaniciID,
                         kul.KullaniciAdi,
                         ProgramIcinYetkiliInx = isTezDosyasiIlgiliSorumluyaAta
                             ? (kul.KullaniciProgramlaris.Any(a => a.ProgramKod == basvuru.ProgramKod)
                                 ? 0
                                 : (!kul.KullaniciProgramlaris.Any()
                                     ? 1
                                     : -1)
                               )
                               : 1

                     }
            into g1
                     select new
                     {
                         g1.Key.KullaniciID,
                         g1.Key.KullaniciAdi,
                         g1.Key.ProgramIcinYetkiliInx,
                         ToplamAtamaCount = g1.Count(c => c.IsAtandi)
                     }).Where(p => p.ProgramIcinYetkiliInx >= 0)
                    .OrderBy(o => o.ProgramIcinYetkiliInx)
                    .ThenBy(o => o.ToplamAtamaCount)
                    .ThenBy(t => Guid.NewGuid()).ToList();

                var enAzAtanan = groupToplamAtamaList.FirstOrDefault();


                if (enAzAtanan == null) return;
                basvuru.TezKontrolKullaniciID = enAzAtanan.KullaniciID;


                entities.SaveChanges();


            }
        }

        public static MmMessage MezuniyetBasvurusuSilKontrol(int mezuniyetBasvurulariId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var entities = new LubsDbEntities())
            {
                var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
                var basvuru =
                    entities.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
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

            using (var entities = new LubsDbEntities())
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
                        //if (UserIdentity.Current.IsAdmin == false && !IsSurecAktif(basvuru.MezuniyetSurecID))
                        //{
                        //    mMessage.Messages.Add(
                        //        "Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");
                        //    return mMessage;
                        //}

                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.MezuniyetSureci.EnstituKod))
                        {
                            mMessage.Messages.Add("Başvurunun ait olduğu enstitü için yetkiniz bulunmamaktadır.");
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

                    //if (!IsSurecAktif(basvuru.MezuniyetSurecID))
                    //{
                    //    mMessage.Messages.Add(
                    //        "Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");
                    //    return mMessage;
                    //}

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
                    var ogrenimTipAdi = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == kul.OgrenimTipKod)
                        .OgrenimTipAdi;
                    mMessage.Messages.Add(ogrenimTipAdi +
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
                    var surec = basvuruKriterleri.MezuniyetSureci;
                    var donemId = "";
                    if (!surec.DonemKontrolObsDonemId.IsNullOrWhiteSpace())
                    {
                        donemId = surec.DonemKontrolObsDonemId;
                    }
                    var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo, donemId);

                    var subMessages = new List<string>();
                    var okuduguDonemNo = donemId.IsNullOrWhiteSpace()
                        ? ogrenciBilgi.OkuduguDonemNo
                        : ogrenciBilgi.HesaplananOkuduguDonemNo;
                    if (okuduguDonemNo > basvuruKriterleri.AktifDonemMaxKriteri)
                    {
                        subMessages.Add("Aktif okuma dönemi " + basvuruKriterleri.AktifDonemMaxKriteri + ". dönemden daha büyük olanlar Mezuniyet başvurusu yapamazlar.");
                    }
                    donemId = "";
                    if (!surec.DersKontrolObsDonemId.IsNullOrWhiteSpace())
                    {
                        donemId = surec.DersKontrolObsDonemId;
                    }
                    ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo, donemId);
                    var basvuruSonDonemSecilecekDersKodlari = basvuruKriterleri
                        .AktifDonemDersKodKriteri.Split(',')
                        .Where(p => !p.IsNullOrWhiteSpace()).ToList();

                    if (basvuruSonDonemSecilecekDersKodlari.Any() && ogrenciBilgi.AktifDonemDers.DersKodNums.Count(p => basvuruSonDonemSecilecekDersKodlari.Any(a => a == p)) != basvuruSonDonemSecilecekDersKodlari.Count)
                    {
                        subMessages.Add(string.Join(", ", basvuruSonDonemSecilecekDersKodlari) + " kodlu derslere son dönemde kayıt yaptırmanız gerekmektedi.");
                    }

                    if (ogrenciBilgi.AktifDonemDers.ToplamKredi < basvuruKriterleri.AktifDonemToplamKrediKriteri)
                    {
                        subMessages.Add("Toplam Kredi sayınız " + basvuruKriterleri.AktifDonemToplamKrediKriteri + " krediden büyük ya da eşit olmalıdır. Mevcut Kredi: " + ogrenciBilgi.AktifDonemDers.ToplamKredi);

                    }
                    if (!basvuruKriterleri.AktifDonemEtikNotKriteri.IsNullOrWhiteSpace() && !HarfNotuHelper.IsHarfNotuBuyukEsit(basvuruKriterleri.AktifDonemEtikNotKriteri, ogrenciBilgi.AktifDonemDers.EtikDersNotu.ToLastNot()))
                    {
                        subMessages.Add("Etik dersi için ders notunuzun " + basvuruKriterleri.AktifDonemEtikNotKriteri + " veya daha üstü bir not olması gerekmektedir.");
                    }

                    if (!basvuruKriterleri.AktifDonemSeminerNotKriteri.IsNullOrWhiteSpace() && !HarfNotuHelper.IsHarfNotuBuyukEsit(basvuruKriterleri.AktifDonemSeminerNotKriteri, ogrenciBilgi.AktifDonemDers.SeminerDersNotu))
                    {
                        subMessages.Add("Seminer dersi için ders notunuzun " + basvuruKriterleri.AktifDonemSeminerNotKriteri + " veya daha üstü bir not olması gerekmektedir.");
                    }

                    if (ogrenciBilgi.AktifDonemDers.Agno < basvuruKriterleri.AktifDonemAgnoKriteri)
                    {
                        subMessages.Add("Ortalamanız " + basvuruKriterleri.AktifDonemAgnoKriteri + " ortalamasından büyük ya da eşit olmalıdır. Mevcut Ortalama: " + ogrenciBilgi.AktifDonemDers.Agno.ToString("n2"));

                    }

                    if (ogrenciBilgi.AktifDonemDers.ToplamAkts < basvuruKriterleri.AktifDonemAktsKriteri)
                    {
                        subMessages.Add("Akts toplamınız " + basvuruKriterleri.AktifDonemAktsKriteri + " akts'den büyük ya da eşit olmalıdır. Mevcut Akts: " + ogrenciBilgi.AktifDonemDers.ToplamAkts);

                    }

                    if (kul.OgrenimTipKod == OgrenimTipi.Doktra || kul.OgrenimTipKod == OgrenimTipi.ButunlesikDoktora)
                    {

                        var ogrenciTezIzlemeBasariliSayi =
                            ogrenciBilgi.OgrenciTez.tezizlemebilgileri.Count(a => a.TEZ_IZL_DURUM == "Başarılı");
                        if (ogrenciTezIzlemeBasariliSayi < 3)
                        {
                            subMessages.Add(
                                "En az 3 başarılı tez izleme raporunuzun bulunması gerekmektedir. Mevcut başarılı tez izleme rapor sayınız :" +
                                ogrenciTezIzlemeBasariliSayi);

                        }
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
            using (var entities = new LubsDbEntities())
            {

                var basvuru = entities.MezuniyetBasvurularis.Include("MezuniyetYayinKontrolDurumlari").First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                var kul = entities.Kullanicilars.First(p => p.KullaniciID == basvuru.KullaniciID);

                #region BasvuruBilgi
                model.EnstituKod = basvuru.MezuniyetSureci.EnstituKod;
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
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
                model.OgrenimTipAdi = entities.OgrenimTipleris.First(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;
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

                var yayins = (from qs in entities.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId)
                              join s in entities.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in entities.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join yb in entities.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in entities.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in entities.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in entities.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                              from klD in defkl.DefaultIfEmpty()
                              join inx in entities.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                                  //join kullanici in entities.Kullanicilars on qs.IslemYapanID equals kullanici.KullaniciID 
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
                                  TarihAraligi = qs.TarihAraligi,
                                  //IslemYapan = kullanici.Ad+" "+kullanici.Soyad,
                                  //IslemTarihi = qs.IslemTarihi

                              }).ToList();

                model.MezuniyetBasvuruYayinlari = yayins;
                if (basvuru.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi) model.Onaylandi = true;

            }
            return model;

        }

        public static MezuniyetBasvuruDetayDto GetMezuniyetBasvuruDetayBilgi(int mezuniyetBasvurulariId, int? mezuniyetBasvurulariYayinId = null, int? showDetayYayinId = null)
        {
            var model = new MezuniyetBasvuruDetayDto();
            using (var entities = new LubsDbEntities())
            {
                var basvuru = entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

                var bsurec = basvuru.MezuniyetSureci;
                var bSurecOtKriter = bsurec.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == basvuru.OgrenimTipKod);
                var enstitu = entities.Enstitulers.First(p => p.EnstituKod == bsurec.EnstituKod);

                var eslesenDanisman = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0));
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
                    var tezAtananKullanici = entities.Kullanicilars.FirstOrDefault(f => model.TezKontrolKullaniciID.HasValue && f.KullaniciID == model.TezKontrolKullaniciID);
                    model.TezKontrolYetkiliUserKey = tezAtananKullanici.UserKey;
                    model.TezKontrolYetkilisiAdSoyad = tezAtananKullanici.Ad + " " + tezAtananKullanici.Soyad;
                }

                if (!model.IsDanismanOnay.HasValue)
                {
                    var ogrenci = basvuru.Kullanicilar;

                    if (ogrenci.DanismanID.HasValue && basvuru.TezDanismanID != ogrenci.DanismanID)
                    {
                        var aktifDanisman = entities.Kullanicilars.First(f => f.KullaniciID == ogrenci.DanismanID);
                        model.GuncellenebilirBasvuruDanismanAdi = aktifDanisman.Unvanlar.UnvanAdi + " " + aktifDanisman.Ad + " " + aktifDanisman.Soyad;
                    }
                }

                model.MezuniyetBasvurulariTezDosyalariDtos = basvuru.MezuniyetBasvurulariTezDosyalaris.Select(s => new MezuniyetBasvurulariTezDosyalariDto
                {
                    MezuniyetBasvurulariTezDosyaID = s.MezuniyetBasvurulariTezDosyaID,
                    RowID = s.RowID,
                    SiraNo = s.SiraNo,
                    MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                    IsLatexOrWordSablonu = s.IsLatexOrWordSablonu,
                    TezDosyaAdi = s.TezDosyaAdi,
                    TezDosyaYolu = s.TezDosyaYolu,
                    IsOnaylandiOrDuzeltme = s.IsOnaylandiOrDuzeltme,
                    Aciklama = s.Aciklama,
                    YuklemeTarihi = s.YuklemeTarihi,
                    IsOnayTaahhutuVerildi = s.IsOnayTaahhutuVerildi,
                    OnayTarihi = s.OnayTarihi,
                    OnayYapanID = s.OnayYapanID,
                }).OrderByDescending(o => o.YuklemeTarihi).ToList();
                var onayYapanIDs = model.MezuniyetBasvurulariTezDosyalariDtos.Where(p => p.OnayYapanID.HasValue).Select(s => s.OnayYapanID).ToList();
                var tezDosyasiOnayYapanYetkililer = entities.Kullanicilars.Where(p => onayYapanIDs.Contains(p.KullaniciID)).ToList();
                var siraNo = model.MezuniyetBasvurulariTezDosyalariDtos.Count;
                foreach (var item in model.MezuniyetBasvurulariTezDosyalariDtos)
                {
                    if (item.OnayYapanID.HasValue)
                    {
                        var kul = tezDosyasiOnayYapanYetkililer.First(p => p.KullaniciID == item.OnayYapanID);
                        item.UserKey = kul.UserKey;
                        item.OnayYapanTezKontrolYetkiliAdSoyad = kul.Ad + " " + kul.Soyad;
                    }
                    item.SiraNo = siraNo;

                    siraNo--;

                }
                model.TezDanismanID = basvuru.TezDanismanID;
                model.IsAnketVar = bsurec.AnketID.HasValue;
                model.IsAnketDolduruldu = basvuru.AnketCevaplaris.Any();
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumOnayYapanKullaniciID = basvuru.MezuniyetYayinKontrolDurumOnayYapanKullaniciID;
                model.MezuniyetYayinKontrolDurumOnayTarihi = basvuru.MezuniyetYayinKontrolDurumOnayTarihi;
                if (basvuru.MezuniyetYayinKontrolDurumOnayYapanKullaniciID.HasValue)
                { 
                    var yayinKontrolYapanKullanici = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == basvuru.MezuniyetYayinKontrolDurumOnayYapanKullaniciID);
                    
                    model.MezuniyetYayinKontrolDurumOnayYapanKullaniciAdi = yayinKontrolYapanKullanici.Unvanlar?.UnvanAdi+" "+ yayinKontrolYapanKullanici.Ad + " " + yayinKontrolYapanKullanici.Soyad;
                }
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.KullaniciTipID = basvuru.KullaniciTipID;
                model.ResimAdi = basvuru.ResimAdi;
                model.Ad = basvuru.Ad;
                model.Soyad = basvuru.Soyad;
                model.TcKimlikNo = basvuru.TcKimlikNo;
                model.UyrukKod = basvuru.UyrukKod;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = entities.OgrenimTipleris.First(p => p.EnstituKod == bsurec.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;
                model.AnabilimdaliID = basvuru.Programlar.AnabilimDaliID;
                model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.IsTezDiliTr = basvuru.IsTezDiliTr;
                model.TezBaslikTr = basvuru.TezBaslikTr;
                model.TezBaslikEn = basvuru.TezBaslikEn;
                model.IsTekKaynakOraniGirisiYapilacak = bSurecOtKriter.TekKaynakOrani.HasValue;
                model.IsToplamKaynakOraniGirisiYapilacak = bSurecOtKriter.ToplamKaynakOrani.HasValue;
                model.TekKaynakOrani = basvuru.TekKaynakOrani;
                model.ToplamKaynakOrani = basvuru.ToplamKaynakOrani;
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
                model.EnstituKod=enstitu.EnstituKod;
                model.EnstituAdi = enstitu.EnstituAd;
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSureci = basvuru.MezuniyetSureci;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.YayinKontrolKabulTaahhutEdildi = basvuru.YayinKontrolKabulTaahhutEdildi;
                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;
                var nowDate = DateTime.Now;
                model.BasvuruSureciTarihi = bsurec.BaslangicYil + "/" + bsurec.BitisYil + " " + entities.Donemlers.First(p => p.DonemID == bsurec.DonemID).DonemAdi + " (" + bsurec.BaslangicTarihi.ToFormatDate() + "-" + bsurec.BitisTarihi.ToFormatDate() + ")";
                model.SonucGirisSureciAktif = bsurec.BaslangicTarihi <= nowDate && bsurec.BitisTarihi >= nowDate;
                model.IsMezunOldu = basvuru.IsMezunOldu;
                model.SonTekKaynakOrani = basvuru.SonTekKaynakOrani;
                model.SonToplamKaynakOrani = basvuru.SonToplamKaynakOrani;
                model.MezuniyetTarihi = basvuru.MezuniyetTarihi;
                model.EYKTarihi = basvuru.EYKTarihi;
                model.EYKSayisi = basvuru.EYKSayisi;
                model.TezTeslimSonTarih = basvuru.TezTeslimSonTarih;
                model.CiltliTezTeslimUzatmaTalebi = basvuru.CiltliTezTeslimUzatmaTalebi;
                model.CiltliTezTeslimUzatmaTalebiTarih = basvuru.CiltliTezTeslimUzatmaTalebiTarih;
                model.CiltliTezTeslimUzatmaTalebiDanismanOnay = basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnay;
                model.CiltliTezTeslimUzatmaTalebiDanismanOnayTarih = basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnayTarih;
                model.CiltliTezTeslimUzatmaTalebiDanismanOnayAciklama = basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnayAciklama;
                model.CiltliTezTeslimUzatmaTalebiEykDaOnay = basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnay;
                model.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi = basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi;
                model.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi = basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi;
                model.CiltliTezTeslimUzatmaTalebiEykDaOnayTarih = basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnayTarih;
                model.CiltliTezTeslimUzatmaTalebiEykDaOnayAciklama = basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnayAciklama;

                model.MezuniyetJuriOneriFormlaris = entities.MezuniyetJuriOneriFormlaris.Include("MezuniyetJuriOneriFormuJurileris").Where(p => p.MezuniyetBasvurulariID == basvuru.MezuniyetBasvurulariID).ToList();
                model.MezuniyetBasvurulariTezTeslimFormlaris = basvuru.MezuniyetBasvurulariTezTeslimFormlaris;

                model.EykYaGonderildi = model.MezuniyetJuriOneriFormlaris.Select(s => s.EYKYaGonderildi).FirstOrDefault();
                model.EykDaOnaylandi = model.MezuniyetJuriOneriFormlaris.Select(s => s.EYKDaOnaylandi).FirstOrDefault();
                var yayins = (from mezuniyetBasvurulariYayin in entities.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId)
                              join mezuniyetSureciYayinTur in entities.MezuniyetSureciYayinTurleris on new { mezuniyetBasvurulariYayin.MezuniyetBasvurulari.MezuniyetSurecID, mezuniyetBasvurulariYayin.MezuniyetYayinTurID } equals new { mezuniyetSureciYayinTur.MezuniyetSurecID, mezuniyetSureciYayinTur.MezuniyetYayinTurID }
                              join mezuniyetYayinTur in entities.MezuniyetYayinTurleris on new { mezuniyetSureciYayinTur.MezuniyetYayinTurID } equals new { mezuniyetYayinTur.MezuniyetYayinTurID }
                              join mezuniyetYayinBelgeTur in entities.MezuniyetYayinBelgeTurleris on new { mezuniyetSureciYayinTur.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)mezuniyetYayinBelgeTur.MezuniyetYayinBelgeTurID } into defMezuniyetYayinBelgeTur
                              from mezuniyetYayinBelgeTurDefItem in defMezuniyetYayinBelgeTur.DefaultIfEmpty()
                              join mezuniyetYayinLinkTur in entities.MezuniyetYayinLinkTurleris on new { mezuniyetSureciYayinTur.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)mezuniyetYayinLinkTur.MezuniyetYayinLinkTurID } into defMezuniyetYayinLinkTur
                              from mezuniyetYayinLinkTurDefItem in defMezuniyetYayinLinkTur.DefaultIfEmpty()
                              join mezuniyetYayinMetinTur in entities.MezuniyetYayinMetinTurleris on new { mezuniyetSureciYayinTur.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)mezuniyetYayinMetinTur.MezuniyetYayinMetinTurID } into defMezuniyetYayinMetinTur
                              from mezuniyetYayinMetinTurDefItem in defMezuniyetYayinMetinTur.DefaultIfEmpty()
                              join mezuniyetYayinLinkTurKaynak in entities.MezuniyetYayinLinkTurleris on new { mezuniyetSureciYayinTur.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)mezuniyetYayinLinkTurKaynak.MezuniyetYayinLinkTurID } into defMezuniyetYayinLinkTurKaynak
                              from mezuniyetYayinLinkTurKaynakrDefItem in defMezuniyetYayinLinkTurKaynak.DefaultIfEmpty()
                              join mezuniyetYayinIndexTur in entities.MezuniyetYayinIndexTurleris on new { mezuniyetBasvurulariYayin.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)mezuniyetYayinIndexTur.MezuniyetYayinIndexTurID } into defMezuniyetYayinIndexTur
                              from mezuniyetYayinIndexTurDefItem in defMezuniyetYayinIndexTur.DefaultIfEmpty()
                                  // join kullanici in entities.Kullanicilars on mezuniyetBasvurulariYayin.IslemYapanID equals kullanici.KullaniciID
                              select new MezuniyetBasvurulariYayinDto
                              {
                                  MezuniyetYayinTurID = mezuniyetBasvurulariYayin.MezuniyetYayinTurID,
                                  ShowDetayYayinID = showDetayYayinId,
                                  MezuniyetBasvurulariYayinID = mezuniyetBasvurulariYayin.MezuniyetBasvurulariYayinID,
                                  MezuniyetBasvurulariID = mezuniyetBasvurulariYayin.MezuniyetBasvurulariID,
                                  DanismanIsmiVar = mezuniyetBasvurulariYayin.DanismanIsmiVar,
                                  TezIcerikUyumuVar = mezuniyetBasvurulariYayin.TezIcerikUyumuVar,
                                  Onaylandi = mezuniyetBasvurulariYayin.Onaylandi,
                                  RetAciklamasi = mezuniyetBasvurulariYayin.RetAciklamasi,
                                  YayinBasligi = mezuniyetBasvurulariYayin.YayinBasligi,
                                  Yayinlanmis = mezuniyetBasvurulariYayin.Yayinlanmis,
                                  MezuniyetYayinTarih = mezuniyetBasvurulariYayin.MezuniyetYayinTarih,
                                  MezuniyetYayinTarihZorunlu = mezuniyetSureciYayinTur.TarihIstensin,
                                  MezuniyetYayinTurAdi = mezuniyetYayinTur.MezuniyetYayinTurAdi,
                                  MezuniyetYayinBelgeTurID = mezuniyetSureciYayinTur.MezuniyetYayinBelgeTurID,
                                  MezuniyetYayinBelgeTurAdi = mezuniyetYayinBelgeTurDefItem != null ? mezuniyetYayinBelgeTurDefItem.BelgeTurAdi : "",
                                  MezuniyetYayinBelgeAdi = mezuniyetYayinBelgeTurDefItem != null ? mezuniyetBasvurulariYayin.MezuniyetYayinBelgeAdi : "",
                                  MezuniyetYayinBelgeDosyaYolu = mezuniyetYayinBelgeTurDefItem != null ? mezuniyetBasvurulariYayin.MezuniyetYayinBelgeDosyaYolu : "",
                                  MezuniyetYayinBelgeTurZorunlu = mezuniyetSureciYayinTur.BelgeZorunlu,
                                  MezuniyetYayinKaynakLinkTurID = mezuniyetSureciYayinTur.KaynakMezuniyetYayinLinkTurID,
                                  MezuniyetYayinKaynakLinkTurAdi = mezuniyetYayinLinkTurDefItem != null ? mezuniyetYayinLinkTurDefItem.LinkTurAdi : "",
                                  MezuniyetYayinKaynakLinkIsUrl = mezuniyetYayinLinkTurDefItem != null && mezuniyetYayinLinkTurDefItem.IsUrl,
                                  MezuniyetYayinKaynakLinkTurZorunlu = mezuniyetSureciYayinTur.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = mezuniyetSureciYayinTur.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = mezuniyetYayinMetinTurDefItem != null ? mezuniyetYayinMetinTurDefItem.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = mezuniyetYayinMetinTurDefItem != null ? mezuniyetBasvurulariYayin.MezuniyetYayinMetniBelgeAdi : "",
                                  MezuniyetYayinMetniBelgeYolu = mezuniyetBasvurulariYayin.MezuniyetYayinMetniBelgeYolu,
                                  MezuniyetYayinMetinZorunlu = mezuniyetSureciYayinTur.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = mezuniyetSureciYayinTur.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = mezuniyetYayinLinkTurKaynakrDefItem != null ? mezuniyetYayinLinkTurKaynakrDefItem.LinkTurAdi : "",
                                  MezuniyetYayinLinkIsUrl = mezuniyetYayinLinkTurKaynakrDefItem != null && mezuniyetYayinLinkTurKaynakrDefItem.IsUrl,
                                  MezuniyetYayinLinkiZorunlu = mezuniyetSureciYayinTur.YayinLinkiZorunlu,
                                  MezuniyetYayinKaynakLinki = mezuniyetBasvurulariYayin.MezuniyetYayinKaynakLinki,
                                  MezuniyetYayinLinki = mezuniyetBasvurulariYayin.MezuniyetYayinLinki,
                                  MezuniyetYayinIndexTurZorunlu = mezuniyetSureciYayinTur.YayinIndexTurIstensin,
                                  MezuniyetYayinIndexTurAdi = mezuniyetYayinIndexTurDefItem != null ? mezuniyetYayinIndexTurDefItem.IndexTurAdi : "",
                                  MezuniyetYayinIndexTurID = mezuniyetBasvurulariYayin.MezuniyetYayinIndexTurID,
                                  YayinIndexTurleri = entities.MezuniyetYayinIndexTurleris.ToList(),
                                  MezuniyetKabulEdilmisMakaleZorunlu = mezuniyetSureciYayinTur.YayinKabulEdilmisMakaleIstensin,
                                  MezuniyetYayinKabulEdilmisMakaleAdi = mezuniyetBasvurulariYayin.MezuniyetYayinKabulEdilmisMakaleAdi,
                                  MezuniyetYayinKabulEdilmisMakaleDosyaYolu = mezuniyetBasvurulariYayin.MezuniyetYayinKabulEdilmisMakaleDosyaYolu,
                                  YayinDeatKurulusIstensin = mezuniyetSureciYayinTur.YayinDeatKurulusIstensin,
                                  ProjeDeatKurulus = mezuniyetBasvurulariYayin.ProjeDeatKurulus,
                                  YayinDergiAdiIstensin = mezuniyetSureciYayinTur.YayinDergiAdiIstensin,
                                  DergiAdi = mezuniyetBasvurulariYayin.DergiAdi,
                                  YayinMevcutDurumIstensin = mezuniyetSureciYayinTur.YayinMevcutDurumIstensin,
                                  IsProjeTamamlandiOrDevamEdiyor = mezuniyetBasvurulariYayin.IsProjeTamamlandiOrDevamEdiyor,
                                  YayinProjeEkibiIstensin = mezuniyetSureciYayinTur.YayinProjeEkibiIstensin,
                                  ProjeEkibi = mezuniyetBasvurulariYayin.ProjeEkibi,
                                  YayinProjeTurIstensin = mezuniyetSureciYayinTur.YayinProjeTurIstensin,
                                  MezuniyetYayinProjeTurID = mezuniyetBasvurulariYayin.MezuniyetYayinProjeTurID,
                                  ProjeTurAdi = mezuniyetBasvurulariYayin.MezuniyetYayinProjeTurID.HasValue ? mezuniyetBasvurulariYayin.MezuniyetYayinProjeTurleri.ProjeTurAdi : "",
                                  YayinYazarlarIstensin = mezuniyetSureciYayinTur.YayinYazarlarIstensin,
                                  YazarAdi = mezuniyetBasvurulariYayin.YazarAdi,
                                  YayinYilCiltSayiIstensin = mezuniyetSureciYayinTur.YayinYilCiltSayiIstensin,
                                  YilCiltSayiSS = mezuniyetBasvurulariYayin.YilCiltSayiSS,
                                  IsTarihAraligiIstensin = mezuniyetSureciYayinTur.IsTarihAraligiIstensin,
                                  TarihAraligi = mezuniyetBasvurulariYayin.TarihAraligi,
                                  YayinYerBilgisiIstensin = mezuniyetSureciYayinTur.YayinYerBilgisiIstensin,
                                  YerBilgisi = mezuniyetBasvurulariYayin.YerBilgisi,
                                  YayinEtkinlikAdiIstensin = mezuniyetSureciYayinTur.YayinEtkinlikAdiIstensin,
                                  EtkinlikAdi = mezuniyetBasvurulariYayin.EtkinlikAdi,
                                  //IslemYapanID = mezuniyetBasvurulariYayin.IslemYapanID,
                                  //IslemYapan = kullanici.Ad + " " + kullanici.Soyad,
                                  //IslemTarihi = mezuniyetBasvurulariYayin.IslemTarihi


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
                model.MezuniyetSrModel.SalonRezervasyonlari = (from s in entities.SRTalepleris
                                                               join tt in entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                                                               join mb in entities.MezuniyetBasvurularis on s.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                                                               join sal in entities.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                                                               from defSl in def1.DefaultIfEmpty()
                                                               join hg in entities.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                                                               join d in entities.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                                                               join sd in entities.MezuniyetSinavDurumlaris on (s.MezuniyetSinavDurumID ?? MezuniyetSinavDurumEnum.SonucGirilmedi) equals sd.MezuniyetSinavDurumID into def2
                                                               from defSd in def2.DefaultIfEmpty()
                                                               join sdj in entities.MezuniyetSinavDurumlaris on (s.JuriSonucMezuniyetSinavDurumID ?? MezuniyetSinavDurumEnum.SonucGirilmedi) equals sdj.MezuniyetSinavDurumID into def3
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
                                                                                                          s.UzatmaSonrasiOgrenciTaahhutSonTarih ?? (DbFunctions.AddMonths(s.Tarih, bSurecOtKriter.SinavUzatmaOgrenciTaahhutMaxAy).Value)
                                                                                                          : DateTime.Now,

                                                                   UzatmaSonrasiYeniSinavTalebiSonTarih = s.UzatmaSonrasiYeniSinavTalebiSonTarih,
                                                                   UzatmaIlkSrTarih = s.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma && s.IsOgrenciUzatmaSonrasiOnay == true ?
                                                                       s.UzatmaSonrasiYeniSinavTalebiSonTarih ?? DbFunctions.AddDays(s.OgrenciOnayTarihi, bSurecOtKriter.SinavKacGunSonraAlabilir).Value
                                                                       : (DateTime?)null,
                                                                   UzatmaSonSrTarih = s.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ?
                                                                                                s.UzatmaSonrasiYeniSinavTalebiSonTarih ?? DbFunctions.AddMonths(s.Tarih, bSurecOtKriter.SinavUzatmaSinavAlmaSuresiMaxAy).Value
                                                                                                : DateTime.Now,
                                                                   TezTeslimSonTarih =
                                                                       model.TezTeslimSonTarih ??
                                                                            (mb.CiltliTezTeslimUzatmaTalebi == true && mb.CiltliTezTeslimUzatmaTalebiEykDaOnay == true
                                                                                ? DbFunctions.AddMonths(s.Tarih, bSurecOtKriter.TezTeslimSuresiAy + 1).Value
                                                                                : DbFunctions.AddMonths(s.Tarih, bSurecOtKriter.TezTeslimSuresiAy).Value),

                                                                   IsOgrenciUzatmaSonrasiOnay = s.IsOgrenciUzatmaSonrasiOnay,
                                                                   OgrenciOnayTarihi = s.OgrenciOnayTarihi,
                                                                   IsDanismanUzatmaSonrasiOnay = s.IsDanismanUzatmaSonrasiOnay,
                                                                   DanismanOnayTarihi = s.DanismanOnayTarihi,
                                                                   DanismanUzatmaSonrasiOnayAciklama = s.DanismanUzatmaSonrasiOnayAciklama,
                                                                   IsYokDrBursiyeriVar = s.IsYokDrBursiyeriVar,
                                                                   YokDrOncelikliAlan = s.YokDrOncelikliAlan,
                                                                   DavetResimYolu = s.DavetResimYolu,
                                                                   DavetResmiGostermeDurum = s.DavetResmiGostermeDurum

                                                               }).OrderByDescending(o => o.SRTalepID).ToList();




                foreach (var item in model.MezuniyetSrModel.SalonRezervasyonlari)
                {

                    var birOncekiSrTalepUzatma = model.MezuniyetSrModel.SalonRezervasyonlari
                        .Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma &&
                                    p.SRTalepID < item.SRTalepID).OrderByDescending(o => o.SRTalepID).Select(su =>
                            new { su.IsTezBasligiDegisti, su.YeniTezBaslikTr, su.YeniTezBaslikEn }).FirstOrDefault();
                    if (birOncekiSrTalepUzatma != null && birOncekiSrTalepUzatma.IsTezBasligiDegisti == true)
                    {
                        item.TezBaslikTr = birOncekiSrTalepUzatma.YeniTezBaslikTr;
                        item.TezBaslikEn = birOncekiSrTalepUzatma.YeniTezBaslikEn;
                    }

                    var sinavDurumIds = new List<int> { item.JuriSonucMezuniyetSinavDurumID ?? 0 };
                    item.SrDurumSelectList.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe(), "Value", "Caption", item.SRDurumID);
                    item.SrDurumSelectList.MezuniyetSinavDurumID = new SelectList(MezuniyetBus.GetCmbMzSinavDurumEnstituOnayListe(false, sinavDurumIds), "Value", "Caption", item.MezuniyetSinavDurumID);

                }
                model.MezuniyetDurumSelectList.IsMezunOldu = new SelectList(MezuniyetBus.GetCmbMezuniyetDurum(), "Value", "Caption", model.IsMezunOldu);
                model.MezuniyetSrModel.EykIlkSrMaxTarih = model.EYKTarihi.HasValue ? (model.TezTeslimSonTarih ?? model.EYKTarihi.Value.AddMonths(bSurecOtKriter.TezTeslimSuresiAy)) : (DateTime?)null;
                model.MezuniyetSrModel.IsSrEykSureAsimi = model.EYKTarihi.HasValue && model.MezuniyetSrModel.EykIlkSrMaxTarih < DateTime.Now.Date;


                #endregion
                var sonBasariliRez = model.MezuniyetSrModel.SalonRezervasyonlari.FirstOrDefault(f => f.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);
                if (sonBasariliRez != null)
                {

                    model.DefaultTezTeslimSonTarih = sonBasariliRez.Tarih.Date.AddMonths(bSurecOtKriter.TezTeslimSuresiAy);
                    model.DefaultMaxTezTeslimSonTarih = sonBasariliRez.Tarih.Date.AddMonths(bSurecOtKriter.TezTeslimSuresiAy + 1);
                    model.SelectedTezTeslimSonTarih = model.TezTeslimSonTarih.HasValue
                        ? model.TezTeslimSonTarih
                        : model.DefaultTezTeslimSonTarih;
                    model.TezTeslimSonTarih = sonBasariliRez.TezTeslimSonTarih;

                    if (model.CiltliTezTeslimUzatmaTalebi == true && model.CiltliTezTeslimUzatmaTalebiEykDaOnay == true)
                        model.AktifTezTeslimSonTarih = model.TezTeslimSonTarih > model.DefaultMaxTezTeslimSonTarih
                            ? model.TezTeslimSonTarih
                            : model.DefaultMaxTezTeslimSonTarih;
                    else
                    {
                        model.AktifTezTeslimSonTarih = model.TezTeslimSonTarih > model.DefaultTezTeslimSonTarih
                            ? model.TezTeslimSonTarih
                            : model.DefaultTezTeslimSonTarih;
                    }

                }

                if (model.IsAnketDolduruldu == false)
                {
                    if (bsurec.AnketID.HasValue)
                    {
                        bool anketCevabiVarMi = entities.AnketCevaplaris
                            .Any(a => a.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

                        if (!anketCevabiVarMi)
                        {
                            model.AnketView = AnketlerBus.GetAnketView(
                                anketId: bsurec.AnketID.Value,
                                anketTipId: AnketTipiEnum.MezuniyetSureciDegerlendirmeAnketi,
                                mezuniyetBasvurulariId: mezuniyetBasvurulariId,
                                rowId: basvuru.RowID.ToString()
                            );
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
            var esDanismanVerileri = new List<string> { kModel.TezEsDanismanAdi, kModel.TezEsDanismanUnvani, kModel.TezEsDanismanEMail };
            if (esDanismanVerileri.Any(a => !a.IsNullOrWhiteSpace()))
            {
                if (esDanismanVerileri.Any(a => a.IsNullOrWhiteSpace()))
                {

                    mmMessage.Messages.Add("Tez İkinci Danışman bilgisi girilecekse eğer İkinci Danışman Unvanı, Ad Soyad bilgisi ve Email bilgisinin tümü girilmelidir. Eğer girilmeyecekse bu bilgileri boş bırakınız.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezEsDanismanUnvani" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezEsDanismanAdi" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezEsDanismanEMail" });

                }
                else
                {
                    if (!kModel.TezEsDanismanEMail.ToIsValidEmail())
                    {
                        mmMessage.Messages.Add("Lütfen İkinci Danışman E-Posta Adresi bilgisini Uygun Formatta Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezEsDanismanEMail" });

                    }
                    else
                    {
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezEsDanismanAdi" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezEsDanismanUnvani" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezEsDanismanEMail" });
                    }
                }
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
            using (var entities = new LubsDbEntities())
            {
                decimal baslangic;
                int ogrenimTipKod;
                if (mezuniyetBasvurulariId > 0)
                {
                    var mBasvuru = entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    ogrenimTipKod = mBasvuru.OgrenimTipKod;
                    baslangic = Convert.ToDecimal(mBasvuru.KayitOgretimYiliBaslangic + "," + mBasvuru.KayitOgretimYiliDonemID.Value);
                }
                else
                {
                    var kul = entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                    baslangic = Convert.ToDecimal(kul.KayitYilBaslangic + "," + kul.KayitDonemID.Value);
                    ogrenimTipKod = kul.OgrenimTipKod ?? 0;
                }
                var kriter = entities.MezuniyetSureciYonetmelikleris.Include("MezuniyetSureciYonetmelikleriOTs").Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList().First(f =>
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
                kriter.MezuniyetSureciYonetmelikleriOTs = kriter.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == ogrenimTipKod)
                    .ToList();
                return kriter;
            }
        }
        public static List<MezuniyetSureciYonetmelikleriOT> GetMezuniyetAktifOgrenimTipiYayinBilgileri(int mezuniyetSurecId, int kullaniciId, int mezuniyetBasvurulariId)
        {
            using (var entities = new LubsDbEntities())
            {
                decimal baslangic;
                int ogrenimTipKod;
                if (mezuniyetBasvurulariId > 0)
                {
                    var mBasvuru = entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    baslangic = Convert.ToDecimal(mBasvuru.KayitOgretimYiliBaslangic + "," + mBasvuru.KayitOgretimYiliDonemID.Value);
                    ogrenimTipKod = mBasvuru.OgrenimTipKod;
                }
                else
                {
                    var kul = entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);



                    baslangic = Convert.ToDecimal(kul.KayitYilBaslangic + "," + kul.KayitDonemID.Value);
                    ogrenimTipKod = kul.OgrenimTipKod.Value;
                }
                var kriter = entities.MezuniyetSureciYonetmelikleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList().FirstOrDefault(f =>
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
                if (kriter == null) return new List<MezuniyetSureciYonetmelikleriOT>();
                var ots = kriter.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == ogrenimTipKod).ToList();
                return ots;
            }
        }


        public static List<string> YayinKontrol(int mezuniyetSurecId, int kullaniciId, int mezuniyetBasvurulariId)
        {
            var mmMessage = new List<string>();
            using (var entities = new LubsDbEntities())
            {
                var kriter = GetMezuniyetAktifYonetmelik(mezuniyetSurecId, kullaniciId, mezuniyetBasvurulariId);
                var yturAds = entities.MezuniyetYayinTurleris.ToList();



                var kriterDetay = (from s in kriter.MezuniyetSureciYonetmelikleriOTs
                                   join yta in yturAds on s.MezuniyetYayinTurID equals yta.MezuniyetYayinTurID
                                   group new
                                   {
                                       s.MezuniyetYayinTurID,
                                       yta.MezuniyetYayinTurAdi,
                                       s.OgrenimTipKod,
                                       s.IsGecerli,
                                       s.IsZorunlu,
                                       s.GrupKodu
                                   } by new { s.IsZorunlu, IsGrup = s.GrupKodu.IsNullOrWhiteSpace() == false, s.GrupKodu }
                   into g1
                                   select new
                                   {
                                       g1.Key.IsGrup,
                                       g1.Key.GrupKodu,
                                       g1.Key.IsZorunlu,
                                       data = g1.ToList()
                                   }).ToList();
                if (kriterDetay.Any(p => p.IsZorunlu) == false)
                {
                    mmMessage.Add("Bu mezuniyet başvurusu için herhangi bir yayın bilgisi istenmemektedir.");
                }
                else
                {
                    var basvuruEklenenYayinlariTurIds = new List<int>();
                    if (mezuniyetBasvurulariId > 0)
                    {
                        var mezuniyetBasvurusu = entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                        basvuruEklenenYayinlariTurIds = mezuniyetBasvurusu.MezuniyetBasvurulariYayins.Select(s => s.MezuniyetYayinTurID).ToList();

                    }
                    foreach (var item in kriterDetay.Where(p => p.IsZorunlu))
                    {

                        if (item.IsGrup)
                        {
                            var eklenmesiGerekenYayinTurGrubu = item.data.Select(s => new { s.MezuniyetYayinTurID, s.MezuniyetYayinTurAdi }).ToList();

                            if (!eklenmesiGerekenYayinTurGrubu.Any(a => basvuruEklenenYayinlariTurIds.Contains(a.MezuniyetYayinTurID)))
                                mmMessage.Add(string.Join(", ", eklenmesiGerekenYayinTurGrubu.Select(s => s.MezuniyetYayinTurAdi)) + " Yayın Türlerinden biri.");

                        }
                    }

                    if (mmMessage.Any())
                    {
                        mmMessage.Insert(0, "Başvuru onaylanmadan önce aşağıdaki yayın bilgileri eklenmelidir.");
                    }
                }
            }

            return mmMessage;
        }
        public static MmMessage YayinKontrol(KmMezuniyetBasvuru kModel)
        {
            var mmMessage = new MmMessage();
            using (var entities = new LubsDbEntities())
            {
                var kriter = GetMezuniyetAktifYonetmelik(kModel.MezuniyetSurecID, kModel.KullaniciID, kModel.MezuniyetBasvurulariID);
                var yturAds = entities.MezuniyetYayinTurleris.ToList();
                var kriterDetay = (from s in kriter.MezuniyetSureciYonetmelikleriOTs
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

        public static XtraReport MezuniyetSavunmaJurisiAtanmistirYazilari(int mezuniyetBasvurulariId)
        {
            using (var entities = new LubsDbEntities())
            {

                var mezuniyetBasvuru = entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

                var mezuniyetSureci = mezuniyetBasvuru.MezuniyetSureci;
                var enstitu = mezuniyetSureci.Enstituler;
                var anabilimDaliAdi = mezuniyetBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.IlkHarfiBuyut();
                var programAdi = mezuniyetBasvuru.Programlar.ProgramAdi.IlkHarfiBuyut();
                var ogrenciNo = mezuniyetBasvuru.OgrenciNo;
                var ogrenciAdSoyad = (mezuniyetBasvuru.Kullanicilar.Ad).IlkHarfiBuyut() + " " + mezuniyetBasvuru.Kullanicilar.Soyad.ToUpper();

                var jof = mezuniyetBasvuru.MezuniyetJuriOneriFormlaris.First();

                var istezBaslikTr = mezuniyetBasvuru.IsTezDiliTr == true || !mezuniyetBasvuru.IsTezDiliTr.HasValue;
                var tezBaslik = istezBaslikTr ?
                                    (jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikTr : mezuniyetBasvuru.TezBaslikTr)
                                    : (jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikEn : mezuniyetBasvuru.TezBaslikEn);




                var sablonInx = 0;
                XtraReport rprX = null;
                var juriler = jof.MezuniyetJuriOneriFormuJurileris.ToList();

                var tezDanisman =
                    juriler.First(f => f.JuriTipAdi == "TezDanismani");
                var tikUyeleri = juriler
                    .Where(p => p.JuriTipAdi.Contains("TikUyesi")).OrderBy(o => o.JuriTipAdi).ToList();
                var juriUyeleri = juriler
                    .Where(p => p.IsAsilOrYedek.HasValue && p.JuriTipAdi.Contains("Juri")).OrderBy(o => o.IsAsilOrYedek == true ? 1 : 2).ThenBy(o => o.JuriTipAdi.Contains("YtuIci") ? 1 : 2).ThenBy(t => t.JuriTipAdi).ToList();

                var abdYaziId = mezuniyetBasvuru.OgrenimTipKod.IsDoktora() ? YaziSablonTipiEnum.MezuniyetDrSavunmaSinaviJurisiAtandiYazisiAbd : YaziSablonTipiEnum.MezuniyetYlSavunmaSinaviJurisiAtandiYazisiAbd;
                var danismanYaziId = mezuniyetBasvuru.OgrenimTipKod.IsDoktora() ? YaziSablonTipiEnum.MezuniyetDrSavunmaSinaviJurisiAtandiYazisiDanisman : YaziSablonTipiEnum.MezuniyetYlSavunmaSinaviJurisiAtandiYazisiDanisman;
                var jurilerYaziId = mezuniyetBasvuru.OgrenimTipKod.IsDoktora() ? YaziSablonTipiEnum.MezuniyetDrSavunmaSinaviJurisiAtandiYazisiTumJuriler : YaziSablonTipiEnum.MezuniyetYlSavunmaSinaviJurisiAtandiYazisiTumJuriler;
                var sablonTipIds = new List<int>
                    {
                         abdYaziId,
                         danismanYaziId,
                         jurilerYaziId

                };


                var sablonlar = entities.YaziSablonlaris.Where(p => sablonTipIds.Contains(p.YaziSablonTipID) && p.EnstituKod == enstitu.EnstituKod && p.IsAktif).ToList();
                var sablonModel = new List<KeyValuePair<YaziSablonlari, MezuniyetJuriOneriFormuJurileri>>();

                // sablonTipIds koleksiyonunu LINQ ile işliyoruz
                foreach (var sablonTipId in sablonTipIds)
                {
                    var sablon = sablonlar.FirstOrDefault(f => f.YaziSablonTipID == sablonTipId);
                    if (sablon == null) continue;

                    // Eğer sablon tipi danisman ise sadece tezDanisman ekle
                    if (sablon.YaziSablonTipID == danismanYaziId)
                    {
                        sablonModel.Add(new KeyValuePair<YaziSablonlari, MezuniyetJuriOneriFormuJurileri>(sablon, tezDanisman));
                    }
                    else if (sablon.YaziSablonTipID == jurilerYaziId)
                    {
                        // Diğer sablonlar için asilJuris elemanlarını ekle
                        tikUyeleri.ForEach(item =>
                            sablonModel.Add(new KeyValuePair<YaziSablonlari, MezuniyetJuriOneriFormuJurileri>(sablon, item)));
                        juriUyeleri.ForEach(item =>
                            sablonModel.Add(new KeyValuePair<YaziSablonlari, MezuniyetJuriOneriFormuJurileri>(sablon, item)));
                    }
                    else sablonModel.Add(new KeyValuePair<YaziSablonlari, MezuniyetJuriOneriFormuJurileri>(sablon, new MezuniyetJuriOneriFormuJurileri()));
                }

                foreach (var sablon in sablonModel)
                {

                    var parameters = new List<MailParameterDto>
                    {
                        new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDaliAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "ProgramAdi", Value = programAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "OgrenciNo", Value = ogrenciNo },
                        new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenciAdSoyad.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "DanismanUnvan", Value = tezDanisman.UnvanAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "DanismanAdSoyad", Value = tezDanisman.AdSoyad.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "EYKTarihi", Value =mezuniyetBasvuru.EYKTarihi.ToFormatDate() },
                        new MailParameterDto { Key = "EYKSayisi", Value =mezuniyetBasvuru.EYKSayisi },
                        new MailParameterDto { Key = "TezBaslik", Value =tezBaslik  },
                        new MailParameterDto { Key = "SeciliKomiteUyesiUnvan", Value = sablon.Value.UnvanAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "SeciliKomiteUyesiAdSoyad", Value =  sablon.Value.AdSoyad.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "SeciliKomiteUyesiUniversite", Value =  sablon.Value.UniversiteAdi.IlkHarfiBuyut()}
                    };
                    parameters.AddRange(SetParameterJuriSavunmas(tikUyeleri, "TikUyesi"));
                    parameters.AddRange(SetParameterJuriSavunmas(juriUyeleri.Where(p => p.IsAsilOrYedek == true).OrderBy(o => o.JuriTipAdi.Contains("YtuIci") ? 1 : 2).ThenBy(t => t.JuriTipAdi).ToList().ToList(), "AsilKomiteUyesi"));
                    parameters.AddRange(SetParameterJuriSavunmas(juriUyeleri.Where(p => p.IsAsilOrYedek == false).OrderBy(o => o.JuriTipAdi.Contains("YtuIci") ? 1 : 2).ThenBy(t => t.JuriTipAdi).ToList().ToList(), "YedekKomiteUyesi"));
                    var html = ValueReplaceExtension.ProcessHtmlContent(sablon.Key.SablonHtml, parameters);
                    var htmlFooter = ValueReplaceExtension.ProcessHtmlContent(sablon.Key.SablonFooterHtml, parameters);
                    if (sablonInx == 0)
                    {
                        rprX = new RprYaziSablonOlusturucu(enstitu, html, htmlFooter, sablon.Key.Konu);
                        rprX.CreateDocument();
                    }
                    else
                    {
                        var rapor = new RprYaziSablonOlusturucu(enstitu, html, htmlFooter, sablon.Key.Konu);
                        rapor.CreateDocument();
                        rprX.Pages.AddRange(rapor.Pages);
                    }


                    sablonInx++;
                }
                return rprX;

            }
        }
        private static List<MailParameterDto> SetParameterJuriSavunmas(List<MezuniyetJuriOneriFormuJurileri> juris, string key)
        {
            var inx = 0;
            var parameters = new List<MailParameterDto>();
            foreach (var itemJuri in juris)
            {


                inx++;
                parameters.AddRange(new List<MailParameterDto>{
                    new MailParameterDto { Key = $"{key}{inx}Unvan", Value = itemJuri.UnvanAdi.IlkHarfiBuyut() },
                    new MailParameterDto { Key = $"{key}{inx}AdSoyad", Value = itemJuri.AdSoyad.IlkHarfiBuyut() },
                    new MailParameterDto { Key = $"{key}{inx}Universite", Value = itemJuri.UniversiteAdi.IlkHarfiBuyut() },
                });
            }

            return parameters;
        }
        public static XtraReport MezuniyetIkinciTezTeslimTaahhutOnayYazilari(int srTalepId)
        {
            using (var entities = new LubsDbEntities())
            {

                var srTalep = entities.SRTalepleris.First(f => f.SRTalepID == srTalepId);
                var mezuniyetBasvuru = srTalep.MezuniyetBasvurulari;

                var mezuniyetSureci = mezuniyetBasvuru.MezuniyetSureci;
                var enstitu = mezuniyetSureci.Enstituler;
                var anabilimDaliAdi = mezuniyetBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.IlkHarfiBuyut();
                var programAdi = mezuniyetBasvuru.Programlar.ProgramAdi.IlkHarfiBuyut();
                var ogrenciNo = mezuniyetBasvuru.OgrenciNo;
                var ogrenciAdSoyad = (mezuniyetBasvuru.Kullanicilar.Ad).IlkHarfiBuyut() + " " + mezuniyetBasvuru.Kullanicilar.Soyad.ToUpper();

                var jof = mezuniyetBasvuru.MezuniyetJuriOneriFormlaris.First();

                var tezBasligiDegisenSinav = mezuniyetBasvuru.SRTalepleris.OrderByDescending(o => o.SRTalepID).FirstOrDefault(a =>
                    a.MezuniyetSinavDurumID != MezuniyetSinavDurumEnum.SonucGirilmedi &&
                    a.IsTezBasligiDegisti == true);

                var baslikTr = tezBasligiDegisenSinav == null ? (jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikTr : jof.MezuniyetBasvurulari.TezBaslikTr) : tezBasligiDegisenSinav.YeniTezBaslikTr;
                var baslikEn = tezBasligiDegisenSinav == null ? (jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikEn : jof.MezuniyetBasvurulari.TezBaslikEn) : tezBasligiDegisenSinav.YeniTezBaslikEn;
                var isTezBaslikTr = jof.MezuniyetBasvurulari.IsTezDiliTr == true;
                var tezBaslik = isTezBaslikTr ? baslikTr : baslikEn;
                var sablonInx = 0;
                XtraReport rprX = null;
                var tumJuriler = srTalep.SRTaleplerJuris.OrderBy(o => o.JuriTipAdi.Contains("TezDanismani") ? 1 : 2).ThenBy(t => t.JuriTipAdi.Contains("Tik") ? 1 : 2).ThenBy(t => t.JuriTipAdi.Contains("YtuIci") ? 1 : 2).ThenBy(o => o.JuriTipAdi).ToList();

                if (srTalep.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma)
                {
                    var uzatmadanSonrakiSinav =
                        mezuniyetBasvuru.SRTalepleris.FirstOrDefault(p => p.SRTalepID > srTalep.SRTalepID && p.SRDurumID == SrTalepDurumEnum.Onaylandı);
                    if (uzatmadanSonrakiSinav != null)
                        tumJuriler = uzatmadanSonrakiSinav.SRTaleplerJuris.OrderBy(o => o.JuriTipAdi.Contains("TezDanismani") ? 1 : 2).ThenBy(t => t.JuriTipAdi.Contains("Tik") ? 1 : 2).ThenBy(t => t.JuriTipAdi.Contains("YtuIci") ? 1 : 2).ThenBy(o => o.JuriTipAdi).ToList();
                }


                var tezDanisman = tumJuriler.First(f => f.JuriTipAdi == "TezDanismani");
                var jurilerUyeleri = tumJuriler.Where(p => !p.JuriTipAdi.Contains("TezDanismani")).ToList();
                var abdYaziId = mezuniyetBasvuru.OgrenimTipKod.IsDoktora() ? YaziSablonTipiEnum.MezuniyetDrIkinciTezTeslimTaahhutOnayYazisiAbd : YaziSablonTipiEnum.MezuniyetYlIkinciTezTeslimTaahhutOnayYazisiAbd;
                var danismanYaziId = mezuniyetBasvuru.OgrenimTipKod.IsDoktora() ? YaziSablonTipiEnum.MezuniyetDrIkinciTezTeslimTaahhutOnayYazisiDanisman : YaziSablonTipiEnum.MezuniyetYlIkinciTezTeslimTaahhutOnayYazisiDanisman;
                var jurilerYaziId = mezuniyetBasvuru.OgrenimTipKod.IsDoktora() ? YaziSablonTipiEnum.MezuniyetDrIkinciTezTeslimTaahhutOnayYazisiTumJuriler : YaziSablonTipiEnum.MezuniyetYlIkinciTezTeslimTaahhutOnayYazisiTumJuriler;
                var sablonTipIds = new List<int>
                    {
                         abdYaziId,
                         danismanYaziId,
                         jurilerYaziId

                };


                var sablonlar = entities.YaziSablonlaris.Where(p => sablonTipIds.Contains(p.YaziSablonTipID) && p.EnstituKod == enstitu.EnstituKod && p.IsAktif).ToList();
                var sablonModel = new List<KeyValuePair<YaziSablonlari, SRTaleplerJuri>>();

                // sablonTipIds koleksiyonunu LINQ ile işliyoruz
                foreach (var sablonTipId in sablonTipIds)
                {
                    var sablon = sablonlar.FirstOrDefault(f => f.YaziSablonTipID == sablonTipId);
                    if (sablon == null) continue;
                    if (sablon.YaziSablonTipID == danismanYaziId)
                        sablonModel.Add(new KeyValuePair<YaziSablonlari, SRTaleplerJuri>(sablon, tezDanisman));
                    else if (sablon.YaziSablonTipID == jurilerYaziId || sablon.YaziSablonTipID == danismanYaziId)
                    {
                        jurilerUyeleri.ForEach(item => sablonModel.Add(new KeyValuePair<YaziSablonlari, SRTaleplerJuri>(sablon, item)));

                    }
                    else sablonModel.Add(new KeyValuePair<YaziSablonlari, SRTaleplerJuri>(sablon, new SRTaleplerJuri()));
                }


                foreach (var sablon in sablonModel)
                {


                    var parameters = new List<MailParameterDto>
                    {
                        new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDaliAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "ProgramAdi", Value = programAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "OgrenciNo", Value = ogrenciNo },
                        new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenciAdSoyad.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "DanismanUnvan", Value = tezDanisman.UnvanAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "DanismanAdSoyad", Value = tezDanisman.JuriAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "TezBaslik", Value =tezBaslik },
                        new MailParameterDto { Key = "OgrenciTaahhutOnayTarihi", Value =srTalep.OgrenciOnayTarihi.ToFormatDate() },
                        new MailParameterDto { Key = "SeciliKomiteUyesiUnvan", Value = sablon.Value.UnvanAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "SeciliKomiteUyesiAdSoyad", Value =  sablon.Value.JuriAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "SeciliKomiteUyesiUniversite", Value =  sablon.Value.UniversiteAdi.IlkHarfiBuyut()}
                    };
                    parameters.AddRange(SetParameterJuris(jurilerUyeleri, "AsilKomiteUyesi"));
                    var html = ValueReplaceExtension.ProcessHtmlContent(sablon.Key.SablonHtml, parameters);
                    var htmlFooter = ValueReplaceExtension.ProcessHtmlContent(sablon.Key.SablonFooterHtml, parameters);
                    if (sablonInx == 0)
                    {
                        rprX = new RprYaziSablonOlusturucu(enstitu, html, htmlFooter, sablon.Key.Konu);
                        rprX.CreateDocument();
                    }
                    else
                    {
                        var rapor = new RprYaziSablonOlusturucu(enstitu, html, htmlFooter, sablon.Key.Konu);
                        rapor.CreateDocument();
                        rprX.Pages.AddRange(rapor.Pages);
                    }


                    sablonInx++;
                }
                return rprX;

            }
        }
        private static List<MailParameterDto> SetParameterJuris(List<SRTaleplerJuri> juris, string key)
        {
            var inx = 0;
            var parameters = new List<MailParameterDto>();
            foreach (var itemJuri in juris)
            {


                inx++;
                parameters.AddRange(new List<MailParameterDto>{
                    new MailParameterDto { Key = $"{key}{inx}Unvan", Value = itemJuri.UnvanAdi.IlkHarfiBuyut() },
                    new MailParameterDto { Key = $"{key}{inx}AdSoyad", Value = itemJuri.JuriAdi.IlkHarfiBuyut() },
                    new MailParameterDto { Key = $"{key}{inx}Universite", Value = itemJuri.UniversiteAdi.IlkHarfiBuyut() },
                });
            }

            return parameters;
        }

        public static KmMezuniyetSureciOgrenimTipModel GetMezuniyetOgrenimTipKriterleri(string enstituKod, int mezuniyetSurecId)
        {
            var model = new KmMezuniyetSureciOgrenimTipModel();
            using (var entities = new LubsDbEntities())
            {
                var ogrenimTipleri = entities.OgrenimTipleris
                    .Where(p => p.EnstituKod == enstituKod && p.IsMezuniyetBasvurusuYapabilir).ToList();

                var sonMezuniyetSurecId = entities.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod && p.MezuniyetSurecID != mezuniyetSurecId)
                    .OrderByDescending(t => t.MezuniyetSurecID).Select(s => s.MezuniyetSurecID).FirstOrDefault();

                var sonMezuniyetOgrenimTipleri = entities.MezuniyetSurecis
                    .Where(p => p.MezuniyetSurecID == sonMezuniyetSurecId).SelectMany(s => s.MezuniyetSureciOgrenimTipKriterleris).ToList();

                var mezuniyetOgrenimTipleri = entities.MezuniyetSureciOgrenimTipKriterleris
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
                                                  TekKaynakOrani = surecOgrenimTipi?.TekKaynakOrani ?? (mezuniyetSurecId > 0 ? null : sonSurecOgrenimTipi?.TekKaynakOrani),
                                                  ToplamKaynakOrani = surecOgrenimTipi?.ToplamKaynakOrani ?? (mezuniyetSurecId > 0 ? null : sonSurecOgrenimTipi?.ToplamKaynakOrani),
                                                  SinavUzatmaOgrenciTaahhutMaxAy = surecOgrenimTipi?.SinavUzatmaOgrenciTaahhutMaxAy ??
                                                                                      sonSurecOgrenimTipi?.SinavUzatmaOgrenciTaahhutMaxAy ?? 0,
                                                  SinavUzatmaSinavAlmaSuresiMaxAy = surecOgrenimTipi?.SinavUzatmaSinavAlmaSuresiMaxAy ??
                                                                                       sonSurecOgrenimTipi?.SinavUzatmaSinavAlmaSuresiMaxAy ?? 0,
                                                  TezTeslimSuresiAy = surecOgrenimTipi?.TezTeslimSuresiAy ??
                                                                         sonSurecOgrenimTipi?.TezTeslimSuresiAy ?? 0,
                                                  SinavKacGunSonraAlabilir = surecOgrenimTipi?.SinavKacGunSonraAlabilir ??
                                                                                  sonSurecOgrenimTipi?.SinavKacGunSonraAlabilir ?? 0,
                                                  SinavEnGecKacAySonraAlabilir = surecOgrenimTipi?.SinavEnGecKacAySonraAlabilir ??
                                                                             sonSurecOgrenimTipi?.SinavEnGecKacAySonraAlabilir ?? 0,


                                              }).ToList();

                foreach (var item in model.OgrenimTipKriterList)
                {
                    item.SlistEtikNots = new SelectList(HarfNotuHelper.NotDegerleri, item.AktifDonemEtikNotKriteri);
                    item.SlistSeminerNots = new SelectList(HarfNotuHelper.NotDegerleri, item.AktifDonemSeminerNotKriteri);
                }

                if (mezuniyetSurecId <= 0 && sonMezuniyetSurecId <= 0)
                {
                    //seçili enstitüye ait hiç mezuniyet süreci yoksa fbe ye bak ve süreç varsa ilk süreçteki bilgilere göre tekrar metodu çalıştır.
                    var digerMezuniyetSureci = entities.MezuniyetSurecis.OrderByDescending(o => o.EnstituKod == EnstituKodlariEnum.FenBilimleri ? 2 : 1)
                         .ThenByDescending(t => t.MezuniyetSurecID).FirstOrDefault();
                    if (digerMezuniyetSureci != null)
                    {
                        //ana süreç öğrenim tiplerinin tamamı diğer süreç öğrenim tipinde varsa diğer sürecin öğrenim tipine göre verileri getir

                        var enstituSurecKriterleri = GetMezuniyetOgrenimTipKriterleri(
                            digerMezuniyetSureci.EnstituKod,
                            digerMezuniyetSureci.MezuniyetSurecID);
                        foreach (var ogrenimTip in model.OgrenimTipKriterList)
                        {
                            var secilenOgrenimTipi = enstituSurecKriterleri.OgrenimTipKriterList.FirstOrDefault(p => p.OgrenimTipKod == ogrenimTip.OgrenimTipKod);
                            if (secilenOgrenimTipi != null)
                            {
                                ogrenimTip.AktifDonemMaxKriteri = secilenOgrenimTipi.AktifDonemMaxKriteri;
                                ogrenimTip.AktifDonemDersKodKriteri = secilenOgrenimTipi.AktifDonemDersKodKriteri;
                                ogrenimTip.AktifDonemEtikNotKriteri = secilenOgrenimTipi.AktifDonemEtikNotKriteri;
                                ogrenimTip.AktifDonemSeminerNotKriteri = secilenOgrenimTipi.AktifDonemSeminerNotKriteri;
                                ogrenimTip.AktifDonemToplamKrediKriteri = secilenOgrenimTipi.AktifDonemToplamKrediKriteri;
                                ogrenimTip.AktifDonemAgnoKriteri = secilenOgrenimTipi.AktifDonemAgnoKriteri;
                                ogrenimTip.AktifDonemAktsKriteri = secilenOgrenimTipi.AktifDonemAktsKriteri;
                                ogrenimTip.TekKaynakOrani = secilenOgrenimTipi.TekKaynakOrani;
                                ogrenimTip.ToplamKaynakOrani = secilenOgrenimTipi.ToplamKaynakOrani;
                                ogrenimTip.SinavKacGunSonraAlabilir = secilenOgrenimTipi.SinavKacGunSonraAlabilir;
                                ogrenimTip.SinavEnGecKacAySonraAlabilir = secilenOgrenimTipi.SinavEnGecKacAySonraAlabilir;
                                ogrenimTip.SinavUzatmaOgrenciTaahhutMaxAy = secilenOgrenimTipi.SinavUzatmaOgrenciTaahhutMaxAy;
                                ogrenimTip.SinavUzatmaSinavAlmaSuresiMaxAy = secilenOgrenimTipi.SinavUzatmaSinavAlmaSuresiMaxAy;
                                ogrenimTip.TezTeslimSuresiAy = secilenOgrenimTipi.TezTeslimSuresiAy;
                            }
                        }

                    }

                }
            }
            return model;
        }
        public static MezuniyetBasvurulariYayinDto GetYayinBilgisi(int mezuniyetSurecId, int mezuniyetYayinTurId)
        {
            MezuniyetBasvurulariYayinDto mdl;
            using (var entities = new LubsDbEntities())
            {
                mdl = (from s in entities.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId && p.MezuniyetYayinTurID == mezuniyetYayinTurId)
                       join sd in entities.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                       join yb in entities.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                       from ybD in defyb.DefaultIfEmpty()
                       join klk in entities.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                       from klkD in defklk.DefaultIfEmpty()
                       join ym in entities.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                       from ymD in defym.DefaultIfEmpty()
                       join kl in entities.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                       from klD in defkl.DefaultIfEmpty()
                       join inx in entities.MezuniyetYayinIndexTurleris on new { s.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
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
                mdl.YayinIndexTurleri = entities.MezuniyetYayinIndexTurleris.ToList();
                mdl.MezuniyetYayinProjeTurleris = entities.MezuniyetYayinProjeTurleris.ToList();
            }
            return mdl;
        }
        public static List<MezuniyetYayinKontrolDurumlari> GetMezuniyetYayinDurumListe(List<int> selectedBDurumId = null)
        {
            using (var entities = new LubsDbEntities())
            {
                var qdata = entities.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif);
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
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "GetDetailMezuniyet_t1_Basvuru");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayEykSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "GetDetailMezuniyet_t2_EYKSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetaySinavSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "GetDetailMezuniyet_t3_SinavSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayTezKontrolSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "GetDetailMezuniyet_t4_TezKontrolSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayMezuniyetSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "GetDetailMezuniyet_t5_MezuniyetSureci");
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
            using (var entities = new LubsDbEntities())
            {
                var data = (from s in entities.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                            join d in entities.Donemlers on s.DonemID equals d.DonemID
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
            using (var entities = new LubsDbEntities())
            {
                var data = (from s in entities.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                            join d in entities.Donemlers on s.DonemID equals d.DonemID
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
            using (var entities = new LubsDbEntities())
            {

                var qData = (from s in entities.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                             join bsv in entities.MezuniyetBasvurularis on s.MezuniyetSurecID equals bsv.MezuniyetSurecID
                             join d in entities.Donemlers on bsv.KayitOgretimYiliDonemID equals d.DonemID
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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.Kullanicilars.Where(p => p.IsAktif && p.EnstituKod == enstituKod && p.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId).OrderBy(o => o.Ad).ThenBy(t => t.Soyad).ToList();
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
        public static List<CmbIntDto> GetCmbMezuniyetYayinDurumBasvuruYapIcin(bool bosSecimVar = false, bool tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif && (tumu || p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
                foreach (var item in data)
                {
                    if (item.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi)
                        item.MezuniyetYayinKontrolDurumAdi = "Başvuruyu Onaylıyorum";
                    else if (item.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Taslak)
                        item.MezuniyetYayinKontrolDurumAdi = "Başvuru Taslak Durumunda Kalsın";
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinKontrolDurumID, Caption = item.MezuniyetYayinKontrolDurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinDurum(bool bosSecimVar = false, bool tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif && (tumu || p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MezuniyetSinavDurumlaris.Where(p => !haricSinavDurumIds.Contains(p.MezuniyetSinavDurumID)).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetSinavDurumID, Caption = item.MezuniyetSinavDurumAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbMzSinavDurumEnstituOnayListe(bool bosSecimVar = false, List<int> mezuniyetSinavDurumIDs = null)
        {
            var dct = new List<CmbIntDto>();
            mezuniyetSinavDurumIDs = mezuniyetSinavDurumIDs ?? new List<int>();
            mezuniyetSinavDurumIDs.Add(MezuniyetSinavDurumEnum.SonucGirilmedi);
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MezuniyetSinavDurumlaris.Where(p => mezuniyetSinavDurumIDs.Contains(p.MezuniyetSinavDurumID)).ToList();
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
            dct.Add(new CmbBoolDto { Value = true, Caption = "Ciltli Son Tez Teslimini Yapmıştır" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Ciltli Son Tez Teslimini Yapmamıştır" });

            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetDurumId(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = -1, Caption = "" });
            dct.Add(new CmbIntDto { Value = null, Caption = "Sonuç Girilmedi" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Ciltli Son Tez Teslimini Yapmıştır" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Ciltli Son Tez Teslimini Yapmamıştır" });
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetSurecYayinTurleri(int mezuniyetSurecId, int kullaniciId, int mezuniyetBasvurulariId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var kriter = MezuniyetBus.GetMezuniyetAktifOgrenimTipiYayinBilgileri(mezuniyetSurecId, kullaniciId, mezuniyetBasvurulariId);
                var mezuniyetYayinTurIDs = kriter.Where(p => p.IsGecerli).Select(s => s.MezuniyetYayinTurID).Distinct().ToList();

                var qdata = entities.MezuniyetYayinTurleris.AsQueryable();
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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MezuniyetYayinKontrolDurumlaris.Where(p => (tumu || p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinKontrolDurumID, Caption = item.MezuniyetYayinKontrolDurumAdi });
                }
                dct.Insert(bosSecimVar ? 3 : 2, new CmbIntDto { Value = MezuniyetYayinKontrolDurumuEnum.DanismanOnayiBekleniyor, Caption = "Danışman Onayı Bekleniyor" });
                dct.Insert(bosSecimVar ? 4 : 3, new CmbIntDto { Value = MezuniyetYayinKontrolDurumuEnum.EnstituOnayiBekleniyor, Caption = "Enstitü Onayı Bekleniyor" });
            }

            return dct;

        }
        public static List<CmbBoolDto> GetCmbMezuniyetYayinKontrolAciklamaDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Açıklama Girilenler" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Açıklama Girilmeyenler" });


            return dct;

        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinBelgeTurleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MezuniyetYayinBelgeTurleris.OrderBy(o => o.BelgeTurAdi).ToList();
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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MezuniyetYayinLinkTurleris.Where(p => p.IsKaynakOrYayin == isKaynakOrYayin).OrderBy(o => o.LinkTurAdi).ToList();
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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.MezuniyetYayinMetinTurleris.OrderBy(o => o.MetinTurAdi).ToList();
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

            dct.Add(new CmbIntDto { Value = 1, Caption = "Form oluşturulmadı" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Form oluşturuldu" });
            // dct.Add(new CmbIntDto { Value = 3, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            // dct.Add(new CmbIntDto { Value = 6, Caption = "EYK'ya Hazırlanma Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'ya Hazırlandı" });
            // dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = 9, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 10, Caption = "EYK'Da Onaylanmadı" });
            return dct;

        }
        public static List<CmbIntDto> GetCmbCiltliTezTeslimUzatmaTalepDurumu(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = MezuniyetTezTeslimUzatmaDurumuEnum.TalepOlusturulmadi, Caption = "Talep Oluşturulmadı" });
            dct.Add(new CmbIntDto { Value = MezuniyetTezTeslimUzatmaDurumuEnum.TalepOlusturuldu, Caption = "Talep oluşturuldu" });
            dct.Add(new CmbIntDto { Value = MezuniyetTezTeslimUzatmaDurumuEnum.DanismanOnayladi, Caption = "Danışman Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = MezuniyetTezTeslimUzatmaDurumuEnum.DanismanOnaylamadi, Caption = "Danışman Tarafından Onaylanmadı" });
            //dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'ya Hazırlandı" });
            // dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = MezuniyetTezTeslimUzatmaDurumuEnum.EykDaOnaylandi, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = MezuniyetTezTeslimUzatmaDurumuEnum.EykDaOnaylanmadi, Caption = "EYK'Da Onaylanmadı" });
            return dct;

        }
        public static List<CmbIntDto> GetCmbTezDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = TezKontrolDurumEnum.IlkKezKontrolBekleyenler, Caption = "İlk Kez Kontrol Bekleyenler" });
            dct.Add(new CmbIntDto { Value = TezKontrolDurumEnum.IslemBekleyenler, Caption = "İşlem Bekleyenler" });
            dct.Add(new CmbIntDto { Value = TezKontrolDurumEnum.DuzeltmeTalepEdildi, Caption = "Düzeltme Talep Edildi" });
            dct.Add(new CmbIntDto { Value = TezKontrolDurumEnum.Onaylananlar, Caption = "Onaylananlar" });

            return dct;

        }
        public static List<CmbIntDto> GetCmbFilterMezuniyetAnabilimDallari(string enstituKod, int? basvuruSurecId, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var yeterliAnabilimDaliIds = entities.MezuniyetBasvurularis
                    .Where(p => p.MezuniyetSureci.EnstituKod == enstituKod &&
                                p.MezuniyetSurecID == (basvuruSurecId ?? p.MezuniyetSurecID)).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = entities.AnabilimDallaris.Where(p => yeterliAnabilimDaliIds.Contains(p.AnabilimDaliID))
                    .Select(s => new { s.AnabilimDaliID, s.AnabilimDaliAdi }).OrderBy(o => o.AnabilimDaliAdi).ToList();

                foreach (var item in anabilimDallaris)
                {
                    lst.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return lst;
        }
        public static MmMessage SendMailBasvuruDanismanOnay(int mezuniyetBasvurulariId)
        {
            return MailSenderMezuniyet.SendMailBasvuruDanismanOnay(mezuniyetBasvurulariId);
        }
        public static MmMessage SendMailBasvuruDurum(int mezuniyetBasvurulariId)
        {
            return MailSenderMezuniyet.SendMailBasvuruDurum(mezuniyetBasvurulariId);
        }
        public static MmMessage SendMailJuriOneriFormuEykOnay(int mezuniyetJuriOneriFormId, bool isOnaylandi)
        {
            return isOnaylandi ? MailSenderMezuniyet.SendMailJuriOneriFormuEykOnay(mezuniyetJuriOneriFormId) :
                MailSenderMezuniyet.SendMailJuriOneriFormuEykRet(mezuniyetJuriOneriFormId);
        }
        public static MmMessage SendMailJuriOneriFormuEykYaGonderimRet(int mezuniyetJuriOneriFormId)
        {
            return MailSenderMezuniyet.SendMailJuriOneriFormuEykYaGonderimRet(mezuniyetJuriOneriFormId);
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
        public static MmMessage SendMailUzatmaSonrasiDanismanOnay(int srTalepId)
        {
            return MailSenderMezuniyet.SendMailUzatmaSonrasiDanismanOnay(srTalepId);
        }


        public static MmMessage SendMailMezuniyetTezSablonKontrol(int mezuniyetBasvurulariTezDosyaId, int sablonTipId, string aciklama = "")
        {
            return MailSenderMezuniyet.SendMailMezuniyetTezSablonKontrol(mezuniyetBasvurulariTezDosyaId, sablonTipId, aciklama);
        }


        public static MmMessage SendMailCiltliTezTeslimEkSureTalebiYapildi(int mezuniyetBasvurulariId)
        {
            return MailSenderMezuniyet.SendMailCiltliTezTeslimEkSureTalebiYapildi(mezuniyetBasvurulariId);
        }
        public static MmMessage SendMailCiltliTezTeslimEkSureTalebiDanismanOnay(int mezuniyetBasvurulariId)
        {
            return MailSenderMezuniyet.SendMailCiltliTezTeslimEkSureTalebiDanismanOnay(mezuniyetBasvurulariId);
        }
        public static MmMessage SendMailCiltliTezTeslimEkSureTalebiEYKOnay(int mezuniyetBasvurulariId)
        {
            return MailSenderMezuniyet.SendMailCiltliTezTeslimEkSureTalebiEYKOnay(mezuniyetBasvurulariId);
        }

        public static SonTezBaslikInfo GeSonTezBaslikInfo(int mezuniyetBasvurulariId, bool isTezTeslimFormuVarsaOrdanAl = false)
        {
            using (LubsDbEntities entities = new LubsDbEntities())
            {
                // Mezuniyet başvurusunu getir
                var basvuru = entities.MezuniyetBasvurularis.FirstOrDefault(p =>
                    p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

                // Başvuru yoksa null dön
                if (basvuru == null)
                {
                    return null;
                }

                string yeniTezBaslikTr = null;
                string yeniTezBaslikEn = null;

                // Tez teslim formundan başlık bilgilerini al (eğer istenirse)
                if (isTezTeslimFormuVarsaOrdanAl)
                {
                    var tezTeslimFormu = basvuru.MezuniyetBasvurulariTezTeslimFormlaris.FirstOrDefault();
                    yeniTezBaslikTr = tezTeslimFormu?.TezBaslikTr;
                    yeniTezBaslikEn = tezTeslimFormu?.TezBaslikEn;
                }

                // Eğer tez teslim formunda başlık bilgileri varsa, onları kullan
                if (!string.IsNullOrWhiteSpace(yeniTezBaslikTr) || !string.IsNullOrWhiteSpace(yeniTezBaslikEn))
                {
                    return new SonTezBaslikInfo
                    {
                        ModuleName = "MezuniyetBasvuru",
                        IsTezDiliTr = basvuru.IsTezDiliTr == true && basvuru.IsTezDiliTr.HasValue,
                        TezBaslikTr = yeniTezBaslikTr,
                        TezBaslikEn = yeniTezBaslikEn
                    };
                }

                // Jüri öneri formundan başlık bilgilerini al
                var juriOneriFormu = basvuru.MezuniyetJuriOneriFormlaris.FirstOrDefault();

                // Sınav talebi bilgilerini al
                var srTalep = basvuru.SRTalepleris.FirstOrDefault(f =>
                    f.MezuniyetSinavDurumID == 2 && f.MezuniyetSinavDurumID.HasValue);

                // Türkçe başlık bilgisini belirle
                string tezBaslikTr = basvuru.TezBaslikTr;
                string tezBaslikEn = basvuru.TezBaslikEn;
                if (juriOneriFormu != null && juriOneriFormu.IsTezBasligiDegisti == true)
                {
                    tezBaslikTr = juriOneriFormu.YeniTezBaslikTr;
                    tezBaslikEn = juriOneriFormu.YeniTezBaslikEn;
                }
                yeniTezBaslikTr = tezBaslikTr;
                yeniTezBaslikEn = tezBaslikEn;

                // Sınav talebindeki başlık değişikliği kontrolü
                if (srTalep != null)
                {
                    if (srTalep.IsTezBasligiDegisti == true && srTalep.IsTezBasligiDegisti.HasValue)
                    {
                        // Sınav talebinde başlık değişmişse, o başlığı kullan
                        yeniTezBaslikTr = srTalep.YeniTezBaslikTr;
                        yeniTezBaslikEn = srTalep.YeniTezBaslikEn;
                    }
                    else
                    {
                        // Önceki sınav taleplerini kontrol et
                        var oncekiTalep = basvuru.SRTalepleris
                            .Where(p =>
                                p.MezuniyetSinavDurumID == 3 &&
                                p.MezuniyetSinavDurumID.HasValue &&
                                p.SRTalepID < srTalep.SRTalepID)
                            .OrderByDescending(p => p.SRTalepID)
                            .Select(p => new
                            {
                                IsTezBasligiDegisti = p.IsTezBasligiDegisti,
                                YeniTezBaslikTr = p.YeniTezBaslikTr,
                                YeniTezBaslikEn = p.YeniTezBaslikEn
                            })
                            .FirstOrDefault();

                        bool tezBasligiDegismis = oncekiTalep != null && oncekiTalep.IsTezBasligiDegisti == true;

                        if (tezBasligiDegismis)
                        {
                            yeniTezBaslikTr = oncekiTalep.YeniTezBaslikTr;
                            yeniTezBaslikEn = oncekiTalep.YeniTezBaslikEn;
                        }
                    }
                }

                // Sonuç bilgilerini dön
                return new SonTezBaslikInfo
                {
                    ModuleName = "MezuniyetBasvuru",
                    IsTezDiliTr = basvuru.IsTezDiliTr == true && basvuru.IsTezDiliTr.HasValue,
                    TezBaslikTr = yeniTezBaslikTr,
                    TezBaslikEn = yeniTezBaslikEn
                };
            }
        }
    }
}