using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Shapes;
using OrchardCore.Entities;
using OrchardCore.Environment.Shell;
using OrchardCore.Security;
using OrchardCore.Settings;
using OrchardCore.Settings.Drivers;
using OrchardCore.Settings.ViewModels;

namespace OrchardCore.Tests.Modules.OrchardCore.Settings;

public class ToastSettingsDisplayDriverTests
{
    private const string ValidationMessage = "Enter 0, or a whole number from 5 to 60.";

    [Theory]
    [InlineData("0", 0)]
    [InlineData("5", 5)]
    [InlineData("60", 60)]
    public async Task Update_ValidValue_SavesSectionAndRequestsRelease(string value, int expected)
    {
        var shellReleaseManager = new Mock<IShellReleaseManager>();
        var driver = CreateDriver(granted: true, shellReleaseManager.Object);
        var site = CreateSite(savedSeconds: 20);
        var context = CreateUpdateEditorContext(value);

        var result = await driver.UpdateAsync(site, context);

        Assert.NotNull(result);
        Assert.True(context.Updater.ModelState.IsValid);
        Assert.Equal(expected, site.GetOrCreate<ToastSettings>().SuccessDismissalDelaySeconds);
        shellReleaseManager.Verify(x => x.RequestRelease(), Times.Once());
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1")]
    [InlineData("4")]
    [InlineData("61")]
    [InlineData("2.5")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" 5")]
    [InlineData("+5")]
    [InlineData("abc")]
    public async Task Update_InvalidValue_AddsSingleFieldErrorAndLeavesSectionUnchanged(string value)
    {
        var shellReleaseManager = new Mock<IShellReleaseManager>();
        var driver = CreateDriver(granted: true, shellReleaseManager.Object);
        var site = CreateSite(savedSeconds: 20);
        var sectionBefore = site.Properties[nameof(ToastSettings)].ToJsonString();
        var context = CreateUpdateEditorContext(value);

        var result = await driver.UpdateAsync(site, context);

        Assert.NotNull(result);
        Assert.False(context.Updater.ModelState.IsValid);

        var entry = Assert.Single(context.Updater.ModelState);
        Assert.Equal(nameof(ToastSettingsViewModel.SuccessDismissalDelaySeconds), entry.Key);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal(ValidationMessage, error.ErrorMessage);

        Assert.Equal(sectionBefore, site.Properties[nameof(ToastSettings)].ToJsonString());
        Assert.Equal(20, site.GetOrCreate<ToastSettings>().SuccessDismissalDelaySeconds);
        shellReleaseManager.Verify(x => x.RequestRelease(), Times.Never());
    }

    [Fact]
    public async Task Edit_WithGeneralPermission_ReturnsEditorWithReloadWarning()
    {
        var driver = CreateDriver(granted: true);
        var context = CreateBuildEditorContext();

        var result = await driver.EditAsync(new SiteSettings(), context);

        Assert.NotNull(result);
        Assert.Contains("Settings_Wrapper__Reload", context.Shape.Metadata.Wrappers);
    }

    [Fact]
    public async Task Edit_OtherGroup_ReturnsNull()
    {
        var driver = CreateDriver(granted: true);
        var context = new BuildEditorContext(new Shape(), "debugging", false, string.Empty, null, null, null);

        var result = await driver.EditAsync(new SiteSettings(), context);

        Assert.Null(result);
    }

    [Fact]
    public async Task Edit_WithoutGeneralPermission_ReturnsNull()
    {
        var driver = CreateDriver(granted: false);
        var context = CreateBuildEditorContext();

        var result = await driver.EditAsync(new SiteSettings(), context);

        Assert.Null(result);
        Assert.DoesNotContain("Settings_Wrapper__Reload", context.Shape.Metadata.Wrappers);
    }

    [Fact]
    public async Task Update_WithoutGeneralPermission_DoesNotUpdate()
    {
        var shellReleaseManager = new Mock<IShellReleaseManager>();
        var driver = CreateDriver(granted: false, shellReleaseManager.Object);
        var site = CreateSite(savedSeconds: 20);
        var context = CreateUpdateEditorContext("30");

        var result = await driver.UpdateAsync(site, context);

        Assert.Null(result);
        Assert.True(context.Updater.ModelState.IsValid);
        Assert.Equal(20, site.GetOrCreate<ToastSettings>().SuccessDismissalDelaySeconds);
        shellReleaseManager.Verify(x => x.RequestRelease(), Times.Never());
    }

    private static SiteSettings CreateSite(int savedSeconds)
    {
        var site = new SiteSettings();
        site.Put(new ToastSettings
        {
            SuccessDismissalDelaySeconds = savedSeconds,
        });

        Assert.IsType<JsonObject>(site.Properties[nameof(ToastSettings)]);

        return site;
    }

    private static ToastSettingsDisplayDriver CreateDriver(bool granted, IShellReleaseManager shellReleaseManager = null)
    {
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object>(),
                It.Is<IEnumerable<IAuthorizationRequirement>>(requirements =>
                    requirements.OfType<PermissionRequirement>().Any(requirement =>
                        requirement.Permission.Name == SettingsPermissions.ManageGeneralSettings.Name))))
            .ReturnsAsync(granted ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        return new ToastSettingsDisplayDriver(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            authorizationService.Object,
            shellReleaseManager ?? Mock.Of<IShellReleaseManager>(),
            new StubStringLocalizer<ToastSettingsDisplayDriver>());
    }

    private static BuildEditorContext CreateBuildEditorContext()
        => new(new Shape(), ToastSettingsDisplayDriver.GroupId, false, string.Empty, null, null, null);

    private static UpdateEditorContext CreateUpdateEditorContext(string value)
        => new(new Shape(), ToastSettingsDisplayDriver.GroupId, false, string.Empty, null, null, new StubUpdater(value));

    private sealed class StubUpdater : IUpdateModel
    {
        private readonly string _value;

        public StubUpdater(string value)
        {
            _value = value;
        }

        public ModelStateDictionary ModelState { get; } = new ModelStateDictionary();

        public Task<bool> TryUpdateModelAsync<TModel>(TModel model) where TModel : class
            => Bind(model);

        public Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix) where TModel : class
            => Bind(model);

        public Task<bool> TryUpdateModelAsync<TModel>(TModel model, string prefix, params Expression<Func<TModel, object>>[] includeExpressions) where TModel : class
            => Bind(model);

        public bool TryValidateModel(object model) => true;

        public bool TryValidateModel(object model, string prefix) => true;

        private Task<bool> Bind<TModel>(TModel model) where TModel : class
        {
            if (model is ToastSettingsViewModel viewModel)
            {
                viewModel.SuccessDismissalDelaySeconds = _value;
            }

            return Task.FromResult(true);
        }
    }

    private sealed class StubStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
