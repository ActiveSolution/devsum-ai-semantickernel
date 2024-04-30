using Active.Toolbox.BlazorWeb.Components;
using Active.Toolbox.Core.Plugins;
using Active.Toolbox.Core.Plugins.ActivePlanning;
using Active.Toolbox.Core.Plugins.Bing;
using Active.Toolbox.Core.Plugins.CV;
using Active.Toolbox.Core.Plugins.PeAccounting;
using Active.Toolbox.Core.Plugins.RAG;
using Active.Toolbox.Core.Plugins.Shelly;
using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.Identity.Web;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSpeechSynthesisServices()
                .AddSpeechRecognitionServices();

var peAccountingPluginConfig = new PeAccountingPluginConfig(
    GetConfig("PeAccounting:Email"),
    GetConfig("PeAccounting:Password")
);

var shellyPluginConfig = new ShellyPluginConfig(
    GetConfig("Shelly:Url"),
    GetConfig("Shelly:AuthKey")
);

var ragPluginConfig = new RAGPluginConfig(
    GetConfig("AzureOpenAi:Endpoint"),
    GetConfig("AzureOpenAi:ApiKey"),
	"gpt-35-turbo",
    GetConfig("AzureAISearch:Endpoint"),
    GetConfig("AzureAISearch:Key"),
    GetConfig("AzureAISearch:Index")
);

var openAIConfig = new AzureOpenAIConfig
{
    APIKey = GetConfig("AzureOpenAi:ApiKey"),
    Deployment = GetConfig("AzureOpenAi:Deployment"),
    Endpoint = GetConfig("AzureOpenAi:Endpoint"),
    APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
    Auth = AzureOpenAIConfig.AuthTypes.APIKey    
};


var openAIMemoryConfig = new AzureOpenAIConfig
{
    APIKey = GetConfig("AzureOpenAiSecond:ApiKey"),
    Deployment = "gpt-4-1106-preview-no",
    Endpoint = GetConfig("AzureOpenAiSecond:Endpoint"),
    APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
    Auth = AzureOpenAIConfig.AuthTypes.APIKey
};

var embeddingConfig = new AzureOpenAIConfig
{
    APIKey = GetConfig("AzureOpenAi:ApiKey"),
    Deployment = GetConfig("AzureOpenAi:EmbeddingDeployment"),
    Endpoint = GetConfig("AzureOpenAi:Endpoint"),
    APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
    Auth = AzureOpenAIConfig.AuthTypes.APIKey
};

var aiSearchConfig = new AzureAISearchConfig()
{
    APIKey = GetConfig("AzureAISearch:Key"),
    Endpoint = GetConfig("AzureAISearch:Endpoint"),
    Auth = AzureAISearchConfig.AuthTypes.APIKey    
};

builder.Services
    .AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");

IKernelMemory memory = new KernelMemoryBuilder()
    .WithAzureOpenAITextGeneration(openAIMemoryConfig)
    .WithAzureOpenAITextEmbeddingGeneration(embeddingConfig)
    .WithAzureAISearchMemoryDb(aiSearchConfig)
    .WithSearchClientConfig(new SearchClientConfig
    {
        AnswerTokens = 200,        
        MaxMatchesCount = 3,
        EmptyAnswer = "Jag hittade ingen som matchade det i våran CV-databas"
    })
    .Build();

builder.Services.AddSingleton(memory);

builder.Services.AddSingleton(x =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);
    // Add services to the container.
    kernelBuilder.Services.AddAuthentication("openid")
        .AddMicrosoftIdentityWebApp( (MicrosoftIdentityOptions opts) =>
        {
            opts.ClientId = GetConfig("AzureAd:ClientId");
            opts.ClientSecret = GetConfig("AzureAd:ClientCredentials:ClientSecret");
            opts.Instance = GetConfig("AzureAd:Instance")!;
            opts.TenantId = GetConfig("AzureAd:TenantId");
        }, cookieScheme: null)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddDownstreamApi("active-booking", configuration.GetSection("ActiveBooking:Endpoint"))
        .AddDownstreamApi("active-planning", configuration.GetSection("ActivePlanning:Endpoint"))
        .AddInMemoryTokenCaches();

    kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
    kernelBuilder.Services.AddTransient<ILiveDataScrambler, FictiveNameGenerator>();

    kernelBuilder.Services.AddAzureOpenAIChatCompletion(
        "gpt-35-turbo",
        GetConfig("AzureOpenAi:Endpoint"),
        GetConfig("AzureOpenAi:ApiKey")
    );

    if ( !String.IsNullOrEmpty(configuration.GetSection("AzureOpenAiSecond:Endpoint").Value) &&
         !String.IsNullOrEmpty(configuration.GetSection("AzureOpenAiSecond:ApiKey").Value))
    { 
        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "gpt-4-1106-preview-no",
            GetConfig("AzureOpenAiSecond:Endpoint"),
            GetConfig("AzureOpenAiSecond:ApiKey")
        );
    }

    if (!String.IsNullOrEmpty(configuration.GetSection("AzureOpenAiThird:Endpoint").Value) &&
        !String.IsNullOrEmpty(configuration.GetSection("AzureOpenAiThird:ApiKey").Value))
    {
        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            "gpt-4-1106-preview-uk",
            GetConfig("AzureOpenAiThird:Endpoint"),
            GetConfig("AzureOpenAiThird:ApiKey")
        );
    }

    kernelBuilder.Services.AddAzureOpenAIChatCompletion(
        "gpt-4-1106-preview",
        GetConfig("AzureOpenAi:Endpoint"),
        GetConfig("AzureOpenAi:ApiKey")
    );



    kernelBuilder.Services.AddSingleton(memory);


    var kernel = kernelBuilder.Build();

    kernel.Plugins.AddFromType<MathPlugin>("Math");
    kernel.Plugins.AddFromType<TimePlugin>("Time");
    //kernel.Plugins.AddFromType<EmailPlugin>();

    //kernel.Plugins.AddFromType<SwedishRadioPlugin>();
    kernel.Plugins.AddFromObject(new ShellyPlugin(shellyPluginConfig), "Shelly");
    //kernel.Plugins.AddFromObject(new PeAccountingPlugin(peAccountingPluginConfig, kernel.Services.GetRequiredService<ILiveDataScrambler>()), "PEAccounting");
    kernel.ImportPluginFromType<ActivePlanningPlugin>("ActivePlanning");
    kernel.ImportPluginFromType<ActiveBookingPlugin>("ActiveBooking");
    kernel.ImportPluginFromType<CVPlugin>("KernelMemory");
    kernel.Plugins.AddFromObject(new RAGPlugin(ragPluginConfig), "RAG");
    //kernel.Plugins.AddFromType<AudioPlugin>();

    var bingClient = new WebSearchClient(new ApiKeyServiceClientCredentials(GetConfig("BingSearch:ApiKey")));
    SetPrivatePropertyValue(bingClient, "BaseUri", "https://api.bing.microsoft.com/v7.0");
    //kernel.Plugins.AddFromObject(new BingPlugin(bingClient), "BingSearch");

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

static void SetPrivatePropertyValue<T>(object obj, string propName, T? val)
{
    var objectType = obj.GetType();
    if (objectType.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
    {
        throw new ArgumentOutOfRangeException(nameof(propName), string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
    }

    objectType.InvokeMember(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj,
    [
        val!
    ]);
}
