using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace Active.Toolbox.Core.Plugins;

public class MathPlugin
{
    [KernelFunction("Math_Sqrt"), Description("Take the square root of a number")]
    public static double Sqrt(
        [Description("The number to take a square root of")] double number1
    )
    {
        return Math.Sqrt(number1);
    }

    [KernelFunction("Math_Add"), Description("Add two numbers")]
    public static double Add(
        [Description("The first number to add")] double number1,
        [Description("The second number to add")] double number2
    )
    {
        return number1 + number2;
    }

    [KernelFunction("Math_Summarize"), Description("Summarize a list of numbers and return the sum")]
    public static double Summarize(
        [Description("A list of numbers to get the sum of, example: [ \"1.1\", \"2.2\", \"3\" ]")] string numbers
    )
    {
        NumberFormatInfo nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";

        try
        {
            var numbersArray = JsonSerializer.Deserialize<string[]>(numbers) ?? throw new ArgumentException("Could not deserialize numbers");
            return numbersArray.Select(x => double.Parse(x, nfi)).Sum();
        }
        catch (Exception)
        {
            var numbersArray = numbers.Split(',');
            return numbersArray.Select(x => double.Parse(x, nfi)).Sum();
        }
    }

    [KernelFunction("Math_Subtract"), Description("Subtract two numbers")]
    public static double Subtract(
        [Description("The first number to subtract from")] double number1,
        [Description("The second number to subtract away")] double number2
    )
    {
        return number1 - number2;
    }

    [KernelFunction("Math_Multiply"), Description("Multiply two numbers. When increasing by a percentage, don't forget to add 1 to the percentage.")]
    public static double Multiply(
        [Description("The first number to multiply")] double number1,
        [Description("The second number to multiply")] double number2
    )
    {
        return number1 * number2;
    }

    [KernelFunction("Math_Divide"), Description("Divide two numbers")]
    public static double Divide(
        [Description("The first number to divide from")] double number1,
        [Description("The second number to divide by")] double number2
    )
    {
        return number1 / number2;
    }

    [KernelFunction("Math_CountList"), Description("Count the number of items in a list")]
    public static int CountList(
        [Description("The list of items")] string items
    )
    {
        if (string.IsNullOrEmpty(items))
            return 0;

        var itemsArray = items.Split(",");
        return itemsArray.Length;
    }
}