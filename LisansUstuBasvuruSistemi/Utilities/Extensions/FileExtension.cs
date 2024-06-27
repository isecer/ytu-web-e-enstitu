using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class FileExtension
    {
        public static List<string> FExtensions()
        {
            return new List<string>() { ".jpg", ".jpeg", ".tif", ".bmp", ".png", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".rtf", ".pptx" };
        }
        public static string GetFileName(this string path, string extensionFilePath = null)
        {
            if (extensionFilePath.IsNullOrWhiteSpace()) return Path.GetFileName(path);

            var fileName = Path.GetFileName(path);
            var fileExtension = Path.GetExtension(extensionFilePath);
            return fileName.ToSetNameFileExtension(fileExtension);

        }
        public static string GetFileExtension(this string path)
        {
            return Path.GetExtension(path);
        }
        public static string ToSetNameFileExtension(this string fileName, string extension)
        {
            if (fileName.ToLower().Contains(extension.ToLower()) == false) fileName += extension;
            return fileName;
        }
        //public static string ToFileNameAddGuid(this string fileName, string extension = null, string addGuid = null)
        //{
        //    fileName = fileName.GetFileName();
        //    extension = extension ?? fileName.GetFileExtension();
        //    var nGuid = Guid.NewGuid().ToString().Substring(0, 8);
        //    if (addGuid != null) nGuid = addGuid + "_" + nGuid;
        //    // Dosya adındaki geçersiz karakterleri temizle
        //    fileName = fileName.RemoveAllInvalidFileCharacters();
        //    // Dosya adına eklenecek GUID'i ekle
        //    fileName = fileName.Replace(extension, "_" + nGuid + extension);
        //    return fileName;
        //}
        public static string ToFileNameAddGuid(this string fileName)
        {

            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = fileName.GetFileExtension();
            // Dosya adındaki geçersiz karakterleri temizle
            name = name.SanitizeFileName();
            // Dosya adına eklenecek GUID'i ekle
            if (name.Length == 0) name = Guid.NewGuid().ToString().Substring(0, 8);
            name += "_" + Guid.NewGuid().ToString().Substring(0, 8) + extension;
            return name;
        }
        private static readonly char[] AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_. ğüşıöçĞÜŞİÖÇ".ToCharArray();

        // Dosya adını temizleyen metod
        public static string SanitizeFileName(this string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            var sanitizedFileName = new StringBuilder();
            foreach (char c in fileName)
            {
                if (AllowedChars.Contains(c))
                {
                    sanitizedFileName.Append(c);
                }
            }

            // Boş veya sadece yasaklı karakterlerden oluşuyorsa bir default isim belirleyelim
            if (sanitizedFileName.Length == 0)
            {
                return "";
            }

            return sanitizedFileName.ToString();
        }
        public static long GetFileSize(this string path)
        {
            path = HttpContext.Current.Server.MapPath("~" + path);
            return !File.Exists(path) ? 0 : new FileInfo(path).Length;
        }
        public static long GetFileSize(this List<string> paths)
        {
            var filesSize = paths.Select(s => new { path = s, fileSize = s.GetFileSize() }).ToList();
            return filesSize.Sum(s => s.fileSize);
        }
    }
}
