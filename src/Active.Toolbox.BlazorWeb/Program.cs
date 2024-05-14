using Active.Toolbox.BlazorWeb.Components;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(x =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);

    kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));

    if(!String.IsNullOrEmpty(configuration["OpenAi:ApiKey"]))
    {
        kernelBuilder.Services.AddOpenAIChatCompletion(configuration["OpenAi:Model"]!, configuration["OpenAi:ApiKey"]!);
	}
    else { 
	    kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            GetConfig("AzureOpenAi:Deployment"),
            GetConfig("AzureOpenAi:Endpoint"),
            GetConfig("AzureOpenAi:ApiKey")
        );
    }
    var kernel = kernelBuilder.Build();

	kernel.Plugins.AddFromType<Active.Toolbox.Core.Plugins.MathPlugin>("Math");
    kernel.Plugins.AddFromType<Active.Toolbox.Core.Plugins.TimePlugin>("Time");

	// Built-in plugins
	//#pragma warning disable SKEXP0050
	//kernel.Plugins.AddFromType<FileIOPlugin>("FileIO");
	//kernel.Plugins.AddFromType<ConversationSummaryPlugin>("ConversationSummary");
	//kernel.Plugins.AddFromType<Microsoft.SemanticKernel.Plugins.Core.WaitPlugin>("Wait");
	//#pragma warning restore SKEXP0050

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
