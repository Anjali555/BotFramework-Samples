using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace ContosoCafe
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBot<ContosoCafeBot>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(Configuration);

                // Trap exceptions and display them (in the Emulator), but show the user a friendly error message.
                options.Middleware.Add(new CatchExceptionMiddleware<Exception>(async (context, exception) =>
                {
                    await context.TraceActivity($"{nameof(ContosoCafeBot)} Exception", exception);
                    await context.SendActivity("Sorry, it looks like something went wrong!");
                }));

                // Use file storage and set up a dedicated directory for the bot.
                var path = Path.Combine(Path.GetTempPath(), nameof(ContosoCafeBot));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                IStorage dataStore = new FileStorage(path);

                options.Middleware.Add(new ConversationState<ConversationData>(dataStore));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
