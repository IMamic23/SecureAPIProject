using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Buffers;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace NetCoreSecureApi
{
    public class Startup
    {
        private IHostingEnvironment _env;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            _env = env;
            Config = builder.Build();
        }

        IConfigurationRoot Config { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Config);
            services.AddDbContext<CampContext>(ServiceLifetime.Scoped);
            services.AddScoped<ICampRepository, CampRepository>();
            services.AddTransient<CampDbInitializer>();
            services.AddTransient<CampIdentityInitializer>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddAutoMapper();

            services.AddIdentity<CampUser, IdentityRole>()
                .AddEntityFrameworkStores<CampContext>();

            services.Configure<IdentityOptions>(config =>
            {
                config.Cookies.ApplicationCookie.Events =
                    new CookieAuthenticationEvents()
                    {
                        OnRedirectToLogin = (ctx) =>
                        {
                            if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                                ctx.Response.StatusCode = 401;

                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = (ctx) =>
                        {
                            if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                                ctx.Response.StatusCode = 403;

                            return Task.CompletedTask;
                        }
                    };
            });

            services.AddCors(config =>
            {
                config.AddPolicy("Wildermuth", builder =>
                {
                    builder.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins("http://wildermuth.com");
                });

                config.AddPolicy("AnyGET", builder =>
                {
                    builder.AllowAnyHeader()
                        .WithMethods("GET")
                        .AllowAnyOrigin();
                });
            });

            services.AddAuthorization(config =>
            {
                config.AddPolicy("SuperUsers", p => p.RequireClaim("SuperUser", "True"));
            });

            // Add framework services.
            services.AddMvc(options =>
                {
                    if (!_env.IsProduction())
                        options.SslPort = 44373;

                    options.Filters.Add(new RequireHttpsAttribute());
                })
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling =
                             ReferenceLoopHandling.Ignore;
            }); ;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, 
            IHostingEnvironment env, 
            ILoggerFactory loggerFactory,
            CampDbInitializer seeder,
            CampIdentityInitializer identitySeeder)
        {
            loggerFactory.AddConsole(Config.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseIdentity();

            app.UseJwtBearerAuthentication(new JwtBearerOptions()
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = Config["Tokens:Issuer"],
                    ValidAudience = Config["Tokens:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config["Tokens:Key"])),
                    ValidateLifetime = true
                }
            });

            //app.UseCors(config =>
            //{
                //config.AllowAnyMethod()
                //    .AllowAnyHeader()
                //    .WithOrigins("http://test.com");
            //});

            app.UseMvc(options =>
            {
            });

            seeder.Seed().Wait();
            identitySeeder.Seed().Wait();
        }
    }
}
