/*using CoreVisionConfig.Configuration;
using CoreVisionDAL.Context;
using CoreVisionFoundation.Extensions;
using CoreVisionBAL.ExceptionHandler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using CoreVisionBAL.Foundation.Web;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.OData;

namespace CoreVisionFoundation
{
    public partial class Startup
    {
        public IConfiguration Configuration { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            var configObject = new APIConfiguration();
            //var stripeSettings = new StripeSettings();
            var mailSettings = new SmtpMailSettings();
            var externalIntegrations = new ExternalIntegrations();

            Configuration.GetRequiredSection("APIConfiguration").Bind(configObject);
            Configuration.GetRequiredSection("ExternalIntegrations").Bind(externalIntegrations);
            //Configuration.GetRequiredSection("AzureConfiguration").Bind(azureConfigurations);
            //Configuration.GetRequiredSection("StripeSettings").Bind(stripeSettings);
            Configuration.GetRequiredSection("SmtpMailSettings").Bind(mailSettings);
            //Configuration.GetRequiredSection("HuggingFaceConfiguration").Bind(hugginFaceConfiguration);
            //configObject.StripeSettings = stripeSettings;
            configObject.SmtpMailSettings = mailSettings;
            configObject.ExternalIntegrations = externalIntegrations;
            //configObject.HuggingFaceConfiguration = hugginFaceConfiguration;
            //configObject.AzureConfiguration = azureConfigurations;
            services.AddSingleton<APIConfiguration>((x) => configObject);
            services.ConfigureCommonApplicationDependencies(Configuration, configObject);
            RegisterAllThirdParties(services);

            // For Razor Pages
            //services.AddRazorPages();

            // For Adding MVC
            //services.AddControllersWithViews();

            // For WbApi Controllers
            var mvcBuilder = services.AddControllers(x =>
            {
                x.Filters.Add<APIExceptionFilter>();
            })
                .AddNewtonsoftJson(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.None;
                    opt.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                });
            if (configObject.IsOdataEnabled)
            {
                mvcBuilder.AddOData((opt, x) =>
                {
                    opt.AddRouteComponents("v1", x.GetEdmModel())
                    .Filter().Select().Expand().OrderBy().SetMaxTop(100).SkipToken().Count();
                });
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, APIConfiguration configObject)
        {
            // Configure the HTTP request pipeline.
            //app.UseHttpsRedirection();
            app.ConfigureCommonInPipeline(configObject);

            EnsureDirectoriesExist(env);
            //wwwroot
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "content")),
            });

            //app.UseStaticFiles(new StaticFileOptions
            //{
            //    FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "website/end-user/browser")),
            //});
            //app.UseStaticFiles(new StaticFileOptions
            //{
            //    FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "website/superadmin/browser")),
            //});
            //app.UseStaticFiles();
            //for static files different folder
            //app.UseStaticFiles(new StaticFileOptions()
            //{
            //    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "PublicRoot")),
            //    RequestPath = new PathString("/public")
            //});
            //app.UseStaticFiles(new StaticFileOptions
            //{
            //    FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "website/superadmin/browser")),
            //    RequestPath = "/superadmin"

            //});
            //app.UseStaticFiles(new StaticFileOptions
            //{
            //    FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "website/end-user/browser")),
            //    RequestPath = ""

            //});

            // add tracingID for the request if not present already.
            app.Use(async (context, next) =>
            {
                context.Request.GetOrAddTracingId();
                await next.Invoke();
            });

            app.UseRouting();
            app.UseAuthorization();

            // for Razor Pages
            //app.MapRazorPages();

            // for map Get
            //app.MapGet("/samDirect", () => { return new { id = 1 }; });

            // For WebApi
            app.UseEndpoints(endpoints =>
            {

                //endpoints.MapFallbackToFile(Path.Combine("website/end-user/server", "index.server.html"));
                //endpoints.MapFallbackToFile(Path.Combine("website", "index.html"));

                // Fallback route for the end-user Angular app

                //endpoints.MapFallback(context =>
                //{
                //    //string filePath = context.Request.Path.StartsWithSegments("/superadmin") ?
                //    //              Path.Combine(env.WebRootPath, "website/superadmin/browser/index.html") :
                //    //              Path.Combine(env.WebRootPath, "website/end-user/browser/index.html");

                //    //context.Response.ContentType = "text/html";
                //    //return context.Response.SendFileAsync(filePath);
                //    if (context.Request.Path.StartsWithSegments("/superadmin"))
                //    {
                //        context.Response.ContentType = "text/html";
                //        return context.Response.SendFileAsync(Path.Combine(env.WebRootPath, "website/superadmin/browser/index.html"));
                //    }
                //    else
                //    {
                //        context.Response.ContentType = "text/html";
                //        //context.Response.ContentType = "application/javascript";
                //        return context.Response.SendFileAsync(Path.Combine(env.WebRootPath, "website/end-user/browser/index.html"));
                //        // Redirect to the controller index endpoint
                //        //var routeData = context.GetEndpoint().RouteValues;
                //        var controllerName = "Home";
                //        var actionName = "Index";
                //        //var queryString = context.Request.QueryString.ToString();

                //        var redirectTo = $"/{controllerName}/{actionName}";

                //        context.Response.Redirect(redirectTo);
                //        return Task.CompletedTask;
                //    }
                //});
                //endpoints.MapControllerRoute(
                //    name: "default",
                //    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllers();

            });

            // for MVC
            //app.MapControllerRoute(
            //    name: "default",
            //    pattern: "{controller=Home}/{action=Index}/{id?}");
        }

        private void RegisterAllThirdParties(IServiceCollection services)
        {
            #region Sql DB context Setup

            //Use Context Pooling
            //https://neelbhatt.com/2018/02/27/use-dbcontextpooling-to-improve-the-performance-net-core-2-1-feature/

            services.AddDbContextPool<ApiDbContext>((provider, options) =>
            {
                options.UseSqlServer(provider.GetService<APIConfiguration>().ApiDbConnectionString,
                    (x) =>
                    {
                        //x.MigrationsAssembly(typeof(Startup).Assembly.FullName);
                        //x.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: new System.TimeSpan(0, 0, 1), errorNumbersToAdd: null);
                        //x.ExecutionStrategy(y => new BOLDExecutionStrategy(y.CurrentContext.Context, provider.GetService<IBoldLogger>()));
                    });
                options.EnableSensitiveDataLogging();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            });

            //services.Add(new ServiceDescriptor(typeof(IEFRepository<,>), typeof(EFRepository<,>), ServiceLifetime.Scoped));

            #endregion Sql DB context Setup

            #region Application Specific Registerations


            #endregion Application Specific Registerations
        }

        private void EnsureDirectoriesExist(IWebHostEnvironment env)
        {
            string[] directories = new string[]
            {
            Path.Combine(env.WebRootPath, "website"),
            Path.Combine(env.WebRootPath, "website/superadmin"),
            Path.Combine(env.WebRootPath, "website/superadmin/browser"),
            Path.Combine(env.WebRootPath, "website/end-user"),
            Path.Combine(env.WebRootPath, "website/end-user/browser")
            };

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }
    }
}
*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.OData;
using SMSFoundation.Extensions;
using SMSDAL.Context;
using SMSConfig.Configuration;
using SMSBAL.Foundation.Web;
using SMSBAL.ExceptionHandler;

namespace SMSFoundation
{
    public partial class Startup
    {
        public IConfiguration Configuration { get; private set; }

        public Startup(IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .SetBasePath(Directory.GetCurrentDirectory()) // Ensure correct path
                .AddJsonFile("/etc/secrets/appSettings.Production.json", optional: true, reloadOnChange: true) // Load from Render Secret Files
                .AddEnvironmentVariables(); // Load env variables

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var configObject = new APIConfiguration();
            var mailSettings = new SmtpMailSettings();
            var externalIntegrations = new ExternalIntegrations();

            // Bind from appsettings.json or Environment Variables
            Configuration.GetRequiredSection("APIConfiguration").Bind(configObject);
            Configuration.GetRequiredSection("ExternalIntegrations").Bind(externalIntegrations);
            Configuration.GetRequiredSection("SmtpMailSettings").Bind(mailSettings);

            configObject.SmtpMailSettings = mailSettings;
            configObject.ExternalIntegrations = externalIntegrations;

            // Override values with environment variables from Render
            configObject.ApiDbConnectionString = Environment.GetEnvironmentVariable("ApiDbConnectionString") ?? configObject.ApiDbConnectionString;
            configObject.JwtTokenSigningKey = Environment.GetEnvironmentVariable("JwtTokenSigningKey") ?? configObject.JwtTokenSigningKey;

            services.AddSingleton(configObject);
            services.ConfigureCommonApplicationDependencies(Configuration, configObject);
            RegisterAllThirdParties(services);

            var mvcBuilder = services.AddControllers(x =>
            {
                x.Filters.Add<APIExceptionFilter>();
            })
            .AddNewtonsoftJson(opt =>
            {
                opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                opt.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.None;
                opt.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });

            if (configObject.IsOdataEnabled)
            {
                mvcBuilder.AddOData((opt, x) =>
                {
                    opt.AddRouteComponents("v1", x.GetEdmModel())
                    .Filter().Select().Expand().OrderBy().SetMaxTop(100).SkipToken().Count();
                });
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, APIConfiguration configObject)
        {
            app.ConfigureCommonInPipeline(configObject);
            EnsureDirectoriesExist(env);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "content")),
            });

            app.Use(async (context, next) =>
            {
                context.Request.GetOrAddTracingId();
                await next.Invoke();
            });

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void RegisterAllThirdParties(IServiceCollection services)
        {
            services.AddDbContextPool<ApiDbContext>((provider, options) =>
            {
                var configuration = provider.GetService<APIConfiguration>();
                var connectionString = Environment.GetEnvironmentVariable("ApiDbConnectionString") ?? configuration.ApiDbConnectionString;

                options.UseSqlServer(connectionString);
                options.EnableSensitiveDataLogging();
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            });
        }

        private void EnsureDirectoriesExist(IWebHostEnvironment env)
        {
            string[] directories = new string[]
            {
                Path.Combine(env.WebRootPath, "website"),
                Path.Combine(env.WebRootPath, "website/superadmin"),
                Path.Combine(env.WebRootPath, "website/superadmin/browser"),
                Path.Combine(env.WebRootPath, "website/end-user"),
                Path.Combine(env.WebRootPath, "website/end-user/browser")
            };

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }
    }
}
