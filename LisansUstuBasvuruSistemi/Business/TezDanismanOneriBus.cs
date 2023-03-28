using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class TezDanismanOneriBus
    {
        public static TdoBasvuruDetayDto GetSecilenBasvuruTdoDetay(int tdoBasvuruId, Guid? uniqueId)
        {
            var model = new TdoBasvuruDetayDto() { TDOBasvuruID = tdoBasvuruId };
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var isYoneticiYetki = RoleNames.TdoeyKdaOnayYetkisi.InRoleCurrent();
                var isDanismanOnayYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();
                var basvuru = db.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId);
                KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(basvuru.KullaniciID);
                var enstitu = db.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);
                var showAllRow = basvuru.KullaniciID == UserIdentity.Current.Id || RoleNames.TdoeyKyaGonderimYetkisi.InRoleCurrent() || RoleNames.TdoeyKdaOnayYetkisi.InRoleCurrent();
                tekrarYukle:

                model.EnstituKod = basvuru.EnstituKod;
                model.TDOBasvuruDanisman = basvuru.TDOBasvuruDanisman;
                model.TDOBasvuruDanismanList = (from s in basvuru.TDOBasvuruDanismen
                                                select new TdoBasvuruDanismanDto
                                                {
                                                    UniqueID = s.UniqueID,
                                                    IsObsData = s.IsObsData,
                                                    TDODanismanTalepTipID = s.TDODanismanTalepTipID,
                                                    TalepTipAdi = s.TDODanismanTalepTipleri.TalepTipAdi,
                                                    DonemBaslangicYil = s.DonemBaslangicYil,
                                                    DonemID = s.DonemID,
                                                    DonemAdi = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + (s.DonemID == 1 ? "Güz" : "Bahar"),
                                                    FormKodu = s.FormKodu,
                                                    TDOBasvuruDanismanID = s.TDOBasvuruDanismanID,
                                                    TDOBasvuruID = s.TDOBasvuruID,
                                                    BasvuruTarihi = s.BasvuruTarihi,
                                                    IsTezDiliTr = s.IsTezDiliTr,
                                                    TezBaslikTr = s.TezBaslikTr,
                                                    TezBaslikEn = s.TezBaslikEn,
                                                    IsYeniTezDiliTr = s.IsYeniTezDiliTr,
                                                    YeniTezBaslikTr = s.YeniTezBaslikTr,
                                                    YeniTezBaslikEn = s.YeniTezBaslikEn,
                                                    SinavAdi = s.SinavAdi,
                                                    SinavPuani = s.SinavPuani,
                                                    SinavYili = s.SinavYili,
                                                    VarolanTezDanismanID = s.VarolanTezDanismanID,
                                                    VarolanDanismanOnayladi = s.VarolanDanismanOnayladi,
                                                    VarolanDanismanOnayTarihi = s.VarolanDanismanOnayTarihi,
                                                    VarolanDanismanOnaylanmadiAciklama = s.VarolanDanismanOnaylanmadiAciklama,

                                                    TDUniversiteAdi = s.TDUniversiteAdi,
                                                    TezDanismanID = s.TezDanismanID,
                                                    TDAdSoyad = s.TDAdSoyad,
                                                    TDUnvanAdi = s.TDUnvanAdi,
                                                    TDAnabilimDaliAdi = s.TDAnabilimDaliAdi,
                                                    TDProgramAdi = s.TDProgramAdi,
                                                    TDSinavTipID = s.TDSinavTipID,
                                                    TDSinavAdi = s.TDSinavAdi,
                                                    TDSinavYili = s.TDSinavYili,
                                                    TDSinavPuani = s.TDSinavPuani,
                                                    TDOgrenciSayisiDR = s.TDOgrenciSayisiDR,
                                                    TDOgrenciSayisiYL = s.TDOgrenciSayisiYL,
                                                    TDTezSayisiDR = s.TDTezSayisiDR,
                                                    TDTezSayisiYL = s.TDTezSayisiYL,
                                                    DanismanOnayladi = s.DanismanOnayladi,
                                                    DanismanOnayTarihi = s.DanismanOnayTarihi,
                                                    DanismanOnaylanmadiAciklama = s.DanismanOnaylanmadiAciklama,

                                                    EYKYaGonderildi = s.EYKYaGonderildi,
                                                    EYKYaGonderildiIslemTarihi = s.EYKYaGonderildiIslemTarihi,
                                                    EYKYaGonderildiIslemYapanID = s.EYKYaGonderildiIslemYapanID,

                                                    EYKDaOnaylandi = s.EYKDaOnaylandi,
                                                    EYKDaOnaylandiIslemYapanID = s.EYKDaOnaylandiIslemYapanID,
                                                    EYKDaOnaylandiOnayTarihi = s.EYKDaOnaylandiOnayTarihi,
                                                    EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi,
                                                    IslemTarihi = s.IslemTarihi,
                                                    IslemYapanID = s.IslemYapanID,
                                                    IslemYapanIP = s.IslemYapanIP,
                                                    TDOBasvuruEsDanismen = basvuru.TDOBasvuruDanismen.SelectMany(sm => sm.TDOBasvuruEsDanismen).OrderByDescending(oe => oe.TDOBasvuruEsDanismanID).ToList(),
                                                    EsDanismanBilgi = basvuru.TDOBasvuruDanismen.SelectMany(sm => sm.TDOBasvuruEsDanismen).OrderByDescending(o => o.TDOBasvuruEsDanismanID).FirstOrDefault()

                                                }).Where(p => p.TezDanismanID == (showAllRow ? p.TezDanismanID : UserIdentity.Current.Id) || p.VarolanTezDanismanID == (showAllRow ? p.VarolanTezDanismanID : UserIdentity.Current.Id)).OrderByDescending(o => o.BasvuruTarihi).ToList();
                if (model.TDOBasvuruDanismanList.Any() && !basvuru.AktifTDOBasvuruDanismanID.HasValue)
                {
                    basvuru.AktifTDOBasvuruDanismanID = model.TDOBasvuruDanismanList.Last().TDOBasvuruDanismanID;
                    db.SaveChanges();
                }

                var kulIds = model.TDOBasvuruDanismanList.Select(s => s.VarolanTezDanismanID).ToList();
                var kulls = db.Kullanicilars.Where(p => kulIds.Contains(p.KullaniciID)).ToList();

                var inx = 0;
                foreach (var item in model.TDOBasvuruDanismanList.OrderByDescending(o => o.TDOBasvuruDanismanID))
                {
                    inx++;
                    if (item.VarolanTezDanismanID.HasValue)
                    {
                        var kul = kulls.First(p => p.KullaniciID == item.VarolanTezDanismanID);
                        item.VarolanDanismanAd = kul.Unvanlar.UnvanAdi + " " + kul.Ad + " " + kul.Soyad;
                    }

                    if (inx == 1)
                    {
                        item.IsYeniEsDanismanOneriOrDegisiklik = item.TDOBasvuruEsDanismen.All(ae => ae.EYKDaOnaylandi != true);
                        if (item.IsYeniEsDanismanOneriOrDegisiklik)
                        {
                            item.TdoEsBasvurusuYapabilir = (item.EsDanismanBilgi == null ||
                                                            item.EsDanismanBilgi.EYKYaGonderildi == false ||
                                                            item.EsDanismanBilgi.EYKDaOnaylandi == false);
                        }
                        else
                        {
                            item.TdoEsBasvurusuYapabilir = item.EsDanismanBilgi == null || (item.EsDanismanBilgi.EYKYaGonderildi == false ||
                                item.EsDanismanBilgi.EYKDaOnaylandi.HasValue);
                        }
                        if (item.TdoEsBasvurusuYapabilir)
                        {
                            if (model.TDOBasvuruDanisman != null)
                            {
                                item.TdoEsBasvurusuYapabilir = isYoneticiYetki || model.TDOBasvuruDanisman.TezDanismanID == UserIdentity.Current.Id;
                            }
                        }

                    }
                }
                model.AktifTDOBasvuruDanismanID = basvuru.AktifTDOBasvuruDanismanID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliDonemID.HasValue ? (basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi) : "";
                model.KullaniciTipID = basvuru.KullaniciTipID;
                model.ResimAdi = basvuru.ResimAdi;
                model.Ad = basvuru.Ad;
                model.Soyad = basvuru.Soyad;
                model.TcKimlikNo = basvuru.TcKimlikNo;
                model.UyrukKod = basvuru.UyrukKod;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.First(p => p.EnstituKod == basvuru.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.AktifTDOBasvuruDanismanID.HasValue
                    ? basvuru.TDOBasvuruDanisman.TDProgramKod
                    : null;
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

                if (basvuru.Kullanicilar.DanismanID.HasValue && model.TDOBasvuruDanismanList.All(a => a.TezDanismanID != basvuru.Kullanicilar.DanismanID))
                {
                    var eslestirildi = ObsDanismanBasvuruBilgiEslestir(model.KullaniciID, model.TDOBasvuruID);
                    if (eslestirildi.Item1)
                    {
                        basvuru = db.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId);
                        goto tekrarYukle;
                    }

                }
                if (model.TDOBasvuruDanismanList.Any())
                {
                    var firstRow = model.TDOBasvuruDanismanList.First();
                    firstRow.VarolanDanismanGozuksun = firstRow.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi || firstRow.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi;
                    firstRow.VarolanDanismanOnayIslemiAcik = (isDanismanOnayYetki && firstRow.VarolanTezDanismanID == UserIdentity.Current.Id || isYoneticiYetki) && !firstRow.IsObsData && !firstRow.DanismanOnayladi.HasValue;
                    if (firstRow.VarolanDanismanGozuksun)
                    {
                        firstRow.YeniDanismanOnayIslemiAcik = firstRow.VarolanDanismanOnayladi == true && (isDanismanOnayYetki && firstRow.TezDanismanID == UserIdentity.Current.Id || isYoneticiYetki) && !firstRow.IsObsData && !firstRow.EYKYaGonderildi.HasValue;
                    }
                    else
                    {
                        firstRow.YeniDanismanOnayIslemiAcik = (isDanismanOnayYetki && firstRow.TezDanismanID == UserIdentity.Current.Id || isYoneticiYetki) && !firstRow.IsObsData && !firstRow.EYKYaGonderildi.HasValue;

                    }
                    firstRow.IsYeniTezBasligiGozuksun = firstRow.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi || firstRow.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi;

                    firstRow.IsDuzeltSilYapabilir = firstRow.DanismanOnayladi != true && firstRow.VarolanDanismanOnayladi != true;
                }


                TDOBasvuruEsDanisman lastEsBasvuru = null;
                if (basvuru.TDOBasvuruDanisman != null)
                    lastEsBasvuru = basvuru.TDOBasvuruDanisman.TDOBasvuruEsDanismen
                        .OrderByDescending(o => o.TDOBasvuruEsDanismanID).FirstOrDefault();
                model.IsYeniDanismanOneriOrDegisiklik = model.TDOBasvuruDanisman == null || model.TDOBasvuruDanismanList.All(a => a.EYKDaOnaylandi != true);
                if (model.IsYeniDanismanOneriOrDegisiklik)
                {
                    model.TdoBasvurusuYapabilir = (model.TDOBasvuruDanisman == null || model.TDOBasvuruDanisman.DanismanOnayladi == false || model.TDOBasvuruDanisman.EYKYaGonderildi == false || model.TDOBasvuruDanisman.EYKDaOnaylandi == false);


                }
                else
                {
                    model.TdoBasvurusuYapabilir = (model.TDOBasvuruDanisman.VarolanDanismanOnayladi == false || model.TDOBasvuruDanisman.DanismanOnayladi == false || model.TDOBasvuruDanisman.EYKYaGonderildi == false || model.TDOBasvuruDanisman.EYKDaOnaylandi.HasValue);

                    if (model.TdoBasvurusuYapabilir) model.TdoBasvurusuYapabilir = (lastEsBasvuru == null || lastEsBasvuru.EYKYaGonderildi == false || lastEsBasvuru.EYKDaOnaylandi.HasValue);
                    if (model.TdoBasvurusuYapabilir)
                    {
                        if (model.TDOBasvuruDanisman != null)
                        {
                            model.TdoBasvurusuYapabilir = isYoneticiYetki || model.TDOBasvuruDanisman.TezDanismanID == UserIdentity.Current.Id || model.KullaniciID == UserIdentity.Current.Id;
                        }
                    }
                }


            }
            return model;

        }


        public static Tuple<bool, string> ObsDanismanBasvuruBilgiEslestir(int kullaniciId, int? tDoBasvuruId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var ogrenciInfo = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(kullaniciId);

                if (ogrenciInfo.DanismanInfo != null)
                {

                    if (ogrenciInfo.IsDanismanHesabiBulunamadi)
                    {
                        var hataMesaji = "Başvuru işlemini yapabilmeniz için varolan danışmanınız '" +
                                      ogrenciInfo.DanismanInfo.UNVAN_AD + " " + ogrenciInfo.DanismanInfo.AD + " " +
                                      ogrenciInfo.DanismanInfo.SOYAD +
                                      "' lisansüstü.yildiz.edu.tr sistemine üye olması gerekmektedir.";
                        return Tuple.Create(false, hataMesaji);
                    }

                    var danismanBasvurusuVar = db.TDOBasvuruDanismen.Any(p => p.TDOBasvuru.KullaniciID == kullaniciId && p.TDOBasvuru.EnstituKod == p.TDOBasvuru.Kullanicilar.EnstituKod);

                    if (!danismanBasvurusuVar)
                    {
                        var kModel = new TDOBasvuruDanisman
                        {
                            IsObsData = true,
                            BasvuruTarihi = DateTime.Now
                        };
                        var donemBilgi = kModel.BasvuruTarihi.ToAraRaporDonemBilgi();
                        kModel.DonemBaslangicYil = donemBilgi.BaslangicYil;
                        kModel.DonemID = donemBilgi.DonemID;
                        var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                        while (db.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
                        {
                            formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                        }
                        kModel.UniqueID = Guid.NewGuid();
                        kModel.FormKodu = formKodu;


                        kModel.IsTezDiliTr = ogrenciInfo.IsTezDiliTr;

                        kModel.TezBaslikTr = ogrenciInfo.OgrenciTez.TEZ_BASLIK;
                        kModel.TezBaslikEn = ogrenciInfo.OgrenciTez.TEZ_BASLIK_ENG.IsNullOrWhiteSpace() ? ogrenciInfo.OgrenciTez.TEZ_BASLIK : ogrenciInfo.OgrenciTez.TEZ_BASLIK_ENG;
                        kModel.TezDanismanID = ogrenciInfo.AktifDanismanID.Value;
                        kModel.TDAdSoyad = ogrenciInfo.DanismanInfo.AD + " " + ogrenciInfo.DanismanInfo.SOYAD;
                        kModel.TDUnvanAdi = ogrenciInfo.DanismanInfo.UNVAN_AD.ToUpper();

                        kModel.TDProgramAdi = ogrenciInfo.DanismanInfo.PROGRAM_AD;
                        kModel.TDAnabilimDaliAdi = ogrenciInfo.DanismanInfo.ANABILIMDALI_AD;

                        kModel.TDOgrenciSayisiYL = ogrenciInfo.DanismanInfo.DANISMAN_OLUNAN_YL_SAYI1.ToIntObj();
                        kModel.TDOgrenciSayisiDR = ogrenciInfo.DanismanInfo.DANISMAN_OLUNAN_DR_SAYI1.ToIntObj();
                        kModel.TDTezSayisiYL = ogrenciInfo.DanismanInfo.DANISMAN_MEZUN_YL_SAYI1.ToIntObj();
                        kModel.TDTezSayisiDR = ogrenciInfo.DanismanInfo.DANISMAN_MEZUN_DR_SAYI1.ToIntObj();


                        kModel.DanismanOnayladi = true;
                        kModel.EYKYaGonderildi = true;
                        kModel.EYKDaOnaylandi = true;

                        kModel.TDODanismanTalepTipID = TDODanismanTalepTip.TezDanismaniOnerisi;

                        kModel.IslemTarihi = DateTime.Now;
                        kModel.IslemYapanID = UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;

                        if (tDoBasvuruId.HasValue)
                        {
                            var tDoBasvuru = db.TDOBasvurus.First(p => p.TDOBasvuruID == tDoBasvuruId);
                            kModel.TDOBasvuruID = tDoBasvuruId.Value;
                            var added = db.TDOBasvuruDanismen.Add(kModel);
                            tDoBasvuru.AktifTDOBasvuruDanismanID = added.TDOBasvuruDanismanID;
                        }

                        db.SaveChanges();
                        return Tuple.Create(false, "");
                    }

                }
                return Tuple.Create(false, "");

            }
        }

        public static MmMessage GetAktifTezDanismanOneriSurecKontrol(string enstituKod, int? kullaniciId, int? tdoBasvuruId = null)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.TdoGelenBasvuruKayit.InRoleCurrent();
                if (tdoBasvuruId.HasValue)
                {
                    var basvuru = db.TDOBasvurus.FirstOrDefault(p => p.TDOBasvuruID == tdoBasvuruId.Value);
                    if (basvuru == null)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                        if (kayitYetki == false) SistemBilgilendirmeBus.SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Tez danışmanı öneri Başvuru ID:" + tdoBasvuruId, "TDO Başvuru Düzelt", LogType.Uyarı);
                    }
                    else
                    {
                        if (basvuru.EnstituKod != enstituKod)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Seçilen Tez danışmanı öneri başvurusu Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Tez danışmanı öneri Başvuru Enstitü Kod:" + basvuru.EnstituKod + " \r\n Aktif Enstitü Kod:" + enstituKod + " \r\n Çağrılan Tez İzleme Başvuru ID:" + basvuru.TDOBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Tez danışmanı öneri Düzelt", LogType.Uyarı);
                            enstituKod = basvuru.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitüde yetkili değilsiniz.");
                            string message = "Bu enstitüye ait Tez danışmanı öneri başvurusu güncellemeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: " + basvuru.TDOBasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Başvuru Düzelt", LogType.Saldırı);
                        }
                        else if (!TdoAyar.BasvurusuAcikmi.GetAyarTdo(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu işlem için yetkili değilsiniz.");
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez danışmanı öneri başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Tez İzleme Başvuru ID:" + basvuru.TDOBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Tez danışmanı öneri Düzelt", LogType.Saldırı);
                        }

                    }
                }
                else
                {
                    msg.IsSuccess = TdoAyar.BasvurusuAcikmi.GetAyarTdo(enstituKod, "false").ToBoolean().Value;
                    if (kullaniciId.HasValue == false) kullaniciId = UserIdentity.Current.Id;
                    else if (kullaniciId != UserIdentity.Current.Id && RoleNames.KullaniciAdinaTezDanismanOnerisiYap.InRoleCurrent() == false && UserIdentity.Current.IsAdmin == false)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + kullaniciId + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, "Tez danışmanı önerisi Yap", LogType.Saldırı);
                        kullaniciId = UserIdentity.Current.Id;
                    }
                    var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId.Value);
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Başvuru süreci kapalı.");
                    }
                    else
                    {
                        if (kul.YtuOgrencisi && kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci && kul.OgrenimTipKod.IsDoktora())
                        {
                            var aktifDevamEdenBasvuruVar = db.TDOBasvurus.Any(p => p.KullaniciID == kullaniciId && p.OgrenciNo == kul.OgrenciNo && p.TDOBasvuruID != tdoBasvuruId.Value);//aynı başvuru sürecindeki başvurular baz alınsın
                            if (aktifDevamEdenBasvuruVar)// toplam başvuru kontrol
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Aktif olarak devam eden bir Tez danışmanı öneri süreciniz bulunuyor. Yeni başvuru yapamazsınız.Tez danışmanı önerisi oluşturmak için aşağıda bulunan başvuru detayınızdan 'Yeni tez danışmanı önerisi' butonuna tıklayınız.");


                            }
                        }
                        else
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Tez danışman öneri başvurusunu Aktif olarak doktora seviyesinde okuyan öğrencileri tarafından yapılabilir.");
                        }
                    }
                }

            }
            return msg;

        }
        public static MmMessage GetTdoBasvuruSilKontrol(int tdoBasvuruId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.TdoGelenBasvuruKayit.InRoleCurrent();
                var basvuru = db.TDOBasvurus.FirstOrDefault(p => p.TDOBasvuruID == tdoBasvuruId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        var message = "Bu enstitüye ait tez danışman başvurusu silmeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: " + basvuru.TDOBasvuruID + " \r\n Tez İzleme Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Tez Danışman Başvuru Sil", LogType.Kritik);
                    }
                    else if (!TdoAyar.BasvurusuAcikmi.GetAyarTdo(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez danışmanı öneri başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye Tez Danışman Başvuru Başvuru ID:" + basvuru.TDOBasvuruID + " \r\n Tez danışmanı öneri Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", LogType.Saldırı);
                    }
                    //else if (KayitYetki == false && basvuru.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi)
                    //{
                    //    msg.IsSuccess = false;
                    //    msg.Messages.Add("Taslak Harici Başvurular Silinemez.");
                    //}
                }
            }
            return msg;
        }
        public static List<CmbIntDto> CmbTdoDanismanTalepTip(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.TDODanismanTalepTipleris
                            select new
                            {
                                s.TDODanismanTalepTipID,
                                s.TalepTipAdi
                            }).AsQueryable();
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.TDODanismanTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbTdoDanismanTalepTip(bool isDegisiklikTalebi, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.TDODanismanTalepTipleris.Where(p => p.TDODanismanTalepTipID == (isDegisiklikTalebi ? p.TDODanismanTalepTipID : 1))
                            select new
                            {
                                s.TDODanismanTalepTipID,
                                s.TalepTipAdi
                            }).AsQueryable();
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.TDODanismanTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbTdoOneriDurumListe(bool bosSecimVar = false, bool isEsDanisman = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 1, Caption = "Danışman Onayı Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Danışman Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Danışman Tarafından Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = 6, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 9, Caption = "EYK'Da Onaylanmadı" });
            return dct;
        }
        public static List<CmbIntDto> CmbTdoEsOneriDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 4, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = 6, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 9, Caption = "EYK'Da Onaylanmadı" });
            return dct;
        }
        public static MmMessage SendMailTdoBilgisi(int tdoBasvuruDanismanId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var tdoBasvuruDanisman = db.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniOnerisi)
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_DanismanOnerisiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_DanismanOnerisiYapildiOgrenci
                        });
                    }
                    else if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniDegisikligi)
                    {
                        var varolanDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.VarolanTezDanismanID);
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Varolan Danışman",
                            AdSoyad = varolanDanisman.Ad + " " + varolanDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = varolanDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanDegisikligiVarolanDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanDegisikligiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanDegisikligiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                    {
                        var varolanDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.VarolanTezDanismanID);
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Varolan Danışman",
                            AdSoyad = varolanDanisman.Ad + " " + varolanDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = varolanDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiVarolanDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezBasligiDegisikligi)
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezBasligiDegisikligiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = danisman.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezBasligiDegisikligiOgrenci
                        });
                    }




                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = tdoBasvuru.Ad + " " + tdoBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezDili", Value = tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce" });

                        if (item.SablonParametreleri.Any(a => a == "@TezBasligiTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBasligiTr", Value = tdoBasvuruDanisman.TezBaslikTr });

                        if (item.SablonParametreleri.Any(a => a == "@TezBasligiEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBasligiEn", Value = tdoBasvuruDanisman.TezBaslikEn });

                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBasligiTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniTezBasligiTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });

                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBasligiEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniTezBasligiEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });

                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = mCOntent.Title
                            };
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }

                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTdoDanismanOnay(int tdoBasvuruDanismanId, bool isOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var tdoBasvuruDanisman = db.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniOnerisi)
                    {
                        if (isOnayOrRed)
                        {
                            var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_DanismanOnerisiOnaylandiDanisman
                            });
                        }
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_DanismanOnerisiOnaylandiOgrenci : MailSablonTipi.TDO_DanismanOnerisiReddedildiOgrenci
                        });
                    }
                    else if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniDegisikligi)
                    {
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezDanismanDegisikligiOnaylandiYeniDanisman : MailSablonTipi.TDO_TezDanismanDegisikligiRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezDanismanDegisikligiOnaylandiOgrenci : MailSablonTipi.TDO_TezDanismanDegisikligiRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                    {
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiOnaylandiYeniDanisman : MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiOnaylandiOgrenci : MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezBasligiDegisikligi)
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezBasligiDegisikligiOnaylandiDanisman : MailSablonTipi.TDO_TezBasligiDegisikligiRetEdildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezBasligiDegisikligiOnaylandiOgrenci : MailSablonTipi.TDO_TezBasligiDegisikligiRetEdildiOgrenci
                        });
                    }



                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();
                        if (isOnayOrRed)
                        {
                            var ids = new List<int?>() { tdoBasvuruDanismanId };
                            var ekler = Management.exportRaporPdf(RaporTipleri.TezDanismanOneriFormu, ids);
                            item.Attachments.AddRange(ekler);
                            gonderilenMailEkleri.AddRange(ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));

                        }
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = tdoBasvuru.Ad + " " + tdoBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniTezBaslikTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniTezBaslikEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (!isOnayOrRed)
                        {
                            var retAciklama = "";

                            if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezBasligiDegisikligi || tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniOnerisi) retAciklama = tdoBasvuruDanisman.DanismanOnaylanmadiAciklama;
                            else retAciklama = tdoBasvuruDanisman.VarolanDanismanOnaylanmadiAciklama;


                            if (item.SablonParametreleri.Any(a => a == "@RedAciklama"))
                            {
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RedAciklama", Value = retAciklama });
                            }
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            {
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RetAciklama", Value = retAciklama });
                            }
                        }
                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }

                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = mCOntent.Title
                            };
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            foreach (var itemMe in gonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemMe);
                            }
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }

                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTdoEykOnay(int tdoBasvuruDanismanId, bool isOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var tdoBasvuruDanisman = db.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();
                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniOnerisi)
                    {

                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_DanismanOnerisiEYKDaOnaylandiDanisman : MailSablonTipi.TDO_DanismanOnerisiEYKDaReddedildiOgrenciDanisman
                        });

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_DanismanOnerisiEYKDaOnaylandiOgrenci : MailSablonTipi.TDO_DanismanOnerisiEYKDaReddedildiOgrenciDanisman
                        });
                    }
                    else if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniDegisikligi)
                    {
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezDanismanDegisikligiEYKDaOnaylandiYeniDanisman : MailSablonTipi.TDO_TezDanismanDegisikligiEYKDaRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezDanismanDegisikligiEYKDaOnaylandiOgrenci : MailSablonTipi.TDO_TezDanismanDegisikligiEYKDaRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                    {
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiEYKDaOnaylandiYeniDanisman : MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiEYKDaRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiEYKDaOnaylandiOgrenci : MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiEYKDaRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TDODanismanTalepTip.TezBasligiDegisikligi)
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezBasligiDegisikligiEYKDaOnaylandiDanisman : MailSablonTipi.TDO_TezBasligiDegisikligiEYKDaRetEdildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipi.TDO_TezBasligiDegisikligiEYKDaOnaylandiOgrenci : MailSablonTipi.TDO_TezBasligiDegisikligiEYKDaRetEdildiOgrenci
                        });
                    }
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();

                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonlar = db.MailSablonlaris.Where(p => sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;

                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = tdoBasvuru.Ad + " " + tdoBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniTezBaslikTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniTezBaslikEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (!isOnayOrRed)
                        {
                            var retAciklama = tdoBasvuruDanisman.EYKDaOnaylanmadiDurumAciklamasi;



                            if (item.SablonParametreleri.Any(a => a == "@RedAciklama"))
                            {
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RedAciklama", Value = retAciklama });
                            }
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            {
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RetAciklama", Value = retAciklama });
                            }
                        }
                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            foreach (var itemMe in gonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemMe);
                            }
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTdoEsBilgisi(int tdoBasvuruEsDanismanId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var esDanisman = db.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
                    var tdoBasvuruDanisman = esDanisman.TDOBasvuruDanisman;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();


                    if (esDanisman.IsDegisiklikTalebi)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiYapildiOgrenci
                        });
                    }
                    else
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiYapildiOgrenci
                        });
                    }



                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        var iDs = new List<int?>() { tdoBasvuruEsDanismanId };
                        var ekler = Management.exportRaporPdf(RaporTipleri.TezEsDanismanOneriFormu, iDs);
                        item.Attachments.AddRange(ekler);
                        gonderilenMailEkleri.AddRange(ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = tdoBasvuru.Ad + " " + tdoBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EsDanismanAdSoyad", Value = esDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUniversite"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EsDanismanUniversite", Value = esDanisman.UniversiteAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanEsDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "VarolanEsDanismanAdSoyad", Value = esDanisman.OncekiEsDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.AdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }


                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;

                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";

                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            foreach (var itemMe in gonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemMe);
                            }
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Eş danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOEsBasvuruDanismanID:" + tdoBasvuruEsDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTdoEsEykOnay(int tDoBasvuruEsDanismanId, bool isOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var esDanisman = db.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tDoBasvuruEsDanismanId);
                    var tdoBasvuruDanisman = esDanisman.TDOBasvuruDanisman;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (esDanisman.IsDegisiklikTalebi)
                    {
                        if (!isOnayOrRed)
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci, Danışman",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + " , " + danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true }, new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiEYKDaRetEdildiOgrenciDanisman
                            });
                        }
                        else
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiEYKDaOnaylandiDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                AdSoyad = esDanisman.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = esDanisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiEYKDaOnaylandiEsDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiEYKDaOnaylandiOgrenci
                            });
                        }
                    }
                    else
                    {
                        if (!isOnayOrRed)
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci, Danışman",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + " , " + danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true }, new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiEYKDaReddedildiOgrenciDanisman
                            });
                        }
                        else
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiEYKDaOnaylandiDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                AdSoyad = esDanisman.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = esDanisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiEYKDaOnaylandiEsDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiEYKDaOnaylandiOgrenci
                            });
                        }
                    }


                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = tdoBasvuru.Ad + " " + tdoBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EsDanismanAdSoyad", Value = esDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUniversite"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EsDanismanUniversite", Value = esDanisman.UniversiteAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanEsDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "VarolanEsDanismanAdSoyad", Value = esDanisman.OncekiEsDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.AdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (esDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = esDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (isOnayOrRed == false && item.SablonParametreleri.Any(a => a == "@RedAciklama"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RedAciklama", Value = esDanisman.EYKDaOnaylanmadiDurumAciklamasi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Eş Danışmanı işlemi için mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }

        public static bool IsSuccessYeniKayit(int tdoBasvuruId)
        {
            var issuccess = true;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var basvuru = db.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId);
                var sonDanismanBasvuru = basvuru.TDOBasvuruDanisman;
                var sonEsDanismanBasvuru =
                    basvuru.TDOBasvuruDanismen.SelectMany(s => s.TDOBasvuruEsDanismen).LastOrDefault();
                if (sonEsDanismanBasvuru != null)
                {
                    if (sonEsDanismanBasvuru.EYKYaGonderildi == true)
                    {
                        issuccess = sonEsDanismanBasvuru.EYKDaOnaylandi.HasValue;
                    }
                    else issuccess = sonEsDanismanBasvuru.EYKYaGonderildi == false;
                }

                if (issuccess)
                {
                    if (sonDanismanBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi ||
                        sonDanismanBasvuru.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                    {
                        if (sonDanismanBasvuru.VarolanDanismanOnayladi == true || sonDanismanBasvuru.VarolanDanismanOnayladi == true)
                        {

                        }
                        else issuccess = false;
                    }

                    if (issuccess)
                    {
                        if (sonDanismanBasvuru.DanismanOnayladi == true)
                        {
                            if (sonDanismanBasvuru.EYKYaGonderildi == true)
                            {
                                if (sonDanismanBasvuru.EYKDaOnaylandi.HasValue)
                                {

                                }
                            }
                        }
                        else issuccess = false;
                    }
                }
            }

            return issuccess;
        }

    }
}