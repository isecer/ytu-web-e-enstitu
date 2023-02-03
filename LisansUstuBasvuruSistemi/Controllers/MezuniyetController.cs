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
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(Guid? RowID, int? KullaniciID, string EKD)
        {
            return Index(new fmMezuniyetBasvurulari() { RowID = RowID, KullaniciID = KullaniciID, PageSize = 10 }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmMezuniyetBasvurulari model, string EKD)
        {

            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            if (model.RowID.HasValue)
            {
                var basvuru = db.MezuniyetBasvurularis.Where(p => p.RowID == model.RowID).FirstOrDefault();
                if (basvuru != null) model.KullaniciID = basvuru.KullaniciID;
            }
            else
            {
                if (!model.KullaniciID.HasValue || !RoleNames.KullaniciAdinaTezIzlemeBasvurusuYap.InRoleCurrent()) model.KullaniciID = UserIdentity.Current.Id;
            }

            #region bilgiModel
            var bbModel = new IndexPageInfoDto();
            var MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(_EnstituKod);
            bbModel.AktifSurecID = MezuniyetSurecID ?? 0;
            bbModel.SistemBasvuruyaAcik = MezuniyetAyar.MezuniyetBasvurusuAcikmi.GetAyarMz(_EnstituKod, "0").ToBoolean().Value && MezuniyetSurecID.HasValue;
            bbModel.MezuniyetSurec = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == MezuniyetSurecID.Value).FirstOrDefault();
            if (bbModel.MezuniyetSurec != null)
            {
                bbModel.DonemAdi = bbModel.MezuniyetSurec.BaslangicYil + "/" + bbModel.MezuniyetSurec.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == bbModel.MezuniyetSurec.DonemID).First().DonemAdi + " " + bbModel.MezuniyetSurec.SiraNo;
            }
            var Kul = db.Kullanicilars.Where(p => p.KullaniciID == model.KullaniciID).First();
            bbModel.Kullanici = Kul;
            if (Kul.YtuOgrencisi)
            {
                var otb = db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod && p.OgrenimTipKod == Kul.OgrenimTipKod).First();

                bbModel.OgrenimDurumAdi = Kul.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                bbModel.AnabilimdaliAdi = Kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                bbModel.ProgramAdi = Kul.Programlar.ProgramAdi;
                bbModel.OgrenciNo = Kul.OgrenciNo;
                bbModel.KullaniciTipYetki = Kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci;

                if (Kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                {
                    var kullKayitB = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(Kul.KullaniciID);
                    if (Kul.KayitTarihi != kullKayitB.KayitTarihi)
                    {
                        Kul.KayitYilBaslangic = kullKayitB.BaslangicYil;
                        Kul.KayitDonemID = kullKayitB.DonemID;
                        Kul.KayitTarihi = kullKayitB.KayitTarihi;
                        db.SaveChanges();
                    }
                    if (kullKayitB.KayitVar == false)
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "Öğrenim Bilginiz Doğrulanamdı. Profil bilgilerinizde giriş yaptığınız YTU Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen";
                    }
                    else bbModel.KayitDonemi = Kul.KayitYilBaslangic + "/" + (Kul.KayitYilBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == Kul.KayitDonemID.Value).First().DonemAdi + " , " + Kul.KayitTarihi.ToString("dd.MM.yyyy");

                }

            }
            else
            {
                bbModel.KullaniciTipYetki = false;
                bbModel.KullaniciTipYetkiYokMsj = "Profil bilgilerinizde YTU Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için mezuniyet başvurusu yapamazsınız. Sağ üst köşeden profil bilgilerini düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
            }
            bbModel.Enstitü = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
            bbModel.Kullanici = Kul;
            #endregion 
            var nowDate = DateTime.Now;
            string EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var q = from s in db.MezuniyetBasvurularis
                    join ms in db.MezuniyetSurecis on s.MezuniyetSurecID equals ms.MezuniyetSurecID
                    join kul in db.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                    join mOT in db.MezuniyetSureciOgrenimTipKriterleris on new { s.MezuniyetSurecID, s.OgrenimTipKod } equals new { mOT.MezuniyetSurecID, mOT.OgrenimTipKod }
                    join o in db.OgrenimTipleris on new { s.OgrenimTipKod, ms.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join ot in db.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID
                    join pr in db.Programlars on s.ProgramKod equals pr.ProgramKod
                    join prl in db.Programlars on s.ProgramKod equals prl.ProgramKod
                    join abl in db.AnabilimDallaris on pr.AnabilimDaliID equals abl.AnabilimDaliID
                    join en in db.Enstitulers on s.MezuniyetSureci.EnstituKod equals en.EnstituKod
                    join bs in db.MezuniyetSurecis on s.MezuniyetSurecID equals bs.MezuniyetSurecID
                    join d in db.Donemlers on bs.DonemID equals d.DonemID
                    join ktip in db.KullaniciTipleris on s.Kullanicilar.KullaniciTipID equals ktip.KullaniciTipID
                    join dr in db.MezuniyetYayinKontrolDurumlaris on s.MezuniyetYayinKontrolDurumID equals dr.MezuniyetYayinKontrolDurumID
                    join qmsd in db.MezuniyetSinavDurumlaris on s.MezuniyetSinavDurumID equals qmsd.MezuniyetSinavDurumID into defMsd
                    from Msd in defMsd.DefaultIfEmpty()
                    join qjOf in db.MezuniyetJuriOneriFormlaris on s.MezuniyetBasvurulariID equals qjOf.MezuniyetBasvurulariID into defJof
                    from jOf in defJof.DefaultIfEmpty()
                    let SrT = s.SRTalepleris.OrderByDescending(o => o.SRTalepID).FirstOrDefault()
                    let TD = s.MezuniyetBasvurulariTezDosyalaris.OrderByDescending(o => o.MezuniyetBasvurulariTezDosyaID).FirstOrDefault()
                    where bs.Enstituler.EnstituKisaAd.Contains(EKD) && s.KullaniciID == model.KullaniciID
                    select new frMezuniyetBasvurulari
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
                        TcPasaPortNo = kul.TcKimlikNo != null ? kul.TcKimlikNo : kul.PasaportNo,
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
                        SrTalebi = SrT,
                        SRDurumID = SrT.SRDurumID,
                        TeslimFormDurumu = SrT != null ? SrT.SRTalepleriBezCiltFormus.Any() : false,
                        IsOnaylandiOrDuzeltme = TD != null ? TD.IsOnaylandiOrDuzeltme : null,
                        MezuniyetBasvurulariTezDosyasi = TD,
                        UzatmaSuresiGun = mOT.MBSinavUzatmaSuresiGun,
                        MezuniyetSuresiGun = mOT.MBSinavUzatmaSuresiGun,
                        EYKTarihi = s.EYKTarihi,
                        MBYayinTurIDs = s.MezuniyetBasvurulariYayins.Select(s => s.MezuniyetYayinTurID).ToList(),
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
                        MezuniyetSinavDurumID = Msd.MezuniyetSinavDurumID,
                        MezuniyetSinavDurumAdi = Msd != null ? Msd.MezuniyetSinavDurumAdi : "",
                        SDurumClassName = Msd != null ? Msd.ClassName : "",
                        SDurumColor = Msd != null ? Msd.Color : "",
                        MezuniyetYayinKontrolDurumAciklamasi = s.MezuniyetYayinKontrolDurumAciklamasi,


                    };
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.MezuniyetSurecID.HasValue) q = q.Where(p => p.MezuniyetSurecID == model.MezuniyetSurecID.Value);
            //if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID.Value);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcPasaPortNo == model.AdSoyad || p.KullaniciTipAdi.Contains(model.AdSoyad));
            if (model.MezuniyetYayinKontrolDurumID.HasValue) q = q.Where(p => p.MezuniyetYayinKontrolDurumID == model.MezuniyetYayinKontrolDurumID.Value);
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
            ViewBag.MezuniyetSurecID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecleri(EnstituKod, true), "Value", "Caption", model.MezuniyetSurecID);
            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurumListe(true, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);

            ViewBag.bModel = bbModel;
            return View(model);
        }



        public ActionResult BasvuruYap(int? MezuniyetBasvurulariID, int? KullaniciID = null, string EnstituKod = "", string EKD = "")
        {
            var model = new kmMezuniyetBasvuru();

            model.EnstituKod = EnstituKod.IsNullOrWhiteSpace() ? EnstituBus.GetSelectedEnstitu(EKD) : EnstituKod;


            if (MezuniyetBasvurulariID.HasValue || KullaniciID.HasValue)
            {
                if (KullaniciID.HasValue)
                    if (RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() == false)
                        KullaniciID = UserIdentity.Current.Id;
                if (MezuniyetBasvurulariID.HasValue)
                {
                    var basvuru = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID.Value).FirstOrDefault();
                    model.EnstituKod = EnstituKod = basvuru.MezuniyetSureci.EnstituKod;
                    if (KullaniciID.HasValue == false) KullaniciID = basvuru.KullaniciID;
                }
            }
            else
            {
                KullaniciID = UserIdentity.Current.Id;
            }

            var _MmMessage = MezuniyetBus.MezuniyetSurecAktifKontrol(model.EnstituKod, KullaniciID, MezuniyetBasvurulariID);
            var studentInfo = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(KullaniciID.Value);
            var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).FirstOrDefault();

            var DanismanBilgi = db.Kullanicilars.Where(p => p.KullaniciID == kul.DanismanID).FirstOrDefault();
            if (!MezuniyetBasvurulariID.HasValue && _MmMessage.IsSuccess)
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

                    //Tez bilgisi gelmiyor ise Tez durumu ile alakalı olabilir. Tez durumu devam ediyor olmalı. Eğer değilse Ya yeni tez eklenecek yada gsis te tez guncellemeden tez durumunu devam ediyor yapılmalı.
                }
                //else if (DanismanBilgi.KullaniciID == -1)
                //{
                //    _MmMessage.Messages.Add("Tez danışmanı bilginiz çekilemedi sisteminden alınamadı.  Obs sisteminde tez durumunuzun devam ediyor olması ve danışmanınızın tanımlı olması gerekmektedir. Başvuru yapabilmeniz için bu durumu enstitü yetkililerine bildiriniz.");
                //    _MmMessage.IsSuccess = false;
                //}
            }
            if (_MmMessage.IsSuccess)
            {
                var DonemAdi = "";
                if (kul.KayitDonemID.HasValue)
                {
                    DonemAdi = db.Donemlers.Where(p => p.DonemID == kul.KayitDonemID.Value).First().DonemAdi;
                }
                model.KayitDonemi = kul.KayitYilBaslangic + "/" + (kul.KayitYilBaslangic + 1) + " " + DonemAdi;
                model.KayitTarihi = kul.KayitTarihi;
                if (MezuniyetBasvurulariID.HasValue)
                {
                    model = MezuniyetBus.GetMezuniyetBasvuruBilgi(MezuniyetBasvurulariID.Value);
                    model.EnstituKod = EnstituKod.IsNullOrWhiteSpace() ? EnstituBus.GetSelectedEnstitu(EKD) : EnstituKod;
                    model.ResimAdi = kul.ResimAdi;
                    KullaniciID = model.KullaniciID;

                }
                else
                {

                    model.MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(model.EnstituKod).Value;
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
                    model.OgrenimTipAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod == kul.OgrenimTipKod).First().OgrenimTipAdi;
                    var progLng = kul.Programlar;
                    model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = progLng.ProgramAdi;
                    model.TezDanismanAdi = (DanismanBilgi.Ad + " " + DanismanBilgi.Soyad).ToUpper();
                    model.TezDanismanUnvani = DanismanBilgi.Unvanlar.UnvanAdi.ToUpper();
                    model.IsTezDiliTr = studentInfo.IsTezDiliTr;
                    model.TezBaslikTr = studentInfo.OgrenciTez.TEZ_BASLIK;
                    model.TezBaslikEn = studentInfo.OgrenciTez.TEZ_BASLIK_ENG;
                }
                var surec = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == model.MezuniyetSurecID).First();
                model.DonemAdi = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi;
                model.SetSelectedStep = 1;
                model.IsYerli = kul.KullaniciTipleri.Yerli;
                model.KullaniciTipAdi = db.KullaniciTipleris.Where(p => p.KullaniciTipID == kul.KullaniciTipID).First().KullaniciTipAdi;
                ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurum(true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
                ViewBag.TezEsDanismanUnvani = new SelectList(MezuniyetBus.GetCmbMezuniyetJofUnvanlar(true), "Value", "Caption", model.TezEsDanismanUnvani);

                ViewBag.MezuniyetYayinTurID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecYayinTurleri(model.MezuniyetSurecID, model.KullaniciID, true), "Value", "Caption");

                ViewBag._MmMessage = _MmMessage;
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, _MmMessage.Messages.ToArray());
                return RedirectToAction("Index", new { KullaniciID = KullaniciID });
            }
            if (model.MezuniyetBasvurulariID > 0)
            {
                ViewBag.MezuniyetYayinKontrolDurumu = db.MezuniyetYayinKontrolDurumlaris.Where(p => p.MezuniyetYayinKontrolDurumID == model.MezuniyetYayinKontrolDurumID).Select(s => new MezuniyetYayinKontrolDurumDto { MezuniyetYayinKontrolDurumID = s.MezuniyetYayinKontrolDurumID, ClassName = s.ClassName, Color = s.Color, DurumAdi = s.MezuniyetYayinKontrolDurumAdi }).First();
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
            var _MmMessage = new MmMessage();


            if (RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() == false) { kModel.KullaniciID = UserIdentity.Current.Id; }
            _MmMessage = MezuniyetBus.MezuniyetSurecAktifKontrol(kModel.EnstituKod, kModel.KullaniciID, kModel.MezuniyetBasvurulariID.toNullIntZero());
            if (kModel.MezuniyetBasvurulariID <= 0)
            {
                kModel.MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(kModel.EnstituKod) ?? 0;
                kModel.BasvuruTarihi = DateTime.Now;
            }
            else
            {
                var btarih = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).First();
                kModel.BasvuruTarihi = btarih.BasvuruTarihi;
            }
            var bsurec = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == kModel.MezuniyetSurecID).First();
            kModel.EnstituKod = bsurec.EnstituKod;
            kModel.DonemAdi = bsurec.BaslangicYil + "/" + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;

            var studentInfo = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(kModel.KullaniciID);
            var kul = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).FirstOrDefault();
            kModel.OgrenimTipKod = kul.OgrenimTipKod.Value;
            #region Kontrol
            var tezK = MezuniyetBus.TezKontrol(kModel);
            _MmMessage.Messages.AddRange(tezK.Messages.ToList());
            _MmMessage.MessagesDialog.AddRange(tezK.MessagesDialog.ToList());
            if (_MmMessage.Messages.Count > 0) stps.Add(1);
            else
            {
                if (kModel.MezuniyetYayinKontrolDurumID <= 0)
                {
                    stps.Add(2);
                    _MmMessage.Messages.Add("Başvuru Durumunu Seçiniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinKontrolDurumID" });
                }
                else if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi)
                {
                    var yaynK = MezuniyetBus.YayinKontrol(kModel);
                    _MmMessage.Messages.AddRange(yaynK.Messages.ToList());
                    _MmMessage.MessagesDialog.AddRange(yaynK.MessagesDialog.ToList());
                    if (_MmMessage.Messages.Count > 0) stps.Add(2);
                }

            }
            #endregion

            if (kModel.MezuniyetBasvurulariID <= 0 && _MmMessage.Messages.Count == 0)
            {

                var DanismanTC = studentInfo.OgrenciInfo.DANISMAN_TC1;
                if (!kul.DanismanID.HasValue && (DanismanTC.IsNullOrWhiteSpace() || DanismanTC.Length != 11))
                {
                    _MmMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                    _MmMessage.IsSuccess = false;
                }
                else if (kul.DanismanID.HasValue == false)
                {

                    _MmMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
                    _MmMessage.IsSuccess = false;
                }

            }
            bool sendMail = false;
            if (_MmMessage.Messages.Count == 0)
            {
                kModel.IsYerli = kul.KullaniciTipleri.Yerli;
                kModel.ResimAdi = kul.ResimAdi;
                kModel.KullaniciTipAdi = db.KullaniciTipleris.Where(p => p.KullaniciTipID == kul.KullaniciTipID).First().KullaniciTipAdi;
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

                var MBasvuru = new MezuniyetBasvurulari();
                bool IsNewRecord = false;
                if (kModel.MezuniyetBasvurulariID <= 0)
                {
                    IsNewRecord = true;
                    kModel.BasvuruTarihi = DateTime.Now;

                    if (kModel.DanismanImzaliFormDosya != null)
                    {

                        string yBDosyaYolu = "/TezDosyalari/" + kModel.DanismanImzaliFormDosya.FileName.ToFileNameAddGuid();
                        var sfilename = Server.MapPath("~" + yBDosyaYolu);
                        kModel.DanismanImzaliFormDosya.SaveAs(sfilename);
                        kModel.DanismanImzaliFormDosyaAdi = kModel.DanismanImzaliFormDosya.FileName.GetFileName().ReplaceSpecialCharacter();
                        kModel.DanismanImzaliFormDosyaYolu = yBDosyaYolu;
                    }
                    MBasvuru = db.MezuniyetBasvurularis.Add(new MezuniyetBasvurulari
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
                        PasaportNo = kModel.PasaportNo,
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



                    MBasvuru.TezDanismanID = kul.DanismanID;



                    db.SaveChanges();
                    kModel.MezuniyetBasvurulariID = MBasvuru.MezuniyetBasvurulariID;
                    if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi) sendMail = true;




                }
                else
                {

                    MBasvuru = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).First();
                    if (!MBasvuru.TezDanismanID.HasValue || MBasvuru.TezDanismanID <= 0) MBasvuru.TezDanismanID = kul.DanismanID;
                    if (kModel.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi && kModel.MezuniyetYayinKontrolDurumID != MBasvuru.MezuniyetYayinKontrolDurumID && MezuniyetAyar.GetAyarMz(MezuniyetAyar.YeniMezuniyetBasvurusundaMailGonder, kModel.EnstituKod).ToBoolean() == true)
                    {
                        MBasvuru.BasvuruTarihi = DateTime.Now;
                        sendMail = true;
                    }
                    if (MBasvuru.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumu.KabulEdildi)
                    {
                        MBasvuru.IsDanismanOnay = null;
                    }
                    MBasvuru.MezuniyetSurecID = kModel.MezuniyetSurecID;
                    MBasvuru.BasvuruTarihi = kModel.BasvuruTarihi;
                    MBasvuru.MezuniyetYayinKontrolDurumID = kModel.MezuniyetYayinKontrolDurumID;
                    MBasvuru.MezuniyetYayinKontrolDurumAciklamasi = kModel.MezuniyetYayinKontrolDurumAciklamasi;
                    MBasvuru.KullaniciID = kModel.KullaniciID;
                    MBasvuru.KullaniciTipID = kModel.KullaniciTipID;
                    MBasvuru.ResimAdi = kModel.ResimAdi;
                    MBasvuru.Ad = kModel.Ad;
                    MBasvuru.Soyad = kModel.Soyad;
                    MBasvuru.UyrukKod = kModel.UyrukKod;
                    MBasvuru.TcKimlikNo = kModel.TcKimlikNo;
                    MBasvuru.PasaportNo = kModel.PasaportNo;
                    MBasvuru.OgrenciNo = kModel.OgrenciNo;
                    MBasvuru.OgrenimDurumID = kModel.OgrenimDurumID;
                    MBasvuru.OgrenimTipKod = kModel.OgrenimTipKod;
                    MBasvuru.ProgramKod = kModel.ProgramKod;
                    MBasvuru.KayitOgretimYiliBaslangic = kModel.KayitOgretimYiliBaslangic;
                    MBasvuru.KayitOgretimYiliDonemID = kModel.KayitOgretimYiliDonemID;
                    MBasvuru.KayitTarihi = kModel.KayitTarihi;
                    MBasvuru.IsTezDiliTr = kModel.IsTezDiliTr;
                    MBasvuru.TezBaslikTr = kModel.TezBaslikTr;
                    MBasvuru.TezBaslikEn = kModel.TezBaslikEn;
                    MBasvuru.TezDanismanUnvani = kModel.TezDanismanUnvani;
                    MBasvuru.TezDanismanAdi = kModel.TezDanismanAdi;
                    MBasvuru.TezEsDanismanAdi = kModel.TezEsDanismanAdi;
                    MBasvuru.TezEsDanismanUnvani = kModel.TezEsDanismanUnvani;
                    MBasvuru.TezOzet = kModel.TezOzet;
                    MBasvuru.OzetAnahtarKelimeler = kModel.OzetAnahtarKelimeler;
                    MBasvuru.TezAbstract = kModel.TezAbstract;
                    MBasvuru.AbstractAnahtarKelimeler = kModel.AbstractAnahtarKelimeler;
                    MBasvuru.IslemTarihi = DateTime.Now;
                    MBasvuru.IslemYapanID = UserIdentity.Current.Id;
                    MBasvuru.IslemYapanIP = UserIdentity.Ip;

                    var SilinecekYayins = db.MezuniyetBasvurulariYayins.Where(p => kModel._MezuniyetBasvurulariYayinID.Contains(p.MezuniyetBasvurulariYayinID) == false && p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).ToList();
                    var GuncellenecekYayins = db.MezuniyetBasvurulariYayins.Where(p => kModel._MezuniyetBasvurulariYayinID.Contains(p.MezuniyetBasvurulariYayinID) && p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).ToList();
                    var fFList = new List<string>();
                    foreach (var item in SilinecekYayins)
                    {
                        if (item.MezuniyetYayinBelgeDosyaYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinBelgeDosyaYolu);
                        if (item.MezuniyetYayinMetniBelgeYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinMetniBelgeYolu);
                    }
                    db.MezuniyetBasvurulariYayins.RemoveRange(SilinecekYayins).ToList();
                    if (kModel.DanismanImzaliFormDosya != null)
                    {
                        var path = Server.MapPath("~" + MBasvuru.DanismanImzaliFormDosyaYolu);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                        if (kModel.DanismanImzaliFormDosya != null)
                        {
                            string yBDosyaYolu = "/TezDosyalari/" + kModel.DanismanImzaliFormDosya.FileName.ToFileNameAddGuid();
                            kModel.DanismanImzaliFormDosya.SaveAs(Server.MapPath("~" + yBDosyaYolu));
                            MBasvuru.DanismanImzaliFormDosyaAdi = kModel.DanismanImzaliFormDosya.FileName.GetFileName().ReplaceSpecialCharacter();
                            MBasvuru.DanismanImzaliFormDosyaYolu = yBDosyaYolu;
                        }
                    }
                    foreach (var item in GuncellenecekYayins)
                    {
                        item.Onaylandi = null; 
                    }
                    db.SaveChanges();
                    foreach (var item in fFList)
                    {
                        var path = Server.MapPath("~" + item);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }

                }

                LogIslemleri.LogEkle("MezuniyetBasvurulari", IsNewRecord ? IslemTipi.Insert : IslemTipi.Update, MBasvuru.ToJson());


                var qMyID = kModel._MezuniyetBasvurulariYayinID.Select((s, inx) => new { MezuniyetBasvurulariYayinID = s, Index = inx }).ToList();
                var qYbaslik = kModel._YayinBasligi.Select((s, inx) => new { YayinBasligi = s, Index = inx }).ToList();
                var qYy = kModel._Yayinlanmis.Select((s, inx) => new { Yayinlanmis = s, Index = inx }).ToList();
                var qYTar = kModel._MezuniyetYayinTarih.Select((s, inx) => new { MezuniyetYayinTarih = s, Index = inx }).ToList();
                var qMytID = kModel._MezuniyetYayinTurID.Select((s, inx) => new { MezuniyetYayinTurID = s, Index = inx }).ToList();
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
                var qPID = kModel._MezuniyetYayinProjeTurID.Select((s, inx) => new { MezuniyetYayinProjeTurID = s, Index = inx }).ToList();
                var qDvm = kModel._IsProjeTamamlandiOrDevamEdiyor.Select((s, inx) => new { IsProjeTamamlandiOrDevamEdiyor = s, Index = inx }).ToList();
                var qPek = kModel._ProjeEkibi.Select((s, inx) => new { ProjeEkibi = s, Index = inx }).ToList();
                var qPdk = kModel._ProjeDeatKurulus.Select((s, inx) => new { ProjeDeatKurulus = s, Index = inx }).ToList();
                var qTara = kModel._TarihAraligi.Select((s, inx) => new { TarihAraligi = s, Index = inx }).ToList();
                var qEtad = kModel._EtkinlikAdi.Select((s, inx) => new { EtkinlikAdi = s, Index = inx }).ToList();
                var qYer = kModel._YerBilgisi.Select((s, inx) => new { YerBilgisi = s, Index = inx }).ToList();

                var qYayins = (from b in qYbaslik
                               join my in qMyID on b.Index equals my.Index
                               join yd in qYy on b.Index equals yd.Index
                               join mytar in qYTar on b.Index equals mytar.Index
                               join myt in qMytID on b.Index equals myt.Index
                               join myb in qMybelge on b.Index equals myb.Index
                               join mybA in qMybelgeAd on b.Index equals mybA.Index
                               join mkl in qMkLink on b.Index equals mkl.Index
                               join mb in qMbelge on b.Index equals mb.Index
                               join mbA in qMbelgeAd on b.Index equals mbA.Index
                               join myl in qMyLink on b.Index equals myl.Index
                               join mI in qIndex on b.Index equals mI.Index
                               join kem in qKeM on b.Index equals kem.Index
                               join bt in db.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == kModel.MezuniyetSurecID) on myt.MezuniyetYayinTurID equals bt.MezuniyetYayinTurID

                               join Yaz in qYaz on b.Index equals Yaz.Index
                               join Der in qDer on b.Index equals Der.Index
                               join Ycs in qYcs on b.Index equals Ycs.Index
                               join PID in qPID on b.Index equals PID.Index
                               join Dvm in qDvm on b.Index equals Dvm.Index
                               join Pek in qPek on b.Index equals Pek.Index
                               join Pdk in qPdk on b.Index equals Pdk.Index
                               join Tara in qTara on b.Index equals Tara.Index
                               join Etad in qEtad on b.Index equals Etad.Index
                               join Yer in qYer on b.Index equals Yer.Index
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
                                   Yaz.YazarAdi,
                                   Der.DergiAdi,
                                   Ycs.YilCiltSayiSS,
                                   PID.MezuniyetYayinProjeTurID,
                                   Dvm.IsProjeTamamlandiOrDevamEdiyor,
                                   Pek.ProjeEkibi,
                                   Pdk.ProjeDeatKurulus,
                                   Tara.TarihAraligi,
                                   Etad.EtkinlikAdi,
                                   Yer.YerBilgisi
                               }).ToList();
                foreach (var item in qYayins)
                {
                    var rowMDY = new Models.MezuniyetBasvurulariYayin();
                    rowMDY.MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID;
                    rowMDY.MezuniyetYayinTurID = item.MezuniyetYayinTurID;
                    rowMDY.Yayinlanmis = item.Yayinlanmis;
                    rowMDY.YayinBasligi = item.YayinBasligi;
                    rowMDY.MezuniyetYayinTarih = item.MezuniyetYayinTarih;
                    if (item.MezuniyetYayinBelgesi != null)
                    {
                        string yBDosyaYolu = "/TezDosyalari/" + item.MezuniyetYayinBelgesi.FileName.ToFileNameAddGuid();
                        item.MezuniyetYayinBelgesi.SaveAs(Server.MapPath("~" + yBDosyaYolu));
                        rowMDY.MezuniyetYayinBelgeTurID = item.MezuniyetYayinBelgeTurID;
                        rowMDY.MezuniyetYayinBelgeAdi = item.MezuniyetYayinBelgesi.FileName.GetFileName().ReplaceSpecialCharacter();
                        rowMDY.MezuniyetYayinBelgeDosyaYolu = yBDosyaYolu;
                    }
                    rowMDY.MezuniyetYayinKaynakLinkTurID = item.KaynakMezuniyetYayinLinkTurID;
                    rowMDY.MezuniyetYayinKaynakLinki = item.MezuniyetYayinKaynakLinki;
                    if (item.YayinMetniBelgesi != null)
                    {
                        string ymTDosyaYolu = "/TezDosyalari/" + item.YayinMetniBelgesi.FileName.ToFileNameAddGuid();
                        item.YayinMetniBelgesi.SaveAs(Server.MapPath("~" + ymTDosyaYolu));
                        rowMDY.MezuniyetYayinMetinTurID = item.MezuniyetYayinMetinTurID;
                        rowMDY.MezuniyetYayinMetniBelgeAdi = item.YayinMetniBelgesi.FileName.GetFileName().ReplaceSpecialCharacter();
                        rowMDY.MezuniyetYayinMetniBelgeYolu = ymTDosyaYolu;
                    }
                    if (item.KabulEdilmisMakale != null)
                    {

                        string ymTDosyaYolu = "/TezDosyalari/" + item.KabulEdilmisMakale.FileName.ToFileNameAddGuid();
                        item.KabulEdilmisMakale.SaveAs(Server.MapPath("~" + ymTDosyaYolu));

                        rowMDY.MezuniyetYayinKabulEdilmisMakaleAdi = item.KabulEdilmisMakale.FileName.GetFileName().ReplaceSpecialCharacter();
                        rowMDY.MezuniyetYayinKabulEdilmisMakaleDosyaYolu = ymTDosyaYolu;
                    }
                    rowMDY.MezuniyetYayinLinkTurID = item.YayinMezuniyetYayinLinkTurID;
                    rowMDY.MezuniyetYayinLinki = item.MezuniyetYayinLinki;
                    rowMDY.MezuniyetYayinIndexTurID = item.MezuniyetYayinIndexTurID;


                    rowMDY.DergiAdi = item.DergiAdi;
                    rowMDY.YazarAdi = item.YazarAdi;
                    rowMDY.YilCiltSayiSS = item.YilCiltSayiSS;
                    rowMDY.MezuniyetYayinProjeTurID = item.MezuniyetYayinProjeTurID;
                    rowMDY.IsProjeTamamlandiOrDevamEdiyor = item.IsProjeTamamlandiOrDevamEdiyor;
                    rowMDY.ProjeEkibi = item.ProjeEkibi;
                    rowMDY.ProjeDeatKurulus = item.ProjeDeatKurulus;
                    rowMDY.TarihAraligi = item.TarihAraligi;
                    rowMDY.EtkinlikAdi = item.EtkinlikAdi;
                    rowMDY.YerBilgisi = item.YerBilgisi;
                    db.MezuniyetBasvurulariYayins.Add(rowMDY);

                }
                db.SaveChanges();
                if (sendMail)
                {
                    var Enstitu = MBasvuru.MezuniyetSureci.Enstituler;
                    var Sablonlar = db.MailSablonlaris.Where(p => p.EnstituKod == Enstitu.EnstituKod).ToList();


                    var mModel = new List<SablonMailModel>();


                    mModel.Add(new SablonMailModel
                    {

                        AdSoyad = MBasvuru.Ad + " " + MBasvuru.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = MBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                        MailSablonTipID = MailSablonTipi.Mez_BasvuruYapildiOgrenci,
                    });
                    var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == kul.DanismanID).First();
                    mModel.Add(new SablonMailModel
                    {

                        AdSoyad = Danisman.Ad + " " + Danisman.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                        MailSablonTipID = MailSablonTipi.Mez_BasvuruYapildiDanisman,
                    });

                    foreach (var item in mModel)
                    {
                        var EnstituL = MBasvuru.MezuniyetSureci.Enstituler;

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var ParamereDegerleri = new List<MailReplaceParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = MBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = MBasvuru.Ad + " " + MBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = MBasvuru.TezDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = MBasvuru.TezDanismanUnvani });


                        var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.SendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, null);
                        if (snded)
                        {
                            var GM = new GonderilenMailler();
                            GM.Tarih = DateTime.Now;
                            GM.EnstituKod = Enstitu.EnstituKod;
                            GM.MesajID = null;
                            GM.IslemTarihi = DateTime.Now;
                            GM.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                            GM.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            GM.IslemYapanIP = UserIdentity.Ip;
                            GM.Aciklama = item.Sablon.Sablon ?? "";
                            GM.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            GM.Gonderildi = true;
                            GM.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            db.GonderilenMaillers.Add(GM);
                            db.SaveChanges();
                        }
                    }


                }
                if (kModel.KullaniciID != UserIdentity.Current.Id) return RedirectToAction("Index", "MezuniyetGelenBasvurular");
                else return RedirectToAction("Index", kModel.KullaniciID);
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, _MmMessage.Messages.ToArray());
            }

            if (stps.Count > 0) kModel.SetSelectedStep = stps.First();
            ViewBag._MmMessage = _MmMessage;
            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurum(true), "Value", "Caption", kModel.MezuniyetYayinKontrolDurumID);
            ViewBag.TezEsDanismanUnvani = new SelectList(MezuniyetBus.GetCmbMezuniyetJofUnvanlar(true), "Value", "Caption", kModel.TezEsDanismanUnvani);
            ViewBag.MezuniyetYayinTurID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecYayinTurleri(kModel.MezuniyetSurecID, kModel.KullaniciID, true), "Value", "Caption");


            if (kModel.MezuniyetYayinKontrolDurumID > 0)
            {
                ViewBag.MezuniyetYayinKontrolDurumu = db.MezuniyetYayinKontrolDurumlaris.Where(p => p.MezuniyetYayinKontrolDurumID == kModel.MezuniyetYayinKontrolDurumID).Select(s => new MezuniyetYayinKontrolDurumDto { MezuniyetYayinKontrolDurumID = s.MezuniyetYayinKontrolDurumID, ClassName = s.ClassName, Color = s.Color, DurumAdi = s.MezuniyetYayinKontrolDurumAdi }).First();
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

        public ActionResult getYayinTur(int MezuniyetSurecID, int MezuniyetYayinTurID)
        {

            var mdl = MezuniyetBus.GetYayinBilgisi(MezuniyetSurecID, MezuniyetYayinTurID);
            mdl.MezuniyetSurecID = MezuniyetSurecID;
            return View(mdl);
        }

        public ActionResult YayinEklemeKontrol(kmMezuniyetBasvuru model)
        {
            string ProjeTurAdi = "";
            if (model.YayinBilgisi.MezuniyetYayinProjeTurID.HasValue)
            {
                var ProjeTuru = db.MezuniyetYayinProjeTurleris.Where(p => p.MezuniyetYayinProjeTurID == model.YayinBilgisi.MezuniyetYayinProjeTurID).First();
                ProjeTurAdi = ProjeTuru.ProjeTurAdi;
            }

            var yayinBilgi = (from s in db.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == model.MezuniyetSurecID && p.MezuniyetYayinTurID == model.YayinBilgisi.MezuniyetYayinTurID)
                              join sd in db.MezuniyetYayinTurleris on s.MezuniyetYayinTurID equals sd.MezuniyetYayinTurID
                              join yb in db.MezuniyetYayinBelgeTurleris on s.MezuniyetYayinBelgeTurID equals yb.MezuniyetYayinBelgeTurID into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in db.MezuniyetYayinLinkTurleris on s.KaynakMezuniyetYayinLinkTurID equals klk.MezuniyetYayinLinkTurID into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in db.MezuniyetYayinMetinTurleris on s.MezuniyetYayinMetinTurID equals ym.MezuniyetYayinMetinTurID into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in db.MezuniyetYayinLinkTurleris on s.YayinMezuniyetYayinLinkTurID equals kl.MezuniyetYayinLinkTurID into defkl
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
                                  MezuniyetYayinKaynakLinkIsUrl = klkD != null ? klkD.IsUrl : false,
                                  MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = model.YayinBilgisi.MezuniyetYayinMetniBelgeAdi,
                                  MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = klD.LinkTurAdi != null ? klD.LinkTurAdi : "",
                                  MezuniyetYayinLinkIsUrl = klD != null ? klD.IsUrl : false,
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
                                  ProjeTurAdi = ProjeTurAdi,
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
                            var inxB = db.MezuniyetYayinIndexTurleris.Where(p => p.MezuniyetYayinIndexTurID == model.YayinBilgisi.MezuniyetYayinIndexTurID.Value).First();
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

        public ActionResult RezervasyonAl(int MezuniyetBasvurulariID, int SRTalepID)
        {

            var yetkiliKullanici = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var srYetkiliKullanici = RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent();
            var mezuniyetBasvurularis = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID);
            if (!yetkiliKullanici && !srYetkiliKullanici) mezuniyetBasvurularis.Where(p => p.KullaniciID == UserIdentity.Current.Id);
            else if (srYetkiliKullanici) mezuniyetBasvurularis.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            var mezuniyetBasvuru = mezuniyetBasvurularis.First();
            var model = new kmSRTalep();
            model.IsSalonSecilsin = mezuniyetBasvuru.OgrenimTipKod == OgrenimTipi.Doktra && mezuniyetBasvuru.MezuniyetSureci.EnstituKod == EnstituKodlari.FenBilimleri;
            if (SRTalepID > 0)
            {
                var srTalebi = mezuniyetBasvuru.SRTalepleris.First(p => p.SRTalepID == SRTalepID);
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

            var mezuniyetBasvurusu = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
            var sonSrTalebi = mezuniyetBasvurusu.SRTalepleris.LastOrDefault();
            var srTalebiYetkisi = mezuniyetBasvurusu.KullaniciID == UserIdentity.Current.Id || RoleNames.MezuniyetGelenBasvurularSrTalebiYap.InRoleCurrent();


            kModel.SRTalepTipID = 1;
            kModel.IsSalonSecilsin = mezuniyetBasvurusu.OgrenimTipKod == OgrenimTipi.Doktra && mezuniyetBasvurusu.MezuniyetSureci.EnstituKod != EnstituKodlari.SosyalBilimleri;
            kModel.EnstituKod = mezuniyetBasvurusu.MezuniyetSureci.EnstituKod;
            if (!srTalebiYetkisi)
            {
                var msg = "Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!";
                mmMessage.Messages.Add(msg);
                Management.SistemBilgisiKaydet(msg + "\r\n İşlem yapılmak istenen KullanıcıID:" + kModel.TalepYapanID + "\r\n İşlemYapanID:" + UserIdentity.Current.Id, "Mezuniyet/RezervasyonAlPost", LogType.Saldırı);
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
                    var ssts = new List<SROzelTanimSaatler>();
                    ssts.Add(new SROzelTanimSaatler { BasSaat = kModel.BasSaat.Value, BitSaat = kModel.BitSaat.Value });
                    var msg = SrTalepleriBus.SrKayitKontrol(kModel.SRSalonID.Value, kModel.SRTalepTipID, kModel.Tarih, ssts, kModel.SRTalepID, null, null, null, mezuniyetBasvurusu.EYKTarihi);
                    mmMessage.Messages.AddRange(msg.Messages);
                }
                kModel.SRSalonID = kModel.IsSalonSecilsin ? kModel.SRSalonID : null;
                kModel.SalonAdi = kModel.IsSalonSecilsin && kModel.SRSalonID.HasValue ? db.SRSalonlars.Where(p => p.SRSalonID == kModel.SRSalonID).First().SalonAdi : kModel.SalonAdi;
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


                            srTalebi = db.SRTalepleris.Add(new SRTalepleri
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
                            srTalebi = db.SRTalepleris.First(p => p.SRTalepID == kModel.SRTalepID);
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
                                db.SRTaleplerJuris.RemoveRange(srTalebi.SRTaleplerJuris);
                                srTalebi.SRTaleplerJuris = kModel.SRTaleplerJuris;

                            }


                        }
                        db.SaveChanges();

                        LogIslemleri.LogEkle("SRTalepleri", kModel.SRTalepID <= 0 ? IslemTipi.Insert : IslemTipi.Update, srTalebi.ToJson());
                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = Msgtype.Success;
                        mmMessage.Messages.Add("Belirtilen tarih için rezervasyon talebi oluşturuldu.");

                        #region SendMail
                        if (kModel.SRTalepID <= 0)
                        {
                            if (kModel.SRDurumID == SRTalepDurum.Onaylandı)
                            {
                                var srtalep = db.SRTalepleris.Where(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).OrderByDescending(o => o.SRTalepID).First();
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



        public ActionResult BezCiltForm(int SRTalepID, int SRTalepleriBezCiltFormID)
        {
            var YetkiliKullanici = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();

            var SrTalep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID && p.TalepYapanID == (YetkiliKullanici ? p.TalepYapanID : UserIdentity.Current.Id)).First();
            var MBasvuru = SrTalep.MezuniyetBasvurulari;
            var model = new SRTalepleriBezCiltFormu();
            if (SRTalepleriBezCiltFormID > 0)
            {
                var data = SrTalep.SRTalepleriBezCiltFormus.Where(p => p.SRTalepleriBezCiltFormID == SRTalepleriBezCiltFormID).First();
                //var Jof= SrTalep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.FirstOrDefault();
                // if (Jof != null)
                // {

                // }
                model.SRTalepleriBezCiltFormID = data.SRTalepleriBezCiltFormID;
                model.SRTalepID = data.SRTalepID;
                model.IsTezDiliTr = MBasvuru.IsTezDiliTr == true;
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
                model.IsTezDiliTr = MBasvuru.IsTezDiliTr == true;
                model.TezBaslikTr = SrTalep.MezuniyetBasvurulari.TezBaslikTr;
                model.TezBaslikEn = SrTalep.MezuniyetBasvurulari.TezBaslikEn;
                model.TezOzet = SrTalep.MezuniyetBasvurulari.TezOzet;
                model.TezOzetHtml = SrTalep.MezuniyetBasvurulari.TezOzetHtml;
                model.TezAbstract = SrTalep.MezuniyetBasvurulari.TezAbstract;
                model.TezAbstractHtml = SrTalep.MezuniyetBasvurulari.TezAbstractHtml;

            }
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BezCiltFormPost(SRTalepleriBezCiltFormu kModel, bool? IsTezDiliTr)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;
            mmMessage.Title = "Tez Teslim Formu Oluşturma İşlemi";
            mmMessage.MessageType = Msgtype.Warning;

            var yetkiliK = RoleNames.SrTalepDuzelt.InRoleCurrent();

            var SrTalep = db.SRTalepleris.Where(p => p.SRTalepID == kModel.SRTalepID).First();

            var MzTalep = SrTalep.MezuniyetBasvurulari;


            if (SrTalep.TalepYapanID != UserIdentity.Current.Id && !yetkiliK)
            {
                string msg = "Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!";
                mmMessage.Messages.Add(msg);
                Management.SistemBilgisiKaydet(msg + "\r\n İşlem yapılmak istenen KullanıcıID:" + SrTalep.TalepYapanID + "\r\n İşlemYapanID:" + UserIdentity.Current.Id, "Mezuniyet/RezervasyonAlPost", LogType.Saldırı);
            }
            else if (MzTalep.IsMezunOldu.HasValue)
            {
                mmMessage.Messages.Add("Mezuniyet sonuç bilgisi girilildikten sonra Tez teslim formu üzerinde düzeltme işlemi yapılamaz!");

            }
            if (!IsTezDiliTr.HasValue)
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
                        db.SRTalepleriBezCiltFormus.Add(new SRTalepleriBezCiltFormu
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
                        var kKayit = db.SRTalepleriBezCiltFormus.Where(p => p.SRTalepID == kModel.SRTalepID && p.SRTalepleriBezCiltFormID == kModel.SRTalepleriBezCiltFormID).First();
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
                    db.SaveChanges();
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
        public ActionResult AddRow(MezuniyetBasvurulariYayin model)
        {
            return View(model);
        }


        [HttpPost]
        public ActionResult TezDosyaEklePost(Guid RowID, int? MezuniyetBasvurulariTezDosyaID, HttpPostedFileBase BelgeDosyasi)
        {
            var mMessage = new MmMessage();

            mMessage.Title = "Tez Dosyası Yükleme İşlemi";

            mMessage.MessageType = Msgtype.Warning;
            var KayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var Basv = db.MezuniyetBasvurularis.Where(p => p.RowID == RowID).First();
            var TezDosyasi = Basv.MezuniyetBasvurulariTezDosyalaris.Where(p => p.MezuniyetBasvurulariTezDosyaID == MezuniyetBasvurulariTezDosyaID).FirstOrDefault();

            if (Basv.MezuniyetSinavDurumID != MezuniyetSinavDurum.Basarili)
            {
                mMessage.Messages.Add("Tez dosyasını yükleyebilmek için Sınav sürecinden başarılı bir şekilde geçmeniz gerekmektedir.!");
            }
            else if (Basv.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true))
            {
                mMessage.Messages.Add("Onaylanmış bir tez dosyanız bulunmaktadır. Yeni tez dosyası yüklenemez!");
            }
            else if (!KayitYetki && Basv.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Bu başvuru üstünde işlem yapmaya yetkili değilsiniz.");
            }
            else if (BelgeDosyasi != null && BelgeDosyasi.ContentLength > (1024 * 1024 * 20))
            {
                mMessage.Messages.Add("Yükleyeceğiniz dosya boyutu en fazla 20MB olmalıdır.");
            }
            else
            {
                if (TezDosyasi != null && TezDosyasi.IsOnaylandiOrDuzeltme.HasValue)
                {
                    mMessage.Messages.Add("Tez dosyası işlem gördüğünden belge yükleme işlemi yapamazsınız.");
                }
                else if (BelgeDosyasi == null && TezDosyasi == null)
                {
                    mMessage.Messages.Add("Tez dosyasını yüklemek için dosya seçiniz.");
                }
                else if (BelgeDosyasi != null && BelgeDosyasi.FileName.Split('.').Last().ToLower() != "pdf")
                {
                    mMessage.Messages.Add("Yükleyeceğiniz belge 'PDF' türünde olmalıdır.");
                }
            }
            if (mMessage.Messages.Count == 0)
            {



                string DosyaYolu = "/BasvuruDosyalari/MezuniyetBelgeleri/" + BelgeDosyasi.FileName.ToFileNameAddGuid(null, Basv.MezuniyetBasvurulariID.ToString());
                string BelgeAdi = BelgeDosyasi.FileName.GetFileName();
                BelgeDosyasi.SaveAs(Server.MapPath("~" + DosyaYolu));

                if (TezDosyasi == null)
                {
                    TezDosyasi = db.MezuniyetBasvurulariTezDosyalaris.Add(new MezuniyetBasvurulariTezDosyalari
                    {
                        MezuniyetBasvurulariID = Basv.MezuniyetBasvurulariID,
                        RowID = Guid.NewGuid(),
                        SiraNo = Basv.MezuniyetBasvurulariTezDosyalaris.Count + 1,
                        YuklemeTarihi = DateTime.Now,
                        TezDosyaAdi = BelgeAdi,
                        TezDosyaYolu = DosyaYolu,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                    });
                }
                else
                {

                    var path = Server.MapPath("~" + TezDosyasi.TezDosyaYolu);

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
                    TezDosyasi.RowID = Guid.NewGuid();
                    TezDosyasi.TezDosyaAdi = BelgeAdi;
                    TezDosyasi.TezDosyaYolu = DosyaYolu;
                    TezDosyasi.YuklemeTarihi = DateTime.Now;
                    TezDosyasi.IslemTarihi = DateTime.Now;
                    TezDosyasi.IslemYapanID = UserIdentity.Current.Id;
                    TezDosyasi.IslemYapanIP = UserIdentity.Ip;
                }

                db.SaveChanges();
                MezuniyetBus.SendMailMezuniyetTezSablonKontrol(TezDosyasi.MezuniyetBasvurulariTezDosyaID, MailSablonTipi.Mez_TezKontrolTezDosyasiYuklendi);
                mMessage.Messages.Add("Tez Dosyası Yükleme İşlemi Başarılı");


                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return new { mMessage.IsSuccess, Messages = strView }.ToJsonResult();
        }

        [AllowAnonymous]
        public ActionResult SinavDegerlendir(Guid? UniqueID, bool? IsTezBasligiDegisti, string YeniTezBaslikTr, string YeniTezBaslikEn, bool? IsTezSanayiVeIsBirligiKapsamindaGerceklesti, bool? IsYokDrBursiyeriVar, string YokDrOncelikliAlan, int? MezuniyetSinavDurumID, string Aciklama)
        {


            var mMessage = new MmMessage();
            mMessage.IsSuccess = false;
            mMessage.Title = "Tez Sınavı Değerlendirme İşlemi";
            var DegerlendirmeDuzeltmeYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            bool IsRefresh = false;
            if (!UniqueID.HasValue)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme için gerekli benzersiz anahtar bilgisi boş gelmektedir.</span>");
            }
            else
            {
                var Komite = db.SRTaleplerJuris.Where(p => p.UniqueID == UniqueID).FirstOrDefault();
                if (Komite == null)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme işlemi yapmanız için size tanınan benzersiz anahtar bilgisi değişti veya bulunamadı!</span>");
                }
                else
                {
                    var SrTalebi = Komite.SRTalepleri;
                    var MBasvuru = SrTalebi.MezuniyetBasvurulari;
                    var JoForm = MBasvuru.MezuniyetJuriOneriFormlaris.First();
                    var SRTalepJuris = SrTalebi.SRTaleplerJuris;

                    bool IsTezDanismani = Komite.JuriTipAdi == "TezDanismani";

                    var Toplanti = Komite.SRTalepleri;


                    var ToplantiTarihi = Toplanti.Tarih.Add(Toplanti.BasSaat);
                    if (!DegerlendirmeDuzeltmeYetki && DateTime.Now < ToplantiTarihi)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + ToplantiTarihi.ToLongDateString() + " " + string.Format("{0:hh\\:mm}", Toplanti.BasSaat) + "'</b> dan önce yapılamaz!</span>");
                    }
                    else if (!DegerlendirmeDuzeltmeYetki && Komite.MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Tez izleme rapor değerlendirme işlemini daha önceden zaten yaptınız!</span>");
                    }
                    //else if (!(Komite.TIBasvuruAraRapor.DonemBaslangicYil == Donem.BaslangicYil && Komite.TIBasvuruAraRapor.DonemID == Donem.DonemID))
                    //{
                    //    mMessage.Messages.Add("<span style='color:maroon;'>Rapor değerlendirme dönemi geçtikten sonra değerlendirme işlemi yapılamaz!</span>");
                    //}
                    else
                    {
                        if (!DegerlendirmeDuzeltmeYetki)
                        {
                            if (IsTezDanismani)
                            {
                                if (!IsTezBasligiDegisti.HasValue)
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Sınavda Tez Başlığı Değişti mi?</span>");
                                }
                                else if (IsTezBasligiDegisti == true)
                                {
                                    if (YeniTezBaslikTr.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı bilgisi girilmeli</span>");
                                    }
                                    if (YeniTezBaslikEn.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı Çevirisi bilgisi girilmeli</span>");
                                    }
                                }
                                if (!IsTezSanayiVeIsBirligiKapsamindaGerceklesti.HasValue)
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Tez Sanayi ile işbirliği kapsamında mı gerçekleştirildi?</span>");
                                }
                                if (new List<int> { OgrenimTipi.Doktra, OgrenimTipi.ButunlesikDoktora }.Contains(MBasvuru.OgrenimTipKod))
                                {
                                    if (!IsYokDrBursiyeriVar.HasValue)
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>100/2000 YÖK Bursiyeri Var Mı?</span>");
                                    }
                                    if (IsYokDrBursiyeriVar == true && YokDrOncelikliAlan.IsNullOrWhiteSpace())
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Öncelikli Alt Alan Adı</span>");
                                    }
                                }
                            }
                            if (!MezuniyetSinavDurumID.HasValue || MezuniyetSinavDurumID <= MezuniyetSinavDurum.SonucGirilmedi)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez Sınavı Değerlendirme Sonucu</span>");
                            }
                            else if (MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarisiz && Aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez sınavı Değerlendirme Açıklaması</span>");
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez sınavı değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");
                        }
                        else
                        {
                            if (IsTezDanismani)
                            {

                                if (MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi)
                                {


                                    if (!IsTezBasligiDegisti.HasValue)
                                    {
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez Başlığı Değişti Mi?</span>");
                                    }
                                    else if (IsTezBasligiDegisti == true)
                                    {
                                        if (YeniTezBaslikTr.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı bilgisi girilmeli</span>");
                                        }
                                        if (YeniTezBaslikEn.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Yeni Tez Başlığı Çevirisi bilgisi girilmeli</span>");
                                        }
                                    }



                                    if (!IsTezSanayiVeIsBirligiKapsamindaGerceklesti.HasValue)
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez Sanayi ile işbirliği kapsamında mı gerçekleştirildi?</span>");

                                    if (!MezuniyetSinavDurumID.HasValue || MezuniyetSinavDurumID <= MezuniyetSinavDurum.SonucGirilmedi)
                                        mMessage.Messages.Add("<span style='color:maroon;'>Tez Sınavı Değerlendirme Sonucu</span>");



                                    if (new List<int> { OgrenimTipi.Doktra, OgrenimTipi.ButunlesikDoktora }.Contains(MBasvuru.OgrenimTipKod))
                                    {
                                        if (!IsYokDrBursiyeriVar.HasValue)
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>100/2000 YÖK Bursiyeri Var Mı?</span>");
                                        }
                                        if (IsYokDrBursiyeriVar == true && YokDrOncelikliAlan.IsNullOrWhiteSpace())
                                        {
                                            mMessage.Messages.Add("<span style='color:maroon;'>Öncelikli Alt Alan Adı</span>");
                                        }
                                    }


                                }

                            }
                            if (MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarisiz && Aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Tez sınavı Değerlendirme Açıklaması</span>");
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Tez sınavı değerlendirme işlemi başarısız. Aşağıda istenen verileri cevaplayınız.");

                        }
                    }
                    if (!mMessage.Messages.Any())
                    {
                        var Degerlendirmeler = new List<SRTaleplerJuri>();
                        foreach (var item in SRTalepJuris)
                        {
                            Degerlendirmeler.Add(new SRTaleplerJuri
                            {
                                UniqueID = item.UniqueID,
                                MezuniyetSinavDurumID = item.MezuniyetSinavDurumID
                            });
                        }
                        var Degerlendirme = Degerlendirmeler.Where(p => p.UniqueID == UniqueID.Value).First();
                        Degerlendirme.MezuniyetSinavDurumID = MezuniyetSinavDurumID;
                        if (!Degerlendirmeler.Any(a => !a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurum.SonucGirilmedi))
                        {
                            var qGroup = Degerlendirmeler.GroupBy(g => new { g.MezuniyetSinavDurumID }).Select(s => new
                            {
                                s.Key.MezuniyetSinavDurumID,
                                Count = s.Count()
                            }).OrderByDescending(o => o.Count).ToList();
                            if (qGroup.Count != 1)
                            {
                                var EnYuksekOy1 = qGroup[0];
                                var EnYuksekOy2 = qGroup[1];

                                if (EnYuksekOy1.Count == EnYuksekOy2.Count)
                                {
                                    var DegerlendirmeAdlari = db.MezuniyetSinavDurumlaris.ToList();

                                    var DegerlendirmeSonuclari = (from s in qGroup
                                                                  join da in DegerlendirmeAdlari on s.MezuniyetSinavDurumID equals da.MezuniyetSinavDurumID
                                                                  select new
                                                                  {
                                                                      s.MezuniyetSinavDurumID,
                                                                      da.MezuniyetSinavDurumAdi,
                                                                      Count = s.Count
                                                                  }).ToList();
                                    foreach (var item in DegerlendirmeSonuclari)
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
                        var SendMailLink = false;
                        var SendSonuc = false;

                        var Juri = db.MezuniyetJuriOneriFormuJurileris.Where(p => p.MezuniyetJuriOneriFormuJuriID == Komite.MezuniyetJuriOneriFormuJuriID).First();
                        var JForm = Juri.MezuniyetJuriOneriFormlari;

                        if (IsTezDanismani && MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi && !Komite.SRTalepleri.SRTaleplerJuris.Any(a => a.IsLinkGonderildi.HasValue))
                        {
                            SendMailLink = true;

                        }
                        if (IsTezDanismani)
                        {
                            if (JForm.IsTezSanayiVeIsBirligiKapsamindaGerceklesti != IsTezSanayiVeIsBirligiKapsamindaGerceklesti) SendSonuc = true;
                            if (!SendSonuc)
                            {

                                if (IsTezBasligiDegisti != SrTalebi.IsTezBasligiDegisti) SendSonuc = true;
                            }
                            if (!SendSonuc)
                            {
                                if (MezuniyetSinavDurumID != Komite.MezuniyetSinavDurumID || Aciklama != Komite.Aciklama) SendSonuc = true;
                            }
                        }
                        else
                        {
                            if (!SendSonuc)
                            {
                                if (MezuniyetSinavDurumID != Komite.MezuniyetSinavDurumID || Aciklama != Komite.Aciklama) SendSonuc = true;
                            }
                        }

                        if (IsTezDanismani)
                        {
                            SrTalebi.IsTezBasligiDegisti = IsTezBasligiDegisti;

                            if (SrTalebi.IsTezBasligiDegisti == true)
                            {
                                SrTalebi.YeniTezBaslikTr = SrTalebi.IsTezBasligiDegisti == true ? YeniTezBaslikTr : null;
                                SrTalebi.YeniTezBaslikEn = SrTalebi.IsTezBasligiDegisti == true ? YeniTezBaslikEn : null;
                            }

                            JForm.IsTezSanayiVeIsBirligiKapsamindaGerceklesti = IsTezSanayiVeIsBirligiKapsamindaGerceklesti;
                            SrTalebi.IsYokDrBursiyeriVar = IsYokDrBursiyeriVar;
                            SrTalebi.YokDrOncelikliAlan = YokDrOncelikliAlan;
                        }
                        Komite.MezuniyetSinavDurumID = MezuniyetSinavDurumID;
                        Komite.Aciklama = Aciklama;
                        Komite.DegerlendirmeIslemTarihi = DateTime.Now;
                        Komite.DegerlendirmeIslemYapanIP = UserIdentity.Ip;
                        Komite.DegerlendirmeYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;

                        Komite.IslemTarihi = DateTime.Now;
                        Komite.IslemYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;
                        Komite.IslemYapanIP = UserIdentity.Ip;
                        db.SaveChanges();
                        LogIslemleri.LogEkle("SRTalepleriJuri", IslemTipi.Update, Komite.ToJson());
                        mMessage.IsSuccess = true;
                        if (SendMailLink)
                        {
                            var Messages = MezuniyetBus.SendMailMezuniyetDegerlendirmeLink(Komite.SRTalepID, null, true);
                            if (IsTezDanismani || DegerlendirmeDuzeltmeYetki)
                            {
                                if (Messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme Linki Jüri Üyelerine Gönderildi.");
                                }
                                else
                                {
                                    mMessage.Messages.AddRange(Messages.Messages);
                                    mMessage.Messages.Add("Değerlendirmeniz geri alınmıştır, Lütfen tekrar değerlendirme yapınız.");
                                    mMessage.IsSuccess = false;
                                    IsRefresh = true;
                                    Komite.MezuniyetSinavDurumID = null;
                                    Komite.Aciklama = null;
                                    Komite.DegerlendirmeIslemTarihi = null;
                                    Komite.DegerlendirmeIslemYapanIP = null;
                                    Komite.DegerlendirmeYapanID = null;
                                    db.SaveChanges();
                                }
                            }
                        }
                        else mMessage.Messages.Add("Değerlendirme işlemi tamamlandı.");


                        var IsDegerlendirmeTamam = !Komite.SRTalepleri.SRTaleplerJuris.Any(a => !a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurum.SonucGirilmedi);
                        SrTalebi = Komite.SRTalepleri;
                        SRTalepJuris = SrTalebi.SRTaleplerJuris;
                        if (IsDegerlendirmeTamam)
                        {

                            var qGroup = SRTalepJuris.GroupBy(g => new { g.MezuniyetSinavDurumID }).Select(s => new
                            {
                                s.Key.MezuniyetSinavDurumID,
                                Count = s.Count()
                            }).OrderByDescending(o => o.Count).ToList();

                            SrTalebi.JuriSonucMezuniyetSinavDurumID = qGroup.First().MezuniyetSinavDurumID;
                            SrTalebi.IsOyBirligiOrCouklugu = qGroup.Count == 1;
                            db.SaveChanges();
                            if (SendSonuc)
                            {
                                var Messages = MezuniyetBus.SendMailMezuniyetDegerlendirmeLink(SrTalebi.SRTalepID, null, false);
                                Messages.IsSuccess = true;

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
                                    SrTalebi.DegerlendirmeSonucMailTarihi = DateTime.Now;
                                }
                                db.SaveChanges();
                            }
                        }
                        else
                        {
                            SrTalebi.IsOyBirligiOrCouklugu = null;
                            SrTalebi.JuriSonucMezuniyetSinavDurumID = MezuniyetSinavDurum.SonucGirilmedi;
                            if (SRTalepJuris.Any(a => a.MezuniyetSinavDurumID.HasValue && a.MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi))
                            {
                                SrTalebi.RSBaslatildiMailGonderimTarihi = DateTime.Now;
                            }
                            else
                            {
                                SrTalebi.RSBaslatildiMailGonderimTarihi = null;
                            }
                            db.SaveChanges();
                        }
                        LogIslemleri.LogEkle("SRTalepJuris", IslemTipi.Update, Komite.ToJson());

                    }
                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { mMessage.IsSuccess, Messages = strView, IsRefresh }, "application/json", JsonRequestBehavior.AllowGet);
        }


        [AllowAnonymous]
        public ActionResult GSinavDegerlendir(Guid? UniqueID)
        {

            var Model = (from s in db.SRTalepleris.Where(p => p.SRTaleplerJuris.Any(a2 => a2.UniqueID == UniqueID.Value))
                         join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                         join mb in db.MezuniyetBasvurularis on s.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                         join sal in db.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                         from defSl in def1.DefaultIfEmpty()
                         join hg in db.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                         join d in db.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                         join sd in db.MezuniyetSinavDurumlaris on (s.MezuniyetSinavDurumID ?? MezuniyetSinavDurum.SonucGirilmedi) equals sd.MezuniyetSinavDurumID into def2
                         from defSD in def2.DefaultIfEmpty()
                         join sdj in db.MezuniyetSinavDurumlaris on (s.JuriSonucMezuniyetSinavDurumID ?? MezuniyetSinavDurum.SonucGirilmedi) equals sdj.MezuniyetSinavDurumID into def3
                         from defsdj in def3.DefaultIfEmpty()
                         let jof = db.MezuniyetJuriOneriFormlaris.Where(p => p.MezuniyetBasvurulariID == mb.MezuniyetBasvurulariID).FirstOrDefault()
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
                             SDurumAdi = defSD != null ? defSD.MezuniyetSinavDurumAdi : "",
                             SDurumListeAdi = defSD != null ? defSD.MezuniyetSinavDurumAdi : "",
                             SClassName = defSD != null ? defSD.ClassName : "",
                             SColor = defSD != null ? defSD.Color : "",
                             SRDurumID = s.SRDurumID,
                             DurumAdi = d.DurumAdi,
                             DurumListeAdi = d.DurumAdi,
                             ClassName = d.ClassName,
                             Color = d.Color,
                             SRDurumAciklamasi = s.SRDurumAciklamasi,
                             JuriSonucMezuniyetSinavDurumID = s.JuriSonucMezuniyetSinavDurumID,
                             IsOyBirligiOrCouklugu = s.IsOyBirligiOrCouklugu,
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

                             SRTaleplerJuris = s.SRTaleplerJuris.Where(p => p.UniqueID == UniqueID.Value).ToList(),
                             IsSonSRTalebi = !mb.SRTalepleris.Any(a => a.SRTalepID > s.SRTalepID),
                             SRTalepleriBezCiltFormus = s.SRTalepleriBezCiltFormus,
                         }).Where(p => p.IsSonSRTalebi).OrderByDescending(o => o.SRTalepID).First();
            var SinavJuri = Model.SRTaleplerJuris.First();
            var JuriOneriFormuJuri = db.MezuniyetJuriOneriFormuJurileris.Where(p => p.MezuniyetJuriOneriFormuJuriID == SinavJuri.MezuniyetJuriOneriFormuJuriID).First();
            var JuriOneriFormu = JuriOneriFormuJuri.MezuniyetJuriOneriFormlari;
            var Basvuru = JuriOneriFormu.MezuniyetBasvurulari;
            Model.IsOncedenUzatmaAlindi =
                Basvuru.SRTalepleris.Any(a => a.SRTalepID <= Model.SRTalepID && a.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma);
            Model.ResimAdi = Basvuru.Kullanicilar.ResimAdi;
            var OgtrenimTip = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == Basvuru.OgrenimTipKod).First();
            ViewBag.OgtrenimTipi = OgtrenimTip;
            ViewBag.MezuniyetBasvurulari = Basvuru;
            ViewBag.JuriOneriFormu = JuriOneriFormu;
            ViewBag.UniqueID = UniqueID;
            return View(Model);
        }

        [Authorize]
        public ActionResult DegerlendirmeLinkiGonder(int SRTalepID, Guid? UniqueID, string EMail, bool IsJuriEmailGuncellensin, bool IsYeniLink)
        {
            var mMessage = new MmMessage();
            mMessage.IsSuccess = false;
            mMessage.Title = "Tez Sınavı Değerlendirme Linki Gönderme İşlemi";
            var SrTalep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();
            var Basvuru = SrTalep.MezuniyetBasvurulari;
            var IsDanismani = Basvuru.TezDanismanID == UserIdentity.Current.Id;
            var SinavDuzeltmeYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var UzatmaSonrasiOgrenciTaahhutu = SrTalep.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma ? SrTalep.IsOgrenciUzatmaSonrasiOnay.HasValue : false;
            var Uye = SrTalep.SRTaleplerJuris.Where(p => p.UniqueID == UniqueID).FirstOrDefault();

            if (!SinavDuzeltmeYetki && Basvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Değerlendirme Linki Göndermek İçin Yetkili Değilsiniz.");
            }
            else if (UzatmaSonrasiOgrenciTaahhutu)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Öğrenci Uzatma işleminden sonra Tez Teslim Taahhütü yaptığı için Jüri Üyesine değerlendirme linki gönderilemez");
            }
            else if (SrTalep.MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Değerlendirme işlemi tüm Jüri üyeler tarafından tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
            }
            else if (EMail.IsNullOrWhiteSpace())
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("E-Posta Giriniz");
            }
            else if (EMail.ToIsValidEmail())
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("E-Posta Formatı Uygun Değildir.");
            }
            else
            {
                if (UniqueID.HasValue)
                {

                    if (Uye == null) mMessage.Messages.Add("Değerlendirme Linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı Yenileyip Tekrar Deneyiniz.");
                    else
                    {
                        if (IsJuriEmailGuncellensin)
                        {
                            Uye.Email = EMail;
                            db.SaveChanges();
                        }
                    }

                }
                var Messages = MezuniyetBus.SendMailMezuniyetDegerlendirmeLink(SrTalep.SRTalepID, UniqueID, true, IsYeniLink, EMail);
                if (Messages.IsSuccess)
                {
                    SrTalep.JuriSonucMezuniyetSinavDurumID = null;
                    db.SaveChanges();
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Değerlendirme Linki Jüri Üyesine Gönderildi.");

                }
                else
                {
                    mMessage.Messages.AddRange(Messages.Messages);

                }
            }
            var strView = mMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage) : "";
            return new { mMessage, MessageView = strView, MessageType = (mMessage.IsSuccess ? "success" : "error") }.ToJsonResult();
        }
        [Authorize]
        public ActionResult DegerlendirmeLinkView(Guid? UniqueID)
        {
            var model = db.SRTaleplerJuris.Where(p => p.UniqueID == UniqueID).First();
            return View(model);
        }

        public ActionResult OgrenciUzatmaOnayKayit(int SRTalepID, bool? IsOgrenciUzatmaSonrasiOnay)
        {
            var mmMessage = new MmMessage();

            mmMessage.Title = "Mezuniyet başvurusu danışman onay işlemi";
            var SrTalep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();
            var KayitYetki = RoleNames.GelenBasvurularKayit.InRole();

            if (!KayitYetki)
            {
                if (SrTalep.MezuniyetBasvurulari.KullaniciID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz!");
                }

            }
            if (SrTalep.IsDanismanUzatmaSonrasiOnay.HasValue)
            {
                mmMessage.Messages.Add("Danışman taahhüt onayı yapıldı. Bu işlemi yapamazsınız.");
            }
            if (!mmMessage.Messages.Any())
            {

                bool SendMail = false;
                if (mmMessage.Messages.Count == 0)
                {

                    if (IsOgrenciUzatmaSonrasiOnay != SrTalep.IsOgrenciUzatmaSonrasiOnay)
                    {
                        SendMail = true;
                    }
                    SrTalep.IsOgrenciUzatmaSonrasiOnay = IsOgrenciUzatmaSonrasiOnay;
                    SrTalep.OgrenciOnayTarihi = DateTime.Now;

                    db.SaveChanges();
                    LogIslemleri.LogEkle("SRTalepleri", IslemTipi.Update, SrTalep.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(IsOgrenciUzatmaSonrasiOnay.HasValue ? (IsOgrenciUzatmaSonrasiOnay.Value ? "Tahhüt Onaylandı." : "Taahhüt Ret Edildi.") : "Taahhüt İşlemi Geril Alındı.");
                    if (SendMail && false)
                    {
                        #region sendMail
                        var SablonTipID = SrTalep.IsOgrenciUzatmaSonrasiOnay == true ? MailSablonTipi.Mez_DanismanOnayladiOgrenci : MailSablonTipi.Mez_DanismanOnaylamadiOgrenci;
                        var Sablonlar = db.MailSablonlaris.Where(p => p.MailSablonTipID == SablonTipID && p.EnstituKod == SrTalep.EnstituKod).ToList();
                        var MB = SrTalep.MezuniyetBasvurulari;
                        var BasvuruSurec = MB.MezuniyetSureci;
                        var Enstitu = BasvuruSurec.Enstituler;

                        var mModel = new List<SablonMailModel>();

                        mModel.Add(new SablonMailModel
                        {

                            AdSoyad = MB.Ad + " " + MB.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = SrTalep.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = SablonTipID,
                        });

                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == MB.TezDanismanID).First();

                        foreach (var item in mModel)
                        {
                            var BasvuruDonemAdi = BasvuruSurec.BaslangicYil + " " + BasvuruSurec.BitisYil + " / " + BasvuruSurec.Donemler.DonemAdi;
                            var EnstituL = Enstitu;

                            item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                            item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                            if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                            var ParamereDegerleri = new List<MailReplaceParameterDto>();
                            if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                            if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                                ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                            if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "BasvuruDonemAdi", Value = BasvuruDonemAdi });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                                ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = Danisman.Ad + " " + Danisman.Soyad });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = Danisman.Unvanlar.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                                ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = MB.OgrenciNo });
                            if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                                ParamereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = MB.Ad + " " + MB.Soyad });

                            var Attachs = new List<System.Net.Mail.Attachment>();

                            var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                            var snded = MailManager.SendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, Attachs);
                            if (snded)
                            {
                                var kModel = new GonderilenMailler();
                                kModel.Tarih = DateTime.Now;
                                kModel.EnstituKod = Enstitu.EnstituKod;
                                kModel.MesajID = null;
                                kModel.IslemTarihi = DateTime.Now;
                                kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                                if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                                kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                                kModel.IslemYapanIP = UserIdentity.Ip;
                                kModel.Aciklama = item.Sablon.Sablon ?? "";
                                kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                                kModel.Gonderildi = true;
                                kModel.GonderilenMailEkleris = Attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                                kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                                db.GonderilenMaillers.Add(kModel);
                                db.SaveChanges();
                            }
                        }

                        #endregion 
                    }



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
                var kayit = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == id).FirstOrDefault();
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
                        db.MezuniyetBasvurulariTezDosyalaris.RemoveRange(kayit.MezuniyetBasvurulariTezDosyalaris);
                        db.MezuniyetJuriOneriFormlaris.RemoveRange(kayit.MezuniyetJuriOneriFormlaris);
                        db.SRTalepleris.RemoveRange(kayit.SRTalepleris);
                    }

                    db.MezuniyetBasvurularis.Remove(kayit);
                    db.SaveChanges();
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
                    Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "Mezuniyet/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}