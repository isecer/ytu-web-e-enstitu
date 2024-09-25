using LisansUstuBasvuruSistemi.Utilities.MailManager;
using System;
using System.Collections.Generic;
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

        public static string SetYaziContentParameters(string htmlContent, List<MailParameterDto> parameterDtos)
        {
            foreach (var parameter in parameterDtos)
            {
                var parameterRegex = new Regex($@"@{Regex.Escape(parameter.Key)}\b", RegexOptions.IgnoreCase);

                htmlContent = parameterRegex.Replace(htmlContent, match =>
                {
                    var replacementValue = parameter.Value ?? string.Empty;

                    // Check if the original parameter was all uppercase
                    if (match.Value == match.Value.ToUpper())
                        replacementValue = replacementValue.ToUpper();

                    // Handle link parameters
                    if (parameter.IsLink)
                        return $"<a href='{replacementValue}' target='_blank'>{replacementValue}</a>";

                    return replacementValue;
                });
            }

            return htmlContent;
        }

        public static string RemoveUnusedParameters(string htmlContent, List<MailParameterDto> parameterDtos)
        {
            var unusedParameterRegex = new Regex(@"\{\{_removeRw_.*?@(\w+).*?\}\}");
             
            var strVal= unusedParameterRegex.Replace(htmlContent, match =>
            {
                var parameterName = match.Groups[1].Value; 
                var parameter = parameterDtos.FirstOrDefault(p => string.Equals(p.Key, parameterName, StringComparison.OrdinalIgnoreCase));

                if (parameter == null || string.IsNullOrWhiteSpace(parameter.Value))
                    return string.Empty; // Remove the entire block if parameter is not found or empty

                // Remove only the {{_removeRw_ and }} parts, keeping the content
                return match.Value.Replace("{{_removeRw_", "").Replace("}}", "");
            });
            return strVal;
        }

        public static string ProcessHtmlContent(string htmlContent, List<MailParameterDto> parameterDtos)
        {
            // First, remove unused parameters
            var processedContent = RemoveUnusedParameters(htmlContent, parameterDtos);

            // Then, replace the remaining parameters
            return SetYaziContentParameters(processedContent, parameterDtos);
        }


    }
}
