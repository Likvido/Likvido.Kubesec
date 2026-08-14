using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Likvido.Kubesec.Tests;

public class RemoveJsonFieldsTests
{
    [Fact]
    public void RemoveJsonFields_WhenGivenANestedField_ShouldRemoveIt()
    {
        // Arrange
        var secret = new Secret("appsettings.json", """{"ConnectionStrings":{"Db":"secret","Cache":"keep"},"Other":1}""");

        // Act
        PullCommand.RemoveJsonFields(secret, ["ConnectionStrings.Db"]);

        // Assert
        var result = JObject.Parse(secret.Value);
        result.SelectToken("ConnectionStrings.Db").ShouldBeNull();
        result.SelectToken("ConnectionStrings.Cache")?.Value<string>().ShouldBe("keep");
        result.SelectToken("Other")?.Value<int>().ShouldBe(1);
    }

    [Fact]
    public void RemoveJsonFields_WhenGivenATopLevelField_ShouldRemoveIt()
    {
        // Arrange
        var secret = new Secret("appsettings.json", """{"Remove":"me","Keep":"me"}""");

        // Act
        PullCommand.RemoveJsonFields(secret, ["Remove"]);

        // Assert
        var result = JObject.Parse(secret.Value);
        result.SelectToken("Remove").ShouldBeNull();
        result.SelectToken("Keep")?.Value<string>().ShouldBe("me");
    }

    [Fact]
    public void RemoveJsonFields_WhenGivenAFieldThatDoesNotExist_ShouldLeaveTheValueIntact()
    {
        // Arrange
        var secret = new Secret("appsettings.json", """{"Keep":"me"}""");

        // Act
        PullCommand.RemoveJsonFields(secret, ["Does.Not.Exist"]);

        // Assert
        JObject.Parse(secret.Value).SelectToken("Keep")?.Value<string>().ShouldBe("me");
    }

    [Fact]
    public void RemoveJsonFields_WhenTheValueIsNotJson_ShouldThrowAReadableError()
    {
        // Arrange
        // The raw Newtonsoft error here is "Unexpected character encountered while parsing value: l",
        // which tells the user nothing about what they did wrong
        var secret = new Secret("AZURE_REGISTRY_NAME", "likvido");

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => PullCommand.RemoveJsonFields(secret, ["a.b"]));

        // Assert
        exception.Message.ShouldBe("The value of the key 'AZURE_REGISTRY_NAME' is not valid JSON, so the remove-json-field option cannot be used with it");
    }
}
