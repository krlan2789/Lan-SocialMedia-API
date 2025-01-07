using System.Text.RegularExpressions;

namespace LanGeng.API.Helper;

public static class HashtagHelper
{
    public static string[] ExtractHashtags(this string content)
    {
        var hashtags = new List<string>();
        var matches = Regex.Matches(content.ToLower(), @"#\w+");

        foreach (Match match in matches)
        {
            hashtags.Add(match.Value.ToLower().Replace("#", ""));
        }

        return [.. hashtags];
    }

}
