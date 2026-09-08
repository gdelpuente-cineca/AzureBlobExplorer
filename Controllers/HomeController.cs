using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace AzureBlobExplorer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var objectId = User.GetObjectId();
                var displayName = User.GetDisplayName();
                _logger.LogInformation($"Authenticated user: {displayName} ({objectId})");
            }
            
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
