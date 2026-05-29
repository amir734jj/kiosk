namespace Shared.Contracts;

public enum BackgroundStyle
{
    Color,
    CityPhoto,
    StaticPhoto
}

public class GlobalConfigModel
{
    [GlobalConfigCol(Name = "CITY")]
    public string City { get; set; } = string.Empty;

    [GlobalConfigCol(Name = "BACKGROUND_STYLE")]
    public BackgroundStyle BackgroundStyle { get; set; } = BackgroundStyle.Color;

    [GlobalConfigCol(Name = "KIOSK_NAME")]
    public string KioskName { get; set; } = string.Empty;
}
