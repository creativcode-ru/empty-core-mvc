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
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            //Перенаправление  url до его обработки
            var rewriteOptions = new RewriteOptions()
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

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
