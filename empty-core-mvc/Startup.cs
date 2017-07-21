using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Rewrite;
// Для перизаписи URL требуется установить Microsoft.AspNetCore.Rewrite https://www.nuget.org/packages/Microsoft.AspNetCore.Rewrite/
// Документация https://docs.microsoft.com/en-us/aspnet/core/fundamentals/url-rewriting
// Примеры на gitgub https://github.com/aspnet/Docs/tree/master/aspnetcore/fundamentals/url-rewriting/sample
using Microsoft.Net.Http.Headers; //для 301 редиректа вклассе RedirectWwwRule

//простая авторизация по кукам https://metanit.com/sharp/aspnet5/15.1.php , документация Microsoft https://docs.microsoft.com/en-us/aspnet/core/security/authentication/cookie
// http://dotnetcoretutorials.com/2017/05/13/cookie-authentication-asp-net-core/
/* Добавить пакет NuGet: Microsoft.AspNetCore.Authentication.Cookies
   и для хранения данных Microsoft.EntityFrameworkCore.SqlServer и Microsoft.EntityFrameworkCore.Tools
*/
using empty_core_mvc.Models; 
using Microsoft.EntityFrameworkCore; //для подключения к база
using Microsoft.AspNetCore.Http;
/*
*/



namespace empty_core_mvc
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connection = Configuration.GetConnectionString("DefaultConnection"); //из файла appsettings.json; имя БД: (localdb)\MSSQLLocalDB
            services.AddDbContext<UserContext>(options => options.UseSqlServer(connection));//для авторизации

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            //Перенаправление  url до его обработки
            var rewriteOptions = new RewriteOptions()
                .Add(new RedirectWwwRule())     //убираем www. см. код класса
                .AddRedirect("(.*)/$", "$1")    //убираем слеж в конце адреса
                .AddRedirect("((?i)(home(/index)?|index))$", "/"); //убираем дубликат стартовой страницы; (?i) это ignorecase

            app.UseRewriter(rewriteOptions); //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/url-rewriting


            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            //Авторизация по кукам
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationScheme = "Cookies",
                LoginPath = new Microsoft.AspNetCore.Http.PathString("/Account/Login"),
                AccessDeniedPath = new PathString("/Account/Forbidden/"),
                AutomaticAuthenticate = true, //при каждом запросе проверять пользователя на  аутентификацию
                AutomaticChallenge = true //не авторизованный пользователь при попытке доступа к ресурсам, для которых нужна авторизация, будет перенаправляться по пути в свойстве LoginPath
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }


    /// <summary>
    /// Редирект 301 с www. на домен без www.
    /// </summary>
    public class RedirectWwwRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        // app.UseRewriter(new RewriteOptions().Add(new RedirectWwwRule()));
        ////отбросить www http://stackoverflow.com/questions/41205435/asp-net-core-1-1-url-rewriting-www-to-non-www
        // или https://www.softfluent.com/blog/dev/Page-redirection-and-URL-Rewriting-with-ASP-NET-Core


        public void ApplyRule(RewriteContext context)
        {
            var req = context.HttpContext.Request;
            var host = req.Host;
            if (string.Equals(host.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = RuleResult.ContinueRules;
                return;
            }

            // checking if the hostName has www. at the beginning
            if (host.ToString().StartsWith("www."))
            {
                // Strip off www.
                var newHostName = host.ToString().Substring(4);

                // Creating new url
                var newUrl = new System.Text.StringBuilder()
                                      .Append(req.Scheme).Append("://")
                                      .Append(newHostName)
                                      .Append(req.PathBase)
                                      .Append(req.Path)
                                      .Append(req.QueryString)
                                      .ToString();

                // Modify Http Response
                var response = context.HttpContext.Response;
                response.Headers[HeaderNames.Location] = newUrl;
                response.StatusCode = 301;
                context.Result = RuleResult.EndResponse; // Do not continue processing the request   
            }
        }
    }

}
