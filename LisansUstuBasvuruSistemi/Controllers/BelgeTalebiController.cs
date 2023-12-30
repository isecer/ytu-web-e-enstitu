using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.MailManager;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class BelgeTalebiController : Controller
    {
        private LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmBelgeTalepleriDto() { PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmBelgeTalepleriDto model, string EKD)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);

            var bbModel = new IndexPageInfoDto
            {
                Kullanici = kullanici,
                SistemBasvuruyaAcik = BelgeTalepAyar.BelgeTalebiAcikmi.GetAyarBt(enstituKod, "0").ToBoolean().Value,
                DonemAdi = DonemlerBus.CmbGetAkademikBulundugumuzTarih(DateTime.Now).Caption,
                EnstituYetki = UserIdentity.Current.SeciliEnstituKodu.Contains(enstituKod) || UserIdentity.Current.SeciliEnstituKodu == enstituKod,
                Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod),
                KullaniciTipYetki = false
            };
            if (kullanici.YtuOgrencisi)
            {
                if (kullanici.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci)
                {
                    var kullKayitB = KullanicilarBus.OgrenciBilgisiGuncelleObs(kullanici.KullaniciID);
                    if (kullKayitB.KayitVar)
                    {
                        kullanici.KayitYilBaslangic = kullKayitB.BaslangicYil;
                        kullanici.KayitDonemID = kullKayitB.DonemID;
                        kullanici.KayitTarihi = kullKayitB.KayitTarihi;
                        _entities.SaveChanges();
                        bbModel.KullaniciTipYetki = true;

                    }
                    else bbModel.KullaniciTipYetkiYokMsj = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Kullanıcı hesabınızdaki YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";

                }
                else bbModel.KullaniciTipYetki = true;
                if (bbModel.KullaniciTipYetki)
                {
                    var otb = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);
                    bbModel.OgrenimDurumAdi = kullanici.OgrenimDurumlari.OgrenimDurumAdi;
                    bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                    bbModel.AnabilimdaliAdi = kullanici.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    bbModel.ProgramAdi = kullanici.Programlar.ProgramAdi;
                    bbModel.OgrenciNo = kullanici.OgrenciNo;
                }
            }

            ViewBag.bModel = bbModel;

            #region data
            var q = from s in _entities.BelgeTalepleris
                    join ibt in _entities.BelgeTipleris on s.BelgeTipID equals ibt.BelgeTipID
                    join btit in _entities.BelgeDurumlaris on s.BelgeDurumID equals btit.BelgeDurumID
                    join d in _entities.Donemlers on s.DonemID equals d.DonemID
                    join dk in _entities.SistemDilleris on s.BelgeDilKodu equals dk.DilKodu
                    join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                    join od in _entities.OgrenimDurumlaris on s.OgrenimDurumID equals od.OgrenimDurumID
                    join kul in _entities.Kullanicilars on s.KullaniciID equals kul.KullaniciID into defk
                    from kl in defk.DefaultIfEmpty()
                    where s.KullaniciID == kullanici.KullaniciID && s.EnstituKod == enstituKod
                    select new
                    {
                        s.BelgeTalepID,
                        btit.ClassName,
                        btit.Color,
                        s.EnstituKod,
                        s.BelgeDurumID,
                        btit.DurumAdi,
                        s.BelgeDurumAciklamasi,
                        s.BelgeTipID,
                        ibt.BelgeTipAdi,
                        s.OgrenimDurumID,
                        od.OgrenimDurumAdi,
                        s.BelgeDilKodu,
                        dk.DilAdi,
                        dk.DilFlagClass,
                        s.OgrenimTipKod,
                        ot.OgrenimTipAdi,
                        s.OgretimYiliBaslangic,
                        s.OgretimYiliBitis,
                        s.TalepTarihi,
                        s.DonemID,
                        d.DonemAdi,
                        s.AdiSoyadi,
                        ResimAdi = kl != null ? kl.ResimAdi : "",
                        KullaniciID = kl != null ? kl.KullaniciID : (int?)null,
                        UserKey = kl != null ? kl.UserKey : (Guid?)null,
                        s.OgrenciNo,
                        s.ErisimKodu,
                        s.ProgramKod,
                        s.Email,
                        s.Telefon,
                        s.IstenenBelgeSayisi,
                        s.BelgeAdi,
                        s.BelgeAciklamasi,
                        s.IslemTarihi,
                        s.IslemYapanID,
                        s.IslemYapanIp,
                        s.VerilenBelgeSayisi,
                        s.BelgeFiyati,
                        s.VerilenBelgeTutar,
                        s.EklenecekGun,
                        s.TeslimBaslangicSaat,
                        s.TeslimBitisSaat
                    };


            if (model.OgrenimDurumId.HasValue) q = q.Where(p => p.OgrenimDurumID == model.OgrenimDurumId);
            if (model.DilKodu.IsNullOrWhiteSpace() == false) q = q.Where(p => p.BelgeDilKodu == model.DilKodu);
            if (model.AranacakKelime.IsNullOrWhiteSpace() == false) q = q.Where(p => p.AdiSoyadi.Contains(model.AranacakKelime) || p.Telefon == model.AranacakKelime || p.Email.Contains(model.AranacakKelime) || p.OgrenciNo == model.AranacakKelime);
            if (model.BelgeTipId.HasValue) q = q.Where(p => p.BelgeTipID == model.BelgeTipId);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (model.ProgramKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.ProgramKod == model.ProgramKod);
            if (model.BelgeId.HasValue) q = q.Where(p => p.BelgeTalepID == model.BelgeId.Value);
            if (model.BelgeDurumId.HasValue) q = q.Where(p => p.BelgeDurumID == model.BelgeDurumId.Value);
            if (model.OgretimYili.IsNullOrWhiteSpace() == false)
            {
                var oy = model.OgretimYili.Split('/').ToList();
                var bas = oy[0].ToInt().Value;
                var bit = oy[1].ToInt().Value;
                var done = oy[2].ToInt().Value;
                q = q.Where(p => p.OgretimYiliBaslangic == bas && p.OgretimYiliBitis == bit && p.DonemID == done);
            }


            q = model.Sort.IsNullOrWhiteSpace() == false ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.TalepTarihi);
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            model.BelgeTalepleriDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(item => new FrBelgeTalepleriDto
            {
                BelgeTalepID = item.BelgeTalepID,
                BelgeDurumID = item.BelgeDurumID,
                TalepTarihi = item.TalepTarihi,
                DurumAdi = item.DurumAdi,
                DurumListeAdi = item.DurumAdi,
                ClassName = item.ClassName,
                Color = item.Color,
                BelgeTipID = item.BelgeTipID,
                BelgeTipAdi = item.BelgeTipAdi,
                OgrenimTipKod = item.OgrenimTipKod,
                OgrenimTipAdi = item.OgrenimTipAdi,
                OgretimYiliBaslangic = item.OgretimYiliBaslangic,
                OgretimYiliBitis = item.OgretimYiliBitis,
                DonemID = item.DonemID,
                DonemAdi = item.DonemAdi,
                AdiSoyadi = item.AdiSoyadi,
                ResimAdi = item.ResimAdi,
                KullaniciID = item.KullaniciID,
                UserKey = item.UserKey,
                OgrenciNo = item.OgrenciNo,
                ProgramKod = item.ProgramKod,
                Email = item.Email,
                Telefon = item.Telefon,
                IstenenBelgeSayisi = item.IstenenBelgeSayisi,
                IslemTarihi = item.IslemTarihi,
                IslemYapanID = item.IslemYapanID,
                IslemYapanIp = item.IslemYapanIp,
                VerilenBelgeSayisi = item.VerilenBelgeSayisi,
                BelgeFiyati = item.BelgeFiyati,
                VerilenBelgeTutar = item.VerilenBelgeTutar,
                BelgeDilKodu = item.BelgeDilKodu,
                DilAdi = item.DilAdi,
                DilFlagClass = item.DilFlagClass,
                BelgeAciklamasi = item.BelgeAciklamasi,
                BelgeAdi = item.BelgeAdi,
                EklenecekGun = item.EklenecekGun,
                TeslimBaslangicSaat = item.TeslimBaslangicSaat,
                TeslimBitisSaat = item.TeslimBitisSaat

            }).AsEnumerable();

            ViewBag.IndexModel = indexModel;

            #endregion

            ViewBag.BelgeTipID = new SelectList(BelgeTalepBus.GetCmbBelgeTipleri(true), "Value", "Caption", model.BelgeTipId);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(true), "Value", "Caption", model.OgretimYili);
            ViewBag.BelgeDurumID = new SelectList(BelgeTalepBus.GetCmbBelgeTalepDurumListe(true), "Value", "Caption", model.BelgeDurumId);
            return View(model);
        }

        public ActionResult Getdetay(int id, string ekd)
        {
            var kYetki = RoleNames.BelgeTalebiDuzelt.InRoleCurrent();
            var belgeTalebi = (from s in _entities.BelgeTalepleris
                               join ibt in _entities.BelgeTipleris on s.BelgeTipID equals ibt.BelgeTipID
                               join btit in _entities.BelgeDurumlaris on s.BelgeDurumID equals btit.BelgeDurumID
                               join d in _entities.Donemlers on s.DonemID equals d.DonemID
                               join dk in _entities.SistemDilleris on s.BelgeDilKodu equals dk.DilKodu
                               join ot in _entities.OgrenimTipleris on new { s.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                               join od in _entities.OgrenimDurumlaris on s.OgrenimDurumID equals od.OgrenimDurumID
                               join kul in _entities.Kullanicilars on s.KullaniciID equals kul.KullaniciID into defk
                               from kl in defk.DefaultIfEmpty()
                               join prg in _entities.Programlars on s.ProgramKod equals prg.ProgramKod
                               where s.BelgeTalepID == id
                               select new BelgeTalepleriDetayDto
                               {
                                   ClassName = s.BelgeDurumlari.ClassName,
                                   Color = s.BelgeDurumlari.Color,
                                   TalepTarihi = s.TalepTarihi,
                                   BelgeTalepID = s.BelgeTalepID,
                                   BelgeTipleri = s.BelgeTipleri,
                                   BelgeTipID = s.BelgeTipID,
                                   BelgeTipAdi = ibt.BelgeTipAdi,
                                   BelgeDurumID = s.BelgeDurumID,
                                   BelgeDurumAciklamasi = s.BelgeDurumAciklamasi,
                                   DilAdi = dk.DilAdi,
                                   DilFlagClass = dk.DilFlagClass,
                                   DurumAdi = btit.DurumAdi,
                                   DurumListeAdi = btit.DurumAdi,
                                   OgrenimDurumID = s.OgrenimDurumID,
                                   OgrenimDurumAdi = od.OgrenimDurumAdi,
                                   OgrenimDurumlari = od,
                                   OgrenimTipAdi = ot.OgrenimTipAdi,
                                   OgrenimTipKod = s.OgrenimTipKod,
                                   OgretimYiliBaslangic = s.OgretimYiliBaslangic,
                                   OgretimYiliBitis = s.OgretimYiliBitis,
                                   DonemID = s.DonemID,
                                   DonemAdi = d.DonemAdi,
                                   AdiSoyadi = s.AdiSoyadi,
                                   OgrenciNo = s.OgrenciNo,
                                   ResimAdi = kl != null ? kl.ResimAdi : "",
                                   KullaniciID = kl != null ? kl.KullaniciID : (int?)null,
                                   KullaniciTipAdi = kl != null ? kl.KullaniciTipleri.KullaniciTipAdi : "",
                                   ProgramKod = s.ProgramKod,
                                   ProgramAdi = prg.ProgramAdi,
                                   Email = s.Email,
                                   Telefon = s.Telefon,
                                   IstenenBelgeSayisi = s.IstenenBelgeSayisi,
                                   IslemTarihi = s.IslemTarihi,
                                   IslemYapanID = s.IslemYapanID,
                                   IslemYapanIp = s.IslemYapanIp,
                                   VerilenBelgeSayisi = s.VerilenBelgeSayisi,
                                   BelgeFiyati = s.BelgeFiyati,
                                   VerilenBelgeTutar = s.VerilenBelgeTutar,
                                   UcretAlimiVar = s.UcretAlimiVar,
                                   UcretAciklamasiLink = s.UcretAciklamasiLink,
                                   DonemlikKotaVar = s.DonemlikKota.HasValue,
                                   DonemlikKota = s.DonemlikKota,
                                   UcretsizMiktar = s.UcretsizMiktar,
                                   DonemdeAlinabilecekToplamMiktar = s.DonemlikKota.HasValue ? s.DonemlikKota.Value : 0,
                                   BelgeAciklamasi = s.BelgeAciklamasi,
                                   BelgeAdi = s.BelgeAdi,
                                   Edit = true// !db.BelgeTalepleris.Any(pt => pt.OgrenciNo == s.OgrenciNo && pt.BelgeTipID == s.BelgeTipID && pt.IslemTarihi > s.IslemTarihi)

                               }).FirstOrDefault();
            var bel = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == id);
            var belh = TutarHesapla(bel);
            belgeTalebi.VerilenBelgeTutar = belh.VerilenBelgeTutar;
            belgeTalebi.BelgeTalepID = -1;
            belgeTalebi.SeciliDonemdeVerilenMiktar = AyniDonemAlinenBelgeSayisi(belgeTalebi);
            belgeTalebi.SeciliDonemdehenuzVerilmeyenMiktar = AyniDonemTalepEdilenBelgeSayisi(belgeTalebi);
            belgeTalebi.BelgeTalepID = belh.BelgeTalepID;
            ViewBag.VerilenBelgeSayisi = new SelectList(GetBelgeSayisi(), "Value", "Caption", belgeTalebi.BelgeDurumID == BelgeTalepDurumEnum.Verildi ? belgeTalebi.VerilenBelgeSayisi : belgeTalebi.IstenenBelgeSayisi);
            ViewBag.BelgeDurumID = new SelectList(BelgeTalepBus.GetCmbBelgeTalepDurum(true, kYetki), "Value", "Caption", belgeTalebi.BelgeDurumID);

            return View(belgeTalebi);
        }
        public static List<CmbIntDto> GetBelgeSayisi(int maxBelgeS = 10)
        {
            var dct = new List<CmbIntDto>();
            for (int i = 1; i <= maxBelgeS; i++)
            {
                dct.Add(new CmbIntDto { Value = i, Caption = i.ToString() });
            }
            return dct;
        }
        public ActionResult GetBelgeSayisiA(int? belgeTalepId, string ogrenciNo, int belgeTipId)
        {
            int ogrenimDurumId;
            if (belgeTalepId > 0)
            {
                ogrenimDurumId = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == belgeTalepId.Value).OgrenimDurumID;
            }
            else
            {
                ogrenimDurumId = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id).OgrenimDurumID.Value;

            }
            List<CmbIntDto> bsay;
            var btip = _entities.BelgeTipDetays.FirstOrDefault(p => p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == belgeTipId) && p.OgrenimDurumID == ogrenimDurumId);//düzeltilecek enstitu filtresi
            if (btip != null && btip.DonemlikKota.HasValue)
            {
                bsay = GetBelgeSayisi(btip.DonemlikKota.Value);
            }
            else
            {
                bsay = GetBelgeSayisi(10);
            }
            return bsay.Select(s => new { Key = s.Value, Value = s.Caption }).ToList().ToJsonResult();
        }

        public ActionResult TalepYap(string ekd, string kod = "", int? id = null)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var belge = new BelgeTalepleri();
            var mmMessage = new MmMessage();
            bool belgeDuzenleYetki = RoleNames.BelgeTalebiDuzelt.InRoleCurrent();
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            if (BelgeTalepAyar.BelgeTalebiAcikmi.GetAyarBt(enstituKod, "0").ToBoolean(false))
            {

                if (id.HasValue)
                {
                    belge = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == id.Value);
                    if (belgeDuzenleYetki == false)
                    {
                        if (belge.KullaniciID != kul.KullaniciID && belge.IslemYapanID != kul.KullaniciID)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Farklı bir kullanıcıya ait belge talebi güncellenmek isteniyor! \r\n BelgeTalepID:" + belge.BelgeTalepID + " \r\n Ad Soyad" + belge.AdiSoyadi, "BelgeTalebi/TalepYap", LogTipiEnum.Saldırı);
                            mmMessage.Messages.Add("Size ait olmayan bir belgeyi düzenlemeye hakkınız yoktur!");

                        }
                        else if (belge.BelgeDurumID == BelgeTalepDurumEnum.IptalEdildi || belge.BelgeDurumID == BelgeTalepDurumEnum.Kapatildi || belge.BelgeDurumID == BelgeTalepDurumEnum.Verildi)
                        {
                            mmMessage.Messages.Add("Bu Belge Talebini Düzeltemezsiniz.");
                        }


                    }
                }
                else if (kod.IsNullOrWhiteSpace() == false)
                {
                    var ID = kod.Split('_')[0].ToInt(0);
                    var kd = kod.Split('_')[1].ToString();
                    var belgeT = _entities.BelgeTalepleris.FirstOrDefault(p => p.BelgeTalepID == ID && p.ErisimKodu == kd);
                    if (belgeT == null)
                    {
                        string msg = "Aranılan belge bilgisi sistemde bulunamadı!";
                        mmMessage.Messages.Add(msg);
                    }
                    else
                    {
                        if (belgeT.BelgeDurumID == BelgeTalepDurumEnum.IptalEdildi)
                            mmMessage.Messages.Add("Aranılan belge iptal edildiğinden dolayı herhangi bir işlem yapamazsınız!");
                        if (belgeT.BelgeDurumID == BelgeTalepDurumEnum.Verildi)
                            mmMessage.Messages.Add("Aranılan belge talebi daha önceden işlem gördüğünden herhangi bir işlem yapamazsınız!");
                        else belge = belgeT;

                    }
                }
                else if (kul.YtuOgrencisi == false)
                {
                    mmMessage.Messages.Add("Belge talebi yapabilmek için Hesap bilginizde bulunan YTÜ öğrencisi bilgilerinizi doldurunuz.");
                    MessageBox.Show("Uyarı", MessageBox.MessageType.Information, mmMessage.Messages.ToArray());
                    return RedirectToAction("Index");
                }
                else if (kul.YtuOgrencisi)
                {
                    belge.OgrenciNo = kul.OgrenciNo;
                    belge.OgrenimDurumID = kul.OgrenimDurumID.Value;
                    belge.AdiSoyadi = kul.Ad + " " + kul.Soyad;
                }
                if (kul.YtuOgrencisi)
                {

                    if (kul.OgrenimDurumID != OgrenimDurumEnum.OzelOgrenci && kul.KayitTarihi.HasValue == false)
                    {
                        var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kul.TcKimlikNo);
                        if (ogrenciBilgi.Hata)
                        {
                            mmMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu!");
                        }
                        else
                        {
                            if (ogrenciBilgi.KayitVar)
                            {
                                kul.KayitTarihi = ogrenciBilgi.KayitTarihi;
                                kul.KayitYilBaslangic = ogrenciBilgi.BaslangicYil;
                                kul.KayitDonemID = ogrenciBilgi.DonemID;
                                _entities.SaveChanges();
                            }
                            else
                            {
                                mmMessage.Messages.Add("Öğrenci Bilgileriniz Doğrulanamadı!");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TcKimlikNo" });
                            }
                        }
                    }

                }
            }
            else
            {
                mmMessage.Messages.Add("Sistem belge talebi işlemine kapalıdır.");
            }
            if (mmMessage.Messages.Count > 0)
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Information, mmMessage.Messages.ToArray());
                return RedirectToAction("Index");
            }
            ViewBag.BelgeDurumID = new SelectList(BelgeTalepBus.GetCmbBelgeTalepDurum(true, belgeDuzenleYetki, belge.BelgeTalepID <= 0), "Value", "Caption", belge.BelgeDurumID);
            ViewBag.BelgeTipID = new SelectList(BelgeTalepBus.GetCmbBelgeTipleri(true, belge.OgrenimDurumID, enstituKod), "Value", "Caption", belge.BelgeTipID);

            ViewBag.ProgramKod = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(enstituKod, true), "Value", "Caption", belge.ProgramKod);
            //ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(_EnstituKod true), "Value", "Caption", belge.OgrenimTipKod);
            ViewBag.OgrenimDurumID = new SelectList(KullanicilarBus.CmbAktifOgrenimDurumu(true, isHesapKayittaGozuksun: true), "Value", "Caption", belge.OgrenimDurumID);
            ViewBag.BelgeDilKodu = new SelectList(BelgeTalepBus.GetDiller(true), "Value", "Caption", belge.BelgeDilKodu);
            ViewBag.MmMessage = mmMessage;
            if (belge.BelgeTalepID > 0)
            {
                var btip = _entities.BelgeTipDetays.First(p => p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == belge.BelgeTipID) && p.EnstituKod == belge.EnstituKod && p.OgrenimDurumID == belge.OgrenimDurumID);
                ViewBag.IstenenBelgeSayisi = new SelectList(GetBelgeSayisi(btip.DonemlikKota.HasValue ? btip.DonemlikKota.Value : 10), "Value", "Caption", belge.IstenenBelgeSayisi);
            }
            else ViewBag.IstenenBelgeSayisi = new SelectList(new Dictionary<int, int>(), "Value", "Caption", belge.IstenenBelgeSayisi);

            return View(belge);
        }

        [HttpPost]
        public ActionResult TalepYap(BelgeTalepleri kModel, string ekd, bool? iptal)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            bool belgeDuzenleYetki = RoleNames.BelgeTalebiDuzelt.InRoleCurrent();
            var mmMessage = new MmMessage();


            #region kontrol

            if (kModel.OgrenciNo.IsNullOrWhiteSpace())
            {

                mmMessage.Messages.Add("Öğrenci Numarası Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenciNo" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgrenciNo" });
                if (kModel.BelgeTalepID <= 0)
                {
                    var kul = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                    kModel.AdiSoyadi = kul.Ad + " " + kul.Soyad;
                    kModel.OgrenciNo = kul.OgrenciNo;
                    kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;
                    kModel.ProgramKod = kul.ProgramKod;
                    kModel.OgrenimDurumID = kul.OgrenimDurumID.Value;
                    kModel.Email = kul.EMail;
                    kModel.Telefon = kul.CepTel;
                }
                else
                {
                    var bsv = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == kModel.BelgeTalepID);

                    kModel.AdiSoyadi = bsv.AdiSoyadi;
                    kModel.OgrenciNo = bsv.OgrenciNo;
                    kModel.OgrenimTipKod = bsv.OgrenimTipKod;
                    kModel.ProgramKod = bsv.ProgramKod;
                    kModel.OgrenimDurumID = bsv.OgrenimDurumID;
                    kModel.Email = bsv.Email;
                    kModel.Telefon = bsv.Telefon;
                }
            }
            if (kModel.BelgeTipID <= 0)
            {
                mmMessage.Messages.Add("Belge Tipini Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeTipID" });
            }
            else if (kModel.BelgeTipID == BelgeTalepTipiEnum.İlgiliMakama && kModel.BelgeAciklamasi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("İlgili Makam Açıklaması Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeAciklamasi" });
            }
            else if (kModel.BelgeTipID == BelgeTalepTipiEnum.Diğer)
            {
                if (kModel.BelgeAdi.IsNullOrWhiteSpace())
                {

                    mmMessage.Messages.Add("Belge Adını Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeAdi" });
                }
                if (kModel.BelgeAciklamasi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Belge Açıklaması Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeAciklamasi" });
                }
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BelgeTipID" });
            if (kModel.BelgeDilKodu.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Belgenin Hazırlanacağı Dili Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeDilKodu" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BelgeDilKodu" });
            if (kModel.IstenenBelgeSayisi <= 0)
            {

                mmMessage.Messages.Add("İstenen Belge Sayısını Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IstenenBelgeSayisi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IstenenBelgeSayisi" });

            if (kModel.BelgeTalepID <= 0)
            {
                kModel.BelgeDurumID = BelgeTalepDurumEnum.TalepEdildi;//talep edildi
            }
            else
            {

                if (kModel.BelgeDurumID <= 0)
                {

                    mmMessage.Messages.Add("Talep Durumunu Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeDurumID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BelgeDurumID" });


            }

            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                var belCont = GetContent((kModel.BelgeTalepID == 0 ? (int?)null : kModel.BelgeTalepID), kModel.BelgeTipID, enstituKod, kModel.OgrenciNo, kModel.IstenenBelgeSayisi, kModel.OgrenimDurumID);
                if (belCont.Success == false)
                {
                    var btip = BelgeTalepBus.GetBelgeTipDetay(kModel.BelgeTipID, kModel.OgrenimDurumID, enstituKod);
                    if (kModel.BelgeTalepID <= 0)
                    {
                        kModel.UcretAlimiVar = btip.UcretAlimiVar;
                        kModel.UcretsizMiktar = btip.UcretsizMiktar;
                        kModel.DonemlikKota = btip.DonemlikKota;
                        kModel.BelgeFiyati = btip.BelgeFiyati;
                    }

                    mmMessage.Messages.Add("Bu belge tipi için aynı dönem içinde alabileceğiniz toplam belge sayısı " + kModel.DonemlikKota.Value + " adettir, eğer " + kModel.DonemlikKota.Value + " adetten fazla belgeye ihtiyaç duyuluyorsanız daha önceden almış olduğunuz belgenin fotokopisini çektirip kurum tarafından 'Aslı Gibidir' kaşesi vurdurabilirsiniz.");
                }
            }
            kModel.EnstituKod = enstituKod;
            if (mmMessage.Messages.Count == 0)
            {
                string msg = "";
                var yeniKayit = kModel.BelgeTalepID <= 0;
                var eoYil = DateTime.Now.ToEgitimOgretimYilBilgi();
                var donem = _entities.Donemlers.First(p => p.DonemID == eoYil.Donem);


                kModel.OgretimYiliBaslangic = eoYil.BaslangicYili;
                kModel.OgretimYiliBitis = eoYil.BitisYili;
                kModel.DonemID = eoYil.Donem;
                int ID = 0;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIp = UserIdentity.Ip;


                if (yeniKayit)
                {
                    kModel.TalepTarihi = DateTime.Now;
                    var belgeSaat = BelgeTalepBus.GetCmbSelectedSaat(kModel.TalepTarihi, kModel.BelgeTipID, kModel.OgrenimDurumID, enstituKod);
                    kModel.EklenecekGun = belgeSaat.EklenecekGun;
                    kModel.TeslimBaslangicSaat = belgeSaat.TeslimBaslangicSaat;
                    kModel.TeslimBitisSaat = belgeSaat.TeslimBitisSaat;
                    var guid = Guid.NewGuid().ToString().Substring(0, 6).ToLower();
                    kModel.ErisimKodu = guid;
                    var btip = BelgeTalepBus.GetBelgeTipDetay(kModel.BelgeTipID, kModel.OgrenimDurumID, enstituKod);
                    kModel.UcretAlimiVar = btip.UcretAlimiVar;
                    kModel.UcretsizMiktar = btip.UcretsizMiktar;
                    kModel.DonemlikKota = btip.DonemlikKota;
                    kModel.BelgeFiyati = btip.BelgeFiyati;
                    kModel.KullaniciID = UserIdentity.Current.Id;
                    msg = "Talep Yapıldı";
                    var bt = _entities.BelgeTalepleris.Add(kModel);
                    if (UserIdentity.Current.Informations.Any(a => a.Key == "BTAnket"))
                    {
                        var anketCevaplari = UserIdentity.Current.Informations.FirstOrDefault(p => p.Key == "BTAnket");
                        var dataAnk = (List<AnketCevaplari>)anketCevaplari.Value;
                        foreach (var item in dataAnk)
                        {
                            bt.AnketCevaplaris.Add(item);
                        }
                        UserIdentity.Current.Informations.Remove("BTAnket");
                    }
                }
                else
                {
                    var belge = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == kModel.BelgeTalepID);

                    var belgeSaat = BelgeTalepBus.GetCmbSelectedSaat(belge.TalepTarihi, kModel.BelgeTipID, kModel.OgrenimDurumID, enstituKod);
                    kModel.EklenecekGun = belgeSaat.EklenecekGun;
                    kModel.TeslimBaslangicSaat = belgeSaat.TeslimBaslangicSaat;
                    kModel.TeslimBitisSaat = belgeSaat.TeslimBitisSaat;
                    belge.EnstituKod = kModel.EnstituKod;
                    belge.BelgeDurumID = kModel.BelgeDurumID;
                    belge.BelgeTipID = kModel.BelgeTipID;
                    belge.OgrenimTipKod = kModel.OgrenimTipKod;
                    belge.OgretimYiliBaslangic = kModel.OgretimYiliBaslangic;
                    belge.OgretimYiliBitis = kModel.OgretimYiliBitis;
                    belge.DonemID = kModel.DonemID;
                    belge.AdiSoyadi = kModel.AdiSoyadi;
                    belge.OgrenciNo = kModel.OgrenciNo;
                    belge.BelgeDilKodu = kModel.BelgeDilKodu;
                    belge.ProgramKod = kModel.ProgramKod;
                    belge.Email = kModel.Email;
                    belge.EklenecekGun = kModel.EklenecekGun;
                    if (kModel.BelgeTipID == BelgeTalepTipiEnum.İlgiliMakama) belge.BelgeAciklamasi = kModel.BelgeAciklamasi;
                    else if (kModel.BelgeTipID == BelgeTalepTipiEnum.Diğer)
                    {
                        belge.BelgeAdi = kModel.BelgeAdi;
                        belge.BelgeAciklamasi = kModel.BelgeAciklamasi;
                    }
                    else
                    {
                        belge.BelgeAciklamasi = null;
                        belge.BelgeAdi = null;
                    }
                    belge.Telefon = kModel.Telefon;
                    belge.IstenenBelgeSayisi = kModel.IstenenBelgeSayisi;
                    belge.IslemTarihi = DateTime.Now;
                    belge.IslemYapanIp = kModel.IslemYapanIp;
                    belge.IslemYapanID = UserIdentity.Current.Id;
                    belge.OgrenimDurumID = kModel.OgrenimDurumID;
                    belge.TeslimBaslangicSaat = kModel.TeslimBaslangicSaat;
                    belge.TeslimBitisSaat = kModel.TeslimBitisSaat;

                    ID = belge.BelgeTalepID;
                    msg = "Talep Düzenlendi.";
                }
                _entities.SaveChanges();
                #region mail
                if (yeniKayit)
                {
                    ID = kModel.BelgeTalepID;
                    kModel.BelgeTalepID = ID;
                    var mGonder = BelgeTalepAyar.YeniBelgeTalebindeMailGonder.GetAyarBt(enstituKod).ToBoolean().Value;
                    if (mGonder)
                        BilgiMaili(kModel, donem.DonemAdi, enstituKod);

                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add("Detaylı bilgiler mail adresinize iletilmiştir.<");

                }
                else
                {
                    if (kModel.BelgeDurumID == BelgeTalepDurumEnum.IptalEdildi)
                    {
                        mmMessage.IsSuccess = true;
                        mmMessage.Messages.Add("Belge talep işleminiz iptal edildi!");
                    }
                }
                #endregion
                MessageBox.Show(msg, "Uyarı", MessageBox.MessageType.Information);
                return RedirectToAction("Index");
            }
            else
            {
                mmMessage.IsSuccess = false;
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.BelgeDurumID = new SelectList(BelgeTalepBus.GetCmbBelgeTalepDurum(true, belgeDuzenleYetki, kModel.BelgeTalepID <= 0), "Value", "Caption", kModel.BelgeDurumID);
            ViewBag.BelgeTipID = new SelectList(BelgeTalepBus.GetCmbBelgeTipleri(true, kModel.OgrenimDurumID, enstituKod), "Value", "Caption", kModel.BelgeTipID);
            ViewBag.ProgramKod = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(enstituKod, true), "Value", "Caption", kModel.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(KullanicilarBus.CmbAktifOgrenimDurumu(true, isHesapKayittaGozuksun: true), "Value", "Caption", kModel.OgrenimDurumID);
            ViewBag.BelgeDilKodu = new SelectList(BelgeTalepBus.GetDiller(true), "Value", "Caption", kModel.BelgeDilKodu);
            ViewBag.MmMessage = mmMessage;
            int sayi = 10;
            if (kModel.OgrenimDurumID > 0 && kModel.BelgeTipID > 0)
            {
                var btip = BelgeTalepBus.GetBelgeTipDetay(kModel.BelgeTipID, kModel.OgrenimDurumID, enstituKod);
                if (btip.DonemlikKota.HasValue)
                {
                    sayi = btip.DonemlikKota.Value;
                }

            }
            ViewBag.IstenenBelgeSayisi = new SelectList(GetBelgeSayisi(sayi), "Value", "Caption", kModel.IstenenBelgeSayisi);

            return View(kModel);
        }
        public ActionResult TalepYapKontrol(BelgeTalepleri kModel, string ekd, bool? iptal, string dlgid = "")
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            string anketGiris = "";
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);

            if (kModel.BelgeTalepID <= 0 && kul.OgrenimDurumID != OgrenimDurumEnum.OzelOgrenci)
            {
                string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

                #region kontrol


                if (kModel.BelgeTipID <= 0)
                {
                    mmMessage.Messages.Add("Belge Tipi Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeTipID" });
                }
                else if (kModel.BelgeTipID == BelgeTalepTipiEnum.İlgiliMakama && kModel.BelgeAciklamasi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("İlgili Makam Açıklaması Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeAciklamasi" });
                }
                else if (kModel.BelgeTipID == BelgeTalepTipiEnum.Diğer)
                {
                    if (kModel.BelgeAdi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Belge Adı Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeAdi" });
                    }
                    if (kModel.BelgeAciklamasi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Belge Açıklaması Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeAciklamasi" });
                    }
                }
                if (kModel.BelgeDilKodu.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Belgenin hazırlanacağı Dili Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BelgeDilKodu" });
                }

                if (kModel.IstenenBelgeSayisi <= 0)
                {
                    mmMessage.Messages.Add("İstenilen Belge Sayısını Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IstenenBelgeSayisi" });
                }


                if (mmMessage.Messages.Count == 0)
                {
                    var ilkBtAnketAdi = BelgeTalepAyar.IlkBelgeTalebiAnketiAdi.GetAyarBt(enstituKod, "");
                    var ilkBtAnketId = _entities.Ankets.Where(p => p.AnketAdi == ilkBtAnketAdi).Select(s => s.AnketID).FirstOrDefault();
                    var ilkBelgeTalebiVar = _entities.BelgeTalepleris.Any(a => a.OgrenciNo == kul.OgrenciNo && kul.ProgramKod == a.ProgramKod) || ilkBtAnketAdi.IsNullOrWhiteSpace();
                    var kullaniciDonem4 = Convert.ToDouble((kul.KayitYilBaslangic.Value + 2) + "." + kul.KayitDonemID.Value);
                    var suankiDonem = DateTime.Now.ToEgitimOgretimYilBilgi();

                    var aktifDonem = Convert.ToDouble((suankiDonem.BaslangicYili) + "." + suankiDonem.Donem);
                    var anketAdi = "";
                    if (kullaniciDonem4 <= aktifDonem)
                    {
                        if (!_entities.BelgeTalepleris.Where(a => a.OgrenciNo == kul.OgrenciNo && kul.ProgramKod == a.ProgramKod).ToList().Any(a => a.AnketCevaplaris.All(a2 => a2.AnketID != ilkBtAnketId)))
                            anketAdi = BelgeTalepAyar.Donem4BelgeTalebiAnketiAdi.GetAyarBt(enstituKod, "");
                    }
                    else if (!ilkBelgeTalebiVar) anketAdi = ilkBtAnketAdi;

                    if (anketAdi != "")
                    {
                        var anketId = _entities.Ankets.Where(p => p.AnketAdi == anketAdi).Select(s => s.AnketID).FirstOrDefault();
                        var anketSorulari = (from bsa in _entities.Ankets.Where(p => p.AnketID == anketId)
                                             join aso in _entities.AnketSorus on bsa.AnketID equals aso.AnketID
                                             join sb in _entities.AnketCevaplaris.Where(p => p.AnketID == anketId && p.BelgeTalepID == kModel.BelgeTalepID) on aso.AnketSoruID equals sb.AnketSoruID into def1
                                             from sbc in def1.DefaultIfEmpty()
                                             select new
                                             {
                                                 aso.AnketSoruID,
                                                 AnketSoruSecenekID = sbc != null ? sbc.AnketSoruSecenekID : (int?)null,
                                                 Aciklama = sbc != null ? sbc.EkAciklama : "",
                                                 aso.SiraNo,
                                                 aso.SoruAdi,
                                                 aso.IsTabloVeriGirisi,
                                                 aso.IsTabloVeriMaxSatir,
                                                 Secenekler = (from s in aso.AnketSoruSeceneks
                                                               select new
                                                               {
                                                                   Value = s.AnketSoruSecenekID,
                                                                   s.SiraNo,
                                                                   s.IsEkAciklamaGir,
                                                                   s.IsYaziOrSayi,
                                                                   Caption = s.SecenekAdi
                                                               }).OrderBy(o => o.SiraNo).ToList()


                                             }).OrderBy(o => o.SiraNo).ToList();
                        var model = new KmAnketlerCevap
                        {
                            AnketTipID = 2,
                            AnketID = anketId,
                            JsonStringData = anketSorulari.ToJson()
                        };
                        foreach (var item in anketSorulari)
                        {
                            model.AnketCevapModel.Add(new AnketCevapDto
                            {
                                SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                                SoruBilgi = new FrAnketDetayDto { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama, IsTabloVeriGirisi = item.IsTabloVeriGirisi, IsTabloVeriMaxSatir = item.IsTabloVeriMaxSatir, },
                                SoruSecenek = item.Secenekler.Select(s => new FrAnketSecenekDetayDto { AnketSoruSecenekID = s.Value, SiraNo = s.SiraNo, IsEkAciklamaGir = s.IsEkAciklamaGir, IsYaziOrSayi = s.IsYaziOrSayi, SecenekAdi = s.Caption }).ToList(),
                                SelectListSoruSecenek = new SelectList(item.Secenekler.ToList(), "Value", "Caption", item.AnketSoruSecenekID)
                            });
                        }

                        anketGiris = ViewRenderHelper.RenderPartialView("Ajax", "getAnket", model);
                    }
                }
                #endregion

            }
            return new { IsSubmitOrAnketShow = anketGiris == "", AnketGiris = anketGiris }.ToJsonResult();
        }
        public ActionResult GetBilgi(int? belgeTalepId, int belgeTipId, int miktar, string ekd)
        {
            belgeTalepId = belgeTalepId <= 0 ? null : belgeTalepId;
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            int ogrenimDurumId;
            string ogrenciNo = "";
            if (belgeTalepId > 0)
            {
                var talep = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == belgeTalepId.Value);
                ogrenciNo = talep.OgrenciNo;
                ogrenimDurumId = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == belgeTalepId.Value).OgrenimDurumID;
            }
            else
            {
                var kul = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                ogrenimDurumId = kul.OgrenimDurumID.Value;
                ogrenciNo = kul.OgrenciNo;

            }
            var contentB = GetContent(belgeTalepId, belgeTipId, enstituKod, ogrenciNo, miktar, ogrenimDurumId);
            var HCB = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", contentB);
            return new { Deger = HCB }.ToJsonResult();

        }
        void BilgiMaili(BelgeTalepleri kModel, string donemAdi, string enstituKodu)
        {
            var htmlBigliRow = new List<MailTableRowDto>();
            var contentBilgi = new MailTableContentDto();
            var tutar = TutarHesapla(kModel);
            var btip = (from s in _entities.BelgeTipleris.Where(p => p.BelgeTipID == kModel.BelgeTipID)
                        join bt in _entities.BelgeTipDetays.Where(p => p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == kModel.BelgeTipID)) on new { kModel.OgrenimDurumID, kModel.EnstituKod } equals new { bt.OgrenimDurumID, bt.EnstituKod }
                        select new { s.BelgeTipID, s.BelgeTipAdi, bt.UcretAlimiVar, bt.BelgeFiyati, bt.UcretsizMiktar, bt.DonemlikKota, bt.UcretAciklamasiLink }).First();
            var OD = _entities.OgrenimDurumlaris.First(p => p.OgrenimDurumID == kModel.OgrenimDurumID);


            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Ad Soyad", Aciklama = kModel.AdiSoyadi });
            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Öğrenci No", Aciklama = kModel.OgrenciNo });
            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Eğitim Öğretim Yılı", Aciklama = kModel.OgretimYiliBaslangic + "/" + kModel.OgretimYiliBitis + " " + donemAdi });
            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Belge Tipi", Aciklama = btip.BelgeTipAdi });
            if (kModel.BelgeAdi.IsNullOrWhiteSpace() == false) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Belge Adı", Aciklama = kModel.BelgeAdi });
            if (kModel.BelgeAciklamasi.IsNullOrWhiteSpace() == false) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Belge Açıklaması", Aciklama = kModel.BelgeAciklamasi });

            htmlBigliRow.Add(new MailTableRowDto { Baslik = "İstenen Belge Sayısı", Aciklama = kModel.IstenenBelgeSayisi + " Adet" });
            //if (kModel.OgrenimDurumID == OgrenimDurum.Mezun) htmlBigliRow.Add(new HtmlContentBilgiRow { Baslik = "NOT", Aciklama = "Mezun olmamış öğrencilerin aynı dönem içinde alabileceği maksimum belge sayısı 3 adettir, eğer 3 adetten fazla belgeye ihtiyaç duyuyorsa daha önceden almış olduğu belgenin fotokopisini çektirip kurum tarafından 'Aslı Gibidir' kaşesi vurdurabilir." });
            if (kModel.UcretsizMiktar.HasValue) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Dönemlik ücretsiz alınabilecek belge sayısı", Aciklama = kModel.UcretsizMiktar.Value + " Adet" });
            if (kModel.UcretAlimiVar) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Adet Fiyatı", Aciklama = kModel.BelgeFiyati.Value + " TL" });
            if (tutar.VerilenBelgeTutar > 0) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Toplam Fiyat", Aciklama = tutar.VerilenBelgeTutar + " TL" });
            htmlBigliRow.Add(kModel.UcretAlimiVar
                ? new MailTableRowDto
                {
                    Baslik = "Ödeme Bilgisi",
                    Aciklama = "<a href=" + btip.UcretAciklamasiLink + "> Ödeme bilgisi için tıklayınız.</a>"
                }
                : new MailTableRowDto
                {
                    Baslik = "Ücret Bilgisi",
                    Aciklama = "Belge Ücretsizdir lütfen verilen randevu zamanında belgenizi alınız."
                });

            if (kModel.DonemlikKota.HasValue) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Not", Aciklama = "Bu belge tipi için dönemlik alınabilecek maksimum belge sayısı " + kModel.DonemlikKota.Value + " adettir, eğer " + kModel.DonemlikKota.Value + " adetten fazla belgeye ihtiyaç duyuluyorsa daha önceden alınmış olan belgenin fotokopisini çektirilip kurum tarafından 'Aslı Gibidir' kaşesi vurdurulabilir." });
            var konu = "";
            if (kModel.BelgeDurumID == BelgeTalepDurumEnum.TalepEdildi)
            {
                var belgeAlimAdresi = BelgeTalepAyar.BelgeAlımAdresi.GetAyarBt(enstituKodu, "");
                contentBilgi.AciklamaDetayi = GunHesap(kModel.TalepTarihi, kModel.EklenecekGun, kModel.TeslimBaslangicSaat.Value, kModel.TeslimBitisSaat.Value, kModel.UcretAlimiVar, belgeAlimAdresi);
                contentBilgi.AciklamaBasligi = "Belge Talebi İşlemi Yapıldı";
                konu = "Belge Talebi İşlemi Yapıldı";
            }
            else if (kModel.BelgeDurumID == BelgeTalepDurumEnum.Kapatildi)
            {
                contentBilgi.AciklamaDetayi = "Reddedilme Nedeni: " + kModel.BelgeDurumAciklamasi;
                contentBilgi.AciklamaBasligi = "Belge talep işleminiz reddedildi!";
                konu = "Belge talep işleminiz reddedildi!";

            }

            contentBilgi.Detaylar = htmlBigliRow;

            var mmmC = new MailMainContentDto();
            var enstitu = _entities.Enstitulers.First(p => p.EnstituKod == enstituKodu);
            var enstituAdi = enstitu.EnstituAd;
            mmmC.EnstituAdi = enstituAdi;
            mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
            var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(enstituKodu);
            var sistemErisimAdresi = mailBilgi.SistemErisimAdresi;
            var wurlAddr = sistemErisimAdresi.Split('/').ToList();
            if (sistemErisimAdresi.Contains("//"))
                sistemErisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
            else
                sistemErisimAdresi = "http://" + wurlAddr.First();
            mmmC.LogoPath = sistemErisimAdresi + "/Content/assets/images/ytu_logo_tr.png";
            var hcb = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
            mmmC.Content = hcb;
            mmmC.WebAdresi = enstitu.WebAdresi;
            string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
            MailManager.SendMail(mailBilgi.EnstituKod, konu, htmlMail, kModel.Email, null);
        }

        MailTableContentDto GetContent(int? belgeTalepId, int belgeTipId, string enstituKod, string numara, int miktar, int ogrenimDurumId)
        {
            var kModel = new BelgeTalepleri
            {
                EnstituKod = enstituKod
            };

            if (belgeTalepId.HasValue)
            {
                kModel = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == belgeTalepId.Value);
            }
            else
            {
                var eoYil = DateTime.Now.ToEgitimOgretimYilBilgi();
                kModel.OgretimYiliBaslangic = eoYil.BaslangicYili;
                kModel.OgretimYiliBitis = eoYil.BitisYili;
                kModel.DonemID = eoYil.Donem;
                kModel.OgrenimDurumID = ogrenimDurumId;
                kModel.BelgeTipID = belgeTipId;
                kModel.OgrenciNo = numara;
                kModel.IstenenBelgeSayisi = miktar;
                kModel.TalepTarihi = DateTime.Now;


            }
            var btiap = BelgeTalepBus.GetBelgeTipDetay(belgeTipId, ogrenimDurumId, enstituKod);
            var saatB = BelgeTalepBus.GetCmbSelectedSaat(kModel.TalepTarihi, kModel.BelgeTipID, kModel.OgrenimDurumID, enstituKod);
            kModel.EklenecekGun = saatB.EklenecekGun;
            kModel.TeslimBaslangicSaat = saatB.TeslimBaslangicSaat;
            kModel.TeslimBitisSaat = saatB.TeslimBitisSaat;
            kModel.UcretAciklamasiLink = btiap.UcretAciklamasiLink;
            kModel.UcretAlimiVar = btiap.UcretAlimiVar;
            kModel.UcretsizMiktar = btiap.UcretsizMiktar;
            kModel.DonemlikKota = btiap.DonemlikKota;
            var donem = _entities.Donemlers.First(p => p.DonemID == kModel.DonemID);
            var ayniDonemAlinanBelge = AyniDonemAlinenBelgeSayisi(kModel);
            var ayniDonemTalepEdilenBelge = AyniDonemTalepEdilenBelgeSayisi(kModel);
            var htmlBigliRow = new List<MailTableRowDto>();
            var contentBilgi = new MailTableContentDto
            {
                CaptTdWidth = 200
            };
            var tutar = TutarHesapla(kModel);

            var btip = (from s in _entities.BelgeTipleris
                        where s.BelgeTipID == kModel.BelgeTipID
                        select new { s.BelgeTipID, s.BelgeTipAdi }).First();
            string aciklamaDetayi = "";
            bool kotaVar = true;
            if (kModel.DonemlikKota.HasValue)
            {
                if (ayniDonemAlinanBelge + ayniDonemTalepEdilenBelge + miktar > kModel.DonemlikKota.Value)
                {
                    aciklamaDetayi = "İstediğiniz belge tipi için bu dönem alabileceğiniz toplam miktar " + kModel.DonemlikKota.Value + " adet dir. Daha fazla miktar için başvuru yapamazsınız!";
                    kotaVar = false;
                }
                else
                {
                    var belgeAlimAdresi = BelgeTalepAyar.BelgeAlımAdresi.GetAyarBt(enstituKod, "");
                    aciklamaDetayi = GunHesap(kModel.TalepTarihi, kModel.EklenecekGun, kModel.TeslimBaslangicSaat.Value, kModel.TeslimBitisSaat.Value, kModel.UcretAlimiVar, belgeAlimAdresi);
                }
            }
            else
            {
                var belgeAlimAdresi = BelgeTalepAyar.BelgeAlımAdresi.GetAyarBt(enstituKod, "");
                aciklamaDetayi = GunHesap(kModel.TalepTarihi, kModel.EklenecekGun, kModel.TeslimBaslangicSaat.Value, kModel.TeslimBitisSaat.Value, kModel.UcretAlimiVar, belgeAlimAdresi);
            }

            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Eğitim Öğretim Yılı ", Aciklama = kModel.OgretimYiliBaslangic + "/" + kModel.OgretimYiliBitis + " " + donem.DonemAdi });
            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Belge Tip Adı", Aciklama = btip.BelgeTipAdi });
            if (kModel.DonemlikKota.HasValue) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Dönemde Alabileceğiniz Toplam Belge", Aciklama = kModel.DonemlikKota.Value + " Adet" });
            if (kotaVar == false) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Not", Aciklama = "Bu belge tipi için dönemlik alınabilecek maksimum belge sayısı " + kModel.DonemlikKota.Value + " adettir, eğer " + kModel.DonemlikKota.Value + " adetten fazla belgeye ihtiyaç duyuluyorsa daha önceden alınmış olan belgenin fotokopisini çektirilip kurum tarafından 'Aslı Gibidir' kaşesi vurdurulabilir." });
            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Dönemde Daha Önceden Alınan Toplam Belge Sayısı", Aciklama = AyniDonemAlinenBelgeSayisi(kModel) + " Adet" });
            htmlBigliRow.Add(new MailTableRowDto { Baslik = "Dönemde Daha Önceden Talep Edilip Henüz Alınmayan Toplam Belge", Aciklama = ayniDonemTalepEdilenBelge + " Adet" });
            htmlBigliRow.Add(new MailTableRowDto { Baslik = "İstenilen Belge Sayısı", Aciklama = kModel.IstenenBelgeSayisi + " Adet" });
            if (kModel.UcretsizMiktar.HasValue) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Dönemlik ücretsiz alınabilecek belge sayısı", Aciklama = kModel.UcretsizMiktar.Value + " Adet" });
            if (kModel.UcretAlimiVar) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Adet Fiyatı", Aciklama = kModel.BelgeFiyati.Value + " TL" });
            if (kModel.UcretAlimiVar) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Toplam Fiyat", Aciklama = tutar.VerilenBelgeTutar + " TL" });
            else htmlBigliRow.Add(new MailTableRowDto { Baslik = "Ücret Bilgisi", Aciklama = "Belge Ücretsizdir lütfen verilen randevu zamanında belgenizi alınız." });

            if (kModel.DonemlikKota.HasValue) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Not", Aciklama = "İstediğiniz belge tipi için bu dönem alabileceğiniz toplam miktar " + kModel.DonemlikKota.Value + " adet dir. Daha fazla miktar için başvuru yapamazsınız!" });

            if (kModel.UcretAlimiVar && kotaVar && kModel.VerilenBelgeTutar > 0 && kModel.UcretAciklamasiLink.IsNullOrWhiteSpace() == false)
                htmlBigliRow.Add(new MailTableRowDto { Baslik = "Ödeme Bilgisi", Aciklama = "<a href=" + kModel.UcretAciklamasiLink + " target=_blank>Ödeme bilgisi için tıklayınız</a>" });
            contentBilgi.AciklamaDetayi = aciklamaDetayi;
            contentBilgi.Success = kotaVar;
            contentBilgi.AciklamaBasligi = "Belge talep açıklaması";
            contentBilgi.Detaylar = htmlBigliRow;

            return contentBilgi;

        }

        [Authorize(Roles = RoleNames.BelgeTalebiDuzelt)]
        public ActionResult Istenenkaydet(int id, int islemTipId, string islemTipAciklamasi, int miktar)
        {


            var belge = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == id);
            var oldTdid = belge.BelgeDurumID;
            if (islemTipId == BelgeTalepDurumEnum.Verildi)
            {
                var bel2 = TutarHesapla(belge);
                belge.VerilenBelgeSayisi = miktar;
                belge.BelgeFiyati = bel2.BelgeFiyati;
                belge.VerilenBelgeTutar = bel2.VerilenBelgeTutar;
            }
            else if (islemTipId == BelgeTalepDurumEnum.Kapatildi && islemTipId != oldTdid)
            {
                belge.BelgeDurumAciklamasi = islemTipAciklamasi;
                belge.VerilenBelgeSayisi = null;
                belge.VerilenBelgeTutar = null;
            }
            belge.BelgeDurumID = islemTipId;
            belge.IslemTarihi = DateTime.Now;
            belge.IslemYapanID = UserIdentity.Current.Id;
            belge.IslemYapanIp = UserIdentity.Ip;
            _entities.SaveChanges();
            if (islemTipId == BelgeTalepDurumEnum.Kapatildi && islemTipId != oldTdid)
            {
                var donem = belge.Donemler;
                BilgiMaili(belge, donem.DonemAdi, belge.EnstituKod);
            }

            return new
            {
                IslemTipListeAdi = belge.BelgeDurumlari.DurumAdi,
                belge.BelgeDurumlari.ClassName,
                belge.BelgeDurumlari.Color,
                verilmeBilgi = BelgeTalepBus.GetBelgeVerilmeBilgisi(belge.BelgeTalepID, belge.BelgeDurumlari.DurumAdi)
            }.ToJsonResult();
        }


        public BelgeTalepleri TutarHesapla(BelgeTalepleri mdl)
        {
            var ayniDonemAlinan = AyniDonemAlinenBelgeSayisi(mdl);
            int ucretsizMiktar = 0;
            double belgeFiyati = 0;

            if (mdl.BelgeTalepID <= 0)
            {
                var btip = _entities.BelgeTipDetays.First(bt => bt.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == mdl.BelgeTipID) && bt.OgrenimDurumID == mdl.OgrenimDurumID && bt.EnstituKod == mdl.EnstituKod);
                mdl.UcretAlimiVar = btip.UcretAlimiVar;
                mdl.BelgeFiyati = btip.BelgeFiyati;
                mdl.UcretsizMiktar = btip.UcretsizMiktar;
            }
            if (!mdl.UcretAlimiVar)
            {
                mdl.BelgeFiyati = belgeFiyati = 0;
                ucretsizMiktar = 0;
            }
            else
            {

                mdl.BelgeFiyati = belgeFiyati = mdl.BelgeFiyati.Value;
                ucretsizMiktar = mdl.UcretsizMiktar ?? 0;
            }
            int belgeSayisi = 0;
            if (mdl.BelgeDurumID == BelgeTalepDurumEnum.Verildi) belgeSayisi = mdl.VerilenBelgeSayisi ?? 1;
            else belgeSayisi = mdl.IstenenBelgeSayisi;

            mdl.VerilenBelgeTutar = ayniDonemAlinan >= ucretsizMiktar ? (belgeSayisi * belgeFiyati) : (((belgeSayisi + ayniDonemAlinan) - ucretsizMiktar) * belgeFiyati);
            if (mdl.VerilenBelgeTutar < 0) mdl.VerilenBelgeTutar = 0;
            return mdl;
        }

        public ActionResult MiktarHesapla(int id, int miktar)
        {

            var belge = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == id);
            if (belge.BelgeDurumID == BelgeTalepDurumEnum.Verildi) belge.VerilenBelgeSayisi = miktar;
            else belge.IstenenBelgeSayisi = miktar;
            var bel2 = TutarHesapla(belge);
            return bel2.VerilenBelgeTutar.ToJsonResult();
        }
        string GunHesap(DateTime islemTarihi, int eklenecekGun, TimeSpan teslimBaslangicSaat, TimeSpan teslimBitisSaat, bool ucretli, string adres)
        {
            adres = "Talep ettiğiniz belgeyi " + islemTarihi.AddDays(eklenecekGun).ToFormatDate() + " Tarihi Saat " + teslimBaslangicSaat.ToString(@"hh\:mm") + " ile " + teslimBitisSaat.ToString(@"hh\:mm") + " arası Banka Dekontu ile birlikte " + adres + " adresine gelip alabilirsiniz";
            return adres;
        }
        public int AyniDonemAlinenBelgeSayisi(BelgeTalepleri mdl)
        {
            var data = _entities.BelgeTalepleris.Where(p => p.BelgeDurumID == BelgeTalepDurumEnum.Verildi &&
                                                      p.BelgeTipID == mdl.BelgeTipID &&
                                                      p.OgretimYiliBaslangic == mdl.OgretimYiliBaslangic &&
                                                      p.OgretimYiliBitis == mdl.OgretimYiliBitis &&
                                                      p.DonemID == mdl.DonemID && p.OgrenciNo == mdl.OgrenciNo && p.BelgeTalepID != mdl.BelgeTalepID).ToList();

            int topl = 0;
            foreach (var item in data)
                topl += item.VerilenBelgeSayisi ?? 0;
            return topl;

        }
        public int AyniDonemTalepEdilenBelgeSayisi(BelgeTalepleri mdl)
        {
            var data = _entities.BelgeTalepleris.Where(p => (p.BelgeDurumID == BelgeTalepDurumEnum.TalepEdildi || p.BelgeDurumID == BelgeTalepDurumEnum.Hazirlandi || p.BelgeDurumID == BelgeTalepDurumEnum.Hazirlaniyor) &&
                                                      p.BelgeTipID == mdl.BelgeTipID &&
                                                      p.OgretimYiliBaslangic == mdl.OgretimYiliBaslangic &&
                                                      p.OgretimYiliBitis == mdl.OgretimYiliBitis &&
                                                      p.DonemID == mdl.DonemID && p.OgrenciNo == mdl.OgrenciNo && p.BelgeTalepID != mdl.BelgeTalepID).ToList();

            int topl = 0;
            foreach (var item in data)
                topl += item.IstenenBelgeSayisi;
            return topl;

        }

        [Authorize]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = true
            };
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            var belge = _entities.BelgeTalepleris.FirstOrDefault(p => p.BelgeTalepID == id && p.KullaniciID == kul.KullaniciID);
            if (mmMessage.IsSuccess)
            {
                try
                {
                    _entities.BelgeTalepleris.Remove(belge);
                    _entities.SaveChanges();
                    mmMessage.Messages.Add("Belge Talebi Silindi.");
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Belge Talebi Silinemedi.");
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex, LogTipiEnum.OnemsizHata);
                }
            }
            return mmMessage.ToJsonResult();
        }
    }
}