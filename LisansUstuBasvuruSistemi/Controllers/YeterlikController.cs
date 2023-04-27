using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class YeterlikController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        [AllowAnonymous]
        public ActionResult Index(string ekd, bool? isKomiteOrJuri = null, Guid? isDegerlendirme = null)
        {
            if (!UserIdentity.Current.IsAuthenticated && (!isKomiteOrJuri.HasValue || !isDegerlendirme.HasValue)) return RedirectToActionPermanent("Login", "Account");

            return Index(new FmYeterlikBasvuruDto { PageSize = 50, IsKomiteOrJuri = isKomiteOrJuri, isDegerlendirme = isDegerlendirme }, ekd);
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Index(FmYeterlikBasvuruDto model, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            if (!UserIdentity.Current.IsAuthenticated && (!model.IsKomiteOrJuri.HasValue || !model.isDegerlendirme.HasValue)) return RedirectToActionPermanent("Login", "Account");
            #region BilgiModel 
            model.AktifYeterlikSurecId = YeterlikBus.GetYeterlikAktifSurecId(enstituKod);
            if (!model.isDegerlendirme.HasValue)
            {
                var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);

                model.AdSoyad = kullanici.Ad + " " + kullanici.Soyad;
                model.EnstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
                if (model.AktifYeterlikSurecId.HasValue)
                {
                    var surec = _entities.YeterlikSurecis.First(p => p.YeterlikSurecID == model.AktifYeterlikSurecId);
                    model.IsAktifSurecBasvuruVar =
                        surec.YeterlikBasvurus.Any(a => a.KullaniciID == kullanici.KullaniciID);
                    model.DonemAdi = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi;

                    model.IsYtuOgrencisi =
                        kullanici.YtuOgrencisi && kullanici.OgrenimDurumID == OgrenimDurum.HalenOğrenci;
                    model.IsEnstituYetki = kullanici.EnstituKod == enstituKod;

                    model.IsOgrenimSeviyeYetki =
                        surec.YeterlikSurecOgrenimTipleris.Any(a => a.OgrenimTipKod == kullanici.OgrenimTipKod);
                    model.OgrenimTipAdis = string.Join(", ",
                        surec.YeterlikSurecOgrenimTipleris.Select(s => s.OgrenimTipleri.OgrenimTipAdi).ToList());

                    if (model.IsOgrenimSeviyeYetki)
                    {
                        KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(kullanici.KullaniciID);
                    }
                }
            }

            #endregion


            var q = from yeterlikBasvuru in _entities.YeterlikBasvurus.Where(p => model.IsKomiteOrJuri.HasValue
                                                                                                            ? (model.IsKomiteOrJuri == true ? p.YeterlikBasvuruKomitelers.Any(a => a.UniqueID == model.isDegerlendirme)
                                                                                                                                            : p.YeterlikBasvuruJuriUyeleris.Any(a => a.UniqueID == model.isDegerlendirme))
                                                                                                            : p.KullaniciID == UserIdentity.Current.Id)
                    join yeterlikSureci in _entities.YeterlikSurecis.Where(p => p.EnstituKod == enstituKod) on yeterlikBasvuru.YeterlikSurecID equals yeterlikSureci.YeterlikSurecID
                    join kullanicilar in _entities.Kullanicilars on yeterlikBasvuru.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _entities.Programlars on yeterlikBasvuru.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _entities.OgrenimTipleris on yeterlikBasvuru.OgrenimTipID equals ogrenimTipleri.OgrenimTipID
                    select new FrYeterlikBasvuruDto
                    {
                        YeterlikBasvuruID = yeterlikBasvuru.YeterlikBasvuruID,
                        UniqueID = yeterlikBasvuru.UniqueID,
                        BasvuruTarihi = yeterlikBasvuru.BasvuruTarihi,
                        ResimAdi = kullanicilar.ResimAdi,
                        KullaniciID = yeterlikBasvuru.KullaniciID,
                        AdSoyad = kullanicilar.Ad + " " + kullanicilar.Soyad,
                        OgrenciNo = yeterlikBasvuru.OgrenciNo,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        OkuduguDonemNo = yeterlikBasvuru.OkuduguDonemNo,
                        IsEnstituOnaylandi = yeterlikBasvuru.IsEnstituOnaylandi,
                        EnstituOnayAciklama = yeterlikBasvuru.EnstituOnayAciklama,
                        IsJuriOlusturuldu = yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.Any(),
                        YaziliSinavTarihi = yeterlikBasvuru.YaziliSinavTarihi,
                        YaziliSinavYeri = yeterlikBasvuru.YaziliSinavYeri,
                        IsYaziliSinavinaKatildi = yeterlikBasvuru.IsYaziliSinavinaKatildi,
                        YaziliSinaviNotu = yeterlikBasvuru.YaziliSinaviNotu,
                        IsYaziliSinavBasarili = yeterlikBasvuru.IsYaziliSinavBasarili,
                        IsSozluSinavOnline = yeterlikBasvuru.IsSozluSinavOnline,
                        SozluSinavTarihi = yeterlikBasvuru.SozluSinavTarihi,
                        SozluSinavYeri = yeterlikBasvuru.SozluSinavYeri,
                        IsSozluSinavinaKatildi = yeterlikBasvuru.IsSozluSinavinaKatildi,
                        SozluSinaviOrtalamaNotu = yeterlikBasvuru.SozluSinaviOrtalamaNotu,
                        GenelBasariNotu = yeterlikBasvuru.GenelBasariNotu,
                        IsGenelSonucBasarili = yeterlikBasvuru.IsGenelSonucBasarili

                    };
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            return View(model);
        }


        public ActionResult BasvuruYap(Guid? id, string ekd = "")
        {
            var model = new KmYeterlikBasvuruDto();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
            var errorMessage = YeterlikBus.YeterlikBasvuruKontrol(enstituKod, id);

            if (!errorMessage.Any())
            {
                if (id.HasValue)
                {
                    var basvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == id && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
                    var ogrenimTip = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == basvuru.Kullanicilar.OgrenimTipKod);
                    var ogrenciBilgi = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(basvuru.KullaniciID);


                    model.UniqueID = basvuru.UniqueID;
                    model.YeterlikSurecID = basvuru.YeterlikSurecID;
                    model.YeterlikBasvuruID = basvuru.YeterlikBasvuruID;
                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = basvuru.KullaniciID;
                    model.AdSoyad = basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad;
                    model.OgrenciNo = basvuru.OgrenciNo;
                    model.OgrenimTipID = basvuru.OgrenimTipID;
                    model.KayitYil = basvuru.Kullanicilar.KayitYilBaslangic.Value;
                    model.KayitDonemID = basvuru.Kullanicilar.KayitDonemID.Value;
                    model.OkuduguDonemNo = ogrenciBilgi.OkuduguDonemNo;
                    model.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                    model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                }
                else
                {
                    var ogrenciBilgi = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(UserIdentity.Current.Id);
                    if (ogrenciBilgi.DanismanInfo == null || ogrenciBilgi.IsDanismanHesabiBulunamadi)
                    {
                        var msg = ogrenciBilgi.DanismanInfo == null
                            ? "Başvuru yapabilmeniz için danışman bilginizin OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz."
                            : $"Başvuru yapabilmeniz için danışmanınızın '{ogrenciBilgi.DanismanInfo.UNVAN_AD + " " + ogrenciBilgi.DanismanInfo.AD + " " + ogrenciBilgi.DanismanInfo.SOYAD}' lisansüstü sisteminde kullanıcı hesabı oluşturması gerekmektedir.";
                        MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, msg);
                        return RedirectToAction("Index");
                    }
                    var kul = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                    var ogrenimTip = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);


                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = UserIdentity.Current.Id;
                    model.AdSoyad = kul.Ad + " " + kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.OgrenimTipID = ogrenimTip.OgrenimTipID;
                    model.KayitYil = kul.KayitYilBaslangic.Value;
                    model.KayitDonemID = kul.KayitDonemID.Value;
                    model.OkuduguDonemNo = ogrenciBilgi.OkuduguDonemNo;
                    model.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                    model.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = kul.Programlar.ProgramAdi;
                }
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, errorMessage.ToArray());
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult BasvuruYap(KmYeterlikBasvuruDto kModel, string ekd = "")
        {
            var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var errprMessages = YeterlikBus.YeterlikBasvuruKontrol(enstituKod, kModel.UniqueID);
            kModel.KullaniciID = kayitYetki ? kModel.KullaniciID : UserIdentity.Current.Id;
            if (!errprMessages.Any())
            {
                var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                var ogrenciBilgi = KullanicilarBus.StudentControl(kullanici.TcKimlikNo);
                var ogrenimTip = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);


                if (kModel.UniqueID.HasValue)
                {
                    var data = _entities.YeterlikBasvurus.FirstOrDefault(p => p.UniqueID == kModel.UniqueID && p.KullaniciID == kModel.KullaniciID);
                    if (data == null) return RedirectToAction("Index");

                    data.OkuduguDonemNo = ogrenciBilgi.OkuduguDonemNo;
                    data.OgrenimTipID = ogrenimTip.OgrenimTipID;
                    data.OgrenciNo = kullanici.OgrenciNo;
                    data.ProgramKod = kullanici.ProgramKod;
                    data.KayitYil = kullanici.KayitYilBaslangic.Value;
                    data.KayitDonemID = kullanici.KayitDonemID.Value;
                    data.KayitTarihi = kullanici.KayitTarihi.Value;
                    data.YsBasToplamKrediKriteri = ogrenciBilgi.AktifDonemDers.ToplamKredi;
                    data.YsBasSeminerNotKriteri = ogrenciBilgi.AktifDonemDers.SeminerDersNotu;
                    data.YsBasEtikNotKriteri = ogrenciBilgi.AktifDonemDers.EtikDersNotu;
                    data.TezDanismanID = kullanici.DanismanID.Value;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Update, data.ToJson());

                }
                else
                {
                    var yeterlikSurecId = YeterlikBus.GetYeterlikAktifSurecId(enstituKod);
                    var kayit = _entities.YeterlikBasvurus.Add(new YeterlikBasvuru
                    {
                        UniqueID = Guid.NewGuid(),
                        YeterlikSurecID = yeterlikSurecId.Value,
                        BasvuruTarihi = DateTime.Now,
                        KullaniciID = UserIdentity.Current.Id,
                        OkuduguDonemNo = ogrenciBilgi.OkuduguDonemNo,
                        OgrenimTipID = ogrenimTip.OgrenimTipID,
                        OgrenciNo = kullanici.OgrenciNo,
                        ProgramKod = kullanici.ProgramKod,
                        KayitYil = kullanici.KayitYilBaslangic.Value,
                        KayitDonemID = kullanici.KayitDonemID.Value,
                        KayitTarihi = kullanici.KayitTarihi.Value,
                        YsBasToplamKrediKriteri = ogrenciBilgi.AktifDonemDers.ToplamKredi,
                        YsBasSeminerNotKriteri = ogrenciBilgi.AktifDonemDers.SeminerDersNotu,
                        YsBasEtikNotKriteri = ogrenciBilgi.AktifDonemDers.EtikDersNotu,
                        TezDanismanID = kullanici.DanismanID.Value,
                        IslemTarihi = DateTime.Now,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemYapanID = UserIdentity.Current.Id

                    });
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Insert, kayit.ToJson());
                }
                return RedirectToAction("Index");
            }
            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, errprMessages.ToArray());
            return RedirectToAction("Index");
        }
        [AllowAnonymous]
        public ActionResult GetDetail(Guid id, bool? isKomiteOrJuri, Guid? isDegerlendirme)
        {
            var query = _entities.YeterlikBasvurus.Select(s => new DmYeterlikDetayDto
            {
                UniqueID = s.UniqueID,
                IsKomiteOrJuri = isKomiteOrJuri,
                IsDegerlendirme = isDegerlendirme,
                YeterlikSurecID = s.YeterlikSurecID,
                ResimAdi = s.Kullanicilar.ResimAdi,
                AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                OgrenciNo = s.OgrenciNo,
                ProgramAdi = s.Programlar.ProgramAdi,
                AnabilimDaliID = s.Programlar.AnabilimDaliID,
                AnabilimdaliAdi = s.Programlar.AnabilimDallari.AnabilimDaliAdi,
                AbdYeterlikKomiteUyeCount = s.Programlar.AnabilimDallari.AnabilimDaliYeterlikKomiteUyeleris.Count,
                OgrenimTipAdi = s.OgrenimTipleri.OgrenimTipAdi,
                OkuduguDonemNo = s.OkuduguDonemNo,
                TezDanismanID = s.TezDanismanID,
                KayitDonemi = s.KayitYil + "/" + (s.KayitYil + 1) + " " + s.Donemler.DonemAdi,
                IsEnstituOnaylandi = s.IsEnstituOnaylandi,
                EnstituOnayTarihi = s.EnstituOnayTarihi,
                EnstituOnayAciklama = s.EnstituOnayAciklama,
                IsAbdKomitesiJuriyiOnayladi = s.IsAbdKomitesiJuriyiOnayladi,
                YaziliSinavTarihi = s.YaziliSinavTarihi,
                YaziliSinavYeri = s.YaziliSinavYeri,
                IsYaziliSinavinaKatildi = s.IsYaziliSinavinaKatildi,
                YaziliSinaviNotu = s.YaziliSinaviNotu,
                IsYaziliSinavBasarili = s.IsYaziliSinavBasarili,
                IsSozluSinavOnline = s.IsSozluSinavOnline,
                SozluSinavTarihi = s.SozluSinavTarihi,
                SozluSinavYeri = s.SozluSinavYeri,
                IsSozluSinavinaKatildi = s.IsSozluSinavinaKatildi,
                SozluSinaviOrtalamaNotu = s.SozluSinaviOrtalamaNotu,
                IsSozluSinavBasarili = s.IsSozluSinavBasarili,
                GenelBasariNotu = s.GenelBasariNotu,
                IsGenelSonucBasarili = s.IsGenelSonucBasarili,
                YeterlikSurecOgrenimTipleri = s.YeterlikSureci.YeterlikSurecOgrenimTipleris.FirstOrDefault(f => f.OgrenimTipID == s.OgrenimTipID),
                YeterlikBasvuruJuriUyeleris = s.YeterlikBasvuruJuriUyeleris.ToList(),
                DmYeterlikKomites = s.YeterlikBasvuruKomitelers.Select(sk => new DmYeterlikKomite
                {
                    UniqueID = sk.UniqueID,
                    KullaniciID = sk.KullaniciID,
                    AdSoyad = sk.Kullanicilar.Ad + " " + sk.Kullanicilar.Soyad,
                    UnvanAdi = sk.Kullanicilar.Unvanlar.UnvanAdi,
                    EMail = sk.Kullanicilar.EMail,
                    IsJuriOnaylandi = sk.IsJuriOnaylandi,
                    DegerlendirmeIslemTarihi = sk.DegerlendirmeIslemTarihi,
                    IsLinkGonderildi = sk.IsLinkGonderildi,
                    LinkGonderimTarihi = sk.LinkGonderimTarihi,


                }).ToList(),
            }).AsQueryable();

            if (!UserIdentity.Current.IsAuthenticated)
                query = query.Where(p =>
                    isKomiteOrJuri == true
                        ? p.DmYeterlikKomites.Any(a => a.UniqueID == isDegerlendirme)
                        : p.YeterlikBasvuruJuriUyeleris.Any(a => a.UniqueID == isDegerlendirme));
            else query = query.Where(p => p.UniqueID == id);
            var basvuru = query.First();
            var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == basvuru.TezDanismanID);
            basvuru.DanismanAdi = danisman.Unvanlar.UnvanAdi + " " + danisman.Ad + " " + danisman.Soyad;


            return View(basvuru);
        }

        public ActionResult GetYeterlikJuriFormu(Guid id)
        {

            var basvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == id);

            var komiteCount = _entities.AnabilimDaliYeterlikKomiteUyeleris.Count(p =>
                  p.AnabilimDaliID == basvuru.Programlar.AnabilimDaliID);
            if (komiteCount != 5)
            {
                return new
                {
                    success = false,
                    message = "Yeterlik jüri üyesi oluşturabilmeniz için " +
                              basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi +
                              " anabilim danı için enstitü tarafından 5 kişilik komite üyesinin belirlenmesi gerekmektedir. Bu durumu enstitü yetkililerine iletiniz."
                }.ToJsonResult();
            }


            var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == basvuru.TezDanismanID);
            var model = new KmYeterlikJuriModel() { UniqueID = id, YeterlikBasvuruJuriUyeleris = basvuru.YeterlikBasvuruJuriUyeleris };
            var juriDanisman = model.YeterlikBasvuruJuriUyeleris.FirstOrDefault(a => a.JuriTipAdi == "TezDanismani");
            if (juriDanisman == null) model.YeterlikBasvuruJuriUyeleris.Add(new YeterlikBasvuruJuriUyeleri
            {
                UniqueID = Guid.NewGuid(),
                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                EMail = danisman.EMail,
                JuriTipAdi = "TezDanismani",
                UnvanAdi = danisman.Unvanlar.UnvanAdi.Replace(" ", ""),
            });
            model.SelectListUndan = new SelectList(UnvanlarBus.GetCmbJuriUnvanlar(true), "Value", "Caption", null);
            model.SelectListUniversite = new SelectList(Management.cmbGetAktifUniversiteler(true), "Value", "Caption", null);
            model.SelectListAnabilimDali = new SelectList(Management.cmbGetAktifAnabilimDallariStr(basvuru.YeterlikSureci.EnstituKod, true), "Value", "Caption", null);

            var view = ViewRenderHelper.RenderPartialView("Yeterlik", "YeterlikJuriFormu", model);
            return new { success = true, view }.ToJsonResult();
        }

        public ActionResult YeterlikJuriFormuPost(KmYeterlikJuriModel kModel)
        {
            var mMessage = new MmMessage()
            {
                Title = "Yeterlik Jüri Üyeleri Tanımlama İşlemi.",
                IsSuccess = false,
                MessageType = Msgtype.Warning

            };
            var yeterlikBasvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == kModel.UniqueID);

            if (yeterlikBasvuru.IsEnstituOnaylandi != true)
            {
                mMessage.Messages.Add("Yeterlik jüri üyeleri tanımlaması yapılabilmesi için öğrenciye ait başvurunun enstitü tarafından onaylanması gerekmektedir!");
                return new { mMessage, mMessage.IsSuccess, kModel.SelectedTabId }.ToJsonResult();
            }
            var toplantiYetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
            if (!toplantiYetki && yeterlikBasvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Yeterlik jüri üyeleri tanımlaması için yetkili değilsiniz!");
                return new { mMessage, mMessage.IsSuccess, kModel.SelectedTabId }.ToJsonResult();

            }
            if (yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.Any(a => a.IsSonucOnaylandi.HasValue))
            {
                mMessage.Messages.Add("Onay işlemi yapan jüri üyeleri bulunduğundan jüri güncellemeleri yapılamaz!");
                return new { mMessage, mMessage.IsSuccess, kModel.SelectedTabId }.ToJsonResult();
            }
            var komiteUyeleri = _entities.AnabilimDaliYeterlikKomiteUyeleris.Where(p =>
                p.AnabilimDaliID == yeterlikBasvuru.Programlar.AnabilimDaliID).ToList().Select(s => new YeterlikBasvuruKomiteler
                {
                    YeterlikBasvuruID = yeterlikBasvuru.YeterlikBasvuruID,
                    UniqueID = Guid.NewGuid(),
                    KullaniciID = s.KullaniciID,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id

                }).ToList();
            if (komiteUyeleri.Count != 5)
            {
                mMessage.Messages.Add("Yeterlik jüri üyesi oluşturabilmeniz için " +
                                      yeterlikBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi +
                                      " anabilim danı için enstitü tarafından 5 kişilik komite üyesinin belirlenmesi gerekmektedir.");
                return new { mMessage, mMessage.IsSuccess, kModel.SelectedTabId }.ToJsonResult();
            }

            var juriData = (from unq in kModel.UniqueIDs.Select((s, inx) => new { s, inx })
                            join tabId in kModel.TabIds.Select((s, inx) => new { s, inx }) on unq.inx equals tabId.inx
                            join juritip in kModel.JuriTipAdis.Select((s, inx) => new { s, inx }) on unq.inx equals juritip.inx
                            join uni in kModel.UniversiteIDs.Select((s, inx) => new { s, inx }) on unq.inx equals uni.inx
                            join abd in kModel.AnabilimDaliAdis.Select((s, inx) => new { s, inx }) on unq.inx equals abd.inx
                            join ad in kModel.AdSoyads.Select((s, inx) => new { s, inx }) on unq.inx equals ad.inx
                            join unvan in kModel.UnvanAdis.Select((s, inx) => new { s, inx }) on unq.inx equals unvan.inx
                            join email in kModel.EMails.Select((s, inx) => new { s, inx }) on unq.inx equals email.inx
                            join isAsil in kModel.IsAsilOrYedeks.Select((s, inx) => new { s, inx }) on unq.inx equals isAsil.inx
                            select new
                            {
                                unq.inx,
                                UniqueID = unq.s,
                                TabId = tabId.s,
                                UniversiteID = uni.s,
                                AnabilimDaliAdi = abd.s,
                                JuriTipAdi = juritip.s,
                                AdSoyad = ad.s,
                                UnvanAdi = unvan.s,
                                EMail = email.s,
                                IsYtuIciOrDisi = juritip.s.Contains("YtuIci") || juritip.s == "TezDanismani",
                                IsAsilOrYedek = isAsil.s,
                                isSuccess = !ad.s.IsNullOrWhiteSpace() && uni.s.HasValue && !abd.s.IsNullOrWhiteSpace() && !unvan.s.IsNullOrWhiteSpace() && !email.s.ToIsValidEmail()
                            }).ToList();

            foreach (var item in juriData)
            {
                if (!item.isSuccess)
                {
                    if (item.JuriTipAdi == "TezDanismani")
                    {
                        mMessage.Messages.Add("1) Danışman Bilgileri eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuIciJuri1")
                    {
                        mMessage.Messages.Add("2) YTÜ içi Asil Üye bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuIciJuri2")
                    {
                        mMessage.Messages.Add("3) YTÜ içi Asil Üye bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuIciJuri3")
                    {
                        mMessage.Messages.Add("4) YTÜ içi Yedek bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuDisiJuri1")
                    {
                        mMessage.Messages.Add("1) YTÜ dışı Asil bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuDisiJuri2")
                    {
                        mMessage.Messages.Add("2) YTÜ dışı Asil  bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuDisiJuri3")
                    {
                        mMessage.Messages.Add("3) YTÜ dışı Yedek bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }

                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = item.UniversiteID.HasValue ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "Universite" });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = !item.AnabilimDaliAdi.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "AnabilimDali" });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = !item.AdSoyad.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "AdSoyad" });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = !item.UnvanAdi.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "UnvanAdi" });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = !item.EMail.ToIsValidEmail() ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "EMail" });

            }
            if (mMessage.Messages.Count > 0)
            {
                kModel.SelectedTabId = juriData.First(p => !p.isSuccess).TabId;
                mMessage.Title = "Yeterlik Jüri Üyeleri Aşağıdaki Sebeplerden Dolayı Oluşturulamadı.";
            }
            else
            {
                var universiteler = _entities.Universitelers.ToList();
                var juriEntitys = juriData.Select(s => new YeterlikBasvuruJuriUyeleri
                {
                    YeterlikBasvuruID = yeterlikBasvuru.YeterlikBasvuruID,
                    UniqueID = s.UniqueID,
                    UniversiteID = s.UniversiteID,
                    UniversiteAdi = universiteler.First(f => f.UniversiteID == s.UniversiteID).Ad,
                    AnabilimDaliAdi = s.AnabilimDaliAdi,
                    JuriTipAdi = s.JuriTipAdi,
                    UnvanAdi = s.UnvanAdi,
                    AdSoyad = s.AdSoyad,
                    EMail = s.EMail,
                    IsYtuIciOrDisi = s.IsYtuIciOrDisi,
                    IsSecilenJuri = s.IsAsilOrYedek == true,
                    IsAsilOrYedek = s.IsAsilOrYedek,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id
                }).ToList();
                if (!yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.Any())
                {
                    _entities.YeterlikBasvuruJuriUyeleris.AddRange(juriEntitys);
                }
                else
                {
                    foreach (var juriEntity in juriEntitys)
                    {
                        var yeterlikJuri = yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.FirstOrDefault(f => f.UniqueID == juriEntity.UniqueID);
                        if (yeterlikJuri != null)
                        {
                            yeterlikJuri.UniversiteID = juriEntity.UniversiteID;
                            yeterlikJuri.UniversiteAdi = juriEntity.UniversiteAdi;
                            yeterlikJuri.AnabilimDaliAdi = juriEntity.AnabilimDaliAdi;
                            yeterlikJuri.AdSoyad = juriEntity.AdSoyad;
                            yeterlikJuri.UnvanAdi = juriEntity.UnvanAdi;
                            yeterlikJuri.EMail = juriEntity.EMail;
                            yeterlikJuri.IsAsilOrYedek = juriEntity.IsAsilOrYedek;
                        }
                        else _entities.YeterlikBasvuruJuriUyeleris.Add(juriEntity);
                    }
                }
                _entities.SaveChanges();
                if (!yeterlikBasvuru.YeterlikBasvuruKomitelers.Any() || !yeterlikBasvuru.YeterlikBasvuruKomitelers.All(a =>
                        komiteUyeleri.Any(ak => ak.KullaniciID == a.KullaniciID)))
                {
                    _entities.YeterlikBasvuruKomitelers.RemoveRange(yeterlikBasvuru.YeterlikBasvuruKomitelers);
                    _entities.YeterlikBasvuruKomitelers.AddRange(komiteUyeleri);
                    _entities.SaveChanges();
                    YeterlikBus.SendMailKomiteDegerlendirmeLink(yeterlikBasvuru.UniqueID, null);
                }
                else
                {
                    var linkGonderilecekler = yeterlikBasvuru.YeterlikBasvuruKomitelers.Where(a => !a.IsLinkGonderildi.HasValue).ToList();
                    foreach (var komite in linkGonderilecekler)
                    {
                        YeterlikBus.SendMailKomiteDegerlendirmeLink(yeterlikBasvuru.UniqueID, komite.UniqueID);
                    }
                }
                LogIslemleri.LogEkle("YeterlikBasvuruJuriUyeleri", (!yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.Any() ? IslemTipi.Insert : IslemTipi.Update), juriEntitys.ToJson());
                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
            }
            return new { mMessage, mMessage.IsSuccess, kModel.SelectedTabId }.ToJsonResult();
        }
        public ActionResult KomiteBilgileriGuncelle(Guid id)
        {
            var mMessage = new MmMessage
            {
                Title = "ABD Komitesi Güncelleme İşlemi"
            };
            var basvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == id);
            var onayDuzeltmeYetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
            if (basvuru.YeterlikBasvuruKomitelers.Any(a => a.IsJuriOnaylandi.HasValue))
            {
                mMessage.Messages.Add("Komite üyeleri arasında değerlendirme yapanlar olduğu için komite güncelleme işlemi yapılamaz. Değerlendirmeleri kaldırıp tekar güncelleme işlemini deneyiniz.");
            }
            else if (!onayDuzeltmeYetki)
            {
                mMessage.Messages.Add("Komite üyeleri değerlendirme linki gönderim işlemi için yetkili değilsiniz.");
            }
            else
            {
                var abdKomiteler = _entities.AnabilimDaliYeterlikKomiteUyeleris
                    .Where(p => p.AnabilimDaliID == basvuru.Programlar.AnabilimDaliID).ToList();
                if (abdKomiteler.Count != 5)
                {
                    mMessage.Messages.Add("Komite güncellemesi yapılabilmesi için " +
                                          basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi +
                                          "anabilim dalında 5 adet komite tanımlı olması gerekmektedir.");

                }

                if (!mMessage.Messages.Any())
                {

                    _entities.YeterlikBasvuruKomitelers.RemoveRange(basvuru.YeterlikBasvuruKomitelers);
                    var komites = _entities.YeterlikBasvuruKomitelers.AddRange(abdKomiteler.Select(s => new YeterlikBasvuruKomiteler
                    {
                        UniqueID = Guid.NewGuid(),
                        IslemTarihi = DateTime.Now,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemYapanID = UserIdentity.Current.Id,
                        YeterlikBasvuruID = basvuru.YeterlikBasvuruID,
                        KullaniciID = s.KullaniciID,
                    }));
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvuruKomiteler", IslemTipi.Update, komites.ToJson());
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Komite üyeleri güncellendi.");
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return new { mMessage.IsSuccess, messageView }.ToJsonResult();
        }

        public ActionResult KomiteDegerlendirmeLinkiGonder(Guid uniqueId)
        {
            var mMessage = new MmMessage
            {
                Title = "ABD Komitesi Jüri Değerlendirme Linki Gönderme İşlemi"
            };

            var uye = _entities.YeterlikBasvuruKomitelers.FirstOrDefault(p => p.UniqueID == uniqueId);
            if (uye == null)
            {
                mMessage.Messages.Add("Değerlendirme Linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı Yenileyip Tekrar Deneyiniz.");

            }
            else
            {
                var juriDuzeltmeYetkisi = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
                var danismanYetkisi = uye.YeterlikBasvuru.TezDanismanID == UserIdentity.Current.Id;
                if (!juriDuzeltmeYetkisi && !danismanYetkisi)
                {
                    mMessage.Messages.Add("Komite üyeleri değerlendirme linki gönderim işlemi için yetkili değilsiniz.");
                }
                else
                {
                    if (!juriDuzeltmeYetkisi && uye.YeterlikBasvuru.YeterlikBasvuruKomitelers.Count ==
                        uye.YeterlikBasvuru.YeterlikBasvuruKomitelers.Count(c => c.IsJuriOnaylandi.HasValue))
                    {
                        mMessage.MessageType = Msgtype.Warning;
                        mMessage.Messages.Add(
                            "Değerlendirme işlemi tüm Komite üyeler tarafından tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
                    }
                }

            }

            if (!mMessage.Messages.Any())
            {
                var messages = YeterlikBus.SendMailKomiteDegerlendirmeLink(uye.YeterlikBasvuru.UniqueID, uniqueId);
                if (messages.IsSuccess)
                {

                    uye.YeterlikBasvuru.IsAbdKomitesiJuriyiOnayladi = null;
                    _entities.SaveChanges();
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Değerlendirme Linki Komite Üyesine Gönderildi.");

                }
                else
                {
                    mMessage.Messages.AddRange(messages.Messages);

                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return new { mMessage.IsSuccess, messageView }.ToJsonResult();
        }
        [AllowAnonymous]
        public ActionResult KomiteJuriOnay(Guid? uniqueId, bool? isGenelSonucBasarili)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Yeterlik Jüri Onay İşlemi"
            };
            var onayDuzeltmeYetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
            if (!uniqueId.HasValue)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Kayıt işlemi yapılamadı için benzersiz anahtar bilgisi boş gözükmekte.</span>");
            }
            else
            {
                var komite = _entities.YeterlikBasvuruKomitelers.FirstOrDefault(p => p.UniqueID == uniqueId);
                if (komite == null)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Onay işlemi yapmanız için size tanınan benzersiz anahtar bilgisi değişti veya bulunamadı!</span>");
                }
                else
                {

                    if (!onayDuzeltmeYetki && komite.IsJuriOnaylandi.HasValue)
                    {
                        mMessage.Messages.Add("<span style='color:maroon;'>Yeterlik Jürisi onaylama işlemini daha önceden zaten yaptınız!</span>");
                    }
                    else
                    {

                        if (!isGenelSonucBasarili.HasValue)
                        {
                            if (!onayDuzeltmeYetki) mMessage.Messages.Add("<span style='color:maroon;'>Onay Durumunu Seçiniz</span>");
                        }
                        if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Yeterlik Jürisi onaylama işlemi başarısız.");
                    }

                    if (!mMessage.Messages.Any())
                    {
                        komite.IsJuriOnaylandi = isGenelSonucBasarili;
                        komite.DegerlendirmeIslemTarihi = DateTime.Now;
                        komite.DegerlendirmeIslemYapanIP = UserIdentity.Ip;
                        komite.DegerlendirmeYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;

                        komite.IslemTarihi = DateTime.Now;
                        komite.IslemYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;
                        komite.IslemYapanIP = UserIdentity.Ip;
                        _entities.SaveChanges();
                        bool? herkesOnayladi = null;
                        if (komite.YeterlikBasvuru.YeterlikBasvuruKomitelers.All(a => a.IsJuriOnaylandi.HasValue))
                            herkesOnayladi = komite.YeterlikBasvuru.YeterlikBasvuruKomitelers.All(a => a.IsJuriOnaylandi.HasValue);
                        komite.YeterlikBasvuru.IsAbdKomitesiJuriyiOnayladi = herkesOnayladi;
                        _entities.SaveChanges();
                        mMessage.IsSuccess = true;
                        LogIslemleri.LogEkle("YeterlikBasvuruKomiteler", IslemTipi.Update, komite.ToJson());
                        if (herkesOnayladi == true)
                        {
                            var messages = YeterlikBus.SendMailKomiteDegerlendirmeSonuc(komite.YeterlikBasvuru.UniqueID);
                        }
                        if (komite.IsJuriOnaylandi == true) mMessage.Messages.Add("Onay işlemi yapıldı.");
                        if (komite.IsJuriOnaylandi != true) mMessage.Messages.Add("Onay işlemi kaldırıldı.");
                    }
                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return new { mMessage.IsSuccess, messageView }.ToJsonResult();
        }

        public ActionResult YaziliSinavKaydet(YeterlikBasvuru kModel)
        {
            var mmMessage = new MmMessage()
            {
                Title = "Yazılı Sınavı Tanımlama İşlemi",
            };
            var toplantiYetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
            var yeterlikBasvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == kModel.UniqueID);
            if (!toplantiYetki && yeterlikBasvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mmMessage.Messages.Add("Sınav oluşturmak için yetkili değilsiniz.");
            }
            else
            {
                if (yeterlikBasvuru.IsSozluSinavOnline.HasValue)
                {
                    mmMessage.Messages.Add("Sözlü sınavı bilgisi oluşturulduktan sonra yazılı sonavı düzenlenemez");
                }
                else
                {
                    if (!kModel.YaziliSinavTarihi.HasValue)
                    {
                        mmMessage.Messages.Add("Sınav Tarihi Giriniz");
                    }
                    if (kModel.YaziliSinavYeri.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Sınav Yeri Giriniz");
                    }
                    if (kModel.IsYaziliSinavinaKatildi == true)
                    {

                        if (!kModel.YaziliSinaviNotu.HasValue || kModel.YaziliSinaviNotu < 0 || kModel.YaziliSinaviNotu > 100)
                            mmMessage.Messages.Add("Sınavı notuna 0 ile 100 arasında bir değer giriniz.");
                        else if (yeterlikBasvuru.YaziliSinavTarihi > DateTime.Now)
                        {
                            mmMessage.Messages.Add("Sınav tarihi not girişi yaptığınız tarihten büyük olamaz!");
                        }
                    }
                }

                if (!mmMessage.Messages.Any())
                {
                    bool isSonucKaldirildi = yeterlikBasvuru.IsYaziliSinavinaKatildi.HasValue &&
                                             !kModel.IsYaziliSinavinaKatildi.HasValue;
                    bool sendMail;


                    if (kModel.IsYaziliSinavinaKatildi == true)
                    {
                        sendMail = yeterlikBasvuru.YaziliSinavTarihi != kModel.YaziliSinavTarihi ||
                               yeterlikBasvuru.YaziliSinavYeri != kModel.YaziliSinavYeri ||
                               yeterlikBasvuru.IsYaziliSinavinaKatildi != kModel.IsYaziliSinavinaKatildi ||
                               yeterlikBasvuru.YaziliSinaviNotu != kModel.YaziliSinaviNotu;
                    }
                    else
                    {
                        sendMail = yeterlikBasvuru.YaziliSinavTarihi != kModel.YaziliSinavTarihi ||
                                   yeterlikBasvuru.YaziliSinavYeri != kModel.YaziliSinavYeri ||
                                   yeterlikBasvuru.IsYaziliSinavinaKatildi != kModel.IsYaziliSinavinaKatildi;
                    }



                    yeterlikBasvuru.YaziliSinavTarihi = kModel.YaziliSinavTarihi;
                    yeterlikBasvuru.YaziliSinavYeri = kModel.YaziliSinavYeri;
                    yeterlikBasvuru.IsYaziliSinavinaKatildi = kModel.IsYaziliSinavinaKatildi;
                    yeterlikBasvuru.YaziliSinaviNotu = kModel.IsYaziliSinavinaKatildi == true ? kModel.YaziliSinaviNotu : null;
                    yeterlikBasvuru.IsYaziliSinavBasarili = null;
                    if (yeterlikBasvuru.YaziliSinaviNotu.HasValue)
                    {
                        var kriterler = yeterlikBasvuru.YeterlikSureci.YeterlikSurecOgrenimTipleris.First(p => p.OgrenimTipID == yeterlikBasvuru.OgrenimTipID);
                        yeterlikBasvuru.IsYaziliSinavBasarili = kriterler.YaziliGecerNot <= yeterlikBasvuru.YaziliSinaviNotu;

                    }
                    else if (yeterlikBasvuru.IsYaziliSinavinaKatildi == false)
                    {
                        yeterlikBasvuru.IsYaziliSinavBasarili = false;
                    }


                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Update, yeterlikBasvuru.ToJson());
                    mmMessage.Messages.Add("Kayıt işlemi yapıldı.");
                    mmMessage.IsSuccess = true;

                    if (!isSonucKaldirildi)
                    {
                        if (sendMail)
                        {

                            if (yeterlikBasvuru.IsYaziliSinavBasarili == false || yeterlikBasvuru.IsYaziliSinavinaKatildi == false || yeterlikBasvuru.IsSozluSinavinaKatildi.HasValue)
                            {
                                YeterlikBus.SendMailSinavJuriLink(yeterlikBasvuru.UniqueID);
                            }
                            else YeterlikBus.SendMailSinavBilgi(yeterlikBasvuru.UniqueID);
                        }
                    }
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                messageView,

            }.ToJsonResult();
        }

        public ActionResult SozluSinavKaydet(YeterlikBasvuru kModel)
        {
            var mmMessage = new MmMessage()
            {
                Title = "Sözlü Sınavı Tanımlama İşlemi",
            };
            var toplantiYetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
            var yeterlikBasvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == kModel.UniqueID);
            if (!toplantiYetki && yeterlikBasvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mmMessage.Messages.Add("Sınav oluşturmak için yetkili değilsiniz.");
            }
            else
            {
                if (kModel.IsSozluSinavOnline.HasValue)
                {
                    if (!kModel.SozluSinavTarihi.HasValue)
                    {
                        mmMessage.Messages.Add("Sınav Tarihi Giriniz");
                    }
                    if (kModel.SozluSinavYeri.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Sınav Yeri Giriniz");
                    }
                }
                else
                {
                    kModel.SozluSinavTarihi = null;
                    kModel.SozluSinavYeri = null;
                    kModel.IsSozluSinavinaKatildi = null;
                }

                if (!mmMessage.Messages.Any())
                {
                    bool sendMail = yeterlikBasvuru.SozluSinavTarihi != kModel.SozluSinavTarihi ||
                                    yeterlikBasvuru.SozluSinavYeri != kModel.SozluSinavYeri ||
                                    yeterlikBasvuru.IsSozluSinavinaKatildi != kModel.IsSozluSinavinaKatildi;

                    yeterlikBasvuru.IsSozluSinavOnline = kModel.IsSozluSinavOnline;
                    yeterlikBasvuru.SozluSinavTarihi = kModel.SozluSinavTarihi;
                    yeterlikBasvuru.SozluSinavYeri = kModel.SozluSinavYeri;
                    yeterlikBasvuru.IsSozluSinavinaKatildi = kModel.IsSozluSinavinaKatildi;
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Update, yeterlikBasvuru.ToJson());
                    mmMessage.Messages.Add("Kayıt işlemi yapıldı.");
                    mmMessage.IsSuccess = true;

                    if (sendMail && kModel.IsSozluSinavOnline.HasValue)
                    {
                        YeterlikBus.SendMailSinavBilgi(yeterlikBasvuru.UniqueID, false);
                        YeterlikBus.SendMailSinavJuriLink(yeterlikBasvuru.UniqueID, false);
                    }
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                messageView,

            }.ToJsonResult();
        }

        [AllowAnonymous]
        public ActionResult SinavDegerlendir(Guid uniqueId, bool? isSonucOnaylandi, int? sozluNotu)
        {
            var mmMessage = new MmMessage()
            {
                Title = "Yeterlik sınav değerlendirme işlemi"
            };
            var juri = _entities.YeterlikBasvuruJuriUyeleris.First(f => f.UniqueID == uniqueId);
            var basvuru = juri.YeterlikBasvuru;
            var duzeltmeyetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRole();
            var isSozluNotuIstensin = basvuru.IsYaziliSinavBasarili == true && basvuru.IsSozluSinavinaKatildi == true;

            if (isSozluNotuIstensin)
            {
                if (sozluNotu.HasValue)
                {
                    if (sozluNotu < 0 || sozluNotu > 100) mmMessage.Messages.Add("Sözlü notuna 0 ile 100 arasında bir değer giriniz.");
                }
                else if (!duzeltmeyetki) mmMessage.Messages.Add("Sözlü notuna giriniz.");
            }
            else
            {
                if (!duzeltmeyetki && !isSonucOnaylandi.HasValue)
                {
                    mmMessage.Messages.Add("Sınav sonucu onayını seçiniz.");
                }
            }


            if (!mmMessage.Messages.Any())
            {
                juri.DegerlendirmeTarihi = DateTime.Now;
                if (isSozluNotuIstensin)
                {
                    juri.SozluNotu = sozluNotu;
                    juri.IsSonucOnaylandi = juri.SozluNotu.HasValue ? true : (bool?)null;
                }
                else
                {
                    juri.IsSonucOnaylandi = isSonucOnaylandi;
                    juri.SozluNotu = null;
                }
                _entities.SaveChanges();
                var juriDegerlendirmeleri = basvuru.YeterlikBasvuruJuriUyeleris.Where(p => p.IsSecilenJuri).ToList();

                if (juriDegerlendirmeleri.All(a => a.IsSonucOnaylandi.HasValue))
                {


                    if (basvuru.IsYaziliSinavBasarili == true && basvuru.IsSozluSinavinaKatildi == true)
                    {
                        var kriterler = basvuru.YeterlikSureci.YeterlikSurecOgrenimTipleris.First(p =>
                                                 p.OgrenimTipID == basvuru.OgrenimTipID);
                        var sozluOrtalama = (decimal)juriDegerlendirmeleri.Sum(s => s.SozluNotu.Value) / juriDegerlendirmeleri.Count;
                        basvuru.SozluSinaviOrtalamaNotu = sozluOrtalama;
                        basvuru.IsSozluSinavBasarili = sozluOrtalama >= kriterler.SozluGecerNot;
                        if (basvuru.IsSozluSinavBasarili.Value)
                        {

                            var genelBasariNotu = (basvuru.YaziliSinaviNotu * kriterler.YaziliYuzde / 100) +
                                                  (sozluOrtalama * kriterler.SozluYuzde / 100);
                            basvuru.GenelBasariNotu = genelBasariNotu;
                            basvuru.IsGenelSonucBasarili = kriterler.OrtalamaGecerNot <= genelBasariNotu;
                        }
                        else
                        {
                            basvuru.GenelBasariNotu = null;
                            basvuru.IsGenelSonucBasarili = false;
                        }

                    }
                    else
                    {
                        if (basvuru.IsSozluSinavBasarili.HasValue)
                        {
                            basvuru.IsGenelSonucBasarili = basvuru.IsSozluSinavBasarili == true;
                        }
                        else if (basvuru.IsYaziliSinavinaKatildi == false || basvuru.IsYaziliSinavBasarili == false)
                        {
                            basvuru.IsGenelSonucBasarili = false;
                        }
                    }




                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Update, basvuru.ToJson());
                    YeterlikBus.SendMailSinavBilgi(basvuru.UniqueID, !basvuru.IsSozluSinavinaKatildi.HasValue);

                }
                else
                {
                    basvuru.GenelBasariNotu = null;
                    basvuru.IsGenelSonucBasarili = null;
                    _entities.SaveChanges();
                }

                mmMessage.IsSuccess = true;
                var msg = "";
                if (juri.IsSonucOnaylandi.HasValue) msg = isSozluNotuIstensin ? "Sözlü not girişi yapıldı." : "Sınav sonucu onaylandı.";
                else msg = isSozluNotuIstensin ? "Sözlü notu kaldırıldı." : "Sınav sonucu onayı kaldırıldı.";

                mmMessage.Messages.Add(msg);
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? (juri.IsSonucOnaylandi.HasValue ? Msgtype.Success : Msgtype.Warning) : Msgtype.Error;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new { mmMessage.IsSuccess, messageView }.ToJsonResult();

        }


        public ActionResult GetJuriDegisiklik(Guid uniqueId)
        {
            var juri = _entities.YeterlikBasvuruJuriUyeleris.First(p => p.UniqueID == uniqueId);
            if (juri.YeterlikBasvuru.YeterlikBasvuruJuriUyeleris.All(a => a.IsSonucOnaylandi.HasValue))
            {
                return new { IsSuccess = false, message = "Jüri üyeleri değerlendirme işlemini tamamladığından bu işlemi yapamazsınız." }.ToJsonResult();
            }
            var model = new KmYeterlikJuriDegisiklik
            {
                UniqueID = uniqueId,
                AdSoyad = juri.AdSoyad,
                UnvanAdi = juri.UnvanAdi,
                JuriTipAdi = juri.JuriTipAdi,
                AnabilimDaliAdi = juri.AnabilimDaliAdi,
                EMail = juri.EMail,
                SelectListYedekJuriler = new SelectList(YeterlikBus.GetCmbJuriYedekList(juri.UniqueID, true), "Value", "Caption", null)
            };

            var view = ViewRenderHelper.RenderPartialView("Yeterlik", "JuriDegisiklikView", model);
            return new { IsSuccess = true, view }.ToJsonResult();
        }
        public ActionResult JuriDegisiklikPost(KmYeterlikJuriDegisiklik kModel)
        {
            var mMessage = new MmMessage()
            {
                Title = "Jüri Bigli Güncelleme İşlemi"
            };
            var duzeltmeyetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRole();

            var juri = _entities.YeterlikBasvuruJuriUyeleris.First(p => p.UniqueID == kModel.UniqueID);
            if (!duzeltmeyetki)
            {
                mMessage.Messages.Add("Jüri üyeleri düzeltme işlemi için yetkili değilsiniz.");
            }
            else if (juri.YeterlikBasvuru.YeterlikBasvuruJuriUyeleris.All(a => a.IsSonucOnaylandi.HasValue))
            {
                mMessage.Messages.Add("Jüri üyeleri değerlendirme işlemini tamamladığından bu işlemi yapamazsınız.");
            }

            if (!mMessage.Messages.Any())
            {
                if (!kModel.EMailOrJuriDegisiklik.HasValue)
                {
                    mMessage.Messages.Add("Değişiklik şeklini seçiniz.");
                }
                else
                {
                    if (kModel.EMailOrJuriDegisiklik.Value)
                    {
                        if (kModel.EMail.IsNullOrWhiteSpace())
                        {
                            mMessage.Messages.Add("Jüriye ait e-posta bilgisi boş bırakılamaz.");
                        }
                        if (kModel.EMail.ToIsValidEmail())
                        {
                            mMessage.Messages.Add("E-posta bilgisi uygun formatta değil.");
                        }
                    }
                    else
                    {
                        if (!kModel.YeniJuriUniqueID.HasValue)
                        {
                            mMessage.Messages.Add("Yeni jüri üyesi seçiniz.");
                        }

                    }
                }

                if (!mMessage.Messages.Any())
                {
                    if (kModel.EMailOrJuriDegisiklik == true)
                    {
                        juri.EMail = kModel.EMail;
                    }
                    else
                    {

                        var yeniJuri =
                                juri.YeterlikBasvuru.YeterlikBasvuruJuriUyeleris.First(p => p.UniqueID == kModel.YeniJuriUniqueID);
                        yeniJuri.IsSecilenJuri = true;
                        juri.IsSecilenJuri = false;
                    }
                    _entities.SaveChanges();
                    mMessage.IsSuccess = true;
                    LogIslemleri.LogEkle("YeterlikBasvuruJuriUyeleri", IslemTipi.Update, juri.ToJson());

                }
            }





            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return new { mMessage.IsSuccess, messageView }.ToJsonResult();
        }
        public ActionResult GetKomiteDegisiklik(Guid uniqueId)
        {
            var komite = _entities.YeterlikBasvuruKomitelers.First(p => p.UniqueID == uniqueId);
            if (komite.IsJuriOnaylandi.HasValue)
            {
                return new { IsSuccess = false, message = "Komite üyesi değerlendirme işlemini tamamladığından bu işlemi yapamazsınız." }.ToJsonResult();
            }

            var komiteData = YeterlikBus.GetCmbKomiteDegisiklikList(komite.UniqueID, true);
            var model = new KmYeterlikKomiteDegisiklik
            {
                UniqueID = uniqueId,
                AnabilimDaliID = komite.YeterlikBasvuru.Programlar.AnabilimDaliID,
                AdSoyad = komite.Kullanicilar.Ad + " " + komite.Kullanicilar.Soyad,
                UnvanAdi = komite.Kullanicilar.Unvanlar.UnvanAdi,
                isDegiseBilecekKomiteVar = komiteData.Any(a => !a.Value.IsNullOrWhiteSpace()),
                SelectListYeniKomiteler = new SelectList(komiteData, "Value", "Caption", null)
            };

            var view = ViewRenderHelper.RenderPartialView("Yeterlik", "KomiteDegisiklikView", model);
            return new { IsSuccess = true, view }.ToJsonResult();
        }
        public ActionResult KomiteDegisiklikPost(KmYeterlikKomiteDegisiklik kModel)
        {
            var mMessage = new MmMessage()
            {
                Title = "Komite Değişiklik İşlemi"
            };
            var duzeltmeyetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRole();

            var varolankomite = _entities.YeterlikBasvuruKomitelers.First(p => p.UniqueID == kModel.UniqueID);
            if (!duzeltmeyetki)
            {
                mMessage.Messages.Add("Komite üye değişikliği işlemi için yetkili değilsiniz.");
            }
            else if (varolankomite.IsJuriOnaylandi.HasValue)
            {
                mMessage.Messages.Add("Komite üyesi değerlendirme işlemini tamamladığından bu işlemi yapamazsınız.");
            }

            if (!mMessage.Messages.Any())
            {


                if (!kModel.YeniKomiteID.HasValue)
                {
                    mMessage.Messages.Add("Yeni Komite üyesi seçiniz.");
                }
                if (!mMessage.Messages.Any())
                {
                    var abdKomite = varolankomite.YeterlikBasvuru.Programlar.AnabilimDallari.AnabilimDaliYeterlikKomiteUyeleris.First(p => p.KullaniciID == kModel.YeniKomiteID);
                    var yeniKomite = new YeterlikBasvuruKomiteler
                    {
                        UniqueID = Guid.NewGuid(),
                        YeterlikBasvuruID = varolankomite.YeterlikBasvuruID,
                        KullaniciID = abdKomite.KullaniciID,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
                    };
                    _entities.YeterlikBasvuruKomitelers.Remove(varolankomite);
                    _entities.YeterlikBasvuruKomitelers.Add(yeniKomite);
                    _entities.SaveChanges();
                    mMessage.IsSuccess = true;
                    LogIslemleri.LogEkle("YeterlikBasvuruKomiteler", IslemTipi.Delete, varolankomite.ToJson());
                    LogIslemleri.LogEkle("YeterlikBasvuruKomiteler", IslemTipi.Insert, yeniKomite.ToJson());

                }
            }





            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return new { mMessage.IsSuccess, messageView }.ToJsonResult();
        }

        public ActionResult JuriDegerlendirmeLinkiGonder(Guid uniqueId)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Jüri Üyesine Değerlendirme Linki Gönderme İşlemi"
            };
            var uye = _entities.YeterlikBasvuruJuriUyeleris.FirstOrDefault(p => p.UniqueID == uniqueId);
            if (uye == null)
            {
                mMessage.Messages.Add("Değerlendirme Linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı Yenileyip Tekrar Deneyiniz.");
            }
            else
            {
                var duzeltmeyetki = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
                var isDanisman = uye.YeterlikBasvuru.TezDanismanID == UserIdentity.Current.Id;
                if (!duzeltmeyetki && !isDanisman)
                {
                    mMessage.Messages.Add("Jüri üyeleri değerlendirme linki gönderim işlemi için yetkili değilsiniz.");
                }
                if (!mMessage.Messages.Any())
                {
                    var basvuru = uye.YeterlikBasvuru;
                    var juriDuzeltmeYetkisi = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
                    if (!juriDuzeltmeYetkisi && basvuru.TezDanismanID != UserIdentity.Current.Id)
                    {
                        mMessage.MessageType = Msgtype.Warning;
                        mMessage.Messages.Add("Değerlendirme Linki Göndermek İçin Yetkili Değilsiniz.");
                    }
                    else if (!juriDuzeltmeYetkisi && basvuru.YeterlikBasvuruJuriUyeleris.Count ==
                             basvuru.YeterlikBasvuruJuriUyeleris.Count(c => c.SozluNotu.HasValue))
                    {
                        mMessage.MessageType = Msgtype.Warning;
                        mMessage.Messages.Add(
                            "Değerlendirme işlemi tüm Jüri üyeler tarafından tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
                    }
                    else
                    {
                        mMessage = YeterlikBus.SendMailSinavJuriLink(basvuru.UniqueID,
                            !basvuru.IsSozluSinavinaKatildi.HasValue, uniqueId);
                        if (mMessage.IsSuccess)
                        {

                            mMessage.Messages.Add("Değerlendirme Linki Jüri Üyesine Gönderildi.");

                        }
                        else
                        {
                            mMessage.Messages.AddRange(mMessage.Messages);
                        }
                    }
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);
            return new { mMessage.IsSuccess, messageView }.ToJsonResult();
        }





        public ActionResult Sil(Guid uniqueId)
        {
            var kayit = _entities.YeterlikBasvurus.First(p => p.UniqueID == uniqueId);
            var adSoyad = kayit.Kullanicilar.Ad + " " + kayit.Kullanicilar.Soyad;
            var mmMessage = YeterlikBus.YeterlikBasvurusuSilKontrol(kayit.YeterlikBasvuruID);
            if (mmMessage.IsSuccess)
            {
                try
                {
                    _entities.YeterlikBasvurus.Remove(kayit);
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Delete, kayit.ToJson());
                    mmMessage.Messages.Add(adSoyad + " Öğrencisine ait Yeterlik başvurusu silindi.");
                    mmMessage.IsSuccess = true;

                }
                catch (Exception ex)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(adSoyad + " Öğrencisine ait Yeterlik başvurusu silinemedi.");
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "Yeterlik/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }

            }

            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new { mmMessage.IsSuccess, messageView }.ToJsonResult();
        }
        public ActionResult GetJuriData(string term)
        {
            var data = Management.getWsPersisOE(term);
            var kul2 = data.Table.Where(p => UnvanlarBus.JuriUnvanList.Contains(p.AKADEMIKUNVAN.ToJuriUnvanAdi())).Select(s => new
            {
                id = s.ADSOYAD,
                AdSoyad = s.ADSOYAD,
                text = s.ADSOYAD,
                BolumAdi = s.BOLUMADI.Replace("BÖLÜMÜ", ""),
                UnvanAdi = s.AKADEMIKUNVAN.ToJuriUnvanAdi(),
                EMail = s.KURUMMAIL
            }).OrderBy(o => o.AdSoyad).Take(25).ToList();

            return kul2.ToJsonResult();
        }
        public ActionResult GetBasvuruDurum(Guid id)
        {
            var q = _entities.YeterlikBasvurus.Where(p => p.UniqueID == id).Select(s => new
                    FrYeterlikBasvuruDto
            {
                IsEnstituOnaylandi = s.IsEnstituOnaylandi,
                EnstituOnayAciklama = s.EnstituOnayAciklama,
                IsJuriOlusturuldu = s.YeterlikBasvuruJuriUyeleris.Any(),
                IsAbdKomitesiJuriyiOnayladi = s.IsAbdKomitesiJuriyiOnayladi,
                YaziliSinavTarihi = s.YaziliSinavTarihi,
                YaziliSinavYeri = s.YaziliSinavYeri,
                IsYaziliSinavinaKatildi = s.IsYaziliSinavinaKatildi,
                YaziliSinaviNotu = s.YaziliSinaviNotu,
                IsYaziliSinavBasarili = s.IsYaziliSinavBasarili,
                IsSozluSinavOnline = s.IsSozluSinavOnline,
                SozluSinavTarihi = s.SozluSinavTarihi,
                SozluSinavYeri = s.SozluSinavYeri,
                IsSozluSinavinaKatildi = s.IsSozluSinavinaKatildi,
                SozluSinaviOrtalamaNotu = s.SozluSinaviOrtalamaNotu,
                GenelBasariNotu = s.GenelBasariNotu,
                IsGenelSonucBasarili = s.IsGenelSonucBasarili


            }).First();
            var messageView = ViewRenderHelper.RenderPartialView("Yeterlik", "BasvuruDurumView", q);
            return messageView.ToJsonResult();
        }


    }
}