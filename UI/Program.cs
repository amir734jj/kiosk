using Blazored.LocalStorage;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;
using Shared.Contracts.Interfaces;
using UI;
using UI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    apiBaseUrl = builder.HostEnvironment.BaseAddress;
}

builder.Services.AddBlazoredLocalStorageAsSingleton();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddTransient<BearerTokenHandler>();

var refitSettings = new RefitSettings
{
    ContentSerializer = new NewtonsoftJsonContentSerializer(
        new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        })
};

foreach (var apiType in new[]
{
    typeof(IAuthApi),
    typeof(IOfficesApi),
    typeof(IAnnouncementsApi),
    typeof(IUsersApi),
    typeof(IPublicApi),
    typeof(IGlobalConfigApi)
})
{
    builder.Services
        .AddRefitClient(apiType, refitSettings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
        .AddHttpMessageHandler<BearerTokenHandler>();
}

builder.Services.AddScoped<ApiService>();
builder.Services.AddHxServices();
builder.Services.AddHxMessenger();
builder.Services.AddHxMessageBoxHost();

var host = builder.Build();

var auth = host.Services.GetRequiredService<AuthService>();
await auth.InitAsync();

await host.RunAsync();
