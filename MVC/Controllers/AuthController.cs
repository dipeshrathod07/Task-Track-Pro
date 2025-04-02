using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        #region Get: Login
        public IActionResult Login()
        {
            return View();
        }
        #endregion

        #region Get: Register
        public IActionResult Register()
        {
            return View();
        }
        #endregion

        #region Get: ChangePass
        [HttpGet]
        public IActionResult ChangePass()
        {
            return View();
        }
        #endregion

        #region Get: Logout
        [HttpGet]
        public IActionResult Logout()
        {
            return View();
        }
        #endregion
    }
}