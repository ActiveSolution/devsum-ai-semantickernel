using System.ComponentModel;
using Microsoft.SemanticKernel;
using NAudio.Wave;

namespace Active.Toolbox.Core.Plugins;

public class AudioPlugin
{
    [KernelFunction("AudioPlugin_PlayAudio"), Description("Play an audio file (MP3)")]
    public static async Task PlayAudio(
        [Description("The url to the audio")] string url
    )
    {
        await using var mf = new MediaFoundationReader(url);
        using var wo = new WasapiOut();
        
        wo.Init(mf);
        wo.Play();
        while (wo.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}