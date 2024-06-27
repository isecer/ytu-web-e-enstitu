using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class FileHelper
    {

        public static bool IsSaveFileServer = SistemAyar.DosyalarSecilenKonumaArsivlensin.GetAyar().ToBoolean(false); //true;
        public static string FileServerUrl = SistemAyar.DosyaArsiviSunucusuErisimAdresi.GetAyar();//"http://194.27.98.10:81";
        public static string FileServerBasePath = SistemAyar.DosyaArsiviFizikselKayitYolu.GetAyar();//"D:\\DocumentServer\\lisansustufiles.yildiz.edu.tr";

        private const string MesajDosyaYolu = "/DosyaArsivi/MesajEkleri";
        private const string MailDosyaYolu = "/DosyaArsivi/MailEkleri";
        private const string TaleplerDosyaYolu = "/DosyaArsivi/TalepDosyalari";
        private const string ToSavunmaDosyaYolu = "/DosyaArsivi/ToSavunmaDosyalari";
        private const string TiAraRaporDosyaYolu = "/DosyaArsivi/TiAraRaporDosyalari";
        private const string MezuniyetYayinDosyaYolu = "/DosyaArsivi/MezuniyetBasvurulari/YayinDosyalari";
        private const string MezuniyetTezSablonDosyaYolu = "/DosyaArsivi/MezuniyetBasvurulari/TezSablonDosyalari";
        private const string DonemProjesiIntihalDosyaYolu = "/DosyaArsivi/DonemProjesi";
        private const string MailSablonDosyaYolu = "/DosyaArsivi/MailSablonEkleri";
        private const string DuyuruDosyaYolu = "/DosyaArsivi/DuyuruEkleri";


        public static string CustomUrlContentMail(this string path, string sistemErisimAdresi)
        {
            return IsSaveFileServer ? FileServerUrl + path : sistemErisimAdresi.Replace("/fbe", "").Replace("/sbe", "").Replace("/tet", "") + path;
        }
        public static string CustomUrlContent(this UrlHelper urlHelper, string contentPath)
        {
            return IsSaveFileServer ? FileServerUrl + contentPath : urlHelper.Content(contentPath);
        }
        public static string FileBaseFullPath(this string contentPath)
        {
            if (contentPath.Contains("/")) contentPath = contentPath.Replace("/", "\\");
            return IsSaveFileServer ? FileServerBasePath + contentPath : HttpContext.Current.Server.MapPath("~" + contentPath);
        }
        private static void Save(HttpPostedFileBase file, string saveFilePath)
        {
            if (IsSaveFileServer) SaveFileBasePath(file, saveFilePath);
            else SaveFile(file, saveFilePath);
        }

        private static void SaveFile(HttpPostedFileBase file, string saveFilePath)
        {
            var mappedPath = HttpContext.Current.Server.MapPath("~" + saveFilePath);

            // Dosyanın bulunduğu klasör yolu
            string folderPath = Path.GetDirectoryName(mappedPath);

            // Klasör yoksa oluştur
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            file.SaveAs(mappedPath);

        }
        private static void SaveFileBasePath(HttpPostedFileBase file, string saveFilePath)
        {


            // Dosya yolu belirtilen klasör yoluna göre oluşturulur
            string dosyaYolu = saveFilePath.Replace("/", "\\");
            string combinedPath = FileServerBasePath + dosyaYolu;

            // Dosyanın bulunduğu klasör yolu
            string folderPath = Path.GetDirectoryName(combinedPath);

            // Klasör yoksa oluştur
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            file.SaveAs(combinedPath);
        }
        public static string SaveMesajDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MesajDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveMailDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MailDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveTalepDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = TaleplerDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveToSavunmaDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = ToSavunmaDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveTiAraRaporDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = TiAraRaporDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveMezuniyetYayinDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MezuniyetYayinDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveMezuniyetTezSablonDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MezuniyetTezSablonDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveDonemProjesiIntihalDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = DonemProjesiIntihalDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveMailSablonDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MailSablonDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveDuyuruDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = DuyuruDosyaYolu + "/" + dosyaAdi;
            Save(file, dosyaYolu);
            return dosyaYolu;
        }

        private static void DeleteFile(string filePath)
        {
            var fullPath = HttpContext.Current.Server.MapPath("~" + filePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        private static void DeleteFileBasePath(string filePath)
        {
            // Dosya yolu belirtilen klasör yoluna göre oluşturulur
            string dosyaYolu = filePath.Replace("/", "\\");
            string combinedPath = FileServerBasePath + dosyaYolu;


            // Dosya varsa sil
            if (File.Exists(combinedPath))
            {
                File.Delete(combinedPath);
            }
        }
        public static void Delete(string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return;
            if (IsSaveFileServer) DeleteFileBasePath(filePath);
            else DeleteFile(filePath);
        }

        public static void DeleteFiles(List<string> filePaths)
        {
            if (filePaths == null) return;
            foreach (var filePath in filePaths)
            {
                Delete(filePath);
            }
        }
    }
}