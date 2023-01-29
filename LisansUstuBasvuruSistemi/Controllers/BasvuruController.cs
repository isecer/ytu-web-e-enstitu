using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using Newtonsoft.Json.Linq;
using System.Xml;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class BasvuruController : Controller
    {


        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD, int? BelgeDetailBasvuruID = null)
        {

            return Index(new fmBasvurular() { PageSize = 10, BelgeDetailBasvuruID = BelgeDetailBasvuruID }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmBasvurular model, string EKD)
        {
            var nowDate = DateTime.Now;
            string EnstituKod = Management.getSelectedEnstitu(EKD);
            int? KullaniciID = UserIdentity.Current.Id;
            var Yetki = RoleNames.BasvuruSureciKayit.InRoleCurrent() || RoleNames.GelenBasvurularKayit.InRoleCurrent();

            if (Yetki && model.BelgeDetailBasvuruID.HasValue)
            {
                KullaniciID = null;
            }

            var q = from s in db.Basvurulars.Where(p => p.KullaniciID == (KullaniciID ?? p.KullaniciID) && p.BasvuruID == (model.BelgeDetailBasvuruID ?? p.BasvuruID))
                    join en in db.Enstitulers on new { s.BasvuruSurec.EnstituKod } equals new { en.EnstituKod }
                    join bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) on s.BasvuruSurecID equals bs.BasvuruSurecID
                    join d in db.Donemlers on new { bs.DonemID } equals new { d.DonemID }
                    join ktip in db.KullaniciTipleris on new { s.Kullanicilar.KullaniciTipID } equals new { ktip.KullaniciTipID }
                    join dr in db.BasvuruDurumlaris on new { s.BasvuruDurumID } equals new { dr.BasvuruDurumID }
                    where bs.Enstituler.EnstituKisaAd.Contains(EKD)
                    select new frBasvurular
                    {
                        KullaniciID = s.KullaniciID,
                        BasvuruSurecID = s.BasvuruSurecID,
                        BasvuruID = s.BasvuruID,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        BasvuruSurecAdi = bs.BaslangicYil + "/" + bs.BitisYil + " " + d.DonemAdi,
                        BasTar = bs.BaslangicTarihi,
                        BitTar = bs.BitisTarihi,
                        ResimAdi = s.Kullanicilar.ResimAdi,
                        TcPasaPortNo = s.Kullanicilar.TcKimlikNo != null ? s.Kullanicilar.TcKimlikNo : s.Kullanicilar.PasaportNo,
                        AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        KullaniciTipID = s.Kullanicilar.KullaniciTipID,
                        KullaniciTipAdi = ktip.KullaniciTipAdi,
                        TercihSayisi = s.BasvurularTercihleris.Count,
                        BasvuruDurumID = s.BasvuruDurumID,
                        BasvuruDurumAdi = dr.BasvuruDurumAdi,
                        DurumClassName = dr.ClassName,
                        DurumColor = dr.Color,
                        BasvuruTarihi = s.BasvuruTarihi,
                        BasvuruDurumAciklamasi = s.BasvuruDurumAciklamasi,
                        IsNotDuzelt = s.BasvuruSurec.AGNOGirisBaslangicTarihi.HasValue && s.LUniversiteID == Management.UniversiteYtuKod ? (s.BasvuruSurec.AGNOGirisBaslangicTarihi.Value <= nowDate && s.BasvuruSurec.AGNOGirisBitisTarihi.Value >= nowDate && s.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans)) : false,
                    };
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.BasvuruSurecID.HasValue) q = q.Where(p => p.BasvuruSurecID == model.BasvuruSurecID.Value);
            //if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID.Value);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcPasaPortNo == model.AdSoyad || p.KullaniciTipAdi.Contains(model.AdSoyad));
            if (model.BasvuruDurumID.HasValue) q = q.Where(p => p.BasvuruDurumID == model.BasvuruDurumID.Value);
            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            //IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.BasvuruTarihi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            var qdata = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToList();
            model.Data = qdata;
            model.KullaniciID = qdata.Count > 0 ? qdata.First().KullaniciID : (int?)null;
            ViewBag.IndexModel = IndexModel;
            ViewBag.BasvuruSurecID = new SelectList(Management.getbasvuruSurecleri(EnstituKod, BasvuruSurecTipi.LisansustuBasvuru, true), "Value", "Caption", model.BasvuruSurecID);
            ViewBag.BasvuruDurumID = new SelectList(Management.cmbBasvuruDurumListe(true, true), "Value", "Caption", model.BasvuruDurumID);

            return View(model);
        }
        [HttpGet]
        public ActionResult getbbModel(int? KullaniciID, string EKD)
        {
            var Yetki = RoleNames.BasvuruSureciKayit.InRoleCurrent() || RoleNames.GelenBasvurularKayit.InRoleCurrent();
            if (!Yetki && KullaniciID.HasValue)
            {
                KullaniciID = UserIdentity.Current.Id;
            }
            if (!KullaniciID.HasValue) KullaniciID = UserIdentity.Current.Id;

            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var bbModel = new BasvuruBilgiModel();
            var BasvuruSurecID = Management.getAktifBasvuruSurecID(_EnstituKod, BasvuruSurecTipi.LisansustuBasvuru);
            bbModel.AktifSurecID = BasvuruSurecID ?? 0;
            bbModel.SistemBasvuruyaAcik = BasvuruSurecID.HasValue;
            bbModel.BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID.Value).FirstOrDefault();
            if (bbModel.BasvuruSurec != null)
                bbModel.DonemAdi = db.Donemlers.Where(p => p.DonemID == bbModel.BasvuruSurec.DonemID).First().DonemAdi;

            bbModel.Enstitü = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod).First();
            ViewBag.Kullanici = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
            return View(bbModel);

        }
        public ActionResult BasvuruYap(int? BasvuruID, int? KullaniciID = null, string EnstituKod = "", string EKD = "")
        {
            var model = new kmBasvuru();
            var _MmMessage = new MmMessage();
            if (EnstituKod.IsNullOrWhiteSpace()) EnstituKod = Management.getSelectedEnstitu(EKD);
            model.EnstituKod = EnstituKod;
            var IsGelenBasvuruYetki = RoleNames.GelenBasvurularKayit.InRoleCurrent();


            if (BasvuruID.HasValue || KullaniciID.HasValue)
            {
                if (KullaniciID.HasValue)
                    if (IsGelenBasvuruYetki == false)
                        KullaniciID = UserIdentity.Current.Id;
                if (BasvuruID.HasValue)
                {
                    var basvuru = db.Basvurulars.Where(p => p.BasvuruID == BasvuruID.Value).FirstOrDefault();
                    model.EnstituKod = EnstituKod = basvuru.BasvuruSurec.EnstituKod;
                    if (KullaniciID.HasValue == false) KullaniciID = basvuru.KullaniciID;
                }
            }
            else
            {
                KullaniciID = UserIdentity.Current.Id;
            }

            var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).FirstOrDefault();
            _MmMessage = Management.getAktifBasvurSurecKontrol(model.EnstituKod, BasvuruSurecTipi.LisansustuBasvuru, KullaniciID, BasvuruID);

            if (_MmMessage.IsSuccess)
            {
                if (BasvuruID.HasValue)
                {
                    model = Management.getSecilenBasvuru(BasvuruID.Value);
                    model.ResimAdi = kul.ResimAdi;
                    KullaniciID = model.KullaniciID;

                }
                else
                {
                    model.BasvuruSurecID = Management.getAktifBasvuruSurecID(model.EnstituKod, BasvuruSurecTipi.LisansustuBasvuru).Value;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = KullaniciID.Value;
                    model.KullaniciTipID = kul.KullaniciTipID;
                    model.ResimAdi = kul.ResimAdi;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.CinsiyetID = kul.CinsiyetID; 
                    model.TcKimlikNo = kul.TcKimlikNo;
                    model.PasaportNo = kul.PasaportNo; 
                    model.IsTel = "";
                    model.EvTel = "";
                    model.CepTel = kul.CepTel;
                    model.EMail = kul.EMail;
                    model.Adres = kul.Adres;
                    model.Adres2 = "";
                }
                var surec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == model.BasvuruSurecID).First();
                model.DonemAdi = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi;
                model.ODurumIstensin = surec.AGNOGirisBaslangicTarihi.HasValue;
                model.SetSelectedStep = 1;
                model.IsYerli = kul.KullaniciTipleri.Yerli;
                model.KullaniciTipAdi = kul.KullaniciTipleri.KullaniciTipAdi;
                ViewBag._MmMessage = _MmMessage;
                ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);
                ViewBag.DogumYeriKod = new SelectList(Management.cmbSehirler(true), "Value", "Caption", model.DogumYeriKod);
                ViewBag.NufusilIlceKod = new SelectList(Management.cmbSehirler(true), "Value", "Caption", model.NufusilIlceKod);
                ViewBag.SehirKod = new SelectList(Management.cmbSehirler(true), "Value", "Caption", model.SehirKod);
                ViewBag.UyrukKod = new SelectList(Management.cmbUyruk(true), "Value", "Caption", model.UyrukKod);
                ViewBag.LUniversiteID = new SelectList(Management.cmbGetAktifUniversiteler(true), "Value", "Caption", model.LUniversiteID);
                ViewBag.LOgrenciBolumID = new SelectList(Management.cmbGetOgrenciBolumleri(model.EnstituKod, true), "Value", "Caption", model.LOgrenciBolumID);
                ViewBag.LOgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu2(true, IsBasvurudaGozuksun: true), "Value", "Caption", model.LOgrenimDurumID);
                ViewBag.LNotSistemID = new SelectList(Management.cmbGetNotSistemleri(true), "Value", "Caption", model.LNotSistemID);
                ViewBag.YLUniversiteID = new SelectList(Management.cmbGetAktifUniversiteler(true), "Value", "Caption", model.YLUniversiteID);
                ViewBag.YLOgrenciBolumID = new SelectList(Management.cmbGetOgrenciBolumleri(model.EnstituKod, true), "Value", "Caption", model.YLOgrenciBolumID);
                ViewBag.YLNotSistemID = new SelectList(Management.cmbGetNotSistemleri(true), "Value", "Caption", model.YLNotSistemID);
                ViewBag.OgrenimTipKod = new SelectList(Management.cmbGetAktifOgrenimTipleriGrup(model.BasvuruSurecID, true), "Value", "Caption", "");
                ViewBag.SubOgrenimTipKod = new SelectList(Management.cmbGetAktifSubOgrenimTipleri(model.BasvuruSurecID, "-", false), "Value", "Caption", "");
                ViewBag.AnabilimdaliKod = new SelectList(Management.cmbGetAktifBolumlerX(0, 0), "Value", "Caption", "");
                ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlarX(0, model.BasvuruSurecID, 0, 0), "Value", "Caption");
                ViewBag.ASinavTipleri = new SelectList(Management.cmbGetBasvuruSurecAktifSinavTipTipleri(model.BasvuruSurecID, SinavTipGrup.Ales_Gree, true), "Value", "Caption");
                ViewBag.DSinavTipleri = new SelectList(Management.cmbGetBasvuruSurecAktifSinavTipTipleri(model.BasvuruSurecID, SinavTipGrup.DilSinavlari, true), "Value", "Caption");
                ViewBag.TSinavTipleri = new SelectList(Management.cmbGetBasvuruSurecAktifSinavTipTipleri(model.BasvuruSurecID, SinavTipGrup.Tomer, true), "Value", "Caption");
                ViewBag.BasvuruDurumID = new SelectList(Management.cmbBasvuruDurum(true, IsGelenBasvuruYetki), "Value", "Caption", model.BasvuruDurumID);
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, _MmMessage.Messages.ToArray());
                return RedirectToAction("Index");
            }
            if (model.BasvuruID > 0)
            {
                ViewBag.BasvuruDurumu = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == model.BasvuruDurumID).Select(s => new basvuruDurumModel { BasvuruDurumID = s.BasvuruDurumID, ClassName = s.ClassName, Color = s.Color, DurumAdi = s.BasvuruDurumAdi }).First();
            }
            else
            {
                ViewBag.BasvuruDurumu = new basvuruDurumModel
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
        public ActionResult BasvuruYap(kmBasvuru kModel)
        {
            var stps = new List<int>();
            var _MmMessage = new MmMessage();
            bool ogrenimDurumuIstensin = false;
            var kYetki = RoleNames.GelenBasvurularKayit.InRoleCurrent();
            if (kYetki == false) { kModel.KullaniciID = UserIdentity.Current.Id; }
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).First();
            kModel.EnstituKod = bsurec.EnstituKod;
            kModel.DonemAdi = bsurec.BaslangicYil + "/" + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
            _MmMessage = Management.getAktifBasvurSurecKontrol(kModel.EnstituKod, BasvuruSurecTipi.LisansustuBasvuru, kModel.KullaniciID, kModel.BasvuruID.toNullIntZero());
            if (kModel.BasvuruID <= 0)
            {
                kModel.BasvuruSurecID = Management.getAktifBasvuruSurecID(kModel.EnstituKod, BasvuruSurecTipi.LisansustuBasvuru) ?? 0;
                kModel.BasvuruTarihi = DateTime.Now;
            }
            else
            {
                var btarih = db.Basvurulars.Where(p => p.BasvuruID == kModel.BasvuruID).First();
                kModel.BasvuruTarihi = btarih.BasvuruTarihi;
            }

            var kul = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).FirstOrDefault();

            kModel.IsYerli = kul.KullaniciTipleri.Yerli;
            kModel.ResimAdi = kul.ResimAdi;
            kModel.KullaniciTipAdi = kul.KullaniciTipleri.KullaniciTipAdi;
            kModel.KullaniciTipID = kul.KullaniciTipID;
            #region Kontrol
            #region KullaniciKontrol
            var kmM = Management.kuKontrol(kModel);
            _MmMessage.Messages.AddRange(kmM.Messages.ToList());
            _MmMessage.MessagesDialog.AddRange(kmM.MessagesDialog.ToList());
            if (_MmMessage.Messages.Count > 0) stps.Add(1);
            #endregion

            #region OB_TercihKontrol

            var kmOb = Management.obKontrol(kModel);
            _MmMessage.Messages.AddRange(kmOb.Messages.ToList());
            _MmMessage.MessagesDialog.AddRange(kmOb.MessagesDialog.ToList());
            if (kmOb.Messages.Count > 0) stps.Add(2);



            var qUn = kModel._UniqueID.Select((s, inx) => new { Index = inx, UniqueID = s }).ToList();
            var qsNo = kModel._tSiraNo.Select((s, inx) => new { Index = inx, SiraNo = s }).ToList();
            var qOt = kModel._OgrenimTipKod.Select((s, inx) => new { Index = inx, OgrenimTipKod = s }).ToList();
            var qPk = kModel._ProgramKod.Select((s, inx) => new { Index = inx, ProgramKod = s }).ToList();
            var qIng = kModel._Ingilizce.Select((s, inx) => new { Index = inx, Ingilizce = s }).ToList();
            var qYLb = kModel._YLBilgiIste.Select((s, inx) => new { Index = inx, YlBilgiIste = s }).ToList();
            var qAt = kModel._AlanTipID.Select((s, inx) => new { Index = inx, AlanTipID = s }).ToList();

            var Prl = db.Programlars.Where(p => kModel._ProgramKod.Contains(p.ProgramKod)).ToList();
            var qtercih = (from s in qsNo
                           join un in qUn on s.Index equals un.Index
                           join ot in qOt on s.Index equals ot.Index
                           join pk in qPk on s.Index equals pk.Index
                           join ing in qIng on s.Index equals ing.Index
                           join ylb in qYLb on s.Index equals ylb.Index
                           join aik in qAt on s.Index equals aik.Index
                           join prl in Prl on pk.ProgramKod equals prl.ProgramKod
                           select new
                           {
                               s.Index,
                               un.UniqueID,
                               s.SiraNo,
                               ot.OgrenimTipKod,
                               ing.Ingilizce,
                               ylb.YlBilgiIste,
                               aik.AlanTipID,
                               pk.ProgramKod,
                               prl.ProgramAdi,
                               prl.Ucretli,
                               prl.Ucret
                           }).ToList();


            if (qtercih.Count == 0)
            {
                _MmMessage.Messages.Add("Kayıt işlemini yapabilemk için en az 1 tercihte bulunmanız gerekmektedir!");
                stps.Add(2);
            }
            else
            {
                bool succes = true;
                var qgrup = (from s in qtercih
                             group new { s.OgrenimTipKod, s.ProgramKod } by new { s.OgrenimTipKod, s.ProgramKod, s.ProgramAdi } into g1
                             select new
                             {
                                 g1.Key.OgrenimTipKod,
                                 g1.Key.ProgramKod,
                                 g1.Key.ProgramAdi,
                                 Count = g1.Where(p => p.ProgramKod == g1.Key.ProgramKod && p.OgrenimTipKod == g1.Key.OgrenimTipKod).Count()
                             }).ToList();

                foreach (var item in qgrup.Where(p => p.Count > 1))
                {
                    _MmMessage.Messages.Add("Eklemeye çalıştığınız program zaten eklidir. tekrar eklenemez!");
                    _MmMessage.Messages.Add("Program: " + item.ProgramAdi);
                    succes = false;
                }
                if (succes)
                {
                    var TercihlerOgrenimTipKods = qtercih.Select(s => s.OgrenimTipKod).ToList();
                    foreach (var item in qtercih)
                    {

                        var alanKotaKontrol = Management.AlanKontrol(kModel.BasvuruSurecID, kModel.LOgrenciBolumID.Value, kModel.YLOgrenciBolumID, item.OgrenimTipKod, item.ProgramKod, kModel.KullaniciID, kModel.BasvuruID);

                        if (alanKotaKontrol.Kota <= 0)
                        {
                            _MmMessage.Messages.Add("Başvurunuzda kontenjanı bulunmayan tercih ekli olduğu için başvurunuz tamamlanamadı! Lütfen başvurunuzdan tercihi kaldırıp duyurulan kontenjan bilgisine uygun bir tercih ekleyiniz.");
                            succes = false;
                        }
                        else if (alanKotaKontrol.AlanDisiProgramKisitlamasiVar)
                        {
                            _MmMessage.Messages.Add(alanKotaKontrol.AlanDisiProgramKisitlamasiMsg);
                            succes = false;
                            foreach (var ak in alanKotaKontrol.AlertInputNames)
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = ak });

                        }



                        bool AyniProgramBasvurusu = false;
                        if (bsurec.Kota_BasvuruSurecKontrolTipID == KotaHesapTipleri.SeciliBasvuruSureci)
                        {
                            AyniProgramBasvurusu = db.Basvurulars.Any(p => p.BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru && p.KullaniciID == kModel.KullaniciID && p.BasvuruSurecID == kModel.BasvuruSurecID && p.BasvurularTercihleris.Any(a => a.ProgramKod == item.ProgramKod && a.BasvuruID != kModel.BasvuruID));
                            if (AyniProgramBasvurusu)
                            {
                                _MmMessage.Messages.Add("Eklemeye çalıştığınız program zaten eklidir. tekrar eklenemez!");
                                _MmMessage.Messages.Add("Program: " + item.ProgramAdi);
                                succes = false;
                            }
                        }
                        else
                        {
                            AyniProgramBasvurusu = db.Basvurulars.Any(p => p.BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru && p.KullaniciID == kModel.KullaniciID && (p.BasvuruSurec.BaslangicYil == bsurec.BaslangicYil && p.BasvuruSurec.BitisYil == bsurec.BitisYil && p.BasvuruSurec.DonemID == bsurec.DonemID) && p.BasvurularTercihleris.Any(a => a.ProgramKod == item.ProgramKod && a.BasvuruID != kModel.BasvuruID));
                            if (AyniProgramBasvurusu)
                            {
                                _MmMessage.Messages.Add("Eklemeye çalıştığınız program bu başvuru sürecinde başka bir başvurunuzda kullandığınız için tekrar tercih olarak ekleyemezsiniz!");
                                _MmMessage.Messages.Add("Program: " + item.ProgramAdi);
                                succes = false;
                            }
                        }

                        if (AyniProgramBasvurusu == false)
                        {
                            var OgrenimTipLng = db.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.OgrenimTipKod == item.OgrenimTipKod).First();
                            var OgrenimTipiKotaModel = Management.getOgrenimTipiKotaBilgi(kModel.BasvuruSurecID, item.OgrenimTipKod, kModel.KullaniciID, BasvuruSurecTipi.LisansustuBasvuru, kModel.BasvuruID > 0 ? kModel.BasvuruID : (int?)null, TercihlerOgrenimTipKods);

                            if (OgrenimTipiKotaModel.ToplamKalanKota < 0)
                            {
                                _MmMessage.Messages.Add("Ekleyebileceğiniz toplam tercih sayısını doldurdunuz! Yeni tercih ekleyemezsiniz.");

                                succes = false;
                            }
                            else if (OgrenimTipiKotaModel.KalanKota < 0)
                            {
                                _MmMessage.Messages.Add("Seçtiğiniz öğrenim tipi için toplam tercih sayısını doldurdunuz! Yeni tercih ekleyemezsiniz.");
                                _MmMessage.Messages.Add(OgrenimTipLng.OgrenimTipAdi);
                                succes = false;
                            }
                            else if (OgrenimTipiKotaModel.FarkliOgrenimTipiEklenemez)
                            {

                                _MmMessage.Messages.Add("Bu öğrenim tipi tercih olarak seçilecekse bu başvuru sürecinde (" + OgrenimTipiKotaModel.FarkliOgrenimTipEklenemezAds + ") öğrenim tipi ile başvurunuzun olmaması gerekmektedir!");
                                _MmMessage.Messages.Add(OgrenimTipLng.OgrenimTipAdi);
                                succes = false;
                            }
                        }
                    }
                }
                if (succes)
                {
                    if (bsurec.AGNOGirisBaslangicTarihi.HasValue) //agno giriş süreci belirlenmişse ve lisans öğrenimini ytu de yapmışsa ve TYL başvurusu yapıyorsa öğrenim durumu seçimini kontrol et
                    {
                        if (kModel.LUniversiteID == Management.UniversiteYtuKod && qtercih.Any(a => a.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans))
                        {
                            ogrenimDurumuIstensin = true;
                            if (kModel.LOgrenimDurumID.HasValue == false)
                            {
                                _MmMessage.Messages.Add("Öğrenim durumunuzu seçiniz");

                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LOgrenimDurumID" });
                                succes = false;
                            }
                            else
                            {
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LOgrenimDurumID" });
                            }
                        }
                    }
                }

                if (succes)
                {
                    var qtercihler = (from s in qtercih
                                      select new CmbMultyTypeDto { Value = s.OgrenimTipKod, ValueB = s.Ingilizce, ValueS2 = s.ProgramKod }).ToList();

                    kModel.AlesIstensinmi = Management.cmbGetdAktifSinavlar(qtercihler, kModel.BasvuruSurecID, SinavTipGrup.Ales_Gree, true).Count > 0;
                    kModel.DilIstensinmi = Management.cmbGetdAktifSinavlar(qtercihler, kModel.BasvuruSurecID, SinavTipGrup.DilSinavlari, true).Count > 0;
                    var TomerVar = Management.cmbGetdAktifSinavlar(qtercihler, kModel.BasvuruSurecID, SinavTipGrup.Tomer, true).Count > 0 && kModel.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci;
                    kModel.TomerIstensinmi = TomerVar;
                    kModel.LEgitimDiliIstensinMi = TomerVar;
                    kModel.YLEgitimDiliIstensinMi = kModel.YLDurum && kModel.LEgitimDiliIstensinMi;
                }
                else
                {
                    stps.Add(2);
                }
            }

            #endregion
            #region SinavBilgiKontrol
            bool stpSon = false;
            var BasvurularSinavBilgiList = new List<BasvurularSinavBilgi>();

            if (kModel.AlesIstensinmi)
            {

                if (kModel.BasvurularSinavBilgi_A.SinavTipID <= 0)
                {
                    stpSon = true;
                    _MmMessage.Messages.Add("Ales/Gre/Gmat Sınavı grubu için sınav tipi seçiniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_A.SinavTipID" });
                }
                else
                {
                    var kmMt = Management.stKontrol(
                        kModel.BasvuruSurecID,
                        kModel._OgrenimTipKod,
                        kModel._Ingilizce,
                        kModel.BasvurularSinavBilgi_A.SinavTipID,
                        kModel.BasvurularSinavBilgi_A.WsSinavYil,
                        null,
                        kModel.BasvurularSinavBilgi_A.WsSinavDonem,
                        kModel.BasvurularSinavBilgi_A.WsXmlData,
                        kModel._ProgramKod,
                        kModel.BasvurularSinavBilgi_A.SinavTarihi,
                        kModel.BasvuruTarihi,
                        kModel.BasvurularSinavBilgi_A.SubSinavAralikID,
                        kModel.BasvurularSinavBilgi_A.BasvuruSurecSubNot,
                        kModel.BasvurularSinavBilgi_A.SinavNotu);
                    _MmMessage.Messages.AddRange(kmMt.Messages.ToList());
                    _MmMessage.MessagesDialog.AddRange(kmMt.MessagesDialog.ToList());
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_A.SinavTipID" });

                    if (kmMt.Messages.Count > 0) stpSon = true;
                    else
                    {
                        //WS not doğruluk kontrol
                        var sbilgi = db.BasvuruSurecSinavTipleris.Where(p => p.SinavTipID == kModel.BasvurularSinavBilgi_A.SinavTipID && p.BasvuruSurecID == kModel.BasvuruSurecID).First();
                        var aStip = db.SinavTipleris.Where(p => p.SinavTipID == kModel.BasvurularSinavBilgi_A.SinavTipID).First();
                        kModel.BasvurularSinavBilgi_A.SinavTipKod = aStip.SinavTipKod;
                        if (sbilgi.WebService)
                        {
                            var _yil = kModel.BasvurularSinavBilgi_A.WsSinavDonem.Split('~')[0].ToInt().Value;
                            // var _Donem = kModel.BasvurularSinavBilgi_A.WsSinavDonem.Split('~')[1];
                            var SonucID = kModel.BasvurularSinavBilgi_A.WsSonucID;
                            // kModel.BasvurularSinavBilgi_A.WsSinavDonem = _Donem;
                            kModel.BasvurularSinavBilgi_A.WsSinavYil = _yil;
                            string tck = kul.KullaniciTipleri.Yerli ? kModel.TcKimlikNo : kModel.PasaportNo;
                            var snvSonuc = Management.getSinavTipSonucModel(kModel.BasvurularSinavBilgi_A.SinavTipID, kModel.BasvuruSurecID, _yil.ToString(), SonucID, tck);
                            if (kModel.BasvurularSinavBilgi_A.SinavNotu != snvSonuc.Puan)
                            {
                                string msg = "Giriş yapılan sınav notu ile ÖSYM den çekilen not birbiri ile uyuşmuyor!";
                                _MmMessage.Messages.Add(msg);
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_A.SinavTipID" });
                                stpSon = true;
                            }
                            kModel.BasvurularSinavBilgi_A.SinavTipGrupID = SinavTipGrup.Ales_Gree;
                        }

                        kModel.BasvurularSinavBilgi_A.SinavTipGrupID = SinavTipGrup.Ales_Gree;
                        BasvurularSinavBilgiList.Add(kModel.BasvurularSinavBilgi_A);

                    }
                }
            }
            if (kModel.DilIstensinmi)
            {
                if (kModel.BasvurularSinavBilgi_D.SinavTipID <= 0)
                {
                    stpSon = true;
                    _MmMessage.Messages.Add("Dil Sınavı grubu için sınav tipi seçiniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_D.SinavTipID" });
                }
                else
                {

                    var kmMt = Management.stKontrol(
                        kModel.BasvuruSurecID,
                        kModel._OgrenimTipKod,
                        kModel._Ingilizce,
                        kModel.BasvurularSinavBilgi_D.SinavTipID,
                        kModel.BasvurularSinavBilgi_D.WsSinavYil,
                        kModel.BasvurularSinavBilgi_D.SinavDilID,
                        kModel.BasvurularSinavBilgi_D.WsSinavDonem,
                        kModel.BasvurularSinavBilgi_D.WsXmlData,
                        kModel._ProgramKod,
                        kModel.BasvurularSinavBilgi_D.SinavTarihi,
                        kModel.BasvuruTarihi,
                        kModel.BasvurularSinavBilgi_D.SubSinavAralikID,
                        kModel.BasvurularSinavBilgi_D.BasvuruSurecSubNot,
                        kModel.BasvurularSinavBilgi_D.SinavNotu,
                        kModel.BasvurularSinavBilgi_D.IsTaahhutVar
                        );

                    _MmMessage.Messages.AddRange(kmMt.Messages.ToList());
                    _MmMessage.MessagesDialog.AddRange(kmMt.MessagesDialog.ToList());
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_D.SinavTipID" });
                    if (kmMt.Messages.Count > 0) stpSon = true;
                    else
                    {
                        //WS not doğruluk kontrol
                        var sbilgi = db.BasvuruSurecSinavTipleris.Where(p => p.SinavTipID == kModel.BasvurularSinavBilgi_D.SinavTipID && p.BasvuruSurecID == kModel.BasvuruSurecID).First();

                        var dStip = db.SinavTipleris.Where(p => p.SinavTipID == kModel.BasvurularSinavBilgi_D.SinavTipID).First();
                        kModel.BasvurularSinavBilgi_D.SinavTipKod = dStip.SinavTipKod;
                        if (sbilgi.WebService)
                        {
                            var _yil = kModel.BasvurularSinavBilgi_D.WsSinavDonem.Split('~')[0].ToInt().Value;
                            // var _Donem = kModel.BasvurularSinavBilgi_D.WsSinavDonem.Split('~')[1];
                            var SonucID = kModel.BasvurularSinavBilgi_D.WsSonucID;
                            string tck = kul.KullaniciTipleri.Yerli ? kModel.TcKimlikNo : kModel.PasaportNo;

                            var snvBW = kModel.BasvurularSinavBilgi_D.WsSinavDonem.Split('~');
                            if (dStip.WsSinavCekimTipID.HasValue && dStip.WsSinavCekimTipID.Value == WsCekimTipi.Tarih)
                            {

                                kModel.BasvurularSinavBilgi_D.WsSinavDonem = snvBW[1];
                                kModel.BasvurularSinavBilgi_D.WsSinavYil = snvBW[1].ToDate().Value.Year;

                            }
                            else
                            {
                                kModel.BasvurularSinavBilgi_D.WsSinavDonem = "";
                                kModel.BasvurularSinavBilgi_D.WsSinavYil = snvBW[0].ToInt().Value;
                            }
                            var snvSonuc = Management.getSinavTipSonucModel(kModel.BasvurularSinavBilgi_D.SinavTipID, kModel.BasvuruSurecID, _yil.ToString(), SonucID, tck);

                            if (!(kModel.BasvurularSinavBilgi_D.IsTaahhutVar == true && snvSonuc.ShowIsTaahhutVar)) //eğer sınav sonucu yok ise seçilen dönem taahhütlü ise ve taahhüt işaretlenmiş ise puan kontrolü yapılmaz.
                                if (kModel.BasvurularSinavBilgi_D.SinavNotu != snvSonuc.Puan)
                                {
                                    string msg = "Giriş yapılan Dil sınavı notu ile ÖSYM den çekilen not birbiri ile uyuşmuyor!";
                                    _MmMessage.Messages.Add(msg);
                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_D.SinavTipID" });
                                    stpSon = true;
                                }
                        }
                        kModel.BasvurularSinavBilgi_D.SinavTipGrupID = SinavTipGrup.DilSinavlari;
                        BasvurularSinavBilgiList.Add(kModel.BasvurularSinavBilgi_D);
                    }
                }
            }
            if (kModel.TomerIstensinmi)
            {

                if (kModel.BasvurularSinavBilgi_T.SinavTipID <= 0)
                {
                    stpSon = true;
                    _MmMessage.Messages.Add("Tomer sınav tipi seçiniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_T.SinavTipID" });
                }
                else
                {
                    var EgitimDiliTurkce = (kModel.YLEgitimDiliTurkce == true || kModel.LEgitimDiliTurkce == true);
                    var kmMt = Management.stKontrol(
                        kModel.BasvuruSurecID,
                        kModel._OgrenimTipKod,
                        kModel._Ingilizce,
                        kModel.BasvurularSinavBilgi_T.SinavTipID,
                        kModel.BasvurularSinavBilgi_T.WsSinavYil,
                        kModel.BasvurularSinavBilgi_T.SinavDilID,
                        kModel.BasvurularSinavBilgi_T.WsSinavDonem,
                        null,
                        kModel._ProgramKod,
                        kModel.BasvurularSinavBilgi_T.SinavTarihi,
                        kModel.BasvuruTarihi,
                        kModel.BasvurularSinavBilgi_T.SubSinavAralikID,
                        kModel.BasvurularSinavBilgi_T.BasvuruSurecSubNot,
                        kModel.BasvurularSinavBilgi_T.SinavNotu,
                        kModel.BasvurularSinavBilgi_T.IsTaahhutVar,
                        EgitimDiliTurkce);
                    _MmMessage.Messages.AddRange(kmMt.Messages.ToList());
                    _MmMessage.MessagesDialog.AddRange(kmMt.MessagesDialog.ToList());
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_T.SinavTipID" });
                    if (kmMt.Messages.Count > 0) stpSon = true;
                    else
                    {
                        var tStip = db.SinavTipleris.Where(p => p.SinavTipID == kModel.BasvurularSinavBilgi_T.SinavTipID).First();
                        kModel.BasvurularSinavBilgi_T.SinavTipKod = tStip.SinavTipKod;
                        if (kModel.BasvurularSinavBilgi_T.WsSinavDonem.IsNullOrWhiteSpace() == false)
                        {
                            var snvBW = kModel.BasvurularSinavBilgi_T.WsSinavDonem.Split('~');
                            kModel.BasvurularSinavBilgi_T.WsSinavDonem = snvBW[1];
                            kModel.BasvurularSinavBilgi_T.WsSinavYil = snvBW[0].ToInt().Value;
                        }
                        kModel.BasvurularSinavBilgi_T.SinavTipGrupID = SinavTipGrup.Tomer;
                        BasvurularSinavBilgiList.Add(kModel.BasvurularSinavBilgi_T);
                    }
                }
            }
            if (kModel.BasvuruDurumID <= 0)
            {
                stpSon = true;
                _MmMessage.Messages.Add("Başvuru Durumunu Seçiniz.");
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Basvuru.BasvuruDurumID" });

            }
            else
            {
                if (kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı && !kModel.Onaylandi)
                {
                    stpSon = true;
                    _MmMessage.Messages.Add("Başvuru Onaylayınız.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Onaylandi" });
                }
                else
                {
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Onaylandi" });
                }
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Basvuru.BasvuruDurumID" });

                if (_MmMessage.Messages.Count == 0 && kModel.BasvuruID > 0)
                {
                    var BTIDs = db.BasvurularTercihleris.Where(p => p.BasvuruID == kModel.BasvuruID).Select(s => s.BasvuruTercihID).ToList();

                    var HesaplananTercihlerVar = db.BasvurularTercihleris.Any(p => BTIDs.Contains(p.BasvuruTercihID) && p.MulakatSonuclaris.Any());
                    if (HesaplananTercihlerVar)
                    {
                        string msg = "Bu başvuruda hesaplandığı için başvuru üzerinden herhangi bir değişiklik yapılamaz!";
                        _MmMessage.Messages.Add(msg);
                    }




                }
            }
            if (stpSon) stps.Add(3);
            #endregion
            #endregion
            bool sendMail = false;

            if (_MmMessage.Messages.Count == 0)
            {
                var otsb = db.BasvuruSurecOgrenimTipleris.Where(p => p.YLEgitimBilgisiIste && p.BasvuruSurecID == kModel.BasvuruSurecID).Select(s => s.OgrenimTipKod).ToList();
                kModel.YLDurum = qtercih.Any(a => otsb.Contains(a.OgrenimTipKod));

                if (!kModel.YLDurum)
                {
                    kModel.YLMezuniyetNotu = null;
                    kModel.YLUniversiteID = null;
                    kModel.YLFakulteAdi = null;
                    kModel.YLOgrenciBolumID = null;
                    kModel.YLBaslamaTarihi = null;
                    kModel.YLMezuniyetTarihi = null;
                    kModel.YLNotSistemID = null;
                    kModel.YLMezuniyetNotu = null;
                    kModel.YLEgitimDiliTurkce = null;
                }
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.KullaniciID = kModel.KullaniciID;
                if (ogrenimDurumuIstensin == false) kModel.LOgrenimDurumID = null;


                var BTercihler = new List<BasvurularTercihleri>();
                var EkliTercihs = db.BasvurularTercihleris.Where(p => p.BasvuruID == kModel.BasvuruID).ToList();
                foreach (var item in qtercih)
                {
                    var eT = EkliTercihs.Where(p => p.UniqueID.ToString() == item.UniqueID).FirstOrDefault();
                    var btR = new BasvurularTercihleri
                    {
                        SiraNo = item.SiraNo,
                        OgrenimTipKod = item.OgrenimTipKod,
                        ProgramKod = item.ProgramKod,
                        AlanTipID = item.AlanTipID,
                        UniqueID = Guid.NewGuid(),
                        IsOgrenimUcretiOrKatkiPayi = item.Ucretli ? (bool?)true : null,
                        ProgramUcret = item.Ucretli ? item.Ucret : null,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemTarihi = DateTime.Now,

                    };

                    BTercihler.Add(btR);
                }


                var dataAnk = new List<AnketCevaplari>();
                if (bsurec.AnketID.HasValue && kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı && kModel.Onaylandi)
                {
                    var anketCevaplari = UserIdentity.Current.Informations.Where(p => p.Key == "LUBAnket").FirstOrDefault();
                    dataAnk = (List<AnketCevaplari>)anketCevaplari.Value;
                    UserIdentity.Current.Informations.Remove("LUBAnket");
                }
                var data = new Basvurular();
                bool IsNewRecord = false;
                if (kModel.BasvuruID <= 0)
                {
                    IsNewRecord = true;
                    kModel.BasvuruTarihi = DateTime.Now;

                    data = db.Basvurulars.Add(new Basvurular
                    {
                        RowID = Guid.NewGuid(),
                        BasvuruSurecID = kModel.BasvuruSurecID,
                        BasvuruTarihi = kModel.BasvuruTarihi,
                        BasvuruDurumID = kModel.BasvuruDurumID,
                        BasvuruDurumAciklamasi = kModel.BasvuruDurumAciklamasi,
                        KullaniciID = kModel.KullaniciID,
                        KullaniciTipID = kModel.KullaniciTipID,
                        ResimAdi = kModel.ResimAdi,
                        Ad = kModel.Ad,
                        Soyad = kModel.Soyad,
                        CinsiyetID = kModel.CinsiyetID,
                        AnaAdi = kModel.AnaAdi,
                        BabaAdi = kModel.BabaAdi,
                        DogumYeriKod = kModel.DogumYeriKod,
                        DogumTarihi = kModel.DogumTarihi,
                        NufusilIlceKod = kModel.NufusilIlceKod,
                        CiltNo = kModel.CiltNo,
                        AileNo = kModel.AileNo,
                        SiraNo = kModel.SiraNo,
                        TcKimlikNo = kModel.TcKimlikNo,
                        PasaportNo = kModel.PasaportNo,
                        UyrukKod = kModel.UyrukKod,
                        SehirKod = kModel.SehirKod,
                        IsTel = kModel.IsTel,
                        EvTel = kModel.EvTel,
                        CepTel = kModel.CepTel,
                        EMail = kModel.EMail,
                        Adres = kModel.Adres,
                        Adres2 = kModel.Adres2,
                        LUniversiteID = kModel.LUniversiteID,
                        LFakulteAdi = kModel.LFakulteAdi,
                        LOgrenciBolumID = kModel.LOgrenciBolumID,
                        LOgrenimDurumID = kModel.LOgrenimDurumID,
                        LBaslamaTarihi = kModel.LBaslamaTarihi,
                        LMezuniyetTarihi = kModel.LMezuniyetTarihi,
                        LNotSistemID = kModel.LNotSistemID,
                        LMezuniyetNotu = kModel.LMezuniyetNotu,
                        LMezuniyetNotu100LukSistem = kModel.LMezuniyetNotu.Value.ToNotCevir(kModel.LNotSistemID.Value).Not100Luk,
                        LEgitimDiliTurkce = kModel.LEgitimDiliTurkce,
                        YLUniversiteID = kModel.YLUniversiteID,
                        YLFakulteAdi = kModel.YLFakulteAdi,
                        YLOgrenciBolumID = kModel.YLOgrenciBolumID,
                        YLBaslamaTarihi = kModel.YLBaslamaTarihi,
                        YLMezuniyetTarihi = kModel.YLMezuniyetTarihi,
                        YLNotSistemID = kModel.YLNotSistemID,
                        YLMezuniyetNotu = kModel.YLMezuniyetNotu,
                        YLMezuniyetNotu100LukSistem = kModel.YLNotSistemID.HasValue ? kModel.YLMezuniyetNotu.Value.ToNotCevir(kModel.YLNotSistemID.Value).Not100Luk : (double?)null,
                        YLEgitimDiliTurkce = kModel.YLEgitimDiliTurkce,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        BasvurularSinavBilgis = BasvurularSinavBilgiList,
                        BasvurularTercihleris = BTercihler,
                        AnketCevaplaris = dataAnk

                    });

                    db.SaveChanges();
                    kModel.BasvuruID = data.BasvuruID;
                    kModel.BasvuruTarihi = data.BasvuruTarihi;
                    if (kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı || kModel.BasvuruDurumID == BasvuruDurumu.IptalEdildi) sendMail = true;
                }
                else
                {
                    data = db.Basvurulars.Where(p => p.BasvuruID == kModel.BasvuruID).First();
                    if ((kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı || kModel.BasvuruDurumID == BasvuruDurumu.IptalEdildi) && kModel.BasvuruDurumID != data.BasvuruDurumID)
                    {
                        data.BasvuruTarihi = DateTime.Now;
                        sendMail = true;
                    }
                    data.KullaniciTipID = kModel.KullaniciTipID;
                    data.ResimAdi = kModel.ResimAdi;
                    data.Ad = kModel.Ad;
                    data.Soyad = kModel.Soyad;
                    data.CinsiyetID = kModel.CinsiyetID;
                    data.AnaAdi = kModel.AnaAdi;
                    data.BabaAdi = kModel.BabaAdi;
                    data.DogumYeriKod = kModel.DogumYeriKod;
                    data.DogumTarihi = kModel.DogumTarihi;
                    data.NufusilIlceKod = kModel.NufusilIlceKod;
                    data.CiltNo = kModel.CiltNo;
                    data.AileNo = kModel.AileNo;
                    data.SiraNo = kModel.SiraNo;
                    data.TcKimlikNo = kModel.TcKimlikNo;
                    data.PasaportNo = kModel.PasaportNo;
                    data.UyrukKod = kModel.UyrukKod;
                    data.SehirKod = kModel.SehirKod;
                    data.IsTel = kModel.IsTel;
                    data.EvTel = kModel.EvTel;
                    data.CepTel = kModel.CepTel;
                    data.EMail = kModel.EMail;
                    data.Adres = kModel.Adres;
                    data.Adres2 = kModel.Adres2;
                    data.BasvuruDurumID = kModel.BasvuruDurumID;
                    data.BasvuruDurumAciklamasi = kModel.BasvuruDurumAciklamasi;
                    data.LUniversiteID = kModel.LUniversiteID;
                    data.LFakulteAdi = kModel.LFakulteAdi;
                    data.LOgrenciBolumID = kModel.LOgrenciBolumID;
                    data.LOgrenimDurumID = kModel.LOgrenimDurumID;
                    data.LBaslamaTarihi = kModel.LBaslamaTarihi;
                    data.LMezuniyetTarihi = kModel.LMezuniyetTarihi;
                    data.LNotSistemID = kModel.LNotSistemID;
                    data.LMezuniyetNotu = kModel.LMezuniyetNotu;
                    data.LMezuniyetNotu100LukSistem = kModel.LMezuniyetNotu.Value.ToNotCevir(kModel.LNotSistemID.Value).Not100Luk;
                    data.YLUniversiteID = kModel.YLUniversiteID;
                    data.YLFakulteAdi = kModel.YLFakulteAdi;
                    data.YLOgrenciBolumID = kModel.YLOgrenciBolumID;
                    data.YLBaslamaTarihi = kModel.YLBaslamaTarihi;
                    data.YLMezuniyetTarihi = kModel.YLMezuniyetTarihi;
                    data.YLNotSistemID = kModel.YLNotSistemID;
                    data.YLMezuniyetNotu = kModel.YLMezuniyetNotu;
                    data.LEgitimDiliTurkce = kModel.LEgitimDiliTurkce;
                    data.YLEgitimDiliTurkce = kModel.YLEgitimDiliTurkce;
                    if (kModel.YLNotSistemID.HasValue) data.YLMezuniyetNotu100LukSistem = kModel.YLMezuniyetNotu.Value.ToNotCevir(kModel.YLNotSistemID.Value).Not100Luk;
                    else data.YLMezuniyetNotu100LukSistem = null;

                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    db.BasvurularSinavBilgis.RemoveRange(data.BasvurularSinavBilgis.ToList());
                    data.BasvurularSinavBilgis = BasvurularSinavBilgiList;
                    db.BasvurularTercihleris.RemoveRange(data.BasvurularTercihleris.ToList());
                    data.BasvurularTercihleris = BTercihler;
                    if (data.AnketCevaplaris.Any() == false)
                    {
                        data.AnketCevaplaris = dataAnk;
                    }
                    db.SaveChanges();
                    kModel.BasvuruTarihi = data.BasvuruTarihi;

                }

                LogIslemleri.LogEkle("Basvurular", IsNewRecord ? IslemTipi.Insert : IslemTipi.Update, data.ToJson());
                if (sendMail && bsurec.Enstituler.LUBMailGonder)
                {
                    var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(kModel.EnstituKod);
                    var mmmC = new mdlMailMainContent();
                    mmmC.EnstituAdi = bsurec.Enstituler.EnstituAd;
                    var _ea = mailBilgi.SistemErisimAdresi;
                    var WurlAddr = _ea.Split('/').ToList();
                    if (_ea.Contains("//"))
                        _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                    else
                        _ea = "http://" + WurlAddr.First();
                    mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                    mmmC.UniversiteAdi = "YILDIZ TEKNİK ÜNİVERSİTESİ";

                    var mtc = new mailTableContent();
                    if (kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı) mtc.AciklamaDetayi = "Başvurunuza ait kısa bilgi aşağıdaki gibidir. Başvurunuzun detaylı bilgisini Mail ekinden  PDF olarak indirebilirsiniz.";
                    else mtc.AciklamaDetayi = "Başvurunuz İptal Edilmiştir. Detaylar Aşağıdaki Gibidir.";
                    mtc.GrupBasligi = "Başvuru Bilgisi";

                    var BasvuruDurumData = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == kModel.BasvuruDurumID).First();
                    mtc.Detaylar.Add(new mailTableRow { Baslik = "Enstitü Adı", Aciklama = bsurec.Enstituler.EnstituAd });
                    mtc.Detaylar.Add(new mailTableRow { Baslik = "Başvuru Dönem Bilgisi", Aciklama = kModel.DonemAdi });
                    mtc.Detaylar.Add(new mailTableRow { Baslik = "Başvuru Türü", Aciklama = "Lisansüstü Başvurusu" });
                    mtc.Detaylar.Add(new mailTableRow { Baslik = "Başvuru Durumu", Aciklama = BasvuruDurumData.BasvuruDurumAdi });
                    if (kModel.BasvuruDurumID == BasvuruDurumu.IptalEdildi)
                    {
                        mtc.Detaylar.Add(new mailTableRow { Baslik = "Açıklama", Aciklama = kModel.BasvuruDurumAciklamasi.ToString() });
                    }
                    else
                    {
                        mtc.Detaylar.Add(new mailTableRow { Baslik = "Başvuru Tarihi", Aciklama = kModel.BasvuruTarihi.ToString() });
                    }
                    if (kModel.LOgrenimDurumID.HasValue && kModel.LOgrenimDurumID == OgrenimDurum.HalenOğrenci)
                    {
                        var OgrenimDurumData = db.OgrenimDurumlaris.Where(p => p.OgrenimDurumID == kModel.LOgrenimDurumID.Value).First();
                        var Msg = OgrenimDurumData.LUBAciklama.Replace("_AGNOGirisBasTarx_", bsurec.AGNOGirisBaslangicTarihi.ToString("dd-MM-yyyy")).Replace("_AGNOGirisBasTar_", bsurec.AGNOGirisBaslangicTarihi.ToString("dd-MM-yyyy HH:mm")).Replace("_AGNOGirisBitTar_", bsurec.AGNOGirisBitisTarihi.ToString("dd-MM-yyyy HH:mm"));
                        mtc.Detaylar.Add(new mailTableRow { Baslik = "Dikkat", Aciklama = Msg });

                    }

                    var tableContent = Management.RenderPartialView("Ajax", "getMailTableContent", mtc);
                    mmmC.Content = tableContent;
                    string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);


                    var attchL = new List<System.Net.Mail.Attachment>();
                    string mTitle = "";
                    if (kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı)
                    {
                        mTitle = "Başvurunuz Sisteme Kaydedilmiştir";
                        attchL = Management.exportRaporPdf(RaporTipleri.Basvuru, new List<int?> { kModel.BasvuruID });

                    }
                    else mTitle = "Başvurunuz İptal Edilmiştir";
                    var uBilgi = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).First();
                    var emailSend = MailManager.sendMail(kModel.EnstituKod, mTitle, htmlMail, uBilgi.EMail, attchL);
                }
                int? BelgeDetailBasvuruID = null;
                if (bsurec.IsBelgeYuklemeVar && kModel.BasvuruDurumID == BasvuruDurumu.Onaylandı)
                {

                    BelgeDetailBasvuruID = kModel.BasvuruID;
                }

                if (kModel.KullaniciID != UserIdentity.Current.Id) return RedirectToAction("Index", "GelenBasvurular", new { BelgeDetailBasvuruID = BelgeDetailBasvuruID });
                else return RedirectToAction("Index", new { BelgeDetailBasvuruID = BelgeDetailBasvuruID });
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, _MmMessage.Messages.ToArray());
            }
            if (qtercih.Count > 0)
            {
                var tercihler = (from s in qtercih
                                 join at in db.AlanTipleris on s.AlanTipID equals at.AlanTipID
                                 join kt in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID) on new { s.OgrenimTipKod, s.ProgramKod } equals new { kt.OgrenimTipKod, kt.ProgramKod }
                                 select new basvuruTercihModel
                                 {
                                     BasvuruID = kModel.BasvuruID,
                                     SiraNo = s.SiraNo,
                                     YlBilgiIste = s.YlBilgiIste,
                                     Ingilizce = s.Ingilizce,
                                     AlanTipID = at.AlanTipID,
                                     AlanTipAdi = at.AlanTipAdi,
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     ProgramKod = s.ProgramKod
                                 }).ToList();
                foreach (var item in tercihler)
                {
                    item.ProgramBilgileri = Management.getKontenjanProgramBilgi(item.ProgramKod, item.OgrenimTipKod, kModel.BasvuruSurecID, kModel.KullaniciTipID.Value, kModel.LOgrenimDurumID, kModel.LUniversiteID);
                    item.ProgramBilgileri.AlanTipID = item.AlanTipID;
                }
                kModel.BasvuruTercihleri = tercihler;

            }

            if (stps.Count > 0) kModel.SetSelectedStep = stps.First();

            ViewBag._MmMessage = _MmMessage;
            ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", kModel.CinsiyetID);
            ViewBag.DogumYeriKod = new SelectList(Management.cmbSehirler(true), "Value", "Caption", kModel.DogumYeriKod);
            ViewBag.NufusilIlceKod = new SelectList(Management.cmbSehirler(true), "Value", "Caption", kModel.NufusilIlceKod);
            ViewBag.SehirKod = new SelectList(Management.cmbSehirler(true), "Value", "Caption", kModel.SehirKod);
            ViewBag.UyrukKod = new SelectList(Management.cmbUyruk(true), "Value", "Caption", kModel.UyrukKod);
            ViewBag.LUniversiteID = new SelectList(Management.cmbGetAktifUniversiteler(true), "Value", "Caption", kModel.LUniversiteID);
            ViewBag.LOgrenciBolumID = new SelectList(Management.cmbGetOgrenciBolumleri(kModel.EnstituKod, true), "Value", "Caption", kModel.LOgrenciBolumID);
            ViewBag.LOgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu2(true, IsBasvurudaGozuksun: true), "Value", "Caption", kModel.LOgrenimDurumID);
            ViewBag.LNotSistemID = new SelectList(Management.cmbGetNotSistemleri(true), "Value", "Caption", kModel.LNotSistemID);
            ViewBag.YLUniversiteID = new SelectList(Management.cmbGetAktifUniversiteler(true), "Value", "Caption", kModel.YLUniversiteID);
            ViewBag.YLOgrenciBolumID = new SelectList(Management.cmbGetOgrenciBolumleri(kModel.EnstituKod, true), "Value", "Caption", kModel.YLOgrenciBolumID);
            ViewBag.YLNotSistemID = new SelectList(Management.cmbGetNotSistemleri(true), "Value", "Caption", kModel.YLNotSistemID);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbGetAktifOgrenimTipleriGrup(kModel.BasvuruSurecID, true), "Value", "Caption");
            ViewBag.SubOgrenimTipKod = new SelectList(Management.cmbGetAktifSubOgrenimTipleri(kModel.BasvuruSurecID, "asd"), "Value", "Caption", "");
            ViewBag.AnabilimdaliKod = new SelectList(Management.cmbGetAktifBolumlerX(0, 0), "Value", "Caption", "");
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlarX(0, kModel.BasvuruSurecID, 0, 0), "Value", "Caption");
            ViewBag.ASinavTipleri = new SelectList(Management.cmbGetBasvuruSurecAktifSinavTipTipleri(kModel.BasvuruSurecID, SinavTipGrup.Ales_Gree, true), "Value", "Caption", kModel.BasvurularSinavBilgi_A != null ? kModel.BasvurularSinavBilgi_A.SinavTipID : 0);
            ViewBag.DSinavTipleri = new SelectList(Management.cmbGetBasvuruSurecAktifSinavTipTipleri(kModel.BasvuruSurecID, SinavTipGrup.DilSinavlari, true), "Value", "Caption", kModel.BasvurularSinavBilgi_D != null ? kModel.BasvurularSinavBilgi_D.SinavTipID : 0);
            ViewBag.TSinavTipleri = new SelectList(Management.cmbGetBasvuruSurecAktifSinavTipTipleri(kModel.BasvuruSurecID, SinavTipGrup.Tomer, true), "Value", "Caption", kModel.BasvurularSinavBilgi_T != null ? kModel.BasvurularSinavBilgi_T.SinavTipID : 0);
            ViewBag.BasvuruDurumID = new SelectList(Management.cmbBasvuruDurum(true), "Value", "Caption", kModel.BasvuruDurumID);

            if (kModel.BasvuruID > 0)
            {
                ViewBag.BasvuruDurumu = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == kModel.BasvuruDurumID).Select(s => new basvuruDurumModel { BasvuruDurumID = s.BasvuruDurumID, ClassName = s.ClassName, Color = s.Color, DurumAdi = s.BasvuruDurumAdi }).First();
            }
            else
            {
                ViewBag.BasvuruDurumu = new basvuruDurumModel
                {
                    DurumAdi = "Yeni Başvuru",
                    ClassName = "fa fa-plus",
                    Color = "color:black;"
                };
            }
            return View(kModel);
        }
        public ActionResult LoDKontrol(int LOgrenimDurumID, int BasvuruSurecID)
        {
            ;
            var _MmMessage = new MmMessage();
            var BasvuruOgrenimDurumu = db.OgrenimDurumlaris.Where(p => p.OgrenimDurumID == LOgrenimDurumID).First();

            var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
            bool ShowMsg = false;
            string Msg = "";
            if (LOgrenimDurumID == OgrenimDurum.HalenOğrenci && BasvuruSurec.AGNOGirisBaslangicTarihi.HasValue)
            {
                Msg = BasvuruOgrenimDurumu.LUBAciklama.Replace("_AGNOGirisBasTarx_", BasvuruSurec.AGNOGirisBaslangicTarihi.ToString("dd-MM-yyyy")).Replace("_AGNOGirisBasTar_", BasvuruSurec.AGNOGirisBaslangicTarihi.ToString("dd-MM-yyyy HH:mm")).Replace("_AGNOGirisBitTar_", BasvuruSurec.AGNOGirisBitisTarihi.ToString("dd-MM-yyyy HH:mm"));
                ShowMsg = true;
            }
            _MmMessage.Title = "Uyarı";
            _MmMessage.MessageType = Msgtype.Success;
            return Json(new { ShowMsg = ShowMsg, Msg = Msg, Capt = BasvuruOgrenimDurumu.OgrenimDurumAdi });
        }
        public ActionResult NotKontrol(int NotSistemID, double Puan)
        {
            if (NotSistemID == NotSistemi.Not1LikSistem && Puan > 4) NotSistemID = 0;
            else if (NotSistemID == NotSistemi.Not20LikSistem && Puan > 20) NotSistemID = 0;
            else if (NotSistemID == NotSistemi.Not4LükSistem && Puan > 4) NotSistemID = 0;
            else if (NotSistemID == NotSistemi.Not5LikSistem && Puan > 5) NotSistemID = 0;
            var not = Puan.ToNotCevir(NotSistemID).Not100Luk.ToString("n2");
            return Json(new { Deger = not }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult getOTdetay(int BasvuruSurecID, int otID, int KullaniciID, int? BasvuruID = null, List<int> ArrOtipIds = null)
        {
            var mdl = Management.getOgrenimTipiKotaBilgi(BasvuruSurecID, otID, KullaniciID, BasvuruSurecTipi.LisansustuBasvuru, BasvuruID, ArrOtipIds);

            return View(mdl);
        }
        public ActionResult getPRdetay(string prgID, int otID, int BasvuruSurecID, int BasvuruID, int KullaniciID, int KullaniciTipID, int? LOgrenimDurumID, int? LUniversiteID)
        {
            var mdl = Management.getKontenjanProgramBilgi(prgID, otID, BasvuruSurecID, KullaniciTipID, LOgrenimDurumID, LUniversiteID);
            mdl.BasvuruSurecID = BasvuruSurecID;
            mdl.KullaniciID = KullaniciID;
            mdl.BasvuruID = BasvuruID;
            return View(mdl);
        }
        public ActionResult getAlanKontrol(int LOgrenciBolumID, int? YLOgrenciBolumID, int oTipKod, string tprog, int BasvuruSurecID, int KullaniciID, int BasvuruID)
        {
            var mdl = Management.AlanKontrol(BasvuruSurecID, LOgrenciBolumID, YLOgrenciBolumID, oTipKod, tprog, KullaniciID, BasvuruID);

            return mdl.toJsonResult();
        }
        [ValidateInput(false)]
        public ActionResult CreateTercihRowHtml(TercihRowModel model)
        {
            return View(model);
        }
        public ActionResult getNotSistemiKontrol(int NotSistemID)
        {
            var mdl = db.NotSistemleris.Where(p => p.NotSistemID == NotSistemID).First();
            var maksNot = mdl.MaxNot.ToString().Split(',');
            return new { firstLength = maksNot[0].Length, lastLength = maksNot[1].Length, maxLenth = mdl.MaxNot.ToString().Length }.toJsonResult();
        }

        public ActionResult getSinavTip(tercihSTKontrolModel model, int BasvuruSurecID, bool? EgitimDiliTurkce, int SinavTipID, string SelectedVal, List<int> OgrenimTipKods, List<bool> Ingilizces, List<string> ProgramKods, int? BasvuruID)
        {

            var mdl = Management.getSinavBilgisi(BasvuruSurecID, SinavTipID, OgrenimTipKods, ProgramKods, Ingilizces);
            mdl.BasvuruSinavData = mdl.BasvuruSinavData ?? new BasvurularSinavBilgi();
            mdl.IsTurkceProgramVar = Ingilizces.Any(a => a == false);

            if (mdl.SinavTipGrupID == SinavTipGrup.Tomer)
            {
                mdl.IsEgitimDiliTurkce = EgitimDiliTurkce.Value;
            }


            if (BasvuruID.HasValue)
            {
                var basvuruSB = db.BasvurularSinavBilgis.Where(p => p.BasvuruID == BasvuruID.Value && p.SinavTipID == SinavTipID).FirstOrDefault();
                if (basvuruSB != null) mdl.BasvuruSinavData = basvuruSB;
            }
            else
            {
                if (SelectedVal.IsNullOrWhiteSpace() == false && SelectedVal != "undefined")
                {
                    mdl.BasvuruSinavData.SinavTipID = SinavTipID;
                    var splV = SelectedVal.Split('~');
                    mdl.BasvuruSinavData.WsSinavDonem = "";
                    mdl.BasvuruSinavData.WsSinavYil = splV[0].ToInt().Value;
                }
            }

            return View(mdl);
        }
        public ActionResult getSinavTipSonuc(int SinavTipID, int BasvuruSurecID, string Donem, int? WsSonucID, string Tck, int? BasvuruID = null, string MinNotAdi = "")
        {
            var getSinavTip = db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == BasvuruSurecID && p.SinavTipID == SinavTipID).First();
            bool uygunMu = true;
            var _yil = Donem.Split('~')[0].ToInt().Value;

            if (getSinavTip.WsSinavCekimTipID.HasValue && getSinavTip.WsSinavCekimTipID.Value == WsCekimTipi.Tarih)
            {
                uygunMu = db.BasvuruSurecSinavTipleriDonems.Any(a => a.BasvuruSurecSinavTipleri.BasvuruSurecID == BasvuruSurecID && a.BasvuruSurecSinavTipleri.SinavTipID == SinavTipID && a.SinavDilID == _yil);
                if (!uygunMu)
                {

                    Management.SistemBilgisiKaydet("BasvuruSurecID:" + BasvuruSurecID + "\n SinavTipID:" + SinavTipID + "\n DilID:" + _yil + "\n Bilgisi sistemde bulunamadı! Konsoldan müdahale olabilir!", "Basvuru/getSinavTipSonuc", BilgiTipi.Saldırı);
                    _yil = 0001;
                }
            }
            else
            {
                uygunMu = db.BasvuruSurecSinavTipleriDonems.Any(a => a.BasvuruSurecSinavTipleri.BasvuruSurecID == BasvuruSurecID && a.BasvuruSurecSinavTipleri.SinavTipID == SinavTipID && a.Yil == _yil);
                if (!uygunMu)
                {

                    Management.SistemBilgisiKaydet("BasvuruSurecID:" + BasvuruSurecID + "\n SinavTipID:" + SinavTipID + "\n Yil:" + _yil + "\n Bilgisi sistemde bulunamadı! Konsoldan müdahale olabilir!", "Basvuru/getSinavTipSonuc", BilgiTipi.Saldırı);
                    _yil = 0001;
                }
            }

            var mdl = Management.getSinavTipSonucModel(SinavTipID, BasvuruSurecID, _yil.ToString(), WsSonucID, Tck, BasvuruID, MinNotAdi);


            string strView = "";
            if (mdl.IsSinavSonucuVar || mdl.ShowIsTaahhutVar)
            {
                strView = Management.RenderPartialView("Basvuru", "getSinavTipSonucView", mdl);


            }

            return Json(new { Model = mdl, view = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }

        public ActionResult getSinavTipSonucView()
        {
            return View();
        }

        public ActionResult getFormul(int BasvuruSurecSinavTipID, int? SubSinavAralikID, float Puan)
        {
            string Sonuc = "";
            if (SubSinavAralikID.HasValue)
            {
                var data = db.BasvuruSurecSinavTiplerSubSinavAraliks.Where(p => p.SubSinavAralikID == SubSinavAralikID && p.BasvuruSurecSinavTipID == BasvuruSurecSinavTipID).First();

                if (data.NotDonusum)
                {
                    var formul = data.NotDonusumFormulu.Replace("Puan", Puan.ToString()).Replace(".", ",");
                    var pn = formul.EvaluateExpression();
                    Sonuc = formul.EvaluateExpression().ToString("n2");
                }
                else
                {
                    Sonuc = Puan.ToString("n2").Replace(".", ",");
                }
            }
            else
            {

                var bsurecSt = db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecSinavTipID == BasvuruSurecSinavTipID).First();
                if (bsurecSt.NotDonusum)
                {
                    var formul = bsurecSt.NotDonusumFormulu.Replace("Puan", Puan.ToString()).Replace(".", ",");
                    var pn = formul.EvaluateExpression();
                    Sonuc = formul.EvaluateExpression().ToString("n2");
                }
                else
                {
                    Sonuc = Puan.ToString("n2").Replace(".", ",");
                }

            }
            return Json(new { Sonuc = Sonuc }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Sil(int id)
        {
            var mmMessage = Management.getBasvuruSilKontrol(id, BasvuruSurecTipi.LisansustuBasvuru);

            if (mmMessage.IsSuccess)
            {
                var kayit = db.Basvurulars.Where(p => p.BasvuruID == id).FirstOrDefault();
                var tarih = kayit.BasvuruTarihi.ToString();
                try
                {
                    mmMessage.Title = "Uyarı";
                    db.Basvurulars.Remove(kayit);
                    db.SaveChanges();
                    LogIslemleri.LogEkle("Basvurular", IslemTipi.Delete, kayit.ToJson());
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = Msgtype.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "Basvuru/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }

            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

    }
}
