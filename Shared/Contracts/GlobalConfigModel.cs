namespace Shared.Contracts;

public class GlobalConfigModel
{
    [GlobalConfigCol(Name = "CITY")]
    public string City { get; set; } = string.Empty;

    [GlobalConfigCol(Name = "SHOW_CITY_IMAGE")]
    public bool ShowCityImage { get; set; } = true;

    [GlobalConfigCol(Name = "KIOSK_NAME")]
    public string KioskName { get; set; } = string.Empty;
}
