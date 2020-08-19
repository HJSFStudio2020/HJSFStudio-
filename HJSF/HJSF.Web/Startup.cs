using System;
using System.IO;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Cache;
using HJSF.AOP;
using Interface.ISqlSguar;
using ISqlSguar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Middleware;
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


            services.AddControllers();
            services.AddControllersWithViews()
           .AddControllersAsServices();//����Ҫд
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
            });
        }
        /// <summary>
        /// ע�����
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {

            builder.RegisterType<RedisAOP>();

            //���ݿ�ע��
            builder.RegisterType<DBServices>()
                   .As<IDBServices>()
                   .WithParameter("ConnectionString", Configuration["AppSetting:DataBase:ContextConn"])
                   .PropertiesAutowired()//��ʼ����ע��
                   .InstancePerLifetimeScope();//��Ϊÿһ����������ô���һ����һ�Ĺ����ʵ��
            //Redisע��
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
            app.UseMiddleware<RequestTimeMiddleware>();
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseAuthorization();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
