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
}

internal enum MyCommand
{
    None = 0,
    [Command<MyOption>("action1", "Print first two options.")]
    Command1,
    [Command<MyOption>("action2", "Print third option and its aliases.")]
    Command2,
}

internal static class MyConverters
{
    public static object? Number(string[] s)
    {
        if (s.Length == 0) return null;
        if (s.Length == 1) return int.TryParse(s[0], out var n) ? n : null;
        var list = new List<int>();
        foreach (var x in s)
            if (int.TryParse(x, out var result)) list.Add(result);
        return list.ToArray();
    }
}

internal class MyArgs : NuArgs<MyOption, MyCommand>
{
    public MyArgs(string[] args) : base()
    {
        ParseArgs(args);
    }
    [Alias<MyOption>(MyOption.Option1)]
    public int? Field1;
    [Alias<MyOption>(MyOption.Option2, nameof(ConvertField2))]
    public int[]? Field2;
    [Alias<MyOption>(MyOption.Option3)]
    public string? Field3;

    private static int[] ConvertField2(string[] arg)
    {
        var list = new List<int>();
        foreach (var x in arg)
            if (int.TryParse(x, out var result)) list.Add(result);
        return list.ToArray();
    }
}

internal class Program
{
    static int Main(string[] args)
    {
        var myArgs = new MyArgs(args);

        switch (myArgs.Command)
        {
            case MyCommand.Command1:
            {
                Console.WriteLine("Action1");
                Console.WriteLine(myArgs.Field1);
                Console.WriteLine(myArgs.Field3);
                break;
            }
            case MyCommand.Command2:
            {
                Console.WriteLine("Action2");
                Console.WriteLine(myArgs.Field3?.Length);
                myArgs.Field3?.ToList().ForEach(Console.WriteLine);
                break;
            }
        }
        
        return 0;
    }
}