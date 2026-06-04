using Api.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared.Contracts;

namespace Api.Pages;

public class AdPreviewModel(IAdvertisementService advertisementService) : PageModel
{
    public AdvertisementDto? Ad { get; private set; }

    public async Task OnGetAsync(int id)
    {
        Ad = await advertisementService.GetByIdAsync(id);
    }
}
