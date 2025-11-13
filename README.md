# uDic - Umbraco (uSync) Dictionary CLI tool

Mess with Umbraco translation dictionaries by manipulating uSync files.

uDic assumes (and enforces) a hierarchical dictionary setup delimited by dots (`.`)

For example:

 - Login
   - Login.Username
   - Login.Password
 - Register
   - Register.Username
   - Register.Email
     - Register.Email.Example
   - Register.Password



> [!CAUTION]
> This is very flaky. Ensure you have backed up uSync before running your commands and that you can restore a database before importing changes.

> [!WARNING]
> There is no file-naming logic, its therefore very important you run a clean export after importing modified files.

## Installation

This is a dotnet tool.

You can either install it globally like so:

`dotnet tool install -g udic`

Or locally to your Umbraco project:

`dotnet tool install udic`

You can also execute it without installing by using:

`dnx udic <commands>`

### Local development

Build the tool with the `dotnet pack` command.

Then you can install it globally by specifying the path:

```cmd
dotnet tool install -g udic --source /path/to/udic/source/npkg/.
```



You can also run the tool with `dotnet run {command} -- {arguments}`

e.g. `dotnet run add item New.Alias "Value goes here" -- -c en-GB`

## Commands

Run `udic --help` or `udic <command> --help` for in-app guidance.

- `tree`
  Enforces a tree structure in the dictionary with `.` as the separator. It will create any missing parent nodes.
  e.g. if `Login` and `Login.Username` were both in the root, `Login.Username` would be moved to nest under `Login`.
  
- `add item`

  Adds a new key in the correct place in the dictionary, passing an optional value and culture to populate.
  e.g. `udic add item New.Alias "Value goes here" -c en-GB` to add a new key in the correct place in the dictionary, passing an optional value and culture to populate.
  Options:

  - `-c`|`--culture` - The culture to add, defaults to your current language

- `copy`
  Copies dictionary items/a whole tree into a new location. Pass the `--empty` flag to clear existing translations when copying
  e.g. `udic copy Login* Register --empty` will copy `Login`, `Login.Username` and `Login.Password` to `Register`, `Register.Username` and `Register.Password` 
  
- `move`
  Moves (or renames) dictionary items/a whole tree.
  e.g. `udic move Login* MembersArea.Login` will move `Login`, `Login.Username` and `Login.Password` to `MembersArea.Login`, `MembersArea.Login.Username` and `MembersArea.Login.Password`
  
- `seed`
  Populates the specified culture with dummy data based on a format string to test internationalization logic without real translation data.
  e.g `udic seed "[{culture}] {value:en-GB} ({alias})" -n * -c da` would populate all (`*` matches all) values in the `da` culture with a calculated value such as `[da] Username (Login.Username)`

Yet to implement:

- `add many <filePath> -c en-GB` which accepts a JSON file of dictionary items to add, with their value. The culture to populate is passed as an argument.

### Global Arguments

You can always pass the following arguments:

- `-p` or `--project`
  A path to the Umbraco project if not the current directory
