# NuArgs

A lightweight, attribute-based command-line argument parser for C#. Define options and commands with enums and attributes; NuArgs uses reflection to parse `string[]` args and populate your types.

## Installation

```bash
dotnet add package NuArgs
```

Or from **Package Manager Console** in Visual Studio:

```powershell
Install-Package NuArgs
```

## Requirements

- .NET 10+
- C# 12

## Features

- **Attribute-driven** — Use `[Option]` and `[Command<OptionEnum>]` on enum members, and `[OptionTarget<OptionEnum>]` on fields/properties to bind options to your parser class.
- **Generic design** — Subclass `Args<OptionEnum, CommandEnum>` so options and commands are type-safe enums.
- **Option kinds** — Flag (boolean), single value, or multiple values per option.
- **Commands** — First positional argument selects a command; each command can declare required options that are filled from positional arguments in order.
- **Converters** — Built-in converters (file paths, arrays of int/long/double, etc.) and custom methods by name.
- **Help** — Built-in `PrintHelp()`; optional about text and extra sections via `[NuArgsExtra<CommandEnum>]`. Reserved commands `help` and `version` print help or assembly version and return without throwing.
- **Unix-style short options** — With `unixStyle: true`, `-abc` is treated as three flags (`-a` `-b` `-c`).

## Option types

| Kind             | Description |
|------------------|-------------|
| `None`           | Sentinel; must be the first member (e.g. `None = 0`) in both option and command enums. |
| `Flag`           | Boolean; no value (e.g. `-c` or `--verbose`). |
| `SingleValue`    | Exactly one value (e.g. `-a 42` or `--file path`). |
| `MultipleValues` | One or more values; when used in a command’s `Required` list, it must be last. |

Option names in attributes are given **without** leading dashes; the parser accepts `-name` and `--name` (single-character options as `-x`, longer as `--name`).

## Attributes

### `[Option]`

Defines an option. Use on **enum fields** of your option enum.

- **OptionNames** — One name (`string`) or multiple (`string[]`). Users pass `-a`, `--file`, etc.
- **Kind** — `OptionType`: `Flag`, `SingleValue`, or `MultipleValues`.
- **HelpText** — Shown in generated help (default: `"No help available."`).
- **DefaultValue** — Optional; applied when the option is not supplied. To show in help, set it on the attribute; setting it on the field/property alone does not show in help.

Constructors: `OptionAttribute(string name, OptionType kind, string helpText = ..., object? defaultValue = null)` and `OptionAttribute(string[] name, OptionType kind, string helpText = ..., object? defaultValue = null)`.

### `[Command<OptionEnum>]`

Defines a command (first positional argument). Use on **enum fields** of your command enum.

- **ActionName** — Command name (e.g. `"run"`, `"build"`). `"help"` and `"version"` are reserved and will throw `ArgumentParsingException(ReservedCommandName)` if used as a command name.
- **HelpText** — Shown in help.
- **Required** — Optional `params OptionEnum[]` of options that must be provided for this command; filled from positional arguments in order. Any `MultipleValues` option must be last in `Required`.

### `[OptionTarget<OptionEnum>]`

Maps an option to a field or property on your `Args` subclass. Use on **fields and properties** of the parser class.

- **Alias** — The option enum value this member receives.
- **Converter** — Optional. Either a method name from `BuiltInConverters` (e.g. `nameof(BuiltInConverters.Int32Array)`) or the name of a static/instance method on your class with signature `object? Method(string[] args)`. Omit for automatic conversion based on the member type (e.g. `string[]` → `int?`, `int[]`, `List<int>`, etc.). For `SingleValue` options the converter still receives a `string[]` of length 1.

### `[NuArgsExtra<CommandEnum>]`

Optional. Applied to your **parser class** to configure global behavior and help.

- **DefaultCommand** — Command used when the user does not pass a command name.
- **UnixStyle** — If `true`, short options can be grouped (e.g. `-abc` → `-a` `-b` `-c`).
- **SectionHelpTexts** / **SectionHeaders** — Parallel arrays for extra help sections; lengths must match.
- **AboutText** — Printed at the top of help.
- **AllowNoCommand** — When `true`, allows invocation with no command and no default (e.g. for help-only or custom handling).
- **CustomOutputType** — Reserved for future use.

Constructor parameter order: `defaultCommand`, `unixStyle`, `sectionHelpTexts`, `sectionHeaders`, `aboutText`, `allowNoCommand`, `customOutputType`. All except the first have defaults.

## Built-in converters

Use the **method name** in `OptionTarget(..., nameof(BuiltInConverters.X))` or the string `"X"`:

| Name               | Description |
|--------------------|-------------|
| `Auto`             | Pass-through; conversion is done from `string[]` by the target member type. |
| `File`             | Single path → full path. |
| `Files`            | Multiple paths → full paths. |
| `FileVerifyPath`   | Single path → full path; throws if file does not exist. |
| `FilesVerifyPaths` | Multiple paths → full paths; throws if any file does not exist. |
| `Int32Array`, `Int64Array`, `DoubleArray`, `StringArray` | Parse each value to the corresponding type. |
| `FirstInt32`, `FirstInt64`, `FirstDouble`, `FirstString`, `FirstBool` | First value only; nullable; empty → `null`. |

Custom converters: add a method on your parser class with signature `object? YourMethod(string[] args)` (static or instance) and pass its name as the converter.

## Exceptions

Parsing throws **`ArgumentParsingException`**. It exposes:

- **Type** — `ArgumentParsingExceptionType`.
- **OptionName** — The option or command name involved, when applicable.
- **GivenValue** — The invalid value, when applicable.

**Exception types:** `NonExistentOption`, `InvalidOptionValue`, `DuplicateOption`, `NoCommandGiven`, `NoDefaultCommandSet`, `NoValueGivenToOption`, `FileDoesNotExist`, `UnknownConverter`, `UnknownOption`, `UnknownCommand`, `UnknownOptionValue`, `UnknownCommandValue`, `TooManyPositionalArguments`, `ReservedCommandName`, `MultipleValuesOptionNotAtEnd`, `CustomMessage`.

Use **`ParseArgsOrExit(args, exitCode)`** to write the exception message to stderr and exit instead of throwing.

## Usage outline

1. **Define two enums** — One for options, one for commands. Each must have a first member `None = 0` (used internally and skipped in option/command lists).
2. **Option enum** — Decorate each option with `[Option(nameOrNames, OptionType.XXX, helpText, defaultValue)]`.
3. **Command enum** — Decorate each command with `[Command<OptionEnum>("name", helpText, requiredOption1, ...)]`.
4. **Parser class** — Create a class that inherits `Args<OptionEnum, CommandEnum>`. Add `[NuArgsExtra<CommandEnum>(...)]` if you want a default command, unix-style options, or extra help sections.
5. **Bind options** — On fields and properties, use `[OptionTarget<OptionEnum>(MyOption.SomeOption)]` or with a converter: `[OptionTarget<OptionEnum>(MyOption.SomeOption, nameof(BuiltInConverters.Int32Array))]` (or a custom method name).
6. **Parse** — Instantiate your parser and call `ParseArgs(args)` or `ParseArgsOrExit(args, exitCode)`.
7. **Use result** — Read the `Command` property and your option-backed fields/properties after parsing. Empty args trigger `PrintHelp()` and return without setting a command.

See **`Example.cs`** in the repository for a full example.

## Minimal example

```csharp
using NuArgs;

internal enum MyOption
{
    None = 0,
    [Option("n", OptionType.SingleValue, "A number.")]
    Number,
}

internal enum MyCommand
{
    None = 0,
    [Command<MyOption>("run", "Run with a number.")]
    Run,
}

[NuArgsExtra<MyCommand>(defaultCommand: MyCommand.Run, aboutText: "Minimal NuArgs demo.")]
internal class MyArgs : Args<MyOption, MyCommand>
{
    [OptionTarget<MyOption>(MyOption.Number)]
    public int? Number { get; set; }
}

// In Main(string[] args):
var myArgs = new MyArgs();
myArgs.ParseArgsOrExit(args);
// Usage: myapp run -n 42   or   myapp -n 42   (default command)
// Then: myArgs.Command == MyCommand.Run, myArgs.Number == 42
```

## Summary

- Both enums: first member must be `None = 0`.
- Parser class must inherit `Args<OptionEnum, CommandEnum>` and apply attributes as above.
- Default command and unix-style parsing are configured on the **class** via `[NuArgsExtra<CommandEnum>]`, not on the constructor.
