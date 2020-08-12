using System;
using Haikakin.Data;
using Haikakin.Repository;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoMapper;
using Haikakin.HaikakinMapper;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using Quartz.Impl;
using Haikakin.Models.OrderScheduler;
using System.Collections.Generic;

namespace Haikakin
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
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

            // 從 appsettings.json 讀取 Ip Rule 設定
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            // 注入 counter and IP Rules 
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            // the clientId/clientIp resolvers use it.
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Rate Limit configuration 設定
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.SetIsOriginAllowed(x => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                    });

                options.AddPolicy("DepolyPolicy",
                    builder =>
                    {
                        builder.SetIsOriginAllowed(MyIsOriginAllowed)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    });

            });

            services.AddDbContext<ApplicationDbContext>(options => options
            .UseNpgsql(Configuration.GetConnectionString("DefaultConnection"))
            .UseSnakeCaseNamingConvention());

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderInfoRepository, OrderInfoRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductInfoRepository, ProductInfoRepository>();
            services.AddScoped<ISmsRepository, SmsRepository>();

            // 設定排程
            var scheduler = StdSchedulerFactory.GetDefaultScheduler().GetAwaiter().GetResult();
            scheduler.JobFactory = new OrderJobFactory(services.BuildServiceProvider());
            services.AddSingleton(scheduler);
            services.AddHostedService<QuartzHostedService>();
            services.AddSingleton<OrderJob, OrderJob>();

            services.AddAutoMapper(typeof(HaikakinMappings));
            //防止密碼外洩
            services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });
            services.AddVersionedApiExplorer(options => options.GroupNameFormat = "'v'VVV");
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen();

            //JWT設定
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.JwtSecret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });
            //限制檔案上傳大小
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 10485760;
            });

            services.AddControllers()
                .AddNewtonsoftJson(opt =>
                {
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    //有NULL值時不回傳，如有異常再拿掉
                    opt.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            applicationLifetime.ApplicationStarted.Register(OnStratUp);
            applicationLifetime.ApplicationStopping.Register(OnShutDown);

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });

            app.UseHttpsRedirection();

            app.UseIpRateLimiting();

            app.UseSwagger(options =>
            {
                options.RouteTemplate = "/api/swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "api";
                foreach (var desc in provider.ApiVersionDescriptions)
                    options.SwaggerEndpoint($"swagger/{desc.GroupName}/swagger.json",
                    desc.GroupName.ToUpperInvariant());
            });

            app.UseCookiePolicy();

            app.UseRouting();

            app.UseCors("DepolyPolicy");

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void OnStratUp()
        {
        }

        private void OnShutDown()
        {
        }

        private static bool MyIsOriginAllowed(string origin)
        {
            var allowRange = new List<string>() {
            "https://www.haikakin.com",
            "http://localhost",
            "https://localhost",
            "https://ccore.newebpay.com/MPG/mpg_gateway",
            "https://core.newebpay.com/MPG/mpg_gateway",
            "https://postgate.ecpay.com.tw" };
            return allowRange.Contains(origin);
        }
    }
}
