using Active.Toolbox.BlazorWeb.Components;
using Active.Toolbox.Core.Plugins;
using Active.Toolbox.Core.Plugins.Shelly;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var shellyPluginConfig = new ShellyPluginConfig(
    GetConfig("Shelly:Url"),
    GetConfig("Shelly:AuthKey")
);

builder.Services.AddSingleton(x =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);

    kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));

    kernelBuilder.Services.AddAzureOpenAIChatCompletion(
        GetConfig("AzureOpenAi:Deployment"),
        GetConfig("AzureOpenAi:Endpoint"),
        GetConfig("AzureOpenAi:ApiKey")
    );

    var kernel = kernelBuilder.Build();

    kernel.Plugins.AddFromType<MathPlugin>("Math");
    kernel.Plugins.AddFromType<TimePlugin>("Time");
    kernel.Plugins.AddFromObject(new ShellyPlugin(shellyPluginConfig), "Shelly");

    return kernel;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

string GetConfig(string setting)
{
    return configuration[setting] ?? throw new Exception("Missing config");
}
