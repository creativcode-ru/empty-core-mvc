using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace empty_core_mvc.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult BassCss()
        {
            ViewData["Message"] = "Примеры Basscss";

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Сведения о пректе";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

    }
}
