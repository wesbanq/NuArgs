# NuArgs

A lightweight, attribute-based command-line argument parser for C#. Define options and commands with enums and attributes; NuArgs uses reflection to parse `string[]` args and populate your types.

## Building and installation

NuArgs is not published to NuGet. To use it, build from source and reference it from your project.

### Requirements

- .NET 10+
- C# 12

### Build from source

1. Clone the repository:
   ```powershell
   git clone https://github.com/wesbanq/NuArgs
   cd NuArgs
   ```

2. Restore and build:
   ```powershell
   dotnet restore
   dotnet build -c Release
   ```

### Use in your project

#### **Option A — Project reference (recommended)**  
In your application’s `.csproj`, add a reference to the NuArgs project:
```xml
<ItemGroup>
  <ProjectReference Include="path/to/NuArgs/NuArgs.csproj" />
</ItemGroup>
```

#### **Option B — Local NuGet package**  
1. From the NuArgs repo directory, create the package:
   ```powershell
   dotnet pack -c Release -o .\nupkgs
   ```
   Confirm the folder contains `NuArgs.1.0.0.nupkg` (e.g. `dir nupkgs` on Windows, `ls nupkgs` on macOS/Linux).

2. From your **application** directory, add the package by pointing at the nupkgs folder. Use the **full path** to the `nupkgs` folder (relative paths often cause “no versions available”):
   ```powershell
   dotnet add package NuArgs --source "C:\path\to\NuArgs\nupkgs"
   ```
   On Windows use a path like `C:\Users\You\repo\NuArgs\nupkgs`. On macOS/Linux use e.g. `/home/you/repo/NuArgs/nupkgs`.

   **Alternatively**, register the folder as a NuGet source once (again use the full path), then add the package by source name:
   ```powershell
   dotnet nuget add source "C:\path\to\NuArgs\nupkgs" --name NuArgsLocal
   dotnet add package NuArgs --source NuArgsLocal
   ```

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

## Usage outline

1. Create two enums, one for the options the program has, and one for the commands the program has. 
**Make sure to add a None = 0 value for both enums.**
```csharp
public enum MyOptions
{
    None = 0,
    Option1,
    Option2,
}
public enum MyCommands
{
    None = 0,
    Command1,
    Command2,
}
```
2. Create a parser class that inherits `Args<OptionEnum, CommandEnum>` and declare fields or properties for each option you want to bind (types can be `string`, `int?`, `string[]`, etc.).
```csharp
public class MyArgs : Args<MyOptions, MyCommands>
{
    // OptionTarget attributes added in step 4
    public string? Option1Value;
    public string[]? Option2Values;
}
```

3. Annotate the **option enum** with `[Option(...)]` and the **command enum** with `[Command<OptionEnum>(...)]`.
```csharp
public enum MyOptions
{
    None = 0,
    [Option("a", OptionType.SingleValue, "First option.")]
    Option1,
    [Option(["b", "other-option"], OptionType.MultipleValues, "Second option.")]
    Option2,
}

public enum MyCommands
{
    None = 0,
    [Command<MyOptions>("cmd1", "First command.")]
    Command1,
    [Command<MyOptions>("cmd2", "Second command.")]
    Command2,
}
```

4. On the parser class, add `[OptionTarget<OptionEnum>(OptionEnum.Value)]` to each field or property that should receive an option. Optionally add `[NuArgsExtra<CommandEnum>(...)]` on the class for a default command, about text, or unix-style options.
```csharp
[NuArgsExtra<MyCommands>(defaultCommand: MyCommands.Command1, aboutText: "My app.")]
public class MyArgs : Args<MyOptions, MyCommands>
{
    [OptionTarget<MyOptions>(MyOptions.Option1)]
    public string? Option1Value { get; set; }

    [OptionTarget<MyOptions>(MyOptions.Option2)]
    public string[]? Option2Values { get; set; }
}
```

5. In your entry point, instantiate the parser, call `ParseArgs(args)` (or `ParseArgsOrExit(args)` to print errors and exit), then read `Command` and your option properties.
```csharp
var myArgs = new MyArgs();
myArgs.ParseArgsOrExit(args);  // or ParseArgs(args) and catch ArgumentParsingException

if (myArgs.Command == MyCommands.Command1)
    Console.WriteLine(myArgs.Option1Value);
```

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

## Built-in converters

Use the **method name** in `OptionTarget(..., nameof(BuiltInConverters.X))`.

| Name               | Description |
|--------------------|-------------|
| `Auto (default)`             | Pass-through; conversion is done from `string[]` by the target member type. |
| `File`             | Single path → full path. |
| `Files`            | Multiple paths → full paths. |
| `FileVerifyPath`   | Single path → full path; throws if file does not exist. |
| `FilesVerifyPaths` | Multiple paths → full paths; throws if any file does not exist. |
| `Int32Array`, `Int64Array`, `DoubleArray`, `StringArray` | Parse each value to the corresponding type. |
| `FirstInt32`, `FirstInt64`, `FirstDouble`, `FirstString`, `FirstBool` | First value only; nullable; empty → `null`. |

Custom converters: add a method on your parser class with signature `object? YourMethod(string[] args)` (static or instance) and pass its name as the converter.