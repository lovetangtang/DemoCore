using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Infrastructure.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Application;
using Domain.Authenticate;
using Infrastructure;
using Quartzs;
using Quartz.Spi;
using Quartz;
using Quartz.Impl;
using CSRedis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Cache.Redis;

namespace Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region 添加Swagger
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                // 获取xml文件名
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                // 获取xml文件路径
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                // 添加控制器层注释，true表示显示控制器注释
                //options.IncludeXmlComments(xmlPath, true);
            });
            #endregion

            #region 添加EFCore
            services.AddControllers();
            var sqlConnection = Configuration.GetConnectionString("SqlServerConnection");
            services.AddDbContext<ApiDBContent>(option => option.UseSqlServer(sqlConnection));
            #endregion

            #region 添加jwt认证
            services.Configure<TokenManagement>(Configuration.GetSection("tokenManagement"));
            var token = Configuration.GetSection("tokenManagement").Get<TokenManagement>();

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(token.Secret)),
                    ValidIssuer = token.Issuer,
                    ValidAudience = token.Audience,
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
            #endregion
            services.Configure<WebSettings>(Configuration.GetSection("WebSettings"));
            #region 依赖注入自定义Service接口
            services.AddDataService();
            //services.AddScoped<IAuthenticateService, AuthenticateService>();
            //services.AddScoped<IUserService, UserService>();
            #endregion

            #region 注入Redis
            // Redis客户端要定义成单例， 不然在大流量并发收数的时候， 会造成redis client来不及释放。另一方面也确认api控制器不是单例模式，
            string redisConnect = Configuration.GetConnectionString("redis");
            var csredis = new CSRedisClient(redisConnect);

            RedisHelper.Initialization(csredis);
            services.AddSingleton(csredis);

            services.AddSingleton<IDistributedCache>(new CSRedisCache(new CSRedisClient(redisConnect)));

            // 连接Redis的容器，此时6380端口。
            services.AddSingleton<IDistributedSessionCache>(new CSRedisSessionCache(new CSRedisClient(redisConnect)));
            services.AddRedisSession();

            services.AddScoped(typeof(RedisCoreHelper));
            #endregion

            // 如果不实现IDistributedCache将会异常。
            services.AddSession();

            //添加后台运行任务
            services.AddHostedService<BackgroundJob>();

            #region 注入 Quartz调度类
            //注入 Quartz调度类
            services.AddSingleton<QuartzStartup>();
            services.AddTransient<UserInfoSyncjob>();      // 这里使用瞬时依赖注入
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();//注册ISchedulerFactory的实例。

            services.AddSingleton<IJobFactory, IOCJobFactory>();
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [Obsolete]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Microsoft.Extensions.Hosting.IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // 添加Swagger有关中间件
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Demo v1");
            });

            //启用验证
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //获取前面注入的Quartz调度类
            var quartz = app.ApplicationServices.GetRequiredService<QuartzStartup>();
            appLifetime.ApplicationStarted.Register(() =>
            {
                quartz.Start().Wait(); //网站启动完成执行
            });

            appLifetime.ApplicationStopped.Register(() =>
            {
                quartz.Stop();  //网站停止完成执行

            });
        }
    }
}
