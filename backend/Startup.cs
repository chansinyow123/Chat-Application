using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using backend.Services;
using backend.Configs;
using backend.Static;
using backend.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Reflection;

namespace backend
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Add SQL Database ---------------------------------------------------------------------------------------------------
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    _configuration.GetConnectionString("DefaultConnection")
                )
            );

            // Add Identity ------------------------------------------------------------------------------------------------------
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add Services ------------------------------------------------------------------------------------------------------
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<INotificationService, NotificationService>();

            // Add Configuration -------------------------------------------------------------------------------------------------
            services.Configure<JWTConfig>(_configuration.GetSection("JWT"));
            services.Configure<MailKitConfig>(_configuration.GetSection("MailKit"));
            services.Configure<VapidConfig>(_configuration.GetSection("Vapid"));

            // Adding Authentication ---------------------------------------------------------------------------------------------
            services.AddAuthentication(options =>
            {
                // Identity made Cookie authentication the default.
                // However, we want JWT Bearer Auth to be the default.
                // we check the bearer to confirm that we are authenticated 
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                // Use this to check if we are allowed to do something
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                // The fallback for scheme if above is failed
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                // Scheme can be "ClientCookie" or "OurServer"
            })
            // Adding Jwt Bearer -------------------------------------------------------------------------------------------------
            .AddJwtBearer(options =>
            {
                // if we want to use url to access the JWT token
                options.Events = new JwtBearerEvents()
                {
                    // when receiving response -------------------------------------------------------------
                    //OnMessageReceived = context =>
                    //{
                    //    // if access-token is on the HttpOnly Cookie, Use it
                    //    if (context.Request.Cookies.ContainsKey("X-Access-Token"))
                    //    {
                    //        context.Token = context.Request.Cookies["X-Access-Token"];
                    //    }
                    //    return Task.CompletedTask;
                    //},
                    // Get Access Token from SignalR --------------------------------------------------------
                    // We have to hook the OnMessageReceived event in order to
                    // allow the JWT authentication handler to read the access
                    // token from the query string when a WebSocket or 
                    // Server-Sent Events request comes in.

                    // Sending the access token in the query string is required due to
                    // a limitation in Browser APIs. We restrict it to only calls to the
                    // SignalR hub in this code.
                    // See https://docs.microsoft.com/aspnet/core/signalr/security#access-token-logging
                    // for more information about security considerations when using
                    // the query string to transmit the access token.
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.ContainsKey("jwt_token"))
                        {
                            context.Token = context.Request.Cookies["jwt_token"];
                        }

                        return Task.CompletedTask;
                    },
                    // When JWT Token authentication failed ------------------------------------------------
                    OnAuthenticationFailed = context =>
                    {
                        // if access-token is expired, add to response header
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };

                options.SaveToken = true;
                options.RequireHttpsMetadata = true;

                // Get the JWT Config from appsetting.json file and then bind to JWTConfig class -----------
                var JWTConfig = _configuration.GetSection("JWT").Get<JWTConfig>();
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ClockSkew = TimeSpan.Zero,  // By default, jwt expiration takes 5 minute
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = JWTConfig.ValidAudience,
                    ValidIssuer = JWTConfig.ValidIssuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTConfig.Secret))
                };
            });

            // Configure Identity Options ------------------------------------------------------------------------------------------
            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 0;

                // User Settings
                options.User.RequireUniqueEmail = true;
            });

            // if forgot to migrate database, it will display migrate button on browser.
            services.AddDatabaseDeveloperPageExceptionFilter();
            
            // In Production, all file will be accessed in wwwroot ---------------------------------------------------------------
            services.AddSpaStaticFiles(options => { options.RootPath = "wwwroot"; });

            // only access controller --------------------------------------------------------------------------------------------
            services.AddControllers();

            // add signalr -------------------------------------------------------------------------------------------------------
            services.AddSignalR();

            // add automapper ----------------------------------------------------------------------------------------------------
            services.AddAutoMapper(typeof(Startup));

            // file upload configuration -----------------------------------------------------------------------------------------
            services.Configure<FormOptions>(options =>
            {
                // Set the limit to max
                options.MultipartBodyLengthLimit = int.MaxValue;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else {

                // May not need it, since we use SPA for pages
                // app.UseExceptionHandler("/Home/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();
            app.UseRouting();

            // Who are you?
            app.UseAuthentication();
            // Are you allowed?
            app.UseAuthorization();

            app.UseEndpoints(endpoints => 
            { 
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("api/chat-hub");
                endpoints.MapHub<VideoHub>("api/video-hub");
            });
            
            // In Production, all file will be accessed in wwwroot
            app.UseSpaStaticFiles();

            // In Development, proxy the url from .net to vuejs
            // eg: if url is not matched inside .net, then go search on vuejs and let vuejs handle it
            app.UseSpa(builder => {
                if (env.IsDevelopment()) {
                    builder.UseProxyToSpaDevelopmentServer("http://localhost:8080");
                }
            });

            CreateRoles(serviceProvider).Wait();
        }

        // When the apps first load, create the role in database, and also add a main Admin user
        // Here is the references link for this implementation: https://stackoverflow.com/questions/42471866/how-to-create-roles-in-asp-net-core-and-assign-them-to-users
        private async Task CreateRoles(IServiceProvider serviceProvider)
        {
            // Get the required service -------------------------------------------------------------------------------------
            var RoleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var UserManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // migrate the database if the database does not exist
            await dbContext.Database.MigrateAsync();

            // Convert the Static UserRoles Class into List -----------------------------------------------------------------
            // Here is the references link for this implementation:
            // https://stackoverflow.com/questions/41477862/linq-get-the-static-class-constants-as-list
            Type type = typeof(UserRoles);
            List<string> roles = type.GetFields().Select(x => x.GetValue(null).ToString()).ToList();

            foreach (var roleName in roles)
            {
                // if role is not exist, create the roles and seed into database
                if (!await RoleManager.RoleExistsAsync(roleName))
                {
                    await RoleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            string userPWD = _configuration["Admin:Password"];

            // Create an Admin User --------------------------------------------------------------------------------------
            var admin = new ApplicationUser
            {
                Name = _configuration["Admin:Name"],
                UserName = _configuration["Admin:Username"],
                Email = _configuration["Admin:Username"],
            };

            // Create Admin if no admin found in database ----------------------------------------------------------------
            var adminExist = await UserManager.FindByNameAsync(admin.UserName);

            if (adminExist == null)
            {

                await UserManager.CreateAsync(admin, userPWD);
                await UserManager.AddToRoleAsync(admin, UserRoles.Admin);
            }

            // Create a ST User -----------------------------------------------------------------------------------------
            var user = new ApplicationUser { Name = "Ally",  UserName = "ally@gmail.com", Email = "ally@gmail.com" };

            user.SentPrivateChats = new List<PrivateChat>
            {
                new PrivateChat { Message = "TestOne", OnCreate = DateTime.Now.AddDays(-2), Sender = user, Receiver = admin },
                new PrivateChat { Message = "TestTwo", OnCreate = DateTime.Now.AddDays(-1), Sender = user, Receiver = admin },
                new PrivateChat { Message = "TestSeven", OnCreate = DateTime.Now.AddDays(-1), Sender = user, Receiver = admin },
                new PrivateChat { Message = "TestEight", OnCreate = DateTime.Now.AddDays(-1), Sender = user, Receiver = admin },
                new PrivateChat { Message = "TestFive", OnCreate = DateTime.Now, Sender = user, Receiver = admin },
            };

            user.ReceivedPrivateChats = new List<PrivateChat>
            {
                new PrivateChat { Message = "TestThree", OnCreate = DateTime.Now, Sender = admin, Receiver = user },
                new PrivateChat { Message = "TestFour", OnCreate = DateTime.Now, Sender = admin, Receiver = user },
                new PrivateChat { Message = "TestSix", OnCreate = DateTime.Now, Sender = admin, Receiver = user },
            };

            // Create ST if no ST found in database ---------------------------------------------------------------------
            var userExist = await UserManager.FindByNameAsync(user.UserName);

            if (userExist == null)
            {
                await UserManager.CreateAsync(user, userPWD);
                await UserManager.AddToRoleAsync(user, UserRoles.ST);
            }

            // Create a ST User -----------------------------------------------------------------------------------------
            var userTwo = new ApplicationUser { Name = "Charles", UserName = "charles@gmail.com", Email = "charles@gmail.com" };

            userTwo.SentPrivateChats = new List<PrivateChat>
            {
                new PrivateChat { Message = "TwoTestOne", OnCreate = DateTime.Now.AddDays(-2), Sender = userTwo, Receiver = admin },
                new PrivateChat { Message = "TwoTestTwo", OnCreate = DateTime.Now.AddDays(-1), Sender = userTwo, Receiver = admin },
                new PrivateChat { Message = "TwoTestFive", OnCreate = DateTime.Now, Sender = userTwo, Receiver = admin },
            };

            userTwo.ReceivedPrivateChats = new List<PrivateChat>
            {
                new PrivateChat { Message = "TwoTestThree", OnCreate = DateTime.Now, Sender = admin, Receiver = userTwo },
                new PrivateChat { Message = "TwoTestFour", OnCreate = DateTime.Now, Sender = admin, Receiver = userTwo },
                new PrivateChat { Message = "TwoTestSix", OnCreate = DateTime.Now, Sender = admin, Receiver = userTwo },
            };

            // Create ST if no ST found in database ---------------------------------------------------------------------
            var userTwoExist = await UserManager.FindByNameAsync(userTwo.UserName);

            if (userTwoExist == null)
            {
                await UserManager.CreateAsync(userTwo, userPWD);
                await UserManager.AddToRoleAsync(userTwo, UserRoles.ST);
            }

            // Create a ST User -----------------------------------------------------------------------------------------
            var userThree = new ApplicationUser { Name = "Wan Theng", UserName = "wantheng@gmail.com", Email = "wantheng@gmail.com" };

            // Create ST if no ST found in database ---------------------------------------------------------------------
            var userThreeExist = await UserManager.FindByNameAsync(userThree.UserName);

            if (userThreeExist == null)
            {
                await UserManager.CreateAsync(userThree, userPWD);
                await UserManager.AddToRoleAsync(userThree, UserRoles.ST);
            }

            // Create a ST User -----------------------------------------------------------------------------------------
            var userFour = new ApplicationUser { Name = "Liaw", UserName = "liaw@gmail.com", Email = "liaw@gmail.com" };

            // Create ST if no ST found in database ---------------------------------------------------------------------
            var userFourExist = await UserManager.FindByNameAsync(userFour.UserName);

            if (userFourExist == null)
            {
                await UserManager.CreateAsync(userFour, userPWD);
                await UserManager.AddToRoleAsync(userFour, UserRoles.ST);
            }

            // Create a ST User -----------------------------------------------------------------------------------------
            var userFive = new ApplicationUser { Name = "Reno", UserName = "reno@gmail.com", Email = "reno@gmail.com" };

            // Create ST if no ST found in database ---------------------------------------------------------------------
            var userFiveExist = await UserManager.FindByNameAsync(userFive.UserName);

            if (userFiveExist == null)
            {
                await UserManager.CreateAsync(userFive, userPWD);
                await UserManager.AddToRoleAsync(userFive, UserRoles.ST);
            }

            // Reset all the user online count -------------------------------------------------------------------------
            var users = await dbContext.Users.ToListAsync();
            foreach (var u in users) {
                u.Online = 0;
            }
            await dbContext.SaveChangesAsync();
        }
    }
}
