using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class FileExtension
    {
        public static List<string> FExtensions()
        {
            return new List<string>() { ".jpg", ".jpeg", ".tif", ".bmp", ".png", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".rtf", ".pptx" };
        }
        public static string GetFileName(this string path)
        {
            return Path.GetFileName(path);
        }
        public static string GetFileExtension(this string path)
        {
            return Path.GetExtension(path);
        }
        public static string ToSetNameFileExtension(this string fName, string extension)
        {
            if (fName.ToLower().Contains(extension.ToLower()) == false) fName += extension;
            return fName;
        }
        public static string ToFileNameAddGuid(this string fileName, string extension = null, string addGuid = null)
        {
            fileName = fileName.GetFileName();
            extension = extension ?? fileName.GetFileExtension();
            var nGuid = Guid.NewGuid().ToString().Substring(0, 8);
            if (addGuid != null) nGuid = addGuid + "_" + nGuid;
            fileName = fileName.Replace(extension, "_" + nGuid).ReplaceSpecialCharacter() + extension;
            fileName = fileName.Replace("+", "_");
            return fileName;
        }

        public static long GetFileSize(this string path)
        {
            path = HttpContext.Current.Server.MapPath("~" + path);
            if (!File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }
        public static long GetFileSize(this List<string> paths)
        { 
            var filesSize = paths.Select(s => new { path = s, fileSize = s.GetFileSize() }).ToList();
            return filesSize.Sum(s => s.fileSize);
        }
    }
}
