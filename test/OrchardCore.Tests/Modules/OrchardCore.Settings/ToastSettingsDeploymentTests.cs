using System.Text.Json.Nodes;
using OrchardCore.Deployment;
using OrchardCore.Entities;
using OrchardCore.Recipes.Models;
using OrchardCore.Settings;
using OrchardCore.Settings.Deployment;
using OrchardCore.Settings.Recipes;
using OrchardCore.Tests.Stubs;

namespace OrchardCore.Tests.Modules.OrchardCore.Settings;

public class ToastSettingsDeploymentTests
{
    [Fact]
    public async Task Export_ThenImport_RoundTripsSuccessDismissalDelaySeconds()
    {
        // Arrange: source site with a saved non-default value.
        var sourceSite = new SiteSettings();
        sourceSite.Put(new ToastSettings
        {
            SuccessDismissalDelaySeconds = 12,
        });

        var sourceSiteService = new Mock<ISiteService>();
        sourceSiteService.Setup(x => x.GetSiteSettingsAsync()).ReturnsAsync(sourceSite);

        var fileBuilder = new MemoryFileBuilder();
        var descriptor = new RecipeDescriptor();
        var result = new DeploymentPlanResult(fileBuilder, descriptor);
        var deploymentSource = new SiteSettingsPropertyDeploymentSource<ToastSettings>(sourceSiteService.Object);

        // Act: export.
        await deploymentSource.ProcessDeploymentStepAsync(new SiteSettingsPropertyDeploymentStep<ToastSettings>(), result);
        await result.FinalizeAsync();

        var content = fileBuilder.GetFileContents("Recipe.json", Encoding.UTF8);
        var recipe = JsonNode.Parse(content);
        var step = (JsonObject)recipe["steps"][0];

        // Assert: the export carries the section.
        Assert.Equal("Settings", step["name"].GetValue<string>());
        Assert.Equal(12, step["ToastSettings"]["SuccessDismissalDelaySeconds"].GetValue<int>());

        // Act: import on a fresh "tenant".
        var targetSite = new SiteSettings();
        var targetSiteService = new Mock<ISiteService>();
        targetSiteService.Setup(x => x.LoadSiteSettingsAsync()).ReturnsAsync(targetSite);

        var settingsStep = new SettingsStep(targetSiteService.Object);
        await settingsStep.ExecuteAsync(new RecipeExecutionContext
        {
            RecipeDescriptor = descriptor,
            Name = "Settings",
            Step = step,
        });

        // Assert: the imported site has the same value.
        targetSiteService.Verify(x => x.UpdateSiteSettingsAsync(targetSite), Times.Once);
        Assert.Equal(12, targetSite.As<ToastSettings>().SuccessDismissalDelaySeconds);
    }
}
