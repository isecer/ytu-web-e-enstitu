using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class DonemProjesiBus
    {

        public static IHtmlString ToDpBasvuruDurumView(this DpBasvuruDurumDto model)
        {
            model = model ?? new DpBasvuruDurumDto();
            var pagerString = model.ToRenderPartialViewHtml("DpBasvuru", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString ToDpBasvuruDonemView(this DpBasvuruDurumDto model)
        {
            model = model ?? new DpBasvuruDurumDto();
            var pagerString = model.ToRenderPartialViewHtml("DpBasvuru", "BasvuruDonemView");
            return pagerString;
        }
        public static List<string> DonemProjesiKontrol(string enstituKod, Guid? donemProjesiUniqueId, Guid? donemProjesiBasvuruUniqueId)
        {
            var errorMessage = new List<string>();
            using (var entities = new LubsDbEntities())
            {

                var donemProjesi = entities.DonemProjesis.FirstOrDefault(p => p.UniqueID == donemProjesiUniqueId);
                var donemProjesiBasvuru = entities.DonemProjesiBasvurus.FirstOrDefault(p => p.UniqueID == donemProjesiBasvuruUniqueId);
                var basvuranKullaniciId = donemProjesi?.KullaniciID ?? UserIdentity.Current.Id;
                var basvuranKullanici = entities.Kullanicilars.First(f => f.KullaniciID == basvuranKullaniciId);
                var obsStudentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(basvuranKullaniciId);

                var basvuruYapmaYetki = RoleNames.DonemProjesiBasvuruYapmaYetkisi.InRoleCurrent();
                var enstituBasvuruOnayYetkisi = RoleNames.DonemProjesiEnstituBasvuruOnayYetkisi.InRoleCurrent();
                var isBasvuruAlimiAcik = DonemProjesiAyar.DonemProjesiBasvuruAlimiAcik.GetAyarDp(enstituKod).ToBoolean(false);


                if (!enstituBasvuruOnayYetkisi && !isBasvuruAlimiAcik)
                {
                    errorMessage.Add("Dönem projesi sınavı başvuru işlemleri kapalıdır.");
                    return errorMessage;
                }
                if (donemProjesi != null && !basvuruYapmaYetki && donemProjesi.KullaniciID != UserIdentity.Current.Id)
                {
                    errorMessage.Add("Dönem projesi başvurusu düzeltme yetkisine sahip değilsiniz.");
                    return errorMessage;
                }
                if (donemProjesi == null && (!basvuranKullanici.YtuOgrencisi || basvuranKullanici.OgrenimTipKod != OgrenimTipi.TezsizYuksekLisans))
                {
                    errorMessage.Add("Dönem Projesi başvurusu işlemi sadece Tezsiz YL öğrencileri tarafından yapılabilir.");
                    return errorMessage;
                }
                if (!UserIdentity.Current.EnstituKods.Contains(donemProjesi == null ? basvuranKullanici.EnstituKod : donemProjesi.EnstituKod))
                {
                    errorMessage.Add("Bu enstitü için işlem yetkiniz bulunmamaktadır.");
                    return errorMessage;
                }
                if (enstituKod != basvuranKullanici.EnstituKod)
                {
                    var enstitu = entities.Enstitulers.First(p => p.EnstituKod == basvuranKullanici.EnstituKod);
                    errorMessage.Add("Kayıtlı olunan enstitü ile başvuru yapılan entistü uyuşmamaktadır. Enstitünüz: " + enstitu.EnstituAd + " olarak gözükmektedir.");
                    return errorMessage;
                }
                if (obsStudentInfo.DanismanInfo == null)
                {
                    errorMessage.Add("Proje yürütücüsü bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunun gerçekleşebilmesi için proje yürütücüsü bilgisinin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    return errorMessage;
                }
                if (obsStudentInfo.DanismanInfo.TCKIMLIKNO.IsNullOrWhiteSpace() || obsStudentInfo.DanismanInfo.TCKIMLIKNO.Length != 11)
                {
                    errorMessage.Add("Proje yürütücüsünün Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunun gerçekleşebilmesi için proje yürütücüsü bilgisinin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    return errorMessage;
                }
                if (!obsStudentInfo.AktifDanismanID.HasValue)
                {
                    var projeYurutucuMessage = obsStudentInfo.DanismanInfo == null
                           ? "Başvuru yapabilmeniz için proje yürütücü bilginizin OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz."
                           : $"Başvuru yapabilmeniz için proje yürütücünüzün '{obsStudentInfo.DanismanInfo.UNVAN_AD} {obsStudentInfo.DanismanInfo.AD} {obsStudentInfo.DanismanInfo.SOYAD}' lisansüstü sisteminde kullanıcı hesabı oluşturması gerekmektedir.";
                    errorMessage.Add(projeYurutucuMessage);
                    return errorMessage;
                }
                if (donemProjesiBasvuru == null && !entities.DonemProjesiMuafOgrencilers.Any(a => a.KullaniciID == basvuranKullanici.KullaniciID && a.OgrenciNo == basvuranKullanici.OgrenciNo))
                {
                    var controlMessage = new List<string>();
                    var minBasvuruObsAktifDonemNo = DonemProjesiAyar.OgrencininBasvuruYapabilecegiMinDonemNo.GetAyarDp(enstituKod).ToInt().Value;
                    var maxBasvuruObsAktifDonemNo = DonemProjesiAyar.OgrencininBasvuruYapabilecegiMaxDonemNo.GetAyarDp(enstituKod).ToInt().Value;
                    var alimnasiGerekenDersKodlari = DonemProjesiAyar.GetBasvuruDonemindeAlmasiGerekenDersKodlari(enstituKod);

                    if (!(minBasvuruObsAktifDonemNo <= obsStudentInfo.OkuduguDonemNo && maxBasvuruObsAktifDonemNo >= obsStudentInfo.OkuduguDonemNo))
                    {
                        controlMessage.Add("Aktif okunan dönem " + minBasvuruObsAktifDonemNo + ".dönem ve " + maxBasvuruObsAktifDonemNo + ".dönem aralığında olması gerekmektedir.");
                    }

                    if (alimnasiGerekenDersKodlari.Any())
                    {
                        var aktifDonem = DateTime.Now.ToAkademikDonemBilgi();
                        var alinanDersler = obsStudentInfo.TumDonemDersNotlari
                            .Where(p => p.DonemId == (aktifDonem.BaslangicYil + "" + aktifDonem.DonemId))
                            .Select(s => s.DersKoduNum).ToList();
                        var alinmayanDersKodlari = alimnasiGerekenDersKodlari.Where(a => alinanDersler.All(a2 => a2 != a)).ToList();
                        if (alinmayanDersKodlari.Any()) controlMessage.Add("Aktif olarak okuduğunuz " + aktifDonem.DonemAdiLong + " dönemi için " + string.Join(",", alinmayanDersKodlari) + " kodlu derslerin alınması gerekmetedir.");
                    }
                    if (controlMessage.Count > 0)
                    {
                        errorMessage.Add("Dönem Projesi başvurusu aşağıdaki sebeplerden dolayı başlatılamadı.");
                        errorMessage.AddRange(controlMessage);
                        return errorMessage;
                    }

                }


            }
            return errorMessage;

        }

        public static MmMessage DonemProjesiSilKontrol(Guid? uniqueId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var entities = new LubsDbEntities())
            {
                var kayitYetki = RoleNames.DonemProjesiEnstituBasvuruOnayYetkisi.InRoleCurrent();
                var basvuru =
                    entities.DonemProjesis.FirstOrDefault(p => p.UniqueID == uniqueId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && kayitYetki &&
                        basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                    }
                    else if (!DonemProjesiAyar.DonemProjesiBasvuruAlimiAcik.GetAyarDp(basvuru.EnstituKod).ToBoolean(false) && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Dönem Projesi başvuru süreci kapalı olduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                    }
                }
            }

            return msg;
        }

        public static MmMessage DonemProjesiBasvuruFormuSilKontrol(Guid? uniqueId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var entities = new LubsDbEntities())
            {
                var kayitYetki = RoleNames.DonemProjesiEnstituBasvuruOnayYetkisi.InRoleCurrent();
                var donemProjesiBasvuru =
                    entities.DonemProjesiBasvurus.FirstOrDefault(p => p.UniqueID == uniqueId);
                if (donemProjesiBasvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(donemProjesiBasvuru.DonemProjesi.EnstituKod) && kayitYetki &&
                        donemProjesiBasvuru.DonemProjesi.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                    }
                    else if (!DonemProjesiAyar.DonemProjesiBasvuruAlimiAcik.GetAyarDp(donemProjesiBasvuru.DonemProjesi.EnstituKod).ToBoolean(false) && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Dönem Projesi başvuru süreci kapalı olduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && donemProjesiBasvuru.DonemProjesi.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                    }
                    else if (donemProjesiBasvuru.DonemProjesiDurumID != DonemProjesiDurumEnum.EnstituOnaySureci || donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID.HasValue)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add(donemProjesiBasvuru.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Dönem Projesinin silinebilmesi için enstitü tarafından onay işlemi yapılmamış olması gerekmetekdir.");

                    }
                }

            }

            return msg;
        }

        public static KmDonemProjesiDto GetDonemProjesiBasvuru(Guid? id, string enstituKod)
        {
            var model = new KmDonemProjesiDto();
            var kayitYetki = RoleNames.DonemProjesiEnstituBasvuruOnayYetkisi.InRoleCurrent();
            using (var entities = new LubsDbEntities())
            {
                if (id.HasValue)
                {
                    var donemProjesi = entities.DonemProjesis.First(p => p.UniqueID == id && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
                    var ogrenimTip = entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == donemProjesi.OgrenimTipKod);
                    model.UniqueID = donemProjesi.UniqueID;
                    model.BasvuruTarihi = donemProjesi.BasvuruTarihi;
                    model.KullaniciID = donemProjesi.KullaniciID;
                    model.AdSoyad = donemProjesi.Kullanicilar.Ad + " " + donemProjesi.Kullanicilar.Soyad;
                    model.OgrenciNo = donemProjesi.OgrenciNo;

                    model.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                    model.AnabilimdaliAdi = donemProjesi.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = donemProjesi.Programlar.ProgramAdi;
                }
                else
                {

                    var kul = entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                    var ogrenimTip = entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);


                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = UserIdentity.Current.Id;
                    model.AdSoyad = kul.Ad + " " + kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;

                    model.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                    model.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = kul.Programlar.ProgramAdi;
                }

                return model;
            }
        }
        public static DpBasvuruDetayDto GetSecilenBasvuruDetay(Guid donemProjesiUniqueId, Guid? uniqueId)
        {
            var model = new DpBasvuruDetayDto();
            using (var entities = new LubsDbEntities())
            {


                int? danismanId = null;
                var kayitYetki = RoleNames.DonemProjesiEnstituBasvuruOnayYetkisi.InRoleCurrent();
                var juriOnerileriYetkili = RoleNames.DonemProjesiEykDaOnay.InRoleCurrent() || RoleNames.DonemProjesiEykYaGonder.InRoleCurrent();
                if (kayitYetki && !juriOnerileriYetkili)
                    danismanId = UserIdentity.Current.Id;
                var basvuru = entities.DonemProjesis.First(p => p.UniqueID == donemProjesiUniqueId);

                var ogrenciObsBilgi = KullanicilarBus.OgrenciBilgisiGuncelleObs(basvuru.KullaniciID);
                //ana başvurudaki danışman ile aktif danışman uyuşmuyor ise aktif danışmanı ana başvuruya eşleştir.
                if (ogrenciObsBilgi.KayitVar && ogrenciObsBilgi.AktifDanismanID.HasValue)
                {
                    if (basvuru.OgrenciNo == ogrenciObsBilgi.OgrenciInfo.OGR_NO && basvuru.TezDanismanID != ogrenciObsBilgi.AktifDanismanID)
                    {
                        basvuru.TezDanismanID = ogrenciObsBilgi.AktifDanismanID.Value;
                        entities.SaveChanges();
                    }
                }

                var enstitu = entities.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);
                var sonBasvuru = basvuru.DonemProjesiBasvurus.OrderByDescending(o => o.DonemProjesiBasvuruID).FirstOrDefault();
                model.DonemProjesiBasvurus = basvuru.DonemProjesiBasvurus.ToList().Where(p => p.TezDanismanID == (danismanId ?? p.TezDanismanID)).Select(s => new DonemProjesiBasvuruDto
                {
                    DonemProjesiID = s.DonemProjesiID,
                    DonemProjesiBasvuruID = s.DonemProjesiBasvuruID,
                    IsSonBasvuru = sonBasvuru == null || s.DonemProjesiBasvuruID == sonBasvuru.DonemProjesiBasvuruID,
                    UniqueID = s.UniqueID,
                    OkuduguDonemNo = s.OkuduguDonemNo,
                    BasvuruYil = s.BasvuruYil,
                    BasvuruDonemID = s.BasvuruDonemID,
                    DonemAdi = s.BasvuruYil + "/" + (s.BasvuruYil + 1) + " " + s.Donemler.DonemAdi,
                    BasvuruTarihi = s.BasvuruTarihi,
                    ProjeBasligi = s.ProjeBasligi,
                    ProjeOzeti = s.ProjeOzeti,
                    TezDanismanID = s.TezDanismanID,
                    DanismanAdi = s.Kullanicilar.Unvanlar.UnvanAdi + " " + s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                    TezDanismaniUserKey = s.Kullanicilar.UserKey,
                    DonemProjesiEnstituOnayDurumID = s.DonemProjesiEnstituOnayDurumID,
                    DonemProjesiEnstituOnayDurumlari = s.DonemProjesiEnstituOnayDurumlari,
                    EnstituOnayTarihi = s.EnstituOnayTarihi,
                    EnstituOnayAciklama = s.EnstituOnayAciklama,
                    IsDanismanOnay = s.IsDanismanOnay,
                    DanismanOnayTarihi = s.DanismanOnayTarihi,
                    DanismanOnayAciklama = s.DanismanOnayAciklama,
                    TekKaynakOrani = s.TekKaynakOrani,
                    ToplamKaynakOrani = s.ToplamKaynakOrani,
                    IntihalRaporuDosyaYolu = s.IntihalRaporuDosyaYolu,
                    IntihalRaporuDosyaAdi = s.IntihalRaporuDosyaAdi,
                    EYKYaGonderildi = s.EYKYaGonderildi,
                    EYKYaGonderildiIslemYapanID = s.EYKYaGonderildiIslemYapanID,
                    EYKYaGonderildiIslemTarihi = s.EYKYaGonderildiIslemTarihi,
                    EYKYaGonderimDurumAciklamasi = s.EYKYaGonderimDurumAciklamasi,
                    EYKYaHazirlandi = s.EYKYaHazirlandi,
                    EYKYaHazirlandiIslemTarihi = s.EYKYaHazirlandiIslemTarihi,
                    EYKYaHazirlandiIslemYapanID = s.EYKYaHazirlandiIslemYapanID,
                    EYKDaOnaylandi = s.EYKDaOnaylandi,
                    EYKDaOnaylandiIslemYapanID = s.EYKDaOnaylandiIslemYapanID,
                    EYKTarihi = s.EYKTarihi,
                    EYKDaOnaylandiIslemTarihi = s.EYKDaOnaylandiIslemTarihi,
                    EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi,
                    IsOyBirligiOrCoklugu = s.IsOyBirligiOrCoklugu,

                    DonemProjesiDurumID = s.DonemProjesiDurumID,
                    DonemProjesiDurumlari = s.DonemProjesiDurumlari,
                    DonemProjesiJuriOnayDurumID = s.DonemProjesiJuriOnayDurumID,
                    DonemProjesiJuriOnayDurumlari = s.DonemProjesiJuriOnayDurumlari,
                    SRModel = (from sR in s.SRTalepleris
                               join tt in entities.SRTalepTipleris on sR.SRTalepTipID equals tt.SRTalepTipID
                               join sal in entities.SRSalonlars on sR.SRSalonID equals sal.SRSalonID into def1
                               from defSl in def1.DefaultIfEmpty()
                               join hg in entities.HaftaGunleris on sR.HaftaGunID equals hg.HaftaGunID
                               join d in entities.SRDurumlaris on sR.SRDurumID equals d.SRDurumID
                               select new FrTalepler
                               {
                                   SRTalepID = sR.SRTalepID,
                                   TalepYapanID = sR.TalepYapanID,
                                   TalepTipAdi = tt.TalepTipAdi,
                                   SRTalepTipID = sR.SRTalepTipID,
                                   SRSalonID = sR.SRSalonID,
                                   IsOnline = sR.IsOnline,
                                   SalonAdi = sR.SRSalonID.HasValue ? defSl.SalonAdi : sR.SalonAdi,
                                   Tarih = sR.Tarih,
                                   HaftaGunID = sR.HaftaGunID,
                                   HaftaGunAdi = hg.HaftaGunAdi,
                                   BasSaat = sR.BasSaat,
                                   BitSaat = sR.BitSaat,
                                   SRDurumID = sR.SRDurumID,
                                   DurumAdi = d.DurumAdi,
                                   DurumListeAdi = d.DurumAdi,
                                   ClassName = d.ClassName,
                                   Color = d.Color,
                                   SRDurumAciklamasi = sR.SRDurumAciklamasi,
                                   IslemTarihi = s.IslemTarihi,
                                   IslemYapanID = s.IslemYapanID,
                                   IslemYapanIP = s.IslemYapanIP
                               }).FirstOrDefault(),
                    DonemProjesiDurumDto = new DpBasvuruDurumDto
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
                        DonemProjesiJuriOnayDurumAdi = s.DonemProjesiJuriOnayDurumID.HasValue ? s.DonemProjesiJuriOnayDurumlari.JuriOnayDurumAdi : "",
                        IsJuriOlusturuldu = s.DonemProjesiJurileris.Any(),
                        IsSrTalebiYapildi = s.SRTalepleris.Any()

                    },
                    DonemProjesiJurileris = entities.DonemProjesiJurileris.Include("DonemProjesiJuriOnayDurumlari").Where(p => p.DonemProjesiBasvuruID == s.DonemProjesiBasvuruID).OrderBy(t => t.IsTezDanismani ? 1 : 2)
                                                                       .ThenBy(t => t.RowNum).ToList()
                }).OrderByDescending(o => o.BasvuruTarihi).ToList();
                var aktifTezDanismani = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == basvuru.TezDanismanID);
                model.IsYeniBasvuruYapilabilir = basvuru.IsYeniBasvuruYapilabilir;
                model.UniqueID = basvuru.UniqueID;
                model.TezDanismaniUserKey = aktifTezDanismani.UserKey;
                model.TezDanismanBilgiEslesen = aktifTezDanismani.Unvanlar.UnvanAdi + " " + aktifTezDanismani.Ad + " " +
                                                aktifTezDanismani.Soyad;
                model.TezDanismanID = basvuru.TezDanismanID;
                model.DonemProjesiID = basvuru.DonemProjesiID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.ResimAdi = basvuru.Kullanicilar.ResimAdi;
                model.Ad = basvuru.Kullanicilar.Ad;
                model.Soyad = basvuru.Kullanicilar.Soyad;
                model.TcKimlikNo = basvuru.Kullanicilar.TcKimlikNo;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                var ogrenimTipi = entities.OgrenimTipleris.First(f =>
                    f.EnstituKod == enstitu.EnstituKod && f.OgrenimTipKod == basvuru.OgrenimTipKod);
                model.OgrenimTipAdi = ogrenimTipi.OgrenimTipAdi;
                model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.EnstituAdi = enstitu.EnstituAd;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;

                model.DegerlendirenUniqueID = uniqueId;
            }
            return model;
        }

        public static void DonemProjesiDurumSet(int donemProjesiBasvuruId)
        {
            using (var entities = new LubsDbEntities())
            {
                var donemProjesiBasvuru =
                    entities.DonemProjesiBasvurus.First(f => f.DonemProjesiBasvuruID == donemProjesiBasvuruId);
                donemProjesiBasvuru.DonemProjesi.IsYeniBasvuruYapilabilir = donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID == DonemProjesiEnstituOnayDurumEnum.IptalEdildi;

                if (!donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID.HasValue || donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID != DonemProjesiEnstituOnayDurumEnum.KabulEdildi)
                {
                    donemProjesiBasvuru.DonemProjesiDurumID = DonemProjesiDurumEnum.EnstituOnaySureci;
                    donemProjesiBasvuru.DonemProjesi.IsYeniBasvuruYapilabilir = donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID == DonemProjesiEnstituOnayDurumEnum.IptalEdildi;
                    entities.SaveChanges();
                    return;
                }
                donemProjesiBasvuru.DonemProjesi.IsYeniBasvuruYapilabilir = donemProjesiBasvuru.IsDanismanOnay == false;

                if (!donemProjesiBasvuru.IsDanismanOnay.HasValue || donemProjesiBasvuru.IsDanismanOnay == false)
                {
                    donemProjesiBasvuru.DonemProjesiDurumID = DonemProjesiDurumEnum.DanismanOnaySureci;
                    donemProjesiBasvuru.DonemProjesi.IsYeniBasvuruYapilabilir = donemProjesiBasvuru.IsDanismanOnay == false;
                    entities.SaveChanges();
                    return;
                }
                if (!donemProjesiBasvuru.DonemProjesiJurileris.Any())
                {
                    donemProjesiBasvuru.DonemProjesiDurumID = DonemProjesiDurumEnum.JuriSinavOlusturmaSureci;
                    entities.SaveChanges();
                    return;
                }
                if (!donemProjesiBasvuru.DonemProjesiJuriOnayDurumID.HasValue)
                {
                    donemProjesiBasvuru.DonemProjesiDurumID = donemProjesiBasvuru.SRTalepleris.Any() ? DonemProjesiDurumEnum.SinavDegerlendirmeSureci : DonemProjesiDurumEnum.JuriSinavOlusturmaSureci;
                    entities.SaveChanges();
                    return;
                }
                donemProjesiBasvuru.DonemProjesi.IsYeniBasvuruYapilabilir = donemProjesiBasvuru.EYKYaGonderildi == false || donemProjesiBasvuru.EYKDaOnaylandi == false;
                donemProjesiBasvuru.DonemProjesiDurumID = DonemProjesiDurumEnum.EnstituYonetimKuruluSureci;
                entities.SaveChanges();
            }
        }
        public static void AddOrUpdateDonemProjesi(KmDonemProjesiDto model)
        {
            using (var entities = new LubsDbEntities())
            {
                var kullanici = entities.Kullanicilars.First(f => f.KullaniciID == model.KullaniciID);

                var donemProjesi = entities.DonemProjesis.FirstOrDefault(p => p.EnstituKod == model.EnstituKod
                                                                                         && p.KullaniciID == model.KullaniciID
                                                                                         && (p.UniqueID == model.UniqueID || (p.OgrenciNo == kullanici.OgrenciNo && p.ProgramKod == kullanici.ProgramKod && p.OgrenimTipKod == kullanici.OgrenimTipKod)));

                if (donemProjesi != null)
                {

                    donemProjesi.BasvuruTarihi = DateTime.Now;
                    donemProjesi.OgrenimTipKod = kullanici.OgrenimTipKod.Value;
                    donemProjesi.OgrenciNo = kullanici.OgrenciNo;
                    donemProjesi.ProgramKod = kullanici.ProgramKod;
                    donemProjesi.KayitOgretimYiliBaslangic = kullanici.KayitYilBaslangic.Value;
                    donemProjesi.KayitOgretimYiliDonemID = kullanici.KayitDonemID.Value;
                    donemProjesi.KayitTarihi = kullanici.KayitTarihi.Value;
                    donemProjesi.TezDanismanID = kullanici.DanismanID.Value;
                    donemProjesi.IslemTarihi = DateTime.Now;
                    donemProjesi.IslemYapanIP = UserIdentity.Ip;
                    donemProjesi.IslemYapanID = UserIdentity.Current.Id;

                    var sonDonemProjesi = donemProjesi.DonemProjesiBasvurus.LastOrDefault();
                    donemProjesi.IsYeniBasvuruYapilabilir = sonDonemProjesi == null ||
                                                            sonDonemProjesi.DonemProjesiEnstituOnayDurumlari
                                                                .IsTekrarBasvuruYapabilir ||
                                                            sonDonemProjesi.EYKYaGonderildi == false ||
                                                            sonDonemProjesi.EYKDaOnaylandi == false;
                    entities.SaveChanges();
                    LogIslemleri.LogEkle("DonemProjesiBasvuru", LogCrudType.Update, donemProjesi.ToJson());

                }
                else
                {

                    var kayit = entities.DonemProjesis.Add(new DonemProjesi
                    {
                        UniqueID = Guid.NewGuid(),
                        EnstituKod = model.EnstituKod,
                        BasvuruTarihi = DateTime.Now,
                        KullaniciID = UserIdentity.Current.Id,
                        OgrenimTipKod = kullanici.OgrenimTipKod.Value,
                        OgrenciNo = kullanici.OgrenciNo,
                        ProgramKod = kullanici.ProgramKod,
                        KayitOgretimYiliBaslangic = kullanici.KayitYilBaslangic.Value,
                        KayitOgretimYiliDonemID = kullanici.KayitDonemID.Value,
                        KayitTarihi = kullanici.KayitTarihi.Value,
                        TezDanismanID = kullanici.DanismanID.Value,
                        IsYeniBasvuruYapilabilir = true,
                        IslemTarihi = DateTime.Now,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemYapanID = UserIdentity.Current.Id

                    });
                    entities.SaveChanges();
                    LogIslemleri.LogEkle("DonemProjesiBasvuru", LogCrudType.Insert, kayit.ToJson());
                }
            }
        }

        public static List<CmbStringDto> CmbDpDonemListe(string enstituKod, bool bosSecimVar = false)
        {

            using (var entities = new LubsDbEntities())
            {
                var donems = entities.DonemProjesiBasvurus.Select(s => new { s.BasvuruYil, s.BasvuruDonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.BasvuruYil).ThenByDescending(t => t.BasvuruDonemID).Select(s => new CmbStringDto
                    {
                        Value = s.BasvuruYil + "" + s.BasvuruDonemID,
                        Caption = s.BasvuruYil + "/" + (s.BasvuruYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }
        public static List<CmbIntDto> CmbDpDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.BasvuruTamamlanmadi, Caption = "Henüz Başvuru Tamamlanmadı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EnstituOnayiBekliyor, Caption = "Enstitü Onayı Bekleniyor" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EnstituTarafindanOnaylandi, Caption = "Enstitü Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EnstituTarafindanOnaylanmadi, Caption = "Enstitü Tarafından Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.DanismanOnayiBekliyor, Caption = "Danışman Onayı Bekleniyor" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.DanismanTarafindanOnaylandi, Caption = "Danışman Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.DanismanTarafindanOnaylanmadi, Caption = "Danışman Tarafından Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.JuriSinavOlusturmaSureci, Caption = "Jüri/Sınav Oluşturma Sürecinde" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.SinavDegerlendirmeSureci, Caption = "Sınav Değerlendirme Sürecinde" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EykYaGonderimOnayiBekleniyor, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EykYaGonderimiOnaylandi, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EykYaGonderimiOnaylanmadi, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EykYaHazirlandi, Caption = "EYK'ya Hazırlandı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EykDaOnayBekleniyor, Caption = "EYK'da Onay Bekleniyor" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EykDaOnaylandi, Caption = "EYK'da Onaylandı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.EykDaOnaylanmadi, Caption = "EYK'da Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.BasariliOlanlar, Caption = "Başarılı Olanlar" });
            dct.Add(new CmbIntDto { Value = DpBasvuruDurumEnum.BasarisizOlanlar, Caption = "Başarısız Olanlar" });
            return dct;
        }
        public static List<CmbIntDto> GetCmbFilterDpAnabilimDallari(string enstituKod, bool bosSecimVar = false)
        {
            using (var entities = new LubsDbEntities())
            {
                var anabilimDaliIds = entities.DonemProjesis
                    .Where(p => p.EnstituKod == enstituKod).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = entities.AnabilimDallaris.Where(p => anabilimDaliIds.Contains(p.AnabilimDaliID))
                    .Select(s => new { s.AnabilimDaliID, s.AnabilimDaliAdi }).OrderBy(o => o.AnabilimDaliAdi).Select(
                        s =>
                            new CmbIntDto { Value = s.AnabilimDaliID, Caption = s.AnabilimDaliAdi }
                    ).ToList();
                if (bosSecimVar) anabilimDallaris.Insert(0, new CmbIntDto { Value = null, Caption = "" });

                return anabilimDallaris;
            }
        }

        public static MmMessage SendMailBasvuruBilgisi(int donemProjesiBasvuruId)
        {
            return MailSenderDp.SendMailBasvuruBilgisi(donemProjesiBasvuruId);
        }
        public static MmMessage SendMailEnstituOnay(int donemProjesiBasvuruId)
        {
            return MailSenderDp.SendMailEnstituOnay(donemProjesiBasvuruId);
        }
        public static MmMessage SendMailDanismanOnay(int donemProjesiBasvuruId)
        {
            return MailSenderDp.SendMailYurutucuOnay(donemProjesiBasvuruId);
        }
        public static MmMessage SendMailSinavBilgisi(int srTalepId)
        {
            return MailSenderDp.SendMailSinavBilgisi(srTalepId);
        }
        public static MmMessage SendMailDegerlendirmeLink(int donemProjesiBasvuruId, Guid? uniqueId)
        {
            return MailSenderDp.SendMailDegerlendirmeLink(donemProjesiBasvuruId, uniqueId);
        }

        public static MmMessage SendMailSinavSonucBilgisi(int donemProjesiBasvuruId)
        {
            return MailSenderDp.SendMailSinavSonucBilgisi(donemProjesiBasvuruId);
        }


    }
}