using System.IO;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    public class FileController : Controller
    {

        public ActionResult Index(string filePath)
        {
            var basePath = FileHelper.FileServerBasePath;
            filePath = "/DosyaArsivi/" + filePath;
            var fullPath = basePath + filePath.Replace("/", "\\");
       
            if (!System.IO.File.Exists(fullPath))
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
            return new FileStreamResult(fileStream, contentType);
        }

        private bool HasAccessToFolder(bool isAuthenticated, string folderName)
        {

            if (FileHelper.MesajDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.MailDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.TaleplerDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.ToSavunmaDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.TiAraRaporDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.MezuniyetYayinDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.MezuniyetTezSablonDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.DonemProjesiIntihalDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if (FileHelper.MailSablonDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;
            if(FileHelper.DuyuruDosyaYolu.ToLower() == folderName.ToLower()) return isAuthenticated;

            return true;
        }
    }

}