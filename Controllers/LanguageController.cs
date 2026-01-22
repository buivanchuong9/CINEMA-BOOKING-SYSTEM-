using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BE.Controllers;

public class LanguageController : Controller
{
    [HttpPost]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            "CineMax.Culture",
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions 
            { 
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                Path = "/"
            }
        );

        return LocalRedirect(returnUrl ?? "/");
    }
}
