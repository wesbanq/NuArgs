using NuArgs;

internal enum MyOption
{
    None = 0,
    [Option("a", OptionType.SingleValue, "Receive a single value.")]
    Option1,
    [Option("b", OptionType.MultipleValues, "Receive multiple values.")]
    Option2,
    [Option("c", OptionType.Flag, "Receive a flag.")]
    Option3,
    [Option("d", OptionType.SingleValue, "Receive a single file path.")]
    Option4,
}

internal enum MyCommand
{
    None = 0,
    [Command<MyOption>("command1", "Print first two options.", MyOption.Option1)]
    Command1,
    [Command<MyOption>("command2", "Print third option and its aliases.")]
    Command2,
}

[NuArgsExtra<MyCommand>(
    aboutText: "This is a test program.", 
    sectionHelpTexts: ["This is a test section.", "This is another test section."], 
    sectionHeaders: ["Section 1", "Section 2"])]
internal class MyArgs : Args<MyOption, MyCommand>
{
    // Auto: no converter — conversion by field type (string[] → int?)
    [OptionTarget<MyOption>(MyOption.Option1)]
    public int? Field1 { get; set; }

    // Built-in: explicitly use Int32Array
    [OptionTarget<MyOption>(MyOption.Option2, nameof(BuiltInConverters.Int32Array))]
    public int[]? Field2 { get; set; }

    [OptionTarget<MyOption>(MyOption.Option3)]
    public bool Field3 { get; set; }

    // Built-in: explicitly use File
    // For default values set the field directly to the value you want to use.
    [OptionTarget<MyOption>(MyOption.Option4, nameof(BuiltInConverters.FileVerifyPath))]
    // Setting the default value this way will will not show up in the help text.
    // To show up in the help text, you need to set the default value in the OptionAttribute.
    // [Option(..., defaultValue: "./Example.cs")]
    public string? Field4 { get; set; } = "./Example.cs";

    // Custom: parse optional ints
    // Keep in mind that for custom converters, 
    // even if the OptionType is SingleValue, the converter will still receive a string[] of length 1.
    private static int[] ParseOptionalInts(string[] arg)
    {
        var list = new List<int>();
        foreach (var x in arg)
            if (int.TryParse(x, out var result)) list.Add(result);
        return [.. list];
    }
}

internal class Program
{
    static int Main(string[] args)
    {
        var myArgs = new MyArgs();
		//myArgs.ParseArgsOrExit(args);
		myArgs.ParseArgs(args);

        switch (myArgs.Command)
        {
            case MyCommand.Command1:
            {
                Console.WriteLine("Command1");
                Console.WriteLine("Field1 (auto): {0}", myArgs.Field1);
                Console.WriteLine("Field3 (flag): {0}", myArgs.Field3);
                break;
            }
            case MyCommand.Command2:
            {
                Console.WriteLine("Command2");
                Console.WriteLine("Flag -a: {0}", myArgs.Field1);
                Console.WriteLine("Option -b (Int32Array): [{0}]", string.Join(", ", myArgs.Field2 ?? []));
                Console.WriteLine("Flag -c: {0}", myArgs.Field3);
                break;
            }
        }
        
        return 0;
    }
}