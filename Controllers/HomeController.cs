using Microsoft.AspNetCore.Mvc;

namespace CMCSPrototype.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult LecturerClaim() => View();
        public IActionResult CoordinatorApproval() => View();
        public IActionResult ClaimStatus() => View();
    }
}