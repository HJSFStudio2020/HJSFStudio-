using System;
using System.IO;
using System.Reflection;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Cache;
using HJSF.AOP;
using HJSF.Enum;
using HJSF.RepositoryServices;
using Interface.ISqlSguar;
using ISqlSguar;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Middleware;
using Middleware.JwtMiddleware;
using RepositoryServices;
using Utility;
namespace HJSF.Web
{
    /// <summary>
    /// ������
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// ���췽��
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {

            Configuration = configuration;
        }
        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        /// <summary>
        /// ע�뷽��
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {

            // ���������Ϣ
            services.Configure<AppSettingModel>(Configuration.GetSection("AppSetting"));
            services.AddScoped<IAuthorizationHandler, Middleware.JwtBearerHandler>();
             Constant.AppSetting = Configuration.GetSection("AppSetting").Get<AppSettingModel>();

            services.AddSingleton<IDBServices, DBServices>(x => new DBServices(Configuration["AppSetting:DataBase:ContextConn"]));
          
            services.AddControllers();
            services.AddMemoryCache();
            #region Jwt
           

            #endregion
            // ��ӿ������
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                    //.AllowAnyOrigin() //�����κ���Դ����������
                    .WithOrigins(Constant.AppSetting.App.Cors)//.SetIsOriginAllowedToAllowWildcardSubdomains()//����������ʵ���
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // ���Session
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(30);
                options.Cookie.HttpOnly = true;
            });
          
            services.AddControllersWithViews()
         .AddControllersAsServices();//����Ҫд
            services.AddJwtBearer(Constant.AppSetting);
            services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Web�ӿ�",
                    Version = "�汾1"

                });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                // ����xmlע��. �÷����ڶ����������ÿ�������ע�ͣ�Ĭ��Ϊfalse.
                option.IncludeXmlComments(xmlPath, true);
                var securityScheme = new OpenApiSecurityScheme()
                {
                    Description = "���¿�����������Header����Ҫ��ӵ�JWT��Ȩ���룺Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                };
                option.AddSecurityDefinition("Bearer", securityScheme);
                option.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
            });
        }
        /// <summary>
        /// ע�����
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {

            builder.RegisterType<RedisAOP>();
            builder.RegisterType<RedisHelp>()
                .As<ICache>()
                 .WithParameter("_connectionString", Configuration["AppSetting:Redis:RedisHostConnection"])
                .PropertiesAutowired()//��ʼ����ע��
                .InstancePerLifetimeScope();//��Ϊÿһ����������ô���һ����һ�Ĺ����ʵ��
            Assembly services = Assembly.LoadFrom("Services");
            Assembly repository = Assembly.Load("Interface");
            builder.RegisterAssemblyTypes(services, repository).
            Where(x => x.Name.EndsWith("Server", StringComparison.OrdinalIgnoreCase))
            .AsImplementedInterfaces()
            .EnableInterfaceInterceptors();
            // .InterceptedBy(typeof(RedisAOP));
            //builder.RegisterType<BaseRepository>()
            //                .As<IBaseRepository>()
            //                 .PropertiesAutowired()//��ʼ����ע��
            //    .InstancePerLifetimeScope();//��Ϊÿһ����������ô���һ����һ�Ĺ����ʵ��



            var controllerBaseType = typeof(ControllerBase);
            builder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .Where(t => controllerBaseType.IsAssignableFrom(t) && t != controllerBaseType)
                .PropertiesAutowired();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();


            // ����Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
                c.RoutePrefix = "doc";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                c.DefaultModelsExpandDepth(-1);
            });

            app.UseRouting();
            app.UseSession();
         
          
            app.UseMiddleware<RequestTimeMiddleware>();
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            // ������Ȩ

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
