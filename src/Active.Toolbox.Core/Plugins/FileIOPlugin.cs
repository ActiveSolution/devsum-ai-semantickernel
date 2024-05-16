using System.ComponentModel;
using System.IO;
using Microsoft.SemanticKernel;

namespace Active.Toolbox.Core.Plugins;

public class FileIOPlugin
{
    [KernelFunction("FileIO_ListFiles"), Description("List files in a specified directory")]
    public static string[] ListFiles(
        [Description("The path of the directory")] string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        return Directory.GetFiles(directoryPath);
    }

    [KernelFunction("FileIO_ReadFile"), Description("Read the contents of a specified file")]
    public static string ReadFile(
        [Description("The path of the file")] string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return File.ReadAllText(filePath);
    }

    [KernelFunction("FileIO_WriteFile"), Description("Write content to a specified file")]
    public static void WriteFile(
        [Description("The path of the file")] string filePath,
        [Description("The content to write")] string content)
    {
        File.WriteAllText(filePath, content);
    }
}
