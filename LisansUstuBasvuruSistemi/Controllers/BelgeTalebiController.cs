using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class BelgeTalebiController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            return Index(new fmBelgeTalepleri() { PageSize = 10 }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmBelgeTalepleri model, string EKD)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var Kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();

            var bbModel = new BasvuruBilgiModel();
            bbModel.Kullanici = Kul;
            bbModel.SistemBasvuruyaAcik = BelgeTalepAyar.BelgeTalebiAcikmi.getAyarBT(_EnstituKod, "0").ToBoolean().Value;
            bbModel.DonemAdi = Management.getAkademikBulundugumuzTarih(DateTime.Now).Caption;
            bbModel.EnstituYetki = UserIdentity.Current.SeciliEnstituKodu.Contains(_EnstituKod) || UserIdentity.Current.SeciliEnstituKodu == _EnstituKod;
            bbModel.Enstitü = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
            bbModel.KullaniciTipYetki = false;
            if (Kul.YtuOgrencisi)
            {
                if (Kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                {
                    var kullKayitB = Management.KullaniciKayitBilgisiGuncelle(Kul.KullaniciID);
                    if (kullKayitB.KayitVar)
                    {
                        Kul.KayitYilBaslangic = kullKayitB.BaslangicYil;
                        Kul.KayitDonemID = kullKayitB.DonemID;
                        Kul.KayitTarihi = kullKayitB.KayitTarihi;
                        db.SaveChanges();
                        bbModel.KullaniciTipYetki = true;

                    }
                    else bbModel.KullaniciTipYetkiYokMsj = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Profil bilgilerinizde giriş yaptığınız YTU Lüsansüstü Öreğnci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";

                }
                else bbModel.KullaniciTipYetki = true;
                if (bbModel.KullaniciTipYetki)
                {
                    var otb = db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod && p.OgrenimTipKod == Kul.OgrenimTipKod).First();
                    //  bbModel.KayitDonemi = Kul.KayitYilBaslangic + "/" + (Kul.KayitYilBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == Kul.KayitDonemID.Value).First().DonemAdi + " , " + Kul.KayitTarihi.ToString("dd.MM.yyyy");
                    bbModel.OgrenimDurumAdi = Kul.OgrenimDurumlari.OgrenimDurumAdi;
                    bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                    bbModel.AnabilimdaliAdi = Kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    bbModel.ProgramAdi = Kul.Programlar.ProgramAdi;
                    bbModel.OgrenciNo = Kul.OgrenciNo;
                }
            }

            ViewBag.bModel = bbModel;

            #region data
            var q = from s in db.BelgeTalepleris
                    join ibt in db.BelgeTipleris on s.BelgeTipID equals ibt.BelgeTipID
                    join btit in db.BelgeDurumlaris on s.BelgeDurumID equals btit.BelgeDurumID
                    join d in db.Donemlers on s.DonemID equals d.DonemID
                    join dk in db.SistemDilleris on s.BelgeDilKodu equals dk.DilKodu
                    join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                    join od in db.OgrenimDurumlaris on s.OgrenimDurumID equals od.OgrenimDurumID
                    join kul in db.Kullanicilars on s.OgrenciNo equals kul.OgrenciNo into defk
                    from kl in defk.DefaultIfEmpty()
                    where s.OgrenciNo == Kul.OgrenciNo && s.EnstituKod == _EnstituKod
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


            if (model.OgrenimDurumID.HasValue) q = q.Where(p => p.OgrenimDurumID == model.OgrenimDurumID);
            if (model.DilKodu.IsNullOrWhiteSpace() == false) q = q.Where(p => p.BelgeDilKodu == model.DilKodu);
            if (model.AranacakKelime.IsNullOrWhiteSpace() == false) q = q.Where(p => p.AdiSoyadi.Contains(model.AranacakKelime) || p.Telefon == model.AranacakKelime || p.Email.Contains(model.AranacakKelime) || p.OgrenciNo == model.AranacakKelime);
            if (model.BelgeTipID.HasValue) q = q.Where(p => p.BelgeTipID == model.BelgeTipID);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (model.ProgramKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.ProgramKod == model.ProgramKod);
            if (model.BelgeID.HasValue) q = q.Where(p => p.BelgeTalepID == model.BelgeID.Value);
            if (model.BelgeDurumID.HasValue) q = q.Where(p => p.BelgeDurumID == model.BelgeDurumID.Value);
            if (model.OgretimYili.IsNullOrWhiteSpace() == false)
            {
                var oy = model.OgretimYili.Split('/').ToList();
                var bas = oy[0].ToInt().Value;
                var bit = oy[1].ToInt().Value;
                var done = oy[2].ToInt().Value;
                q = q.Where(p => p.OgretimYiliBaslangic == bas && p.OgretimYiliBitis == bit && p.DonemID == done);
            }


            if (model.Sort.IsNullOrWhiteSpace() == false) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.TalepTarihi);




            model.RowCount = q.Count();
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;

            var IndexModel = new MIndexBilgi();
            var btDurulari = Management.BelgeTalepDurumList();

            IndexModel.Toplam = model.RowCount;
            model.Data = q.Skip(PS.StartRowIndex).Take(model.PageSize).Select(item => new frBelgeTalepleri
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

            ViewBag.IndexModel = IndexModel;

            #endregion

            ViewBag.BelgeTipID = new SelectList(Management.cmbBelgeTipleri(true), "Value", "Caption", model.BelgeTipID);
            ViewBag.OgretimYili = new SelectList(Management.getAkademikTarih(true), "Value", "Caption", model.OgretimYili);
            ViewBag.BelgeDurumID = new SelectList(Management.cmbBelgeTalepDurumListe(true), "Value", "Caption", model.BelgeDurumID);
            return View(model);
        }

        public ActionResult getdetay(int id, string EKD)
        {
            var kYetki = RoleNames.BelgeTalebiDuzelt.InRoleCurrent();
            var belgeTalebi = (from s in db.BelgeTalepleris
                               join ibt in db.BelgeTipleris on s.BelgeTipID equals ibt.BelgeTipID
                               join btit in db.BelgeDurumlaris on s.BelgeDurumID equals btit.BelgeDurumID
                               join d in db.Donemlers on s.DonemID equals d.DonemID
                               join dk in db.SistemDilleris on s.BelgeDilKodu equals dk.DilKodu
                               join ot in db.OgrenimTipleris on new { s.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                               join od in db.OgrenimDurumlaris on s.OgrenimDurumID equals od.OgrenimDurumID
                               join kul in db.Kullanicilars on s.OgrenciNo equals kul.OgrenciNo into defk
                               from kl in defk.DefaultIfEmpty()
                               join prg in db.Programlars on s.ProgramKod equals prg.ProgramKod
                               where s.BelgeTalepID == id
                               select new BelgeTalepleriDetaymodel
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
            var bel = db.BelgeTalepleris.Where(p => p.BelgeTalepID == id).First();
            var belh = tutarHesapla(bel);
            belgeTalebi.VerilenBelgeTutar = belh.VerilenBelgeTutar;
            belgeTalebi.BelgeTalepID = -1;
            belgeTalebi.SeciliDonemdeVerilenMiktar = AyniDonemAlinenBelgeSayisi(belgeTalebi);
            belgeTalebi.SeciliDonemdehenuzVerilmeyenMiktar = AyniDonemTalepEdilenBelgeSayisi(belgeTalebi);
            belgeTalebi.BelgeTalepID = belh.BelgeTalepID;
            ViewBag.VerilenBelgeSayisi = new SelectList(getBelgeSayisi(), "Value", "Caption", belgeTalebi.BelgeDurumID == BelgeTalepDurum.Verildi ? belgeTalebi.VerilenBelgeSayisi : belgeTalebi.IstenenBelgeSayisi);
            ViewBag.BelgeDurumID = new SelectList(Management.cmbBelgeTalepDurum(true, kYetki), "Value", "Caption", belgeTalebi.BelgeDurumID);

            return View(belgeTalebi);
        }
        public static List<CmbIntDto> getBelgeSayisi(int MaxBelgeS = 10)
        {
            var dct = new List<CmbIntDto>();
            for (int i = 1; i <= MaxBelgeS; i++)
            {
                dct.Add(new CmbIntDto { Value = i, Caption = i.ToString() });
            }
            return dct;
        }
        public ActionResult getBelgeSayisiA(int? BelgeTalepID, string OgrenciNo, int BelgeTipID)
        {
            int OgrenimDurumID;
            if (BelgeTalepID.HasValue && BelgeTalepID.Value > 0)
            {
                OgrenimDurumID = db.BelgeTalepleris.Where(p => p.BelgeTalepID == BelgeTalepID.Value).First().OgrenimDurumID;
            }
            else
            {
                OgrenimDurumID = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First().OgrenimDurumID.Value;

            }
            var bsay = new List<CmbIntDto>();
            var btip = db.BelgeTipDetays.Where(p => p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == BelgeTipID) && p.OgrenimDurumID == OgrenimDurumID).FirstOrDefault();//düzeltilecek enstitu filtresi
            if (btip != null && btip.DonemlikKota.HasValue)
            {
                bsay = getBelgeSayisi(btip.DonemlikKota.Value);
            }
            else
            {
                bsay = getBelgeSayisi(10);
            }
            return bsay.Select(s => new { Key = s.Value, Value = s.Caption }).ToList().toJsonResult();
        }

        public ActionResult TalepYap(string EKD, string Kod = "", int? id = null)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var belge = new BelgeTalepleri();
            var MmMessage = new MmMessage();
            bool belgeDuzenleYetki = RoleNames.BelgeTalebiDuzelt.InRoleCurrent();
            var kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
            if (BelgeTalepAyar.BelgeTalebiAcikmi.getAyarBT(_EnstituKod, "0").ToBoolean().Value)
            {

                if (id.HasValue)
                {
                    belge = db.BelgeTalepleris.Where(p => p.BelgeTalepID == id.Value).First();
                    if (belgeDuzenleYetki == false)
                    {
                        if (belge.OgrenciNo != kul.OgrenciNo && belge.IslemYapanID != kul.KullaniciID)
                        {
                            Management.SistemBilgisiKaydet("Farklı bir kullanıcıya ait belge talebi güncellenmek isteniyor! \r\n BelgeTalepID:" + belge.BelgeTalepID + " \r\n Ad Soyad" + belge.AdiSoyadi, "BelgeTalebi/TalepYap", BilgiTipi.Saldırı);
                            MmMessage.Messages.Add("Size ait olmayan bir belgeyi düzenlemeye hakkınız yoktur!");

                        }
                        else if (belge.BelgeDurumID == BelgeTalepDurum.IptalEdildi || belge.BelgeDurumID == BelgeTalepDurum.Kapatildi || belge.BelgeDurumID == BelgeTalepDurum.Verildi)
                        {
                            var bDurumAdi = belge.BelgeDurumlari.DurumAdi;
                            MmMessage.Messages.Add("Bu Belge Talebini Düzeltemezsiniz.");
                        }


                    }
                }
                else if (Kod.IsNullOrWhiteSpace() == false)
                {
                    var ID = Kod.Split('_')[0].ToInt().Value;
                    var kd = Kod.Split('_')[1].ToString();
                    var belgeT = db.BelgeTalepleris.Where(p => p.BelgeTalepID == ID && p.ErisimKodu == kd).FirstOrDefault();
                    if (belgeT == null)
                    {
                        string msg = "Aranılan belge bilgisi sistemde bulunamadı!";
                        MmMessage.Messages.Add(msg);
                    }
                    else
                    {
                        if (belgeT.BelgeDurumID == BelgeTalepDurum.IptalEdildi)
                            MmMessage.Messages.Add("Aranılan belge iptal edildiğinden dolayı herhangi bir işlem yapamazsınız!");
                        if (belgeT.BelgeDurumID == BelgeTalepDurum.Verildi)
                            MmMessage.Messages.Add("Aranılan belge talebi daha önceden işlem gördüğünden herhangi bir işlem yapamazsınız!");
                        else belge = belgeT;

                    }
                }
                else if (kul.YtuOgrencisi == false)
                {
                    MmMessage.Messages.Add("Belge talebi yapabilmek için profil bilginizi düzeltip ytu öğrencisi olduğunuzu belirtiniz");
                    MessageBox.Show("Uyarı", MessageBox.MessageType.Information, MmMessage.Messages.ToArray());
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

                    if (kul.OgrenimDurumID != OgrenimDurum.OzelOgrenci && kul.KayitTarihi.HasValue == false)
                    {
                        var ogrenciBilgi = Management.StudentControl(kul.TcKimlikNo);
                        if (ogrenciBilgi.Hata)
                        {
                            MmMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu!");
                        }
                        else
                        {
                            if (ogrenciBilgi.KayitVar)
                            {
                                kul.KayitTarihi = ogrenciBilgi.KayitTarihi;
                                kul.KayitYilBaslangic = ogrenciBilgi.BaslangicYil;
                                kul.KayitDonemID = ogrenciBilgi.DonemID;
                                db.SaveChanges();
                            }
                            else
                            {
                                MmMessage.Messages.Add("Öğrenci Bilgileriniz Doğrulanamadı!");
                                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                            }
                        }
                    }

                }
            }
            else
            {
                MmMessage.Messages.Add("Sistem belge talebi işlemine kapalıdır.");
            }
            if (MmMessage.Messages.Count > 0)
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Information, MmMessage.Messages.ToArray());
                return RedirectToAction("Index");
            }
            ViewBag.BelgeDurumID = new SelectList(Management.cmbBelgeTalepDurum(true, belgeDuzenleYetki, belge.BelgeTalepID <= 0), "Value", "Caption", belge.BelgeDurumID);
            ViewBag.BelgeTipID = new SelectList(Management.cmbBelgeTipleri(true, belge.OgrenimDurumID, _EnstituKod), "Value", "Caption", belge.BelgeTipID);

            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(_EnstituKod, true), "Value", "Caption", belge.ProgramKod);
            //ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(_EnstituKod true), "Value", "Caption", belge.OgrenimTipKod);
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", belge.OgrenimDurumID);
            ViewBag.BelgeDilKodu = new SelectList(Management.GetDiller(true), "Value", "Caption", belge.BelgeDilKodu);
            ViewBag.MmMessage = MmMessage;
            if (belge.BelgeTalepID > 0)
            {
                var btip = db.BelgeTipDetays.Where(p => p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == belge.BelgeTipID) && p.EnstituKod == belge.EnstituKod && p.OgrenimDurumID == belge.OgrenimDurumID).First();
                ViewBag.IstenenBelgeSayisi = new SelectList(getBelgeSayisi(btip.DonemlikKota.HasValue ? btip.DonemlikKota.Value : 10), "Value", "Caption", belge.IstenenBelgeSayisi);
            }
            else ViewBag.IstenenBelgeSayisi = new SelectList(new Dictionary<int, int>(), "Value", "Caption", belge.IstenenBelgeSayisi);

            return View(belge);
        }

        [HttpPost]
        public ActionResult TalepYap(BelgeTalepleri kModel, string EKD, bool? Iptal)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            bool belgeDuzenleYetki = RoleNames.BelgeTalebiDuzelt.InRoleCurrent();
            var MmMessage = new MmMessage();


            #region kontrol

            if (kModel.OgrenciNo.IsNullOrWhiteSpace())
            {

                MmMessage.Messages.Add("Öğrenci Numarası Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
            }
            else
            {
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenciNo" });
                if (kModel.BelgeTalepID <= 0)
                {
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
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
                    var bsv = db.BelgeTalepleris.Where(p => p.BelgeTalepID == kModel.BelgeTalepID).First();

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
                MmMessage.Messages.Add("Belge Tipini Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeTipID" });
            }
            else if (kModel.BelgeTipID == BelgeTalepTip.İlgiliMakama && kModel.BelgeAciklamasi.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("İlgili Makam Açıklaması Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeAciklamasi" });
            }
            else if (kModel.BelgeTipID == BelgeTalepTip.Diğer)
            {
                if (kModel.BelgeAdi.IsNullOrWhiteSpace())
                {

                    MmMessage.Messages.Add("Belge Adını Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeAdi" });
                }
                if (kModel.BelgeAciklamasi.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Belge Açıklaması Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeAciklamasi" });
                }
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BelgeTipID" });
            if (kModel.BelgeDilKodu.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Belgenin Hazırlanacağı Dili Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeDilKodu" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BelgeDilKodu" });
            if (kModel.IstenenBelgeSayisi <= 0)
            {

                MmMessage.Messages.Add("İstenen Belge Sayısını Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IstenenBelgeSayisi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IstenenBelgeSayisi" });

            if (kModel.BelgeTalepID <= 0)
            {
                kModel.BelgeDurumID = BelgeTalepDurum.TalepEdildi;//talep edildi
            }
            else
            {

                if (kModel.BelgeDurumID <= 0)
                {

                    MmMessage.Messages.Add("Talep Durumunu Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeDurumID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BelgeDurumID" });


            }

            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                var belCont = getContent((kModel.BelgeTalepID == 0 ? (int?)null : kModel.BelgeTalepID), kModel.BelgeTipID, _EnstituKod, kModel.OgrenciNo, kModel.IstenenBelgeSayisi, kModel.OgrenimDurumID);
                if (belCont.Success == false)
                {
                    var eoYil = DateTime.Now.toEoYilBilgi();
                    var btip = Management.getBtipDetay(kModel.BelgeTipID, kModel.OgrenimDurumID, _EnstituKod);
                    if (kModel.BelgeTalepID <= 0)
                    {
                        kModel.UcretAlimiVar = btip.UcretAlimiVar;
                        kModel.UcretsizMiktar = btip.UcretsizMiktar;
                        kModel.DonemlikKota = btip.DonemlikKota;
                        kModel.BelgeFiyati = btip.BelgeFiyati;
                    }

                    MmMessage.Messages.Add("Bu belge tipi için aynı dönem içinde alabileceğiniz toplam belge sayısı " + kModel.DonemlikKota.Value + " adettir, eğer " + kModel.DonemlikKota.Value + " adetten fazla belgeye ihtiyaç duyuluyorsanız daha önceden almış olduğunuz belgenin fotokopisini çektirip kurum tarafından 'Aslı Gibidir' kaşesi vurdurabilirsiniz.");
                }
            }
            kModel.EnstituKod = _EnstituKod;
            if (MmMessage.Messages.Count == 0)
            {
                string msg = "";
                var YeniKayit = kModel.BelgeTalepID <= 0;
                var eoYil = DateTime.Now.toEoYilBilgi();
                var donem = db.Donemlers.Where(p => p.DonemID == eoYil.Donem).First();


                kModel.OgretimYiliBaslangic = eoYil.BaslangicYili;
                kModel.OgretimYiliBitis = eoYil.BitisYili;
                kModel.DonemID = eoYil.Donem;
                int ID = 0;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIp = UserIdentity.Ip;


                if (YeniKayit)
                {
                    kModel.TalepTarihi = DateTime.Now;
                    var belgeSaat = Management.getSelectedSaat(kModel.TalepTarihi, kModel.BelgeTipID, kModel.OgrenimDurumID, _EnstituKod);
                    kModel.EklenecekGun = belgeSaat.EklenecekGun;
                    kModel.TeslimBaslangicSaat = belgeSaat.TeslimBaslangicSaat;
                    kModel.TeslimBitisSaat = belgeSaat.TeslimBitisSaat;
                    var guid = Guid.NewGuid().ToString().Substring(0, 6).ToLower();
                    kModel.ErisimKodu = guid;
                    var btip = Management.getBtipDetay(kModel.BelgeTipID, kModel.OgrenimDurumID, _EnstituKod);
                    kModel.UcretAlimiVar = btip.UcretAlimiVar;
                    kModel.UcretsizMiktar = btip.UcretsizMiktar;
                    kModel.DonemlikKota = btip.DonemlikKota;
                    kModel.BelgeFiyati = btip.BelgeFiyati;
                    msg = "Talep Yapıldı";
                    var bt = db.BelgeTalepleris.Add(kModel);
                    if (UserIdentity.Current.Informations.Any(a => a.Key == "BTAnket"))
                    {
                        var anketCevaplari = UserIdentity.Current.Informations.Where(p => p.Key == "BTAnket").FirstOrDefault();
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
                    var belge = db.BelgeTalepleris.Where(p => p.BelgeTalepID == kModel.BelgeTalepID).First();

                    var belgeSaat = Management.getSelectedSaat(belge.TalepTarihi, kModel.BelgeTipID, kModel.OgrenimDurumID, _EnstituKod);
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
                    if (kModel.BelgeTipID == BelgeTalepTip.İlgiliMakama) belge.BelgeAciklamasi = kModel.BelgeAciklamasi;
                    else if (kModel.BelgeTipID == BelgeTalepTip.Diğer)
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
                db.SaveChanges();
                #region mail
                if (YeniKayit)
                {
                    ID = kModel.BelgeTalepID;
                    kModel.BelgeTalepID = ID;
                    var mGonder = BelgeTalepAyar.getAyarBT(BelgeTalepAyar.YeniBelgeTalebindeMailGonder, _EnstituKod).ToBoolean().Value;
                    if (mGonder)
                        bilgiMaili(kModel, donem.DonemAdi, _EnstituKod);

                    MmMessage.IsSuccess = true;
                    MmMessage.Messages.Add("Detaylı bilgiler mail adresinize iletilmiştir.<");

                }
                else
                {
                    if (kModel.BelgeDurumID == BelgeTalepDurum.IptalEdildi)
                    {
                        MmMessage.IsSuccess = true;
                        MmMessage.Messages.Add("Belge talep işleminiz iptal edildi!");
                    }
                }
                #endregion
                MessageBox.Show(msg, "Uyarı", MessageBox.MessageType.Information);
                return RedirectToAction("Index");
            }
            else
            {
                MmMessage.IsSuccess = false;
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.BelgeDurumID = new SelectList(Management.cmbBelgeTalepDurum(true, belgeDuzenleYetki, kModel.BelgeTalepID <= 0), "Value", "Caption", kModel.BelgeDurumID);
            ViewBag.BelgeTipID = new SelectList(Management.cmbBelgeTipleri(true, kModel.OgrenimDurumID, _EnstituKod), "Value", "Caption", kModel.BelgeTipID);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(_EnstituKod, true), "Value", "Caption", kModel.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", kModel.OgrenimDurumID);
            ViewBag.BelgeDilKodu = new SelectList(Management.GetDiller(true), "Value", "Caption", kModel.BelgeDilKodu);
            ViewBag.MmMessage = MmMessage;
            int sayi = 10;
            if (kModel.OgrenimDurumID > 0 && kModel.BelgeTipID > 0)
            {
                var btip = Management.getBtipDetay(kModel.BelgeTipID, kModel.OgrenimDurumID, _EnstituKod);
                if (btip.DonemlikKota.HasValue)
                {
                    sayi = btip.DonemlikKota.Value;
                }

            }
            ViewBag.IstenenBelgeSayisi = new SelectList(getBelgeSayisi(sayi), "Value", "Caption", kModel.IstenenBelgeSayisi);

            return View(kModel);
        }
        public ActionResult TalepYapKontrol(BelgeTalepleri kModel, string EKD, bool? Iptal, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            string AnketGiris = "";
            var kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();

            if (kModel.BelgeTalepID <= 0 && kul.OgrenimDurumID != OgrenimDurum.OzelOgrenci)
            {
                string _EnstituKod = Management.getSelectedEnstitu(EKD);

                #region kontrol


                if (kModel.BelgeTipID <= 0)
                {
                    MmMessage.Messages.Add("Belge Tipi Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeTipID" });
                }
                else if (kModel.BelgeTipID == BelgeTalepTip.İlgiliMakama && kModel.BelgeAciklamasi.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("İlgili Makam Açıklaması Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeAciklamasi" });
                }
                else if (kModel.BelgeTipID == BelgeTalepTip.Diğer)
                {
                    if (kModel.BelgeAdi.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Belge Adı Giriniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeAdi" });
                    }
                    if (kModel.BelgeAciklamasi.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Belge Açıklaması Giriniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeAciklamasi" });
                    }
                }
                if (kModel.BelgeDilKodu.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Belgenin hazırlanacağı Dili Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeDilKodu" });
                }

                if (kModel.IstenenBelgeSayisi <= 0)
                {
                    MmMessage.Messages.Add("İstenilen Belge Sayısını Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IstenenBelgeSayisi" });
                }


                if (MmMessage.Messages.Count == 0)
                {
                    var IlkBTAnketAdi = BelgeTalepAyar.getAyarBT(BelgeTalepAyar.IlkBelgeTalebiAnketiAdi, _EnstituKod, "");
                    var IlkBTAnketID = db.Ankets.Where(p => p.AnketAdi == IlkBTAnketAdi).Select(s => s.AnketID).FirstOrDefault();
                    var IlkBelgeTalebiVar = db.BelgeTalepleris.Any(a => a.OgrenciNo == kul.OgrenciNo && kul.ProgramKod == a.ProgramKod) || IlkBTAnketAdi.IsNullOrWhiteSpace();
                    var KullaniciDonem4 = Convert.ToDouble((kul.KayitYilBaslangic.Value + 2) + "." + kul.KayitDonemID.Value);
                    var SuankiDonem = DateTime.Now.toEoYilBilgi();

                    var AktifDonem = Convert.ToDouble((SuankiDonem.BaslangicYili) + "." + SuankiDonem.Donem);
                    var AnketAdi = "";
                    if (KullaniciDonem4 <= AktifDonem)
                    {
                        if (!db.BelgeTalepleris.Where(a => a.OgrenciNo == kul.OgrenciNo && kul.ProgramKod == a.ProgramKod).ToList().Any(a => !a.AnketCevaplaris.Any(a2 => a2.AnketID == IlkBTAnketID)))
                            AnketAdi = BelgeTalepAyar.getAyarBT(BelgeTalepAyar.Donem4BelgeTalebiAnketiAdi, _EnstituKod, "");
                    }
                    else if (!IlkBelgeTalebiVar) AnketAdi = IlkBTAnketAdi;

                    if (AnketAdi != "")
                    {
                        var AnketID = db.Ankets.Where(p => p.AnketAdi == AnketAdi).Select(s => s.AnketID).FirstOrDefault();
                        var anketSorulari = (from bsa in db.Ankets.Where(p => p.AnketID == AnketID)
                                             join aso in db.AnketSorus on bsa.AnketID equals aso.AnketID
                                             join sb in db.AnketCevaplaris.Where(p => p.AnketID == AnketID && p.BelgeTalepID == kModel.BelgeTalepID) on aso.AnketSoruID equals sb.AnketSoruID into def1
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
                        var model = new kmAnketlerCevap();
                        model.AnketTipID = 2;
                        model.AnketID = AnketID;
                        model.JsonStringData = anketSorulari.toJsonText();
                        foreach (var item in anketSorulari)
                        {
                            model.AnketCevapModel.Add(new AnketCevapModel
                            {
                                SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                                SoruBilgi = new frAnketDetay { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama, IsTabloVeriGirisi = item.IsTabloVeriGirisi, IsTabloVeriMaxSatir = item.IsTabloVeriMaxSatir, },
                                SoruSecenek = item.Secenekler.Select(s => new frAnketSecenekDetay { AnketSoruSecenekID = s.Value, SiraNo = s.SiraNo, IsEkAciklamaGir = s.IsEkAciklamaGir, IsYaziOrSayi = s.IsYaziOrSayi, SecenekAdi = s.Caption }).ToList(),
                                SelectListSoruSecenek = new SelectList(item.Secenekler.ToList(), "Value", "Caption", item.AnketSoruSecenekID)
                            });
                        }

                        AnketGiris = Management.RenderPartialView("Ajax", "getAnket", model);
                    }
                }
                #endregion

            }
            return new { IsSubmitOrAnketShow = AnketGiris == "", AnketGiris = AnketGiris }.toJsonResult();
        }
        public ActionResult getBilgi(int? BelgeTalepID, int BelgeTipID, int miktar, string EKD)
        {
            BelgeTalepID = BelgeTalepID <= 0 ? null : BelgeTalepID;
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            int OgrenimDurumID;
            string OgrenciNo = "";


            if (BelgeTalepID.HasValue && BelgeTalepID.Value > 0)
            {
                var Talep = db.BelgeTalepleris.Where(p => p.BelgeTalepID == BelgeTalepID.Value).First();
                OgrenciNo = Talep.OgrenciNo;
                OgrenimDurumID = db.BelgeTalepleris.Where(p => p.BelgeTalepID == BelgeTalepID.Value).First().OgrenimDurumID;
            }
            else
            {
                var Kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
                OgrenimDurumID = Kul.OgrenimDurumID.Value;
                OgrenciNo = Kul.OgrenciNo;

            }
            var contentB = getContent(BelgeTalepID, BelgeTipID, _EnstituKod, OgrenciNo, miktar, OgrenimDurumID);
            var HCB = Management.RenderPartialView("Ajax", "getMailTableContent", contentB);
            return new { Deger = HCB }.toJsonResult();

        }
        void bilgiMaili(BelgeTalepleri kModel, string DonemAdi, string _EnstituKodu)
        {
            var htmlBigliRow = new List<mailTableRow>();
            var contentBilgi = new mailTableContent();
            var tutar = tutarHesapla(kModel);
            var btip = (from s in db.BelgeTipleris.Where(p => p.BelgeTipID == kModel.BelgeTipID)
                        join bt in db.BelgeTipDetays.Where(p => p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == kModel.BelgeTipID)) on new { kModel.OgrenimDurumID, kModel.EnstituKod } equals new { bt.OgrenimDurumID, bt.EnstituKod }
                        select new { s.BelgeTipID, s.BelgeTipAdi, bt.UcretAlimiVar, bt.BelgeFiyati, bt.UcretsizMiktar, bt.DonemlikKota, bt.UcretAciklamasiLink }).First();
            var OD = db.OgrenimDurumlaris.Where(p => p.OgrenimDurumID == kModel.OgrenimDurumID).First();


            htmlBigliRow.Add(new mailTableRow { Baslik = "Ad Soyad", Aciklama = kModel.AdiSoyadi });
            htmlBigliRow.Add(new mailTableRow { Baslik = "Öğrenci No", Aciklama = kModel.OgrenciNo });
            htmlBigliRow.Add(new mailTableRow { Baslik = "Eğitim Öğretim Yılı", Aciklama = kModel.OgretimYiliBaslangic + "/" + kModel.OgretimYiliBitis + " " + DonemAdi });
            htmlBigliRow.Add(new mailTableRow { Baslik = "Belge Tipi", Aciklama = btip.BelgeTipAdi });
            if (kModel.BelgeAdi.IsNullOrWhiteSpace() == false) htmlBigliRow.Add(new mailTableRow { Baslik = "Belge Adı", Aciklama = kModel.BelgeAdi });
            if (kModel.BelgeAciklamasi.IsNullOrWhiteSpace() == false) htmlBigliRow.Add(new mailTableRow { Baslik = "Belge Açıklaması", Aciklama = kModel.BelgeAciklamasi });

            htmlBigliRow.Add(new mailTableRow { Baslik = "İstenen Belge Sayısı", Aciklama = kModel.IstenenBelgeSayisi + " Adet" });
            //if (kModel.OgrenimDurumID == OgrenimDurum.Mezun) htmlBigliRow.Add(new HtmlContentBilgiRow { Baslik = "NOT", Aciklama = "Mezun olmamış öğrencilerin aynı dönem içinde alabileceği maksimum belge sayısı 3 adettir, eğer 3 adetten fazla belgeye ihtiyaç duyuyorsa daha önceden almış olduğu belgenin fotokopisini çektirip kurum tarafından 'Aslı Gibidir' kaşesi vurdurabilir." });
            if (kModel.UcretsizMiktar.HasValue) htmlBigliRow.Add(new mailTableRow { Baslik = "Dönemlik ücretsiz alınabilecek belge sayısı", Aciklama = kModel.UcretsizMiktar.Value + " Adet" });
            if (kModel.UcretAlimiVar) htmlBigliRow.Add(new mailTableRow { Baslik = "Adet Fiyatı", Aciklama = kModel.BelgeFiyati.Value + " TL" });
            if (tutar.VerilenBelgeTutar > 0) htmlBigliRow.Add(new mailTableRow { Baslik = "Toplam Fiyat", Aciklama = tutar.VerilenBelgeTutar + " TL" });
            if (kModel.UcretAlimiVar) htmlBigliRow.Add(new mailTableRow { Baslik = "Ödeme Bilgisi", Aciklama = "<a href=" + btip.UcretAciklamasiLink + "> Ödeme bilgisi için tıklayınız.</a>" });
            else htmlBigliRow.Add(new mailTableRow { Baslik = "Ücret Bilgisi", Aciklama = "Belge Ücretsizdir lütfen verilen randevu zamanında belgenizi alınız." });

            if (kModel.DonemlikKota.HasValue) htmlBigliRow.Add(new mailTableRow { Baslik = "Not", Aciklama = "Bu belge tipi için dönemlik alınabilecek maksimum belge sayısı " + kModel.DonemlikKota.Value + " adettir, eğer " + kModel.DonemlikKota.Value + " adetten fazla belgeye ihtiyaç duyuluyorsa daha önceden alınmış olan belgenin fotokopisini çektirilip kurum tarafından 'Aslı Gibidir' kaşesi vurdurulabilir." });
            var konu = "";
            if (kModel.BelgeDurumID == BelgeTalepDurum.TalepEdildi)
            {
                var BelgeAlimAdresi = BelgeTalepAyar.BelgeAlımAdresi.getAyarBT(_EnstituKodu, "");
                contentBilgi.AciklamaDetayi = GunHesap(kModel.TalepTarihi, kModel.EklenecekGun, kModel.TeslimBaslangicSaat.Value, kModel.TeslimBitisSaat.Value, kModel.UcretAlimiVar, BelgeAlimAdresi);
                contentBilgi.AciklamaBasligi = "Belge Talebi İşlemi Yapıldı";
                konu = "Belge Talebi İşlemi Yapıldı";
            }
            else if (kModel.BelgeDurumID == BelgeTalepDurum.Kapatildi)
            {
                contentBilgi.AciklamaDetayi = "Reddedilme Nedeni: " + kModel.BelgeDurumAciklamasi;
                contentBilgi.AciklamaBasligi = "Belge talep işleminiz reddedildi!";
                konu = "Belge talep işleminiz reddedildi!";

            }

            contentBilgi.Detaylar = htmlBigliRow;

            var mmmC = new mdlMailMainContent();
            var enstituAdi = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKodu).First().EnstituAd;
            mmmC.EnstituAdi = enstituAdi;
            mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
            var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(_EnstituKodu);
            var _ea = mailBilgi.SistemErisimAdresi;
            var WurlAddr = _ea.Split('/').ToList();
            if (_ea.Contains("//"))
                _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
            else
                _ea = "http://" + WurlAddr.First();
            mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
            var HCB = Management.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
            mmmC.Content = HCB;
            string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
            var emailSend = MailManager.sendMail(mailBilgi.EnstituKod, konu, htmlMail, kModel.Email, null);
        }

        mailTableContent getContent(int? BelgeTalepID, int BelgeTipID, string _EnstituKod, string Numara, int miktar, int OgrenimDurumID)
        {
            var kModel = new BelgeTalepleri();
            kModel.EnstituKod = _EnstituKod;

            if (BelgeTalepID.HasValue)
            {
                kModel = db.BelgeTalepleris.Where(p => p.BelgeTalepID == BelgeTalepID.Value).First();
            }
            else
            {
                var eoYil = DateTime.Now.toEoYilBilgi();
                kModel.OgretimYiliBaslangic = eoYil.BaslangicYili;
                kModel.OgretimYiliBitis = eoYil.BitisYili;
                kModel.DonemID = eoYil.Donem;
                kModel.OgrenimDurumID = OgrenimDurumID;
                kModel.BelgeTipID = BelgeTipID;
                kModel.OgrenciNo = Numara;
                kModel.IstenenBelgeSayisi = miktar;
                kModel.TalepTarihi = DateTime.Now;


            }
            var btiap = Management.getBtipDetay(BelgeTipID, OgrenimDurumID, _EnstituKod);
            var saatB = Management.getSelectedSaat(kModel.TalepTarihi, kModel.BelgeTipID, kModel.OgrenimDurumID, _EnstituKod);
            kModel.EklenecekGun = saatB.EklenecekGun;
            kModel.TeslimBaslangicSaat = saatB.TeslimBaslangicSaat;
            kModel.TeslimBitisSaat = saatB.TeslimBitisSaat;
            kModel.UcretAciklamasiLink = btiap.UcretAciklamasiLink;
            kModel.UcretAlimiVar = btiap.UcretAlimiVar;
            kModel.UcretsizMiktar = btiap.UcretsizMiktar;
            kModel.DonemlikKota = btiap.DonemlikKota;
            var donem = db.Donemlers.Where(p => p.DonemID == kModel.DonemID).First();
            var AyniDonemAlinanBelge = AyniDonemAlinenBelgeSayisi(kModel);
            var AyniDonemTalepEdilenBelge = AyniDonemTalepEdilenBelgeSayisi(kModel);
            var htmlBigliRow = new List<mailTableRow>();
            var contentBilgi = new mailTableContent();
            contentBilgi.CaptTdWidth = 200;
            var tutar = tutarHesapla(kModel);

            var btip = (from s in db.BelgeTipleris
                        where s.BelgeTipID == kModel.BelgeTipID
                        select new { s.BelgeTipID, s.BelgeTipAdi }).First();

            string AciklamaDetayi = "";
            bool kotaVar = true;

            if (kModel.DonemlikKota.HasValue)
            {
                if (AyniDonemAlinanBelge + AyniDonemTalepEdilenBelge + miktar > kModel.DonemlikKota.Value)
                {
                    int talepEdilebilecekMiktar = kModel.DonemlikKota.Value - (AyniDonemAlinanBelge + AyniDonemTalepEdilenBelge);
                    AciklamaDetayi = "İstediğiniz belge tipi için bu dönem alabileceğiniz toplam miktar " + kModel.DonemlikKota.Value + " adet dir. Daha fazla miktar için başvuru yapamazsınız!";
                    kotaVar = false;
                }
                else
                {
                    var BelgeAlimAdresi = BelgeTalepAyar.BelgeAlımAdresi.getAyarBT(_EnstituKod, "");
                    AciklamaDetayi = GunHesap(kModel.TalepTarihi, kModel.EklenecekGun, kModel.TeslimBaslangicSaat.Value, kModel.TeslimBitisSaat.Value, kModel.UcretAlimiVar, BelgeAlimAdresi);
                }
            }
            else
            {
                var BelgeAlimAdresi = BelgeTalepAyar.BelgeAlımAdresi.getAyarBT(_EnstituKod, "");
                AciklamaDetayi = GunHesap(kModel.TalepTarihi, kModel.EklenecekGun, kModel.TeslimBaslangicSaat.Value, kModel.TeslimBitisSaat.Value, kModel.UcretAlimiVar, BelgeAlimAdresi);
            }

            htmlBigliRow.Add(new mailTableRow { Baslik = "Eğitim Öğretim Yılı ", Aciklama = kModel.OgretimYiliBaslangic + "/" + kModel.OgretimYiliBitis + " " + donem.DonemAdi });
            htmlBigliRow.Add(new mailTableRow { Baslik = "Belge Tip Adı", Aciklama = btip.BelgeTipAdi });
            if (kModel.DonemlikKota.HasValue) htmlBigliRow.Add(new mailTableRow { Baslik = "Dönemde Alabileceğiniz Toplam Belge", Aciklama = kModel.DonemlikKota.Value + " Adet" });
            if (kotaVar == false) htmlBigliRow.Add(new mailTableRow { Baslik = "Not", Aciklama = "Bu belge tipi için dönemlik alınabilecek maksimum belge sayısı " + kModel.DonemlikKota.Value + " adettir, eğer " + kModel.DonemlikKota.Value + " adetten fazla belgeye ihtiyaç duyuluyorsa daha önceden alınmış olan belgenin fotokopisini çektirilip kurum tarafından 'Aslı Gibidir' kaşesi vurdurulabilir." });
            htmlBigliRow.Add(new mailTableRow { Baslik = "Dönemde Daha Önceden Alınan Toplam Belge Sayısı", Aciklama = AyniDonemAlinenBelgeSayisi(kModel) + " Adet" });
            htmlBigliRow.Add(new mailTableRow { Baslik = "Dönemde Daha Önceden Talep Edilip Henüz Alınmayan Toplam Belge", Aciklama = AyniDonemTalepEdilenBelge + " Adet" });
            htmlBigliRow.Add(new mailTableRow { Baslik = "İstenilen Belge Sayısı", Aciklama = kModel.IstenenBelgeSayisi + " Adet" });
            if (kModel.UcretsizMiktar.HasValue) htmlBigliRow.Add(new mailTableRow { Baslik = "Dönemlik ücretsiz alınabilecek belge sayısı", Aciklama = kModel.UcretsizMiktar.Value + " Adet" });
            if (kModel.UcretAlimiVar) htmlBigliRow.Add(new mailTableRow { Baslik = "Adet Fiyatı", Aciklama = kModel.BelgeFiyati.Value + " TL" });
            if (kModel.UcretAlimiVar) htmlBigliRow.Add(new mailTableRow { Baslik = "Toplam Fiyat", Aciklama = tutar.VerilenBelgeTutar + " TL" });
            else htmlBigliRow.Add(new mailTableRow { Baslik = "Ücret Bilgisi", Aciklama = "Belge Ücretsizdir lütfen verilen randevu zamanında belgenizi alınız." });

            if (kModel.DonemlikKota.HasValue) htmlBigliRow.Add(new mailTableRow { Baslik = "Not", Aciklama = "İstediğiniz belge tipi için bu dönem alabileceğiniz toplam miktar " + kModel.DonemlikKota.Value + " adet dir. Daha fazla miktar için başvuru yapamazsınız!" });

            if (kModel.UcretAlimiVar && kotaVar && kModel.VerilenBelgeTutar > 0 && kModel.UcretAciklamasiLink.IsNullOrWhiteSpace() == false)
                htmlBigliRow.Add(new mailTableRow { Baslik = "Ödeme Bilgisi", Aciklama = "<a href=" + kModel.UcretAciklamasiLink + " target=_blank>Ödeme bilgisi için tıklayınız</a>" });
            contentBilgi.AciklamaDetayi = AciklamaDetayi;
            contentBilgi.Success = kotaVar;
            contentBilgi.AciklamaBasligi = "Belge talep açıklaması";
            contentBilgi.Detaylar = htmlBigliRow;

            return contentBilgi;

        }

        [Authorize(Roles = RoleNames.BelgeTalebiDuzelt)]
        public ActionResult Istenenkaydet(int id, int IslemTipID, string IslemTipAciklamasi, int miktar)
        {


            var belge = db.BelgeTalepleris.Where(p => p.BelgeTalepID == id).First();
            var oldTDID = belge.BelgeDurumID;
            if (IslemTipID == BelgeTalepDurum.Verildi)
            {
                var bel2 = tutarHesapla(belge);
                belge.VerilenBelgeSayisi = miktar;
                belge.BelgeFiyati = bel2.BelgeFiyati;
                belge.VerilenBelgeTutar = bel2.VerilenBelgeTutar;
            }
            else if (IslemTipID == BelgeTalepDurum.Kapatildi && IslemTipID != oldTDID)
            {
                belge.BelgeDurumAciklamasi = IslemTipAciklamasi;
                belge.VerilenBelgeSayisi = null;
                belge.VerilenBelgeTutar = null;
            }
            belge.BelgeDurumID = IslemTipID;
            belge.IslemTarihi = DateTime.Now;
            belge.IslemYapanID = UserIdentity.Current.Id;
            belge.IslemYapanIp = UserIdentity.Ip;
            db.SaveChanges();
            if (IslemTipID == BelgeTalepDurum.Kapatildi && IslemTipID != oldTDID)
            {
                var donem = belge.Donemler;
                bilgiMaili(belge, donem.DonemAdi, belge.EnstituKod);
            }

            return new
            {
                IslemTipListeAdi = belge.BelgeDurumlari.DurumAdi,
                belge.BelgeDurumlari.ClassName,
                belge.BelgeDurumlari.Color,
                verilmeBilgi = belge.BelgeTalepID.toVerilmeBilgisi(belge.BelgeDurumlari.DurumAdi)
            }.toJsonResult();
        }


        public BelgeTalepleri tutarHesapla(BelgeTalepleri mdl)
        {
            var AyniDonemAlinan = AyniDonemAlinenBelgeSayisi(mdl);
            int UcretsizMiktar = 0;
            double belgeFiyati = 0;

            if (mdl.BelgeTalepID <= 0)
            {
                var btip = db.BelgeTipDetays.Where(bt => bt.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == mdl.BelgeTipID) && bt.OgrenimDurumID == mdl.OgrenimDurumID && bt.EnstituKod == mdl.EnstituKod).First();
                mdl.UcretAlimiVar = btip.UcretAlimiVar;
                mdl.BelgeFiyati = btip.BelgeFiyati;
                mdl.UcretsizMiktar = btip.UcretsizMiktar;
            }
            if (!mdl.UcretAlimiVar)
            {
                mdl.BelgeFiyati = belgeFiyati = 0;
                UcretsizMiktar = 0;
            }
            else
            {

                mdl.BelgeFiyati = belgeFiyati = mdl.BelgeFiyati.Value;
                UcretsizMiktar = mdl.UcretsizMiktar.HasValue ? mdl.UcretsizMiktar.Value : 0;
            }
            int belgeSayisi = 0;
            if (mdl.BelgeDurumID == BelgeTalepDurum.Verildi) belgeSayisi = mdl.VerilenBelgeSayisi ?? 1;
            else belgeSayisi = mdl.IstenenBelgeSayisi;

            mdl.VerilenBelgeTutar = AyniDonemAlinan >= UcretsizMiktar ? (belgeSayisi * belgeFiyati) : (((belgeSayisi + AyniDonemAlinan) - UcretsizMiktar) * belgeFiyati);
            if (mdl.VerilenBelgeTutar < 0) mdl.VerilenBelgeTutar = 0;
            return mdl;
        }

        public ActionResult miktarHesapla(int id, int miktar)
        {

            var belge = db.BelgeTalepleris.Where(p => p.BelgeTalepID == id).First();
            if (belge.BelgeDurumID == BelgeTalepDurum.Verildi) belge.VerilenBelgeSayisi = miktar;
            else belge.IstenenBelgeSayisi = miktar;
            var bel2 = tutarHesapla(belge);
            return bel2.VerilenBelgeTutar.toJsonResult();
        }
        string GunHesap(DateTime IslemTarihi, int EklenecekGun, TimeSpan TeslimBaslangicSaat, TimeSpan TeslimBitisSaat, bool Ucretli, string Adres)
        {
            Adres = "Talep ettiğiniz belgeyi " + IslemTarihi.AddDays(EklenecekGun).ToString("dd.MM.yyyy") + " Tarihi Saat " + TeslimBaslangicSaat.ToString(@"hh\:mm") + " ile " + TeslimBitisSaat.ToString(@"hh\:mm") + " arası Banka Dekontu ile birlikte " + Adres + " adresine gelip alabilirsiniz";
            return Adres;
        }
        public int AyniDonemAlinenBelgeSayisi(BelgeTalepleri mdl)
        {
            var data = db.BelgeTalepleris.Where(p => p.BelgeDurumID == BelgeTalepDurum.Verildi &&
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
            var data = db.BelgeTalepleris.Where(p => (p.BelgeDurumID == BelgeTalepDurum.TalepEdildi || p.BelgeDurumID == BelgeTalepDurum.Hazirlandi || p.BelgeDurumID == BelgeTalepDurum.Hazirlaniyor) &&
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
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = true;
            var kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
            var belge = db.BelgeTalepleris.Where(p => p.BelgeTalepID == id && p.OgrenciNo == kul.OgrenciNo).FirstOrDefault();
            if (mmMessage.IsSuccess)
            {
                try
                {
                    db.BelgeTalepleris.Remove(belge);
                    db.SaveChanges();
                    mmMessage.Messages.Add("Belge Talebi Silindi.");
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Belge Talebi Silinemedi.");
                    Management.SistemBilgisiKaydet(ex, BilgiTipi.OnemsizHata);
                }
            }
            return mmMessage.toJsonResult();
        }
    }
}