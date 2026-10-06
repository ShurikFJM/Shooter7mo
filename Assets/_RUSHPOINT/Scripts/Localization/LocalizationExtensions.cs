public static class LocalizationExtensions
{
    public static readonly string[] LanguageKeys = { "English", "Spanish", "Portuguese", "Japanese" };

    public static string Localize(this string key)
    {
        return LocalizationManager.Source.GetLocalizedText(key);
    }
}
