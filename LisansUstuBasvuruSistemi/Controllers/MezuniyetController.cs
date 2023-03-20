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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
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
                if (!model.KullaniciID.HasValue || !RoleNames.KullaniciAdinaTezIzlemeBasvurusuYap.InRoleCurrent()) model.KullaniciID = UserIdentity.Current.Id;
            }

            #region bilgiModel
            var bbModel = new IndexPageInfoDto();
            var mezuniyetSurecId = MezuniyetBus.GetMezuniyetAktifSurecId(enstituKod);
            bbModel.AktifSurecID = mezuniyetSurecId ?? 0;
            bbModel.SistemBasvuruyaAcik = MezuniyetAyar.MezuniyetBasvurusuAcikmi.GetAyarMz(enstituKod, "0").ToBoolean().Value && mezuniyetSurecId.HasValue;
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
                bbModel.KullaniciTipYetki = kullanici.OgrenimDurumID == OgrenimDurum.HalenOğrenci;

                if (kullanici.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                {
                    var kullKayitB = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(kullanici.KullaniciID);
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
                        bbModel.KullaniciTipYetkiYokMsj = "Öğrenim Bilginiz Doğrulanamdı. Profil bilgilerinizde giriş yaptığınız YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen";
                    }
                    else bbModel.KayitDonemi = kullanici.KayitYilBaslangic + "/" + (kullanici.KayitYilBaslangic + 1) + " " + _entities.Donemlers.First(p => p.DonemID == kullanici.KayitDonemID.Value).DonemAdi + " , " + kullanici.KayitTarihi.ToString("dd.MM.yyyy");

                }

            }
            else
            {
                bbModel.KullaniciTipYetki = false;
                bbModel.KullaniciTipYetkiYokMsj = "Profil bilgilerinizde YTÜ Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için mezuniyet başvurusu yapamazsınız. Sağ üst köşeden profil bilgilerini düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
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
                        KullaniciTipAdi = s.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci ? "" : ktip.KullaniciTipAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        IsMezunOldu = s.IsMezunOldu,
                        MezuniyetTarihi = s.MezuniyetTarihi,
                        SrTalebi = srT,
                        SRDurumID = srT.SRDurumID,
                        TeslimFormDurumu = srT != null && srT.SRTalepleriBezCiltFormus.Any(),
                        IsOnaylandiOrDuzeltme = td != null ? td.IsOnaylandiOrDuzeltme : null,
                        MezuniyetBasvurulariTezDosyasi = td,
                        UzatmaSuresiGun = mOt.MBSinavUzatmaSuresiGun,
                        MezuniyetSuresiGun = mOt.MBSinavUzatmaSuresiGun,
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



        public ActionResult BasvuruYap(int? mezuniyetBasvurulariId, int? kullaniciId = null, string enstituKod = "", string ekd = "")
        {
            var model = new kmMezuniyetBasvuru
            {
                EnstituKod = enstituKod.IsNullOrWhiteSpace() ? EnstituBus.GetSelectedEnstitu(ekd) : enstituKod
            };


            if (mezuniyetBasvurulariId.HasValue || kullaniciId.HasValue)
            {
                if (kullaniciId.HasValue)
                    if (RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() == false)
                        kullaniciId = UserIdentity.Current.Id;
                if (mezuniyetBasvurulariId.HasValue)
                {
                    var basvuru = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId.Value);
                    model.EnstituKod = enstituKod = basvuru.MezuniyetSureci.EnstituKod;
                    if (kullaniciId.HasValue == false) kullaniciId = basvuru.KullaniciID;
                }
            }
            else
            {
                kullaniciId = UserIdentity.Current.Id;
            }

            var mmMessage = MezuniyetBus.MezuniyetSurecAktifKontrol(model.EnstituKod, kullaniciId, mezuniyetBasvurulariId);
            var studentInfo = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(kullaniciId.Value);
            var kul = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kullaniciId);

            var danismanBilgi = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kul.DanismanID);
            if (!mezuniyetBasvurulariId.HasValue && mmMessage.IsSuccess)
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

                    //Tez bilgisi gelmiyor ise Tez durumu ile alakalı olabilir. Tez durumu devam ediyor olmalı. Eğer değilse Ya yeni tez eklenecek yada gsis te tez guncellemeden tez durumunu devam ediyor yapılmalı.
                }
                //else if (DanismanBilgi.KullaniciID == -1)
                //{
                //    _MmMessage.Messages.Add("Tez danışmanı bilginiz çekilemedi sisteminden alınamadı.  Obs sisteminde tez durumunuzun devam ediyor olması ve danışmanınızın tanımlı olması gerekmektedir. Başvuru yapabilmeniz için bu durumu enstitü yetkililerine bildiriniz.");
                //    _MmMessage.IsSuccess = false;
                //}
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
                    kullaniciId = model.KullaniciID;

                }
                else
                {

                    model.MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(model.EnstituKod).Value;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = kullaniciId.Value;
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

                ViewBag.MezuniyetYayinTurID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecYayinTurleri(model.MezuniyetSurecID, model.KullaniciID, true), "Value", "Caption");

                ViewBag._MmMessage = mmMessage;
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
                return RedirectToAction("Index", new { KullaniciID = kullaniciId });
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
        public ActionResult BasvuruYap(kmMezuniyetBasvuru kModel)
        {
            var stps = new List<int>();


            if (RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() == false) { kModel.KullaniciID = UserIdentity.Current.Id; }
            var mmMessage = MezuniyetBus.MezuniyetSurecAktifKontrol(kModel.EnstituKod, kModel.KullaniciID, kModel.MezuniyetBasvurulariID.ToNullIntZero());
            if (kModel.MezuniyetBasvurulariID <= 0)
            {
                kModel.MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(kModel.EnstituKod) ?? 0;
                kModel.BasvuruTarihi = DateTime.Now;
            }
            else
            {
                var btarih = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
                kModel.BasvuruTarihi = btarih.BasvuruTarihi;
            }
            var bsurec = _entities.MezuniyetSurecis.First(p => p.MezuniyetSurecID == kModel.MezuniyetSurecID);
            kModel.EnstituKod = bsurec.EnstituKod;
            kModel.DonemAdi = bsurec.BaslangicYil + "/" + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;

            var studentInfo = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(kModel.KullaniciID);
            var kul = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kModel.KullaniciID);
            kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;
            #region Kontrol
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
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinKontrolDurumID" });
                }
                else if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi)
                {
                    var yaynK = MezuniyetBus.YayinKontrol(kModel);
                    mmMessage.Messages.AddRange(yaynK.Messages.ToList());
                    mmMessage.MessagesDialog.AddRange(yaynK.MessagesDialog.ToList());
                    if (mmMessage.Messages.Count > 0) stps.Add(2);
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
            bool sendMail = false;
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IsYerli = kul.KullaniciTipleri.Yerli;
                kModel.ResimAdi = kul.ResimAdi;
                kModel.KullaniciTipAdi = _entities.KullaniciTipleris.First(p => p.KullaniciTipID == kul.KullaniciTipID).KullaniciTipAdi;
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

                MezuniyetBasvurulari mBasvuru;
                bool isNewRecord = false;
                if (kModel.MezuniyetBasvurulariID <= 0)
                {
                    isNewRecord = true;
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
                    if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi) sendMail = true;




                }
                else
                {

                    mBasvuru = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
                    if (!mBasvuru.TezDanismanID.HasValue || mBasvuru.TezDanismanID <= 0) mBasvuru.TezDanismanID = kul.DanismanID;
                    if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi && kModel.MezuniyetYayinKontrolDurumID != mBasvuru.MezuniyetYayinKontrolDurumID && MezuniyetAyar.GetAyarMz(MezuniyetAyar.YeniMezuniyetBasvurusundaMailGonder, kModel.EnstituKod).ToBoolean() == true)
                    {
                        mBasvuru.BasvuruTarihi = DateTime.Now;
                        sendMail = true;
                    }
                    if (mBasvuru.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumu.KabulEdildi)
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
                    mBasvuru.OgrenciNo = kModel.OgrenciNo;
                    mBasvuru.OgrenimDurumID = kModel.OgrenimDurumID;
                    mBasvuru.OgrenimTipKod = kModel.OgrenimTipKod;
                    mBasvuru.ProgramKod = kModel.ProgramKod;
                    mBasvuru.KayitOgretimYiliBaslangic = kModel.KayitOgretimYiliBaslangic;
                    mBasvuru.KayitOgretimYiliDonemID = kModel.KayitOgretimYiliDonemID;
                    mBasvuru.KayitTarihi = kModel.KayitTarihi;
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

                LogIslemleri.LogEkle("MezuniyetBasvurulari", isNewRecord ? IslemTipi.Insert : IslemTipi.Update, mBasvuru.ToJson());


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
                    var enstitu = mBasvuru.MezuniyetSureci.Enstituler;
                    var sablonlar = _entities.MailSablonlaris.Where(p => p.EnstituKod == enstitu.EnstituKod).ToList();


                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            AdSoyad = mBasvuru.Ad + " " + mBasvuru.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = mBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Mez_BasvuruYapildiOgrenci,
                        }
                    };


                    var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == kul.DanismanID);
                    mModel.Add(new SablonMailModel
                    {

                        AdSoyad = danisman.Ad + " " + danisman.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                        MailSablonTipID = MailSablonTipi.Mez_BasvuruYapildiDanisman,
                    });

                    foreach (var item in mModel)
                    {
                        var enstituL = mBasvuru.MezuniyetSureci.Enstituler;

                        item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipID);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = mBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = mBasvuru.Ad + " " + mBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = mBasvuru.TezDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = mBasvuru.TezDanismanUnvani });


                        var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, null);
                        if (snded)
                        {
                            var gm = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")",
                                IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id,
                                IslemYapanIP = UserIdentity.Ip,
                                Aciklama = item.Sablon.Sablon ?? "",
                                AciklamaHtml = mCOntent.HtmlContent ?? "",
                                Gonderildi = true,
                                GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList()
                            };
                            _entities.GonderilenMaillers.Add(gm);
                            _entities.SaveChanges();
                        }
                    }


                }
                if (kModel.KullaniciID != UserIdentity.Current.Id) return RedirectToAction("Index", "MezuniyetGelenBasvurular");
                else return RedirectToAction("Index", kModel.KullaniciID);
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

            if (stps.Count > 0) kModel.SetSelectedStep = stps.First();
            ViewBag._MmMessage = mmMessage;
            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurum(true), "Value", "Caption", kModel.MezuniyetYayinKontrolDurumID);
            ViewBag.TezEsDanismanUnvani = new SelectList(UnvanlarBus.GetCmbJuriUnvanlar(true), "Value", "Caption", kModel.TezEsDanismanUnvani);
            ViewBag.MezuniyetYayinTurID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecYayinTurleri(kModel.MezuniyetSurecID, kModel.KullaniciID, true), "Value", "Caption");


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

        public ActionResult YayinEklemeKontrol(kmMezuniyetBasvuru model)
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
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Yayinlanmis" });
                    if (model.YayinBilgisi.YayinBasligi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Yayın Başlığı Bilgisini Giriniz");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YayinBasligi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YayinBasligi" });
                    if (yayinBilgi.YayinYazarlarIstensin && model.YayinBilgisi.YazarAdi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Yazarları giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YazarAdi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YazarAdi" });
                    if (yayinBilgi.YayinProjeTurIstensin && !model.YayinBilgisi.MezuniyetYayinProjeTurID.HasValue)
                    {
                        mmMessage.Messages.Add("Proje Türü seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinProjeTurID" });
                    }
                    else
                    {
                        if (model.YayinBilgisi.MezuniyetYayinProjeTurID == 3)
                        {
                            if (yayinBilgi.YayinProjeTurIstensin && model.YayinBilgisi.ProjeDeatKurulus.IsNullOrWhiteSpace())
                            {
                                mmMessage.Messages.Add("Proje Dest. Kuruluş giriniz.");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProjeDeatKurulus" });
                            }
                            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProjeDeatKurulus" });
                        }
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinProjeTurID" });
                    }
                    if (yayinBilgi.YayinProjeEkibiIstensin && model.YayinBilgisi.ProjeEkibi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Proje Ekibi giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProjeEkibi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProjeEkibi" });
                    if (yayinBilgi.IsTarihAraligiIstensin && model.YayinBilgisi.TarihAraligi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Tarih Aralığı giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TarihAraligi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TarihAraligi" });
                    if (yayinBilgi.YayinMevcutDurumIstensin && !model.YayinBilgisi.IsProjeTamamlandiOrDevamEdiyor.HasValue)
                    {
                        mmMessage.Messages.Add("Proje Mevcut Durum seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsProjeTamamlandiOrDevamEdiyor" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsProjeTamamlandiOrDevamEdiyor" });


                    if (yayinBilgi.YayinDergiAdiIstensin && model.YayinBilgisi.DergiAdi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Dergi Adı giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DergiAdi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DergiAdi" });
                    if (yayinBilgi.YayinYilCiltSayiIstensin && model.YayinBilgisi.YilCiltSayiSS.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Yıl/Cilt/Sayı/ss giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YilCiltSayiSS" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YilCiltSayiSS" });
                    if (yayinBilgi.YayinEtkinlikAdiIstensin && model.YayinBilgisi.EtkinlikAdi.IsNullOrWhiteSpace())
                    {

                        mmMessage.Messages.Add("Etkinlik Adı giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EtkinlikAdi" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EtkinlikAdi" });
                    if (yayinBilgi.YayinYerBilgisiIstensin && model.YayinBilgisi.YerBilgisi.IsNullOrWhiteSpace())
                    {

                        mmMessage.Messages.Add("Yer Bilgisi giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YerBilgisi" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EtkinlikAdi" });

                    if (yayinBilgi.MezuniyetYayinTarihZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == true) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinTarih.HasValue == false)
                        {
                            mmMessage.Messages.Add("Bildiri Tarihi");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTarih" });
                        }
                        else if (model.YayinBilgisi.MezuniyetYayinTarih.Value > DateTime.Now)
                        {
                            mmMessage.Messages.Add("Bildiri Tarihi Bu günkü tarihten büyük olamaz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTarih" });
                        }

                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinTarih" });
                    }

                    if (yayinBilgi.MezuniyetYayinBelgeTurZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == false) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinBelgeAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinBelgeTurAdi + " Belgesini Yükleyiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinBelgeAdi" });
                        }
                        else if (model.YayinBilgisi.MezuniyetYayinBelgeAdi.Split('.').Last().ToLower() != "pdf")
                        {
                            mmMessage.Messages.Add(model.YayinBilgisi.MezuniyetYayinBelgeAdi + " Belgesini Pdf Türünde Olmalı");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinBelgeAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinBelgeAdi" });
                    }
                    if (yayinBilgi.MezuniyetYayinKaynakLinkTurZorunlu)
                    {
                        if (model.YayinBilgisi.MezuniyetYayinKaynakLinki.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinKaynakLinkTurAdi + " Bilgisini Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinKaynakLinki" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinKaynakLinki" });
                    }
                    if (yayinBilgi.MezuniyetYayinMetinZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == true) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinMetniBelgeAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinMetinTurAdi + " Belgesini Yükleyiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinMetniBelgeAdi" });
                        }
                        else if (model.YayinBilgisi.MezuniyetYayinMetniBelgeAdi.Split('.').Last().ToLower() != "pdf")
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinMetinTurAdi + " Belgesi PDF Türünde Olmalıdır");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinMetniBelgeAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinMetniBelgeAdi" });
                    }
                    if (yayinBilgi.MezuniyetYayinLinkiZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == true) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinLinki.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add(yayinBilgi.MezuniyetYayinLinkTurAdi + " Bilgisini Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinLinki" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinLinki" });
                    }
                    if (yayinBilgi.MezuniyetYayinIndexTurZorunlu)
                    {
                        if (model.YayinBilgisi.MezuniyetYayinIndexTurID.HasValue == false)
                        {

                            mmMessage.Messages.Add("Index Türü Seçiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinIndexTurID" });
                        }
                        else
                        {
                            var inxB = _entities.MezuniyetYayinIndexTurleris.First(p => p.MezuniyetYayinIndexTurID == model.YayinBilgisi.MezuniyetYayinIndexTurID.Value);
                            yayinBilgi.MezuniyetYayinIndexTurID = model.YayinBilgisi.MezuniyetYayinIndexTurID;
                            yayinBilgi.MezuniyetYayinIndexTurAdi = inxB.IndexTurAdi;
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinIndexTurID" });
                        }
                    }
                    if (yayinBilgi.MezuniyetKabulEdilmisMakaleZorunlu && ((yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() && yayinBilgi.Yayinlanmis == false) || yayinBilgi.MezuniyetYayinTurID.IsMakaleYayinDurumIsteniyor() == false))
                    {
                        if (model.YayinBilgisi.MezuniyetYayinKabulEdilmisMakaleAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Kabul Edilmiş Makale Yükleyiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinKabulEdilmisMakaleAdi" });
                        }
                        else if (model.YayinBilgisi.MezuniyetYayinKabulEdilmisMakaleAdi.Split('.').Last().ToLower() != "pdf")
                        {
                            mmMessage.Messages.Add("Kabul Edilmiş Makale PDF Türünde Olmalıdır");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinKabulEdilmisMakaleAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MezuniyetYayinKabulEdilmisMakaleAdi" });
                    }
                }
                else
                {
                    mmMessage.Messages.Add("Yayın Durumunu Seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Yayinlanmis" });
                }
            }
            mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
            string row = "";
            if (mmMessage.IsSuccess)
            {
                row = ViewRenderHelper.RenderPartialView("Mezuniyet", "AddRow", yayinBilgi);
            }
            mmMessage.Title = "Yayın Bilgisi Ekleme İşlemi";
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
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
            var model = new kmSRTalep
            {
                IsSalonSecilsin = mezuniyetBasvuru.OgrenimTipKod.IsDoktora() && mezuniyetBasvuru.MezuniyetSureci.EnstituKod == EnstituKodlari.FenBilimleri
            };
            if (srTalepId > 0)
            {
                var srTalebi = mezuniyetBasvuru.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                var tarih = model.IsSalonSecilsin ? srTalebi.Tarih : (srTalebi.Tarih.AddHours(srTalebi.BasSaat.Hours).AddMinutes(srTalebi.BasSaat.Minutes));
                model.MzRowID = mezuniyetBasvuru.RowID.ToString();
                model.SRTalepID = srTalebi.SRTalepID;
                model.SRTalepTipID = srTalebi.SRTalepTipID;
                model.TalepYapanID = srTalebi.TalepYapanID;
                model.SRSalonID = srTalebi.SRSalonID;
                model.SalonAdi = srTalebi.SalonAdi;
                model.Tarih = tarih;


            }
            else
            {
                model.MzRowID = mezuniyetBasvuru.RowID.ToString();
                model.SRTalepTipID = 1;
                model.TalepYapanID = mezuniyetBasvuru.KullaniciID;
                var ogrenimTipKriterleri = mezuniyetBasvuru.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == mezuniyetBasvuru.OgrenimTipKod);
                if (mezuniyetBasvuru.EYKTarihi != null)
                    model.Tarih = mezuniyetBasvuru.EYKTarihi.Value.AddDays(ogrenimTipKriterleri.MBSRTalebiKacGunSonraAlabilir);
            }
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(mezuniyetBasvuru.MezuniyetSureci.EnstituKod, model.SRTalepTipID, true), "Value", "Caption", model.SRSalonID);
            return View(model);
        }
        [HttpPost]
        public ActionResult RezervasyonAlPost(kmSRTalep kModel)
        {

            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;
            mmMessage.Title = "Salon Rezervasyonu Talep İşlemi";
            mmMessage.MessageType = Msgtype.Warning;
            var surecKayitYetki = RoleNames.MezuniyetSureciKayıt.InRole();

            var mezuniyetBasvurusu = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
            var sonSrTalebi = mezuniyetBasvurusu.SRTalepleris.LastOrDefault();
            var srTalebiYetkisi = mezuniyetBasvurusu.KullaniciID == UserIdentity.Current.Id || RoleNames.MezuniyetGelenBasvurularSrTalebiYap.InRoleCurrent();


            kModel.SRTalepTipID = 1;
            kModel.IsSalonSecilsin = mezuniyetBasvurusu.OgrenimTipKod.IsDoktora() && mezuniyetBasvurusu.MezuniyetSureci.EnstituKod != EnstituKodlari.SosyalBilimleri;
            kModel.EnstituKod = mezuniyetBasvurusu.MezuniyetSureci.EnstituKod;
            if (!srTalebiYetkisi)
            {
                var msg = "Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!";
                mmMessage.Messages.Add(msg);
                SistemBilgilendirmeBus.SistemBilgisiKaydet(msg + "\r\n İşlem yapılmak istenen KullanıcıID:" + kModel.TalepYapanID + "\r\n İşlemYapanID:" + UserIdentity.Current.Id, "Mezuniyet/RezervasyonAlPost", LogType.Saldırı);
            }
            if (sonSrTalebi != null && sonSrTalebi.SRDurumID == SRTalepDurum.Onaylandı && !sonSrTalebi.MezuniyetSinavDurumID.HasValue)
            {
                mmMessage.Messages.Add("Son Sınav bilgisi Enstitü Tarafından Onaylanmadan yeni rezervasyon alınamaz.");

            }
            if (kModel.SRTalepID > 0)
            {
                var srTalep = mezuniyetBasvurusu.SRTalepleris.First(p => p.SRTalepID == kModel.SRTalepID);
                var srDurum = srTalep.SRDurumlari;
                kModel.SRDurumID = srTalep.SRDurumID;
                if (srTalep.SRDurumID != SRTalepDurum.TalepEdildi && !(srTalep.MezuniyetSinavDurumID == SRTalepDurum.Onaylandı && UserIdentity.Current.IsAdmin))
                {
                    mmMessage.Messages.Add("Salon rezervasyonu " + srDurum.DurumAdi + " olduğundan düzeltme işlemi yapılamaz.");
                }
            }
            if (kModel.MezuniyetSinavDurumID.HasValue && mezuniyetBasvurusu.MezuniyetSinavDurumID != MezuniyetSinavDurum.SonucGirilmedi)
            {
                mmMessage.Messages.Add("Sınav sonuç bilgisi girilen rezervasyonlar üzerinde düzeltme işlemi yapılamaz!");

            }

            if (mezuniyetBasvurusu.SRTalepleris.Any(a => (a.SRDurumID == SRSalonDurum.Alındı || a.SRDurumID == SRSalonDurum.OnTalep) && a.MezuniyetSinavDurumID != MezuniyetSinavDurum.Uzatma && a.SRTalepID != kModel.SRTalepID))
            {
                mmMessage.Messages.Add("Aktif bir salon rezervasyonu kaydınız bulunmaktadır. Tekrar rezervasyon işlemi yapamazsınız.");
            }
            var mezuniyetSureciOgrenimTip = mezuniyetBasvurusu.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == mezuniyetBasvurusu.OgrenimTipKod);

            if (mmMessage.Messages.Count == 0)
            {
                if (mezuniyetBasvurusu.EYKTarihi != null)
                {
                    var srBaslangicTarihi = mezuniyetBasvurusu.EYKTarihi.Value.AddDays(mezuniyetSureciOgrenimTip.MBSRTalebiKacGunSonraAlabilir);
                    if (kModel.IsSalonSecilsin)
                    {
                        if (kModel.SRSalonID <= 0 || !kModel.SRSalonID.HasValue)
                        {
                            mmMessage.Messages.Add("Salon seçimi yapınız!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRSalonID" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "SRSalonID" });
                        if (kModel.Tarih == DateTime.MinValue)
                        {
                            mmMessage.Messages.Add("Talep tarihi seçimi yapınız!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                        }
                        else if (kModel.Tarih.Date < srBaslangicTarihi.Date)
                        {
                            mmMessage.Messages.Add("Talep tarihi " + srBaslangicTarihi.Date.ToString("yyyy-MM-dd") + " tarihinden küçük olamaz!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                        }
                        if (!kModel.BasSaat.HasValue || !kModel.BitSaat.HasValue)//bitiş saati mi baz alınsın başlangıç saati mi ?
                        {
                            mmMessage.Messages.Add("Lütfen belirtilen güne ait uygun saat seçiniz!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                        }
                    }
                    else
                    {
                        if (kModel.SalonAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Salon Adı Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SalonAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "SalonAdi" });
                        if (kModel.Tarih == DateTime.MinValue)
                        {
                            mmMessage.Messages.Add("Talep tarihi seçimi yapınız!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                        }
                        else if (kModel.Tarih.Date < srBaslangicTarihi.Date)
                        {
                            mmMessage.Messages.Add("Talep tarihi " + srBaslangicTarihi.Date.ToString("yyyy-MM-dd") + " tarihinden küçük olamaz!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                        }
                    }
                }

                if (mmMessage.Messages.Count == 0 && !surecKayitYetki)
                {
                    var uzatmaAlinanSrTalebi = mezuniyetBasvurusu.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma && p.SRDurumID == SRTalepDurum.Onaylandı).OrderByDescending(o => o.SRTalepID).FirstOrDefault();
                    if (uzatmaAlinanSrTalebi != null)
                    {
                        var uzatmaSonSrAlmaTarihi = uzatmaAlinanSrTalebi.Tarih.AddDays(mezuniyetSureciOgrenimTip.MBSinavUzatmaSuresiGun);
                        if (kModel.Tarih > uzatmaSonSrAlmaTarihi)
                        {
                            string msg = "Mezuniyet sınavı sonucunda almış olduğunuz uzatma işlemi sonrası salon rezervasyonu işemi son tarihi olan '" + uzatmaSonSrAlmaTarihi.ToFormatDate() + "' tarihini aştığınız için salon rezervasyonu alamazsınız.";
                            mmMessage.Messages.Add(msg);
                        }
                    }
                }

                if (mmMessage.Messages.Count == 0 && kModel.IsSalonSecilsin)
                {
                    var ssts = new List<SROzelTanimSaatler> { new SROzelTanimSaatler { BasSaat = kModel.BasSaat.Value, BitSaat = kModel.BitSaat.Value } };
                    var msg = SrTalepleriBus.SrKayitKontrol(kModel.SRSalonID.Value, kModel.SRTalepTipID, kModel.Tarih, ssts, kModel.SRTalepID, null, null, null, mezuniyetBasvurusu.EYKTarihi);
                    mmMessage.Messages.AddRange(msg.Messages);
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
                        kModel.BasSaat = kModel.IsSalonSecilsin ? kModel.BasSaat.Value : kModel.Tarih.TimeOfDay;
                        kModel.BitSaat = kModel.IsSalonSecilsin ? kModel.BitSaat.Value : kModel.BasSaat.Value.Add(new TimeSpan(2, 0, 0));
                        kModel.Tarih = kModel.Tarih.Date;


                        kModel.DanismanAdi = mezuniyetBasvurusu.TezDanismanAdi;
                        kModel.EsDanismanAdi = mezuniyetBasvurusu.TezEsDanismanAdi;
                        kModel.TezOzeti = mezuniyetBasvurusu.TezOzet;
                        if (kModel.SRTalepID <= 0)
                        {
                            kModel.SRDurumID = SRTalepDurum.TalepEdildi;
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
                                BasSaat = kModel.BasSaat.Value,
                                BitSaat = kModel.BitSaat.Value,
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
                            mezuniyetBasvurusu.MezuniyetSinavDurumID = MezuniyetSinavDurum.SonucGirilmedi;

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
                            srTalebi.BasSaat = kModel.BasSaat.Value;
                            srTalebi.BitSaat = kModel.BitSaat.Value;
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

                        LogIslemleri.LogEkle("SRTalepleri", kModel.SRTalepID <= 0 ? IslemTipi.Insert : IslemTipi.Update, srTalebi.ToJson());
                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = Msgtype.Success;
                        mmMessage.Messages.Add("Belirtilen tarih için rezervasyon talebi oluşturuldu.");

                        #region SendMail
                        if (kModel.SRTalepID <= 0)
                        {
                            if (kModel.SRDurumID == SRTalepDurum.Onaylandı)
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
                        mmMessage.MessageType = Msgtype.Error;
                        mmMessage.Messages.Add("Hata" + ": " + ex.ToExceptionMessage());
                    }

                }

            }


            return mmMessage.ToJsonResult();
        }



        public ActionResult BezCiltForm(int srTalepId, int srTalepleriBezCiltFormId)
        {
            var yetkiliKullanici = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();

            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == srTalepId && p.TalepYapanID == (yetkiliKullanici ? p.TalepYapanID : UserIdentity.Current.Id));
            var mBasvuru = srTalep.MezuniyetBasvurulari;
            var model = new SRTalepleriBezCiltFormu();
            if (srTalepleriBezCiltFormId > 0)
            {
                var data = srTalep.SRTalepleriBezCiltFormus.First(p => p.SRTalepleriBezCiltFormID == srTalepleriBezCiltFormId);
                //var Jof= SrTalep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.FirstOrDefault();
                // if (Jof != null)
                // {

                // }
                model.SRTalepleriBezCiltFormID = data.SRTalepleriBezCiltFormID;
                model.SRTalepID = data.SRTalepID;
                model.IsTezDiliTr = mBasvuru.IsTezDiliTr == true;
                model.TezDili = data.TezDili;
                model.TezBaslikTr = data.TezBaslikTr;
                model.TezBaslikEn = data.TezBaslikEn;
                model.TezOzet = data.TezOzet;
                model.TezOzetHtml = data.TezOzetHtml;
                model.TezAbstract = data.TezAbstract;
                model.TezAbstractHtml = data.TezAbstractHtml;
            }
            else
            {
                model.IsTezDiliTr = mBasvuru.IsTezDiliTr == true;
                model.TezBaslikTr = srTalep.MezuniyetBasvurulari.TezBaslikTr;
                model.TezBaslikEn = srTalep.MezuniyetBasvurulari.TezBaslikEn;
                model.TezOzet = srTalep.MezuniyetBasvurulari.TezOzet;
                model.TezOzetHtml = srTalep.MezuniyetBasvurulari.TezOzetHtml;
                model.TezAbstract = srTalep.MezuniyetBasvurulari.TezAbstract;
                model.TezAbstractHtml = srTalep.MezuniyetBasvurulari.TezAbstractHtml;

            }
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BezCiltFormPost(SRTalepleriBezCiltFormu kModel, bool? isTezDiliTr)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Teslim Formu Oluşturma İşlemi",
                MessageType = Msgtype.Warning
            };

            var yetkiliK = RoleNames.SrTalepDuzelt.InRoleCurrent();

            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == kModel.SRTalepID);

            var mzTalep = srTalep.MezuniyetBasvurulari;


            if (srTalep.TalepYapanID != UserIdentity.Current.Id && !yetkiliK)
            {
                string msg = "Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!";
                mmMessage.Messages.Add(msg);
                SistemBilgilendirmeBus.SistemBilgisiKaydet(msg + "\r\n İşlem yapılmak istenen KullanıcıID:" + srTalep.TalepYapanID + "\r\n İşlemYapanID:" + UserIdentity.Current.Id, "Mezuniyet/RezervasyonAlPost", LogType.Saldırı);
            }
            else if (mzTalep.IsMezunOldu.HasValue)
            {
                mmMessage.Messages.Add("Mezuniyet sonuç bilgisi girilildikten sonra Tez teslim formu üzerinde düzeltme işlemi yapılamaz!");

            }
            if (!isTezDiliTr.HasValue)
            {
                mmMessage.Messages.Add("Tez Dilini Seçiniz.");

                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsTezDiliTr" });
            }
            if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Başlığını Türkçe Olarak Giriniz.");

                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezBaslikTr" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezBaslikTr" });

            if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Başlığını İngilizce Olarak Giriniz.");

                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezBaslikEn" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezBaslikEn" });
            if (kModel.TezOzetHtml.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özetini Türkçe Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezOzetHtml" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezOzetHtml" });
            if (kModel.TezAbstractHtml.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özetini İngilizce Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezAbstractHtml" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezAbstractHtml" });
            if (mmMessage.Messages.Count == 0)
            {

                try
                {
                    kModel.IslemTarihi = DateTime.Now;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;

                    if (kModel.SRTalepleriBezCiltFormID <= 0)
                    {
                        _entities.SRTalepleriBezCiltFormus.Add(new SRTalepleriBezCiltFormu
                        {
                            SRTalepID = kModel.SRTalepID,
                            RowID = Guid.NewGuid(),
                            IsTezDiliTr = kModel.IsTezDiliTr,
                            TezDili = kModel.TezDili,
                            TezBaslikTr = kModel.TezBaslikTr,
                            TezBaslikEn = kModel.TezBaslikEn,
                            TezOzet = kModel.TezOzet,
                            TezOzetHtml = kModel.TezOzetHtml,
                            TezAbstract = kModel.TezAbstract,
                            TezAbstractHtml = kModel.TezAbstractHtml,
                            IslemTarihi = kModel.IslemTarihi,
                            IslemYapanID = kModel.IslemYapanID,
                            IslemYapanIP = kModel.IslemYapanIP,

                        });
                    }
                    else
                    {
                        var kKayit = _entities.SRTalepleriBezCiltFormus.First(p => p.SRTalepID == kModel.SRTalepID && p.SRTalepleriBezCiltFormID == kModel.SRTalepleriBezCiltFormID);
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
                    mmMessage.MessageType = Msgtype.Success;


                }
                catch (Exception ex)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = Msgtype.Error;
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
                MessageType = Msgtype.Warning
            };

            var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var basv = _entities.MezuniyetBasvurularis.First(p => p.RowID == rowId);
            var tezDosyasi = basv.MezuniyetBasvurulariTezDosyalaris.FirstOrDefault(p => p.MezuniyetBasvurulariTezDosyaID == mezuniyetBasvurulariTezDosyaId);

            if (basv.MezuniyetSinavDurumID != MezuniyetSinavDurum.Basarili)
            {
                mMessage.Messages.Add("Tez dosyasını yükleyebilmek için Sınav sürecinden başarılı bir şekilde geçmeniz gerekmektedir.!");
            }
            else if (basv.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true))
            {
                mMessage.Messages.Add("Onaylanmış bir tez dosyanız bulunmaktadır. Yeni tez dosyası yüklenemez!");
            }
            else if (!kayitYetki && basv.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Bu başvuru üstünde işlem yapmaya yetkili değilsiniz.");
            }
            else if (belgeDosyasi != null && belgeDosyasi.ContentLength > (1024 * 1024 * 20))
            {
                mMessage.Messages.Add("Yükleyeceğiniz dosya boyutu en fazla 20MB olmalıdır.");
            }
            else
            {
                if (tezDosyasi != null && tezDosyasi.IsOnaylandiOrDuzeltme.HasValue)
                {
                    mMessage.Messages.Add("Tez dosyası işlem gördüğünden belge yükleme işlemi yapamazsınız.");
                }
                else if (belgeDosyasi == null && tezDosyasi == null)
                {
                    mMessage.Messages.Add("Tez dosyasını yüklemek için dosya seçiniz.");
                }
                else if (belgeDosyasi != null && belgeDosyasi.FileName.Split('.').Last().ToLower() != "pdf")
                {
                    mMessage.Messages.Add("Yükleyeceğiniz belge 'PDF' türünde olmalıdır.");
                }
            }
            if (mMessage.Messages.Count == 0)
            {



                string dosyaYolu = "/BasvuruDosyalari/MezuniyetBelgeleri/" + belgeDosyasi.FileName.ToFileNameAddGuid(null, basv.MezuniyetBasvurulariID.ToString());
                string belgeAdi = belgeDosyasi.FileName.GetFileName();
                belgeDosyasi.SaveAs(Server.MapPath("~" + dosyaYolu));

                if (tezDosyasi == null)
                {
                    tezDosyasi = _entities.MezuniyetBasvurulariTezDosyalaris.Add(new MezuniyetBasvurulariTezDosyalari
                    {
                        MezuniyetBasvurulariID = basv.MezuniyetBasvurulariID,
                        RowID = Guid.NewGuid(),
                        SiraNo = basv.MezuniyetBasvurulariTezDosyalaris.Count + 1,
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
                MezuniyetBus.SendMailMezuniyetTezSablonKontrol(tezDosyasi.MezuniyetBasvurulariTezDosyaID, MailSablonTipi.Mez_TezKontrolTezDosyasiYuklendi);
                mMessage.Messages.Add("Tez Dosyası Yükleme İşlemi Başarılı");


                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
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
                    var mBasvuru = srTalebi.MezuniyetBasvurulari;
                    var srTalepJuris = srTalebi.SRTaleplerJuris;
                    bool isTezDanismani = komite.JuriTipAdi == "TezDanismani";
                    var toplanti = komite.SRTalepleri;
                    var toplantiTarihi = toplanti.Tarih.Add(toplanti.BasSaat);
                    if (!degerlendirmeDuzeltmeYetki && DateTime.Now < toplantiTarihi)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + toplantiTarihi.ToLongDateString() + " " + $"{toplanti.BasSaat:hh\\:mm}" + "'</b> dan önce yapılamaz!</span>");
                    }
                    else if (!degerlendirmeDuzeltmeYetki && komite.MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemini daha önceden zaten yaptınız!</span>");
                    }
                    else
                    {
                        if (!degerlendirmeDuzeltmeYetki)
                        {
                            if (isTezDanismani)
                            {
                                if (!isTezBasligiDegisti.HasValue)
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Sınavda Tez Başlığı Değişti mi?</span>");
                                }
                                else if (isTezBasligiDegisti == true)
                                {
                                    if (yeniTezBaslikTr.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı bilgisi girilmeli</span>");
                                    }
                                    if (yeniTezBaslikEn.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı Çevirisi bilgisi girilmeli</span>");
                                    }
                                }
                                if (!isTezSanayiVeIsBirligiKapsamindaGerceklesti.HasValue)
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Tez Sanayi ile işbirliği kapsamında mı gerçekleştirildi?</span>");
                                }
                                if (mBasvuru.OgrenimTipKod.IsDoktora())
                                {
                                    if (!isYokDrBursiyeriVar.HasValue)
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>100/2000 YÖK Bursiyeri Var Mı?</span>");
                                    }
                                    if (isYokDrBursiyeriVar == true && yokDrOncelikliAlan.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Öncelikli Alt Alan Adı</span>");
                                    }
                                }
                            }
                            if (!mezuniyetSinavDurumId.HasValue || mezuniyetSinavDurumId <= MezuniyetSinavDurum.SonucGirilmedi)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez Sınavı Değerlendirme Sonucu</span>");
                            }
                            else if (mezuniyetSinavDurumId == MezuniyetSinavDurum.Basarisiz && aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez sınavı Değerlendirme Açıklaması</span>");
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez sınavı değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");
                        }
                        else
                        {
                            if (isTezDanismani)
                            {

                                if (mezuniyetSinavDurumId > MezuniyetSinavDurum.SonucGirilmedi)
                                {


                                    if (!isTezBasligiDegisti.HasValue)
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez Başlığı Değişti Mi?</span>");
                                    }
                                    else if (isTezBasligiDegisti == true)
                                    {
                                        if (yeniTezBaslikTr.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı bilgisi girilmeli</span>");
                                        }
                                        if (yeniTezBaslikEn.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı Çevirisi bilgisi girilmeli</span>");
                                        }
                                    }



                                    if (!isTezSanayiVeIsBirligiKapsamindaGerceklesti.HasValue)
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez Sanayi ile işbirliği kapsamında mı gerçekleştirildi?</span>");

                                    if (!mezuniyetSinavDurumId.HasValue || mezuniyetSinavDurumId <= MezuniyetSinavDurum.SonucGirilmedi)
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez Sınavı Değerlendirme Sonucu</span>");



                                    if (mBasvuru.OgrenimTipKod.IsDoktora())
                                    {
                                        if (!isYokDrBursiyeriVar.HasValue)
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>100/2000 YÖK Bursiyeri Var Mı?</span>");
                                        }
                                        if (isYokDrBursiyeriVar == true && yokDrOncelikliAlan.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Öncelikli Alt Alan Adı</span>");
                                        }
                                    }


                                }

                            }
                            if (mezuniyetSinavDurumId == MezuniyetSinavDurum.Basarisiz && aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez sınavı Değerlendirme Açıklaması</span>");
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
                        if (!degerlendirmeler.Any(a => !a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurum.SonucGirilmedi))
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
                        var juri = _entities.MezuniyetJuriOneriFormuJurileris.First(p => p.MezuniyetJuriOneriFormuJuriID == komite.MezuniyetJuriOneriFormuJuriID);
                        var jForm = juri.MezuniyetJuriOneriFormlari;
                        if (isTezDanismani && mezuniyetSinavDurumId > MezuniyetSinavDurum.SonucGirilmedi && !komite.SRTalepleri.SRTaleplerJuris.Any(a => a.IsLinkGonderildi.HasValue))
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
                        LogIslemleri.LogEkle("SRTalepleriJuri", IslemTipi.Update, komite.ToJson());
                        mMessage.IsSuccess = true;
                        if (sendMailLink)
                        {
                            var messages = MezuniyetBus.SendMailMezuniyetDegerlendirmeLink(komite.SRTalepID, null, true);
                            if (isTezDanismani || degerlendirmeDuzeltmeYetki)
                            {
                                if (messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme Linki Jüri Üyelerine Gönderildi.");
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


                        var isDegerlendirmeTamam = !komite.SRTalepleri.SRTaleplerJuris.Any(a => !a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurum.SonucGirilmedi);
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
                                    srTalebi.DegerlendirmeSonucMailTarihi = DateTime.Now;
                                }
                                _entities.SaveChanges();
                            }
                        }
                        else
                        {
                            srTalebi.IsOyBirligiOrCoklugu = null;
                            srTalebi.JuriSonucMezuniyetSinavDurumID = MezuniyetSinavDurum.SonucGirilmedi;
                            if (srTalepJuris.Any(a => a.MezuniyetSinavDurumID.HasValue && a.MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi))
                            {
                                srTalebi.RSBaslatildiMailGonderimTarihi = DateTime.Now;
                            }
                            else
                            {
                                srTalebi.RSBaslatildiMailGonderimTarihi = null;
                            }
                            _entities.SaveChanges();
                        }
                        LogIslemleri.LogEkle("SRTalepJuris", IslemTipi.Update, komite.ToJson());

                    }
                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
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
                         join sd in _entities.MezuniyetSinavDurumlaris on (s.MezuniyetSinavDurumID ?? MezuniyetSinavDurum.SonucGirilmedi) equals sd.MezuniyetSinavDurumID into def2
                         from defSd in def2.DefaultIfEmpty()
                         join sdj in _entities.MezuniyetSinavDurumlaris on (s.JuriSonucMezuniyetSinavDurumID ?? MezuniyetSinavDurum.SonucGirilmedi) equals sdj.MezuniyetSinavDurumID into def3
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
                             IsSonSRTalebi = !mb.SRTalepleris.Any(a => a.SRTalepID > s.SRTalepID),
                             SRTalepleriBezCiltFormus = s.SRTalepleriBezCiltFormus,
                         }).Where(p => p.IsSonSRTalebi).OrderByDescending(o => o.SRTalepID).First();
            var sinavJuri = model.SRTaleplerJuris.First();
            var juriOneriFormuJuri = _entities.MezuniyetJuriOneriFormuJurileris.First(p => p.MezuniyetJuriOneriFormuJuriID == sinavJuri.MezuniyetJuriOneriFormuJuriID);
            var juriOneriFormu = juriOneriFormuJuri.MezuniyetJuriOneriFormlari;
            var basvuru = juriOneriFormu.MezuniyetBasvurulari;
            model.IsOncedenUzatmaAlindi =
                basvuru.SRTalepleris.Any(a => a.SRTalepID <= model.SRTalepID && a.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma);
            model.ResimAdi = basvuru.Kullanicilar.ResimAdi;
            var ogtrenimTip = _entities.OgrenimTipleris.First(p => p.OgrenimTipKod == basvuru.OgrenimTipKod);
            ViewBag.OgtrenimTipi = ogtrenimTip;
            ViewBag.MezuniyetBasvurulari = basvuru;
            ViewBag.JuriOneriFormu = juriOneriFormu;
            ViewBag.UniqueID = uniqueId;
            return View(model);
        }

        [Authorize]
        public ActionResult DegerlendirmeLinkiGonder(int srTalepId, Guid? uniqueId, string eMail, bool isJuriEmailGuncellensin, bool isYeniLink)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Sınavı Değerlendirme Linki Gönderme İşlemi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
            var basvuru = srTalep.MezuniyetBasvurulari;
            var sinavDuzeltmeYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var uzatmaSonrasiOgrenciTaahhutu = srTalep.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma && srTalep.IsOgrenciUzatmaSonrasiOnay.HasValue;
            var uye = srTalep.SRTaleplerJuris.FirstOrDefault(p => p.UniqueID == uniqueId);

            if (!sinavDuzeltmeYetki && basvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Değerlendirme Linki Göndermek İçin Yetkili Değilsiniz.");
            }
            else if (uzatmaSonrasiOgrenciTaahhutu)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Öğrenci Uzatma işleminden sonra Tez Teslim Taahhütü yaptığı için Jüri Üyesine değerlendirme linki gönderilemez");
            }
            else if (srTalep.MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Değerlendirme işlemi tüm Jüri üyeler tarafından tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
            }
            else if (eMail.IsNullOrWhiteSpace())
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("E-Posta Giriniz");
            }
            else if (eMail.ToIsValidEmail())
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("E-Posta Formatı Uygun Değildir.");
            }
            else
            {
                if (uniqueId.HasValue)
                {
                    if (uye == null) mMessage.Messages.Add("Değerlendirme Linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı Yenileyip Tekrar Deneyiniz.");
                    else
                    {
                        if (isJuriEmailGuncellensin)
                        {
                            uye.Email = eMail;
                            _entities.SaveChanges();
                        }
                    }
                }
                var messages = MezuniyetBus.SendMailMezuniyetDegerlendirmeLink(srTalep.SRTalepID, uniqueId, true, isYeniLink, eMail);
                if (messages.IsSuccess)
                {
                    srTalep.JuriSonucMezuniyetSinavDurumID = null;
                    _entities.SaveChanges();
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Değerlendirme Linki Jüri Üyesine Gönderildi.");
                }
                else
                {
                    mMessage.Messages.AddRange(messages.Messages);
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

        public ActionResult OgrenciUzatmaOnayKayit(int srTalepId, bool? isOgrenciUzatmaSonrasiOnay)
        {
            var mmMessage = new MmMessage
            {
                Title = "Mezuniyet başvurusu danışman onay işlemi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
            var kayitYetki = RoleNames.GelenBasvurularKayit.InRole();

            if (!kayitYetki)
            {
                if (srTalep.MezuniyetBasvurulari.KullaniciID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz!");
                }
            }
            if (srTalep.IsDanismanUzatmaSonrasiOnay.HasValue)
            {
                mmMessage.Messages.Add("Danışman taahhüt onayı yapıldı. Bu işlemi yapamazsınız.");
            }
            if (!mmMessage.Messages.Any())
            {
                if (mmMessage.Messages.Count == 0)
                {
                    srTalep.IsOgrenciUzatmaSonrasiOnay = isOgrenciUzatmaSonrasiOnay;
                    srTalep.OgrenciOnayTarihi = DateTime.Now;

                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("SRTalepleri", IslemTipi.Update, srTalep.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(isOgrenciUzatmaSonrasiOnay.HasValue ? (isOgrenciUzatmaSonrasiOnay.Value ? "Tahhüt Onaylandı." : "Taahhüt Ret Edildi.") : "Taahhüt İşlemi Geril Alındı.");
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }


        public ActionResult Sil(int id)
        {
            var mmMessage = MezuniyetBus.MezuniyetBasvurusuSilKontrol(id);

            if (mmMessage.IsSuccess)
            {
                var kayit = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == id);
                var tarih = kayit.BasvuruTarihi.ToString();
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
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Delete, kayit.ToJson());
                    if (kayit.DanismanImzaliFormDosyaYolu.IsNullOrWhiteSpace() == false)
                    {
                        var path = Server.MapPath("~" + kayit.DanismanImzaliFormDosyaYolu);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = Msgtype.Success;
                    foreach (var item in fFList)
                    {
                        var path = Server.MapPath("~" + item);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "Mezuniyet/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}