public class NotEnoughArgmunetsException : Exception
{
	public NotEnoughArgmunetsException() : base("Not enough arguments passed")
    { }
}
public class ArgumentParsingException : Exception
{
	public string OptionName { get; set; }
	public string? GivenValue { get; set; }
	public ArgumentParsingException(string optName, string givenValue)
		: base($"Invalid value given to option '{optName}': '{givenValue}'")
	{
		OptionName = optName;
		GivenValue = givenValue;
	}
	public ArgumentParsingException(string optName)
		: base($"No value given to option '{optName}'")
	{
		OptionName = optName;
	}
	public ArgumentParsingException(string customMessage, string optName, string givenValue)
		: base(String.Format(customMessage, optName, givenValue))
	{
		OptionName = optName;
		GivenValue = givenValue;
	}
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class OptionAttribute : Attribute
{
    public string[] OptionNames { get; private set; }
    public OptionType Kind { get; private set; }
    public string HelpText { get; private set; }
    
    public OptionAttribute(string name, string helpText, OptionType type = OptionType.ValueRequired)
    {
        OptionNames = [name];
        HelpText = helpText;
    }
    public OptionAttribute(string[] name, string helpText, OptionType type = OptionType.ValueRequired)
    {
        OptionNames = name;
        HelpText = helpText;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class ActionAttribute : Attribute
	{
		public OptionEnum[]? Required { get; private set; };
		public string[] ActionNames { get; private set; }
        public string HelpText { get; private set; }

		public ActionAttribute(string name, string helpText, OptionEnum[]? required = null)
		{
			ActionNames = [name];
			Required = required;
			HelpText = helpText;
		}
        public ActionAttribute(string[] name, string helpText, OptionEnum[]? required = null)
        {
            ActionNames = name;
            Required = required;
            HelpText = helpText;
        }
	}

public enum OptionType
{ ValueRequired, ValueMultiple, ValueOptional, Flag }

public abstract class NuArgs<OptionEnum, ActionEnum>
    where OptionEnum : Enum
    where ActionEnum : Enum
{ 
    protected string? HelpText { get; private set; }
    private List<OptionEnum> UsedOptions { get; set; } = [];
    
    public static void PrintHelp()
	{
		//TODO dynamically generate help text for specific options/actions using reflection
		Console.WriteLine("help text");
	}

	private static OptionEnum? WhichOption(string arg)
	{
		var fields = typeof(OptionEnum).GetFields();
		for (int i = 1; i < fields.Length; i++)
		{
			var sym = fields[i].GetCustomAttributes<OptionAttribute>();
			foreach (var s in sym)
			{
				if (String.Compare(s.OptionNames[0], arg) == 0)
				{
					return (OptionEnum)fields[i].GetValue(null);
				}
			}
		}
		return null;
	}
	
    private void GiveValueTo(string opt, string value)
	{
		//TODO redo this entirely
		//var fields = GetType().GetFields();
		//var a = Array.Find(fields, f => f.Name == opt);
		//if (a is null)
		//	throw new ArgumentException($"Non-existent argument: {opt}");
		//a.SetValue(this, a.FieldType.);
	}
	
    public void ParseArgs(string[] args)
    {
        if (args.Length == 0)
		{
			throw new ArgumentParsingException("No action given.{0}{1}", "", "");
		}
		Action = Program.GetEnumFromDescription<Command, ActionAttribute>(args[0].ToLower());
		for (int i = 1; i < args.Length; i++)
		{
			var current = WhichOption(args[i]);
			var currentAttribute = typeof(Option).GetField(current.ToString())?.GetCustomAttribute<OptionAttribute>();
			var nextAttribute = args.Length > i+1 ? Program.GetAttrFromDescription<Option, OptionAttribute>(args[i+1]) : null;
			if (currentAttribute?.Kind == OptionEnum.ValueMultiple)
			{
				++i;
				for (; i < args.Length; ++i)
				{
					var newCurrent = WhichOption(args[i]);
					
					if (newCurrent != Option.None) break;
					GiveValueTo(currentAttribute.AssociatedPropertyName, args[i]);
				}
				UsedOptions.Add(current);
				continue;
			}
			else if (WhichOption(args[i - 1]) == Option.None)
			{
				var a = Program.GetAttributeFromEnum<Command, ActionAttribute>(Action);
				if (a is not null && a.Required is not null)
				{
					foreach (var b in a.Required)
					{
						if (!UsedOptions.Contains(b))
						{
							GiveValueTo(
								Program.GetAttributeFromEnum<Option, OptionAttribute>(b)
									?.AssociatedPropertyName ?? "", args[i]
							);
							UsedOptions.Add(b);
						}
					}
				}
			}
		}
        return;
    }
    
	
	public static A? GetEnumAttribute<T, A>(T opt)
		where T : Enum
		where A : Attribute
	{
		return typeof(T).GetField(opt.ToString())?.GetCustomAttribute<A>();
	}
	
}