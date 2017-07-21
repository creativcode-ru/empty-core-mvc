using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore; //запросы к БД
using System.Security.Claims;

using empty_core_mvc.ViewModels; // пространство имен моделей RegisterModel и LoginModel
using empty_core_mvc.Models; // пространство имен UserContext и класса User
using Microsoft.AspNetCore.Authorization;

namespace empty_core_mvc.Controllers
{
    public class AccountController : Controller
    {

        private UserContext db;
        public AccountController(UserContext context)
        {
            db = context;
            /* Так как в файле Startup ранее были добавлены сервисы Entity Framework, 
               то мы можем получить объект контекста данных в конструкторе контроллера.
            */
        }


        #region ---------------------------------------------------- Login ----------------------------------------------------
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            /*  Cмотрим, а есть ли с таким же email в базе данных какой-либо пользователь, 
                если такой пользователь имеется в БД, то выполняем аутентификацию и устанавливаем аутентификационные куки.
             */
            if (ModelState.IsValid)
            {
                User user = await db.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);
                if (user != null)
                {
                    await Authenticate(model.Email); // аутентификация

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }
        #endregion

        #region --------------------------------------------------- Register --------------------------------------------------
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    // добавляем пользователя в бд
                    db.Users.Add(new User { Email = model.Email, Password = model.Password });
                    await db.SaveChangesAsync();

                    await Authenticate(model.Email); // аутентификация

                    return RedirectToAction("Index", "Home");
                }
                else
                    ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }

        #endregion

        private async Task Authenticate(string userName)
        {
            // создаем один claim
            /* Для правильного создания и настройки объекта ClaimsPrincipal вначале создается список claims 
              - набор данных, которые шифруются и добавляются в аутентификационные куки. Каждый такой claim принимает тип и значение.
              В нашем случае у нас только один claim, который в качестве типа принимает константу ClaimsIdentity.DefaultNameClaimType, 
              а в качестве значения - email пользователя.
            */
            var claims = new List<Claim>
                    {
                        new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
                    };


            // создаем объект ClaimsIdentity, нужен для инициализации ClaimsPrincipal
            /* В ClaimsIdentity передается:
                    •Ранее созданный список claims
                    •Тип аутентификации, в данном случае "ApplicationCookie"
                    •Тип данных в списке claims, который преставляет логин пользователя. 
                     То есть при добавлении claimа мы использовали в качестве типа ClaimsIdentity.DefaultNameClaimType, 
                     поэтому и тут нам надо указать то же самое значение. 
                     Мы, конечно, можем указать и разные значения, но тогда система не сможет связать различные claim с логином пользователя.
                    
                   •Тип данных в списке claims, который представляет роль пользователя. 
                    Хотя у нас такого claim нет, который бы представлял роль пользователя, 
                    но но опционально мы можем указать константу ClaimsIdentity.DefaultRoleClaimType.
                    В данном случае она ни на что не влияет.
             */
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            
            // установка аутентификационных куки
            /* В качестве параметра используется схема аутентификации, 
               которая была использована при установки middleware app.UseCookieAuthentication в классе Startup (произвольное название).
             * ClaimsPrincipal, который представляет пользователя.
             */
            await HttpContext.Authentication.SignInAsync("Cookies", new ClaimsPrincipal(id));
            /* после вызова метода HttpContext.Authentication.SignInAsync в ответ клиенту будут отправляться аутентификационные куки, 
               которые при последующих запросах будут передаваться обратно на сервер, десериализоваться и использоваться для аутентификации пользователя.
            */
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.Authentication.SignOutAsync("Cookies"); //выход из авторизации, передается название схемы аутентификации, использованное в классе Startup
            return RedirectToAction("Index", "Home"); //переход на страницу выхода - главная стрвница
        }



        // GET: /<controller>/
        [Authorize]
        public IActionResult Index()
        {
            return Content(User.Identity.Name);
            //return View();
        }
    }
}
