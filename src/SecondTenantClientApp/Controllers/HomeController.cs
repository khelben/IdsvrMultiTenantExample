using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace SecondTenantClientApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var user = User;
            return View();
        }

        [Authorize]
        public IActionResult Secure()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.Authentication.SignOutAsync("Cookies");
            return new SignOutResult("ExternalIdSvr", new AuthenticationProperties()
            {
                RedirectUri = "/"
            });
        }
    }
}
