using System.CommandLine;
using Shouldly;
using Xunit;

namespace Likvido.Kubesec.Tests;

public class CommandFactoryTests
{
    [Fact]
    public void Parse_WhenPullIsGivenNoOptions_ShouldYieldAnEmptyListForRemoveJsonField()
    {
        // Arrange
        var rootCommand = CommandFactory.CreateRootCommand();

        // Act
        var parseResult = rootCommand.Parse("pull my-secret");

        // Assert
        // This pins the System.CommandLine behaviour that regressed in the 2.0.0-beta3 -> 2.0.2 upgrade:
        // an unsupplied collection option comes back empty, never null
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<List<string>>("--remove-json-field").ShouldBeEmpty();
    }

    [Fact]
    public void Parse_WhenPullIsGivenRemoveJsonField_ShouldYieldTheSuppliedFields()
    {
        // Arrange
        var rootCommand = CommandFactory.CreateRootCommand();

        // Act
        var parseResult = rootCommand.Parse("pull my-secret --unwrap-key SOME_KEY --remove-json-field a.b --remove-json-field c");

        // Assert
        parseResult.Errors.ShouldBeEmpty();
        parseResult.GetValue<List<string>>("--remove-json-field").ShouldBe(["a.b", "c"]);
    }

    [Fact]
    public void CreateRootCommand_ShouldContainAllCommands()
    {
        // Act
        var rootCommand = CommandFactory.CreateRootCommand();

        // Assert
        rootCommand.Subcommands.Select(x => x.Name)
            .ShouldBe(["pull", "push", "backup", "restore", "find", "update-value"], ignoreOrder: true);
    }
}
