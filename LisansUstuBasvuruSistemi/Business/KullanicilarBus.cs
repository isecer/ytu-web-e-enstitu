using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil; 
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class KullanicilarBus
    {

        public static StudentControl KullaniciObsOgrenciBilgisiGuncelle(int kullaniciId)
        {
            var kayitBilgi = new StudentControl();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kulls = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                if (kulls.YtuOgrencisi)
                {
                    kayitBilgi = Management.StudentControl(kulls.TcKimlikNo);
                    if (kayitBilgi.KayitVar && kayitBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.toIntObj() == kulls.OgrenimTipKod)
                    {
                        kulls.KayitDonemID = kayitBilgi.DonemID;
                        kulls.KayitYilBaslangic = kayitBilgi.BaslangicYil;
                        kulls.KayitTarihi = kayitBilgi.KayitTarihi;
                        if (kayitBilgi.OgrenciInfo != null)
                        {
                            int? danismanId = null;
                            if (!kayitBilgi.OgrenciInfo.DANISMAN_TC1.IsNullOrWhiteSpace())
                            {
                                var danisman = db.Kullanicilars.FirstOrDefault(p => p.TcKimlikNo == kayitBilgi.OgrenciInfo.DANISMAN_TC1);
                                if (danisman != null)
                                    danismanId = danisman.KullaniciID;
                            }

                            kulls.DanismanID = danismanId;
                        }

                    }
                    else
                    {
                        kulls.YtuOgrencisi = false;
                        kulls.OgrenimTipKod = null;
                        kulls.ProgramKod = null;
                        kulls.OgrenciNo = null;
                        kulls.KayitDonemID = null;
                        kulls.KayitYilBaslangic = null;
                        kulls.KayitTarihi = null;
                    }
                    db.SaveChanges();
                }
                return kayitBilgi;
            }
        }
        public static MmMessage KullaniciKayitKontrol(kmBasvuru kModel)
        {
            var mmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kullanici = db.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                var isYerli = kullanici.KullaniciTipleri.Yerli; 
                #region kullaniciKontrol 
                if (isYerli)
                    if (kModel.TcKimlikNo.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Tc Kimlik No Giriniz");

                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                    }
                    else if (kModel.TcKimlikNo.IsNumber() == false)
                    {
                        mmMessage.Messages.Add("Tc Kimlik No Sadece Sayıdan Oluşmalıdır");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                    }
                    else if (kModel.TcKimlikNo.Length != 11)
                    {
                        mmMessage.Messages.Add("Tc Kimlik No 11 haneli olmalıdır!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TcKimlikNo" });
                if (!isYerli)
                    if (kModel.PasaportNo.IsNullOrWhiteSpace())
                    {
                        string msg = "Pasaport No Giriniz";
                        mmMessage.Messages.Add(msg);

                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "PasaportNo" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "PasaportNo" });
                if (!kModel.CinsiyetID.HasValue)
                {
                    mmMessage.Messages.Add("Cinsiyet Bilgisini Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CinsiyetID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CinsiyetID" });



                if (kModel.AnaAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Ana Adı Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnaAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnaAdi" });

                if (kModel.BabaAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Baba Adı Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BabaAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BabaAdi" });

                if (!kModel.DogumYeriKod.HasValue)
                {
                    mmMessage.Messages.Add("Doğum Yeri Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DogumYeriKod" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DogumYeriKod" });

                if (!kModel.DogumTarihi.HasValue)
                {
                    mmMessage.Messages.Add("Doğum Tarihi Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DogumTarihi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DogumTarihi" });
                if (isYerli)
                    if (!kModel.NufusilIlceKod.HasValue)
                    {
                        mmMessage.Messages.Add("Nüfus İl/İlçe Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "NufusilIlceKod" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "NufusilIlceKod" });

                if (isYerli)
                    if (!kModel.CiltNo.HasValue)
                    {
                        mmMessage.Messages.Add("Cilt No Bilgisi Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CiltNo" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CiltNo" });
                if (isYerli)
                    if (!kModel.AileNo.HasValue)
                    {
                        mmMessage.Messages.Add("Aile No Bilgisi Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AileNo" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AileNo" });
                if (isYerli)
                    if (!kModel.SiraNo.HasValue)
                    {
                        mmMessage.Messages.Add("Sıra No Bilgisi Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SiraNo" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SiraNo" });

                if (!kModel.UyrukKod.HasValue)
                {
                    mmMessage.Messages.Add("Uyruk Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UyrukKod" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UyrukKod" });



                if (isYerli)
                    if (!kModel.SehirKod.HasValue)
                    {
                        mmMessage.Messages.Add("Yaşadığı Şehir Bilgisini Giriniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SehirKod" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SehirKod" });

                if (kModel.CepTel.IsNullOrWhiteSpace() && kModel.EvTel.IsNullOrWhiteSpace() && kModel.IsTel.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Cep, iş ve ev telefonu bilgilerinden en az birinin girilmesi zorunludur!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CepTel" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EvTel" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsTel" });
                }
                else
                {
                    if (kModel.CepTel.IsNullOrWhiteSpace() == false) mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CepTel" });
                    if (kModel.EvTel.IsNullOrWhiteSpace() == false) mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EvTel" });
                    if (kModel.IsTel.IsNullOrWhiteSpace() == false) mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsTel" });
                }

                if (kModel.EMail.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("EMail Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else if (kModel.EMail.ToIsValidEmail())
                {
                    mmMessage.Messages.Add("Lütfen EMail Formatını Doğru Giriniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else
                {
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EMail" });
                }

                if (kModel.Adres.IsNullOrWhiteSpace() && kModel.Adres2.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Adres Bilgisi Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Adres" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Adres2" });
                }
                else
                {
                    if (!kModel.Adres.IsNullOrWhiteSpace()) mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Adres" });
                    if (!kModel.Adres2.IsNullOrWhiteSpace()) mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Adres2" });
                }


                #endregion
            }
            return mmMessage;
        }

        public static List<CmbIntDto> GetCmbKullaniciTipleri(bool bosSecimVar = false, bool isHesapOlusturFiltre = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.YeniHesapOlusturabilir == (isHesapOlusturFiltre || p.YeniHesapOlusturabilir)).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> GetCmbKullaniciTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.KurumIci == false).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> GetCmbKullaniciTipleriOgrenciler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.BasvuruYapabilir).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }
            return dct;

        }

        public static List<CheckObject<Kullanicilar>> GetProgramYetkisiOlanKullanicilar(List<Kullanicilar> kullanicilar, string programKod, string enstituKod = null)
        {
            var data = new List<CheckObject<Kullanicilar>>();

            var qData = kullanicilar.Where(p => p.EnstituKod == (enstituKod ?? p.EnstituKod))
                .Select(s => new CheckObject<Kullanicilar>
                {
                    Checked = s.KullaniciProgramlaris.Any(a => a.ProgramKod == programKod),
                    Value = s
                }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.Ad).ThenBy(t => t.Value.Soyad).ToList();
            return qData;

        }

        public static List<KulaniciProgramYetkiModel> GetKullaniciProgramlari(int kullaniciId, string enstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kull = (from s in db.Programlars
                    join b in db.AnabilimDallaris on s.AnabilimDaliID equals b.AnabilimDaliID
                    join e in db.Enstitulers on b.EnstituKod equals e.EnstituKod
                    select new KulaniciProgramYetkiModel
                    {
                        EnstituKod = e.EnstituKod,
                        EnstituAdi = e.EnstituAd,
                        EnstituKisaAd = e.EnstituKisaAd,
                        AnabilimDaliAdi = b.AnabilimDaliAdi,
                        AnabilimDaliKod = b.AnabilimDaliKod,
                        ProgramKod = s.ProgramKod,
                        ProgramAdi = s.ProgramAdi,
                        YetkiVar = db.KullaniciProgramlaris.Any(a => a.KullaniciID == kullaniciId && a.ProgramKod == s.ProgramKod)
                    });

                if (enstituKod.IsNullOrWhiteSpace() == false) kull = kull.Where(p => p.EnstituKod == enstituKod);
                var data = kull.OrderByDescending(o => o.YetkiVar).ThenBy(t => t.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ToList();
                return data;
            }
        }

        public static Exception YeniHesapMailGonder(Kullanicilar kModel, string sfr)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(kModel.EnstituKod);
                var mRowModel = new List<mailTableRow>();
                var enstitu = db.Enstitulers.First(p => p.EnstituKod == kModel.EnstituKod);


                var erisimAdresi = mailBilgi.SistemErisimAdresi;
                var erisimAdresiSpl = erisimAdresi.Split('/').ToList();
                if (erisimAdresi.Contains("//"))
                    erisimAdresi = erisimAdresiSpl[0] + "//" + erisimAdresiSpl.Skip(2).Take(1).First();
                else
                    erisimAdresi = "http://" + erisimAdresiSpl.First();
                mRowModel.Add(new mailTableRow { Baslik = "Ad Soyad", Aciklama = kModel.Ad + " " + kModel.Soyad });

                if (kModel.BirimID.HasValue)
                {
                    var birim = db.Birimlers.First(p => p.BirimID == kModel.BirimID);
                    mRowModel.Add(new mailTableRow { Baslik = "Birim", Aciklama = birim.BirimAdi });
                }
                if (kModel.UnvanID.HasValue)
                {
                    var unvan = db.Unvanlars.First(p => p.UnvanID == kModel.UnvanID);
                    mRowModel.Add(new mailTableRow { Baslik = "Unvan", Aciklama = unvan.UnvanAdi });
                }
                if (kModel.SicilNo.IsNullOrWhiteSpace() == false) mRowModel.Add(new mailTableRow { Baslik = "Sicil No", Aciklama = kModel.SicilNo });
                if (kModel.TcKimlikNo.IsNullOrWhiteSpace() == false) mRowModel.Add(new mailTableRow { Baslik = "Tc kimlik No", Aciklama = kModel.TcKimlikNo });
                if (kModel.PasaportNo.IsNullOrWhiteSpace() == false) mRowModel.Add(new mailTableRow { Baslik = "Pasaport No", Aciklama = kModel.PasaportNo });
                if (kModel.CepTel.IsNullOrWhiteSpace() == false) mRowModel.Add(new mailTableRow { Baslik = "Cep Tel", Aciklama = kModel.CepTel });

                mRowModel.Add(new mailTableRow { Baslik = "Kullanıcı Adı", Aciklama = kModel.KullaniciAdi });
                mRowModel.Add(new mailTableRow { Baslik = "Şifre", Aciklama = kModel.IsActiveDirectoryUser ? "Email şifreniz ile aynı" : sfr });
                mRowModel.Add(new mailTableRow { Baslik = "Sistem Erişim Adresi", Aciklama = "<a href='" + mailBilgi.SistemErisimAdresi + "' target='_blank'>" + mailBilgi.SistemErisimAdresi + "</a>" });
                var mmmC = new mdlMailMainContent();

                mmmC.EnstituAdi = enstitu.EnstituAd;
                var mtc = new mailTableContent();
                mtc.AciklamaBasligi = "Kullanıcı hesabınız oluşturuldu. Sisteme Giriş Bilgisi Aşağıdaki Gibidir.";
                mtc.Detaylar = mRowModel;
                var tavleContent = Management.RenderPartialView("Ajax", "getMailTableContent", mtc);
                mmmC.Content = tavleContent;
                mmmC.LogoPath = erisimAdresi + "/Content/assets/images/ytu_logo_tr.png";
                mmmC.UniversiteAdi = "Yıldız Tekni Üniversitesi";
                var htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                var user = mailBilgi.SmtpKullaniciAdi;
                var snded = MailManager.sendMailRetVal(kModel.EnstituKod, user, htmlMail, kModel.EMail, null);
                return snded;

            }

        }
    }
}