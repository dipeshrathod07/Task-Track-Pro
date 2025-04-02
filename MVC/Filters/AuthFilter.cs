using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MVC.Filters
{
    public class AuthFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            int? empId = session.GetInt32("EmpId");

            if (empId == null) // No active session
            {
                context.HttpContext.Items["AuthMessage"] = "Please log in to access this page.";
                context.Result = new RedirectToActionResult("Login", "Employee", new { status = "Unauthorized"});
                return;
            }

            await Task.CompletedTask; // Required for async compliance
        }
    }
}