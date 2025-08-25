namespace MySensorApi.Services.Utils
{
    public static class ChipId
    {
        public static string Normalize(string? raw) =>
            (raw ?? string.Empty).Trim().ToUpperInvariant();
    }
}
