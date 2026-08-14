using Shouldly;
using Xunit;

namespace Likvido.Kubesec.Tests;

public class PullCommandValidateTests
{
    [Fact]
    public void Validate_WhenNoOptionsAreSupplied_ShouldReturnNull()
    {
        // Arrange
        // System.CommandLine hands us an empty list for an unsupplied --remove-json-field, not null.
        // Treating that as "supplied" is what broke every single pull in 2.0.0.
        var jsonFieldsToDelete = new List<string>();

        // Act
        var result = PullCommand.Validate(configurePortForwarding: false, unwrapKeyName: null, jsonFieldsToDelete);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Validate_WhenOnlyUnwrapKeyIsSupplied_ShouldReturnNull()
    {
        // Act
        var result = PullCommand.Validate(configurePortForwarding: false, unwrapKeyName: "SOME_KEY", new List<string>());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Validate_WhenJsonFieldsAreSuppliedWithoutUnwrapKey_ShouldReturnError()
    {
        // Act
        var result = PullCommand.Validate(configurePortForwarding: false, unwrapKeyName: null, new List<string> { "a.b" });

        // Assert
        result.ShouldBe("When using the remove-json-fields option, you also have to specify the unwrap-key option");
    }

    [Fact]
    public void Validate_WhenJsonFieldsAreSuppliedWithUnwrapKey_ShouldReturnNull()
    {
        // Act
        var result = PullCommand.Validate(configurePortForwarding: false, unwrapKeyName: "SOME_KEY", new List<string> { "a.b" });

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Validate_WhenPortForwardingIsSuppliedWithoutUnwrapKey_ShouldReturnError()
    {
        // Act
        var result = PullCommand.Validate(configurePortForwarding: true, unwrapKeyName: null, new List<string>());

        // Assert
        result.ShouldBe("When using the port-forward flag, you also have to specify the unwrap-key option");
    }

    [Fact]
    public void Validate_WhenPortForwardingIsSuppliedWithUnwrapKey_ShouldReturnNull()
    {
        // Act
        var result = PullCommand.Validate(configurePortForwarding: true, unwrapKeyName: "SOME_KEY", new List<string>());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Validate_WhenJsonFieldsAreNull_ShouldReturnNull()
    {
        // Act
        var result = PullCommand.Validate(configurePortForwarding: false, unwrapKeyName: null, jsonFieldsToDelete: null);

        // Assert
        result.ShouldBeNull();
    }
}
