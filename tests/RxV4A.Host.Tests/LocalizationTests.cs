using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using RxV4A.Host.Localization;

namespace RxV4A.Host.Tests;

public sealed class LocalizationTests
{
    private static readonly Regex JapaneseScript = new(@"[\u3040-\u30ff\u4e00-\u9fff]", RegexOptions.Compiled);

    [Fact]
    public void Resources_HaveTheSameKeysInFrenchEnglishAndJapanese()
    {
        var french = ReadSet(CultureInfo.GetCultureInfo("fr"));
        var english = ReadSet(CultureInfo.GetCultureInfo("en"));
        var japanese = ReadSet(CultureInfo.GetCultureInfo("ja"));
        var properties = typeof(Strings)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(string) && property.GetMethod?.GetParameters().Length == 0)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(properties.OrderBy(name => name, StringComparer.Ordinal), french.Keys.OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(french.Keys.OrderBy(name => name, StringComparer.Ordinal), english.Keys.OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(french.Keys.OrderBy(name => name, StringComparer.Ordinal), japanese.Keys.OrderBy(name => name, StringComparer.Ordinal));
        Assert.All(french.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(english.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(japanese.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.DoesNotContain(french.Values, value => JapaneseScript.IsMatch(value));
        Assert.DoesNotContain(english.Values, value => JapaneseScript.IsMatch(value));
        Assert.Contains(japanese.Values, value => JapaneseScript.IsMatch(value));
    }

    [Fact]
    public void Strings_FollowTheCurrentUiCulture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
            Assert.Equal("Recherche de l'amplificateur…", Strings.Main_Searching);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            Assert.Equal("Searching for the amplifier…", Strings.Main_Searching);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja");
            Assert.Equal("アンプを検索しています…", Strings.Main_Searching);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData(null, "fr-FR", "fr")]
    [InlineData(null, "en-GB", "en")]
    [InlineData(null, "ja-JP", "ja")]
    [InlineData(null, "de-DE", "fr")]
    [InlineData("auto", "ja-JP", "ja")]
    [InlineData("auto", "de-DE", "fr")]
    [InlineData("en", "fr-FR", "en")]
    [InlineData("ja", "en-US", "ja")]
    [InlineData("fr-CA", "en-US", "fr")]
    [InlineData("de", "en-US", "en")]
    public void Resolve_UsesTheSavedLanguageOrTheWindowsLanguage(string? preference, string system, string expected)
    {
        var culture = UiLanguageSelection.Resolve(preference, CultureInfo.GetCultureInfo(system));
        Assert.Equal(expected, culture.TwoLetterISOLanguageName);
    }

    [Fact]
    public async Task UiLanguage_RoundTripsAndCanBeReadBeforeStartup()
    {
        var root = Path.Combine(Path.GetTempPath(), "rxv4a-settings-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonSettingsStore(root);
            await store.SaveAsync(new AppSettings { UiLanguage = "en" }, CancellationToken.None);

            var loaded = await store.LoadAsync(CancellationToken.None);
            Assert.Equal("en", loaded.UiLanguage);
            Assert.Equal("en", UiLanguageSelection.ReadPersistedPreference(root));
            Assert.Equal("en", UiLanguageSelection.Resolve(
                UiLanguageSelection.ReadPersistedPreference(root),
                CultureInfo.GetCultureInfo("fr-FR")).TwoLetterISOLanguageName);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static Dictionary<string, string> ReadSet(CultureInfo culture)
    {
        var manager = new ResourceManager(
            "RxV4A.Host.Localization.Strings",
            typeof(Strings).Assembly);
        var set = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"Missing resources for {culture.Name}.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry entry in set)
        {
            values[(string)entry.Key!] = (string)entry.Value!;
        }

        return values;
    }
}
