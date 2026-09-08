using Microsoft.AspNetCore.Mvc;
using AzureBlobExplorer.Models;

namespace AzureBlobExplorer.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("error/{id:regex(^\\d{3}$)}")]
        public IActionResult Index(int id)
        {
            var model = new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier,
                ErrorMessage = id switch
                {
                    404 => "The page you are looking for was not found.",
                    403 => "You do not have permission to access this resource.",
                    500 => "An internal server error occurred. Please try again later.",
                    _ => $"An error occurred (Error code: {id})"
                }
            };

            _logger.LogError($"Error {id}: {model.ErrorMessage}");
            return View("Error", model);
        }
    }
}
