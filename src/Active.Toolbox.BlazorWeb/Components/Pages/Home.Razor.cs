using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;

using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Active.Toolbox.Core;
using Microsoft.AspNetCore.Components.Web;

namespace Active.Toolbox.BlazorWeb.Components.Pages
{
    public class HomeBase : ComponentBase
    {
        private const string AssistantIntroMessage = "Hi! I am your helpful copilot, what can I do for you?";

        [Inject]
        protected Kernel Kernel { get; set; } = default!;

        protected Dictionary<string, bool> _pluginsEnabled = [];

        protected bool _showDebugPane = false;
        protected bool _showFunctionCalls = false;
        protected bool _enableSpeech = false;

        protected string _error = string.Empty;

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

        protected string _newMessage = string.Empty;
        protected ChatMessages _chatMessages = new();
        protected ChatHistory _history = [];

        protected async Task ResetChat()
        {
            _chatMessages = new ChatMessages();
            _chatMessages.AddCopilotChat(AssistantIntroMessage);

            _history = [
                new ChatMessageContent(AuthorRole.System, CopilotPocCore.GetSystemPrompt()),
                new ChatMessageContent(AuthorRole.Assistant, AssistantIntroMessage),
            ];
        }

        protected void CloseError()
        {
            _error = string.Empty;
            StateHasChanged();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            _availableModels = Kernel.GetAllServices<IChatCompletionService>().SelectMany(c => c.Attributes).Select(a => a.Value.ToString()).Distinct().ToList()!;
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
    }

}
