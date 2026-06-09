using BE.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BE.Infrastructure.Filters;

/// <summary>
/// Tự động inject SiteSettings vào ViewData["SiteSettings"] cho mọi Controller action.
/// </summary>
public class SiteSettingsFilter : IAsyncActionFilter
{
    private readonly ISiteSettingsService _siteSettingsService;

    public SiteSettingsFilter(ISiteSettingsService siteSettingsService)
    {
        _siteSettingsService = siteSettingsService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller)
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            controller.ViewData["SiteSettings"] = settings;
        }
        await next();
    }
}
