using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueReplaceExtension
    {
        public static string RemoveIllegalFileNameChars(this string input, string replacement = "")
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(input, replacement);
        }
        public static string ReplaceSpecialCharacter(this string gelenStr)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            var fname = r.Replace(gelenStr, "");
            return fname;

        }
    }
}
