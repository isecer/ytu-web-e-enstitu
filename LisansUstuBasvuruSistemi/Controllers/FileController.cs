using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    public class FileController : Controller
    {

        //public ActionResult Index(string filePath)
        //{
        //    var basePath = FileHelper.FileServerBasePath;
        //    var fullPath = basePath + filePath.Replace("/", "\\");

        //    if (fullPath.IsNullOrWhiteSpace() || !System.IO.File.Exists(fullPath))
        //    {
        //        return HttpNotFound("Dosya bulunamadı.");
        //    }

        //    var folderPath = filePath.Substring(0, filePath.LastIndexOf('/'));

        //    // Kullanıcı yetki kontrolü
        //    var isAuthenticated = UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated;
        //    if (!HasAccessToFolder(isAuthenticated, folderPath))
        //    {
        //        return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "Yetkisiz erişim.");
        //    }

        //    var contentType = MimeMapping.GetMimeMapping(fullPath);
        //    var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

        //    // Dosya adını al
        //    var fileName = Path.GetFileName(fullPath); 
        //    // URL kodlaması yapılmış dosya adı
        //    var encodedFileName = Uri.EscapeDataString(fileName);

        //    bool showBrowserInline = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase);

        //    // Content-Disposition header'ını ayarla
        //    string contentDisposition = showBrowserInline ? "inline" : "attachment";
        //    Response.AppendHeader("Content-Disposition", $"{contentDisposition}; filename=\"{fileName}\"; filename*=UTF-8''{encodedFileName}");

        //    // Content-Type header'ına filename parametresi ekle
        //    Response.ContentType = $"{contentType}; name=\"{fileName}\"";

        //    // X-Filename header'ı ekle
        //    Response.AppendHeader("X-Filename", fileName);

        //    // FileStreamResult döndür
        //    return File(fileStream, contentType);
        //}
        public ActionResult Index(string filePath)
        {
            return RedirectToAction("ShowFile", new { filePath });
        }
        public ActionResult ShowFile(string filePath)
        {
            var basePath = FileHelper.FileServerBasePath;
            var fullPath = basePath + filePath.Replace("/", "\\");

            if (fullPath.IsNullOrWhiteSpace() || !System.IO.File.Exists(fullPath))
            {
                return HttpNotFound("Dosya bulunamadı.");
            }

            var folderPath = filePath.Substring(0, filePath.LastIndexOf('/'));

            // Kullanıcı yetki kontrolü
            var isAuthenticated = UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated;
            if (!HasAccessToFolder(isAuthenticated, folderPath))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "Yetkisiz erişim.");
            }

            var contentType = MimeMapping.GetMimeMapping(fullPath);
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

            // Dosya adını al
            var fileName = Path.GetFileName(fullPath);
            // URL kodlaması yapılmış dosya adı
            var encodedFileName = Uri.EscapeDataString(fileName);

            bool showBrowserInline = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase);

            // Content-Disposition header'ını ayarla
            string contentDisposition = showBrowserInline ? "inline" : "attachment";
            Response.AppendHeader("Content-Disposition", $"{contentDisposition}; filename=\"{fileName}\"; filename*=UTF-8''{encodedFileName}");

            // Content-Type header'ına filename parametresi ekle
            Response.ContentType = $"{contentType}; name=\"{fileName}\"";

            // X-Filename header'ı ekle
            Response.AppendHeader("X-Filename", fileName);
            ViewBag.Title = "Hasam";
            // FileStreamResult döndür
            return File(fileStream, contentType);
        }
        private bool HasAccessToFolder(bool isAuthenticated, string folderName)
        {

            if (FileHelper.MesajDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.MailDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.LisansustuBasvuruDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            //if (FileHelper.TaleplerDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            //if (FileHelper.ToSavunmaDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            //if (FileHelper.TiAraRaporDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            //if (FileHelper.MezuniyetYayinDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            //if (FileHelper.MezuniyetTezSablonDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            //if (FileHelper.DonemProjesiIntihalDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            //if (FileHelper.MailSablonDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            //if (FileHelper.DuyuruDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;

            return true;
        }
    }

}