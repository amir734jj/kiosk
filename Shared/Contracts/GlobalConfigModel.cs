namespace Shared.Contracts;

public static class ImageUpload
{
    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    public const string AcceptAttribute = "image/jpeg,image/png,image/webp";
    public const int MaxFileSize = 10 * 1024 * 1024;
}

public enum BackgroundStyle
{
    Color,
    CityPhoto,
    StaticPhoto,
    UploadedImage
}

public class GlobalConfigModel
{
    [GlobalConfigCol(Name = "CITY")]
    public string City { get; set; } = string.Empty;

    [GlobalConfigCol(Name = "BACKGROUND_STYLE")]
    public BackgroundStyle BackgroundStyle { get; set; } = BackgroundStyle.Color;

    [GlobalConfigCol(Name = "KIOSK_NAME")]
    public string KioskName { get; set; } = string.Empty;

    [GlobalConfigCol(Name = "REFRESH_INTERVAL_SECONDS")]
    public int RefreshIntervalSeconds { get; set; } = 60;
}
