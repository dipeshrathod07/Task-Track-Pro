using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVC.Filters;
using Repositories.Models;
using Repositories.Interfaces;

namespace MVC.Controllers
{
    // [Route("[controller]")]
    public class EmployeeController : Controller
    {
        public EmployeeController() {}

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Tasks()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Chats()
        {
            return View();
        }

        public IActionResult ChatUser()
        {
            return View();
        }
    }
}