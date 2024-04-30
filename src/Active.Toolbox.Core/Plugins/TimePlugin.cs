using System.ComponentModel;
using Microsoft.SemanticKernel;
using moment.net;

namespace Active.Toolbox.Core.Plugins;

public class TimePlugin
{
    [KernelFunction("Time_ToRelativeTime")]
    [Description("Get the relative time between now and the input described for a human. Example output: 'in 7 minutes'")]
    public string ToRelativeTime(
        [Description("The exact date and time")] DateTime input)
    {
        var now = DateTime.Now;
        var relativeTime = input.To(now);

        return relativeTime;
    }

    [KernelFunction("Time_GetCurrentTime")]
    [Description("Get current date and time")]
    public DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }
}