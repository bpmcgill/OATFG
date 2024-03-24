using System.Text.RegularExpressions;

namespace OATFG.Core.Utilities
{
    public static class StringManipulation
    {
        public static string AddSpaceBeforeCapitalLetters(this string input)
            => Regex.Replace(input, @"(?<!^)(?=[A-Z])", " ");

        public static string AppendSpaces(this string input, int count)
            => input + new string(' ', count);

        public static string LowerCaseFirstLetter(this string input)
            => string.Concat(input[..1].ToLower(), input[1..]);

        public static string MakeFileSafeName(this string input)
            => input
            .Replace("-", "_")
            .Replace(".", "_")
            .Replace(":", "_")
            .Replace("?", "_")
            .Replace("!", "_")
            .Replace(",", "_")
            .Replace("'", "_")
            .Replace("\"", "_")
            .Replace(";", "_")
            .Replace("(", "_")
            .Replace(")", "_")
            .Replace("&", "")
            .ReduceDashesToOne()
            .ReduceUnderscoresToOne()
            .ReduceSpacesToOne()
            .Replace(" ", "_");

        public static string MakePascalCase(this string input)
                    => string.Join("", input.Split(' ').Select(x => string.Concat(x.First().ToString().ToUpper(), x.AsSpan(1))));

        public static string MakeSentenceCase(this string input)
            => input[0].ToString().ToUpper() + input[1..].ToLower().ReduceSpacesToOne();

        public static string PrependSpaces(this string input, int count)
            => new string(' ', count) + input;

        public static string ReduceDashesToOne(this string input)
            => Regex.Replace(input, @"\-+", "-");

        public static string ReduceSpacesToOne(this string input)
            => Regex.Replace(input, @"\s+", " ");

        public static string ReduceUnderscoresToOne(this string input)
            => Regex.Replace(input, @"_+", "_");

        public static string RemoveDashes(this string input)
            => input.Replace("-", "");

        public static string RemoveNonAlphanumericCharacters(this string input)
            => Regex.Replace(input, @"[^a-zA-Z0-9]", "");

        public static string RemoveSpaces(this string input)
            => input.Replace(" ", "");

        public static string RemoveTargetedSpecialCharacters(this string input)
            => Regex.Replace(input, @"[:?!(),.'\"";]", "");

        public static string RemoveUnderscores(this string input)
            => input.Replace("_", "");

        public static string ReplaceAmpersandWithAnd(this string input)
            => input.Replace("&", "and");

        public static string ReplaceAmpersandWithAndEx(this string input)
            => input.Replace("&", "And");

        public static string ReplacePluralWithSingular(this string input)
            => input.ToLower()
                .Replace("adds", "add")
                .Replace("appends", "append")
                .Replace("gets", "get")
                .Replace("creates", "create")
                .Replace("deletes", "delete")
                .Replace("links", "link")
                .Replace("puts", "put")
                .Replace("removes", "delete")
                .Replace("retrieves", "get")
                .Replace("returns", "get")
                .Replace("searches", "find")
                .Replace("starts", "start")
                .Replace("unlinks", "unlink")
                .Replace("updates", "update")
                .Replace("upserts", "upsert");

        public static string ReplaceSpacesWithUnderscores(this string input)
            => input.Replace(" ", "_");

        public static string UpperCaseFirstLetter(this string input)
            => string.Concat(input[..1].ToUpper(), input[1..].ToLower());
    }
}