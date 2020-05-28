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
            #region ����Swagger
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                // ��ȡxml�ļ���
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                // ��ȡxml�ļ�·��
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                // ���ӿ�������ע�ͣ�true��ʾ��ʾ������ע��
                //options.IncludeXmlComments(xmlPath, true);
            });
            #endregion

            #region ����EFCore
            services.AddControllers();
            var sqlConnection = Configuration.GetConnectionString("SqlServerConnection");
            services.AddDbContext<ApiDBContent>(option => option.UseSqlServer(sqlConnection));
            #endregion

            #region ����jwt��֤
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
            #region ����ע���Զ���Service�ӿ�
            services.AddDataService();
            //services.AddScoped<IAuthenticateService, AuthenticateService>();
            //services.AddScoped<IUserService, UserService>();
            #endregion

            #region ע��Redis
            // Redis�ͻ���Ҫ����ɵ����� ��Ȼ�ڴ���������������ʱ�� �����redis client�������ͷš���һ����Ҳȷ��api���������ǵ���ģʽ��
            string redisConnect = Configuration.GetConnectionString("redis");
            var csredis = new CSRedisClient(redisConnect + ",name=receiver");
            RedisHelper.Initialization(csredis);
            services.AddSingleton(csredis);

            services.AddSingleton<IDistributedCache>(new CSRedisCache(new CSRedisClient(redisConnect)));

            // ����Redis����������ʱ6380�˿ڡ�
            services.AddSingleton<IDistributedSessionCache>(new CSRedisSessionCache(new CSRedisClient("127.0.0.1:6380")));
            services.AddRedisSession();


            #endregion

            // �����ʵ��IDistributedCache�����쳣��
            services.AddSession();

            //���Ӻ�̨��������
            services.AddHostedService<BackgroundJob>();

            #region ע�� Quartz������
            //ע�� Quartz������
            services.AddSingleton<QuartzStartup>();
            services.AddTransient<UserInfoSyncjob>();      // ����ʹ��˲ʱ����ע��
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();//ע��ISchedulerFactory��ʵ����

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

            // ����Swagger�й��м��
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Demo v1");
            });

            //������֤
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //��ȡǰ��ע���Quartz������
            var quartz = app.ApplicationServices.GetRequiredService<QuartzStartup>();
            appLifetime.ApplicationStarted.Register(() =>
            {
                quartz.Start().Wait(); //��վ�������ִ��
            });

            appLifetime.ApplicationStopped.Register(() =>
            {
                quartz.Stop();  //��վֹͣ���ִ��

            });
        }
    }
}