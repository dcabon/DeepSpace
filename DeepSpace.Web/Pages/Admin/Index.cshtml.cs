using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
namespace DeepSpace.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
