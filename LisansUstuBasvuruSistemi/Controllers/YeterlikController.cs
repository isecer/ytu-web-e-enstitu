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
        public ActionResult Index(string ekd)
        {

            return Index(new FmYeterlikBasvuruDto { PageSize = 40 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmYeterlikBasvuruDto model, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            #region BilgiModel 
            model.AktifYeterlikSurecId = YeterlikBus.GetYeterlikAktifSurecId(enstituKod);

            var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);

            model.AdSoyad = kullanici.Ad + " " + kullanici.Soyad;
            model.EnstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
            if (model.AktifYeterlikSurecId.HasValue)
            {
                var surec = _entities.YeterlikSurecis.First(p => p.YeterlikSurecID == model.AktifYeterlikSurecId);
                model.IsAktifSurecBasvuruVar = surec.YeterlikBasvurus.Any(a => a.KullaniciID == kullanici.KullaniciID);
                model.DonemAdi = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi;

                model.IsYtuOgrencisi = kullanici.YtuOgrencisi && kullanici.OgrenimDurumID == OgrenimDurum.HalenOğrenci;
                model.IsEnstituYetki = kullanici.EnstituKod == enstituKod;

                model.IsOgrenimSeviyeYetki = surec.YeterlikSurecOgrenimTipleris.Any(a => a.OgrenimTipKod == kullanici.OgrenimTipKod);
                model.OgrenimTipAdis = string.Join(", ", surec.YeterlikSurecOgrenimTipleris.Select(s => s.OgrenimTipleri.OgrenimTipAdi).ToList());

                if (model.IsOgrenimSeviyeYetki)
                {
                    KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(kullanici.KullaniciID);
                }
            }

            #endregion


            var q = from yeterlikBasvuru in _entities.YeterlikBasvurus.Where(p => p.KullaniciID == UserIdentity.Current.Id)
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
                        AdSoyad = kullanicilar.Ad + " " + kullanici.Soyad,
                        OgrenciNo = yeterlikBasvuru.OgrenciNo,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        OkuduguDonemNo = yeterlikBasvuru.OkuduguDonemNo,
                        IsOnaylandi = yeterlikBasvuru.IsOnaylandi,
                        OnayAciklama = yeterlikBasvuru.OnayAciklama,
                        IsBasarili = yeterlikBasvuru.IsBasarili,

                    };
            model.RowCount = q.Count();
            //IndexModel.Toplam = model.RowCount;
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
                    _entities.YeterlikBasvurus.Add(new YeterlikBasvuru
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
                }
                return RedirectToAction("Index");
            }
            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, errprMessages.ToArray());
            return RedirectToAction("Index");
        }

        public ActionResult GetDetail(Guid id)
        {
            var basvuru = _entities.YeterlikBasvurus.Select(s => new DmYeterlikDetayDto
            {
                UniqueID = s.UniqueID,
                YeterlikSurecID = s.YeterlikSurecID,
                ResimAdi = s.Kullanicilar.ResimAdi,
                AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                OgrenciNo = s.OgrenciNo,
                ProgramAdi = s.Programlar.ProgramAdi,
                AnabilimdaliAdi = s.Programlar.AnabilimDallari.AnabilimDaliAdi,
                OgrenimTipAdi = s.OgrenimTipleri.OgrenimTipAdi,
                OkuduguDonemNo = s.OkuduguDonemNo,
                TezDanismanID = s.TezDanismanID,
                KayitDonemi = s.KayitYil + "/" + (s.KayitYil + 1) + " " + s.Donemler.DonemAdi,
                IsOnaylandi = s.IsOnaylandi,
                OnayTarihi = s.OnayTarihi,
                OnayAciklama = s.OnayAciklama,
                IsBasarili = s.IsBasarili,
                YeterlikBasvuruJuriUyeleris = s.YeterlikBasvuruJuriUyeleris,
                DmYeterlikKomites = s.YeterlikBasvuruKomitelers.Select(sk => new DmYeterlikKomite
                {
                    UniqueID = sk.UniqueID,
                    AdSoyad = sk.Kullanicilar.Ad + " " + sk.Kullanicilar.Soyad,
                    UnvanAdi = sk.Kullanicilar.Unvanlar.UnvanAdi,
                    EMail = sk.Kullanicilar.EMail

                }).ToList()
            }).First(p => p.UniqueID == id);
            var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == basvuru.TezDanismanID);
            basvuru.DanismanAdi = danisman.Unvanlar.UnvanAdi + " " + danisman.Ad + " " + danisman.Soyad;

            if (basvuru.IsOnaylandi == true)
            {

            }

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
                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                EMail = danisman.EMail,
                JuriTipAdi = "TezDanismani",
                UnvanAdi = danisman.Unvanlar.UnvanAdi.Replace(" ", ""),
            });
            model.SelectListUndan = new SelectList(UnvanlarBus.GetCmbJuriUnvanlar(true), "Value", "Caption", null);
            model.SelectListUniversite = new SelectList(Management.cmbGetAktifUniversiteler(true), "Value", "Caption", null);

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

            if (yeterlikBasvuru.IsOnaylandi != true)
            {
                mMessage.Messages.Add("Yeterlik jüri üyeleri tanımlaması yapılabilmesi için öğrenciye ait başvurunun enstitü tarafından onaylanması gerekmektedir!");
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
                                JuriTipAdi = juritip.s,
                                AdSoyad = ad.s,
                                UnvanAdi = unvan.s,
                                EMail = email.s,
                                IsAsilOrYedek = isAsil.s,
                                isSuccess = !ad.s.IsNullOrWhiteSpace() && uni.s.HasValue && !unvan.s.IsNullOrWhiteSpace() && !email.s.ToIsValidEmail()
                            }).ToList();

            foreach (var item in juriData)
            {
                if (!item.isSuccess)
                {
                    if (item.JuriTipAdi == "TezDanismani")
                    {
                        mMessage.Messages.Add("Danışman bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuIciJuri1")
                    {
                        mMessage.Messages.Add("YTÜ içi Jüri 1 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuIciJuri2")
                    {
                        mMessage.Messages.Add("YTÜ içi Jüri 2 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuDisiJuri1")
                    {
                        mMessage.Messages.Add("YTÜ dışı Jüri 1 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuDisiJuri2")
                    {
                        mMessage.Messages.Add("YTÜ dışı Jüri 2 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }
                    else if (item.JuriTipAdi == "YtuDisiJuri3")
                    {
                        mMessage.Messages.Add("YTÜ dışı Jüri 3 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                    }

                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = item.UniversiteID.HasValue ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "Universite" });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = !item.AdSoyad.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "AdSoyad" });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = !item.UnvanAdi.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "UnvanAdi" });
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = !item.EMail.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Error, PropertyName = item.JuriTipAdi + "EMail" });

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
                    JuriTipAdi = s.JuriTipAdi,
                    UnvanAdi = s.UnvanAdi,
                    AdSoyad = s.AdSoyad,
                    EMail = s.EMail,
                    IsAsilOrYedek = s.IsAsilOrYedek,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id
                }).ToList();
                if (!yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.Any())
                    _entities.YeterlikBasvuruJuriUyeleris.AddRange(juriEntitys);
                else
                {
                    foreach (var juriEntity in juriEntitys)
                    {
                        var yeterlikJuri = yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.FirstOrDefault(f => f.UniqueID == juriEntity.UniqueID);
                        if (yeterlikJuri != null)
                        {
                            yeterlikJuri.UniversiteID = juriEntity.UniversiteID;
                            yeterlikJuri.UniversiteAdi = juriEntity.UniversiteAdi;
                            yeterlikJuri.AdSoyad = juriEntity.AdSoyad;
                            yeterlikJuri.UnvanAdi = juriEntity.UnvanAdi;
                            yeterlikJuri.EMail = juriEntity.EMail;
                            yeterlikJuri.IsAsilOrYedek = juriEntity.IsAsilOrYedek;
                        }
                    }
                }

                _entities.YeterlikBasvuruKomitelers.RemoveRange(yeterlikBasvuru.YeterlikBasvuruKomitelers);
                _entities.YeterlikBasvuruKomitelers.AddRange(komiteUyeleri);
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
            }
            return new { mMessage, mMessage.IsSuccess, kModel.SelectedTabId }.ToJsonResult();
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
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
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

    }
}