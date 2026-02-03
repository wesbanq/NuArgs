# NuArgs

A lightweight, attribute-based command-line argument parser for C#. Define options and commands with enums and attributes; NuArgs uses reflection to parse `string[]` args and populate your types.

## Features

- **Attribute-driven** — Use `[Option]`, `[Command]`, and `[Alias]` on enums and fields/properties.
- **Generic design** — `NuArgs<OptionEnum, CommandEnum>` so options and commands are type-safe enums.
- **Option kinds** — Flags, single value, optional value, or multiple values per option.
- **Commands** — First positional argument can select a command; support for required options per command.
- **Help** — Built-in `PrintHelp()`; optional custom help text.

## Option types

| Kind            | Description                    |
|-----------------|--------------------------------|
| `Flag`          | Boolean; no value (e.g. `--verbose`). |
| `SingleValue`   | One value required (e.g. `--file path`). |
| `OptionalValue` | Value optional.                |
| `MultipleValues`| One or more values.            |

## Attributes

### `[Option]`

Marks an option. Use on **enum members** for the option definition.

- **OptionNames** — One or multiple names, e.g. `"-f"`, `"--file"`.
- **HelpText** — Shown in help.
- **Kind** — `OptionType` (default: `SingleValue`).
- **WhenPassed** — Optional `Action<string[]>` invoked when the option is seen.

### `[Command]`

Marks a command (first positional argument).

- **ActionName** — Command name (e.g. `"run"`, `"build"`). `"help"` and `"version"` are reserved and will throw.
- **HelpText** — Shown in help.
- **Required** — Optional `OptionEnum[]` of options required when this command is used.

### `[Alias]`

Maps one option to others (e.g. shared behavior or multiple names for the same logical option).

- **Aliases** — `params OptionEnum[]` of option enums that act as aliases.

## Exceptions

- **`NotEnoughArgmunetsException`** — Thrown when not enough arguments are passed.
- **`ArgumentParsingException`** — Thrown for invalid or missing option values. Exposes `OptionName` and `GivenValue`. Constructors:
  - No value: `ArgumentParsingException(string optName)`
  - Invalid value: `ArgumentParsingException(string optName, string givenValue)`
  - Custom message: `ArgumentParsingException(string customMessage, string optName, string givenValue)`

## Usage outline

1. Define two enums: one for options, one for commands.
2. Decorate option enum members with `[Option(...)]` (and optionally fields/properties with `[Alias(...)]`).
3. Decorate command enum members with `[Command(...)]`.
4. Construct your parser (e.g. `new MyArgs(unixStyle: true)`) and call `ParseArgs(args)`.
5. Read `Command` and your option fields/properties after parsing.

## Requirements

- .NET 10+
- C# 12
