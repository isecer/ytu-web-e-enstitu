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
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class MezuniyetController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(Guid? rowId, int? kullaniciId, string ekd)
        {
            return Index(new FmMezuniyetBasvurulari() { RowID = rowId, KullaniciID = kullaniciId, PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmMezuniyetBasvurulari model, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            if (model.RowID.HasValue)
            {
                var basvuru = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.RowID == model.RowID);
                if (basvuru != null) model.KullaniciID = basvuru.KullaniciID;
            }
            else
            {
                if (!model.KullaniciID.HasValue) model.KullaniciID = UserIdentity.Current.Id;
            }

            #region bilgiModel
            var bbModel = new IndexPageInfoDto();
            var mezuniyetSurecId = MezuniyetBus.GetMezuniyetAktifSurecId(enstituKod);
            bbModel.AktifSurecID = mezuniyetSurecId ?? 0;
            bbModel.SistemBasvuruyaAcik = MezuniyetAyar.MezuniyetBasvurusuAcikmi.GetAyarMz(enstituKod).ToBoolean(false) && mezuniyetSurecId.HasValue;
            bbModel.MezuniyetSurec = _entities.MezuniyetSurecis.FirstOrDefault(p => p.MezuniyetSurecID == mezuniyetSurecId.Value);
            if (bbModel.MezuniyetSurec != null)
            {
                bbModel.DonemAdi = bbModel.MezuniyetSurec.BaslangicYil + "/" + bbModel.MezuniyetSurec.BitisYil + " " + _entities.Donemlers.First(p => p.DonemID == bbModel.MezuniyetSurec.DonemID).DonemAdi + " " + bbModel.MezuniyetSurec.SiraNo;
            }
            var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == model.KullaniciID);
            bbModel.Kullanici = kullanici;
            if (kullanici.YtuOgrencisi)
            {
                var otb = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);

                bbModel.OgrenimDurumAdi = kullanici.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                bbModel.AnabilimdaliAdi = kullanici.Programlar.AnabilimDallari.AnabilimDaliAdi;
                bbModel.ProgramAdi = kullanici.Programlar.ProgramAdi;
                bbModel.OgrenciNo = kullanici.OgrenciNo;
                bbModel.KullaniciTipYetki = kullanici.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci;
                bbModel.EnstituYetki = kullanici.Programlar.AnabilimDallari.EnstituKod == enstituKod;
                if (kullanici.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci)
                {
                    var kullKayitB = KullanicilarBus.OgrenciBilgisiGuncelleObs(kullanici.KullaniciID);
                    if (kullanici.KayitTarihi != kullKayitB.KayitTarihi)
                    {
                        kullanici.KayitYilBaslangic = kullKayitB.BaslangicYil;
                        kullanici.KayitDonemID = kullKayitB.DonemID;
                        kullanici.KayitTarihi = kullKayitB.KayitTarihi;
                        _entities.SaveChanges();
                    }
                    if (kullKayitB.KayitVar == false)
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "Öğrenim Bilginiz Doğrulanamdı. Hesap bilgilerinizde bulunan YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen";
                    }
                    else bbModel.KayitDonemi = kullanici.KayitYilBaslangic + "/" + (kullanici.KayitYilBaslangic + 1) + " " + _entities.Donemlers.First(p => p.DonemID == kullanici.KayitDonemID.Value).DonemAdi + " , " + kullanici.KayitTarihi.ToFormatDate();

                }

            }
            else
            {
                bbModel.KullaniciTipYetki = false;
                bbModel.KullaniciTipYetkiYokMsj = "Hesap bilgilerinizde YTÜ Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için mezuniyet başvurusu yapamazsınız. Sağ üst köşeden profil bilgilerini düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
            }
            bbModel.Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
            bbModel.Kullanici = kullanici;
            #endregion 
            var nowDate = DateTime.Now;
            var q = from s in _entities.MezuniyetBasvurularis
                    join ms in _entities.MezuniyetSurecis on s.MezuniyetSurecID equals ms.MezuniyetSurecID
                    join kul in _entities.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                    join mOt in _entities.MezuniyetSureciOgrenimTipKriterleris on new { s.MezuniyetSurecID, s.OgrenimTipKod } equals new { mOt.MezuniyetSurecID, mOt.OgrenimTipKod }
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, ms.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join ot in _entities.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join prl in _entities.Programlars on s.ProgramKod equals prl.ProgramKod
                    join abl in _entities.AnabilimDallaris on pr.AnabilimDaliID equals abl.AnabilimDaliID
                    join en in _entities.Enstitulers on s.MezuniyetSureci.EnstituKod equals en.EnstituKod
                    join bs in _entities.MezuniyetSurecis on s.MezuniyetSurecID equals bs.MezuniyetSurecID
                    join d in _entities.Donemlers on bs.DonemID equals d.DonemID
                    join ktip in _entities.KullaniciTipleris on s.Kullanicilar.KullaniciTipID equals ktip.KullaniciTipID
                    join dr in _entities.MezuniyetYayinKontrolDurumlaris on s.MezuniyetYayinKontrolDurumID equals dr.MezuniyetYayinKontrolDurumID
                    join qmsd in _entities.MezuniyetSinavDurumlaris on s.MezuniyetSinavDurumID equals qmsd.MezuniyetSinavDurumID into defMsd
                    from msd in defMsd.DefaultIfEmpty()
                    join qjOf in _entities.MezuniyetJuriOneriFormlaris on s.MezuniyetBasvurulariID equals qjOf.MezuniyetBasvurulariID into defJof
                    from jOf in defJof.DefaultIfEmpty()
                    let srT = s.SRTalepleris.OrderByDescending(ods => ods.SRTalepID).FirstOrDefault()
                    let td = s.MezuniyetBasvurulariTezDosyalaris.OrderByDescending(ods => ods.MezuniyetBasvurulariTezDosyaID).FirstOrDefault()
                    where bs.Enstituler.EnstituKisaAd.Contains(ekd) && s.KullaniciID == model.KullaniciID
                    select new FrMezuniyetBasvurulari
                    {

                        MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                        TezDanismanID = s.TezDanismanID,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = ot.OgrenimTipAdi,
                        AnabilimdaliAdi = abl.AnabilimDaliAdi,
                        ProgramAdi = prl.ProgramAdi,
                        MezuniyetSurecID = s.MezuniyetSurecID,
                        SurecBaslangicYil = bs.BaslangicYil,
                        DonemID = bs.DonemID,
                        MezuniyetSurecAdi = bs.BaslangicYil + "/" + bs.BitisYil + " " + d.DonemAdi + " " + bs.SiraNo,
                        BasTar = bs.BaslangicTarihi,
                        BitTar = bs.BitisTarihi,
                        KullaniciID = s.KullaniciID,
                        UserKey = kul.UserKey,
                        TezBaslikTr = s.TezBaslikTr,
                        TezDanismanAdi = s.TezDanismanAdi,
                        TezDanismanUnvani = s.TezDanismanUnvani,
                        EMail = kul.EMail,
                        CepTel = kul.CepTel,
                        KayitTarihi = kul.KayitTarihi,
                        AdSoyad = kul.Ad + " " + kul.Soyad,
                        TcKimlikNo = kul.TcKimlikNo,
                        OgrenciNo = s.OgrenciNo,
                        ResimAdi = kul.ResimAdi,
                        KullaniciTipID = kul.KullaniciTipID,
                        KullaniciTipAdi = s.KullaniciTipID == KullaniciTipiEnum.YerliOgrenci ? "" : ktip.KullaniciTipAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        IsMezunOldu = s.IsMezunOldu,
                        MezuniyetTarihi = s.MezuniyetTarihi,
                        SrTalebi = srT,
                        SRDurumID = srT.SRDurumID,
                        TeslimFormDurumu = srT != null && s.MezuniyetBasvurulariTezTeslimFormlaris.Any(),
                        IsOnaylandiOrDuzeltme = td != null ? td.IsOnaylandiOrDuzeltme : null,
                        MezuniyetBasvurulariTezDosyasi = td,
                        UzatmaSuresiGun = mOt.SinavUzatmaSinavAlmaSuresiMaxGun,
                        MezuniyetSuresiGun = mOt.SinavUzatmaSinavAlmaSuresiMaxGun,
                        EYKTarihi = s.EYKTarihi,
                        MBYayinTurIDs = s.MezuniyetBasvurulariYayins.Select(s2 => s2.MezuniyetYayinTurID).ToList(),
                        FormNo = jOf != null ? jOf.UniqueID : "",
                        MezuniyetJuriOneriFormu = jOf,
                        TezTeslimSonTarih = s.TezTeslimSonTarih,
                        IsDanismanOnay = s.IsDanismanOnay,
                        DanismanOnayTarihi = s.DanismanOnayTarihi,
                        DanismanOnayAciklama = s.DanismanOnayAciklama,
                        MezuniyetYayinKontrolDurumID = s.MezuniyetYayinKontrolDurumID,
                        MezuniyetYayinKontrolDurumAdi = dr.MezuniyetYayinKontrolDurumAdi,
                        DurumClassName = dr.ClassName,
                        DurumColor = dr.Color,
                        MezuniyetSinavDurumID = msd.MezuniyetSinavDurumID,
                        MezuniyetSinavDurumAdi = msd != null ? msd.MezuniyetSinavDurumAdi : "",
                        SDurumClassName = msd != null ? msd.ClassName : "",
                        SDurumColor = msd != null ? msd.Color : "",
                        MezuniyetYayinKontrolDurumAciklamasi = s.MezuniyetYayinKontrolDurumAciklamasi,


                    };
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.MezuniyetSurecID.HasValue) q = q.Where(p => p.MezuniyetSurecID == model.MezuniyetSurecID.Value);
            //if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID.Value);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcKimlikNo == model.AdSoyad || p.KullaniciTipAdi.Contains(model.AdSoyad));
            if (model.MezuniyetYayinKontrolDurumID.HasValue) q = q.Where(p => p.MezuniyetYayinKontrolDurumID == model.MezuniyetYayinKontrolDurumID.Value);
            model.RowCount = q.Count();
            //IndexModel.Toplam = model.RowCount;
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList(); ;
            ViewBag.MezuniyetSurecID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecleri(enstituKod, true), "Value", "Caption", model.MezuniyetSurecID);
            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurumListe(true, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);

            ViewBag.bModel = bbModel;
            return View(model);
        }



        public ActionResult BasvuruYap(int? mezuniyetBasvurulariId, string enstituKod = "", string ekd = "")
        {
            var model = new KmMezuniyetBasvuru
            {
                EnstituKod = enstituKod.IsNullOrWhiteSpace() ? EnstituBus.GetSelectedEnstitu(ekd) : enstituKod
            };
            if (mezuniyetBasvurulariId > 0)
            {
                var basvuru =
                    _entities.MezuniyetBasvurularis.First(p =>
                        p.MezuniyetBasvurulariID == mezuniyetBasvurulariId.Value);
                model.EnstituKod = enstituKod = basvuru.MezuniyetSureci.EnstituKod;
                model.KullaniciID = basvuru.KullaniciID;
            }
            else model.KullaniciID = UserIdentity.Current.Id;
            var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(model.KullaniciID);
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == model.KullaniciID);

            var mmMessage = MezuniyetBus.MezuniyetBasvuruKriterKontrol(model.EnstituKod, mezuniyetBasvurulariId);

            if (mmMessage.IsSuccess && !mezuniyetBasvurulariId.HasValue)
            {
                if (studentInfo.OgrenciTez.TEZ_DILI.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("OBS sisteminde tez bilgilerinize ait tez dili bilginiz boş gelmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    mmMessage.IsSuccess = false;
                }
                else
                {
                    if (studentInfo.OgrenciTez.TEZ_BASLIK.IsNullOrWhiteSpace() &&
                        studentInfo.OgrenciTez.TEZ_BASLIK_ENG.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add(
                            "Tezinizin türkçe ve ingilizce başlığı bilgisi OBS sisteminde tanımlı değildir. Başvuru yapabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                        mmMessage.IsSuccess = false;
                    }
                    else if (studentInfo.IsTezDiliTr && studentInfo.OgrenciTez.TEZ_BASLIK.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add(
                            "Tezinizin türkçe başlığı bilgisi OBS sisteminde tanımlı değildir. Başvuru yapabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                        mmMessage.IsSuccess = false;
                    }
                    else if (!studentInfo.IsTezDiliTr && studentInfo.OgrenciTez.TEZ_BASLIK_ENG.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add(
                            "Tezinizin ingilizce başlığı bilgisi OBS sisteminde tanımlı değildir. Başvuru yapabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                        mmMessage.IsSuccess = false;
                    }
                }
            }

            var danismanBilgi = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kul.DanismanID);
            if (!mezuniyetBasvurulariId.HasValue && mmMessage.IsSuccess)
            {
                var danismanTc = studentInfo.OgrenciInfo.DANISMAN_TC1;
                if (!kul.DanismanID.HasValue && (danismanTc.IsNullOrWhiteSpace() || danismanTc.Length != 11))
                {
                    mmMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir. Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    mmMessage.IsSuccess = false;
                }
                else if (!kul.DanismanID.HasValue)
                {
                    mmMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
                    mmMessage.IsSuccess = false;

                    //Tez bilgisi gelmiyor ise Tez durumu ile alakalı olabilir. Tez durumu devam ediyor olmalı. Eğer değilse Ya yeni tez eklenecek yada gsis te tez guncellemeden tez durumunu devam ediyor yapılmalı.
                }
            }
            if (mmMessage.IsSuccess)
            {
                var donemAdi = "";
                if (kul.KayitDonemID.HasValue)
                {
                    donemAdi = _entities.Donemlers.First(p => p.DonemID == kul.KayitDonemID.Value).DonemAdi;
                }
                model.KayitDonemi = kul.KayitYilBaslangic + "/" + (kul.KayitYilBaslangic + 1) + " " + donemAdi;
                model.KayitTarihi = kul.KayitTarihi;
                if (mezuniyetBasvurulariId.HasValue)
                {
                    model = MezuniyetBus.GetMezuniyetBasvuruBilgi(mezuniyetBasvurulariId.Value);
                    model.EnstituKod = enstituKod.IsNullOrWhiteSpace() ? EnstituBus.GetSelectedEnstitu(ekd) : enstituKod;
                    model.ResimAdi = kul.ResimAdi;
                    if (model.IsTezDiliTr != studentInfo.IsTezDiliTr)
                    {
                        model.IsTezDiliTr = studentInfo.IsTezDiliTr;
                        mmMessage.Messages.Add("Tez dili bilgisi değiştiği gözükmektedir. Bu değişikliğin yansıması için başvurunuzu tekrar kaydedin.");
                    }
                }
                else
                {
                    model.MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(model.EnstituKod).Value;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciTipID = kul.KullaniciTipID;
                    model.ResimAdi = kul.ResimAdi;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.TcKimlikNo = kul.TcKimlikNo;
                    model.OgrenimTipKod = kul.OgrenimTipKod.Value;
                    model.OgrenimTipAdi = _entities.OgrenimTipleris.First(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;
                    var progLng = kul.Programlar;
                    model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = progLng.ProgramAdi;
                    model.TezDanismanAdi = (danismanBilgi.Ad + " " + danismanBilgi.Soyad).ToUpper();
                    model.TezDanismanUnvani = danismanBilgi.Unvanlar.UnvanAdi.ToUpper();
                    model.IsTezDiliTr = studentInfo.IsTezDiliTr;
                    model.TezBaslikTr = studentInfo.OgrenciTez.TEZ_BASLIK;
                    model.TezBaslikEn = studentInfo.OgrenciTez.TEZ_BASLIK_ENG;
                }
                var surec = _entities.MezuniyetSurecis.First(p => p.MezuniyetSurecID == model.MezuniyetSurecID);
                model.DonemAdi = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi;
                model.SetSelectedStep = 1;
                model.IsYerli = kul.KullaniciTipleri.Yerli;
                model.KullaniciTipAdi = _entities.KullaniciTipleris.First(p => p.KullaniciTipID == kul.KullaniciTipID).KullaniciTipAdi;
                ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurum(true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
                ViewBag.TezEsDanismanUnvani = new SelectList(UnvanlarBus.GetCmbJuriUnvanlar(true), "Value", "Caption", model.TezEsDanismanUnvani);

                ViewBag.MezuniyetYayinTurID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecYayinTurleri(model.MezuniyetSurecID, model.KullaniciID, mezuniyetBasvurulariId ?? 0, true), "Value", "Caption");

                ViewBag._MmMessage = mmMessage;
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
                return RedirectToAction("Index", new { KullaniciID = model.KullaniciID });
            }

            if (mmMessage.Messages.Any())
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            if (model.MezuniyetBasvurulariID > 0)
            {
                ViewBag.MezuniyetYayinKontrolDurumu = _entities.MezuniyetYayinKontrolDurumlaris.Where(p => p.MezuniyetYayinKontrolDurumID == model.MezuniyetYayinKontrolDurumID).Select(s => new MezuniyetYayinKontrolDurumDto { MezuniyetYayinKontrolDurumID = s.MezuniyetYayinKontrolDurumID, ClassName = s.ClassName, Color = s.Color, DurumAdi = s.MezuniyetYayinKontrolDurumAdi }).First();
            }
            else
            {
                ViewBag.MezuniyetYayinKontrolDurumu = new MezuniyetYayinKontrolDurumDto
                {
                    DurumAdi = "Yeni Başvuru",
                    ClassName = "fa fa-plus",
                    Color = "color:black;"
                };
            }
            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BasvuruYap(KmMezuniyetBasvuru kModel)
        {
            var stps = new List<int>();

            if (kModel.MezuniyetBasvurulariID <= 0)
            {
                kModel.MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(kModel.EnstituKod) ?? 0;
                kModel.BasvuruTarihi = DateTime.Now;
                kModel.KullaniciID = UserIdentity.Current.Id;
                var bsurec = _entities.MezuniyetSurecis.First(p => p.MezuniyetSurecID == kModel.MezuniyetSurecID);
                kModel.EnstituKod = bsurec.EnstituKod;
                kModel.DonemAdi = bsurec.BaslangicYil + "/" + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;

            }
            else
            {
                var mezuniyetBasvurusu = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
                kModel.BasvuruTarihi = mezuniyetBasvurusu.BasvuruTarihi;
                kModel.KullaniciID = mezuniyetBasvurusu.KullaniciID;
                kModel.EnstituKod = mezuniyetBasvurusu.MezuniyetSureci.EnstituKod;
                kModel.DonemAdi = mezuniyetBasvurusu.MezuniyetSureci.BaslangicYil + "/" + mezuniyetBasvurusu.MezuniyetSureci.BitisYil + " " + mezuniyetBasvurusu.MezuniyetSureci.Donemler.DonemAdi;

            }
            var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(kModel.KullaniciID);

            var mmMessage = MezuniyetBus.MezuniyetBasvuruKriterKontrol(kModel.EnstituKod, kModel.MezuniyetBasvurulariID.ToNullIntZero());


            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);

            #region Kontrol

            if (mmMessage.IsSuccess)
            {
                var tezK = MezuniyetBus.TezKontrol(kModel);
                mmMessage.Messages.AddRange(tezK.Messages.ToList());
                mmMessage.MessagesDialog.AddRange(tezK.MessagesDialog.ToList());
                if (mmMessage.Messages.Count > 0) stps.Add(1);
                else
                {
                    if (kModel.MezuniyetYayinKontrolDurumID <= 0)
                    {
                        stps.Add(2);
                        mmMessage.Messages.Add("Başvuru Durumunu Seçiniz!");
                        mmMessage.MessagesDialog.Add(new MrMessage
                        { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinKontrolDurumID" });
                    }
                    else if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi)
                    {
                        var yaynK = MezuniyetBus.YayinKontrol(kModel);
                        mmMessage.Messages.AddRange(yaynK.Messages.ToList());
                        mmMessage.MessagesDialog.AddRange(yaynK.MessagesDialog.ToList());
                        if (mmMessage.Messages.Count > 0) stps.Add(2);
                    }

                }
            }

            #endregion

            if (kModel.MezuniyetBasvurulariID <= 0 && mmMessage.Messages.Count == 0)
            {

                var danismanTc = studentInfo.OgrenciInfo.DANISMAN_TC1;
                if (!kul.DanismanID.HasValue && (danismanTc.IsNullOrWhiteSpace() || danismanTc.Length != 11))
                {
                    mmMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    mmMessage.IsSuccess = false;
                }
                else if (kul.DanismanID.HasValue == false)
                {

                    mmMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
                    mmMessage.IsSuccess = false;
                }

            }
            kModel.IsYerli = kul.KullaniciTipleri.Yerli;
            kModel.ResimAdi = kul.ResimAdi;
            kModel.KullaniciTipAdi = _entities.KullaniciTipleris.First(p => p.KullaniciTipID == kul.KullaniciTipID).KullaniciTipAdi;
            kModel.KullaniciTipID = kul.KullaniciTipID;
            kModel.Ad = kul.Ad;
            kModel.Soyad = kul.Soyad;
            kModel.OgrenciNo = kul.OgrenciNo;

            kModel.IslemYapanID = UserIdentity.Current.Id;
            kModel.IslemTarihi = DateTime.Now;
            kModel.IslemYapanIP = UserIdentity.Ip;
            bool sendMail = false;
            if (mmMessage.Messages.Count == 0)
            {


                MezuniyetBasvurulari mBasvuru;
                bool isNewRecord = false;
                if (kModel.MezuniyetBasvurulariID <= 0)
                {
                    isNewRecord = true;


                    kModel.KayitOgretimYiliBaslangic = kul.KayitYilBaslangic;
                    kModel.KayitOgretimYiliDonemID = kul.KayitDonemID;
                    kModel.KayitTarihi = kul.KayitTarihi;

                    kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;
                    kModel.OgrenciNo = kul.OgrenciNo;
                    kModel.OgrenimDurumID = kul.OgrenimDurumID.Value;
                    kModel.ProgramKod = kul.ProgramKod;

                    kModel.BasvuruTarihi = DateTime.Now;

                    if (kModel.DanismanImzaliFormDosya != null)
                    {

                        string yBDosyaYolu = "/TezDosyalari/" + kModel.DanismanImzaliFormDosya.FileName.ToFileNameAddGuid();
                        var sfilename = Server.MapPath("~" + yBDosyaYolu);
                        kModel.DanismanImzaliFormDosya.SaveAs(sfilename);
                        kModel.DanismanImzaliFormDosyaAdi = kModel.DanismanImzaliFormDosya.FileName.GetFileName().ReplaceSpecialCharacter();
                        kModel.DanismanImzaliFormDosyaYolu = yBDosyaYolu;
                    }
                    mBasvuru = _entities.MezuniyetBasvurularis.Add(new MezuniyetBasvurulari
                    {
                        MezuniyetSurecID = kModel.MezuniyetSurecID,
                        RowID = Guid.NewGuid(),
                        BasvuruTarihi = kModel.BasvuruTarihi,
                        MezuniyetYayinKontrolDurumID = kModel.MezuniyetYayinKontrolDurumID,
                        MezuniyetYayinKontrolDurumAciklamasi = kModel.MezuniyetYayinKontrolDurumAciklamasi,
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
                        IsTezDiliTr = kModel.IsTezDiliTr,
                        TezBaslikTr = kModel.TezBaslikTr,
                        TezBaslikEn = kModel.TezBaslikEn,
                        TezDanismanUnvani = kModel.TezDanismanUnvani,
                        TezDanismanAdi = kModel.TezDanismanAdi,
                        TezEsDanismanUnvani = kModel.TezEsDanismanUnvani,
                        TezEsDanismanEMail = kModel.TezEsDanismanEMail,
                        TezEsDanismanAdi = kModel.TezEsDanismanAdi,
                        TezOzet = kModel.TezOzet,
                        OzetAnahtarKelimeler = kModel.OzetAnahtarKelimeler,
                        TezAbstract = kModel.TezAbstract,
                        AbstractAnahtarKelimeler = kModel.AbstractAnahtarKelimeler,
                        DanismanImzaliFormDosyaAdi = kModel.DanismanImzaliFormDosyaAdi,
                        DanismanImzaliFormDosyaYolu = kModel.DanismanImzaliFormDosyaYolu,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip

                    });



                    mBasvuru.TezDanismanID = kul.DanismanID;



                    _entities.SaveChanges();
                    kModel.MezuniyetBasvurulariID = mBasvuru.MezuniyetBasvurulariID;
                    if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi) sendMail = true;




                }
                else
                {

                    mBasvuru = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
                    if (!mBasvuru.TezDanismanID.HasValue || mBasvuru.TezDanismanID <= 0) mBasvuru.TezDanismanID = kul.DanismanID;
                    if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi && kModel.MezuniyetYayinKontrolDurumID != mBasvuru.MezuniyetYayinKontrolDurumID && MezuniyetAyar.GetAyarMz(MezuniyetAyar.YeniMezuniyetBasvurusundaMailGonder, kModel.EnstituKod).ToBoolean() == true)
                    {
                        mBasvuru.BasvuruTarihi = DateTime.Now;
                        sendMail = true;
                    }
                    if (mBasvuru.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
                    {
                        mBasvuru.IsDanismanOnay = null;
                    }
                    mBasvuru.MezuniyetSurecID = kModel.MezuniyetSurecID;
                    mBasvuru.BasvuruTarihi = kModel.BasvuruTarihi;
                    mBasvuru.MezuniyetYayinKontrolDurumID = kModel.MezuniyetYayinKontrolDurumID;
                    mBasvuru.MezuniyetYayinKontrolDurumAciklamasi = kModel.MezuniyetYayinKontrolDurumAciklamasi;
                    mBasvuru.KullaniciID = kModel.KullaniciID;
                    mBasvuru.KullaniciTipID = kModel.KullaniciTipID;
                    mBasvuru.ResimAdi = kModel.ResimAdi;
                    mBasvuru.Ad = kModel.Ad;
                    mBasvuru.Soyad = kModel.Soyad;
                    mBasvuru.UyrukKod = kModel.UyrukKod;
                    mBasvuru.TcKimlikNo = kModel.TcKimlikNo;
                    //mBasvuru.OgrenciNo = kModel.OgrenciNo;
                    //mBasvuru.OgrenimDurumID = kModel.OgrenimDurumID;
                    //mBasvuru.OgrenimTipKod = kModel.OgrenimTipKod;
                    //mBasvuru.ProgramKod = kModel.ProgramKod;
                    //mBasvuru.KayitOgretimYiliBaslangic = kModel.KayitOgretimYiliBaslangic;
                    //mBasvuru.KayitOgretimYiliDonemID = kModel.KayitOgretimYiliDonemID;
                    //mBasvuru.KayitTarihi = kModel.KayitTarihi;
                    mBasvuru.IsTezDiliTr = kModel.IsTezDiliTr;
                    mBasvuru.TezBaslikTr = kModel.TezBaslikTr;
                    mBasvuru.TezBaslikEn = kModel.TezBaslikEn;
                    mBasvuru.TezDanismanUnvani = kModel.TezDanismanUnvani;
                    mBasvuru.TezDanismanAdi = kModel.TezDanismanAdi;
                    mBasvuru.TezEsDanismanAdi = kModel.TezEsDanismanAdi;
                    mBasvuru.TezEsDanismanUnvani = kModel.TezEsDanismanUnvani;
                    mBasvuru.TezOzet = kModel.TezOzet;
                    mBasvuru.OzetAnahtarKelimeler = kModel.OzetAnahtarKelimeler;
                    mBasvuru.TezAbstract = kModel.TezAbstract;
                    mBasvuru.AbstractAnahtarKelimeler = kModel.AbstractAnahtarKelimeler;
                    mBasvuru.IslemTarihi = DateTime.Now;
                    mBasvuru.IslemYapanID = UserIdentity.Current.Id;
                    mBasvuru.IslemYapanIP = UserIdentity.Ip;

                    var silinecekYayins = _entities.MezuniyetBasvurulariYayins.Where(p => kModel._MezuniyetBasvurulariYayinID.Contains(p.MezuniyetBasvurulariYayinID) == false && p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).ToList();
                    var guncellenecekYayins = _entities.MezuniyetBasvurulariYayins.Where(p => kModel._MezuniyetBasvurulariYayinID.Contains(p.MezuniyetBasvurulariYayinID) && p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).ToList();
                    var fFList = new List<string>();
                    foreach (var item in silinecekYayins)
                    {
                        if (item.MezuniyetYayinBelgeDosyaYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinBelgeDosyaYolu);
                        if (item.MezuniyetYayinMetniBelgeYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinMetniBelgeYolu);
                    }

                    _entities.MezuniyetBasvurulariYayins.RemoveRange(silinecekYayins);
                    if (kModel.DanismanImzaliFormDosya != null)
                    {
                        var path = Server.MapPath("~" + mBasvuru.DanismanImzaliFormDosyaYolu);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                        if (kModel.DanismanImzaliFormDosya != null)
                        {
                            string yBDosyaYolu = "/TezDosyalari/" + kModel.DanismanImzaliFormDosya.FileName.ToFileNameAddGuid();
                            kModel.DanismanImzaliFormDosya.SaveAs(Server.MapPath("~" + yBDosyaYolu));
                            mBasvuru.DanismanImzaliFormDosyaAdi = kModel.DanismanImzaliFormDosya.FileName.GetFileName().ReplaceSpecialCharacter();
                            mBasvuru.DanismanImzaliFormDosyaYolu = yBDosyaYolu;
                        }
                    }
                    foreach (var item in guncellenecekYayins)
                    {
                        item.Onaylandi = null;
                    }
                    _entities.SaveChanges();
                    foreach (var item in fFList)
                    {
                        var path = Server.MapPath("~" + item);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }

                }

                LogIslemleri.LogEkle("MezuniyetBasvurulari", isNewRecord ? LogCrudType.Insert : LogCrudType.Update, mBasvuru.ToJson());


                var qMyId = kModel._MezuniyetBasvurulariYayinID.Select((s, inx) => new { MezuniyetBasvurulariYayinID = s, Index = inx }).ToList();
                var qYbaslik = kModel._YayinBasligi.Select((s, inx) => new { YayinBasligi = s, Index = inx }).ToList();
                var qYy = kModel._Yayinlanmis.Select((s, inx) => new { Yayinlanmis = s, Index = inx }).ToList();
                var qYTar = kModel._MezuniyetYayinTarih.Select((s, inx) => new { MezuniyetYayinTarih = s, Index = inx }).ToList();
                var qMytId = kModel._MezuniyetYayinTurID.Select((s, inx) => new { MezuniyetYayinTurID = s, Index = inx }).ToList();
                var qMybelge = kModel._MezuniyetYayinBelgesi.Select((s, inx) => new { MezuniyetYayinBelgesi = s, Index = inx }).ToList();
                var qMybelgeAd = kModel._MezuniyetYayinBelgesiAdi.Select((s, inx) => new { MezuniyetYayinBelgesiAdi = s, Index = inx }).ToList();
                var qMkLink = kModel._MezuniyetYayinKaynakLinki.Select((s, inx) => new { MezuniyetYayinKaynakLinki = s, Index = inx }).ToList();
                var qMbelge = kModel._YayinMetniBelgesi.Select((s, inx) => new { YayinMetniBelgesi = s, Index = inx }).ToList();
                var qMbelgeAd = kModel._YayinMetniBelgesiAdi.Select((s, inx) => new { YayinMetniBelgesiAdi = s, Index = inx }).ToList();
                var qMyLink = kModel._MezuniyetYayinLinki.Select((s, inx) => new { MezuniyetYayinLinki = s, Index = inx }).ToList();
                var qIndex = kModel._MezuniyetYayinIndexTurID.Select((s, inx) => new { MezuniyetYayinIndexTurID = s, Index = inx }).ToList();
                var qKeM = kModel._MezuniyetYayinKabulEdilmisMakaleBelgesi.Select((s, inx) => new { KabulEdilmisMakale = s, Index = inx }).ToList();


                var qYaz = kModel._YazarAdi.Select((s, inx) => new { YazarAdi = s, Index = inx }).ToList();
                var qDer = kModel._DergiAdi.Select((s, inx) => new { DergiAdi = s, Index = inx }).ToList();
                var qYcs = kModel._YilCiltSayiSS.Select((s, inx) => new { YilCiltSayiSS = s, Index = inx }).ToList();
                var qPid = kModel._MezuniyetYayinProjeTurID.Select((s, inx) => new { MezuniyetYayinProjeTurID = s, Index = inx }).ToList();
                var qDvm = kModel._IsProjeTamamlandiOrDevamEdiyor.Select((s, inx) => new { IsProjeTamamlandiOrDevamEdiyor = s, Index = inx }).ToList();
                var qPek = kModel._ProjeEkibi.Select((s, inx) => new { ProjeEkibi = s, Index = inx }).ToList();
                var qPdk = kModel._ProjeDeatKurulus.Select((s, inx) => new { ProjeDeatKurulus = s, Index = inx }).ToList();
                var qTara = kModel._TarihAraligi.Select((s, inx) => new { TarihAraligi = s, Index = inx }).ToList();
                var qEtad = kModel._EtkinlikAdi.Select((s, inx) => new { EtkinlikAdi = s, Index = inx }).ToList();
                var qYer = kModel._YerBilgisi.Select((s, inx) => new { YerBilgisi = s, Index = inx }).ToList();

                var qYayins = (from b in qYbaslik
                               join my in qMyId on b.Index equals my.Index
                               join yd in qYy on b.Index equals yd.Index
                               join mytar in qYTar on b.Index equals mytar.Index
                               join myt in qMytId on b.Index equals myt.Index
                               join myb in qMybelge on b.Index equals myb.Index
                               join mybA in qMybelgeAd on b.Index equals mybA.Index
                               join mkl in qMkLink on b.Index equals mkl.Index
                               join mb in qMbelge on b.Index equals mb.Index
                               join mbA in qMbelgeAd on b.Index equals mbA.Index
                               join myl in qMyLink on b.Index equals myl.Index
                               join mI in qIndex on b.Index equals mI.Index
                               join kem in qKeM on b.Index equals kem.Index
                               join bt in _entities.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == kModel.MezuniyetSurecID) on myt.MezuniyetYayinTurID equals bt.MezuniyetYayinTurID

                               join yaz in qYaz on b.Index equals yaz.Index
                               join der in qDer on b.Index equals der.Index
                               join ycs in qYcs on b.Index equals ycs.Index
                               join pid in qPid on b.Index equals pid.Index
                               join dvm in qDvm on b.Index equals dvm.Index
                               join pek in qPek on b.Index equals pek.Index
                               join pdk in qPdk on b.Index equals pdk.Index
                               join tara in qTara on b.Index equals tara.Index
                               join etad in qEtad on b.Index equals etad.Index
                               join yer in qYer on b.Index equals yer.Index
                               where my.MezuniyetBasvurulariYayinID == 0
                               select new
                               {
                                   b.Index,
                                   yd.Yayinlanmis,
                                   b.YayinBasligi,
                                   mytar.MezuniyetYayinTarih,
                                   my.MezuniyetBasvurulariYayinID,
                                   myt.MezuniyetYayinTurID,
                                   bt.MezuniyetYayinBelgeTurID,
                                   myb.MezuniyetYayinBelgesi,
                                   mybA.MezuniyetYayinBelgesiAdi,
                                   bt.KaynakMezuniyetYayinLinkTurID,
                                   mkl.MezuniyetYayinKaynakLinki,
                                   bt.MezuniyetYayinMetinTurID,
                                   mb.YayinMetniBelgesi,
                                   mbA.YayinMetniBelgesiAdi,
                                   bt.YayinMezuniyetYayinLinkTurID,
                                   myl.MezuniyetYayinLinki,
                                   mI.MezuniyetYayinIndexTurID,
                                   kem.KabulEdilmisMakale,
                                   yaz.YazarAdi,
                                   der.DergiAdi,
                                   ycs.YilCiltSayiSS,
                                   pid.MezuniyetYayinProjeTurID,
                                   dvm.IsProjeTamamlandiOrDevamEdiyor,
                                   pek.ProjeEkibi,
                                   pdk.ProjeDeatKurulus,
                                   tara.TarihAraligi,
                                   etad.EtkinlikAdi,
                                   yer.YerBilgisi
                               }).ToList();
                foreach (var item in qYayins)
                {
                    var rowMdy = new Models.MezuniyetBasvurulariYayin
                    {
                        MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID,
                        MezuniyetYayinTurID = item.MezuniyetYayinTurID,
                        Yayinlanmis = item.Yayinlanmis,
                        YayinBasligi = item.YayinBasligi,
                        MezuniyetYayinTarih = item.MezuniyetYayinTarih
                    };
                    if (item.MezuniyetYayinBelgesi != null)
                    {
                        string yBDosyaYolu = "/TezDosyalari/" + item.MezuniyetYayinBelgesi.FileName.ToFileNameAddGuid();
                        item.MezuniyetYayinBelgesi.SaveAs(Server.MapPath("~" + yBDosyaYolu));
                        rowMdy.MezuniyetYayinBelgeTurID = item.MezuniyetYayinBelgeTurID;
                        rowMdy.MezuniyetYayinBelgeAdi = item.MezuniyetYayinBelgesi.FileName.GetFileName().ReplaceSpecialCharacter();
                        rowMdy.MezuniyetYayinBelgeDosyaYolu = yBDosyaYolu;
                    }
                    rowMdy.MezuniyetYayinKaynakLinkTurID = item.KaynakMezuniyetYayinLinkTurID;
                    rowMdy.MezuniyetYayinKaynakLinki = item.MezuniyetYayinKaynakLinki;
                    if (item.YayinMetniBelgesi != null)
                    {
                        string ymTDosyaYolu = "/TezDosyalari/" + item.YayinMetniBelgesi.FileName.ToFileNameAddGuid();
                        item.YayinMetniBelgesi.SaveAs(Server.MapPath("~" + ymTDosyaYolu));
                        rowMdy.MezuniyetYayinMetinTurID = item.MezuniyetYayinMetinTurID;
                        rowMdy.MezuniyetYayinMetniBelgeAdi = item.YayinMetniBelgesi.FileName.GetFileName().ReplaceSpecialCharacter();
                        rowMdy.MezuniyetYayinMetniBelgeYolu = ymTDosyaYolu;
                    }
                    if (item.KabulEdilmisMakale != null)
                    {

                        string ymTDosyaYolu = "/TezDosyalari/" + item.KabulEdilmisMakale.FileName.ToFileNameAddGuid();
                        item.KabulEdilmisMakale.SaveAs(Server.MapPath("~" + ymTDosyaYolu));

                        rowMdy.MezuniyetYayinKabulEdilmisMakaleAdi = item.KabulEdilmisMakale.FileName.GetFileName().ReplaceSpecialCharacter();
                        rowMdy.MezuniyetYayinKabulEdilmisMakaleDosyaYolu = ymTDosyaYolu;
                    }
                    rowMdy.MezuniyetYayinLinkTurID = item.YayinMezuniyetYayinLinkTurID;
                    rowMdy.MezuniyetYayinLinki = item.MezuniyetYayinLinki;
                    rowMdy.MezuniyetYayinIndexTurID = item.MezuniyetYayinIndexTurID;


                    rowMdy.DergiAdi = item.DergiAdi;
                    rowMdy.YazarAdi = item.YazarAdi;
                    rowMdy.YilCiltSayiSS = item.YilCiltSayiSS;
                    rowMdy.MezuniyetYayinProjeTurID = item.MezuniyetYayinProjeTurID;
                    rowMdy.IsProjeTamamlandiOrDevamEdiyor = item.IsProjeTamamlandiOrDevamEdiyor;
                    rowMdy.ProjeEkibi = item.ProjeEkibi;
                    rowMdy.ProjeDeatKurulus = item.ProjeDeatKurulus;
                    rowMdy.TarihAraligi = item.TarihAraligi;
                    rowMdy.EtkinlikAdi = item.EtkinlikAdi;
                    rowMdy.YerBilgisi = item.YerBilgisi;
                    _entities.MezuniyetBasvurulariYayins.Add(rowMdy);

                }
                _entities.SaveChanges();
                if (sendMail)
                {
                    MailSenderMezuniyet.SendMailBasvuruYapildi(mBasvuru.MezuniyetBasvurulariID);
                }
                if (kModel.KullaniciID != UserIdentity.Current.Id) return RedirectToAction("Index", "MezuniyetGelenBasvurular");
                return RedirectToAction("Index", kModel.KullaniciID);
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

            if (stps.Count > 0) kModel.SetSelectedStep = stps.First();
            ViewBag._MmMessage = mmMessage;
            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurum(true), "Value", "Caption", kModel.MezuniyetYayinKontrolDurumID);
            ViewBag.TezEsDanismanUnvani = new SelectList(UnvanlarBus.GetCmbJuriUnvanlar(true), "Value", "Caption", kModel.TezEsDanismanUnvani);
            ViewBag.MezuniyetYayinTurID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecYayinTurleri(kModel.MezuniyetSurecID, kModel.KullaniciID, kModel.MezuniyetBasvurulariID, true), "Value", "Caption");


            if (kModel.MezuniyetYayinKontrolDurumID > 0)
            {
                ViewBag.MezuniyetYayinKontrolDurumu = _entities.MezuniyetYayinKontrolDurumlaris.Where(p => p.MezuniyetYayinKontrolDurumID == kModel.MezuniyetYayinKontrolDurumID).Select(s => new MezuniyetYayinKontrolDurumDto { MezuniyetYayinKontrolDurumID = s.MezuniyetYayinKontrolDurumID, ClassName = s.ClassName, Color = s.Color, DurumAdi = s.MezuniyetYayinKontrolDurumAdi }).First();
            }
            else
            {
                ViewBag.MezuniyetYayinKontrolDurumu = new MezuniyetYayinKontrolDurumDto
                {
                    DurumAdi = "Yeni Başvuru",
                    ClassName = "fa fa-plus",
                    Color = "color:black;"
                };
            }

            return View(kModel);
        }
        public ActionResult GetYayinTur(int mezuniyetSurecId, int mezuniyetYayinTurId)
        {

            var mdl = MezuniyetBus.GetYayinBilgisi(mezuniyetSurecId, mezuniyetYayinTurId);
            mdl.MezuniyetSurecID = mezuniyetSurecId;
            return View(mdl);
        }

        public ActionResult YayinEklemeKontrol(KmMezuniyetBasvuru model)
        {
            string projeTurAdi = "";
            if (model.YayinBilgisi.MezuniyetYayinProjeTurID.HasValue)
            {
                var projeTuru = _entities.MezuniyetYayinProjeTurleris.First(p => p.MezuniyetYayinProjeTurID == model.YayinBilgisi.MezuniyetYayinProjeTurID);
                projeTurAdi = projeTuru.ProjeTurAdi;
            }

            var yayinBilgi = (from s in _entities.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == model.MezuniyetSurecID && p.MezuniyetYayinTurID == model.YayinBilgisi.MezuniyetYayinTurID)
                              join sd in _entities.MezuniyetYayinTurleris on s.MezuniyetYayinTurID equals sd.MezuniyetYayinTurID
                              join yb in _entities.MezuniyetYayinBelgeTurleris on s.MezuniyetYayinBelgeTurID equals yb.MezuniyetYayinBelgeTurID into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in _entities.MezuniyetYayinLinkTurleris on s.KaynakMezuniyetYayinLinkTurID equals klk.MezuniyetYayinLinkTurID into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in _entities.MezuniyetYayinMetinTurleris on s.MezuniyetYayinMetinTurID equals ym.MezuniyetYayinMetinTurID into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in _entities.MezuniyetYayinLinkTurleris on s.YayinMezuniyetYayinLinkTurID equals kl.MezuniyetYayinLinkTurID into defkl
                              from klD in defkl.DefaultIfEmpty()
                              select new MezuniyetBasvurulariYayinDto
                              {
                                  Yayinlanmis = model.YayinBilgisi.Yayinlanmis,
                                  MezuniyetYayinTurID = model.YayinBilgisi.MezuniyetYayinTurID,
                                  YayinBasligi = model.YayinBilgisi.YayinBasligi,
                                  MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                                  MezuniyetYayinTarih = model.YayinBilgisi.MezuniyetYayinTarih,
                                  MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                                  MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                                  MezuniyetYayinBelgeAdi = model.YayinBilgisi.MezuniyetYayinBelgeAdi,
                                  MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                                  MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                                  MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                                  MezuniyetYayinKaynakLinki = model.YayinBilgisi.MezuniyetYayinKaynakLinki,
                                  MezuniyetYayinKaynakLinkIsUrl = klkD != null && klkD.IsUrl,
                                  MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = model.YayinBilgisi.MezuniyetYayinMetniBelgeAdi,
                                  MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = klD.LinkTurAdi ?? "",
                                  MezuniyetYayinLinkIsUrl = klD != null && klD.IsUrl,
                                  MezuniyetYayinLinki = model.YayinBilgisi.MezuniyetYayinLinki,
                                  MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                                  MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                                  MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                                  MezuniyetYayinKabulEdilmisMakaleAdi = model.YayinBilgisi.MezuniyetYayinKabulEdilmisMakaleAdi,
                                  YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                                  ProjeDeatKurulus = model.YayinBilgisi.ProjeDeatKurulus,
                                  YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                                  DergiAdi = model.YayinBilgisi.DergiAdi,
                                  YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                                  IsProjeTamamlandiOrDevamEdiyor = model.YayinBilgisi.IsProjeTamamlandiOrDevamEdiyor,
                                  YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                                  ProjeEkibi = model.YayinBilgisi.ProjeEkibi,
                                  YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                                  MezuniyetYayinProjeTurID = model.YayinBilgisi.MezuniyetYayinProjeTurID,
                                  ProjeTurAdi = projeTurAdi,
                                  YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                                  YazarAdi = model.YayinBilgisi.YazarAdi,
                                  YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                                  YilCiltSayiSS = model.YayinBilgisi.YilCiltSayiSS,
                                  IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                                  TarihAraligi = model.YayinBilgisi.TarihAraligi,
                                  YayinEtkinlikAdiIstensin = s.YayinEtkinlikAdiIstensin,
                                  EtkinlikAdi = model.YayinBilgisi.EtkinlikAdi,
                                  YayinYerBilgisiIstensin = s.YayinYerBilgisiIstensin,
                                  YerBilgisi = model.YayinBilgisi.YerBilgisi
                              }).First();
            var mmMessage = MezuniyetBus.YayinKontrol(model);
            if (mmMessage.Messages.Count == 0)
            {


                if ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis.HasValue) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false)
                {
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Yayinlanmis" });
                    if (model.YayinBilgisi.YayinBasligi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Yayın Başlığı Bilgisini Giriniz");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YayinBasligi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YayinBasligi" });
                    if (yayinBilgi.YayinYazarlarIstensin && model.YayinBilgisi.YazarAdi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Yazarları giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YazarAdi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YazarAdi" });
                    if (yayinBilgi.YayinProjeTurIstensin && !model.YayinBilgisi.MezuniyetYayinProjeTurID.HasValue)
                    {
                        mmMessage.Messages.Add("Proje Türü seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinProjeTurID" });
                    }
                    else
                    {
                        if (model.YayinBilgisi.MezuniyetYayinProjeTurID == 3)
                        {
                            if (yayinBilgi.YayinProjeTurIstensin && model.YayinBilgisi.ProjeDeatKurulus.IsNullOrWhiteSpace())
                            {
                                mmMessage.Messages.Add("Proje Dest. Kuruluş giriniz.");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "ProjeDeatKurulus" });
                            }
                            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "ProjeDeatKurulus" });
                        }
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MezuniyetYayinProjeTurID" });
                    }
                    if (yayinBilgi.YayinProjeEkibiIstensin && model.YayinBilgisi.ProjeEkibi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Proje Ekibi giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "ProjeEkibi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "ProjeEkibi" });
                    if (yayinBilgi.IsTarihAraligiIstensin && model.YayinBilgisi.TarihAraligi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Tarih Aralığı giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TarihAraligi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TarihAraligi" });
                    if (yayinBilgi.YayinMevcutDurumIstensin && !model.YayinBilgisi.IsProjeTamamlandiOrDevamEdiyor.HasValue)
                    {
                        mmMessage.Messages.Add("Proje Mevcut Durum seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsProjeTamamlandiOrDevamEdiyor" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IsProjeTamamlandiOrDevamEdiyor" });


                    if (yayinBilgi.YayinDergiAdiIstensin && model.YayinBilgisi.DergiAdi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Dergi Adı giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DergiAdi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DergiAdi" });
                    if (yayinBilgi.YayinYilCiltSayiIstensin && model.YayinBilgisi.YilCiltSayiSS.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Yıl/Cilt/Sayı/ss giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YilCiltSayiSS" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YilCiltSayiSS" });
                    if (yayinBilgi.YayinEtkinlikAdiIstensin && model.YayinBilgisi.EtkinlikAdi.IsNullOrWhiteSpace())
                    {

                        mmMessage.Messages.Add("Etkinlik Adı giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EtkinlikAdi" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EtkinlikAdi" });
                    if (yayinBilgi.YayinYerBilgisiIstensin && model.YayinBilgisi.YerBilgisi.IsNullOrWhiteSpace())
                    {

                        mmMessage.Messages.Add("Yer Bilgisi giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YerBilgisi" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EtkinlikAdi" });

                    if (yayinBilgi.MezuniyetYayinTarihZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == true) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinTarih.HasValue == false)
                        {
                            mmMessage.Messages.Add("Bildiri Tarihi");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinTarih" });
                        }
                        else if (model.YayinBilgisi.MezuniyetYayinTarih.Value > DateTime.Now)
                        {
                            mmMessage.Messages.Add("Bildiri Tarihi Bu günkü tarihten büyük olamaz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinTarih" });
                        }

                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MezuniyetYayinTarih" });
                    }

                    if (yayinBilgi.MezuniyetYayinBelgeTurZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == false) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinBelgeAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinBelgeTurAdi + " Belgesini Yükleyiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinBelgeAdi" });
                        }
                        else if (model.YayinBilgisi.MezuniyetYayinBelgeAdi.Split('.').Last().ToLower() != "pdf")
                        {
                            mmMessage.Messages.Add(model.YayinBilgisi.MezuniyetYayinBelgeAdi + " Belgesini Pdf Türünde Olmalı");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinBelgeAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MezuniyetYayinBelgeAdi" });
                    }
                    if (yayinBilgi.MezuniyetYayinKaynakLinkTurZorunlu)
                    {
                        if (model.YayinBilgisi.MezuniyetYayinKaynakLinki.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinKaynakLinkTurAdi + " Bilgisini Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinKaynakLinki" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MezuniyetYayinKaynakLinki" });
                    }
                    if (yayinBilgi.MezuniyetYayinMetinZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == true) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinMetniBelgeAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinMetinTurAdi + " Belgesini Yükleyiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinMetniBelgeAdi" });
                        }
                        else if (model.YayinBilgisi.MezuniyetYayinMetniBelgeAdi.Split('.').Last().ToLower() != "pdf")
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinMetinTurAdi + " Belgesi PDF Türünde Olmalıdır");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinMetniBelgeAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MezuniyetYayinMetniBelgeAdi" });
                    }
                    if (yayinBilgi.MezuniyetYayinLinkiZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == true) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinLinki.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinLinkTurAdi + " Bilgisini Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinLinki" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MezuniyetYayinLinki" });
                    }
                    if (yayinBilgi.MezuniyetYayinIndexTurZorunlu)
                    {
                        if (model.YayinBilgisi.MezuniyetYayinIndexTurID.HasValue == false)
                        {

                            mmMessage.Messages.Add("Index Türü Seçiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinIndexTurID" });
                        }
                        else
                        {
                            var inxB = _entities.MezuniyetYayinIndexTurleris.First(p => p.MezuniyetYayinIndexTurID == model.YayinBilgisi.MezuniyetYayinIndexTurID.Value);
                            yayinBilgi.MezuniyetYayinIndexTurID = model.YayinBilgisi.MezuniyetYayinIndexTurID;
                            yayinBilgi.MezuniyetYayinIndexTurAdi = inxB.IndexTurAdi;
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MezuniyetYayinIndexTurID" });
                        }
                    }
                    if (yayinBilgi.MezuniyetKabulEdilmisMakaleZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == false) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinKabulEdilmisMakaleAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Kabul Edilmiş Makale Yükleyiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinKabulEdilmisMakaleAdi" });
                        }
                        else if (model.YayinBilgisi.MezuniyetYayinKabulEdilmisMakaleAdi.Split('.').Last().ToLower() != "pdf")
                        {
                            mmMessage.Messages.Add("Kabul Edilmiş Makale PDF Türünde Olmalıdır");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "MezuniyetYayinKabulEdilmisMakaleAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "MezuniyetYayinKabulEdilmisMakaleAdi" });
                    }
                }
                else
                {
                    mmMessage.Messages.Add("Yayın Durumunu Seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Yayinlanmis" });
                }
            }
            mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
            string row = "";
            if (mmMessage.IsSuccess)
            {
                row = ViewRenderHelper.RenderPartialView("Mezuniyet", "AddRow", yayinBilgi);
            }
            mmMessage.Title = "Yayın Bilgisi Ekleme İşlemi";
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            return Json(new { _Row = row, _guID = yayinBilgi.guID, msg = mmMessage });


        }

        public ActionResult RezervasyonAl(int mezuniyetBasvurulariId, int srTalepId)
        {

            var yetkiliKullanici = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var srYetkiliKullanici = RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent();
            var mezuniyetBasvurularis = _entities.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
            if (!yetkiliKullanici && !srYetkiliKullanici) mezuniyetBasvurularis.Where(p => p.KullaniciID == UserIdentity.Current.Id);
            else if (srYetkiliKullanici) mezuniyetBasvurularis.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            var mezuniyetBasvuru = mezuniyetBasvurularis.First();
            var model = new SrTalepleriKayitDto
            {
                IsSalonSecilsin = mezuniyetBasvuru.OgrenimTipKod.IsDoktora() && mezuniyetBasvuru.MezuniyetSureci.EnstituKod == EnstituKodlariEnum.FenBilimleri
            };
            if (srTalepId > 0)
            {
                var srTalebi = mezuniyetBasvuru.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                var tarih = model.IsSalonSecilsin ? srTalebi.Tarih : (srTalebi.Tarih.AddHours(srTalebi.BasSaat.Hours).AddMinutes(srTalebi.BasSaat.Minutes));
                model.MzRowId = mezuniyetBasvuru.RowID.ToString();
                model.SRTalepID = srTalebi.SRTalepID;
                model.SRTalepTipID = srTalebi.SRTalepTipID;
                model.TalepYapanID = srTalebi.TalepYapanID;
                model.SRSalonID = srTalebi.SRSalonID;
                model.SalonAdi = srTalebi.SalonAdi;
                model.Tarih = tarih;


            }
            else
            {
                model.MzRowId = mezuniyetBasvuru.RowID.ToString();
                model.SRTalepTipID = 1;
                model.TalepYapanID = mezuniyetBasvuru.KullaniciID;
                var ogrenimTipKriterleri = mezuniyetBasvuru.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == mezuniyetBasvuru.OgrenimTipKod);
                if (mezuniyetBasvuru.EYKTarihi != null)
                    model.Tarih = mezuniyetBasvuru.EYKTarihi.Value.AddDays(ogrenimTipKriterleri.SinavKacGunSonraAlabilir);
            }
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(mezuniyetBasvuru.MezuniyetSureci.EnstituKod, model.SRTalepTipID, true), "Value", "Caption", model.SRSalonID);
            return View(model);
        }
        [HttpPost]
        public ActionResult RezervasyonAlPost(SrTalepleriKayitDto kModel)
        {

            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Salon rezervasyonu talep işlemi",
                MessageType = MsgTypeEnum.Warning
            };
            var surecKayitYetki = RoleNames.MezuniyetSureciKayıt.InRoleCurrent();

            var mezuniyetBasvurusu = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
            var sonSrTalebi = mezuniyetBasvurusu.SRTalepleris.LastOrDefault();
            var srTalebiYetkisi = mezuniyetBasvurusu.KullaniciID == UserIdentity.Current.Id || RoleNames.MezuniyetGelenBasvurularSrTalebiYap.InRoleCurrent();


            kModel.SRTalepTipID = 1;
            kModel.IsSalonSecilsin = mezuniyetBasvurusu.OgrenimTipKod.IsDoktora() && mezuniyetBasvurusu.MezuniyetSureci.EnstituKod != EnstituKodlariEnum.SosyalBilimleri;
            kModel.EnstituKod = mezuniyetBasvurusu.MezuniyetSureci.EnstituKod;
            if (!srTalebiYetkisi)
            {
                const string msg = "Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!";
                mmMessage.Messages.Add(msg);
                SistemBilgilendirmeBus.SistemBilgisiKaydet(msg + "\r\n İşlem yapılmak istenen KullanıcıID:" + kModel.TalepYapanID + "\r\n İşlemYapanID:" + UserIdentity.Current.Id, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Saldırı);
            }
            if (sonSrTalebi != null && sonSrTalebi.SRDurumID == SrTalepDurumEnum.Onaylandı && !sonSrTalebi.MezuniyetSinavDurumID.HasValue)
            {
                mmMessage.Messages.Add("Son Sınav bilgisi enstitü tarafından onaylanmadan yeni rezervasyon alınamaz.");

            }
            if (mezuniyetBasvurusu.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            if (kModel.SRTalepID > 0)
            {
                var srTalep = mezuniyetBasvurusu.SRTalepleris.First(p => p.SRTalepID == kModel.SRTalepID);
                var srDurum = srTalep.SRDurumlari;
                kModel.SRDurumID = srTalep.SRDurumID;
                if (srTalep.SRDurumID != SrTalepDurumEnum.TalepEdildi && !(srTalep.MezuniyetSinavDurumID == SrTalepDurumEnum.Onaylandı && UserIdentity.Current.IsAdmin))
                {
                    mmMessage.Messages.Add("Salon rezervasyonu " + srDurum.DurumAdi + " olduğundan düzeltme işlemi yapılamaz.");
                }
            }
            else
            {
                if (!mezuniyetBasvurusu.EYKTarihi.HasValue)
                {
                    mmMessage.Messages.Add("Enstitü tarafındanEyk tarihi girilmeden salon rezervasyonu yapılamaz!");
                }
            }
            if (kModel.MezuniyetSinavDurumID.HasValue && mezuniyetBasvurusu.MezuniyetSinavDurumID != MezuniyetSinavDurumEnum.SonucGirilmedi)
            {
                mmMessage.Messages.Add("Sınav sonuç bilgisi girilen rezervasyonlar üzerinde düzeltme işlemi yapılamaz!");

            }


            if (mezuniyetBasvurusu.SRTalepleris.Any(a => (a.SRDurumID == SrSalonDurumEnum.Alındı || a.SRDurumID == SrSalonDurumEnum.OnTalep) && a.MezuniyetSinavDurumID != MezuniyetSinavDurumEnum.Uzatma && a.SRTalepID != kModel.SRTalepID))
            {
                mmMessage.Messages.Add("Aktif bir salon rezervasyonu kaydınız bulunmaktadır. Tekrar rezervasyon işlemi yapamazsınız.");
            }
            var mezuniyetSureciOgrenimTip = mezuniyetBasvurusu.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == mezuniyetBasvurusu.OgrenimTipKod);

            if (mmMessage.Messages.Count == 0)
            {
                if (mezuniyetBasvurusu.EYKTarihi != null)
                {
                    var srBaslangicTarihi = mezuniyetBasvurusu.EYKTarihi.Value.AddDays(mezuniyetSureciOgrenimTip.SinavKacGunSonraAlabilir);
                    if (kModel.IsSalonSecilsin)
                    {
                        if (kModel.SRSalonID <= 0 || !kModel.SRSalonID.HasValue)
                        {
                            mmMessage.Messages.Add("Salon seçimi yapınız!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SRSalonID" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "SRSalonID" });
                        if (kModel.Tarih == DateTime.MinValue)
                        {
                            mmMessage.Messages.Add("Sınav tarihi seçimi yapınız!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                        }
                        else if (kModel.Tarih.Date < srBaslangicTarihi.Date)
                        {
                            mmMessage.Messages.Add("Sınav tarihi " + srBaslangicTarihi.Date.ToFormatDate() + " tarihinden küçük olamaz!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                        }
                        if (kModel.BasSaat == TimeSpan.MinValue || kModel.BitSaat == TimeSpan.MinValue)
                        {
                            mmMessage.Messages.Add("Lütfen belirtilen güne ait uygun saat seçiniz!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                        }
                    }
                    else
                    {
                        if (kModel.SalonAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Salon Adı Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SalonAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "SalonAdi" });
                        if (kModel.Tarih == DateTime.MinValue)
                        {
                            mmMessage.Messages.Add("Sınav tarihi seçimi yapınız!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                        }
                        else if (kModel.Tarih.Date < srBaslangicTarihi.Date)
                        {
                            mmMessage.Messages.Add("Sınav tarihi " + srBaslangicTarihi.Date.ToFormatDate() + " tarihinden küçük olamaz!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                        }
                    }
                }

                if (mmMessage.Messages.Count == 0 && !surecKayitYetki)
                {
                    var uzatmaAlinanSrTalebi = mezuniyetBasvurusu.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma && p.SRDurumID == SrTalepDurumEnum.Onaylandı).OrderByDescending(o => o.SRTalepID).FirstOrDefault();
                    if (uzatmaAlinanSrTalebi != null)
                    {
                        var uzatmaSonSrAlmaTarihi = uzatmaAlinanSrTalebi.UzatmaSonrasiYeniSinavTalebiSonTarih ?? uzatmaAlinanSrTalebi.Tarih.AddDays(mezuniyetSureciOgrenimTip.SinavUzatmaSinavAlmaSuresiMaxGun);
                        if (kModel.Tarih > uzatmaSonSrAlmaTarihi)
                        {
                            mmMessage.Messages.Add("Mezuniyet sınavı sonucunda almış olduğunuz uzatma işlemi sonrası son sınav tarihi olan '" + uzatmaSonSrAlmaTarihi.ToFormatDate() + "' tarihini aştığınız için yeni sınav oluşturamazsınız.");
                        }
                    }
                }

                if (mmMessage.Messages.Count == 0 && kModel.IsSalonSecilsin)
                {
                    if (kModel.SRSalonID != null)
                    {
                        var srKayitKontrolMessage = SrTalepleriBus.SrKayitKontrol(kModel.SRSalonID.Value, kModel.Tarih, kModel.BasSaat, kModel.BitSaat, kModel.SRTalepID, mezuniyetBasvurusu.EYKTarihi);
                        mmMessage.Messages.AddRange(srKayitKontrolMessage.Messages);
                    }
                }
                kModel.SRSalonID = kModel.IsSalonSecilsin ? kModel.SRSalonID : null;
                kModel.SalonAdi = kModel.IsSalonSecilsin && kModel.SRSalonID.HasValue ? _entities.SRSalonlars.First(p => p.SRSalonID == kModel.SRSalonID).SalonAdi : kModel.SalonAdi;
                if (mmMessage.Messages.Count == 0)
                {
                    try
                    {
                        kModel.IslemTarihi = DateTime.Now;
                        kModel.IslemYapanID = UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        kModel.HaftaGunID = (int)kModel.Tarih.DayOfWeek;
                        kModel.BasSaat = kModel.IsSalonSecilsin ? kModel.BasSaat : kModel.Tarih.TimeOfDay;
                        kModel.BitSaat = kModel.IsSalonSecilsin ? kModel.BitSaat : kModel.BasSaat.Add(new TimeSpan(2, 0, 0));
                        kModel.Tarih = kModel.Tarih.Date;


                        kModel.DanismanAdi = mezuniyetBasvurusu.TezDanismanAdi;
                        kModel.EsDanismanAdi = mezuniyetBasvurusu.TezEsDanismanAdi;
                        kModel.TezOzeti = mezuniyetBasvurusu.TezOzet;
                        if (kModel.SRTalepID <= 0)
                        {
                            kModel.SRDurumID = SrTalepDurumEnum.TalepEdildi;
                        }

                        kModel.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                        kModel.IslemTarihi = kModel.IslemTarihi;
                        kModel.IslemYapanID = kModel.IslemYapanID;
                        kModel.IslemYapanIP = kModel.IslemYapanIP;
                        var juriOneriFormu = mezuniyetBasvurusu.MezuniyetJuriOneriFormlaris.First();
                        var mezuniyetJuriOneriFormuJurileris = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == true).ToList();
                        foreach (var juri in mezuniyetJuriOneriFormuJurileris)
                        {
                            kModel.SRTaleplerJuris.Add(new SRTaleplerJuri
                            {
                                UniqueID = Guid.NewGuid(),
                                MezuniyetJuriOneriFormuJuriID = juri.MezuniyetJuriOneriFormuJuriID,
                                UniversiteAdi = juri.UniversiteAdi,
                                AnabilimdaliProgramAdi = juri.AnabilimdaliProgramAdi,
                                JuriTipAdi = juri.JuriTipAdi,
                                UnvanAdi = juri.UnvanAdi,
                                JuriAdi = juri.AdSoyad,
                                Telefon = "",
                                Email = juri.EMail,
                                IslemTarihi = DateTime.Now,
                                IslemYapanID = UserIdentity.Current.Id,
                                IslemYapanIP = UserIdentity.Ip
                            });
                        }
                        SRTalepleri srTalebi;

                        if (kModel.SRTalepID <= 0)
                        {


                            srTalebi = _entities.SRTalepleris.Add(new SRTalepleri
                            {
                                EnstituKod = kModel.EnstituKod,
                                UniqueID = Guid.NewGuid(),
                                MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID,
                                SRTalepTipID = kModel.SRTalepTipID,
                                TalepYapanID = kModel.TalepYapanID,
                                SRSalonID = kModel.SRSalonID,
                                SalonAdi = kModel.SalonAdi,
                                Tarih = kModel.Tarih,
                                HaftaGunID = kModel.HaftaGunID,
                                BasSaat = kModel.BasSaat,
                                BitSaat = kModel.BitSaat,
                                DanismanAdi = kModel.DanismanAdi,
                                EsDanismanAdi = kModel.EsDanismanAdi,
                                TezOzeti = kModel.TezOzeti,
                                Aciklama = kModel.Aciklama,
                                SRDurumID = kModel.SRDurumID,
                                IslemTarihi = kModel.IslemTarihi,
                                IslemYapanID = kModel.IslemYapanID,
                                IslemYapanIP = kModel.IslemYapanIP,
                                SRTaleplerJuris = kModel.SRTaleplerJuris

                            });
                            mezuniyetBasvurusu.MezuniyetSinavDurumID = MezuniyetSinavDurumEnum.SonucGirilmedi;

                        }
                        else
                        {
                            srTalebi = _entities.SRTalepleris.First(p => p.SRTalepID == kModel.SRTalepID);
                            srTalebi.SRTalepTipID = kModel.SRTalepTipID;
                            srTalebi.SalonAdi = kModel.SalonAdi;
                            srTalebi.TalepYapanID = kModel.TalepYapanID;
                            srTalebi.SRSalonID = kModel.SRSalonID;
                            srTalebi.Tarih = kModel.Tarih;
                            srTalebi.HaftaGunID = kModel.HaftaGunID;
                            srTalebi.BasSaat = kModel.BasSaat;
                            srTalebi.BitSaat = kModel.BitSaat;
                            srTalebi.DanismanAdi = kModel.DanismanAdi;
                            srTalebi.EsDanismanAdi = kModel.EsDanismanAdi;
                            srTalebi.TezOzeti = kModel.TezOzeti;
                            srTalebi.Aciklama = null;
                            srTalebi.SRDurumID = kModel.SRDurumID;
                            srTalebi.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                            srTalebi.IslemTarihi = kModel.IslemTarihi;
                            srTalebi.IslemYapanID = kModel.IslemYapanID;
                            srTalebi.IslemYapanIP = kModel.IslemYapanIP;
                            if (srTalebi.SRTaleplerJuris.Any(a => a.UniversiteAdi.IsNullOrWhiteSpace()))
                            {
                                _entities.SRTaleplerJuris.RemoveRange(srTalebi.SRTaleplerJuris);
                                srTalebi.SRTaleplerJuris = kModel.SRTaleplerJuris;

                            }


                        }
                        _entities.SaveChanges();

                        LogIslemleri.LogEkle("SRTalepleri", kModel.SRTalepID <= 0 ? LogCrudType.Insert : LogCrudType.Update, srTalebi.ToJson());
                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = MsgTypeEnum.Success;
                        mmMessage.Messages.Add("Belirtilen tarih için rezervasyon talebi oluşturuldu.");

                        #region SendMail
                        if (kModel.SRTalepID <= 0)
                        {
                            if (kModel.SRDurumID == SrTalepDurumEnum.Onaylandı)
                            {
                                var srtalep = _entities.SRTalepleris.Where(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).OrderByDescending(o => o.SRTalepID).First();
                                MezuniyetBus.SendMailMezuniyetSinavYerBilgisi(srtalep.SRTalepID, true);
                            }
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        mmMessage.IsSuccess = false;
                        mmMessage.MessageType = MsgTypeEnum.Error;
                        mmMessage.Messages.Add("Hata" + ": " + ex.ToExceptionMessage());
                    }

                }

            }


            return mmMessage.ToJsonResult();
        }



        public ActionResult TezTeslimFormu(int mezuniyetBasvurulariId)
        {
            var yetkiliKullanici = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();

            var mBasvuru = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId && p.KullaniciID == (yetkiliKullanici ? p.KullaniciID : UserIdentity.Current.Id));
            var mezuniyetBasvurulariTezTeslimForm = mBasvuru.MezuniyetBasvurulariTezTeslimFormlaris.FirstOrDefault();
            var model = new MezuniyetBasvurulariTezTeslimFormlari();
            if (mezuniyetBasvurulariTezTeslimForm != null)
            {
                model.MezuniyetBasvurulariID = mezuniyetBasvurulariId;
                model.MezuniyetBasvurulariTezTeslimFormID = mezuniyetBasvurulariTezTeslimForm.MezuniyetBasvurulariTezTeslimFormID;
                model.IsTezDiliTr = mBasvuru.IsTezDiliTr == true;
                model.TezDili = mezuniyetBasvurulariTezTeslimForm.TezDili;
                model.TezBaslikTr = mezuniyetBasvurulariTezTeslimForm.TezBaslikTr;
                model.TezBaslikEn = mezuniyetBasvurulariTezTeslimForm.TezBaslikEn;
                model.TezOzet = mezuniyetBasvurulariTezTeslimForm.TezOzet;
                model.TezOzetHtml = mezuniyetBasvurulariTezTeslimForm.TezOzetHtml;
                model.TezAbstract = mezuniyetBasvurulariTezTeslimForm.TezAbstract;
                model.TezAbstractHtml = mezuniyetBasvurulariTezTeslimForm.TezAbstractHtml;
            }
            else
            {
                var jof = mBasvuru.MezuniyetJuriOneriFormlaris.First();
                var srTalep = mBasvuru.SRTalepleris.FirstOrDefault(f => f.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);
                model.MezuniyetBasvurulariID = mezuniyetBasvurulariId;
                model.IsTezDiliTr = mBasvuru.IsTezDiliTr == true;
                model.TezBaslikTr = srTalep.IsTezBasligiDegisti == true ? srTalep.YeniTezBaslikTr : (jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikTr : mBasvuru.TezBaslikTr);
                model.TezBaslikEn = srTalep.IsTezBasligiDegisti == true ? srTalep.YeniTezBaslikEn : (jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikEn : mBasvuru.TezBaslikEn);
                model.TezOzet = mBasvuru.TezOzet;
                model.TezOzetHtml = mBasvuru.TezOzet;
                model.TezAbstract = mBasvuru.TezAbstract;
                model.TezAbstractHtml = mBasvuru.TezAbstract;

            }
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult TezTeslimFormuPost(MezuniyetBasvurulariTezTeslimFormlari kModel, bool? isTezDiliTr)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Teslim Formu Oluşturma İşlemi",
                MessageType = MsgTypeEnum.Warning
            };

            var yetkiliK = RoleNames.SrTalepDuzelt.InRoleCurrent();
            var mezuniyetBasvurusu = _entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
            if (mezuniyetBasvurusu.KullaniciID != UserIdentity.Current.Id && !yetkiliK)
            {
                mmMessage.Messages.Add("Başka bir kullanıcı tez teslim formu oluşturmaya yetkili değilsiniz!");
            }
            else if (mezuniyetBasvurusu.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else if (mezuniyetBasvurusu.IsMezunOldu.HasValue)
            {
                mmMessage.Messages.Add("Mezuniyet sonuç bilgisi girilildikten sonra Tez teslim formu üzerinde düzeltme işlemi yapılamaz!");

            }
            if (!isTezDiliTr.HasValue)
            {
                mmMessage.Messages.Add("Tez Dilini Seçiniz.");

                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsTezDiliTr" });
            }
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
            if (kModel.TezOzetHtml.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özetini Türkçe Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezOzetHtml" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezOzetHtml" });
            if (kModel.TezAbstractHtml.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özetini İngilizce Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezAbstractHtml" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TezAbstractHtml" });
            if (mmMessage.Messages.Count == 0)
            {

                try
                {
                    kModel.IslemTarihi = DateTime.Now;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;

                    var kKayit = _entities.MezuniyetBasvurulariTezTeslimFormlaris.FirstOrDefault(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
                    if (kKayit == null)
                    {
                        kModel.RowID = Guid.NewGuid();
                        _entities.MezuniyetBasvurulariTezTeslimFormlaris.Add(kModel);
                    }
                    else
                    {
                        if (
                           kKayit.IsTezDiliTr != kModel.IsTezDiliTr ||
                           kKayit.TezDili != kModel.TezDili ||
                           kKayit.TezBaslikTr != kModel.TezBaslikTr ||
                           kKayit.TezBaslikEn != kModel.TezBaslikEn ||
                           kKayit.TezOzet != kModel.TezOzet ||
                           kKayit.TezOzetHtml != kModel.TezOzetHtml ||
                           kKayit.TezAbstract != kModel.TezAbstract ||
                           kKayit.TezAbstractHtml != kModel.TezAbstractHtml
                          ) kKayit.RowID = Guid.NewGuid();
                        kKayit.IsTezDiliTr = kModel.IsTezDiliTr;
                        kKayit.TezDili = kModel.TezDili;
                        kKayit.TezBaslikTr = kModel.TezBaslikTr;
                        kKayit.TezBaslikEn = kModel.TezBaslikEn;
                        kKayit.TezOzet = kModel.TezOzet;
                        kKayit.TezOzetHtml = kModel.TezOzetHtml;
                        kKayit.TezAbstract = kModel.TezAbstract;
                        kKayit.TezAbstractHtml = kModel.TezAbstractHtml;
                        kKayit.IslemTarihi = kModel.IslemTarihi;
                        kKayit.IslemYapanID = kModel.IslemYapanID;
                        kKayit.IslemYapanIP = kModel.IslemYapanIP;
                    }
                    _entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add("Tez Teslim Formu Oluşturuldu.");
                    mmMessage.MessageType = MsgTypeEnum.Success;


                }
                catch (Exception ex)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.Messages.Add("Hata: </br> " + ex.ToExceptionMessage());
                }



            }


            return mmMessage.ToJsonResult();
        }

        [HttpGet]
        public ActionResult AddRow()
        {
            return View();
        }


        [HttpPost]
        public ActionResult TezDosyaEklePost(Guid rowId, int? mezuniyetBasvurulariTezDosyaId, HttpPostedFileBase belgeDosyasi)
        {
            var mMessage = new MmMessage
            {
                Title = "Tez Dosyası Yükleme İşlemi",
                MessageType = MsgTypeEnum.Warning
            };

            var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var mezuniyetBasvurusu = _entities.MezuniyetBasvurularis.First(p => p.RowID == rowId);
            var tezDosyasi = mezuniyetBasvurusu.MezuniyetBasvurulariTezDosyalaris.FirstOrDefault(p => p.MezuniyetBasvurulariTezDosyaID == mezuniyetBasvurulariTezDosyaId);

            if (mezuniyetBasvurusu.MezuniyetSinavDurumID != MezuniyetSinavDurumEnum.Basarili)
            {
                mMessage.Messages.Add("Tez dosyasını yükleyebilmek için Sınav sürecinden başarılı bir şekilde geçmeniz gerekmektedir.!");
            }
            else if (mezuniyetBasvurusu.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else if (mezuniyetBasvurusu.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true))
            {
                mMessage.Messages.Add("Onaylanmış bir tez dosyanız bulunmaktadır. Yeni tez dosyası yüklenemez!");
            }
            else if (!kayitYetki && mezuniyetBasvurusu.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Bu başvuru üstünde işlem yapmaya yetkili değilsiniz.");
            }
            else if (belgeDosyasi != null && belgeDosyasi.ContentLength > (1024 * 1024 * 20))
            {
                mMessage.Messages.Add("Yükleyeceğiniz dosya boyutu en fazla 20MB olmalıdır.");
            }
            else if (belgeDosyasi != null && belgeDosyasi.FileName.Length > 1024)
            {
                mMessage.Messages.Add("Yükleyeceğiniz dosya adı en fazla 1024 karakter uzunluğunda olmalıdır.");
            }
            else
            {
                if (tezDosyasi != null && tezDosyasi.IsOnaylandiOrDuzeltme.HasValue)
                {
                    mMessage.Messages.Add("Tez dosyası işlem gördüğünden belge yükleme işlemi yapamazsınız.");
                }
                else if (belgeDosyasi != null && belgeDosyasi.FileName.Split('.').Last().ToLower() != "pdf")
                {
                    mMessage.Messages.Add("Yükleyeceğiniz belge 'PDF' türünde olmalıdır.");
                }
            }
            if (mMessage.Messages.Count == 0)
            {



                var dosyaYolu = "/BasvuruDosyalari/MezuniyetBelgeleri/" + belgeDosyasi.FileName.ToFileNameAddGuid(null, mezuniyetBasvurusu.MezuniyetBasvurulariID.ToString());
                var belgeAdi = belgeDosyasi.FileName.GetFileName();
                belgeDosyasi.SaveAs(Server.MapPath("~" + dosyaYolu));

                if (tezDosyasi == null)
                {
                    tezDosyasi = _entities.MezuniyetBasvurulariTezDosyalaris.Add(new MezuniyetBasvurulariTezDosyalari
                    {
                        MezuniyetBasvurulariID = mezuniyetBasvurusu.MezuniyetBasvurulariID,
                        RowID = Guid.NewGuid(),
                        SiraNo = mezuniyetBasvurusu.MezuniyetBasvurulariTezDosyalaris.Count + 1,
                        YuklemeTarihi = DateTime.Now,
                        TezDosyaAdi = belgeAdi,
                        TezDosyaYolu = dosyaYolu,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                    });
                }
                else
                {

                    var path = Server.MapPath("~" + tezDosyasi.TezDosyaYolu);

                    if (System.IO.File.Exists(path))
                    {
                        try
                        {
                            System.IO.File.Delete(path);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    tezDosyasi.RowID = Guid.NewGuid();
                    tezDosyasi.TezDosyaAdi = belgeAdi;
                    tezDosyasi.TezDosyaYolu = dosyaYolu;
                    tezDosyasi.YuklemeTarihi = DateTime.Now;
                    tezDosyasi.IslemTarihi = DateTime.Now;
                    tezDosyasi.IslemYapanID = UserIdentity.Current.Id;
                    tezDosyasi.IslemYapanIP = UserIdentity.Ip;
                }

                _entities.SaveChanges();

                MezuniyetBus.TezDosyasiKontrolYetkilisiAta(mezuniyetBasvurusu.MezuniyetBasvurulariID);
                MezuniyetBus.SendMailMezuniyetTezSablonKontrol(tezDosyasi.MezuniyetBasvurulariTezDosyaID, MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendi);
                mMessage.Messages.Add("Tez Dosyası Yükleme İşlemi Başarılı");


                mMessage.IsSuccess = true;
                mMessage.MessageType = MsgTypeEnum.Success;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return new { mMessage.IsSuccess, Messages = strView }.ToJsonResult();
        }

        [AllowAnonymous]
        public ActionResult SinavDegerlendir(Guid? uniqueId, bool? isTezBasligiDegisti, string yeniTezBaslikTr, string yeniTezBaslikEn, bool? isTezSanayiVeIsBirligiKapsamindaGerceklesti, bool? isYokDrBursiyeriVar, string yokDrOncelikliAlan, int? mezuniyetSinavDurumId, string aciklama)
        {


            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Sınavı Değerlendirme İşlemi"
            };
            var degerlendirmeDuzeltmeYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            bool isRefresh = false;
            if (!uniqueId.HasValue)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme için gerekli benzersiz anahtar bilgisi boş gelmektedir.</span>");
            }
            else
            {
                var komite = _entities.SRTaleplerJuris.FirstOrDefault(p => p.UniqueID == uniqueId);
                if (komite == null)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme işlemi yapmanız için size tanınan benzersiz anahtar bilgisi değişti veya bulunamadı!</span>");
                }
                else
                {
                    var srTalebi = komite.SRTalepleri;
                    var mezuniyetBasvurusu = srTalebi.MezuniyetBasvurulari;
                    var srTalepJuris = srTalebi.SRTaleplerJuris;
                    bool isTezDanismani = komite.JuriTipAdi == "TezDanismani";
                    var toplanti = komite.SRTalepleri;
                    var toplantiTarihi = toplanti.Tarih.Add(toplanti.BasSaat);
                    if (mezuniyetBasvurusu.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
                    {
                        mMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
                    }
                    else if (!degerlendirmeDuzeltmeYetki && DateTime.Now < toplantiTarihi)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Sınav değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + toplantiTarihi.ToLongDateString() + " " + $"{toplanti.BasSaat:hh\\:mm}" + "'</b> dan önce yapılamaz!</span>");
                    }
                    else if (!degerlendirmeDuzeltmeYetki && komite.MezuniyetSinavDurumID > MezuniyetSinavDurumEnum.SonucGirilmedi)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Sınav değerlendirme işlemini daha önceden zaten yaptınız!</span>");
                    }
                    else if (mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Uzatma &&
                             mezuniyetBasvurusu.SRTalepleris.Any(a =>
                                 a.SRTalepID < srTalebi.SRTalepID &&
                                 a.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma))
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Öğrenci daha önceden Uzatma aldığı için tekrar Uzatma işlemi yapılamaz!</span>");
                    }
                    else
                    {
                        if (!degerlendirmeDuzeltmeYetki)
                        {
                            if (isTezDanismani)
                            {
                                if (!isTezBasligiDegisti.HasValue)
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Sınavda tez başlığı değişti mi?</span>");
                                }
                                else if (isTezBasligiDegisti == true)
                                {
                                    if (yeniTezBaslikTr.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Yeni tez başlığı türkçe bilgisi girilmeli</span>");
                                    }
                                    if (yeniTezBaslikEn.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Yeni tez başlığı ingilizce bilgisi girilmeli</span>");
                                    }
                                }
                                if (!isTezSanayiVeIsBirligiKapsamindaGerceklesti.HasValue)
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Tez sanayi ile işbirliği kapsamında mı gerçekleştirildi?</span>");
                                }
                                if (mezuniyetBasvurusu.OgrenimTipKod.IsDoktora())
                                {
                                    if (!isYokDrBursiyeriVar.HasValue)
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>100/2000 YÖK bursiyeri var mı?</span>");
                                    }
                                    if (isYokDrBursiyeriVar == true && yokDrOncelikliAlan.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Öncelikli alt alan Adı</span>");
                                    }
                                }
                            }
                            if (!mezuniyetSinavDurumId.HasValue || mezuniyetSinavDurumId <= MezuniyetSinavDurumEnum.SonucGirilmedi)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez sınavı değerlendirme sonucu</span>");
                            }
                            else if (mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Basarisiz && aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez sınavı değerlendirme açıklaması</span>");
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez sınavı değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");
                        }
                        else
                        {
                            if (isTezDanismani)
                            {

                                if (mezuniyetSinavDurumId > MezuniyetSinavDurumEnum.SonucGirilmedi)
                                {


                                    if (!isTezBasligiDegisti.HasValue)
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez başlığı değişti mi?</span>");
                                    }
                                    else if (isTezBasligiDegisti == true)
                                    {
                                        if (yeniTezBaslikTr.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Yeni tez başlığı türkçe bilgisi girilmeli</span>");
                                        }
                                        if (yeniTezBaslikEn.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Yeni tez başlığı ingilizce bilgisi girilmeli</span>");
                                        }
                                    }



                                    if (!isTezSanayiVeIsBirligiKapsamindaGerceklesti.HasValue)
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez sanayi ile işbirliği kapsamında mı gerçekleştirildi?</span>");

                                    if (mezuniyetSinavDurumId <= MezuniyetSinavDurumEnum.SonucGirilmedi)
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez sınavı değerlendirme sonucu</span>");



                                    if (mezuniyetBasvurusu.OgrenimTipKod.IsDoktora())
                                    {
                                        if (!isYokDrBursiyeriVar.HasValue)
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>100/2000 YÖK bursiyeri var mı?</span>");
                                        }
                                        if (isYokDrBursiyeriVar == true && yokDrOncelikliAlan.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Öncelikli alt alan adı</span>");
                                        }
                                    }


                                }

                            }
                            if (mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Basarisiz && aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez sınavı değerlendirme açıklaması</span>");
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez sınavı değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");

                        }
                    }
                    if (!mMessage.Messages.Any())
                    {
                        var degerlendirmeler = new List<SRTaleplerJuri>();
                        foreach (var item in srTalepJuris)
                        {
                            degerlendirmeler.Add(new SRTaleplerJuri
                            {
                                UniqueID = item.UniqueID,
                                MezuniyetSinavDurumID = item.MezuniyetSinavDurumID
                            });
                        }
                        var degerlendirme = degerlendirmeler.First(p => p.UniqueID == uniqueId.Value);
                        degerlendirme.MezuniyetSinavDurumID = mezuniyetSinavDurumId;
                        if (!degerlendirmeler.Any(a => !a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi))
                        {
                            var qGroup = degerlendirmeler.GroupBy(g => new { g.MezuniyetSinavDurumID }).Select(s => new
                            {
                                s.Key.MezuniyetSinavDurumID,
                                Count = s.Count()
                            }).OrderByDescending(o => o.Count).ToList();
                            if (qGroup.Count != 1)
                            {
                                var enYuksekOy1 = qGroup[0];
                                var enYuksekOy2 = qGroup[1];

                                if (enYuksekOy1.Count == enYuksekOy2.Count)
                                {
                                    var degerlendirmeAdlari = _entities.MezuniyetSinavDurumlaris.ToList();
                                    var degerlendirmeSonuclari = (from s in qGroup
                                                                  join da in degerlendirmeAdlari on s.MezuniyetSinavDurumID equals da.MezuniyetSinavDurumID
                                                                  select new
                                                                  {
                                                                      s.MezuniyetSinavDurumID,
                                                                      da.MezuniyetSinavDurumAdi,
                                                                      Count = s.Count
                                                                  }).ToList();
                                    foreach (var item in degerlendirmeSonuclari)
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>" + item.MezuniyetSinavDurumAdi + " : " + item.Count + "</span>");
                                    }
                                    if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Yaptığınız değerlendirme sonucunda oy birliği sağlanamamaktadır. Bu durumu öğrenci danışmanı ile görüşüp tekrar değerlendirme yapınız. Oy dağılımı aşağıdaki gibi sonuçlanmakta.");

                                }
                            }
                        }

                    }

                    if (!mMessage.Messages.Any())
                    {
                        var sendMailLink = false;
                        var sendSonuc = false;
                        var jForm = mezuniyetBasvurusu.MezuniyetJuriOneriFormlaris.First();
                        if (isTezDanismani && mezuniyetSinavDurumId > MezuniyetSinavDurumEnum.SonucGirilmedi && !komite.SRTalepleri.SRTaleplerJuris.Any(a => a.IsLinkGonderildi.HasValue))
                        {
                            sendMailLink = true;
                        }
                        if (isTezDanismani)
                        {
                            if (jForm.IsTezSanayiVeIsBirligiKapsamindaGerceklesti != isTezSanayiVeIsBirligiKapsamindaGerceklesti) sendSonuc = true;
                            if (!sendSonuc)
                            {

                                if (isTezBasligiDegisti != srTalebi.IsTezBasligiDegisti) sendSonuc = true;
                            }
                            if (!sendSonuc)
                            {
                                if (mezuniyetSinavDurumId != komite.MezuniyetSinavDurumID || aciklama != komite.Aciklama) sendSonuc = true;
                            }
                        }
                        else
                        {
                            if (!sendSonuc)
                            {
                                if (mezuniyetSinavDurumId != komite.MezuniyetSinavDurumID || aciklama != komite.Aciklama) sendSonuc = true;
                            }
                        }

                        if (isTezDanismani)
                        {
                            srTalebi.IsTezBasligiDegisti = isTezBasligiDegisti;

                            if (srTalebi.IsTezBasligiDegisti == true)
                            {
                                srTalebi.YeniTezBaslikTr = srTalebi.IsTezBasligiDegisti == true ? yeniTezBaslikTr : null;
                                srTalebi.YeniTezBaslikEn = srTalebi.IsTezBasligiDegisti == true ? yeniTezBaslikEn : null;
                            }

                            jForm.IsTezSanayiVeIsBirligiKapsamindaGerceklesti = isTezSanayiVeIsBirligiKapsamindaGerceklesti;
                            srTalebi.IsYokDrBursiyeriVar = isYokDrBursiyeriVar;
                            srTalebi.YokDrOncelikliAlan = yokDrOncelikliAlan;
                        }
                        komite.MezuniyetSinavDurumID = mezuniyetSinavDurumId;
                        komite.Aciklama = aciklama;
                        komite.DegerlendirmeIslemTarihi = DateTime.Now;
                        komite.DegerlendirmeIslemYapanIP = UserIdentity.Ip;
                        komite.DegerlendirmeYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;

                        komite.IslemTarihi = DateTime.Now;
                        komite.IslemYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;
                        komite.IslemYapanIP = UserIdentity.Ip;
                        _entities.SaveChanges();
                        LogIslemleri.LogEkle("SRTalepleriJuri", LogCrudType.Update, komite.ToJson());
                        mMessage.IsSuccess = true;
                        if (sendMailLink)
                        {
                            var messages = MezuniyetBus.SendMailMezuniyetDegerlendirmeLink(komite.SRTalepID, null, true);
                            if (isTezDanismani || degerlendirmeDuzeltmeYetki)
                            {
                                if (messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme linki jüri üyelerine gönderildi.");
                                }
                                else
                                {
                                    mMessage.Messages.AddRange(messages.Messages);
                                    mMessage.Messages.Add("Değerlendirmeniz geri alınmıştır, Lütfen tekrar değerlendirme yapınız.");
                                    mMessage.IsSuccess = false;
                                    isRefresh = true;
                                    komite.MezuniyetSinavDurumID = null;
                                    komite.Aciklama = null;
                                    komite.DegerlendirmeIslemTarihi = null;
                                    komite.DegerlendirmeIslemYapanIP = null;
                                    komite.DegerlendirmeYapanID = null;
                                    _entities.SaveChanges();
                                }
                            }
                        }
                        else mMessage.Messages.Add("Değerlendirme kaydedildi.");


                        var isDegerlendirmeTamam = !komite.SRTalepleri.SRTaleplerJuris.Any(a => !a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi);
                        srTalebi = komite.SRTalepleri;
                        srTalepJuris = srTalebi.SRTaleplerJuris;
                        if (isDegerlendirmeTamam)
                        {

                            var qGroup = srTalepJuris.GroupBy(g => new { g.MezuniyetSinavDurumID }).Select(s => new
                            {
                                s.Key.MezuniyetSinavDurumID,
                                Count = s.Count()
                            }).OrderByDescending(o => o.Count).ToList();

                            srTalebi.JuriSonucMezuniyetSinavDurumID = qGroup.First().MezuniyetSinavDurumID;
                            srTalebi.IsOyBirligiOrCoklugu = qGroup.Count == 1;
                            _entities.SaveChanges();
                            if (sendSonuc)
                            {
                                var messages = MezuniyetBus.SendMailMezuniyetDegerlendirmeLink(srTalebi.SRTalepID, null, false);
                                messages.IsSuccess = true;

                                if (isTezDanismani || degerlendirmeDuzeltmeYetki)
                                {
                                    if (messages.IsSuccess)
                                    {
                                        mMessage.Messages.Add("Değerlendirme sonucu danışman ve öğrenciye gönderildi.");

                                    }
                                    else
                                    {
                                        mMessage.Messages.AddRange(messages.Messages);
                                        mMessage.IsSuccess = false;
                                    }
                                }
                                if (messages.IsSuccess)
                                {
                                    srTalebi.DegerlendirmeSonucMailTarihi = DateTime.Now;
                                }
                                _entities.SaveChanges();
                            }
                        }
                        else
                        {
                            srTalebi.IsOyBirligiOrCoklugu = null;
                            srTalebi.JuriSonucMezuniyetSinavDurumID = MezuniyetSinavDurumEnum.SonucGirilmedi;
                            if (srTalepJuris.Any(a => a.MezuniyetSinavDurumID.HasValue && a.MezuniyetSinavDurumID > MezuniyetSinavDurumEnum.SonucGirilmedi))
                            {
                                srTalebi.RSBaslatildiMailGonderimTarihi = DateTime.Now;
                            }
                            else
                            {
                                srTalebi.RSBaslatildiMailGonderimTarihi = null;
                            }
                            _entities.SaveChanges();
                        }
                        LogIslemleri.LogEkle("SRTalepJuris", LogCrudType.Update, komite.ToJson());

                    }
                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { mMessage.IsSuccess, Messages = strView, IsRefresh = isRefresh }, "application/json", JsonRequestBehavior.AllowGet);
        }


        [AllowAnonymous]
        public ActionResult GSinavDegerlendir(Guid? uniqueId)
        {

            var model = (from s in _entities.SRTalepleris.Where(p => p.SRTaleplerJuris.Any(a2 => a2.UniqueID == uniqueId.Value))
                         join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                         join mb in _entities.MezuniyetBasvurularis on s.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                         join sal in _entities.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                         from defSl in def1.DefaultIfEmpty()
                         join hg in _entities.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                         join d in _entities.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                         join sd in _entities.MezuniyetSinavDurumlaris on (s.MezuniyetSinavDurumID ?? MezuniyetSinavDurumEnum.SonucGirilmedi) equals sd.MezuniyetSinavDurumID into def2
                         from defSd in def2.DefaultIfEmpty()
                         join sdj in _entities.MezuniyetSinavDurumlaris on (s.JuriSonucMezuniyetSinavDurumID ?? MezuniyetSinavDurumEnum.SonucGirilmedi) equals sdj.MezuniyetSinavDurumID into def3
                         from defsdj in def3.DefaultIfEmpty()
                         let jof = _entities.MezuniyetJuriOneriFormlaris.FirstOrDefault(p => p.MezuniyetBasvurulariID == mb.MezuniyetBasvurulariID)
                         select new FrTalepler
                         {
                             MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                             SRTalepID = s.SRTalepID,
                             TalepYapanID = s.TalepYapanID,
                             TalepTipAdi = tt.TalepTipAdi,
                             SRTalepTipID = s.SRTalepTipID,
                             SRSalonID = s.SRSalonID,
                             SalonAdi = s.SRSalonID.HasValue ? defSl.SalonAdi : s.SalonAdi,
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
                             IsTezDiliTr = mb.IsTezDiliTr == true,
                             TezBaslikTr = jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikTr : mb.TezBaslikTr,
                             TezBaslikEn = jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikEn : mb.TezBaslikEn,
                             IsTezBasligiDegisti = s.IsTezBasligiDegisti,
                             YeniTezBaslikTr = s.YeniTezBaslikTr,
                             YeniTezBaslikEn = s.YeniTezBaslikEn,
                             IsYokDrBursiyeriVar = s.IsYokDrBursiyeriVar,
                             YokDrOncelikliAlan = s.YokDrOncelikliAlan,
                             Aciklama = s.Aciklama,
                             SRTaleplerJuris = s.SRTaleplerJuris.Where(p => p.UniqueID == uniqueId.Value).ToList(),
                             IsSonSrTalebi = !mb.SRTalepleris.Any(a => a.SRTalepID > s.SRTalepID),
                         }).Where(p => p.IsSonSrTalebi).OrderByDescending(o => o.SRTalepID).FirstOrDefault();

            if (model != null)
            {

                var sRjuri = _entities.SRTaleplerJuris.First(f => f.UniqueID == uniqueId);
                var basvuru = sRjuri.SRTalepleri.MezuniyetBasvurulari;
                var juriOneriFormu = basvuru.MezuniyetJuriOneriFormlaris.FirstOrDefault();
                model.IsOncedenUzatmaAlindi =
                    basvuru.SRTalepleris.Any(a =>
                        a.SRTalepID <= model.SRTalepID && a.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma);
                model.ResimAdi = basvuru.Kullanicilar.ResimAdi;
                var ogtrenimTip = _entities.OgrenimTipleris.First(p => p.OgrenimTipKod == basvuru.OgrenimTipKod);
                ViewBag.OgtrenimTipi = ogtrenimTip;
                ViewBag.MezuniyetBasvurulari = basvuru;
                ViewBag.JuriOneriFormu = juriOneriFormu;
                ViewBag.UniqueID = uniqueId;
            }

            return View(model);
        }

        [Authorize]
        public ActionResult SinavJuriBilgiGuncelle(int srTalepId, Guid? uniqueId, string unvanAdi, string juriAdi, string eMail)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                MessageType = MsgTypeEnum.Warning,
                Title = "Tez sınavı jüri bilgisi güncelleme işlemi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
            var sinavDuzeltmeYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var uzatmaSonrasiOgrenciTaahhutu = srTalep.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma && srTalep.IsOgrenciUzatmaSonrasiOnay.HasValue;
            var uye = srTalep.SRTaleplerJuris.FirstOrDefault(p => p.UniqueID == uniqueId);

            if (!sinavDuzeltmeYetki)
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Jüri bilgisini güncellemek için yetkili değilsiniz.");
            }
            else if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else if (uzatmaSonrasiOgrenciTaahhutu)
            {
                mMessage.Messages.Add("Öğrenci uzatma işleminden sonra tez teslim taahhütü yaptığı için Jüri bilgisi güncellenemez.");
            }
            else if (srTalep.MezuniyetSinavDurumID > MezuniyetSinavDurumEnum.SonucGirilmedi)
            {
                mMessage.Messages.Add("Değerlendirme işlemi tüm Jüri üyeler tarafından tamamlandığı için Jüri bilgisi güncellenemez.");
            }
            else if (unvanAdi.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("Jüri ünvanını seçiniz.");
            }
            else if (juriAdi.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("Jüri adı giriniz.");
            }
            else if (eMail.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("E-Posta giriniz.");
            }
            else if (eMail.ToIsValidEmail())
            {
                mMessage.Messages.Add("E-Posta formatı uygun değildir.");
            }
            else
            {
                if (uniqueId.HasValue)
                {
                    if (uye == null) mMessage.Messages.Add("Değerlendirme linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı yenileyip tekrar deneyiniz.");
                    else
                    {
                        uye.UnvanAdi = unvanAdi;
                        uye.JuriAdi = juriAdi;
                        uye.Email = eMail;
                        _entities.SaveChanges();
                        mMessage.IsSuccess = true;
                        mMessage.Messages.Add("Jüri bilgileri güncellendi.");

                    }
                }
            }
            var strView = mMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage) : "";
            return new { mMessage, MessageView = strView, MessageType = (mMessage.IsSuccess ? "success" : "error") }.ToJsonResult();
        }
        [Authorize]
        public ActionResult SinavJuriyeMailGonder(int srTalepId, Guid? uniqueId, string eMail)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                MessageType = MsgTypeEnum.Warning,
                Title = "Tez sınavı değerlendirme linki gönderme işlemi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
            var basvuru = srTalep.MezuniyetBasvurulari;
            var sinavDuzeltmeYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var uzatmaSonrasiOgrenciTaahhutu = srTalep.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma && srTalep.IsOgrenciUzatmaSonrasiOnay.HasValue;
            var uye = srTalep.SRTaleplerJuris.FirstOrDefault(p => p.UniqueID == uniqueId);

            if (!sinavDuzeltmeYetki && basvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Değerlendirme linkini göndermek için yetkili değilsiniz.");
            }
            else if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else if (uzatmaSonrasiOgrenciTaahhutu)
            {
                mMessage.Messages.Add("Öğrenci uzatma işleminden sonra tez teslim taahhütü yaptığı için jüri üyesine değerlendirme linki gönderilemez.");
            }
            else if (srTalep.MezuniyetSinavDurumID > MezuniyetSinavDurumEnum.SonucGirilmedi)
            {
                mMessage.Messages.Add("Değerlendirme işlemi tüm Jüri üyeler tarafından tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
            }
            else if (eMail.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("E-Posta giriniz.");
            }
            else if (eMail.ToIsValidEmail())
            {
                mMessage.Messages.Add("E-Posta formatı uygun değildir.");
            }
            else
            {
                if (uniqueId.HasValue)
                {
                    if (uye == null) mMessage.Messages.Add("Değerlendirme linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı yenileyip tekrar deneyiniz.");
                    else
                    {
                        uye.Email = eMail;
                        _entities.SaveChanges();
                        var messages = MezuniyetBus.SendMailMezuniyetDegerlendirmeLink(srTalep.SRTalepID, uniqueId, true, true, eMail);
                        if (messages.IsSuccess)
                        {
                            srTalep.JuriSonucMezuniyetSinavDurumID = null;
                            _entities.SaveChanges();
                            mMessage.IsSuccess = true;
                            mMessage.MessageType = MsgTypeEnum.Success;
                            mMessage.Messages.Add("Değerlendirme linki jüri üyesine gönderildi.");
                        }
                        else
                        {
                            mMessage.Messages.AddRange(messages.Messages);
                        }
                    }
                }


            }
            var strView = mMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage) : "";
            return new { mMessage, MessageView = strView, MessageType = (mMessage.IsSuccess ? "success" : "error") }.ToJsonResult();
        }
        [Authorize]
        public ActionResult DegerlendirmeLinkView(Guid? uniqueId)
        {
            var model = _entities.SRTaleplerJuris.First(p => p.UniqueID == uniqueId);
            return View(model);
        }
        [Authorize]
        public ActionResult DegerlendirmeJuriView(Guid? uniqueId)
        {
            var model = _entities.SRTaleplerJuris.First(p => p.UniqueID == uniqueId);
            return View(model);
        }
        public ActionResult OgrenciUzatmaOnayKayit(int srTalepId, bool? isOgrenciUzatmaSonrasiOnay)
        {
            var mmMessage = new MmMessage
            {
                Title = "Tez sınavı öğrenci tez teslim taahhütü"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
            var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRole();
            var onayTarihi = DateTime.Now;
            if (!kayitYetki)
            {
                if (srTalep.MezuniyetBasvurulari.KullaniciID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz!");
                }
            }
            else if (srTalep.IsDanismanUzatmaSonrasiOnay.HasValue)
            {
                mmMessage.Messages.Add("Danışman tarafından taahhüt onayı yapıldı. Bu işlemi yapamazsınız.");
            }
            else if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else
            {
                if (srTalep.MezuniyetSinavDurumID != MezuniyetSinavDurumEnum.Uzatma)
                {
                    mmMessage.Messages.Add("Sadece uzatma alınan sınavlar için tez teslim taahhütü yapılabilir!");
                }
                else
                {
                    var mezuniyetSureciOgrenimTip = srTalep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == srTalep.MezuniyetBasvurulari.OgrenimTipKod);
                    var uzatmaSonrasiTezTeslimSonTarih = srTalep.UzatmaSonrasiOgrenciTaahhutSonTarih ?? srTalep.Tarih.AddDays(mezuniyetSureciOgrenimTip.SinavUzatmaOgrenciTaahhutMaxGun);
                    if (onayTarihi > uzatmaSonrasiTezTeslimSonTarih)
                    {
                        mmMessage.Messages.Add("Mezuniyet sınavı sonucunda almış olduğunuz uzatma işlemi sonrası tez teslim taahhütü işemi için son tarihi olan '" + uzatmaSonrasiTezTeslimSonTarih.ToFormatDate() + "' tarihini aştığınız için taahhüt onay işlemi yapamazsınız.");
                    }
                }
            }

            if (!mmMessage.Messages.Any())
            {
                if (mmMessage.Messages.Count == 0)
                {
                    srTalep.IsOgrenciUzatmaSonrasiOnay = isOgrenciUzatmaSonrasiOnay;
                    srTalep.OgrenciOnayTarihi = onayTarihi;

                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("SRTalepleri", LogCrudType.Update, srTalep.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(isOgrenciUzatmaSonrasiOnay.HasValue ? (isOgrenciUzatmaSonrasiOnay.Value ? "Tahhüt onaylandı." : "Taahhüt ret edildi.") : "Taahhüt işlemi geril alındı.");
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }


        public ActionResult Sil(int id)
        {
            var mmMessage = MezuniyetBus.MezuniyetBasvurusuSilKontrol(id);

            if (mmMessage.IsSuccess)
            {
                var kayit = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);

                try
                {
                    var fFList = new List<string>();
                    foreach (var item in kayit.MezuniyetBasvurulariYayins)
                    {
                        if (item.MezuniyetYayinBelgeDosyaYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinBelgeDosyaYolu);
                        if (item.MezuniyetYayinMetniBelgeYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinMetniBelgeYolu);
                        if (item.MezuniyetYayinKabulEdilmisMakaleDosyaYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinKabulEdilmisMakaleDosyaYolu);
                    }
                    mmMessage.Title = "Uyarı";
                    if (UserIdentity.Current.IsAdmin && kayit.IsMezunOldu != true)
                    {
                        _entities.MezuniyetBasvurulariTezDosyalaris.RemoveRange(kayit.MezuniyetBasvurulariTezDosyalaris);
                        _entities.MezuniyetJuriOneriFormlaris.RemoveRange(kayit.MezuniyetJuriOneriFormlaris);
                        _entities.SRTalepleris.RemoveRange(kayit.SRTalepleris);
                    }

                    _entities.MezuniyetBasvurularis.Remove(kayit);
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Delete, kayit.ToJson());
                    if (kayit.DanismanImzaliFormDosyaYolu.IsNullOrWhiteSpace() == false)
                    {
                        var path = Server.MapPath("~" + kayit.DanismanImzaliFormDosyaYolu);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }
                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    foreach (var item in fFList)
                    {
                        var path = Server.MapPath("~" + item);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(kayit.BasvuruTarihi + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}