# NuArgs

A lightweight, attribute-based command-line argument parser for C#. Define options and commands with enums and attributes; NuArgs uses reflection to parse `string[]` args and populate your types.

## Features

- **Attribute-driven** — Use `[Option]` and `[Command]` on enum members, and `[OptionTarget]` on fields/properties to bind options to your class.
- **Generic design** — Subclass `Args<OptionEnum, CommandEnum>` so options and commands are type-safe enums.
- **Option kinds** — Flag (boolean), single value, or multiple values per option.
- **Commands** — First positional argument selects a command; each command can declare required options that are filled positionally.
- **Converters** — Built-in converters (file paths, arrays of int/long/double, etc.) and custom methods by name.
- **Help** — Built-in `PrintHelp()`; optional about text and extra sections via `[NuArgsExtra]`.
- **Unix-style short options** — With `unixStyle: true`, `-abc` is treated as three flags (`-a` `-b` `-c`).

## Requirements

- .NET 10+
- C# 12

## Option types

| Kind            | Description |
|-----------------|-------------|
| `None`          | Sentinel value; must be the first member (e.g. `None = 0`) in both option and command enums. |
| `Flag`          | Boolean; no value (e.g. `-c` or `--verbose`). |
| `SingleValue`   | Exactly one value (e.g. `-a 42` or `--file path`). |
| `MultipleValues`| One or more values; when used in a command’s `Required` list, it must be last. |

Option names in attributes are given **without** leading dashes; the parser accepts both short (`-x`) and long (`--name`) form by prefixing the name.

## Attributes

### `[Option]`

Defines an option. Use on **enum fields** of your option enum.

- **Overloads:** `OptionAttribute(string name, OptionType kind, string helpText = "...", object? defaultValue = null)` and `OptionAttribute(string[] name, OptionType kind, string helpText = "...", object? defaultValue = null)` for multiple names (e.g. `"f"` and `"file"`).
- **OptionNames** — One or more names (e.g. `"a"`, `"file"`). Users pass `-a`, `--a`, `--file`, etc.
- **Kind** — `OptionType`: `Flag`, `SingleValue`, or `MultipleValues`.
- **HelpText** — Shown in generated help.
- **DefaultValue** — Optional; applied when the option is not supplied (and shown in help when set on the attribute).

### `[Command<OptionEnum>]`

Defines a command (first positional argument). Use on **enum fields** of your command enum.

- **Constructor:** `CommandAttribute(string name, string helpText = "No help available.", params OptionEnum[] required)`.
- **ActionName** — Command name (e.g. `"run"`, `"build"`). `"help"` and `"version"` are reserved and will throw at startup.
- **HelpText** — Shown in help.
- **Required** — Optional list of options that must be provided for this command; they are filled from positional arguments in order. Any `MultipleValues` option must be last in `Required`.

### `[OptionTarget<OptionEnum>]`

Maps an option to a field or property on your `Args` subclass. Use on **fields and properties** of the parser class.

- **Constructor:** `OptionTargetAttribute(OptionEnum alias, string? converter = null)`.
- **Alias** — The option enum value this member receives.
- **Converter** — Optional. Either a method name from `BuiltInConverters` (e.g. `nameof(BuiltInConverters.Int32Array)`) or the name of a static/instance method on your class with signature `object? Method(string[] args)`. Omit for automatic conversion based on the member type (e.g. `string[]` → `int?`, `int[]`, `List<int>`, etc.).

### `[NuArgsExtra<CommandEnum>]`

Optional. Applied to your **parser class** to configure global behavior and help.

- **defaultCommand** — Command used when the user does not pass a command name (e.g. when using a default subcommand).
- **unixStyle** — If `true`, short options can be grouped (e.g. `-abc` → `-a` `-b` `-c`).
- **aboutText** — Printed at the top of help.
- **sectionHeaders** / **sectionHelpTexts** — Parallel arrays for extra help sections; lengths must match.
- **customOutputType** — Reserved for future use.

## Built-in converters

Use the **method name** in `OptionTarget(..., nameof(BuiltInConverters.X))` or the string `"X"`:

| Name | Description |
|------|-------------|
| `Auto` | Pass-through; conversion is done from `string[]` by the target member type. |
| `File` | Single path → full path. |
| `Files` | Multiple paths → full paths. |
| `FileVerifyPath` | Single path → full path; throws if file does not exist. |
| `FilesVerifyPaths` | Multiple paths → full paths; throws if any file does not exist. |
| `Int32Array`, `Int64Array`, `DoubleArray`, `StringArray` | Parse each value to the corresponding type. |
| `FirstInt32`, `FirstInt64`, `FirstDouble`, `FirstString`, `FirstBool` | First value only; nullable; empty → `null`. |

Custom converters: add a method on your parser class with signature `object? YourMethod(string[] args)` (static or instance) and pass its name as the converter. For `SingleValue` options the array has length 1.

## Exceptions

**`ArgumentParsingException`** — Thrown for parsing errors. Properties:

- **Type** — `ArgumentParsingExceptionType` (e.g. `UnknownOption`, `NoValueGivenToOption`, `TooManyPositionalArguments`).
- **OptionName** — Option or command name involved, when applicable.
- **GivenValue** — Value that was invalid, when applicable.

Constructors:

- `ArgumentParsingException(string message)` — Custom message; `Type` is `CustomMessage`.
- `ArgumentParsingException(ArgumentParsingExceptionType type)`
- `ArgumentParsingException(ArgumentParsingExceptionType type, string optionName)`
- `ArgumentParsingException(ArgumentParsingExceptionType type, string optionName, string givenValue)`

There is no separate “not enough arguments” exception; those cases use specific types such as `NoValueGivenToOption` or `TooManyPositionalArguments`.

## Usage outline

1. **Define two enums** — One for options, one for commands. Each must have a first member `None = 0` (used internally and skipped in option/command lists).
2. **Option enum** — Decorate each option with `[Option(nameOrNames, OptionType.XXX, helpText, defaultValue)]`.
3. **Command enum** — Decorate each command with `[Command<OptionEnum>("name", helpText, requiredOption1, ...)]`.
4. **Parser class** — Create a class that inherits `Args<OptionEnum, CommandEnum>`. Add `[NuArgsExtra<CommandEnum>(...)]` if you want a default command, unix-style options, or extra help sections.
5. **Bind options** — On fields and properties, use `[OptionTarget<OptionEnum>(MyOption.SomeOption)]` or `[OptionTarget<OptionEnum>(MyOption.SomeOption, nameof(BuiltInConverters.Int32Array))]` (or a custom method name).
6. **Parse** — Instantiate your parser and call `ParseArgs(args)` or `ParseArgsOrExit(args, exitCode)`.
7. **Use result** — Read the `Command` property and your option-backed fields/properties after parsing. Empty args trigger `PrintHelp()` and return without setting a command.

Example: see `Example.cs` in the repository.

## Requirements summary

- Both enums: first member must be `None = 0`.
- Parser class must inherit `Args<OptionEnum, CommandEnum>` and apply attributes as above.
- Default command and unix-style parsing are configured on the **class** via `[NuArgsExtra]`, not on the constructor.
