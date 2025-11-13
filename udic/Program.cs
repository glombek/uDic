using Spectre.Console.Cli;
using System.ComponentModel;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<MoveCommand>("move")
    .WithDescription("Moves dictionary items matching the specified name to a new name. * matches all child paths.")
    .WithExample("move Test.Old.Name*", "New.Name");

    config.AddCommand<CopyCommand>("copy")
    .WithDescription("Copies dictionary items matching the specified name to a new name. * matches all child paths.")
    .WithExample("copy Test.Old.Name*", "New.Name");

    config.AddCommand<TreeCommand>("tree")
    .WithDescription("Ensures parent dictionary items exist for every dot-separated segment in aliases.");

    config.AddBranch("add", add =>
    {
        add.AddCommand<AddItemCommand>("item")
            .WithDescription("Adds a new dictionary item with the specified name.")
            .WithExample("add item New.Alias \"Value goes here\" -c en-GB");
        //add.AddCommand<AddFileCommand>("file")
        //.WithDescription("Adds dictionary items from the specified file.");
    });

    config.AddCommand<ShimCommand>("shim")
        .WithDescription("Sets the value of the specified culture for specified name(s) to the given format.")
        .WithExample("shim \"[{culture}] {value:en-GB} ({alias})\" -n * -c da");
});

return app.Run(args);
