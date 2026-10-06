using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectNamePlaceholder.Application.Security;

namespace ProjectNamePlaceholder.Api.Pages;

[Authorize(Policy = Permissions.AuditRead)]
public class AuditLogsModel : PageModel
{
    public void OnGet()
    {
    }
}
