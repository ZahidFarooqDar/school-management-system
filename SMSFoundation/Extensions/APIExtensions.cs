using AutoMapper;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation;
using SMSBAL.Foundation.Base;
using SMSBAL.Foundation.Config;
using SMSBAL.Foundation.Web;
using SMSConfig.Configuration;
using SMSFoundation.AutoMapperBindings;
using SMSFoundation.Foundation.Web.Security;
using SMSFoundation.Security;
using SMSServiceModels.AppUser;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;

namespace SMSFoundation.Extensions
{
    public static class APIExtensions
    {
        public static void ConfigureCommonApplicationDependencies(this IServiceCollection services, IConfiguration baseConfiguration, APIConfiguration configObject)
        {
            #region Register Application Identification

            services.AddSingleton((x) =>
            {
                var appIdentification = new ApplicationIdentificationRoot();
                baseConfiguration.GetRequiredSection("ApplicationIdentification").Bind(appIdentification);
                return appIdentification;
            });

            #endregion Application Identification

            #region Register Mapper

            services.AddSingleton<AutoMapper.IConfigurationProvider>(x =>
            {
                var config = new MapperConfiguration(
                    cfg =>
                    {
                        cfg.ConstructServicesUsing(t => x.GetService(t));
                        cfg.AddProfile(new AutoMapperDefaultProfile(x));
                    });
                return config;
            });
            services.AddSingleton(x =>
            {
                var config = x.GetRequiredService<AutoMapper.IConfigurationProvider>();
                return config.CreateMapper();
            });

            #endregion Register Mapper

            #region Register Logger

            #endregion Register Logger

            #region Register Context Accessor

            services.AddHttpContextAccessor();
            //services.AddScoped<APIExceptionFilter>();
            #endregion Register Context Accessor

            #region Stripe
            //Stripe.StripeConfiguration.ApiKey = configObject.StripeSettings.PrivateKey;
            #endregion Stripe

            #region Register Base Configuration

            #endregion Register Base Configuration

            #region API Authentication

            //Register Auth
            services.Configure<SMSAuthenticationSchemeOptions>(x => x.JwtTokenSigningKey = configObject.JwtTokenSigningKey);

            //Auth // can use issuer constructor if we want seperate issuer for qa,reg and prod etc
            services.AddSingleton(x => new JwtHandler(configObject.JwtIssuerName));

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = SMSBearerTokenAuthHandlerRoot.DefaultSchema;
            })
                .AddScheme<SMSAuthenticationSchemeOptions, APIBearerTokenAuthHandler>(SMSBearerTokenAuthHandlerRoot.DefaultSchema, o => { })
                // Uncomment for Cookie Authentication , see CookieController for more info
                //.AddCookie((x) =>
                //{
                //    x.LoginPath = "/Cookie/ClientLogin";
                //    x.TicketDataFormat = new CustomSecureDateFormatter(JwtHandler, objAuthDecryptionConfiguration);
                //})
                ;

            services.AddSingleton<IPasswordEncryptHelper>((x) => new PasswordEncryptHelper(configObject.AuthTokenEncryptionKey, configObject.AuthTokenDecryptionKey));


            #endregion

            #region AutoRegister All Process

            services.AutoRegisterAllBALAsSelfFromBaseTypes<SMSBalBase>(ServiceLifetime.Scoped);
            services.AddSingleton(sp =>
            {
                string endpoint = configObject.ExternalIntegrations.AzureConfiguration.TextAnalyticsConfiguration.EndPoint;
                string key = configObject.ExternalIntegrations.AzureConfiguration.TextAnalyticsConfiguration.ApiKey;

                var credential = new AzureKeyCredential(key);
                return new TextAnalyticsClient(new Uri(endpoint), credential);
            });
            services.AddHttpClient();
            

            #endregion AutoRegister All Process

            #region Register Swagger
            if (configObject.IsSwaggerEnabled)
            {
                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(option =>
                {
                    option.SwaggerDoc("v1", new OpenApiInfo { Title = "School Management API", Version = "v1" });
                    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Enter Token Only (Without 'Bearer')",
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        BearerFormat = "JWT",
                        Scheme = "Bearer"
                    });
                    option.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type=ReferenceType.SecurityScheme,
                                    Id="Bearer"
                                }
                            },
                            new string[]{}
                        }
                    });
                });
            }
            #endregion Register Swagger

            #region  To Enable Cors

            if (configObject.IsCorsEnabled)
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowAllPolicy",
                        builder =>
                        {
                            builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            //.AllowCredentials()
                            ;
                        });
                });
            }

            #endregion  To Enable Cors

            #region LoggedInUser

            services.AddScoped<ILoginUserDetail>(x =>
            {
                var user = x.GetService<IHttpContextAccessor>().HttpContext.User;
                if (user != null && user.Identity.IsAuthenticated)
                {
                    if ( user.IsInRole(RoleTypeSM.SystemAdmin.ToString()))
                    {
                        var u = new LoginUserDetail();
                        u.DbRecordId = user.GetUserRecordIdFromCurrentUserClaims();
                        u.LoginId = user.Identity.Name;
                        u.UserType = Enum.Parse<RoleTypeSM>(user.GetUserRoleTypeFromCurrentUserClaims());
                        return u;
                    }
                    else if (user.IsInRole(RoleTypeSM.Admin.ToString()) || user.IsInRole(RoleTypeSM.Student.ToString())
                    || user.IsInRole(RoleTypeSM.Parent.ToString()))
                    {
                        var u = new LoginUserDetailWithCompany();
                        u.DbRecordId = user.GetUserRecordIdFromCurrentUserClaims();
                        u.LoginId = user.Identity.Name;
                        u.UserType = Enum.Parse<RoleTypeSM>(user.GetUserRoleTypeFromCurrentUserClaims());
                        u.CompanyRecordId = user.GetCompanyRecordIdFromCurrentUserClaims();
                        u.CompanyCode = user.GetCompanyCodeFromCurrentUserClaims();
                        return u;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                return new LoginUserDetail() { DbRecordId = 0, LoginId = "nullUser", UserType = RoleTypeSM.Unknown };
            });

            #endregion LoggedInUser

            #region Register Error Handler

            services.AddSingleton(x =>
            {
                var appId = x.GetService<ApplicationIdentificationRoot>();
                /*var appId2 = new CoreVisionFoundation.Foundation.Config.ApplicationIdentificationRoot()
                {
                    ApplicationName =  appId.ApplicationName,
                    AppNameToken = appId.AppNameToken
                };*/
                var errorBal = new ErrorLogProcessRoot(configObject.ApiDbConnectionString, appId);
                return new ErrorLogHandlerRoot(configObject, errorBal, appId);
            });

            #endregion Register Error Handler
        }


        public static void ConfigureCommonInPipeline(this IApplicationBuilder app, APIConfiguration configObject)
        {
            //To Enable Cors
            if (configObject.IsCorsEnabled)
            {
                app.UseCors("AllowAllPolicy");
            }

            if (configObject.IsSwaggerEnabled)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
        }

        #region Register Edmx Model
        public static IEdmModel GetEdmModel(this IServiceProvider serviceProvider)
        {
            ODataConventionModelBuilder builder = new();
            builder.EntitySet<ClientUserSM>(nameof(ClientUserSM));
            builder.EntitySet<ApplicationUserSM>(nameof(ApplicationUserSM));
            return builder.GetEdmModel();
        }

        #endregion Register Edmx Model
    }
}
