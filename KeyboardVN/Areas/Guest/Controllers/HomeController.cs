﻿using Microsoft.AspNetCore.Mvc;

namespace KeyboardVN.Areas.Guest.Controllers
{
    public class HomeController : Controller
    {
        [Area("Guest")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
