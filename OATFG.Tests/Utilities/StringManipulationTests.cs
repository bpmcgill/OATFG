using OATFG.Core.Utilities;
using Xunit;

namespace OATFG.Tests.Utilities;

public class StringManipulationTests
{
    [Theory]
    [InlineData("get users", "GetUsers")]
    [InlineData("createUser", "CreateUser")]
    [InlineData("list pets by id", "ListPetsById")]
    public void MakePascalCase_UpperCasesFirstLetterOfEachWord(string input, string expected)
        => Assert.Equal(expected, input.MakePascalCase());

    [Theory]
    [InlineData("GetUsers", "getUsers")]
    [InlineData("HTTP", "hTTP")] // only the first letter is lowercased
    public void LowerCaseFirstLetter_OnlyAffectsFirstCharacter(string input, string expected)
        => Assert.Equal(expected, input.LowerCaseFirstLetter());

    [Theory]
    [InlineData("User", "User")] // already mixed case: first upper, rest lower
    [InlineData("hELLO", "Hello")] // rest of the string is lowercased
    public void UpperCaseFirstLetter_UpperCasesFirstAndLowerCasesRest(string input, string expected)
        => Assert.Equal(expected, input.UpperCaseFirstLetter());

    [Theory]
    [InlineData("hELLO wORLD", "Hello world")]
    public void MakeSentenceCase_UpperCasesFirstAndLowerCasesRest(string input, string expected)
        => Assert.Equal(expected, input.MakeSentenceCase());

    [Theory]
    [InlineData("HelloWorld", "Hello World")]
    public void AddSpaceBeforeCapitalLetters_SplitsCamelCase(string input, string expected)
        => Assert.Equal(expected, input.AddSpaceBeforeCapitalLetters());

    [Theory]
    [InlineData("my-api", "my_api")]
    [InlineData("semi;colon", "semi_colon")]
    [InlineData("Amper&sand", "Ampersand")] // & is removed, not replaced
    [InlineData("hello world", "hello_world")]
    [InlineData("note:  the! 'quoted' ,comma (err)", "note__the___quoted___comma__err_")]
    public void MakeFileSafeName_ReplacesUnsafeCharacters(string input, string expected)
        => Assert.Equal(expected, input.MakeFileSafeName());

    [Theory]
    [InlineData("a--b", "a-b")]
    public void ReduceDashesToOne_CollapsesRuns(string input, string expected)
        => Assert.Equal(expected, input.ReduceDashesToOne());

    [Theory]
    [InlineData("a__b", "a_b")]
    public void ReduceUnderscoresToOne_CollapsesRuns(string input, string expected)
        => Assert.Equal(expected, input.ReduceUnderscoresToOne());

    [Theory]
    [InlineData("a   b", "a b")]
    public void ReduceSpacesToOne_CollapsesRuns(string input, string expected)
        => Assert.Equal(expected, input.ReduceSpacesToOne());

    [Theory]
    [InlineData("abc", 3, "abc   ")]
    public void AppendSpaces_AddsTrailingSpaces(string input, int count, string expected)
        => Assert.Equal(expected, input.AppendSpaces(count));

    [Theory]
    [InlineData("abc", 2, "  abc")]
    public void PrependSpaces_AddsLeadingSpaces(string input, int count, string expected)
        => Assert.Equal(expected, input.PrependSpaces(count));

    [Theory]
    [InlineData("a1!b2@ c3#", "a1b2c3")] // spaces are non-alphanumeric too
    public void RemoveNonAlphanumericCharacters_KeepsOnlyLettersAndDigits(string input, string expected)
        => Assert.Equal(expected, input.RemoveNonAlphanumericCharacters());

    [Theory]
    [InlineData("creates users", "create users")] // nouns are not touched, only verbs
    [InlineData("updates the pets", "update the pets")]
    [InlineData("deletes orphans", "delete orphans")] // only verbs on the list are replaced
    public void ReplacePluralWithSingular_HandlesVerbForms(string input, string expected)
        => Assert.Equal(expected, input.ReplacePluralWithSingular());

    [Theory]
    [InlineData("under_score", "underscore")]
    public void RemoveUnderscores_StripsAll(string input, string expected)
        => Assert.Equal(expected, input.RemoveUnderscores());

    [Theory]
    [InlineData("hyphen-ated", "hyphenated")]
    public void RemoveDashes_StripsAll(string input, string expected)
        => Assert.Equal(expected, input.RemoveDashes());

    [Theory]
    [InlineData("keep these:?!(),.'\"ok", "keep theseok")]
    public void RemoveTargetedSpecialCharacters_StripsOnlyListed(string input, string expected)
        => Assert.Equal(expected, input.RemoveTargetedSpecialCharacters());

    [Theory]
    [InlineData("rock & roll", "rock and roll")]
    public void ReplaceAmpersandWithAnd_LowerCaseAnd(string input, string expected)
        => Assert.Equal(expected, input.ReplaceAmpersandWithAnd());

    [Theory]
    [InlineData("rock & roll", "rock And roll")]
    public void ReplaceAmpersandWithAndEx_UpperCaseAnd(string input, string expected)
        => Assert.Equal(expected, input.ReplaceAmpersandWithAndEx());

    [Theory]
    [InlineData("snake_case name", "snake_case_name")]
    public void ReplaceSpacesWithUnderscores_OnlyAffectsSpaces(string input, string expected)
        => Assert.Equal(expected, input.ReplaceSpacesWithUnderscores());
}

public class FileAndFolderUtilitiesTests
{
    [Fact]
    public void CreateFolder_CreatesAndReturnsTrue()
    {
        string path = Path.Combine(Path.GetTempPath(), "oatfg-folder-" + Guid.NewGuid().ToString("N"), "nested");

        try
        {
            Assert.True(FileAndFolderUtilities.CreateFolder(path));
            Assert.True(Directory.Exists(path));
            // Existing folders report success too
            Assert.True(FileAndFolderUtilities.CreateFolder(path));
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.Delete(Path.GetDirectoryName(path), recursive: true);
            }
        }
    }

    [Fact]
    public void GetExecutableFilePath_ReturnsExistingDirectory()
    {
        string path = FileAndFolderUtilities.GetExecutableFilePath();

        Assert.NotNull(path);
        Assert.True(Directory.Exists(path));
    }
}