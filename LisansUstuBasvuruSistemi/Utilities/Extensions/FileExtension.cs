using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Text;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
   public  static class FileExtension
    { 
        public static string GetFileName(this string path)
        {
            return System.IO.Path.GetFileName(path);
        }
        public static string GetFileExtension(this string path)
        {
            return System.IO.Path.GetExtension(path);
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
            var nGuid = Guid.NewGuid().ToString().Substring(0, 4);
            if (addGuid != null) nGuid = addGuid + "_" + nGuid;
            fileName = fileName.Replace(extension, "_" + nGuid).ReplaceSpecialCharacter() + extension;
            fileName = fileName.Replace("+", "_");
            return fileName;
        }
        public static ImageCodecInfo GetImageCodecInfo(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
