using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class TdoBus
    {
        public static bool IsAktifDanismanOneriVar(int kullaniciId)
        {
            using (var entities = new LubsDbEntities())
            {
                var danismanOneri = entities.TDOBasvuruDanismen.Where(p => p.TDOBasvuru.KullaniciID == kullaniciId)
                    .OrderByDescending(o => o.TDOBasvuruDanismanID).FirstOrDefault();
                var isAktif = true;
                if (danismanOneri == null) isAktif = false;
                else if (danismanOneri.EYKDaOnaylandi.HasValue) isAktif = false;
                else if (danismanOneri.EYKYaGonderildi == false) isAktif = false;
                else if (danismanOneri.VarolanTezDanismanID.HasValue && danismanOneri.VarolanDanismanOnayladi == false) isAktif = false;
                else if (danismanOneri.DanismanOnayladi == false) isAktif = false;
                return isAktif;
            }
        }
        public static bool IsAktifEsDanismanOneriVar(int kullaniciId)
        {
            using (var entities = new LubsDbEntities())
            {
                var danismanOneri = entities.TDOBasvuruEsDanismen.Where(p => p.TDOBasvuruDanisman.TDOBasvuru.KullaniciID == kullaniciId)
                    .OrderByDescending(o => o.TDOBasvuruEsDanismanID).FirstOrDefault();
                var isAktif = true;
                if (danismanOneri == null) isAktif = false;
                else if (danismanOneri.EYKDaOnaylandi.HasValue) isAktif = false;
                else if (danismanOneri.EYKYaGonderildi == false) isAktif = false;
                return isAktif;
            }
        }
        public static TdoBasvuruDetayDto GetSecilenBasvuruTdoDetay(int tdoBasvuruId, Guid? uniqueId)
        {
        tekrarYukle:
            var model = new TdoBasvuruDetayDto() { TDOBasvuruID = tdoBasvuruId };

            using (var entities = new LubsDbEntities())
            {
                var isYoneticiYetki = RoleNames.TdoEykdaOnayYetkisi.InRoleCurrent();
                var isDanismanOnayYetki = RoleNames.TdoDanismanOnayYetkisi.InRoleCurrent();

                var basvuru = entities.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId);
                var ogrenciBilgiUpdate = KullanicilarBus.OgrenciBilgisiGuncelleObs(basvuru.KullaniciID);
                var obsOgrenciBilgi = KullanicilarBus.OgrenciKontrol(basvuru.OgrenciNo);
                model.IsObsOgrenciNoAktif = obsOgrenciBilgi.KayitVar;


                var ogrenci = basvuru.Kullanicilar;
                if (ogrenci.YtuOgrencisi && basvuru.ProgramKod == ogrenci.ProgramKod && basvuru.OgrenimTipKod == ogrenci.OgrenimTipKod && basvuru.OgrenciNo != ogrenci.OgrenciNo)
                {
                    basvuru = entities.TDOBasvurus.First(p => p.TDOBasvuruID == tdoBasvuruId);
                }

                model.OgrenciAdi = ogrenci.Ad + " " + ogrenci.Soyad;
                model.ResimAdi = ogrenci.ResimAdi;
                var enstitu = entities.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);
                var showAllRow = basvuru.KullaniciID == UserIdentity.Current.Id || RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent() || RoleNames.TdoEykdaOnayYetkisi.InRoleCurrent();

                model.EnstituKod = basvuru.EnstituKod;
                model.TDOBasvuruDanisman = basvuru.TDOBasvuruDanisman;
                model.TDOBasvuruDanismanList = (from s in basvuru.TDOBasvuruDanismen
                                                let varolanTdUserkey = entities.Kullanicilars.Where(f => f.KullaniciID == s.VarolanTezDanismanID).Select(sv => sv.UserKey).FirstOrDefault()
                                                let tdUserkey = entities.Kullanicilars.Where(f => f.KullaniciID == s.TezDanismanID).Select(sv => sv.UserKey).FirstOrDefault()
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
                                                    VarolanTDAdSoyad = s.VarolanTDAdSoyad,
                                                    VarolanTDUnvanAdi = s.VarolanTDUnvanAdi,
                                                    VarolanTezDanismaniUserKey = varolanTdUserkey,
                                                    VarolanDanismanOnayladi = s.VarolanDanismanOnayladi,
                                                    VarolanDanismanOnayTarihi = s.VarolanDanismanOnayTarihi,
                                                    VarolanDanismanOnaylanmadiAciklama = s.VarolanDanismanOnaylanmadiAciklama,
                                                    TezDanismaniUserKey = tdUserkey,
                                                    TezDanismanID = s.TezDanismanID,
                                                    TDAdSoyad = s.TDAdSoyad,
                                                    TDUnvanAdi = s.TDUnvanAdi,
                                                    TDAnabilimDaliAdi = s.TDAnabilimDaliAdi,
                                                    TDProgramAdi = s.TDProgramAdi,
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
                                                    EYKYaGonderimDurumAciklamasi = s.EYKYaGonderimDurumAciklamasi,


                                                    EYKYaHazirlandi = s.EYKYaHazirlandi,
                                                    EYKYaHazirlandiAciklamasi = s.EYKYaHazirlandiAciklamasi,
                                                    EYKYaHazirlandiIslemTarihi = s.EYKYaHazirlandiIslemTarihi,
                                                    EYKYaHazirlandiIslemYapanID = s.EYKYaHazirlandiIslemYapanID,

                                                    EYKDaOnaylandi = s.EYKDaOnaylandi,
                                                    EYKDaOnaylandiIslemYapanID = s.EYKDaOnaylandiIslemYapanID,
                                                    EYKDaOnaylandiOnayTarihi = s.EYKDaOnaylandiOnayTarihi,
                                                    EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi,
                                                    Gerekce = s.Gerekce,
                                                    IslemTarihi = s.IslemTarihi,
                                                    IslemYapanID = s.IslemYapanID,
                                                    IslemYapanIP = s.IslemYapanIP,
                                                    TDOBasvuruEsDanismen = basvuru.TDOBasvuruDanismen.SelectMany(sm => sm.TDOBasvuruEsDanismen).OrderByDescending(oe => oe.TDOBasvuruEsDanismanID).ToList(),
                                                    EsDanismanBilgi = basvuru.TDOBasvuruDanismen.SelectMany(sm => sm.TDOBasvuruEsDanismen).OrderByDescending(o => o.TDOBasvuruEsDanismanID).FirstOrDefault()

                                                }).Where(p => p.TezDanismanID == (showAllRow ? p.TezDanismanID : UserIdentity.Current.Id) || p.VarolanTezDanismanID == (showAllRow ? p.VarolanTezDanismanID : UserIdentity.Current.Id)).OrderByDescending(o => o.BasvuruTarihi).ToList();
                if (model.TDOBasvuruDanismanList.Any() && !basvuru.AktifTDOBasvuruDanismanID.HasValue)
                {
                    basvuru.AktifTDOBasvuruDanismanID = model.TDOBasvuruDanismanList.Last().TDOBasvuruDanismanID;
                    entities.SaveChanges();
                }

                var kulIds = model.TDOBasvuruDanismanList.Select(s => s.VarolanTezDanismanID).ToList();
                var kulls = entities.Kullanicilars.Where(p => kulIds.Contains(p.KullaniciID)).ToList();

                var inx = 0;
                foreach (var item in model.TDOBasvuruDanismanList.OrderByDescending(o => o.TDOBasvuruDanismanID))
                {
                    inx++;
                    if (item.VarolanTezDanismanID.HasValue)
                    {
                        if (item.VarolanTDAdSoyad.IsNullOrWhiteSpace())
                        {
                            var kul = kulls.First(p => p.KullaniciID == item.VarolanTezDanismanID);
                            item.VarolanDanismanAd = kul.Unvanlar?.UnvanAdi + " " + kul.Ad + " " + kul.Soyad;
                        }
                        else
                        {
                            item.VarolanDanismanAd = item.VarolanTDUnvanAdi + " " + item.VarolanTDAdSoyad;
                        }
                    }

                    if (inx == 1)
                    {
                        item.IsYeniEsDanismanOneriOrDegisiklik = item.TDOBasvuruEsDanismen.All(ae => ae.EYKDaOnaylandi != true);
                        item.TdoEsBasvurusuYapabilir = (item.EsDanismanBilgi == null ||
                                                        item.EsDanismanBilgi.EYKYaGonderildi == false ||
                                                        item.EsDanismanBilgi.EYKDaOnaylandi == false ||
                                                        item.EsDanismanBilgi.EYKDaOnaylandi == true);
                        //if (item.IsYeniEsDanismanOneriOrDegisiklik)
                        //{
                        //    item.TdoEsBasvurusuYapabilir = (item.EsDanismanBilgi == null ||
                        //                                    item.EsDanismanBilgi.EYKYaGonderildi == false ||
                        //                                    item.EsDanismanBilgi.EYKDaOnaylandi == false ||
                        //                                    item.EsDanismanBilgi.EYKDaOnaylandi==true);
                        //}
                        //else
                        //{
                        //    item.TdoEsBasvurusuYapabilir = item.EsDanismanBilgi == null || (item.EsDanismanBilgi.EYKYaGonderildi == false ||
                        //        item.EsDanismanBilgi.EYKDaOnaylandi.HasValue);
                        //}
                        // obs de öğrenci numarası aktif gözüküyor ise baivuru yapabilsin
                        if (item.TdoEsBasvurusuYapabilir) item.TdoEsBasvurusuYapabilir = model.IsObsOgrenciNoAktif;

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
                model.KayitDonemi = basvuru.KayitOgretimYiliDonemID.HasValue ? (basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi) : "";

                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = entities.OgrenimTipleris.First(p => p.EnstituKod == basvuru.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliID = progLng.AnabilimDaliID;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.AktifTDOBasvuruDanismanID.HasValue
                    ? basvuru.TDOBasvuruDanisman.TDProgramKod
                    : null;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.EnstituAdi = enstitu.EnstituAd;

                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;
                model.DegerlendirenUniqueID = uniqueId;

                if (tdoBasvuruId > 0 && basvuru.Kullanicilar.DanismanID.HasValue && basvuru.TDOBasvuruDanismen.All(a => a.TezDanismanID != basvuru.Kullanicilar.DanismanID))
                {
                    var eslestirildi = ObsDanismanBasvuruBilgiEslestir(model.KullaniciID, model.TDOBasvuruID);
                    if (eslestirildi.Item1)
                    {
                        goto tekrarYukle;
                    }

                }
                else if (tdoBasvuruId > 0 && !model.TDOBasvuruDanismanList.Any() && ogrenciBilgiUpdate.IsDanismanHesabiBulunamadi)
                {
                    model.IsDanismanHesabiBulunamadi = true;

                    //model.BulunamayanDanismanAdSoyad = ogrenciBilgiUpdate.DanismanInfo?.AD+" "+ ogrenciBilgiUpdate.OgrenciInfo.DANISMAN_AD_SOYAD1;
                }

                foreach (var firstRow in model.TDOBasvuruDanismanList)
                {
                    firstRow.VarolanDanismanGozuksun = firstRow.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi || firstRow.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi;
                    firstRow.VarolanDanismanOnayIslemiAcik = ((isDanismanOnayYetki && firstRow.VarolanTezDanismanID == UserIdentity.Current.Id) || isYoneticiYetki) && !firstRow.IsObsData && !firstRow.DanismanOnayladi.HasValue;
                    var danismanOnayYetkiKontrol = ((isDanismanOnayYetki && firstRow.TezDanismanID == UserIdentity.Current.Id) || isYoneticiYetki) && !firstRow.IsObsData && firstRow.EYKYaGonderildi != true;

                    if (firstRow.VarolanDanismanGozuksun)
                    {
                        firstRow.YeniDanismanOnayIslemiAcik = firstRow.VarolanDanismanOnayladi == true && danismanOnayYetkiKontrol;
                    }
                    else
                    {
                        firstRow.YeniDanismanOnayIslemiAcik = danismanOnayYetkiKontrol;

                    }
                    firstRow.IsYeniTezBasligiGozuksun = firstRow.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi || firstRow.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezBasligiDegisikligi;

                    firstRow.IsDuzeltSilYapabilir = firstRow.DanismanOnayladi != true && firstRow.VarolanDanismanOnayladi != true;
                }


                TDOBasvuruEsDanisman lastEsBasvuru = null;
                if (basvuru.TDOBasvuruDanisman != null)
                    lastEsBasvuru = basvuru.TDOBasvuruDanisman.TDOBasvuruEsDanismen
                        .OrderByDescending(o => o.TDOBasvuruEsDanismanID).FirstOrDefault();


                model.IsYeniDanismanOneriOrDegisiklik = model.TDOBasvuruDanisman == null || model.TDOBasvuruDanismanList.All(a => a.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniOnerisi && a.EYKDaOnaylandi != true);

                // obs de öğrenci numarası aktif gözüküyor ise başvuru yapabilsin
                model.TdoBasvurusuYapabilir = model.IsObsOgrenciNoAktif;
                if (!model.TdoBasvurusuYapabilir) return model;


                if (model.IsYeniDanismanOneriOrDegisiklik)
                {
                    model.TdoBasvurusuYapabilir =
                        model.TDOBasvuruDanisman == null ||
                        model.TDOBasvuruDanisman.DanismanOnayladi == false ||
                        model.TDOBasvuruDanisman.EYKYaGonderildi == false ||
                        model.TDOBasvuruDanisman.EYKDaOnaylandi == false; 

                    model.IsAnketDolduruldu = basvuru.AnketCevaplaris.Any();
                    if (model.IsAnketDolduruldu == false)
                    {
                        var anketId = TdoAyar.IlkDanismanOnerisindeIstenenAnket.GetAyar(basvuru.EnstituKod, "").ToInt();  
                        model.IsAnketVar = anketId > 0;
                        if (anketId > 0)
                        {
                            model.AnketView = AnketlerBus.GetAnketView(
                                anketId: anketId.Value,
                                anketTipId: AnketTipiEnum.DanismanAtamaBasvurunAnketi,
                                tdoBasvuruID: basvuru.TDOBasvuruID,
                                rowId:basvuru.UniqueID.ToString()
                            );
                        }
                    }


                }
                else
                {
                    model.TdoBasvurusuYapabilir = model.TDOBasvuruDanisman.VarolanDanismanOnayladi == false ||
                                                  model.TDOBasvuruDanisman.DanismanOnayladi == false ||
                                                  model.TDOBasvuruDanisman.EYKYaGonderildi == false ||
                                                  model.TDOBasvuruDanisman.EYKDaOnaylandi.HasValue;

                    if (model.TdoBasvurusuYapabilir)
                        model.TdoBasvurusuYapabilir = (lastEsBasvuru == null ||
                                                       lastEsBasvuru.EYKYaGonderildi == false ||
                                                       lastEsBasvuru.EYKDaOnaylandi.HasValue);
                    if (model.TdoBasvurusuYapabilir)
                    {
                        if (model.TDOBasvuruDanisman != null)
                        {
                            model.TdoBasvurusuYapabilir =
                                isYoneticiYetki || model.KullaniciID == UserIdentity.Current.Id;
                        }
                    }
                }

            }


            return model;

        }


        //public static bool TosBasvuruOlustur(int kullaniciId)
        //{
        //   using (var entities = new LubsDbEntities())
        //    {
        //        var kul = entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
        //        var isBasvuruEklenebilecekKullanici = kul.YtuOgrencisi &&
        //                                       kul.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci &&
        //                                       (kul.OgrenimTipKod.IsDoktora() ||
        //                                        kul.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans);
        //        if (!isBasvuruEklenebilecekKullanici) return false;

        //        var tdoBasvurusu = entities.TDOBasvurus.FirstOrDefault(f =>
        //            f.KullaniciID == kul.KullaniciID && f.OgrenciNo == kul.OgrenciNo &&
        //            f.ProgramKod == kul.ProgramKod && f.OgrenimTipKod == kul.OgrenimTipKod);

        //        if (tdoBasvurusu != null) return false;



        //var insertModel = new TDOBasvuru
        //{
        //    OgrenimTipKod = kul.OgrenimTipKod.Value,
        //    ResimAdi = kul.ResimAdi,
        //    KullaniciTipID = kul.KullaniciTipID,
        //    KayitOgretimYiliBaslangic = kul.KayitYilBaslangic,
        //    KayitOgretimYiliDonemID = kul.KayitDonemID,
        //    KayitTarihi = kul.KayitTarihi,
        //    OgrenciNo = kul.OgrenciNo,
        //    IslemYapanID = UserIdentity.Current.Id,
        //    IslemTarihi = DateTime.Now,
        //    IslemYapanIP = UserIdentity.Ip
        //};

        //         insertModel.OgrenciNo = kul.OgrenciNo;
        //        insertModel.OgrenimDurumID = kul.OgrenimDurumID.Value;
        //        insertModel.ProgramKod = kul.ProgramKod;
        //        insertModel.Ad = kul.Ad;
        //        insertModel.Soyad = kul.Soyad;
        //        TDOBasvuru data;
        //        var isNewRecord = false;
        //        if (kModel.TDOBasvuruID <= 0)
        //        {
        //            isNewRecord = true;
        //            kModel.BasvuruTarihi = DateTime.Now;

        //            data = _entities.TDOBasvurus.Add(new TDOBasvuru
        //            {
        //                EnstituKod = kModel.EnstituKod,
        //                UniqueID = Guid.NewGuid(),
        //                BasvuruTarihi = kModel.BasvuruTarihi,
        //                KullaniciID = kModel.KullaniciID,
        //                KullaniciTipID = kModel.KullaniciTipID,
        //                ResimAdi = kModel.ResimAdi,
        //                Ad = kModel.Ad,
        //                Soyad = kModel.Soyad,
        //                UyrukKod = kModel.UyrukKod,
        //                TcKimlikNo = kModel.TcKimlikNo,
        //                OgrenciNo = kModel.OgrenciNo,
        //                OgrenimDurumID = kModel.OgrenimDurumID,
        //                OgrenimTipKod = kModel.OgrenimTipKod,
        //                ProgramKod = kModel.ProgramKod,
        //                KayitOgretimYiliBaslangic = kModel.KayitOgretimYiliBaslangic,
        //                KayitOgretimYiliDonemID = kModel.KayitOgretimYiliDonemID,
        //                KayitTarihi = kModel.KayitTarihi,
        //                IslemTarihi = DateTime.Now,
        //                IslemYapanID = UserIdentity.Current.Id,
        //                IslemYapanIP = UserIdentity.Ip

        //            });
        //            _entities.SaveChanges();
        //            TdoBus.ObsDanismanBasvuruBilgiEslestir(data.KullaniciID, data.TDOBasvuruID);

        //        }
        //        else
        //        {

        //            data = _entities.TDOBasvurus.First(p => p.TDOBasvuruID == kModel.TDOBasvuruID);
        //            data.EnstituKod = kModel.EnstituKod;
        //            data.BasvuruTarihi = kModel.BasvuruTarihi;
        //            data.KullaniciID = kModel.KullaniciID;
        //            data.KullaniciTipID = kModel.KullaniciTipID;
        //            data.ResimAdi = kModel.ResimAdi;
        //            data.Ad = kModel.Ad;
        //            data.Soyad = kModel.Soyad;
        //            data.UyrukKod = kModel.UyrukKod;
        //            data.TcKimlikNo = kModel.TcKimlikNo;
        //            data.OgrenciNo = kModel.OgrenciNo;
        //            data.OgrenimDurumID = kModel.OgrenimDurumID;
        //            data.OgrenimTipKod = kModel.OgrenimTipKod;
        //            data.ProgramKod = kModel.ProgramKod;
        //            data.KayitOgretimYiliBaslangic = kModel.KayitOgretimYiliBaslangic;
        //            data.KayitOgretimYiliDonemID = kModel.KayitOgretimYiliDonemID;
        //            data.KayitTarihi = kModel.KayitTarihi;
        //            data.IslemTarihi = DateTime.Now;
        //            data.IslemYapanID = UserIdentity.Current.Id;
        //            data.IslemYapanIP = UserIdentity.Ip;
        //            _entities.SaveChanges();


        //        }
        //        LogIslemleri.LogEkle("TdoBasvuru", isNewRecord ? LogCrudType.Insert : LogCrudType.Update, data.ToJson());

        //    }

        //    return true;
        //}

        public static Tuple<bool, string> ObsDanismanBasvuruBilgiEslestir(int kullaniciId, int tDoBasvuruId)
        {
            using (var entities = new LubsDbEntities())
            {

                var ogrenciInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(kullaniciId);

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

                    var danismanBasvurusuVar = entities.TDOBasvuruDanismen.Any(p => p.TDOBasvuru.KullaniciID == kullaniciId && p.TDOBasvuruID == tDoBasvuruId && p.TezDanismanID == ogrenciInfo.AktifDanismanID);

                    var sonbasvuru = entities.TDOBasvuruDanismen.Where(p => p.TDOBasvuru.KullaniciID == kullaniciId && p.TDOBasvuruID == tDoBasvuruId).OrderByDescending(o => o.TDOBasvuruDanismanID).FirstOrDefault();

                    var sonBasvuruTamamlandi = sonbasvuru == null || sonbasvuru.EYKDaOnaylandi.HasValue || sonbasvuru.EYKYaGonderildi == false || sonbasvuru.DanismanOnayladi == false || sonbasvuru.VarolanDanismanOnayladi == false;


                    if (!danismanBasvurusuVar && sonBasvuruTamamlandi)
                    {
                        var kModel = new TDOBasvuruDanisman
                        {
                            IsObsData = true,
                            BasvuruTarihi = DateTime.Now
                        };
                        var donemBilgi = kModel.BasvuruTarihi.ToAkademikDonemBilgi();
                        kModel.DonemBaslangicYil = donemBilgi.BaslangicYil;
                        kModel.DonemID = donemBilgi.DonemId;
                        var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                        while (entities.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
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
                        kModel.EYKYaHazirlandi = true;
                        kModel.EYKDaOnaylandi = true;

                        kModel.TDODanismanTalepTipID = TdoDanismanTalepTipEnum.TezDanismaniOnerisi;

                        kModel.IslemTarihi = DateTime.Now;
                        kModel.IslemYapanID = UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;


                        var tDoBasvuru = entities.TDOBasvurus.First(p => p.TDOBasvuruID == tDoBasvuruId);
                        kModel.TDOBasvuruID = tDoBasvuruId;
                        if (!ogrenciInfo.OgrenciInfo.ES_DANISMAN_ADSOYAD.IsNullOrWhiteSpace())
                        {
                            var formKodu2 = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            while (entities.TDOBasvuruEsDanismen.Any(a => a.FormKodu == formKodu2))
                            {
                                formKodu2 = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            }

                            kModel.TDOBasvuruEsDanismen = new List<TDOBasvuruEsDanisman>
                            {
                                new TDOBasvuruEsDanisman
                                {
                                    UniqueID = Guid.NewGuid(),
                                    IsObsData = true,
                                    FormKodu = formKodu2,
                                    IsDegisiklikTalebi = false,
                                    BasvuruTarihi = DateTime.Now,
                                    UnvanAdi = ogrenciInfo.OgrenciInfo.ES_DANISMAN_UNVAN,
                                    AdSoyad = ogrenciInfo.OgrenciInfo.ES_DANISMAN_ADSOYAD,
                                    UniversiteAdi = "",
                                    AnabilimDaliAdi = "",
                                    ProgramAdi = "",
                                    EMail = "",
                                    EYKYaGonderildi = true,
                                    EYKYaHazirlandi = true,
                                    EYKDaOnaylandi = true,
                                    IslemTarihi = DateTime.Now,
                                    IslemYapanID = UserIdentity.Current.Id,
                                    IslemYapanIP = UserIdentity.Ip
                                }
                            };
                        }
                        var added = entities.TDOBasvuruDanismen.Add(kModel);
                        tDoBasvuru.AktifTDOBasvuruDanismanID = added.TDOBasvuruDanismanID;


                        entities.SaveChanges();
                        return Tuple.Create(true, "");
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
            using (var entities = new LubsDbEntities())
            {
                var kayitYetki = RoleNames.TdoGelenBasvuruKayit.InRoleCurrent();
                if (tdoBasvuruId.HasValue)
                {
                    var basvuru = entities.TDOBasvurus.FirstOrDefault(p => p.TDOBasvuruID == tdoBasvuruId.Value);
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
                            msg.Messages.Add("Bu Enstitüde yetkili değilsiniz.");
                        }
                        else if (!TdoAyar.BasvurusuAcikmi.GetAyar(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu işlem için yetkili değilsiniz.");
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez danışmanı öneri başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Tez İzleme Başvuru ID:" + basvuru.TDOBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Saldırı);
                        }

                    }
                }
                else
                {
                    msg.IsSuccess = TdoAyar.BasvurusuAcikmi.GetAyar(enstituKod, "false").ToBoolean().Value;
                    if (kullaniciId.HasValue == false) kullaniciId = UserIdentity.Current.Id;
                    else if (kullaniciId != UserIdentity.Current.Id && UserIdentity.Current.IsAdmin == false)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + kullaniciId + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Saldırı);
                        kullaniciId = UserIdentity.Current.Id;
                    }
                    var kul = entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId.Value);
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Başvuru süreci kapalı.");
                    }
                    else
                    {
                        if (kul.YtuOgrencisi && kul.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci && (kul.OgrenimTipKod.IsDoktora() || kul.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans))
                        {
                            var aktifDevamEdenBasvuruVar = entities.TDOBasvurus.Any(p => p.KullaniciID == kullaniciId && p.OgrenciNo == kul.OgrenciNo && p.TDOBasvuruID != tdoBasvuruId.Value);
                            if (aktifDevamEdenBasvuruVar)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Aktif olarak devam eden bir Tez danışmanı öneri süreciniz bulunuyor. Yeni başvuru yapamazsınız.Tez danışmanı önerisi oluşturmak için aşağıda bulunan başvuru detayınızdan 'Yeni tez danışmanı önerisi' butonuna tıklayınız.");


                            }
                        }
                        else
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Tez danışman öneri başvurusunu Aktif olarak doktora  eya Tezli yl seviyesinde okuyan öğrencileri tarafından yapılabilir.");
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

            using (var entities = new LubsDbEntities())
            {
                var kayitYetki = RoleNames.TdoGelenBasvuruKayit.InRoleCurrent();
                var basvuru = entities.TDOBasvurus.FirstOrDefault(p => p.TDOBasvuruID == tdoBasvuruId);
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
                        var message = "Bu enstitüye ait tez danışman başvurusu silmeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: " + basvuru.TDOBasvuruID + " \r\n Tez İzleme Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi;
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Tez Danışman Başvuru Sil", BilgiTipiEnum.Kritik);
                    }
                    else if (!TdoAyar.BasvurusuAcikmi.GetAyar(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez danışmanı öneri başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye Tez Danışman Başvuru Başvuru ID:" + basvuru.TDOBasvuruID + " \r\n Tez danışmanı öneri Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Saldırı);
                    }
                }
            }
            return msg;
        }
        public static List<CmbIntDto> CmbTdoDanismanTalepTip(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = (from s in entities.TDODanismanTalepTipleris
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
        public static List<CmbIntDto> CmbTdoDanismanTalepTip(bool isDegisiklikTalebi, bool bosSecimVar)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = (from s in entities.TDODanismanTalepTipleris.Where(p => p.TDODanismanTalepTipID == (isDegisiklikTalebi ? p.TDODanismanTalepTipID : 1))
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
        public static List<CmbIntDto> CmbTdoOneriDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 1, Caption = "Danışman Onayı Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Danışman Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Danışman Tarafından Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = 6, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 10, Caption = "EYK'ya Hazırlanma Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 11, Caption = "EYK'ya Hazırlandı" });
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
            dct.Add(new CmbIntDto { Value = 10, Caption = "EYK'ya Hazırlanma Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 11, Caption = "EYK'ya Hazırlandı" });
            dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 9, Caption = "EYK'Da Onaylanmadı" });
            return dct;
        }
        public static List<CmbStringDto> CmbTdoDonemListe(string enstituKod, bool bosSecimVar = false)
        {

            using (var entities = new LubsDbEntities())
            {
                var donems = entities.TDOBasvuruDanismen.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.DonemBaslangicYil).ThenByDescending(t => t.DonemID).Select(s => new CmbStringDto
                    {
                        Value = s.DonemBaslangicYil + "" + s.DonemID,
                        Caption = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }
        public static MmMessage SendMailTdoBilgisi(int tdoBasvuruDanismanId)
        {
            return MailSenderTdo.SendMailTdoBilgisi(tdoBasvuruDanismanId);
        }
        public static MmMessage SendMailTdoDanismanOnay(int tdoBasvuruDanismanId, bool isOnayOrRed)
        {
            return MailSenderTdo.SendMailTdoDanismanOnay(tdoBasvuruDanismanId, isOnayOrRed);
        }
        public static MmMessage SendMailTdoEykOnay(int tdoBasvuruDanismanId, bool isOnayOrRed)
        {
            return MailSenderTdo.SendMailTdoEykOnay(tdoBasvuruDanismanId, isOnayOrRed);
        }
        public static MmMessage SendMailTdoEykYaGonderimRet(int tdoBasvuruDanismanId)
        {
            return MailSenderTdo.SendMailTdoEykYaGonderimRet(tdoBasvuruDanismanId);
        }
        public static MmMessage SendMailTdoEsBilgisi(int tdoBasvuruEsDanismanId)
        {
            return MailSenderTdo.SendMailTdoEsBilgisi(tdoBasvuruEsDanismanId);
        }
        public static MmMessage SendMailTdoEsEykOnay(int tDoBasvuruEsDanismanId, bool isOnayOrRed)
        {
            return MailSenderTdo.SendMailTdoEsEykOnay(tDoBasvuruEsDanismanId, isOnayOrRed);
        }
        public static MmMessage SendMailTdoEsEykYaGonderimRet(int tDoBasvuruEsDanismanId)
        {
            return MailSenderTdo.SendMailTdoEsEykYaGonderimRet(tDoBasvuruEsDanismanId);
        }

        public static SonTezBaslikInfo GetSonTezBaslik(string ogrenciNo)
        {
            using (var db = new LubsDbEntities())
            {
                var q1 = from s in db.ToBasvuruSavunmas
                         where s.ToBasvuru.OgrenciNo == ogrenciNo &&
                               s.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi
                         let sr = db.SRTalepleris.FirstOrDefault(f => f.ToBasvuruSavunmaID == s.ToBasvuruSavunmaID)
                         select new
                         {
                             ModuleName = "Tez Öneri Savunma",
                             sr.Tarih,
                             s.IsTezDiliTr,
                             TezBaslikTr = s.YeniTezBaslikTr,
                             TezBaslikEn = s.YeniTezBaslikEn
                         };

                var q2 = from s in db.TIBasvuruAraRapors
                         where s.TIBasvuru.OgrenciNo == ogrenciNo &&
                               s.TIBasvuruAraRaporDurumID == TiAraRaporDurumuEnum.DegerlendirmeSureciTamamlandi &&
                               s.IsBasariliOrBasarisiz == true
                         let sr = db.SRTalepleris.FirstOrDefault(f => f.TIBasvuruAraRaporID == s.TIBasvuruAraRaporID)
                         select new
                         {
                             ModuleName = "Tez İzleme Ara Rapor",
                             sr.Tarih,
                             s.IsTezDiliTr,
                             TezBaslikTr = s.IsTezBasligiDegisti ? s.YeniTezBaslikTr : s.TezBaslikTr,
                             TezBaslikEn = s.IsTezBasligiDegisti ? s.YeniTezBaslikEn : s.TezBaslikEn
                         };

                var result = q1.Concat(q2)
                    .OrderByDescending(x => x.Tarih)
                    .FirstOrDefault();

                if (result == null) return null;

                return new SonTezBaslikInfo
                {
                    ModuleName = result.ModuleName,
                    IsTezDiliTr = result.IsTezDiliTr,
                    TezBaslikTr = result.TezBaslikTr,
                    TezBaslikEn = result.TezBaslikEn
                };
            }
        }


        public static IHtmlString ToBasvuruDurumView(this FrTdoBasvuruDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("TdoBasvuru", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString ToBasvuruDurumView(this TdoBasvuruDanismanDto model)
        {

            var modelData = new List<TdoBasvuruDurumSortDto>
            {
                model.EYKDaOnaylandi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.EYKDaOnaylandi.Value, DurumAciklama = model.EYKDaOnaylandi.Value ? "EYK'da Onaylandı." : "EYK'da Onaylanmadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "EYK'da Onay işlemi bekleniyor." },
                model.EYKYaHazirlandi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.EYKYaHazirlandi.Value, DurumAciklama = model.EYKYaHazirlandi.Value ? "EYK'ya Hazırlandı." : "EYK'ya Hazırlanmadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "EYK'ya Hazırlanma işlemi bekleniyor." },
                model.EYKYaGonderildi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.EYKYaGonderildi.Value, DurumAciklama = model.EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı." : "EYK'ya Gönderimi Onaylanmadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "EYK'ya Gönderim Onayı Bekleniyor." }
            };


            var danismanDegisiklik = model.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi || model.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi;

            if (danismanDegisiklik)
            {
                modelData.Add(model.DanismanOnayladi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.DanismanOnayladi.Value, DurumAciklama = model.DanismanOnayladi.Value ? "Yeni Danışman Onayladı." : "Yeni Danışman Onaylamadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "Yeni Danışman Onayı Bekleniyor." });

                modelData.Add(model.VarolanDanismanOnayladi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.VarolanDanismanOnayladi.Value, DurumAciklama = model.VarolanDanismanOnayladi.Value ? "Varolan Danışman Onayladı." : "Varolan Danışman Onaylamadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "Varolan Danışman Onayı Bekleniyor." });

            }
            else
            {

                modelData.Add(model.DanismanOnayladi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.DanismanOnayladi.Value, DurumAciklama = model.DanismanOnayladi.Value ? "Danışman Onayladı." : "Danışman Onaylamadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "Danışman Onayı Bekleniyor." });

            }

            var activeDurum = modelData.Any(a => a.IsOnayOrRed.HasValue) ? modelData.First(p => p.IsOnayOrRed.HasValue) : modelData.Last();
            var htmlString = $"<span style=\"color:{activeDurum.DurumColor};\" aria-hidden=\"true\">" +
                             $"<i class=\"{activeDurum.DurumClass}\" style=\"font-size:12pt;\" aria-hidden=\"true\"></i> {activeDurum.DurumAciklama}</span>";
            return new MvcHtmlString(htmlString);

        }
        public static IHtmlString ToBasvuruDurumViewEs(this TDOBasvuruEsDanisman model)
        {

            var modelData = new List<TdoBasvuruDurumSortDto>
            {
                model.EYKDaOnaylandi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.EYKDaOnaylandi.Value, DurumAciklama = model.EYKDaOnaylandi.Value ? "EYK'da Onaylandı." : "EYK'da Onaylanmadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "EYK'da Onay işlemi bekleniyor." },
                model.EYKYaHazirlandi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.EYKYaHazirlandi.Value, DurumAciklama = model.EYKYaHazirlandi.Value ? "EYK'ya Hazırlandı." : "EYK'ya Hazırlanmadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "EYK'ya Hazırlanma işlemi bekleniyor." },
                model.EYKYaGonderildi.HasValue
                    ? new TdoBasvuruDurumSortDto { IsOnayOrRed = model.EYKYaGonderildi.Value, DurumAciklama = model.EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı." : "EYK'ya Gönderimi Onaylanmadı." }
                    : new TdoBasvuruDurumSortDto { DurumAciklama = "EYK'ya Gönderim Onayı Bekleniyor." }
            };
            var activeDurum = modelData.Any(a => a.IsOnayOrRed.HasValue) ? modelData.First(p => p.IsOnayOrRed.HasValue) : modelData.Last();
            var htmlString = $"<span style=\"color:{activeDurum.DurumColor};\" aria-hidden=\"true\">" +
                             $"<i class=\"{activeDurum.DurumClass}\" style=\"font-size:12pt;\" aria-hidden=\"true\"></i> {activeDurum.DurumAciklama}</span>";
            return new MvcHtmlString(htmlString);

        }


    }


    public class SonTezBaslikInfo
    {
        public string ModuleName { get; set; }
        public bool IsTezDiliTr { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
    }
}