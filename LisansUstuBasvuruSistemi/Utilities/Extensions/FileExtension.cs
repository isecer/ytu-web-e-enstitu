using System;
using System.Collections.Generic;
using System.Text;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
   public  static class FileExtension
    {
        public static string GetFileName(this string Path)
        {
            return System.IO.Path.GetFileName(Path);
        }
        public static string GetFileExtension(this string Path)
        {
            return System.IO.Path.GetExtension(Path);
        }
        public static string ToSetNameFileExtension(this string FName, string Extension)
        {
            if (FName.ToLower().Contains(Extension.ToLower()) == false) FName += Extension;
            return FName;
        }
        public static string ToFileNameAddGuid(this string FileName, string Extension = null, string addGuid = null)
        {
            FileName = FileName.GetFileName();
            Extension = Extension ?? FileName.GetFileExtension();
            var nGuid = Guid.NewGuid().ToString().Substring(0, 4);
            if (addGuid != null) nGuid = addGuid + "_" + nGuid;
            FileName = FileName.Replace(Extension, "_" + nGuid).ReplaceSpecialCharacter() + Extension;
            FileName = FileName.Replace("+", "_");
            return FileName;
        }
    }
}
