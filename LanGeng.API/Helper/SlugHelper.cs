using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LanGeng.API.Helper;

public static class SlugHelper
{
    public static string Create(string phrase = "")
    {
        int maxLength = 16;
        string str = RemoveAccents(phrase).ToLower();
        str = Regex.Replace(str, @"[^a-z0-9\s-]", ""); // Remove invalid characters
        str = Regex.Replace(str, @"\s+", " ").Trim(); // Convert multiple spaces into one space
        str = str[..(str.Length <= maxLength ? str.Length : maxLength)].Trim(); // Cut to maxLength chars
        str = Regex.Replace(str, @"\s", "-"); // Hyphens
        str = $"{str}-{RandomString(64 - str.Length)}";
        var strArr = str.ToList();
        strArr[32] = '-';
        strArr[48] = '-';
        return string.Join("", strArr) + DateTime.Now.Ticks;
    }

    private static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static string RemoveAccents(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
