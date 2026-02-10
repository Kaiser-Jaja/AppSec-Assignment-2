using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSec_Assignment_2.Pages.Errors;

public class StatusCodeModel : PageModel
{
    public int StatusCode { get; set; }

    public void OnGet(int statusCode = 0)
    {
        StatusCode = statusCode;
    }
}
