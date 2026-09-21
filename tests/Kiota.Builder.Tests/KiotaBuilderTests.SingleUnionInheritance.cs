using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData("oneOf", false)]
    [InlineData("anyOf", false)]
    [InlineData("oneOf", true)]
    [InlineData("anyOf", true)]
    public async Task PreservesSingleUnionReferencedInheritanceWithoutOwnPropertiesAsync(string union, bool hasDirectProperties)
    {
        var description = """
        {"openapi":"3.0.3","info":{"title":"Single union inheritance","version":"1"},
        "paths":{"/test":{"get":{"responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Wrapper"}}}}}}}},
        "components":{"schemas":{
          "Wrapper":{"type":"object","UNION":[{"$ref":"#/components/schemas/Child"}],"discriminator":{"propertyName":"kind"}},
          "Common":{"type":"object","properties":{"kind":{"type":"string"},"common":{"type":"string"}}},
          "Child":{"type":"object",DIRECT"allOf":[{"$ref":"#/components/schemas/Common"},{"type":"object","properties":{"one":{"type":"string"}}}]}
        }}}
        """.Replace("UNION", union).Replace("DIRECT", hasDirectProperties ? "\"properties\":{\"extra\":{\"type\":\"string\"}}," : "");
        await using var stream = await GetDocumentStreamAsync(description);
        var builder = new KiotaBuilder(NullLogger<KiotaBuilder>.Instance, new GenerationConfiguration(), _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var model = builder.CreateSourceModel(builder.CreateUriSpace(document));
        var wrapper = model.FindChildByName<CodeClass>("Wrapper");
        Assert.NotNull(wrapper);
        Assert.Equal("Common", wrapper.BaseClass?.Name);
        Assert.NotNull(wrapper.FindChildByName<CodeProperty>("one", false));
        Assert.Equal(hasDirectProperties, wrapper.FindChildByName<CodeProperty>("extra", false) is not null);
        Assert.NotNull(wrapper.BaseClass.FindChildByName<CodeProperty>("common", false));
    }
}
