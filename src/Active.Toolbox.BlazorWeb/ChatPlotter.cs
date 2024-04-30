﻿using System.Text.Json;

namespace Active.Toolbox.BlazorWeb;

public class ChatMessages()
{
    public List<ChatMessage> UserChatMessages = new();
    public List<DebugMessage> DebugMessages = new();

    public void AddUserChat(string message)
    {
        UserChatMessages.Add(new ChatMessage("User", message));
    }

    public void AddCopilotChat(string message)
    {
        UserChatMessages.Add(new ChatMessage("Assistant", message));
    }

    public void AddCopilotChat(string message, List<SourceReference> sources)
    {
        UserChatMessages.Add(new ChatMessage("Assistant", message)
        {
            Sources = sources
        });
    }

    public void AddDebugMessageForFunction(string functionName, object? arguments, object? result = null)
    {
        JsonSerializerOptions jso = new JsonSerializerOptions();
        jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        var input = string.Empty;
        if (arguments != null)
        {
            try
            {
                input = JsonSerializer.Serialize(arguments, jso) ?? string.Empty;
            }
            catch (Exception)
            {
                input = arguments.ToString() ?? string.Empty;
            }
        }

        var output = string.Empty;
        if (result != null)
        {
            try
            {
                output += JsonSerializer.Serialize(result, jso) ?? string.Empty;
            }
            catch (Exception)
            {
                output += result.ToString() ?? string.Empty;
            }
        }

        DebugMessages.Add(new DebugMessage(functionName, input, output));
    }

    public void AppendChat(string message)
    {
        var lastMessage = UserChatMessages.Last();
        lastMessage.Content += message;
    }

    public void AppendChat(string message, List<SourceReference> sources)
    {
        var lastMessage = UserChatMessages.Last();
        lastMessage.Content += message;
        lastMessage.Sources.AddRange(sources);
    }
}

public class ChatMessage(string role, string content)
{
    public string Role { get; set; } = role;
    public string Content { get; set; } = content;
    public List<SourceReference> Sources { get; set; } = new List<SourceReference>();
}

public class SourceReference
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string SourceContent { get; set; }
}

public class DebugMessage(string functionName, string input, string output)
{
    public string FunctionName { get; set; } = functionName;
    public string Input { get; set; } = input;
    public string Output { get; set; } = output;
}