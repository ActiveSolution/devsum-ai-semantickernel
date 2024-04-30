using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;

using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Active.Toolbox.Core;
using Microsoft.KernelMemory;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;

namespace Active.Toolbox.BlazorWeb.Components.Pages
{
    public class HomeBase : ComponentBase
    {
        private const string AssistantIntroMessage = "Hej! Jag är Active Copilot. Vad kan jag hjälpa dig med?";

        [Inject]
        protected Kernel Kernel { get; set; } = default!;
        [Inject]
        IKernelMemory KernelMemory { get; set; } = default!;
        [Inject]
        ISpeechSynthesisService SpeechSynthesis { get; set; } = default!;
        [Inject]
        ISpeechRecognitionService SpeechRecognition { get; set; } = default!;
        [Inject]
        IConfiguration config { get; set; } = default!;
        [Inject]
        AuthenticationStateProvider authenticationStateProvider { get; set; } = default!;

        protected Dictionary<string, bool> _pluginsEnabled = [];

        protected bool _showDebugPane = false;
        protected bool _showFunctionCalls = false;
        protected bool _enableSpeech = false;

        protected string _error = string.Empty;

        protected string _diagQuestion = string.Empty;

        protected string _openAiModelId = string.Empty;
        protected string _userLanguage = "sv-SE";
        protected List<string> _availableModels = [];
        protected bool _isWaitingForResponse = false;
        protected string _currentFunctionCall = null!;

        public string UserLanguage
        {
            get => _userLanguage;
            set
            {
                _userLanguage = value;
                _history.AddUserMessage($"From now on, the language of this conversation should be: {value}");
            }
        }

        protected RAGSearchMode searchMode = RAGSearchMode.AISearchSemantic;
        protected string _newMessage = string.Empty;
        protected ChatMessages _chatMessages = new();
        protected ChatHistory _history = [];

        protected async Task ResetChat()
        {
            _chatMessages = new ChatMessages();
            _chatMessages.AddCopilotChat(AssistantIntroMessage);

            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var currentUserEmail = authState.User.Identity!.Name;
            var currentUserName = authState.User.Claims.First(c => c.Type == "name")?.Value;

            _history = [
                new ChatMessageContent(AuthorRole.System, CopilotPocCore.GetSystemPrompt(currentUserName!, currentUserEmail!)),
                new ChatMessageContent(AuthorRole.Assistant, AssistantIntroMessage),
            ];
            //var helloMessageResult = await Kernel.InvokePromptAsync($"Write a short greeting to {currentUserName}. Example: Hello John Doe, what can I help you with today?. Use language {UserLanguage}");
            //_chatMessages.AddCopilotChat(helloMessageResult.ToString());
        }

        protected void CloseError()
        {
            _error = string.Empty;
            StateHasChanged();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            _availableModels = Kernel.GetAllServices<IChatCompletionService>().SelectMany(c => c.Attributes).Select(a => a.Value.ToString()).Distinct().ToList();
            _openAiModelId = _availableModels.First();

            _pluginsEnabled = Kernel.Plugins.ToDictionary(x => x.Name, x => true);

#pragma warning disable SKEXP0004

            Kernel.FunctionInvoked += (sender, args) =>
            {
                _currentFunctionCall = null!;
                _chatMessages.AddDebugMessageForFunction(args.Function.Name, args.Arguments, args.Result.GetValue<object>());

                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            };

            Kernel.FunctionInvoking += (sender, args) =>
            {
                _currentFunctionCall = args.Function.Name + "(";
                foreach( var a in args.Arguments)
                {
                    _currentFunctionCall += "'" + a.Value + "',";
                }
                _currentFunctionCall = _currentFunctionCall.TrimEnd(',', ' ');
                _currentFunctionCall += ")";
                InvokeAsync(() =>
                {
                    StateHasChanged();
                });
            };

#pragma warning restore SKEXP0004

        }

        protected async override Task OnInitializedAsync()
        {
            //foreach (var file in Directory.EnumerateFiles(@"C:\Users\jakobe\Documents\cv"))
            //{
            //    Console.WriteLine("Importing " + Path.GetFileName(file));
            //    var i = await KernelMemory.ImportDocumentAsync(file, index: "cv");
            //}

            // var i = kernelMemory.ImportDocumentAsync(@".\data\Personalhandbok.pdf").Result;
            // i = kernelMemory.ImportDocumentAsync(@".\data\Arbetsmiljöhandbok hos Active Solution Sverige AB.docx").Result;

            await ResetChat();
        }

        protected async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await SendMessage();
            }
        }

        protected async Task SendMessage()
        {
            if (string.IsNullOrEmpty(_newMessage))
                return;

            string userMessage = _newMessage;
            _chatMessages.AddUserChat(_newMessage);
            _history.AddUserMessage(_newMessage);
            _newMessage = string.Empty;

            StateHasChanged();

            var localKernel = Kernel.Clone();

            foreach (var pluginEnabled in _pluginsEnabled.Where(x => !x.Value))
            {
                localKernel.Plugins.Remove(localKernel.Plugins[pluginEnabled.Key]);
            }

            if( searchMode == RAGSearchMode.VectorEmbeddings)
            {
                localKernel.Plugins.Remove(localKernel.Plugins["RAG"]);
            }
            else if (searchMode == RAGSearchMode.AISearchSemantic)
            {
                localKernel.Plugins.Remove(localKernel.Plugins["KernelMemory"]);
            }

            var chatCompletionService = localKernel.GetAllServices<IChatCompletionService>().First(c => c.Attributes.Any(a => a.Value.ToString() == _openAiModelId));

            var openAiPromptExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                ChatSystemPrompt = string.Empty,
                ModelId = _openAiModelId,
                Temperature = 0.7,
                MaxTokens = 500
            };

            try
            {
                var fullMessage = "";
                _isWaitingForResponse = true;
                var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
                    _history,
                    executionSettings: openAiPromptExecutionSettings,
                    kernel: localKernel
                );

                _chatMessages.AddCopilotChat("");
                StateHasChanged();
                await foreach (var content in result)
                {
                    _chatMessages.AppendChat(content.Content ?? string.Empty);
                    fullMessage += content.Content;

                    StateHasChanged();
                }

                _history.AddAssistantMessage(fullMessage);
                _isWaitingForResponse = false;
                if (_enableSpeech)
                {
                    await SpeechSynthesis.SpeakAsync(new SpeechSynthesisUtterance()
                    {
                        Text = fullMessage,
                        Lang = UserLanguage,
                        Pitch = 1.0,
                        Rate = 1.0
                    });
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException is Azure.RequestFailedException)
                {
                    var requestFailedException = ex.InnerException as Azure.RequestFailedException;
                    if (requestFailedException.ErrorCode == "content_filter")
                    {
                        _error = "Your message was blocked by the content filter. Please try again.";

                    }
                }
                else
                {
					_error = ex.Message;
				}

				StateHasChanged();
			}
        }

        protected async Task OnRecognizeSpeechClick()
        {
            if (_isRecognizingSpeech)
            {
                await SpeechRecognition.CancelSpeechRecognitionAsync(false);
                await SendMessage();
                StateHasChanged();
            }
            else
            {
                _recognitionSubscription?.Dispose();
                _recognitionSubscription = await SpeechRecognition.RecognizeSpeechAsync(
                    UserLanguage,
                    OnRecognized,
                    null,
                    OnStarted,
                    OnEnded);
            }
        }

        IDisposable? _recognitionSubscription;
        protected bool _isRecognizingSpeech = false;

        Task OnStarted() =>
            InvokeAsync(() =>
            {
                _isRecognizingSpeech = true;
                StateHasChanged();
            });

        Task OnEnded() =>
            InvokeAsync(async () =>
            {
                _isRecognizingSpeech = false;
                await SendMessage();
                StateHasChanged();
            });

        Task OnRecognized(string transcript) =>
            InvokeAsync(() =>
            {
                _newMessage = _newMessage switch
                {
                    "" => transcript,
                    _ => $"{transcript.Trim()} {transcript}".Trim()
                };

                StateHasChanged();
            });

        public void Dispose() => _recognitionSubscription?.Dispose();

    }

}
