using System.Collections.Generic;
using System.IO;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class FileHelper
    {
        private const string MesajDosyaYolu = "/DosyaArsivi/MesajEkleri";
        private const string MailDosyaYolu = "/DosyaArsivi/MailEkleri";
        private const string TaleplerDosyaYolu = "/DosyaArsivi/TalepDosyalari";
        private const string ToSavunmaDosyaYolu = "/DosyaArsivi/ToSavunmaDosyalari";
        private const string TiAraRaporDosyaYolu = "/DosyaArsivi/TiAraRaporDosyalari";
        private const string MezuniyetYayinDosyaYolu = "/DosyaArsivi/MezuniyetBasvurulari/YayinDosyalari";
        private const string MezuniyetTezSablonDosyaYolu = "/DosyaArsivi/MezuniyetBasvurulari/TezSablonDosyalari";
        private const string MailSablonDosyaYolu = "/DosyaArsivi/MailSablonEkleri";
        private const string DuyuruDosyaYolu = "/DosyaArsivi/DuyuruEkleri";
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
        public static string SaveMesajDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MesajDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveMailDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MailDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveTalepDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = TaleplerDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveToSavunmaDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = ToSavunmaDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveTiAraRaporDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = TiAraRaporDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveMezuniyetYayinDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MezuniyetYayinDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveMezuniyetTezSablonDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MezuniyetTezSablonDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveMailSablonDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = MailSablonDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static string SaveDuyuruDosya(HttpPostedFileBase file)
        {
            var dosyaAdi = file.FileName.ToFileNameAddGuid();
            var dosyaYolu = DuyuruDosyaYolu + "/" + dosyaAdi;
            SaveFile(file, dosyaYolu);
            return dosyaYolu;
        }
        public static void DeleteFile(string filePath)
        {
            var fullPath = HttpContext.Current.Server.MapPath("~" + filePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        public static void DeleteFiles(List<string> filePaths)
        {
            if (filePaths == null) return;
            foreach (var filePath in filePaths)
            {
                DeleteFile(filePath);
            }
        }
    }
}