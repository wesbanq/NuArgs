using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace NuArgs
{
	public enum ArgumentParsingExceptionType : byte
	{
		NonExistantOption,
		InvalidOptionValue,
		DuplicateOption,
		NoCommandGiven,
		NoDefaultCommandSet,
		NoValueGivenToOption,
		FileDoesNotExist,
		UnknownConverter,
		UnknownOption,
		UnknownCommand,
		UnknownOptionValue,
		UnknownCommandValue,
		TooManyPositionalArguments,
		ReservedCommandName,
		MultipleValuesOptionNotAtEnd,
		CustomMessage,
	}
	
	public class ArgumentParsingException : Exception
	{
		public ArgumentParsingExceptionType Type { get; set; }
		public string OptionName { get; set; }
		public string? GivenValue { get; set; }

		private static string GetMessage(ArgumentParsingExceptionType type, string? optionName = null, string? givenValue = null)
		{
			return type switch
			{
				ArgumentParsingExceptionType.NonExistantOption => $"No value given to option '{optionName}'.",
				ArgumentParsingExceptionType.InvalidOptionValue => $"Invalid value given to option '{optionName}': '{givenValue}'.",
				ArgumentParsingExceptionType.DuplicateOption => $"Option '{optionName}' used twice.",
				ArgumentParsingExceptionType.NoCommandGiven => "No command given.",
				ArgumentParsingExceptionType.NoDefaultCommandSet => "No default command set.",
				ArgumentParsingExceptionType.NoValueGivenToOption => $"No value given to option '{optionName}'.",
				ArgumentParsingExceptionType.FileDoesNotExist => $"File does not exist: '{optionName}'.",
				ArgumentParsingExceptionType.UnknownConverter => $"Unknown converter: '{optionName}'.",
				ArgumentParsingExceptionType.UnknownOption => $"Unknown option: '{optionName}'.",
				ArgumentParsingExceptionType.UnknownCommand => $"Unknown command: '{optionName}'.",
				ArgumentParsingExceptionType.UnknownOptionValue => $"Unknown option value: '{optionName}'.",
				ArgumentParsingExceptionType.UnknownCommandValue => $"Unknown command value: '{optionName}'.",
				ArgumentParsingExceptionType.TooManyPositionalArguments => "Too many positional arguments.",
				ArgumentParsingExceptionType.ReservedCommandName => $"Reserved command name: '{optionName}'.",
				ArgumentParsingExceptionType.MultipleValuesOptionNotAtEnd => $"Multiple values option '{optionName}' is not at the end of the required options.",
				ArgumentParsingExceptionType.CustomMessage => throw new UnreachableException("Custom message should not be used in this context."),
			};
		}

		public ArgumentParsingException(string message)
			: base(message)
		{ 
			Type = ArgumentParsingExceptionType.CustomMessage;
		}
		public ArgumentParsingException(ArgumentParsingExceptionType type)
			: base(GetMessage(type))
		{ 
			Type = type;
		}
		public ArgumentParsingException(ArgumentParsingExceptionType type, string optionName)
			: base(GetMessage(type, optionName))
		{
			OptionName = optionName;
			Type = type;
		}
		public ArgumentParsingException(ArgumentParsingExceptionType type, string optionName, string givenValue)
			: base(GetMessage(type, optionName, givenValue))
		{
			OptionName = optionName;
			GivenValue = givenValue;
			Type = type;
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
			OptionType kind,
			string helpText = "No help available.",
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
			OptionType kind,
			string helpText = "No help available.",
			object? defaultValue = null
		)
		{
			OptionNames = name;
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


		public CommandAttribute(
			string name, 
			string helpText = "No help available.", 
			params OptionEnum[] required
		)
		{
			if (name.Equals("help", StringComparison.OrdinalIgnoreCase) 
			|| name.Equals("version", StringComparison.OrdinalIgnoreCase))
				throw new ArgumentParsingException(ArgumentParsingExceptionType.ReservedCommandName, name);
			
			// check if the first MultipleValues option is not at the end of the 'required' array
			if (required is not null)
			{
				for (int i = 0; i < required.Length; i++)
				{
					var option = required[i];
					var attr = typeof(OptionEnum)
						.GetField(option.ToString())
						?.GetCustomAttributes(typeof(OptionAttribute), false)
						.First() as OptionAttribute;
					if (attr is not null && attr.Kind == OptionType.MultipleValues && i != required.Length - 1)
						throw new ArgumentParsingException(ArgumentParsingExceptionType.MultipleValuesOptionNotAtEnd, option.ToString());
				}
			}

			ActionName = name;
			HelpText = helpText;
			Required = required;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class OptionTargetAttribute<OptionEnum> : Attribute
		where OptionEnum : Enum
	{
		public OptionEnum Alias { get; private set; }
		public DataAccessor Accessor;
		public string? Converter { get; private set; }

		public OptionTargetAttribute(
			OptionEnum alias,
			string? converter = null
		)
		{
			Alias = alias;
			Converter = converter;
		}
	}

	public sealed class NuArgsExtraAttribute<CommandEnum> : Attribute
		where CommandEnum : Enum
	{
		public CommandEnum? DefaultCommand { get; private set; }
		public bool UnixStyle { get; private set; }
		public string? AboutText { get; private set; }
		public string[]? SectionHelpTexts { get; private set; }
		public string[]? SectionHeaders { get; private set; }
		public Type? CustomOutputType { get; private set; }

		public NuArgsExtraAttribute(
			CommandEnum defaultCommand = default, 
			bool unixStyle = false, 
			string[]? sectionHelpTexts = null, 
			string[]? sectionHeaders = null, 
			string? aboutText = null,
			Type? customOutputType = null
		)
		{
			if (sectionHeaders?.Length != sectionHelpTexts?.Length)
				throw new ArgumentException("Section headers and help texts must have the same length.");
			
			SectionHeaders = sectionHeaders;
			SectionHelpTexts = sectionHelpTexts;
			UnixStyle = unixStyle;
			DefaultCommand = defaultCommand;
			AboutText = aboutText;
			CustomOutputType = customOutputType;
		}
	}

	public sealed class DataAccessor
	{
		private readonly MemberInfo _member;
		private Type _dataType => _member is PropertyInfo p ? p.PropertyType : ((FieldInfo)_member).FieldType;

		public object? GetValue(object target)
		{
			if (_member is PropertyInfo p)
				return p.GetValue(target);
			else if (_member is FieldInfo f)
				return f.GetValue(target);
			return null;
		}

		public void SetValue(object target, object? value, bool convert = true)
		{
			if (value is null)
				return;
			
			object? converted;
			if (convert && value is string[] strArr)
				converted = ConvertStringArrayTo(strArr, _dataType);
			else if (convert)
				converted = Convert.ChangeType(value, _dataType);
			else
				converted = value;
			
			if (_member is PropertyInfo p)
				p.SetValue(target, converted);
			else if (_member is FieldInfo f)
				f.SetValue(target, converted);
		}

		private static object? ConvertStringArrayTo(string[] values, Type targetType)
		{
			if (targetType == typeof(string[]))
				return values;

			// Single value: use first element
			if (!targetType.IsArray && (targetType.IsValueType || targetType == typeof(string)))
			{
				var elementType = Nullable.GetUnderlyingType(targetType) ?? targetType;
				if (values.Length == 0)
					return null;
				return Convert.ChangeType(values[0], elementType);
			}

			// Array of T
			if (targetType.IsArray)
			{
				var elementType = targetType.GetElementType()!;
				var array = Array.CreateInstance(elementType, values.Length);
				for (int i = 0; i < values.Length; i++)
					array.SetValue(Convert.ChangeType(values[i], elementType), i);
				return array;
			}

			// List<T> / IList<T> / ICollection<T> etc.
			if (targetType.IsGenericType)
			{
				var gen = targetType.GetGenericTypeDefinition();
				var args = targetType.GetGenericArguments();
				if (args.Length == 1 && (gen == typeof(List<>) || gen == typeof(IList<>) || gen == typeof(ICollection<>) || gen == typeof(IEnumerable<>)))
				{
					var elementType = args[0];
					var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
					foreach (var s in values)
						list.Add(Convert.ChangeType(s, elementType));
					if (gen == typeof(List<>))
						return list;
					// IList<>/ICollection<>/IEnumerable<>: List<T> is assignable
					return list;
				}
			}

			// Fallback: single value from first element (e.g. nullable ref types)
			if (values.Length > 0)
				return Convert.ChangeType(values[0], targetType);
			return null;
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

	public interface IArgConverter
	{
		object? Convert(string[] args);
	}

	/// <summary>Built-in converters for option values. Use these names in OptionTargetAttribute(alias, converter: nameof(BuiltInConverters.Int32Array)) or the string "Int32Array", etc.</summary>
	public static class BuiltInConverters
	{
		/// <summary>Return as-is; conversion is done by field type in DataAccessor.</summary>
		public static string[] Auto(string[] args) => args;

		public static string File(string[] args) => Path.GetFullPath(args[0]);
		public static string[] Files(string[] args) => args.Select(Path.GetFullPath).ToArray();
		public static string? FileVerifyPath(string[] args) => args.Length != 1
			? null
			: !System.IO.File.Exists(args[0])
			? throw new ArgumentParsingException(ArgumentParsingExceptionType.FileDoesNotExist, args[0])
			: Path.GetFullPath(args[0]);
		public static string[] FilesVerifyPaths(string[] args) => args.Any(path => !System.IO.File.Exists(path))
			? throw new ArgumentParsingException(ArgumentParsingExceptionType.FileDoesNotExist, args.First(path => !System.IO.File.Exists(path)))
			: args.Select(Path.GetFullPath).ToArray();

		public static int[] Int32Array(string[] args) => args.Select(int.Parse).ToArray();
		public static long[] Int64Array(string[] args) => args.Select(long.Parse).ToArray();
		public static double[] DoubleArray(string[] args) => args.Select(double.Parse).ToArray();
		public static string[] StringArray(string[] args) => args;

		public static int? FirstInt32(string[] args) => args.Length == 0 ? null : int.Parse(args[0]);
		public static long? FirstInt64(string[] args) => args.Length == 0 ? null : long.Parse(args[0]);
		public static double? FirstDouble(string[] args) => args.Length == 0 ? null : double.Parse(args[0]);
		public static string? FirstString(string[] args) => args.Length == 0 ? null : args[0];
		public static bool? FirstBool(string[] args) => args.Length == 0 ? null : bool.Parse(args[0]);
	}

	public abstract class Args<OptionEnum, CommandEnum>
		where OptionEnum : Enum
		where CommandEnum : Enum
	{
		public CommandEnum? Command;
		public List<OptionEnum> UsedOptions { get; private set; } = [];

		public NuArgsExtraAttribute<CommandEnum>? _extraAttributes;
		private Dictionary<OptionEnum, OptionAttribute> _optionAttributes;
		private Dictionary<OptionEnum, List<(DataAccessor, OptionTargetAttribute<OptionEnum>)>> _aliasAttributes;
		private Dictionary<CommandEnum, CommandAttribute<OptionEnum>> _commandAttributes;
		private Dictionary<string, CommandEnum> _commandNames;
		private Dictionary<string, OptionEnum> _optionNames;

		public void PrintHelp(CommandEnum command = default)
		{
			//TODO dynamically generate help text for specific options/actions using reflection
			if (EqualityComparer<CommandEnum>.Default.Equals(command, default))
			{
				if (_extraAttributes?.AboutText is not null)
				{
					Console.WriteLine("ABOUT:");
					Console.WriteLine("\t" + _extraAttributes.AboutText);
					Console.WriteLine();
				}
				Console.WriteLine($"USAGE:\n\t{GetType().Assembly.GetName().Name} <COMMAND> [OPTIONS...]");
				Console.WriteLine("\nCOMMANDS:");
				Console.WriteLine("\thelp: Print this help message or the help of a specific command.");
				Console.WriteLine("\tversion: Print the version of the program.");
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
					for (int i = 0; i < _extraAttributes.SectionHelpTexts.Length; i++)
					{
						Console.WriteLine($"\n{_extraAttributes.SectionHeaders[i].ToUpper()}:");
						Console.WriteLine($"\t{_extraAttributes.SectionHelpTexts[i].Replace("\n", "\n\t")}");
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
			_optionNames = _optionAttributes.ToDictionary(kvp => kvp.Value.OptionNames.First(), kvp => kvp.Key);
			_aliasAttributes = new Dictionary<OptionEnum, List<(DataAccessor, OptionTargetAttribute<OptionEnum>)>>();

			foreach (var field in GetType().GetFields())
			{
				var attribute = field.GetCustomAttribute<OptionTargetAttribute<OptionEnum>>();
				var accessor = new DataAccessor(field);
				if (attribute is not null)
				{
					attribute.Accessor = accessor;
					if (!_aliasAttributes.TryGetValue(attribute.Alias, out var list))
						_aliasAttributes.Add(attribute.Alias, [(accessor, attribute)]);
					else
						list.Add((accessor, attribute));
				}
			}
			foreach (var prop in GetType().GetProperties())
			{
				var attribute = prop.GetCustomAttribute<OptionTargetAttribute<OptionEnum>>();
				var accessor = new DataAccessor(prop);
				if (attribute is not null)
				{
					attribute.Accessor = accessor;
					if (!_aliasAttributes.TryGetValue(attribute.Alias, out var list))
						_aliasAttributes.Add(attribute.Alias, [(accessor, attribute)]);
					else
						list.Add((accessor, attribute));
				}
			}
		}

		public OptionEnum? WhichOption(string arg)
		{
			return _optionNames.GetValueOrDefault(arg, default(OptionEnum));
		}
		
		private void GiveValueTo(OptionEnum option, string[] value)
		{
			var pairs = _aliasAttributes[option];
			var attr = _optionAttributes[option];

			switch (attr.Kind)
			{
				case OptionType.SingleValue:
				{
					if (value.Length == 0)
						throw new ArgumentParsingException(ArgumentParsingExceptionType.NoValueGivenToOption, attr.OptionNames.First());
					foreach (var (accessor, aliasAttr) in pairs)
					{
						var converted = ResolveValue(option, value, aliasAttr);
						accessor.SetValue(this, converted, converted is null || converted.GetType() == typeof(string[]));
					}
					break;
				}
				case OptionType.MultipleValues:
				{
					if (value.Length == 0)
						throw new ArgumentParsingException(ArgumentParsingExceptionType.NoValueGivenToOption, attr.OptionNames.First());
					foreach (var (accessor, aliasAttr) in pairs)
					{
						var converted = ResolveValue(option, value, aliasAttr);
						accessor.SetValue(this, converted, converted is null || converted.GetType() == typeof(string[]));
					}
					break;
				}
				case OptionType.Flag:
				{
					foreach (var (accessor, _) in pairs)
						accessor.SetValue(this, true);
					break;
				}
			}

			UsedOptions.Add(option);
		}

		private object? ResolveValue(OptionEnum option, string[] value, OptionTargetAttribute<OptionEnum> aliasAttr)
		{
			var name = aliasAttr.Converter;
			if (string.IsNullOrEmpty(name))
				return value;

			// Explicit built-in converter
			var builtIn = typeof(BuiltInConverters).GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, [typeof(string[])], null);
			if (builtIn is not null)
				return builtIn.Invoke(null, [value]);

			// Custom method on the derived class: object? Method(string[] args) or T Method(string[] args)
			var custom = GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, null, [typeof(string[])], null);
			if (custom is not null)
				return custom.Invoke(custom.IsStatic ? null : this, [value]);

			throw new ArgumentParsingException(ArgumentParsingExceptionType.UnknownConverter, name);
		}
		
		private static string[]? ParseOptionUnixStyle(string arg)
		{
			if (arg[0].Equals('-'))
			{
				if (arg[1].Equals('-'))
					return [arg[2..]];
				return arg[1..].Split();
			}
			return null;
		}

		private static string[]? ParseOption(string arg)
		{
			if (arg[0].Equals('-'))
			{
				return [arg[1..]];
			}
			return null;
		}

		private void SetDefaultValues()
		{
			foreach (var alias in _aliasAttributes)
			{
				foreach (var (accessor, _) in alias.Value)
				{
					if (!UsedOptions.Contains(alias.Key) 
						&& _optionAttributes[alias.Key].DefaultValue is not null 
						&& accessor.GetValue(this) is null)
					{
						accessor.SetValue(this, _optionAttributes[alias.Key].DefaultValue, false);
					}
				}
			}
		}

		public void ParseArgs(string[] args)
		{
			if (args.Length == 0)
			{
				PrintHelp();
				return;
			}

			// predefined commands
			if (args[0].Equals("help"))
			{
				if (args.Length > 1 && _commandNames.TryGetValue(args[1], out var nextCommand))
					PrintHelp(nextCommand);
				else
					PrintHelp();
				return;
			}
			if (args[0].Equals("version"))
			{
				Console.WriteLine($"{GetType().Assembly.GetName().Name}: {GetType().Assembly.GetName().Version}v");
				return;
			}

			// get default command if there is no command given
			var givenCommand = _commandNames.TryGetValue(args[0], out Command);
			if (!givenCommand)
			{
				if (_extraAttributes is null)
					throw new ArgumentParsingException(ArgumentParsingExceptionType.NoCommandGiven, "");
				Command = _extraAttributes.DefaultCommand
					?? throw new ArgumentParsingException(ArgumentParsingExceptionType.NoDefaultCommandSet, "");
			}

			// parse options
			List<string> positionalArguments = [];
			for (int i = 1; i < args.Length; i++)
			{		
				var optName = (_extraAttributes?.UnixStyle == true 
					? ParseOptionUnixStyle(args[i])
					: ParseOption(args[i]));
				if (optName is null)
				{
					positionalArguments.Add(args[i]);
					continue;
				}
                var finalName = optName.Last();

				if (optName.Length > 1)
				{
					for (var ii = 0; ii < optName.Length; ++ii)
					{
						var thisFlag = WhichOption(optName[ii]);
						if (thisFlag is null || !_optionAttributes.TryGetValue(thisFlag, out var attr))
							throw new ArgumentParsingException(ArgumentParsingExceptionType.UnknownOption, optName[ii]);

						if (attr.Kind == OptionType.Flag)
						{
							GiveValueTo(thisFlag, []);
							continue;
						}
						else
						{
							if (ii == optName.Length-1)
								break;
							else
								throw new ArgumentParsingException(ArgumentParsingExceptionType.NoValueGivenToOption, optName[ii]);
						}
					}
				}

				var current = WhichOption(finalName);
				if (current is null || current.Equals(default(OptionEnum)))
					throw new ArgumentParsingException(ArgumentParsingExceptionType.UnknownCommand, finalName);	

				var currentAttribute = _optionAttributes[current];
				var next = i+1 < args.Length ? WhichOption(args[i+1]) : default;
				_optionAttributes.TryGetValue(next, out var nextAttribute);

				if (UsedOptions.Contains(current))
					throw new ArgumentParsingException(ArgumentParsingExceptionType.DuplicateOption, currentAttribute.OptionNames.First());

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
						--i; // for loop will increment i again
						continue;
					}
					case OptionType.SingleValue:
					{
						if (nextAttribute is not null)
							throw new ArgumentParsingException(ArgumentParsingExceptionType.NoValueGivenToOption, currentAttribute.OptionNames.First());
						GiveValueTo(current, [args[++i]]);
						continue;
					}
					case OptionType.Flag:
					{
						GiveValueTo(current, []);
						continue;
					}
				}
			}

			// set positional arguments
			var requiredOptions = _commandAttributes[Command].Required;
			if (requiredOptions is not null)
			{
				foreach (var option in requiredOptions)
				{
					if (UsedOptions.Contains(option))
						continue;

					if (positionalArguments.Count == 0)
						throw new ArgumentParsingException(ArgumentParsingExceptionType.NoValueGivenToOption, option.ToString());

					if (_optionAttributes[option].Kind == OptionType.MultipleValues)
					{
						GiveValueTo(option, positionalArguments.ToArray());
						positionalArguments.Clear();
						break;
					}
					else
					{
						GiveValueTo(option, [positionalArguments.First()]);
						positionalArguments.RemoveAt(0);
					}
				}
			}
			if (positionalArguments.Count > 0)
				throw new ArgumentParsingException(ArgumentParsingExceptionType.TooManyPositionalArguments);

			SetDefaultValues();

			return;
		}

		public void ParseArgsOrExit(string[] args, int exitCodeOnError = 1)
		{
			try 
			{
				ParseArgs(args);
			}
			catch (ArgumentParsingException e)
			{
				Console.Error.WriteLine(e.Message);
				Environment.Exit(exitCodeOnError);
			}
		}

		public Args()
		{
			Initialize();
		}
	}
}