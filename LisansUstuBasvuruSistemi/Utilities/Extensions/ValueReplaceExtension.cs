using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueReplaceExtension
    {
        public static string RemoveAllInvalidFileCharacters(this string gelenStr)
        {
            // Geçersiz dosya adı karakterlerini tanımla
            var invalidChars = Path.GetInvalidFileNameChars();

            // Geçersiz dosya yolu karakterlerini tanımla (isteğe bağlı)
            //char[] invalidPathChars = Path.GetInvalidPathChars();

            // Geçersiz karakterleri temizle
            var cleanedStr = new string(gelenStr
                .Where(c => !invalidChars.Contains(c))
                //.Where(c => !invalidPathChars.Contains(c)) // Eğer dosya yollarını da temizlemek istiyorsanız, bu satırı ekleyin
                .ToArray());

            return cleanedStr;
        }
        public static string RemoveNonAlphanumeric(this string input)
        {
            // Yalnızca harfler ve sayıları koru
            const string pattern = "[^a-zA-Z0-9]";
            var cleanedText = Regex.Replace(input, pattern, ""); 
            return cleanedText;
        } 
         
    }
}
