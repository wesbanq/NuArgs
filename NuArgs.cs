using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NuArgs
{
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
	public sealed class OptionAttribute : Attribute
	{
		public string[] OptionNames { get; private set; }
		public OptionType Kind { get; private set; }
		public string HelpText { get; private set; }
		public object? DefaultValue { get; private set; }
		
		public OptionAttribute(
			string name, 
			string helpText, 
			OptionType kind,
			object? defaultValue = null
		)
		{
			OptionNames = [name];
			HelpText = helpText;
			Kind = kind;
			DefaultValue = defaultValue;
		}
		public OptionAttribute(
			string[] name, 
			string helpText, 
			OptionType kind,
			object? defaultValue = null
		)
		{
			OptionNames = name;
			HelpText = helpText;
			Kind = kind;
			DefaultValue = defaultValue;
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class CommandAttribute<OptionEnum> : Attribute
		where OptionEnum : Enum
	{
		public OptionEnum[]? Required { get; private set; }
		public string ActionName { get; private set; }
		public string HelpText { get; private set; }


		public CommandAttribute(string name, string helpText = "", OptionEnum[] required = null)
		{
			if (name.Equals("help", StringComparison.OrdinalIgnoreCase) 
			|| name.Equals("version", StringComparison.OrdinalIgnoreCase))
				throw new ArgumentParsingException($"Reserved command name: '{name}'.");
			
			ActionName = name;
			HelpText = helpText;
			if (required is not null)
				Required = required;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class AliasAttribute<OptionEnum> : Attribute
		where OptionEnum : Enum
	{
		public OptionEnum[] Aliases { get; private set; }
		public Func<string[], object?> Converter { get; private set; }

		public AliasAttribute(OptionEnum[] aliases, Func<string[], object?> converter = null)
		{
			Aliases = aliases;
			Converter = converter;
		}
	}

	public sealed class NuArgsExtraAttribute<CommandEnum> : Attribute
		where CommandEnum : Enum
	{
		public CommandEnum? DefaultCommand { get; private set; }
		public bool UnixStyle { get; private set; }
		public Dictionary<string, string> SectionHelpTexts { get; private set; }

		public NuArgsExtraAttribute(Dictionary<string, string> sectionHelpTexts, CommandEnum defaultCommand = default, bool unixStyle = false)
		{
			DefaultCommand = defaultCommand;
			UnixStyle = unixStyle;
			SectionHelpTexts = sectionHelpTexts;
		}
	}

	internal sealed class DataAccessor
	{
		private readonly MemberInfo _member;
		private Type _dataType => _member is PropertyInfo p ? p.PropertyType : ((FieldInfo)_member).FieldType;

		public void SetValue(object target, object? value, bool convert = true)
		{
			if (value is null)
				return;
			
			var converted = convert 
				? Convert.ChangeType(value, _dataType)
				: value;
			
			if (_member is PropertyInfo p)
				p.SetValue(target, converted);
			else if (_member is FieldInfo f)
				f.SetValue(target, converted);
		}

		public DataAccessor(PropertyInfo prop)
		{
			_member = prop;
		}
		public DataAccessor(FieldInfo field)
		{
			_member = field;
		}
	}

	public enum OptionType : byte
	{ None = 0, Flag = 1, SingleValue = 2, MultipleValues = 3 }

	public abstract class NuArgs<OptionEnum, CommandEnum>
		where OptionEnum : Enum
		where CommandEnum : Enum
	{
		public CommandEnum? Command;
		public List<OptionEnum> UsedOptions { get; private set; }

		protected NuArgsExtraAttribute<CommandEnum>? _extraAttributes;

		private Dictionary<OptionEnum, OptionAttribute> _optionAttributes;
		private Dictionary<OptionEnum, List<(DataAccessor, AliasAttribute<OptionEnum>)>> _aliasAttributes;
		private Dictionary<CommandEnum, CommandAttribute<OptionEnum>> _commandAttributes;
		private Dictionary<string, CommandEnum> _commandNames;
		private Dictionary<string, OptionEnum> _optionNames;
		
		public void PrintHelp(CommandEnum command = default)
		{
			if (command.Equals(default))
			{
				Console.WriteLine($"USAGE:\n\t{GetType().Assembly.GetName().Name} <COMMAND> [OPTIONS...]");
				Console.WriteLine("\nCOMMANDS:");
				foreach (var commandAttribute in _commandAttributes.Values)
				{
					Console.WriteLine($"\t{string.Join(", ", commandAttribute.ActionName)}: {commandAttribute.HelpText}");
				}
				Console.WriteLine("\nOPTIONS:");
				foreach (var optionAttribute in _optionAttributes.Values)
				{
					Console.WriteLine($"\t{string.Join(", ", optionAttribute.OptionNames)}: {optionAttribute.HelpText}");
				}
				if (_extraAttributes?.SectionHelpTexts is not null)
				{
					foreach (var section in _extraAttributes.SectionHelpTexts)
					{
						Console.WriteLine($"\n{section.Key.ToUpper()}:");
						Console.WriteLine($"\t{section.Value.Replace("\n", "\n\t")}");
					}
				}
			}
			else
			{
				var commandAttribute = _commandAttributes[command];
				Console.WriteLine($"{commandAttribute.ActionName}: {commandAttribute.HelpText}");
			}
		}

		private static Dictionary<E, A> InitializeDictionary<E, A>()
			where E : Enum
			where A : Attribute
		{
			var dict = new Dictionary<E, A>();
			var fields = typeof(E).GetFields();
			for (int i = 1; i < fields.Length; i++)
			{
				var sym = fields[i].GetCustomAttributes(false);
				foreach (var s in sym)
				{
					var newKey = (E)fields[i].GetValue(null);
					if (newKey is null) 
						throw new InvalidOperationException($"Failed to get value for option {fields[i].Name}");
					dict.Add(newKey, (A)s!);
				}
			}
			return dict;
		}

		private void Initialize()
		{
			_extraAttributes = GetType().GetCustomAttribute<NuArgsExtraAttribute<CommandEnum>>();
			_optionAttributes = InitializeDictionary<OptionEnum, OptionAttribute>();
			_commandAttributes = InitializeDictionary<CommandEnum, CommandAttribute<OptionEnum>>();
			_commandNames = _commandAttributes.ToDictionary(kvp => kvp.Value.ActionName, kvp => kvp.Key);
			_aliasAttributes = new Dictionary<OptionEnum, List<(DataAccessor, AliasAttribute<OptionEnum>)>>();

			foreach (var field in GetType().GetFields())
			{
				var attribute = field.GetCustomAttribute<AliasAttribute<OptionEnum>>();
				if (attribute is not null && !_aliasAttributes.TryGetValue((OptionEnum)field.GetValue(null)!, out var _))
				{
					foreach (var alias in attribute.Aliases)
					{
						if (!_aliasAttributes.TryGetValue(alias, out var _))
							_aliasAttributes.Add(alias, []);
						_aliasAttributes[alias].Add((new DataAccessor(field), attribute));
					}
				}
				_aliasAttributes[(OptionEnum)field.GetValue(null)!].Add((new DataAccessor(field), attribute));
			}
			foreach (var prop in GetType().GetProperties())
			{
				var attribute = prop.GetCustomAttribute<AliasAttribute<OptionEnum>>();
				if (attribute is not null && !_aliasAttributes.TryGetValue((OptionEnum)prop.GetValue(null)!, out var _))
				{
					foreach (var alias in attribute.Aliases)
					{
						if (!_aliasAttributes.TryGetValue(alias, out var _))
							_aliasAttributes.Add(alias, []);
						_aliasAttributes[alias].Add((new DataAccessor(prop), attribute));
					}
				}
				_aliasAttributes[(OptionEnum)prop.GetValue(null)!].Add((new DataAccessor(prop), attribute));
			}
		}

		private OptionEnum WhichOption(string arg)
		{
			return _optionNames.GetValueOrDefault(arg);
		}
		
		private void GiveValueTo(OptionEnum option, string[] value)
		{
			var accessors = _aliasAttributes[option].Select(x => x.Item1).ToList();
			var converters = _aliasAttributes[option].Select(x => x.Item2).ToList();
			var attr = _optionAttributes[option];

			switch (attr.Kind)
			{
				case OptionType.SingleValue:
				{
					if (value.Length == 0)
						throw new ArgumentParsingException("No value given to option", attr.OptionNames.First());
					foreach (var a in accessors)
						a.SetValue(this, converters.First().Converter(value), false);
					break;
				}
				case OptionType.MultipleValues:
				{
					if (value.Length == 0)
						throw new ArgumentParsingException("No value given to option", attr.OptionNames.First());
					foreach (var a in accessors)
						a.SetValue(this, value);
					break;
				}
				case OptionType.Flag:
				{
					foreach (var a in accessors)
						a.SetValue(this, true);
					break;
				}
			}
		}
		
		private string[]? ParseOptionUnixStyle(string arg)
		{
			if (arg[0].Equals('-'))
			{
				if (arg[1].Equals('-'))
					return [arg[2..]];
				return arg[1..].Split();
			}
			return null;
		}

		private string[]? ParseOption(string arg)
		{
			if (arg[0].Equals('-'))
			{
				return [arg.SkipWhile(c => c.Equals('-')).ToString()];
			}
			return null;
		}

		private void SetDefaultValues()
		{
			foreach (var alias in _aliasAttributes)
			{
				foreach (var a in alias.Value.Select(x => x.Item1))
				{
					if (!UsedOptions.Contains(alias.Key) && _optionAttributes[alias.Key].DefaultValue is not null)
						a.SetValue(this, _optionAttributes[alias.Key].DefaultValue, false);
				}
			}
		}

		public void ParseArgs(string[] args)
		{
			SetDefaultValues();

			if (args[0].Equals("help") || args.Length == 0)
			{
				if (args.Length > 1)
				{
					var nextCommand = _commandNames[args[1]];
					PrintHelp(nextCommand);
				}
				else
				{
					PrintHelp();
				}
				return;
			}
			if (args[0].Equals("version"))
			{
				Console.WriteLine($"{GetType().Assembly.GetName().Name}: {GetType().Assembly.GetName().Version}v");
				return;
			}

			// get default command if there is no command given
			var startIndex = 1;
			var givenCommand = _commandNames.TryGetValue(args[0], out Command);
			if (!givenCommand)
			{
				if (_extraAttributes == null)
					throw new NotEnoughArgmunetsException();
				var rawDefault = _extraAttributes.DefaultCommand;
				if (rawDefault == null)
					throw new NotEnoughArgmunetsException();
				Command = (CommandEnum)rawDefault;
			}

			// parse required options
			var requiredOptions = _commandAttributes[Command].Required;
			if (requiredOptions is not null)
			{
				var lastOpt = requiredOptions.Length-1;
				for (int i = startIndex; i < args.Length; i++)
				{
					var optName = ParseOption(args[i]);
					if (optName is not null || lastOpt == 0)
					{
						startIndex = i;
						break;
					}


				}
			}

			// parse options
			for (int i = startIndex; i < args.Length; i++)
			{		
				var optName = (_extraAttributes?.UnixStyle == true 
					? ParseOptionUnixStyle(args[i])
					: ParseOption(args[i])) 
					?? throw new ArgumentParsingException($"Unknown option: '{args[i]}'.");
                var finalName = optName.Last();

				for (var ii = 0; ii < optName.Length; ++ii)
				{
					var thisFlag = WhichOption(optName[ii]);
					if (_optionAttributes[thisFlag].Kind == OptionType.Flag)
					{
						GiveValueTo(thisFlag, []);
						UsedOptions.Add(thisFlag);
						continue;
					}
					else
					{
						if (ii == optName.Length-1)
							break;
						else
							throw new ArgumentParsingException("No value given to option", optName[ii]);
					}
				}

				var current = WhichOption(finalName);
				var currentAttribute = _optionAttributes[current];
				var next = i+1 < args.Length ? WhichOption(args[i+1]) : default;
				var nextAttribute = next.Equals(default) ? null : _optionAttributes[next];

				if (UsedOptions.Contains(current))
					throw new ArgumentParsingException("Option used twice", currentAttribute.OptionNames.First());

				switch (currentAttribute.Kind)
				{
					case OptionType.MultipleValues:
					{
						++i;
						var v = 0;
						for (; i < args.Length; ++i)
						{
							if (!WhichOption(args[i]).Equals(default(OptionEnum))) break;
							++v;
						}
						GiveValueTo(current, args.AsSpan(i, v).ToArray());
						UsedOptions.Add(current);
						--i; // for loop will increment i again
						continue;
					}
					case OptionType.SingleValue:
					{
						if (nextAttribute is null)
							throw new ArgumentParsingException("No value given to option", currentAttribute.OptionNames.First());
						GiveValueTo(current, [args[++i]]);
						UsedOptions.Add(current);
						continue;
					}
					case OptionType.Flag:
					{
						GiveValueTo(current, []);
						UsedOptions.Add(current);
						continue;
					}
				}
			}

			return;
		}

		public NuArgs()
		{
			Initialize();
		}
	}
}