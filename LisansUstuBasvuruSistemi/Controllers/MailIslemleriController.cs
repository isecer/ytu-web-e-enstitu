using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using HtmlAgilityPack;
using System.Text;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MailIslemleri)]
    public class MailIslemleriController : Controller
    {
        private readonly LubsDbEntities _entities;

        public MailIslemleriController()
        {
            _entities = new LubsDbEntities();
        }

        public ActionResult Index()
        {
            FmMailGondermeDto model = new FmMailGondermeDto();
            model.PageSize = 15;
            return Index(model);
        }


        [HttpPost]
        public ActionResult Index(FmMailGondermeDto model)
        {
            // Stored procedure kullanarak arama yapma
            ObjectParameter totalCount = new ObjectParameter("TotalCount", typeof(int));

            List<sp_SearchMailsFullText_Result> results = _entities.sp_SearchMailsFullText(
                string.Join(",", UserIdentity.Current.EnstituKods),
                model.IsEkVar,
                !string.IsNullOrWhiteSpace(model.Konu) ? model.Konu : null,
                !string.IsNullOrWhiteSpace(model.EnstituKod) ? model.EnstituKod : null,
                model.Tarih,
                !string.IsNullOrWhiteSpace(model.MailGonderen) ? model.MailGonderen : null,
                model.StartRowIndex,
                model.PageSize,
                totalCount).ToList();

            model.RowCount = (int)totalCount.Value;
            model.MailGondermeDtos = results.Select(r => new FrMailGondermeDto
            {
                GonderilenMailID = r.GonderilenMailID.Value,
                Tarih = r.Tarih.Value,
                EnstituAdi = r.EnstituAd,
                Konu = r.Konu,
                MailGonderen = r.MailGonderen,
                Gonderildi = r.Gonderildi.Value,
                HataMesaji = r.HataMesaji
            }).ToList();

            // Ek sayıları ve alıcı sayılarını hesaplama
            var gonderilenMailIds = model.MailGondermeDtos.Select(s => s.GonderilenMailID).ToList();

            // Mail eklerini hesaplama
            var ekSayilari = _entities.GonderilenMailEkleris
                .Where(e => gonderilenMailIds.Contains(e.GonderilenMailID))
                .GroupBy(e => e.GonderilenMailID)
                .Select(g => new { GonderilenMailID = g.Key, EkSayisi = g.Count() })
                .ToDictionary(x => x.GonderilenMailID, x => x.EkSayisi);

            // Mail alıcılarını hesaplama
            var aliciSayilari = _entities.GonderilenMailKullanicilars
                .Where(a => gonderilenMailIds.Contains(a.GonderilenMailID))
                .GroupBy(a => a.GonderilenMailID)
                .Select(g => new { GonderilenMailID = g.Key, AliciSayisi = g.Count() })
                .ToDictionary(x => x.GonderilenMailID, x => x.AliciSayisi);

            // Her mail için ek ve alıcı sayılarını atama
            foreach (var mail in model.MailGondermeDtos)
            {
                mail.EkSayisi = ekSayilari.ContainsKey(mail.GonderilenMailID) ? ekSayilari[mail.GonderilenMailID] : 0;
                mail.KisiSayisi = aliciSayilari.ContainsKey(mail.GonderilenMailID) ? aliciSayilari[mail.GonderilenMailID] : 0;
            }

            ViewBag.IsEkVar = new SelectList(GonderilenMaillerBus.GetCmbMailEkKontrol(true), "Value", "Caption", model.IsEkVar);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);

            return View(model);
        }

        public ActionResult MailDetay(int gonderilenMailId)
        {
            // Mail detaylarını getirme
            var mailDetay = (from mail in _entities.GonderilenMaillers
                             join enst in _entities.Enstitulers on mail.EnstituKod equals enst.EnstituKod into enstGroup
                             from enst in enstGroup.DefaultIfEmpty()
                             join k in _entities.Kullanicilars on mail.IslemYapanID equals k.KullaniciID
                             where mail.GonderilenMailID == gonderilenMailId
                             select new FrMailGondermeDto
                             {
                                 GonderilenMailID = mail.GonderilenMailID,
                                 EnstituAdi = enst != null ? enst.EnstituAd : "Sistem",
                                 Tarih = mail.Tarih,
                                 Konu = mail.Konu,
                                 Aciklama = mail.Aciklama,
                                 AciklamaHtml = mail.AciklamaHtml,
                                 MailGonderen = k.Ad + " " + k.Soyad,
                                 UserKey = k.UserKey,
                                 IslemYapanID = mail.IslemYapanID,
                                 IslemYapanIP = mail.IslemYapanIP,
                                 EkSayisi = mail.GonderilenMailEkleris.Count,
                                 KisiSayisi = mail.GonderilenMailKullanicilars.Count,
                                 GonderilenMailEkleris = mail.GonderilenMailEkleris.ToList()
                             }).First();

            // Mail alıcılarını getirme
            var alicilar = _entities.GonderilenMailKullanicilars
                .Where(s => s.GonderilenMailID == gonderilenMailId)
                .OrderBy(s => s.Kullanicilar.Ad)
                .ThenBy(s => s.Kullanicilar.Soyad)
                .Select(s => new MailKullaniciBilgi
                {
                    AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                    Email = s.Email
                })
                .ToList();

            ViewBag.DataK = alicilar;
            return View(mailDetay);
        }

        public ActionResult MailIstatitik()
        {
            FmMailIstatistikDto model = new FmMailIstatistikDto();
            model.AyId = DateTime.Now.Month;
            model.Yil = DateTime.Now.Year;
            return MailIstatitik(model);
        }

        [HttpPost]
        public ActionResult MailIstatitik(FmMailIstatistikDto model)
        {
            // Mail istatistiklerini getirme
            var query = _entities.GonderilenMaillers
                .Where(p => p.Tarih.Year == model.Yil &&
                           (!model.AyId.HasValue || p.Tarih.Month == model.AyId.Value));

            // İstatistikleri hesaplama
            var stats = query
                .Select(s => new { s.EnstituKod, s.Tarih })
                .ToList()
                .GroupBy(g => new
                {
                    Year = g.Tarih.Year,
                    Month = g.Tarih.Month,
                    Day = g.Tarih.Day
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    FbeCount = g.Count(p => p.EnstituKod == "010"),
                    SbeCount = g.Count(p => p.EnstituKod == "020"),
                    TetCount = g.Count(p => p.EnstituKod == "030"),
                    ToplamCount = g.Count()
                })
                .AsQueryable();

            model.RowCount = stats.Count();
            model.ToplamCount = stats.Sum(s => s.ToplamCount);
            model.FbeCount = stats.Sum(s => s.FbeCount);
            model.SbeCount = stats.Sum(s => s.SbeCount);
            model.TetCount = stats.Sum(s => s.TetCount);

            // Sıralama
            var orderedStats = string.IsNullOrWhiteSpace(model.Sort)
                ? stats.OrderByDescending(o => o.Year)
                      .ThenByDescending(t => t.Month)
                      .ThenByDescending(o => o.Day)
                : stats.OrderBy(model.Sort);

            // İstatistik verilerini DTO'ya dönüştürme
            model.Data = orderedStats
                .Skip(model.StartRowIndex)
                .Take(model.PageSize)
                .ToList()
                .Select(s => new FrIstatistikDto
                {
                    Tarih = new DateTime(s.Year, s.Month, s.Day),
                    FbeCount = s.FbeCount,
                    SbeCount = s.SbeCount,
                    TetCount = s.TetCount,
                    ToplamCount = s.ToplamCount
                })
                .ToList();

            // Combo box verilerini getirme
            var cmbAylar = SrTalepleriBus.GetCmbAylar(true);
            var gonderilenMailYil = ComboData.GetCmbGonderilenMailYil();

            ViewBag.AyId = new SelectList(cmbAylar, "Value", "Caption", model.AyId);
            ViewBag.Yil = new SelectList(gonderilenMailYil, "Value", "Caption", model.Yil);

            return View(model);
        }

        public ActionResult Sil(int id)
        {
            // Maili silme (soft delete)
            var gonderilenMail = _entities.GonderilenMaillers.FirstOrDefault(p => p.GonderilenMailID == id);
            string mesaj = "";
            bool basarili = true;

            if (gonderilenMail != null)
            {
                try
                {
                    if (string.IsNullOrEmpty(mesaj))
                    {
                        gonderilenMail.Silindi = true;
                        _entities.SaveChanges();
                        mesaj = string.Concat("'", gonderilenMail.Konu, "' konulu email Silindi!");
                    }
                }
                catch (Exception ex)
                {
                    basarili = false;
                    mesaj = string.Concat("'", gonderilenMail.Konu, "' Konulu Mail Silinemedi! <br/> Bilgi:", ex.ToExceptionMessage());
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(mesaj, ex.ToExceptionStackTrace(), (byte)4);
                }
            }
            else
            {
                basarili = false;
                mesaj = "Silmek istediğiniz mail bilgisi sistemde bulunamadı!";
            }

            return Json(new { basarili, mesaj }, "application/json", JsonRequestBehavior.AllowGet);
        }








        private static Dictionary<string, ResendMailsProcess> ActiveResendProcesses = new Dictionary<string, ResendMailsProcess>();

        // Mail gönderim işlemini yönetmek için sınıf
        private class ResendMailsProcess
        {
            public string Id { get; set; }
            public DateTime StartTime { get; set; }
            public bool IsRunning { get; set; }
            public bool IsComplete { get; set; }
            public bool IsCancelled { get; set; }
            public int TotalMailCount { get; set; }
            public int ProcessedMailCount { get; set; }
            public int SuccessCount { get; set; }
            public int ErrorCount { get; set; }
            public string CurrentStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public CancellationTokenSource CancellationToken { get; set; }
            public Dictionary<string, string> ErrorDetails { get; set; }

            public ResendMailsProcess()
            {
                Id = Guid.NewGuid().ToString();
                StartTime = DateTime.Now;
                IsRunning = false;
                IsComplete = false;
                IsCancelled = false;
                TotalMailCount = 0;
                ProcessedMailCount = 0;
                SuccessCount = 0;
                ErrorCount = 0;
                CurrentStatus = "İşlem başlatılıyor...";
                ErrorMessages = new List<string>();
                CancellationToken = new CancellationTokenSource();
                ErrorDetails = new Dictionary<string, string>();
            }

            public int PercentComplete
            {
                get
                {
                    if (TotalMailCount == 0) return 0;
                    return (int)Math.Min(100, Math.Round((double)ProcessedMailCount / TotalMailCount * 100));
                }
            }
        }

        [HttpPost]
        public ActionResult PreviewResendMails(FormCollection form)
        {
            try
            {
                DateTime startDate = DateTime.ParseExact(form["startDate"], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                DateTime endDate = DateTime.ParseExact(form["endDate"], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                List<string> institutes = form.GetValues("institutes")?.ToList() ?? new List<string>();
                int batchSize = int.Parse(form["batchSize"]);

                if (startDate > endDate)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Başlangıç tarihi bitiş tarihinden sonra olamaz."
                    });
                }

                if (institutes.Count == 0)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "En az bir enstitü seçmelisiniz."
                    });
                }

                var mailQuery = _entities.GonderilenMaillers
                    .Where(m => m.Tarih >= startDate && m.Tarih <= endDate &&
                           institutes.Contains(m.EnstituKod) && m.Silindi == false);

                int totalMailCount = mailQuery.Count();

                int totalRecipientCount = _entities.GonderilenMailKullanicilars
                    .Count(r => mailQuery.Select(m => m.GonderilenMailID).Contains(r.GonderilenMailID));

                if (totalMailCount == 0)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Seçilen kriterlere uygun mail bulunamadı."
                    });
                }

                int batchCount = (int)Math.Ceiling((double)totalMailCount / batchSize);

                var instituteNames = _entities.Enstitulers
                    .Where(e => institutes.Contains(e.EnstituKod))
                    .OrderBy(e => e.EnstituKod)
                    .Select(e => e.EnstituAd)
                    .ToList();

                return Json(new
                {
                    Success = true,
                    StartDate = startDate.ToString("dd.MM.yyyy HH:mm"),
                    EndDate = endDate.ToString("dd.MM.yyyy HH:mm"),
                    InstituteNames = instituteNames,
                    TotalMailCount = totalMailCount,
                    TotalRecipientCount = totalRecipientCount,
                    BatchCount = batchCount,
                    BatchSize = batchSize
                });
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(
                    "Mail önizleme işleminde hata: " + ex.Message,
                    ex.StackTrace,
                    BilgiTipiEnum.Hata,
                    UserIdentity.Current.Id,
                    UserIdentity.Ip
                );

                return Json(new
                {
                    Success = false,
                    Message = "İşlem sırasında bir hata oluştu: " + ex.Message
                });
            }
        }
        [HttpPost]
        public ActionResult StartResendMails(FormCollection form)
        {
            try
            {
                DateTime startDate = DateTime.ParseExact(form["startDate"], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                DateTime endDate = DateTime.ParseExact(form["endDate"], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                List<string> institutes = form.GetValues("institutes")?.ToList() ?? new List<string>();
                string additionalNote = form["additionalNote"];
                int batchSize = int.Parse(form["batchSize"]);
                int batchDelay = int.Parse(form["batchDelay"]);
                int subjectPrefix = form["subjectPrefix"].ToInt(1);
                int currentUserId = UserIdentity.Current.Id;
                string currentUserIp = UserIdentity.Ip;

                var process = new ResendMailsProcess();

                ActiveResendProcesses[process.Id] = process;

                Task.Factory.StartNew(() =>
                {
                    ExecuteResendProcess(process, startDate, endDate, institutes, additionalNote, batchSize, batchDelay, subjectPrefix, currentUserId, currentUserIp);
                }, process.CancellationToken.Token);

                return Json(new
                {
                    Success = true,
                    ProcessId = process.Id
                });
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(
                    "Mail gönderme işlemi başlatılırken hata: " + ex.Message,
                    ex.StackTrace,
                    BilgiTipiEnum.Hata,
                    UserIdentity.Current.Id,
                    UserIdentity.Ip
                );

                return Json(new
                {
                    Success = false,
                    Message = "İşlem başlatılırken bir hata oluştu: " + ex.Message
                });
            }
        }

        private void ExecuteResendProcess(ResendMailsProcess process, DateTime startDate, DateTime endDate,
                                         List<string> institutes, string additionalNote, int batchSize, int batchDelay, int subjectPrefix,
                                         int currentUserId, string currentUserIp)
        {
            try
            {
                process.IsRunning = true;
                process.CurrentStatus = "Mail bilgileri alınıyor...";

                // Bu thread için yeni bir veritabanı bağlantısı oluştur
                using (var entities = new LubsDbEntities())
                {
                    // Gönderilecek mailleri getir
                    var mailsToResend = entities.GonderilenMaillers
                        .Where(m => m.Tarih >= startDate && m.Tarih <= endDate &&
                              institutes.Contains(m.EnstituKod) && m.Silindi == false)
                        .OrderBy(m => m.Tarih)
                        .ToList();

                    process.TotalMailCount = mailsToResend.Count;

                    if (process.TotalMailCount == 0)
                    {
                        process.CurrentStatus = "Seçilen kriterlere uygun mail bulunamadı.";
                        process.IsComplete = true;
                        process.IsRunning = false;
                        return;
                    }

                    // Mailleri gruplara ayır
                    process.CurrentStatus = "Mailler gruplandırılıyor...";

                    var batches = new List<List<GonderilenMailler>>();
                    for (int i = 0; i < mailsToResend.Count; i += batchSize)
                    {
                        batches.Add(mailsToResend.Skip(i).Take(batchSize).ToList());
                    }

                    process.CurrentStatus = $"Toplam {process.TotalMailCount} mail, {batches.Count} grup halinde işleme alınıyor...";

                    // Her grubu işle
                    int batchNumber = 1;
                    foreach (var batch in batches)
                    {
                        if (process.IsCancelled)
                        {
                            process.CurrentStatus = "İşlem kullanıcı tarafından iptal edildi.";
                            break;
                        }

                        process.CurrentStatus = $"Grup {batchNumber}/{batches.Count} işleniyor ({batch.Count} mail)...";

                        // Gruptaki her maili işle
                        foreach (var mail in batch)
                        {
                            if (process.IsCancelled)
                            {
                                break;
                            }

                            try
                            {
                                // Mail detaylarını al
                                var mailDetails = entities.GonderilenMaillers
                                    .Include("GonderilenMailEkleris")
                                    .Include("GonderilenMailKullanicilars")
                                    .FirstOrDefault(m => m.GonderilenMailID == mail.GonderilenMailID);

                                if (mailDetails == null)
                                {
                                    // Mail bulunamazsa atla
                                    process.ErrorCount++;
                                    process.ErrorMessages.Add($"Mail bulunamadı (ID: {mail.GonderilenMailID})");
                                    process.ErrorDetails[$"Mail_{mail.GonderilenMailID}"] = "Mail veritabanında bulunamadı";
                                    continue;
                                }

                                string mailContent = mailDetails.AciklamaHtml;

                                if (!string.IsNullOrWhiteSpace(additionalNote))
                                {
                                    mailContent = AppendAdditionalNote(mailContent, additionalNote);
                                }

                                var recipients = entities.GonderilenMailKullanicilars
                                    .Where(r => r.GonderilenMailID == mail.GonderilenMailID)
                                    .ToList();

                                if (recipients.Count == 0)
                                {
                                    process.ErrorCount++;
                                    process.ErrorMessages.Add($"Alıcı bulunamadı (Mail ID: {mail.GonderilenMailID})");
                                    process.ErrorDetails[$"Mail_{mail.GonderilenMailID}"] = "Mail için hiç alıcı bulunamadı";
                                    continue;
                                }

                                // Mail alıcı listesi oluştur
                                var mailSendList = recipients.Select(r => new MailSendList
                                {
                                    KullaniciId = r.KullaniciID,
                                    EMail = r.Email,
                                    ToOrBcc = true
                                }).ToList();

                                // Mail eklerini getir
                                var attachments = new List<System.Net.Mail.Attachment>();
                                foreach (var attachment in mailDetails.GonderilenMailEkleris.Where(p => p.EkDosyaYolu != null))
                                {
                                    try
                                    {
                                        var fileAttachmentInfo = new FileAttachmentInfo
                                        {
                                            FilePath = attachment.EkDosyaYolu,
                                            FileName = attachment.EkAdi
                                        };

                                        var mailAttachment = fileAttachmentInfo.GetFileToAttachment();
                                        if (mailAttachment != null)
                                        {
                                            attachments.Add(mailAttachment);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Ek dosya hatasını logla ama devam et
                                        process.ErrorMessages.Add($"Ek dosya hatası (Mail ID: {mail.GonderilenMailID}, Dosya: {attachment.EkAdi}): {ex.Message}");
                                        process.ErrorDetails[$"Attachment_{mail.GonderilenMailID}_{attachment.EkAdi}"] = $"Ek dosya hatası: {ex.Message}";
                                    }
                                }

                                // Konu başlığını ayarla
                                string subject = mail.Konu;
                                switch (subjectPrefix)
                                {
                                    case 1:
                                        subject = "[YENİDEN] " + subject;
                                        break;
                                    case 2:
                                        subject = "[HATIRLATMA] " + subject;
                                        break;
                                    case 3:
                                        subject = "[DÜZELTME] " + subject;
                                        break;
                                    default:
                                        // Orijinal konu kullan
                                        break;
                                }
                                MailManager.SendMail(mail.GonderilenMailID, subject, mailContent, mailSendList, attachments);

                                process.SuccessCount++;
                            }
                            catch (Exception ex)
                            {
                                process.ErrorCount++;
                                process.ErrorMessages.Add($"Mail gönderme hatası (Mail ID: {mail.GonderilenMailID}): {ex.Message}");
                                process.ErrorDetails[$"Error_{mail.GonderilenMailID}"] = ex.ToString();

                                // Kritik hataları logla - parametre ile gelen kullanıcı bilgilerini kullan
                                SistemBilgilendirmeBus.SistemBilgisiKaydet(
                                    $"Mail yeniden gönderme hatası (Mail ID: {mail.GonderilenMailID}): {ex.Message}",
                                    ex.StackTrace,
                                    BilgiTipiEnum.Hata,
                                    currentUserId,
                                    currentUserIp
                                );
                            }

                            process.ProcessedMailCount++;
                            process.CurrentStatus = $"Grup {batchNumber}/{batches.Count}: {process.ProcessedMailCount}/{process.TotalMailCount} mail işlendi.";
                        }

                        // Gruplar arası bekle
                        if (!process.IsCancelled && batchNumber < batches.Count)
                        {
                            process.CurrentStatus = $"Grup {batchNumber} tamamlandı. Sonraki grup için {batchDelay} saniye bekleniyor...";
                            Thread.Sleep(batchDelay * 1000);
                        }

                        batchNumber++;
                    }

                    //İşlemi tamamla
                    process.IsComplete = true;
                    process.IsRunning = false;

                    if (process.IsCancelled)
                    {
                        process.CurrentStatus = $"İşlem iptal edildi. {process.ProcessedMailCount} mail işlendi, {process.SuccessCount} başarılı, {process.ErrorCount} hatalı.";
                    }
                    else if (process.ErrorCount > 0)
                    {
                        process.CurrentStatus = $"İşlem tamamlandı. {process.ProcessedMailCount} mail işlendi, {process.SuccessCount} başarılı, {process.ErrorCount} hatalı.";
                    }
                    else
                    {
                        process.CurrentStatus = $"İşlem başarıyla tamamlandı. {process.ProcessedMailCount} mail başarıyla gönderildi.";
                    }
                }
            }
            catch (Exception ex)
            {
                process.IsComplete = true;
                process.IsRunning = false;
                process.ErrorCount++;
                process.ErrorMessages.Add($"Genel hata: {ex.Message}");
                process.ErrorDetails["General_Error"] = ex.ToString();
                process.CurrentStatus = $"İşlem sırasında hata oluştu: {ex.Message}";

                // Kritik hataları logla - parametre ile gelen kullanıcı bilgilerini kullan
                SistemBilgilendirmeBus.SistemBilgisiKaydet(
                    "Mail toplu gönderme işleminde kritik hata: " + ex.Message,
                    ex.StackTrace,
                    BilgiTipiEnum.Hata,
                    currentUserId,
                    currentUserIp
                );
            }
        }

        [HttpGet]
        public ActionResult GetResendProgress(string processId)
        {
            if (string.IsNullOrEmpty(processId) || !ActiveResendProcesses.ContainsKey(processId))
            {
                return Json(new
                {
                    Success = false,
                    Message = "Belirtilen işlem bulunamadı."
                }, JsonRequestBehavior.AllowGet);
            }

            var process = ActiveResendProcesses[processId];

            var response = new
            {
                Success = true,
                PercentComplete = process.PercentComplete,
                StatusMessage = process.CurrentStatus,
                IsComplete = process.IsComplete,
                HasErrors = process.ErrorCount > 0,
                TotalMailCount = process.TotalMailCount,
                ProcessedMailCount = process.ProcessedMailCount,
                SuccessCount = process.SuccessCount,
                ErrorCount = process.ErrorCount
            };

            CleanupOldProcesses();

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CancelResendMails()
        {
            try
            {
                var process = ActiveResendProcesses
                    .Where(p => p.Value.IsRunning && !p.Value.IsComplete)
                    .OrderByDescending(p => p.Value.StartTime)
                    .FirstOrDefault();

                if (process.Value == null)
                {
                    return Json(new { Success = false, Message = "Aktif işlem bulunamadı." });
                }

                process.Value.IsCancelled = true;
                process.Value.CancellationToken.Cancel();
                process.Value.CurrentStatus = "İşlem iptal ediliyor...";

                return Json(new { Success = true });
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(
                    "Mail gönderme işlemi iptal edilirken hata: " + ex.Message,
                    ex.StackTrace,
                    BilgiTipiEnum.Hata,
                    UserIdentity.Current.Id,
                    UserIdentity.Ip
                );

                return Json(new { Success = false, Message = "İşlem iptal edilirken bir hata oluştu: " + ex.Message });
            }
        }


        private void CleanupOldProcesses()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-30);

            var oldProcesses = ActiveResendProcesses
                .Where(p => p.Value.IsComplete && p.Value.StartTime < cutoffTime)
                .Select(p => p.Key)
                .ToList();

            foreach (var key in oldProcesses)
            {
                ActiveResendProcesses.Remove(key);
            }
        }

        private string AppendAdditionalNote(string htmlContent, string additionalNote)
        {
            if (string.IsNullOrWhiteSpace(additionalNote))
            {
                return htmlContent;
            }

            try
            {
                // HTML içeriğini düzgün şekilde değiştirmek için HtmlAgilityPack kullan
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // Mail içerik konteynırını bul - yaygın içerik konteynırlarını ara
                var contentNode = doc.DocumentNode.SelectSingleNode("//td[@id='mSendContent']") ??
                                 doc.DocumentNode.SelectSingleNode("//div[@class='content']") ??
                                 doc.DocumentNode.SelectSingleNode("//body");

                if (contentNode != null)
                {
                    // Not için HTML oluştur - içeriğin en üstünde kutulu bir bildirim
                    var noteHtml = $@"<div style='margin-bottom: 20px; padding: 10px; border: 1px solid #f8d7da; background-color: #f8d7da; border-radius: 4px; color: #721c24;'>
                    <strong>Not:</strong> {additionalNote}
                </div>";

                    // İçeriğin başına ekle
                    var noteNode = HtmlNode.CreateNode(noteHtml);
                    contentNode.PrependChild(noteNode);

                    return doc.DocumentNode.OuterHtml;
                }
                else
                {
                    // Uygun bir konteyner bulamazsak, notu tüm içeriğin başına ekle
                    return $@"<div style='margin-bottom: 20px; padding: 10px; border: 1px solid #f8d7da; background-color: #f8d7da; border-radius: 4px; color: #721c24;'>
                    <strong>Not:</strong> {additionalNote}
                </div>{htmlContent}";
                }
            }
            catch (Exception ex)
            {
                // HTML ayrıştırma başarısız olursa - notu basitçe ekle
                SistemBilgilendirmeBus.SistemBilgisiKaydet(
                    "Mail ek açıklama eklenirken hata: " + ex.Message,
                    ex.StackTrace,
                    BilgiTipiEnum.Uyarı,
                    UserIdentity.Current.Id,
                    UserIdentity.Ip
                );

                return $@"<div style='margin-bottom: 20px; padding: 10px; border: 1px solid #f8d7da; background-color: #f8d7da; border-radius: 4px; color: #721c24;'>
                <strong>Not:</strong> {additionalNote}
            </div>{htmlContent}";
            }
        }
    }
}