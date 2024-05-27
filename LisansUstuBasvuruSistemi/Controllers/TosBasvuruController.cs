using BiskaUtil;
using Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TosBasvuruController : Controller
    {
        // GET: TosBasvuru
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string ekd, Guid? uniqueId, int? kullaniciId, Guid? isDegerlendirme = null)
        {
            if (!UserIdentity.Current.IsAuthenticated && isDegerlendirme == null) return RedirectToActionPermanent("Login", "Account");

            return Index(new FmTosBasvuru { UniqueId = uniqueId, KullaniciID = kullaniciId, IsDegerlendirme = isDegerlendirme, PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTosBasvuru model, string ekd)
        {
            if (!UserIdentity.Current.IsAuthenticated && model.IsDegerlendirme == null) return RedirectToActionPermanent("Login", "Account");

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);


            #region bilgiModel
            var bbModel = new IndexPageInfoDto();
            if (model.IsDegerlendirme == null)
            {
                bbModel.SistemBasvuruyaAcik = TiAyar.TezOneriSavunmaBasvuruAlimiAcik.GetAyarTi(enstituKod, "false").ToBoolean(false);

                if (model.KullaniciID.HasValue) model.KullaniciID = UserIdentity.Current.Id;
                model.KullaniciID = model.KullaniciID ?? UserIdentity.Current.Id;
                var kullKayitB = KullanicilarBus.OgrenciBilgisiGuncelleObs(model.KullaniciID.Value);
                var kul = _entities.Kullanicilars.First(p => p.KullaniciID == model.KullaniciID);

                if (kul.YtuOgrencisi)
                {
                    if (kullKayitB.KayitVar == false)
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Hesap bilgilerinizde bulunan YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";
                    }
                    else
                    {
                        if (kul.OgrenimTipKod.IsDoktora() && kul.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci)
                        {
                            bbModel.KullaniciTipYetki = true;
                            var donemBilgi = _entities.Donemlers.FirstOrDefault(p => p.DonemID == kul.KayitDonemID.Value);
                            if (donemBilgi != null)
                            {
                                bbModel.KayitDonemi = kul.KayitYilBaslangic + "/" + (kul.KayitYilBaslangic + 1);
                            }
                            if (kul.KayitTarihi.HasValue) bbModel.KayitDonemi += " " + kul.KayitTarihi.ToFormatDate();
                            model.AktifOgrenimIcinBasvuruVar = _entities.TIBasvurus.Any(a => a.KullaniciID == kul.KullaniciID && a.OgrenciNo == kul.OgrenciNo && a.ProgramKod == kul.ProgramKod);
                        }
                        else
                        {
                            bbModel.KullaniciTipYetki = false;
                            bbModel.KullaniciTipYetkiYokMsj = "Tez Önerisi Savunma başvurusu yapılabilmesi için Doktora öğrencisi olunması gerekmektedir.";

                        }
                        if (bbModel.KullaniciTipYetki)
                        {
                            if (kul.Programlar.AnabilimDallari.EnstituKod != enstituKod)
                            {
                                bbModel.KullaniciTipYetki = false;
                                bbModel.KullaniciTipYetkiYokMsj = "Kayıtlı olduğunuz program ve başvuru yapmaya çalıştığınız enstitü birbiri ile uyuşmamaktadır. Doğru enstitü sayfasından başvuru yaptığınızdan emin olunuz.";
                            }
                        }
                    }
                }
                else
                {
                    bbModel.KullaniciTipYetki = false;
                    bbModel.KullaniciTipYetkiYokMsj = "Hesap bilgilerinizde YTÜ Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için Tez Öneri Savunma başvurusu yapamazsınız. Sağ üst köşeden hesap bilgilerini düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
                }
                if (bbModel.KullaniciTipYetki)
                {
                    var otb = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);

                    bbModel.OgrenimDurumAdi = kul.OgrenimDurumlari.OgrenimDurumAdi;
                    bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                    bbModel.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    bbModel.ProgramAdi = kul.Programlar.ProgramAdi;
                    bbModel.OgrenciNo = kul.OgrenciNo;

                    if (bbModel.SistemBasvuruyaAcik)
                        TosBus.BasvuruOlustur(kul.KullaniciID);
                }


                bbModel.Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
                bbModel.Kullanici = kul;
            }
            #endregion


            var q = from s in _entities.ToBasvurus.Where(p => !model.IsDegerlendirme.HasValue || p.ToBasvuruSavunmas.Any(a => a.ToBasvuruSavunmaKomites.Any(a2 => a2.UniqueID == model.IsDegerlendirme)))
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join ab in _entities.AnabilimDallaris on s.Programlar.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in _entities.Enstitulers on e.EnstituKod equals en.EnstituKod
                    let ard = _entities.ToBasvuruSavunmas.Where(p => p.ToBasvuruID == s.ToBasvuruID).OrderByDescending(ot => ot.ToBasvuruSavunmaID).FirstOrDefault()
                    where s.EnstituKod == enstituKod && s.KullaniciID == (model.IsDegerlendirme.HasValue ? s.KullaniciID : model.KullaniciID.Value)
                    select new FrTosBasvuru()
                    {
                        UniqueID = s.UniqueID,
                        ToBasvuruID = s.ToBasvuruID,
                        TezDanismanID = s.TezDanismanID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        KullaniciID = s.KullaniciID,
                        UserKey = k.UserKey,
                        AdSoyad = k.Ad + " " + k.Soyad,
                        OgrenciNo = s.OgrenciNo,
                        ResimAdi = k.ResimAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifSavunmaNo = ard != null ? ard.SavunmaNo : (int?)null,
                        AktifDonemAdi = ard == null ? "----" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),
                        AktifDonemID = ard == null ? null : (ard.DonemBaslangicYil + "" + ard.DonemID),
                        DurumID = ard == null ? 0 : ard.ToBasvuruSavunmaDurumID,
                        IsOyBirligiOrCoklugu = ard != null ? ard.IsOyBirligiOrCoklugu : null,
                        DurumModel = new TosDurumDto
                        {
                            ToBasvuruSavunmaDurumID = ard.ToBasvuruSavunmaDurumID,
                            IsSrTalebiYapildi = ard != null && ard.SRTalepleris.Any(),
                            DegerlendirmeBasladi = ard != null && ard.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue),
                            IsOyBirligiOrCoklugu = ard.IsOyBirligiOrCoklugu
                        },
                    };


            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.IndexModel = indexModel;
            ViewBag.bModel = bbModel;
            return View(model);
        }

        [Authorize]
        public ActionResult GetToSavunmaFormu(Guid toUniqueId, Guid? tosUniqueId)
        {
            var model = new ToSavunmaSaveModel
            {
                UniqueID = toUniqueId
            };

            var mMessage = new MmMessage();
            string view = "";
            var toBasvuru = _entities.ToBasvurus.First(p => p.UniqueID == toUniqueId);
            var toBasvuruSavunma = toBasvuru.ToBasvuruSavunmas.FirstOrDefault(p => p.UniqueID == tosUniqueId);
            var degerlendirmeYetki = RoleNames.TosDegerlendirmeYap.InRoleCurrent() || toBasvuru.KullaniciID == UserIdentity.Current.Id;
            var basvuruAlimiAktif = TiAyar.TezOneriSavunmaBasvuruAlimiAcik.GetAyarTi(toBasvuru.EnstituKod, "false").ToBoolean().Value;
            var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(toBasvuru.KullaniciID);
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == toBasvuru.KullaniciID);
            if (!degerlendirmeYetki)
            {
                mMessage.Messages.Add("Tez Önerisi Savunma başvurusu oluşturma yetkisine sahip değilsiniz.");
            }
            else if (toBasvuruSavunma == null && !basvuruAlimiAktif)
            {
                mMessage.Messages.Add("Tez Öneri Savunma başvuru süreci pasif durumdadır. Yeni bir Tez Öneri Savunma başvurusu oluşturamazsınız. Detaylı bilgi almak için duyuruları takip edebilirsiniz ya da Enstitünüz ile görüşebilirsiniz.");
            }
            else if (tosUniqueId.HasValue && toBasvuruSavunma.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue))
            {
                mMessage.Messages.Add("Komite üyeleri değerlendirme yaptıktan sonra Tez Öneri Savunma formunda değişiklik yapamazsınız. ");
            }
            else if (!tosUniqueId.HasValue && toBasvuru.ToBasvuruSavunmas.Any(a => !a.ToBasvuruSavunmaDurumID.HasValue))
            {
                mMessage.Messages.Add("Süreci devam eden bir Tez Öneri Savunma formunuz bulunmakta yeni bir Tez Öneri Savunma başvurusu yapamazsınız. ");
            }
            else if (!tosUniqueId.HasValue && !kul.DanismanID.HasValue)
            {
                var danismanTc = studentInfo.OgrenciInfo.DANISMAN_TC1;
                if (!kul.DanismanID.HasValue && (danismanTc.IsNullOrWhiteSpace() || danismanTc.Length != 11))
                {
                    mMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
                }
                else if (!kul.DanismanID.HasValue)
                {
                    mMessage.Messages.Add("Tez danışmanı bilginiz OBS sisteminden boş gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin OBS sisteminde tanımlı olması gerekmektedir.");

                }
                else mMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
            }
            else if (toBasvuruSavunma == null && studentInfo.OgrenciTez.TEZ_DILI.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("OBS sisteminde tez bilgilerinize ait tez dili bilginiz boş gelmektedir. Bu durumu enstitü yetkililerine iletiniz.");

            }
            else
            {
                var donemBilgi = (toBasvuruSavunma?.SavunmaBasvuruTarihi ?? DateTime.Now).ToAkademikDonemBilgi();

                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);

                if (ogrenciBilgi.Hata)
                {
                    mMessage.Messages.Add("Öğrenci kimlik numarası bilgisi ile OBS sisteminden öğrenci bilgisi kontrol edilirken bir hata oluştu. Hata: " + ogrenciBilgi.HataMsj);
                }
                else
                {
                    if (toBasvuru.ToBasvuruSavunmas.Any(p => p.UniqueID != tosUniqueId && !p.ToBasvuruSavunmaDurumID.HasValue && p.DonemBaslangicYil == donemBilgi.BaslangicYil && p.DonemID == donemBilgi.DonemId))
                    {
                        mMessage.Messages.Add(donemBilgi.DonemAdiLong + " döneminde zaten bir tez öneri savunma başvurunuz bulunmakta!");
                    }
                    else if (UserIdentity.Current.Id == toBasvuru.KullaniciID && toBasvuruSavunma != null && toBasvuruSavunma.ToBasvuruSavunmaKomites.Any(a => a.LinkGonderenID.HasValue))
                    {
                        mMessage.Messages.Add("Komite üyeleri değerlendirme linki gönderildiğinden Rapor bilgisinde değişiklik yapamazsınız. ");
                    }
                }

                if (toBasvuruSavunma == null && !mMessage.Messages.Any())
                {

                    var tezOneriToplamSavunmaSavunmaHak = TiAyar.TezOneriToplamBasarisizTezOneriSavunmaHak
                        .GetAyarTi(toBasvuru.EnstituKod, "0").ToInt(0);
                    var basarisizTezOneriSavunmaSayisi = 0;
                    if (basarisizTezOneriSavunmaSayisi >= tezOneriToplamSavunmaSavunmaHak)
                    {
                        mMessage.Messages.Add("Tez Önerisi Savunması için başarısızlıkla sonuçlanan toplam savunma hakkı sayısını aştığınız için yeni Tez Önerisi Savunması yapamazsınız.");
                    }
                    else
                    {
                        if (TdoBus.IsAktifDanismanOneriVar(kul.KullaniciID))
                        {
                            mMessage.Messages.Add("Aktif bir Tez Danışman Öneri başvurunuz bulunmakta. Tez Önerisi Savunma başvurusu yapılabilmesi için bu sürecin tamamlanması gerekmektedir.");
                        }
                        //else if (TdoBus.IsAktifEsDanismanOneriVar(kul.KullaniciID))
                        //{
                        //    mMessage.Messages.Add("Aktif bir Tez Eş Danışman Öneri başvurunuz bulunmakta. Tez Önerisi Savunma başvurusu yapılabilmesi için bu sürecin tamamlanması gerekmektedir.");
                        //}
                    }

                }


                if (!tosUniqueId.HasValue)
                {
                    mMessage.Messages.AddRange(TosBus.TosKalanHakSavunmaBaslangicTarihKriter(toUniqueId).MessagesDialog.Where(p => !p.IsSucces).Select(s => s.Message));
                }
                if (mMessage.Messages.Count == 0)
                {
                    var danisman = _entities.Kullanicilars.First(f => f.KullaniciID == kul.DanismanID);


                    var tiks = ogrenciBilgi.TezIzlJuriBilgileri.Where(p => p.TEZ_DANISMAN != "1").ToList();
                    if (tiks.Count < 2)
                    {
                        mMessage.Messages.Add("Tik üye bilgileri OBS sisteminden alınamadı.");
                    }
                    if (mMessage.Messages.Count > 0)
                    {
                        mMessage.Messages.Add("Tez öneri savunma formunu oluşturabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                    }

                    else
                    {
                        var obsTik1 = tiks[0];
                        var obsTik2 = tiks[1];

                        var komites = new List<ToBasvuruSavunmaKomite>
                        {
                            new ToBasvuruSavunmaKomite()
                            {
                                IsTezDanismani = true,
                                UnvanAdi = studentInfo.OgrenciInfo.DANISMAN_UNVAN1.ToUpper().Trim().ToJuriUnvanAdi(),
                                AdSoyad = studentInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper().Trim(),
                                EMail = danisman.EMail,
                                UniversiteAdi = "YILDIZ TEKNİK ÜNİVERSİTESİ",
                                AnabilimdaliAdi = studentInfo.OgrenciInfo.ANABILIMDALI_AD.ToUpper(),

                            },
                            new ToBasvuruSavunmaKomite
                            {
                                TikNum = 1,
                                AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper(),
                                UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToUpper().Trim().ToJuriUnvanAdi(),
                                EMail = obsTik1.TEZ_IZLEME_JURI_EPOSTA,
                                UniversiteAdi = obsTik1.TEZ_IZLEME_JURI_UNIVER.ToUpper(),
                                AnabilimdaliAdi = obsTik1.TEZ_IZLEME_JURI_ANABLMDAL.ToUpper(),
                            },
                            new ToBasvuruSavunmaKomite
                            {
                                TikNum = 2,
                                AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY.ToUpper(),
                                UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN.ToUpper().Trim().ToJuriUnvanAdi(),
                                EMail = obsTik2.TEZ_IZLEME_JURI_EPOSTA,
                                UniversiteAdi = obsTik2.TEZ_IZLEME_JURI_UNIVER.ToUpper(),
                                AnabilimdaliAdi = obsTik2.TEZ_IZLEME_JURI_ANABLMDAL.ToUpper(),
                            }
                        };


                        if (mMessage.Messages.Count == 0)
                        {
                            model.IsTezDiliTr = studentInfo.IsTezDiliTr;

                            model.OgrenciAdSoyad = kul.Ad + " " + kul.Soyad + " - " + toBasvuru.OgrenciNo;
                            model.OgrenciProgramAdi = toBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " - " + toBasvuru.Programlar.ProgramAdi;
                            if (toBasvuruSavunma != null)
                            {
                                model.ToBasvuruSavunmaID = toBasvuruSavunma.ToBasvuruSavunmaID;
                                model.IsTezDiliTr = model.IsTezDiliTr;

                                model.YeniTezBaslikTr = toBasvuruSavunma.YeniTezBaslikTr;
                                model.YeniTezBaslikEn = toBasvuruSavunma.YeniTezBaslikEn;

                                model.CalismaRaporDosyaAdi = toBasvuruSavunma.CalismaRaporDosyaAdi;
                                model.CalismaRaporDosyaYolu = toBasvuruSavunma.CalismaRaporDosyaYolu;
                                model.IsYokDrBursiyeriVar = toBasvuruSavunma.IsYokDrBursiyeriVar;
                                model.YokDrOncelikliAlan = toBasvuruSavunma.YokDrOncelikliAlan;
                                model.ToBasvuruSavunmaKomites = toBasvuruSavunma.ToBasvuruSavunmaKomites.ToList();

                                if (!model.ToBasvuruSavunmaKomites.Any())
                                {
                                    model.ToBasvuruSavunmaKomites = komites;
                                }
                                else
                                {
                                    var isdegisiklikVar = false;
                                    foreach (var itemkom in komites)
                                    {
                                        var dbKom = model.ToBasvuruSavunmaKomites.FirstOrDefault(p =>
                                            p.IsTezDanismani == itemkom.IsTezDanismani && p.TikNum == itemkom.TikNum);
                                        if (dbKom == null)
                                        {
                                            isdegisiklikVar = true;
                                            model.ToBasvuruSavunmaKomites.Add(itemkom);
                                        }
                                        else
                                        {
                                            if (itemkom.AdSoyad != dbKom.AdSoyad
                                                || itemkom.UnvanAdi != dbKom.UnvanAdi
                                                || itemkom.UniversiteAdi != dbKom.UniversiteAdi
                                                || itemkom.AnabilimdaliAdi != dbKom.AnabilimdaliAdi)
                                            {
                                                isdegisiklikVar = true;
                                                dbKom.AdSoyad = itemkom.AdSoyad;
                                                dbKom.UnvanAdi = itemkom.UnvanAdi;
                                                dbKom.UniversiteAdi = itemkom.UniversiteAdi;
                                                dbKom.AnabilimdaliAdi = itemkom.AnabilimdaliAdi;
                                            }
                                        }

                                    }

                                    if (isdegisiklikVar)
                                    {
                                        mMessage.Title = "Komite Üye Bilgilerinde Değişikliğe Rastlandı!";
                                        mMessage.Messages.Add("<span style='color:maroon;'>Değişikliklerin formunuza yansıması için Kayıt işlemini tamamlayınız.</span>");
                                    }

                                }
                            }
                            else
                            {
                                model.ToBasvuruSavunmaKomites = komites;
                            }

                            var donemSelectedValue = (tosUniqueId.HasValue
                                ? (toBasvuruSavunma.DonemBaslangicYil + "" + toBasvuruSavunma.DonemID)
                                : (donemBilgi.BaslangicYil + "" + donemBilgi.DonemId));
                            model.SListDonemSecim = new SelectList(TosBus.CmbTosDonemListeBasvuru(toBasvuru.EnstituKod), "Value", "Caption", donemSelectedValue);

                            mMessage.MessageType = MsgTypeEnum.Information;
                            mMessage.IsSuccess = true;
                            view = ViewRenderHelper.RenderPartialView("TosBasvuru", "ToSavunmaFormu", model);
                        }
                    }

                }
            }


            if (mMessage.MessageType != MsgTypeEnum.Information) mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = view,
                Messages = strView
            }.ToJsonResult();

        }
        [Authorize]
        [ValidateInput(false)]
        public ActionResult ToSavunmaFormuPost(ToSavunmaSaveModel kModel, string donemSecim)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Tez Öneri Savunma Formu Oluşturma İşlemi"
            };

            var toBasvuru = _entities.ToBasvurus.First(p => p.UniqueID == kModel.UniqueID);
            kModel.ToBasvuruID = toBasvuru.ToBasvuruID;
            var toBasvuruSavunma = toBasvuru.ToBasvuruSavunmas.FirstOrDefault(p => p.ToBasvuruSavunmaID == kModel.ToBasvuruSavunmaID);
            var degerlendirmeYetki = RoleNames.TosDegerlendirmeYap.InRoleCurrent() || toBasvuru.KullaniciID == UserIdentity.Current.Id;
            var enstituYetki = RoleNames.TosGelenBasvuruKayit.InRole();
            var basvuruAlimiAktif = TiAyar.TezOneriSavunmaBasvuruAlimiAcik.GetAyarTi(toBasvuru.EnstituKod, "false")
                .ToBoolean(false);
            var studentInfo = KullanicilarBus.OgrenciBilgisiGuncelleObs(toBasvuru.KullaniciID);
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == toBasvuru.KullaniciID);
            int? baslangicYil = null;
            int? donemId = null;
            if (enstituYetki && !donemSecim.IsNullOrWhiteSpace())
            {
                baslangicYil = donemSecim.Substring(0, 4).ToInt(0);
                donemId = donemSecim.Substring(4, 1).ToInt(0);
            }
            var danismanTc = studentInfo.OgrenciInfo.DANISMAN_TC1;
            if (!degerlendirmeYetki)
            {
                mMessage.Messages.Add("kayıt yetkisine sahip değilsiniz.");
            }
            else if (toBasvuruSavunma == null && !basvuruAlimiAktif)
            {
                mMessage.Messages.Add("Tez Öneri Savunma başvuru süreci pasif durumdadır. Yeni bir Tez Öneri Savunma başvurusu oluşturamazsınız. Detaylı bilgi almak için duyuruları takip edebilirsiniz ya da Enstitünüz ile görüşebilirsiniz.");
            }
            else if (toBasvuru.ToBasvuruSavunmas.Any(a => a.ToBasvuruSavunmaID != kModel.ToBasvuruSavunmaID && !a.ToBasvuruSavunmaDurumID.HasValue))
            {
                mMessage.Messages.Add("Süreci devam eden bir Tez Öneri Savunma formunuz bulunmakta yeni bir Tez Öneri Savunma başvurusu yapamazsınız. ");
            }

            else if (toBasvuruSavunma == null && !kul.DanismanID.HasValue && (danismanTc.IsNullOrWhiteSpace() || danismanTc.Length != 11))
            {
                mMessage.Messages.Add("Tez danışmanınızın Tc Kimlik Numarası  bilgisi OBS sisteminden boş ya da hatalı gelmektedir.  Başvurunuzu gerçekleştirebilmeniz için danışman bilginizin düzgün bir şekilde OBS sisteminde tanımlı olması gerekmektedir. Bu durumu enstitü yetkililerine iletiniz.");
            }
            else if (toBasvuruSavunma == null && !kul.DanismanID.HasValue)
            {
                mMessage.Messages.Add("Tez danışmanınıza ait lisansutu.yildiz.edu.tr sisteminde kullanıcı hesabı bulunamadı. Başvurunuzu gerçekleştirebilmeniz için danışmanınızın lisansustu.yildiz.edu.tr sisteminde hesap oluşturarak üye olması gerekmektedir.");
            }
            else if (toBasvuruSavunma != null && toBasvuruSavunma.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue))
            {
                mMessage.Messages.Add("Komite üyelerinden herhangi biri değerlendirme yaptıktan sonra Tez Öneri Savunma formunda değişiklik yapılamaz.");
            }
            else
            {
                if (toBasvuruSavunma == null && !mMessage.Messages.Any())
                {

                    var tezOneriToplamBasarisizSavunmaSavunmaHak = TiAyar.TezOneriToplamBasarisizTezOneriSavunmaHak
                        .GetAyarTi(toBasvuru.EnstituKod, "0").ToInt(0);
                    var basarisizTezOneriSavunmaSayisi = 0;
                    if (basarisizTezOneriSavunmaSayisi >= tezOneriToplamBasarisizSavunmaSavunmaHak)
                    {
                        mMessage.Messages.Add("Tez Önerisi Savunması için başarısızlıkla sonuçlanan toplam savunma hakkı sayısını aştığınız için yeni Tez Önerisi Savunması yapamazsınız.");
                    }
                    else
                    {
                        if (TdoBus.IsAktifDanismanOneriVar(kul.KullaniciID))
                        {
                            mMessage.Messages.Add("Aktif bir Tez Danışman Öneri başvurunuz bulunmakta. Tez Önerisi Savunma başvurusu yapılabilmesi içim bu sürecinin tamamlanması gerekmektedir.");
                        }
                        //else if (TdoBus.IsAktifEsDanismanOneriVar(kul.KullaniciID))
                        //{
                        //    mMessage.Messages.Add("Aktif bir Tez Eş Danışman Öneri başvurunuz bulunmakta. Tez Önerisi Savunma başvurusu yapılabilmesi için bu sürecin tamamlanması gerekmektedir.");
                        //}
                    }
                }


                var isYeniKayit = toBasvuruSavunma == null;
                bool isDegisiklikVar = false;
                var donemBilgi = (isYeniKayit ? DateTime.Now : toBasvuruSavunma.SavunmaBasvuruTarihi).ToAkademikDonemBilgi();

                if (mMessage.Messages.Count == 0)
                {

                    if (kModel.Dosya == null && kModel.CalismaRaporDosyaYolu.IsNullOrWhiteSpace()) mMessage.Messages.Add("Çalışma Raporu Dosyası Seçiniz.");
                    else if (kModel.Dosya != null && !kModel.Dosya.FileName.IsPdfFile()) mMessage.Messages.Add("Çalışma Raporu Dosyası PDF türünde olmalıdır.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = ((kModel.Dosya == null && kModel.CalismaRaporDosyaYolu.IsNullOrWhiteSpace()) || (kModel.Dosya != null && !kModel.Dosya.FileName.IsPdfFile()) ? MsgTypeEnum.Warning : MsgTypeEnum.Success), PropertyName = "Dosya" });
                    if (!kModel.IsYokDrBursiyeriVar.HasValue)
                    {
                        mMessage.Messages.Add("100/2000 YÖK Bursiyeri Bilgisini Seçiniz");
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsYokDrBursiyeriVar" });
                    }
                    else
                    {
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IsYokDrBursiyeriVar" });
                        if (kModel.IsYokDrBursiyeriVar.Value)
                            if (kModel.YokDrOncelikliAlan.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("Öncelikli Alt Alan Adı Giriniz.");
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YokDrOncelikliAlan" });
                            }
                            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YokDrOncelikliAlan" });
                    }
                }
                if (mMessage.Messages.Count == 0)
                {

                    var tikNums = kModel.TikNums.Select((s, inx) => new { s, inx }).ToList();
                    var isTezDanismanis = kModel.IsTezDanismanis.Select((s, inx) => new { s, inx }).ToList();
                    var unvanAdis = kModel.UnvanAdis.Select((s, inx) => new { s, inx }).ToList();
                    var adSoyads = kModel.AdSoyads.Select((s, inx) => new { s, inx }).ToList();
                    var eMails = kModel.EMails.Select((s, inx) => new { s, inx }).ToList();
                    var universiteAdis = kModel.UniversiteAdis.Select((s, inx) => new { s, inx }).ToList();
                    var anabilimDaliAdis = kModel.AnabilimDaliAdis.Select((s, inx) => new { s, inx }).ToList();

                    var tiks = (from tikNum in tikNums
                                join tezD in isTezDanismanis on tikNum.inx equals tezD.inx
                                join unvan in unvanAdis on tikNum.inx equals unvan.inx
                                join adsoyad in adSoyads on tikNum.inx equals adsoyad.inx
                                join email in eMails on tikNum.inx equals email.inx
                                join uni in universiteAdis on tikNum.inx equals uni.inx
                                join abd in anabilimDaliAdis on tikNum.inx equals abd.inx
                                select new ToBasvuruSavunmaKomite
                                {
                                    UniqueID = Guid.NewGuid(),
                                    TikNum = tikNum.s,
                                    IsTezDanismani = tezD.s,
                                    UnvanAdi = unvan.s,
                                    AdSoyad = adsoyad.s,
                                    EMail = email.s,
                                    UniversiteAdi = uni.s,
                                    AnabilimdaliAdi = abd.s,
                                    IslemTarihi = DateTime.Now,
                                    IslemYapanID = UserIdentity.Current.Id,
                                    IslemYapanIP = UserIdentity.Ip

                                }).ToList();


                    foreach (var item in tiks)
                    {
                        var isUnvanSuccess = !item.UnvanAdi.IsNullOrWhiteSpace();
                        var isAdSoyadSuccess = !item.AdSoyad.IsNullOrWhiteSpace();
                        var isEMailSuccess = !item.EMail.IsNullOrWhiteSpace() && !item.EMail.ToIsValidEmail();
                        var isUniversiteAdiSuccess = !item.UniversiteAdi.IsNullOrWhiteSpace();
                        var isAnabilimdaliAdiSuccess = !item.AnabilimdaliAdi.IsNullOrWhiteSpace();

                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = isEMailSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error, PropertyName = "EMail_" + item.TikNum });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = isUniversiteAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error, PropertyName = "UniversiteAdi_" + item.TikNum });
                        mMessage.MessagesDialog.Add(new MrMessage { MessageType = isAnabilimdaliAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error, PropertyName = "AnabilimdaliAdi_" + item.TikNum });

                        if (!isUnvanSuccess || !isAdSoyadSuccess || !isEMailSuccess || !isUniversiteAdiSuccess || !isAnabilimdaliAdiSuccess)
                        {
                            mMessage.Messages.Add((item.IsTezDanismani ? "Tez Danışmanı" : "Tik Üyesi") + " için istenen bilgilerde eksik ya da hatalı girişler mevcut");
                        }
                    }


                    if (mMessage.Messages.Count == 0)
                    {
                        try
                        {

                            toBasvuruSavunma = isYeniKayit ? new ToBasvuruSavunma() : toBasvuruSavunma;
                            if (
                                   toBasvuruSavunma.IsTezDiliTr != kModel.IsTezDiliTr ||
                                   toBasvuruSavunma.IsYokDrBursiyeriVar != kModel.IsYokDrBursiyeriVar ||
                                   toBasvuruSavunma.YokDrOncelikliAlan != kModel.YokDrOncelikliAlan
                                  ) isDegisiklikVar = true;




                            if (isYeniKayit || isDegisiklikVar)
                            {
                                toBasvuruSavunma.SavunmaNo = kModel.SavunmaNo;
                                toBasvuruSavunma.UniqueID = Guid.NewGuid();
                                var formKodu = toBasvuruSavunma.UniqueID.ToString().Replace("-", "").Substring(0, 8).ToUpper();
                                while (_entities.TIBasvuruAraRapors.Any(a => a.FormKodu == formKodu))
                                {
                                    formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                                }
                                toBasvuruSavunma.FormKodu = formKodu;


                            }

                            _entities.ToBasvuruSavunmaKomites.RemoveRange(toBasvuruSavunma.ToBasvuruSavunmaKomites);
                            toBasvuruSavunma.ToBasvuruSavunmaKomites = tiks;
                            toBasvuruSavunma.DonemID = donemId ?? donemBilgi.DonemId;
                            toBasvuruSavunma.ToBasvuruID = kModel.ToBasvuruID;
                            toBasvuruSavunma.IsTezDiliTr = kModel.IsTezDiliTr;
                            toBasvuruSavunma.IsYokDrBursiyeriVar = kModel.IsYokDrBursiyeriVar.Value;
                            toBasvuruSavunma.YokDrOncelikliAlan = kModel.YokDrOncelikliAlan;
                            toBasvuruSavunma.IslemTarihi = DateTime.Now;
                            toBasvuruSavunma.IslemYapanID = UserIdentity.Current.Id;
                            toBasvuruSavunma.IslemYapanIP = UserIdentity.Ip;

                            if (kModel.Dosya != null)
                            {
                                FileHelper.Delete(toBasvuruSavunma.CalismaRaporDosyaYolu);
                                toBasvuruSavunma.CalismaRaporDosyaAdi = kModel.Dosya.FileName.GetFileName();
                                toBasvuruSavunma.CalismaRaporDosyaYolu = FileHelper.SaveToSavunmaDosya(kModel.Dosya);
                            }
                            toBasvuruSavunma.DonemBaslangicYil = (baslangicYil ?? donemBilgi.BaslangicYil);
                            toBasvuruSavunma.DonemID = (donemId ?? donemBilgi.DonemId);
                            if (isYeniKayit)
                            {
                                var td = _entities.Kullanicilars.First(p => p.KullaniciID == kul.DanismanID);
                                toBasvuru.TezDanismanID = td.KullaniciID;

                                toBasvuruSavunma.TezDanismanID = td.KullaniciID;
                                toBasvuruSavunma.SavunmaBasvuruTarihi = DateTime.Now;
                                toBasvuruSavunma.YeniTezBaslikTr = studentInfo.OgrenciTez.TEZ_BASLIK;
                                toBasvuruSavunma.YeniTezBaslikEn = studentInfo.OgrenciTez.TEZ_BASLIK_ENG;
                                toBasvuruSavunma = _entities.ToBasvuruSavunmas.Add(toBasvuruSavunma);
                            }


                            _entities.SaveChanges();
                            LogIslemleri.LogEkle("ToBasvuruSavunma", isYeniKayit ? LogCrudType.Insert : LogCrudType.Update, toBasvuruSavunma.ToJson());
                            foreach (var item in toBasvuruSavunma.ToBasvuruSavunmaKomites)
                            {
                                LogIslemleri.LogEkle("ToBasvuruSavunmaKomite", isYeniKayit ? LogCrudType.Insert : LogCrudType.Update, item.ToJson());
                            }
                            mMessage.IsSuccess = true;
                            int? srTalepId = null;
                            if (isDegisiklikVar || isYeniKayit)
                            {

                                if (isDegisiklikVar && !isYeniKayit && toBasvuruSavunma.SRTalepleris.Any()) srTalepId = toBasvuruSavunma.SRTalepleris.First().SRTalepID;
                                TosBus.SendMailTosBilgisi(toBasvuruSavunma.ToBasvuruSavunmaID, srTalepId);
                                if (srTalepId.HasValue && mMessage.IsSuccess)
                                    mMessage.Messages.Add("<br/><i class='fa fa-lg fa-envelope-o' style='font-size:11pt;'></i> <span style=font-size:10pt;'>Tez Önerisi Savunma bilgilerinde değişiklik yapıldığı için Tez Önerisi Savunma, Toplantı bilgileri Danışman ve Öğrenciye mail olarak tekrar gönderildi!</span>");

                            }
                        }
                        catch (Exception ex)
                        {
                            var hataMsj = "Kayıt işlemi sırasında bir hata oluştu! \r\nHata:" + ex.ToExceptionMessage();
                            mMessage.Messages.Add(hataMsj);
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(hataMsj, ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                        }


                    }

                }


            }
            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return new
            {
                mMessage
            }.ToJsonResult();
        }
        [Authorize]
        public ActionResult ToSavunmaFormu()
        {

            return View();
        }

        [Authorize]

        public ActionResult RezervasyonAl(Guid tosUniqueId)
        {
            var toplantiYetki = RoleNames.TosToplantiTalebiYap.InRoleCurrent();
            var toBasvuruSavunma = _entities.ToBasvuruSavunmas.First(p => p.UniqueID == tosUniqueId);
            var model = new SrTalepleriKayitDto();
            if (!toplantiYetki && toBasvuruSavunma.ToBasvuru.TezDanismanID != UserIdentity.Current.Id) model.YetkisizErisim = true;
            else
            {
                if (toBasvuruSavunma.SRTalepleris.Any())
                {

                    var srTalep = toBasvuruSavunma.SRTalepleris.First();
                    var tarih = model.IsSalonSecilsin ? srTalep.Tarih : (srTalep.Tarih.AddHours(srTalep.BasSaat.Hours).AddMinutes(srTalep.BasSaat.Minutes));

                    model.IsSalonSecilsin = srTalep.SRSalonID.HasValue;
                    model.IsOnline = srTalep.IsOnline;
                    model.SRTalepID = srTalep.SRTalepID;
                    model.SRTalepTipID = srTalep.SRTalepTipID;
                    model.EnstituKod = srTalep.EnstituKod;
                    model.TalepYapanID = srTalep.TalepYapanID;
                    model.SRSalonID = srTalep.SRSalonID;
                    model.SalonAdi = srTalep.SalonAdi;
                    model.Tarih = tarih;
                    model.HaftaGunID = srTalep.HaftaGunID;
                    model.BasSaat = srTalep.BasSaat;
                    model.BitSaat = srTalep.BitSaat;
                    model.DanismanAdi = srTalep.DanismanAdi;
                    model.EsDanismanAdi = srTalep.EsDanismanAdi;
                    model.TezOzeti = srTalep.TezOzeti;
                    model.TezOzetiHtml = srTalep.TezOzetiHtml;
                    model.SRDurumID = srTalep.SRDurumID;
                    model.SRDurumAciklamasi = srTalep.SRDurumAciklamasi;
                    model.IslemTarihi = srTalep.IslemTarihi;
                    model.IslemYapanID = srTalep.IslemYapanID;
                    model.IslemYapanIP = srTalep.IslemYapanIP;
                    model.Aciklama = srTalep.Aciklama;
                }
                else
                {

                    model.EnstituKod = toBasvuruSavunma.ToBasvuru.EnstituKod;
                    model.SRTalepTipID = 4;
                    model.TalepYapanID = toBasvuruSavunma.ToBasvuru.KullaniciID;
                    model.Tarih = DateTime.Now.Date;
                }

                model.UniqueID = toBasvuruSavunma.UniqueID;
            }

            return View(model);
        }
        [Authorize]
        [HttpPost]
        public ActionResult RezervasyonAlPost(SrTalepleriKayitDto kModel, bool isSendMail = true)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Öneri Savunma Toplantı Bilgileri",
                MessageType = MsgTypeEnum.Warning
            };
            var toBasvuruSavunma = _entities.ToBasvuruSavunmas.First(p => p.UniqueID == kModel.UniqueID);
            var srTalep = toBasvuruSavunma.SRTalepleris.FirstOrDefault();
            var toToplantiTalebiYap = RoleNames.TosToplantiTalebiYap.InRoleCurrent();
            var toDegerlendirmeDuzeltme = RoleNames.TosDegerlendirmeDuzeltme.InRoleCurrent();

            if (!toToplantiTalebiYap && toBasvuruSavunma.ToBasvuru.TezDanismanID != UserIdentity.Current.Id) kModel.YetkisizErisim = true;


            mmMessage.DialogID = toBasvuruSavunma.ToBasvuru.ToString();
            kModel.SRTalepTipID = 4;

            kModel.EnstituKod = toBasvuruSavunma.ToBasvuru.EnstituKod;
            if (kModel.YetkisizErisim)
            {
                mmMessage.Messages.Add("Tez Öneri Savunma Toplantı Kayıt işlemi yapmaya yetkili değilsiniz.");
            }
            else
            {
                if (toBasvuruSavunma.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue))
                {
                    mmMessage.Messages.Add("Komite üyelerinden herhangi biri değerlendirme yaptıktan sonra Toplantı bilgileri değiştirilemez.");
                }
            }
            kModel.SRTalepID = srTalep?.SRTalepID ?? 0;
            if (!kModel.IsOnline.HasValue)
            {

                mmMessage.Messages.Add("Toplantı Şekli seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsOnline" });
            }
            else
            {
                if (kModel.IsOnline == true && !TiAyar.TezOneriSavunmaSinaviOnlineYapilabilsin
                        .GetAyarTi(toBasvuruSavunma.ToBasvuru.EnstituKod).ToBoolean(false))
                {
                    mmMessage.Messages.Add("Tez Öneri Savunma Sınavı Online olarak yapılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsOnline" });
                }
                else if (kModel.IsOnline == false && !TiAyar.TezOneriSavunmaSinaviYuzYuzeYapilabilsin
                             .GetAyarTi(toBasvuruSavunma.ToBasvuru.EnstituKod).ToBoolean(false))
                {
                    mmMessage.Messages.Add("Tez Öneri Savunma Sınavı Yüz Yüze olarak yapılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsOnline" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "IsOnline" });
            }


            if (mmMessage.Messages.Count == 0)
            {

                if (kModel.Tarih == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Tarih Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                else if (kModel.BasSaat == TimeSpan.MinValue)
                {
                    mmMessage.Messages.Add("Saat Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                else if (!toDegerlendirmeDuzeltme && kModel.Tarih < DateTime.Now)
                {
                    mmMessage.Messages.Add("Toplantı tarihi bilgisi günümüz tarihten küçük olamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                if (kModel.SalonAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add(kModel.IsOnline == true ? "Toplantı katılım linkini giriniz." : "Toplantı yeri bilgisini giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SalonAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "SalonAdi" });

                if (!mmMessage.Messages.Any())
                {
                    var basvuruKiterKontrolMsg = TosBus.TosKalanHakSavunmaBaslangicTarihKriter(toBasvuruSavunma.ToBasvuru.UniqueID, kModel.Tarih)
                        .MessagesDialog.Where(p => !p.IsSucces).Select(s => s.Message).ToList();
                    if (basvuruKiterKontrolMsg.Any()) mmMessage.Messages.AddRange(basvuruKiterKontrolMsg);
                }

                if (mmMessage.Messages.Count == 0)
                {
                    try
                    {
                        kModel.IslemTarihi = DateTime.Now;
                        kModel.IslemYapanID = UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        var tarih = kModel.Tarih;
                        toBasvuruSavunma.SavunmaNo = TosBus.TosSavunmaNo(toBasvuruSavunma.ToBasvuru.UniqueID, toBasvuruSavunma.UniqueID, tarih);


                        kModel.Tarih = tarih.Date;
                        kModel.HaftaGunID = (int)tarih.DayOfWeek;
                        kModel.BasSaat = kModel.IsSalonSecilsin ? kModel.BasSaat : tarih.TimeOfDay;
                        kModel.BitSaat = kModel.IsSalonSecilsin ? kModel.BitSaat : kModel.BasSaat.Add(new TimeSpan(2, 0, 0));
                        kModel.SRDurumID = SrTalepDurumEnum.Onaylandı;
                        kModel.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                        kModel.IslemTarihi = kModel.IslemTarihi;
                        kModel.IslemYapanID = kModel.IslemYapanID;
                        kModel.IslemYapanIP = kModel.IslemYapanIP;
                        kModel.SRTaleplerJuris = toBasvuruSavunma.ToBasvuruSavunmaKomites.Select(s => new SRTaleplerJuri
                        {
                            JuriTipAdi = s.IsTezDanismani ? "Tez Danışmanı" : "Tik Üyesi",
                            AnabilimdaliProgramAdi = s.AnabilimdaliAdi,
                            UniversiteAdi = s.UniversiteAdi,
                            UnvanAdi = s.UnvanAdi,
                            JuriAdi = s.UnvanAdi + " " + s.AdSoyad,
                            Telefon = "",
                            Email = s.EMail,
                            IslemTarihi = DateTime.Now,
                            IslemYapanID = UserIdentity.Current.Id,
                            IslemYapanIP = UserIdentity.Ip
                        }).ToList();

                        if (!toDegerlendirmeDuzeltme)
                        {
                            isSendMail = srTalep == null || (srTalep.IsOnline != kModel.IsOnline || srTalep.SalonAdi != kModel.SalonAdi || srTalep.Tarih != kModel.Tarih || srTalep.BasSaat != kModel.BasSaat);
                        }

                        bool isNewRecord = false;
                        if (srTalep == null)
                        {
                            toBasvuruSavunma.ToBasvuru.IsBasvuruKriterMuaf = false;
                            isNewRecord = true;
                            srTalep = _entities.SRTalepleris.Add(new SRTalepleri
                            {
                                UniqueID = Guid.NewGuid(),
                                ToBasvuruSavunmaID = toBasvuruSavunma.ToBasvuruSavunmaID,
                                IsOnline = kModel.IsOnline ?? false,
                                EnstituKod = kModel.EnstituKod,
                                MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID,
                                SRTalepTipID = kModel.SRTalepTipID,
                                TalepYapanID = kModel.TalepYapanID,
                                SRSalonID = null,
                                SalonAdi = kModel.SalonAdi,
                                Tarih = kModel.Tarih,
                                HaftaGunID = kModel.HaftaGunID,
                                BasSaat = kModel.BasSaat,
                                BitSaat = kModel.BitSaat,
                                Aciklama = kModel.Aciklama,
                                SRDurumID = kModel.SRDurumID,
                                IslemTarihi = kModel.IslemTarihi,
                                IslemYapanID = kModel.IslemYapanID,
                                IslemYapanIP = kModel.IslemYapanIP

                            });

                        }
                        else
                        {

                            srTalep.ToBasvuruSavunmaID = toBasvuruSavunma.ToBasvuruSavunmaID;
                            srTalep.SRTalepTipID = kModel.SRTalepTipID;
                            srTalep.IsOnline = kModel.IsOnline ?? false;
                            srTalep.SalonAdi = kModel.SalonAdi;
                            srTalep.TalepYapanID = kModel.TalepYapanID;
                            srTalep.SRSalonID = null;
                            srTalep.Tarih = kModel.Tarih;
                            srTalep.HaftaGunID = kModel.HaftaGunID;
                            srTalep.BasSaat = kModel.BasSaat;
                            srTalep.BitSaat = kModel.BitSaat;
                            srTalep.DanismanAdi = kModel.DanismanAdi;
                            srTalep.EsDanismanAdi = kModel.EsDanismanAdi;
                            srTalep.SRDurumID = kModel.SRDurumID;
                            srTalep.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                            srTalep.IslemTarihi = kModel.IslemTarihi;
                            srTalep.IslemYapanID = kModel.IslemYapanID;
                            srTalep.IslemYapanIP = kModel.IslemYapanIP;
                        }
                        _entities.SaveChanges();
                        LogIslemleri.LogEkle("SRTalepleri", isNewRecord ? LogCrudType.Insert : LogCrudType.Update, srTalep.ToJson());

                        mmMessage.IsSuccess = true;
                        mmMessage.MessageType = MsgTypeEnum.Success;
                        mmMessage.Messages.Add("Komite toplantı bilgisi düzenlendi.");
                        #region SendMail

                        if (isSendMail)
                        {
                            var messages = TosBus.SendMailTosBilgisi(null, srTalep.SRTalepID);
                            mmMessage.Messages.Add(messages.IsSuccess
                                ? "<br/><i class='fa fa-envelope-o'></i> <span style=font-size:10pt;'>Toplantı bilgisi Komite üyelerine ve öğrenciye mail olarak gönderildi.</span>"
                                : "<br/><i class='fa fa-lg fa-envelope-o' style='font-size:11pt;'></i> <span style=font-size:10pt;'>Toplantı bilgisi Komite üyelerine ve öğrenciye mail olarak gönderilemedi!</span>");
                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        mmMessage.IsSuccess = false;
                        mmMessage.MessageType = MsgTypeEnum.Error;
                        mmMessage.Messages.Add("İşlem yapılırken bir hata oluştu.");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Tez Öneri Savunma toplantı bilgisi oluşturulurken bir hata oluştu! Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Kritik);
                    }

                }

            }

            return mmMessage.ToJsonResult();
        }
        [ValidateInput(false)]
        public ActionResult TosDegerlendir(Guid? tosKomiteUniqueId, bool? isCalismaRaporuAltAlanUygun, int? toBasvuruSavunmaDurumId, string yeniTezBaslikTr, string yeniTezBaslikEn, string aciklama)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Öneri Savunma Değerlendirme İşlemi"
            };
            var degerlendirmeDuzeltmeYetki = RoleNames.TosDegerlendirmeDuzeltme.InRoleCurrent();
            bool isRefresh = false;
            if (!tosKomiteUniqueId.HasValue)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme için gerekli benzersiz anahtar bilgisi boş gelmektedir.</span>");
            }
            else
            {
                var komite = _entities.ToBasvuruSavunmaKomites.FirstOrDefault(p => p.UniqueID == tosKomiteUniqueId);
                if (komite == null)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme işlemi yapmanız için size tanınan benzersiz anahtar bilgisi değişti veya bulunamadı!</span>");
                }
                else
                {

                    bool isTezDanismani = komite.IsTezDanismani;
                    if (!degerlendirmeDuzeltmeYetki)
                    {
                        var toplanti = komite.ToBasvuruSavunma.SRTalepleris.First();

                        var toplantiTarihi = toplanti.Tarih.Add(toplanti.BasSaat);
                        if (DateTime.Now < toplantiTarihi)
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez Önerisi Savunma değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + toplantiTarihi.ToLongDateString() + " " +
                                                  $"{toplanti.BasSaat:hh\\:mm}" + "'</b> dan önce yapılamaz!</span>");
                        }
                        else if (komite.ToBasvuruSavunmaDurumID.HasValue)
                        {
                            mMessage.IsSuccess = true;
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez Önerisi Savunma değerlendirme işlemini daha önceden zaten yaptınız!</span>");
                        }

                        else
                        {

                            if (isTezDanismani)
                            {
                                if (komite.ToBasvuruSavunma.IsYokDrBursiyeriVar && !isCalismaRaporuAltAlanUygun.HasValue)
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Çalışma Raporu İle 100/2000 YÖK Bursu Alt Alan Uyumlu mu?</span>");
                                }

                                if (toBasvuruSavunmaDurumId.HasValue)
                                {
                                    if (yeniTezBaslikTr.IsNullOrWhiteSpace())
                                        mMessage.Messages.Add("<span style='color:maroon;'>Önerilen Tez Başlığı Türkçe</span>");

                                    if (yeniTezBaslikEn.IsNullOrWhiteSpace())
                                        mMessage.Messages.Add("<span style='color:maroon;'>Önerilen Tez Başlığı İngilizce</span>");
                                }

                            }


                            if (!toBasvuruSavunmaDurumId.HasValue)
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme Sonucu</span>");
                            }
                            else if (toBasvuruSavunmaDurumId.Value != ToBasvuruSavunmaDurumuEnum.KabulEdildi && aciklama.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme Açıklaması</span>");
                            }
                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Aşağıda istenen verileri cevaplayınız.");
                        }
                    }
                    else
                    {
                        var toplanti = komite.ToBasvuruSavunma.SRTalepleris.First();
                        var toplantiTarihi = toplanti.Tarih.Add(toplanti.BasSaat);
                        if (!UserIdentity.Current.IsAdmin && DateTime.Now < toplantiTarihi)
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Tez Önerisi Savunma değerlendirme işlemi başarısız.<br/>Değerlendirme işlemi toplantı tarihi olan <b>'" + toplantiTarihi.ToLongDateString() + " " +
                                                  $"{toplanti.BasSaat:hh\\:mm}" + "'</b> tarihinden önce yapılamaz!</span>");
                        }
                        else
                        {


                            if (toBasvuruSavunmaDurumId.HasValue)
                            {
                                if (isTezDanismani)
                                {
                                    if (komite.ToBasvuruSavunma.IsYokDrBursiyeriVar &&
                                        !isCalismaRaporuAltAlanUygun.HasValue)
                                    {
                                        mMessage.Messages.Add(
                                            "<span style='color:maroon;'>Çalışma Raporu İle 100/2000 YÖK Bursu Alt Alan Uyumlu mu?</span>");
                                    }

                                    if (yeniTezBaslikTr.IsNullOrWhiteSpace())
                                        mMessage.Messages.Add("<span style='color:maroon;'>Önerilen Tez Başlığı Türkçe</span>");

                                    if (yeniTezBaslikEn.IsNullOrWhiteSpace())
                                        mMessage.Messages.Add("<span style='color:maroon;'>Önerilen Tez Başlığı İngilizce</span>");
                                }

                                if (toBasvuruSavunmaDurumId.Value != ToBasvuruSavunmaDurumuEnum.KabulEdildi && aciklama.IsNullOrWhiteSpace())
                                {
                                    mMessage.Messages.Add("<span style='color:maroon;'>Değerlendirme Açıklaması</span>");
                                }
                            }


                            if (mMessage.Messages.Any()) mMessage.Messages.Insert(0, "Aşağıda istenen verileri cevaplayınız.");
                        }
                    }
                    if (!mMessage.Messages.Any() && toBasvuruSavunmaDurumId.HasValue)
                    {
                        var degerlendirmeGroups = komite.ToBasvuruSavunma.ToBasvuruSavunmaKomites.Where(p => p.ToBasvuruSavunmaDurumID.HasValue && p.ToBasvuruSavunmaKomiteID != komite.ToBasvuruSavunmaKomiteID).GroupBy(g => new
                        {
                            g.ToBasvuruSavunmaDurumID,
                            g.ToBasvuruSavunmaDurumlari.DurumAdi
                        })
                             .Select(s => new { s.Key.ToBasvuruSavunmaDurumID, s.Key.DurumAdi }).ToList();

                        if (degerlendirmeGroups.Count >= 2 && degerlendirmeGroups.All(a => a.ToBasvuruSavunmaDurumID != toBasvuruSavunmaDurumId))
                        {
                            mMessage.Messages.Add("<span style='color:maroon;'>Yapılan değerlendirmeler için gerekli olan <b>oy birliği veya oy çokluğu</b> kuralının sağlanabilmesi için aşağıdaki değerlendirmelerden birini yapmanız gerekmektedir.</span>");

                            degerlendirmeGroups.ForEach(f =>
                            {
                                mMessage.Messages.Add("<span style='color:green;'><b> " + f.DurumAdi + "</b></span>");
                            });
                        }

                    }

                    if (!mMessage.Messages.Any())
                    {
                        var birOncekiDegerlendirme = komite.ToBasvuruSavunma.ToBasvuru.ToBasvuruSavunmas
                            .Where(o => o.ToBasvuruSavunmaID < komite.ToBasvuruSavunmaID)
                            .OrderByDescending(o => o.ToBasvuruSavunmaID).FirstOrDefault();
                        if (birOncekiDegerlendirme != null &&
                            birOncekiDegerlendirme.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.Duzeltme &&
                            toBasvuruSavunmaDurumId == ToBasvuruSavunmaDurumuEnum.Duzeltme)
                        {
                            mMessage.Messages.Add(
                                "<span style='color:maroon;'>Öğrenci bir önceki savunmadan düzeltme aldığı için tekrar düzeltme verilemez!</span>");
                        }
                    }
                    if (!mMessage.Messages.Any())
                    {
                        var sendMailLink = isTezDanismani && toBasvuruSavunmaDurumId.HasValue && !komite.ToBasvuruSavunma.ToBasvuruSavunmaKomites.Any(a => a.IsLinkGonderildi.HasValue);
                        var isDegisiklikVar = komite.ToBasvuruSavunmaDurumID != toBasvuruSavunmaDurumId || komite.Aciklama != aciklama;
                        if (isTezDanismani && !isDegisiklikVar && komite.ToBasvuruSavunma.IsYokDrBursiyeriVar) isDegisiklikVar = komite.IsCalismaRaporuAltAlanUygun != isCalismaRaporuAltAlanUygun;
                        komite.IsCalismaRaporuAltAlanUygun = isCalismaRaporuAltAlanUygun;
                        komite.ToBasvuruSavunmaDurumID = toBasvuruSavunmaDurumId;
                        komite.Aciklama = aciklama;
                        if (isTezDanismani)
                        {
                            komite.ToBasvuruSavunma.YeniTezBaslikTr = yeniTezBaslikTr;
                            komite.ToBasvuruSavunma.YeniTezBaslikEn = yeniTezBaslikEn;
                        }
                        komite.DegerlendirmeIslemTarihi = DateTime.Now;
                        komite.DegerlendirmeIslemYapanIP = UserIdentity.Ip;
                        komite.DegerlendirmeYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;

                        komite.IslemTarihi = DateTime.Now;
                        komite.IslemYapanID = UserIdentity.Current != null ? UserIdentity.Current.Id : (int?)null;
                        komite.IslemYapanIP = UserIdentity.Ip;
                        if (isDegisiklikVar)
                        {
                            komite.ToBasvuruSavunma.UniqueID = Guid.NewGuid();
                            var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            while (_entities.TIBasvuruAraRapors.Any(a => a.FormKodu == formKodu))
                            {
                                formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            }
                            komite.ToBasvuruSavunma.FormKodu = formKodu;
                        }
                        _entities.SaveChanges();
                        LogIslemleri.LogEkle("ToBasvuruSavunmaKomite", LogCrudType.Update, komite.ToJson());
                        mMessage.IsSuccess = true;
                        if (sendMailLink)
                        {
                            var messages = TosBus.SendMailTosDegerlendirmeLink(komite.ToBasvuruSavunma.UniqueID, null, true);
                            if (messages.IsSuccess)
                            {
                                mMessage.Messages.Add("Değerlendirme Linki Komite Üyelerine Gönderildi.");
                            }
                            else
                            {
                                mMessage.Messages.AddRange(messages.Messages);
                                mMessage.Messages.Add("Değerlendirmeniz geri alınmıştır, Lütfen tekrar değerlendirme yapınız.");
                                mMessage.IsSuccess = false;
                                isRefresh = true;
                                komite.IsCalismaRaporuAltAlanUygun = null;
                                komite.ToBasvuruSavunmaDurumID = null;
                                komite.Aciklama = null;
                                komite.DegerlendirmeIslemTarihi = null;
                                komite.DegerlendirmeIslemYapanIP = null;
                                komite.DegerlendirmeYapanID = null;
                                _entities.SaveChanges();
                            }
                        }
                        else mMessage.Messages.Add("Değerlendirme işlemi tamamlandı.");




                        var isDegerlendirmeTamam = komite.ToBasvuruSavunma.ToBasvuruSavunmaKomites.All(a => a.ToBasvuruSavunmaDurumID.HasValue);
                        var toBasvuruSavunma = komite.ToBasvuruSavunma;
                        var toBasvuruSavunmaKomites = toBasvuruSavunma.ToBasvuruSavunmaKomites;
                        if (isDegerlendirmeTamam)
                        {
                            var groupDurumData = toBasvuruSavunmaKomites.GroupBy(g => g.ToBasvuruSavunmaDurumID).Select(s => new { ToBasvuruSavunmaDurumID = s.Key, Count = s.Count() }).ToList();
                            toBasvuruSavunma.ToBasvuruSavunmaDurumID = groupDurumData.OrderByDescending(o => o.Count).First().ToBasvuruSavunmaDurumID;
                            toBasvuruSavunma.IsOyBirligiOrCoklugu = groupDurumData.Count == 1;
                            toBasvuruSavunma.ToBasvuru.IlkOneriBitisTarihi = null;
                            toBasvuruSavunma.ToBasvuru.IkinciOneriBitisTarihi = null;
                            toBasvuruSavunma.ToBasvuru.RetDuzeltmeBitisTarihi = null;
                            _entities.SaveChanges();
                            var messages = TosBus.SendMailTosDegerlendirmeLink(toBasvuruSavunma.UniqueID, null, false);
                            if (isTezDanismani || degerlendirmeDuzeltmeYetki)
                            {
                                if (messages.IsSuccess)
                                {
                                    mMessage.Messages.Add("Değerlendirme Sonucu Danışman ve Öğrenciye E-Posta Olarak Gönderildi.");

                                }
                                else
                                {
                                    mMessage.Messages.AddRange(messages.Messages);
                                    mMessage.IsSuccess = false;
                                }
                            }




                        }
                        else
                        {

                            toBasvuruSavunma.IsOyBirligiOrCoklugu = null;
                            toBasvuruSavunma.ToBasvuruSavunmaDurumID = null;
                        }
                        _entities.SaveChanges();
                        LogIslemleri.LogEkle("ToBasvuruSavunmaKomite", LogCrudType.Update, komite.ToJson());
                        LogIslemleri.LogEkle("ToBasvuruSavunma", LogCrudType.Update, toBasvuruSavunma.ToJson());
                    }
                }
            }
            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);
            return Json(new { mMessage.IsSuccess, messageView, IsRefresh = isRefresh }, "application/json", JsonRequestBehavior.AllowGet);
        }



        public ActionResult SavunmaBitisTarihGuncelle(Guid toUniqueId, DateTime? ilkOneriBitisTarihi, DateTime? ikinciOneriBitisTarihi)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false
            };
            var tosDegerlendirmeDuzeltmeRole = RoleNames.TosSavunmaBitisTarihDuzeltme.InRoleCurrent();
            if (!UserIdentity.Current.IsAdmin && !tosDegerlendirmeDuzeltmeRole)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Bu işlemi yapmaya yetkili değilsiniz.</span>");

            }
            else
            {
                var toBasvuru = _entities.ToBasvurus.First(f => f.UniqueID == toUniqueId);
                if (!ilkOneriBitisTarihi.HasValue || !ikinciOneriBitisTarihi.HasValue)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>1. savunma bitiş ve 2. savunma bitiş tarihleri boş bırakılamaz.</span>");
                }
                else if (ilkOneriBitisTarihi.Value <= toBasvuru.YeterlikSozluSinavTarihi)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>1. savunma bitiş tarihi Yeterlik başarı tarihinden büyük olmalı.</span>");
                }
                else if (ilkOneriBitisTarihi.Value >= ikinciOneriBitisTarihi)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>1. savunma bitiş tarihi 2. savunma bitiş tarihinden büyük olmalı.</span>");
                }
                else
                {
                    toBasvuru.IlkOneriBitisTarihi = ilkOneriBitisTarihi;
                    toBasvuru.IkinciOneriBitisTarihi = ikinciOneriBitisTarihi;
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("ToBasvuru", LogCrudType.Update, toBasvuru.ToJson());

                    mMessage.Messages.Add("Tez Önerisi savunma bitiş tarihi bilgileri güncellendi.");
                    mMessage.IsSuccess = true;
                }
            }


            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);


            return Json(new { mMessage.IsSuccess, messageView }, "application/json", JsonRequestBehavior.AllowGet);

        }
        public ActionResult SavunmaRetDuezltmeTarihGuncelle(Guid toUniqueId, DateTime? retDuzeltmeBitisTarihi)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false
            };
            var tosDegerlendirmeDuzeltmeRole = RoleNames.TosSavunmaBitisTarihDuzeltme.InRoleCurrent();
            if (!tosDegerlendirmeDuzeltmeRole)
            {
                mMessage.Messages.Add("<span style='color:maroon;'>Bu işlemi yapmaya yetkili değilsiniz.</span>");

            }
            else
            {
                var toBasvuru = _entities.ToBasvurus.First(f => f.UniqueID == toUniqueId);
                var sonSavunma = toBasvuru.ToBasvuruSavunmas.OrderByDescending(o => o.ToBasvuruSavunmaID)
                    .First(f => f.ToBasvuruSavunmaDurumID.HasValue);
                if (!retDuzeltmeBitisTarihi.HasValue)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Ret/Düzeltme bitiş tarihi boş bırakılamaz.</span>");
                }
                else if (retDuzeltmeBitisTarihi.Value <= sonSavunma.SRTalepleris.First().Tarih)
                {
                    mMessage.Messages.Add("<span style='color:maroon;'>Ret/Düzeltme bitiş tarihi Son savunma sınavı tarihinden büyük olmalı.</span>");
                }
                else
                {
                    toBasvuru.RetDuzeltmeBitisTarihi = retDuzeltmeBitisTarihi;
                    toBasvuru.IlkOneriBitisTarihi = null;
                    toBasvuru.IkinciOneriBitisTarihi = null;
                    _entities.SaveChanges();
                    mMessage.Messages.Add("Tez Önerisi savunması ret/düzeltme bitiş tarihi bilgileri güncellendi.");
                    mMessage.IsSuccess = true;
                    LogIslemleri.LogEkle("ToBasvuru", LogCrudType.Update, toBasvuru.ToJson());
                }
            }


            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);


            return Json(new { mMessage.IsSuccess, messageView }, "application/json", JsonRequestBehavior.AllowGet);

        }
        [Authorize]
        public ActionResult SilDetay(Guid tosUniqueId)
        {
            var mmMessage = TosBus.GetTosSilKontrol(tosUniqueId);
            if (mmMessage.IsSuccess)
            {
                var tezOneriSavunma = _entities.ToBasvuruSavunmas.First(f => f.UniqueID == tosUniqueId);

                try
                {

                    _entities.SRTalepleris.RemoveRange(tezOneriSavunma.SRTalepleris);
                    _entities.ToBasvuruSavunmaKomites.RemoveRange(tezOneriSavunma.ToBasvuruSavunmaKomites);
                    _entities.ToBasvuruSavunmas.Remove(tezOneriSavunma);
                    _entities.SaveChanges();
                    FileHelper.Delete(tezOneriSavunma.CalismaRaporDosyaYolu);
                    LogIslemleri.LogEkle("ToBasvuruSavunma", LogCrudType.Delete, tezOneriSavunma.ToJson());
                    if (tezOneriSavunma.SRTalepleris.Any()) LogIslemleri.LogEkle("SRTalepleri", LogCrudType.Delete, tezOneriSavunma.SRTalepleris.ToJson());
                    if (tezOneriSavunma.ToBasvuruSavunmaKomites.Any()) LogIslemleri.LogEkle("ToBasvuruSavunmaKomite", LogCrudType.Delete, tezOneriSavunma.ToBasvuruSavunmaKomites.ToJson());

                    mmMessage.Messages.Add(tezOneriSavunma.SavunmaBasvuruTarihi + " Tarihli Tez Önerisi Savunma silindi.");
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(tezOneriSavunma.SavunmaBasvuruTarihi + " Tarihli Tez Önerisi Savunma silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }

            }
            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, messageView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult DegerlendirmeLinkView(Guid? tosKomiteUniqueId)
        {
            var model = _entities.ToBasvuruSavunmaKomites.First(p => p.UniqueID == tosKomiteUniqueId);
            return View(model);
        }
        [Authorize]
        public ActionResult DegerlendirmeLinkiGonder(Guid tosUniqueId, Guid? tosKomiteUniqueId, string eMail)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Önerisi Savunma Değerlendirme Linki Gönderme İşlemi"
            };
            var toBasvuruSavunma = _entities.ToBasvuruSavunmas.First(p => p.UniqueID == tosUniqueId);
            var basvuru = toBasvuruSavunma.ToBasvuru;
            var tiTezDegerlendirmeDuzeltme = RoleNames.TiTezDegerlendirmeDuzeltme.InRoleCurrent();
            if (!tiTezDegerlendirmeDuzeltme && basvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Değerlendirme Linki Göndermek İçin Yetkili Değilsiniz.");
            }
            else if (!tiTezDegerlendirmeDuzeltme && toBasvuruSavunma.ToBasvuruSavunmaKomites.Count == toBasvuruSavunma.ToBasvuruSavunmaKomites.Count(c => c.ToBasvuruSavunmaDurumID.HasValue))
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Tüm Komite üyeleri tarafından değerlendirme işlemi tamamlandığı için tekrar değerlendirme linki gönderemezsiniz.");
            }
            else if (eMail.IsNullOrWhiteSpace())
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("E-Posta Giriniz");
            }
            else if (eMail.ToIsValidEmail())
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("E-Posta Formatı Uygun Değildir.");
            }
            else
            {
                if (tosKomiteUniqueId.HasValue)
                {
                    var uye = toBasvuruSavunma.ToBasvuruSavunmaKomites.FirstOrDefault(p => p.UniqueID == tosKomiteUniqueId);
                    if (uye == null) mMessage.Messages.Add("Değerlendirme Linki göndermek için benzersiz anahtar bilgisi değişti veya bulunamadı! Sayfayı Yenileyip Tekrar Deneyiniz.");
                    else
                    {
                        uye.EMail = eMail;
                        _entities.SaveChanges();

                    }
                }
                var messages = TosBus.SendMailTosDegerlendirmeLink(tosUniqueId, tosKomiteUniqueId, true);
                if (messages.IsSuccess)
                {

                    toBasvuruSavunma.ToBasvuruSavunmaDurumID = null;
                    toBasvuruSavunma.IsOyBirligiOrCoklugu = null;

                    _entities.SaveChanges();
                    mMessage.IsSuccess = true;
                    mMessage.Messages.Add("Değerlendirme Linki Komite Üyesine Gönderildi.");

                }
                else
                {
                    mMessage.Messages.AddRange(messages.Messages);

                }
            }
            var messageView = mMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage) : "";
            return new { mMessage.IsSuccess, messageView }.ToJsonResult();
        }
        [Authorize]
        public ActionResult KriterMuafSet(Guid toUniqueId, bool isMuaf)
        {
            var mMessage = new MmMessage
            {
                IsSuccess = false
            };

            if (!(RoleNames.TosDegerlendirmeDuzeltme.InRoleCurrent() || UserIdentity.Current.IsAdmin))
            {
                mMessage.Messages.Add("Bu işlemi yapmak için yetkili değilsiniz.");
            }

            if (!mMessage.Messages.Any())
            {
                var toBasvuru = _entities.ToBasvurus.FirstOrDefault(f => f.UniqueID == toUniqueId);
                if (toBasvuru != null)
                {
                    toBasvuru.IsBasvuruKriterMuaf = isMuaf;
                    toBasvuru.IslemTarihi = DateTime.Now;
                    toBasvuru.IslemYapanID = UserIdentity.Current.Id;
                    toBasvuru.IslemYapanIP = UserIdentity.Ip;
                    _entities.SaveChanges();
                    mMessage.IsSuccess = true;
                    var ogrenci = toBasvuru.Kullanicilar;
                    if (isMuaf) mMessage.Messages.Add(ogrenci.Ad + " " + ogrenci.Soyad + " isimli öğrenci başvuru kriterinden <b>muaf tutuldu.</b>");
                    else mMessage.Messages.Add(ogrenci.Ad + " " + ogrenci.Soyad + " isimli öğrenci başvuru kriterinden <b>muaf kriteri kaldırıldı.</b>");
                    LogIslemleri.LogEkle("ToBasvuru", LogCrudType.Update, toBasvuru.ToJson());

                }
            }

            var messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);
            return Json(new { mMessage.IsSuccess, messageView }, "application/json", JsonRequestBehavior.AllowGet);

        }
    }
}