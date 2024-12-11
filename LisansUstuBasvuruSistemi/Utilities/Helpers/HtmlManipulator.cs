using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public class HtmlManipulator
    {
        public static string RemoveTrWithParameter(string htmlContent, string parameter)
        {
            string pattern = $@"<tr\b[^>]*>.*?{Regex.Escape(parameter)}.*?</tr>";
            return Regex.Replace(htmlContent, pattern, "", RegexOptions.Singleline);
        }
        public static string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            // HTML kodunu çöz
            string decoded = HttpUtility.HtmlDecode(html);

            // Script ve style etiketlerini kaldır
            decoded = Regex.Replace(decoded, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            decoded = Regex.Replace(decoded, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

            // HTML etiketlerini kaldır
            decoded = Regex.Replace(decoded, @"<[^>]+>", "");

            // Fazla boşlukları temizle
            decoded = Regex.Replace(decoded, @"(\r\n|\n|\r|\t|\s)+", " ", RegexOptions.Multiline);

            // Baştaki ve sondaki boşlukları kaldır
            return decoded.Trim();
        }
    }
}