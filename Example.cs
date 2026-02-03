using NuArgs;

internal enum MyOption
{
    None = 0,
    [Option("a", "Receive a single value.", OptionType.SingleValue)]
    Option1,
    [Option("b", "Receive a multiple values.", OptionType.MultipleValues)]
    Option2,
    [Option("c", "Receive a flag.", OptionType.Flag)]
    Option3,
}

internal enum MyAction
{
    None = 0,
    [Command<MyOption>("action1", "Print first two options.")]
    Action1,
    [Command<MyOption>("action2", "Print third option and its aliases.")]
    Action2,
}

internal class MyArgs : NuArgs<MyOption, MyAction>
{
    [Alias<MyOption>([MyOption.Option1, MyOption.Option2])]
    public bool? Field1;
    [Alias<MyOption>([MyOption.Option3])]
    public string? Field2;
    [Alias<MyOption>([MyOption.Option3], (s) => s.ToList())]
    public int[]? Field3;
}

internal class Program
{
    static int Main(string[] args)
    {
        var myArgs = new MyArgs();
        myArgs.ParseArgs(args);

        switch (myArgs.Command)
        {
            case MyAction.Action1:
            {
                Console.WriteLine("Action1");
                Console.WriteLine(myArgs.Field1);
                Console.WriteLine(myArgs.Field2);
                break;
            }
            case MyAction.Action2:
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