internal enum ExampleOption
{
    Option1,
    Option2,
    Option3,
}

internal enum ExampleAction
{
    Action1,
    Action2,
    Action3,
}

internal class ExampleArgs : NuArgs<ExampleOption, ExampleAction>
{
    
}

internal class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        return 0;
    }
}