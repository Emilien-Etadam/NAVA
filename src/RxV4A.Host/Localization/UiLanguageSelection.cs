using System.Globalization;
using System.Text.Json;

namespace RxV4A.Host;

public static class UiLanguageSelection
{
    public const string Auto = "auto";
    public const string French = "fr";
    public const string English = "en";
    public const string Japanese = "ja";

    public static void ApplyPersistedOrSystem(string? localAppDataPath = null) =>
        Apply(ReadPersistedPreference(localAppDataPath));

    public static void Apply(string? preference)
    {
        var culture = Resolve(preference, CultureInfo.CurrentUICulture);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static CultureInfo Resolve(string? preference, CultureInfo systemCulture)
    {
        var code = Normalize(preference) ?? MapSystem(systemCulture);
        return CultureInfo.GetCultureInfo(code);
    }

    public static string? Normalize(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference))
        {
            return null;
        }

        var trimmed = preference.Trim();
        if (trimmed.Equals(Auto, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = trimmed.Replace('_', '-');
        var dash = token.IndexOf('-', StringComparison.Ordinal);
        var language = (dash > 0 ? token[..dash] : token).ToLowerInvariant();
        return language is French or English or Japanese ? language : null;
    }

    public static string MapSystem(CultureInfo systemCulture)
    {
        var language = systemCulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return language is French or English or Japanese ? language : French;
    }

    public static string? ReadPersistedPreference(string? localAppDataPath = null)
    {
        foreach (var path in JsonSettingsStore.CandidateSettingsPaths(localAppDataPath))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty("uiLanguage", out var property) &&
                    property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
            {
                return null;
            }

            return null;
        }

        return null;
    }
}
