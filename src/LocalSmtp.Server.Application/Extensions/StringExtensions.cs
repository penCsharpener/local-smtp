// source: https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/PunyCodeReplacer.cs

using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalSmtp.Server.Application.Extensions;

public static class StringExtensions
{
    private static Regex punycodeRegex = new Regex("xn--[a-z0-9]+", RegexOptions.Compiled);

    public static string DecodeIdnMapping(this string idnMapping)
    {
        if (idnMapping == null)
        {
            return null;
        }

        return punycodeRegex.Replace(idnMapping, (m) =>
        {
            try
            {
                return new IdnMapping().GetUnicode(m.Value);
            }
            catch (ArgumentException)
            {
                return m.Value;
            }
        });
    }
}
