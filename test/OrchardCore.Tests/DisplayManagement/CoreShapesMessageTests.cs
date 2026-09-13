using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.DisplayManagement.Shapes;

namespace OrchardCore.Tests.DisplayManagement;

public class CoreShapesMessageTests
{
    [Theory]
    [InlineData(NotifyType.Success, "status")]
    [InlineData(NotifyType.Information, "status")]
    [InlineData(NotifyType.Warning, "alert")]
    [InlineData(NotifyType.Error, "alert")]
    public void MessageShouldRenderRoleBySeverity(NotifyType type, string expectedRole)
    {
        var html = RenderMessage(type, "Hello");

        Assert.Contains($"role=\"{expectedRole}\"", html);
    }

    [Theory]
    [InlineData(NotifyType.Success)]
    [InlineData(NotifyType.Error)]
    public void MessageShouldKeepExistingMarkup(NotifyType type)
    {
        var html = RenderMessage(type, "Hello");
        var severity = type.ToString().ToLowerInvariant();

        Assert.StartsWith("<div", html);
        Assert.Contains("message", html);
        Assert.Contains($"message-{severity}", html);
        Assert.Contains(">Hello</div>", html);
        Assert.DoesNotContain("aria-live", html);
    }

    private static string RenderMessage(NotifyType type, string message)
    {
        var shape = new Shape();
        shape.Properties["Type"] = type;
        shape.Properties["Message"] = new HtmlString(message);

        var content = CoreShapes.Message(shape);

        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);

        return writer.ToString();
    }
}
