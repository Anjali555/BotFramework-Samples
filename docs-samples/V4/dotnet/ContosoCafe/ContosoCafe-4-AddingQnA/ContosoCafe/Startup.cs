// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

namespace ContosoCafe
{
    using System;
    using System.Linq;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Ai.QnA;
    using Microsoft.Bot.Builder.BotFramework;
    using Microsoft.Bot.Builder.Integration;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Defines the startup type to be used by the web host for the bot.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">The web hosting information.</param>
        /// <remarks>This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940.
        /// </remarks>
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration properties for the app.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Called by the web host before the <see cref="Configure(IApplicationBuilder, IHostingEnvironment)"/>
        /// method to configure the app's services.
        /// </summary>
        /// <param name="services">The container to which to add services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Create and register the bot.
            services.AddBot<ContosoCafeBot>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(this.Configuration);
                options.OnTurnError = async (context, exception) =>
                {
                    await context.TraceActivityAsync($"{nameof(ContosoCafeBot)} exception", exception).ConfigureAwait(false);
                    await context.SendActivityAsync("Sorry, it looks like something went wrong!").ConfigureAwait(false);
                };

                // Add state management middleware.
                var storage = new MemoryStorage();
                var convState = new ConversationState(storage);
                options.Middleware.Add(convState);
            });

            // Create and register state accessors.
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value
                    ?? throw new InvalidOperationException(
                        "BotFrameworkOptions must be configured prior to setting up the state accessors.");

                var convState = options.Middleware.OfType<ConversationState>().FirstOrDefault()
                    ?? throw new InvalidOperationException(
                        "Conversation state must be defined and added before adding conversation-scoped state accessors.");

                return new StateAccessors
                {
                    ConvData = convState.CreateProperty<ConversationData>(StateAccessors.ConvDataName),
                };
            });

            // Create and register a QnA service and knowledgebase
            services.AddSingleton(sp =>
            {
                var section = this.Configuration.GetSection("QnA");
                var hostPath = section["HostPath"];
                var endpointKey = section["EndpointKey"];
                var knowledgebaseId = section["KnowledgebaseId"];
                var qnaOptions = float.TryParse(section["ScoreThreshold"], out float scoreThreshold)
                    ? new QnAMakerOptions { ScoreThreshold = scoreThreshold, Top = 1 } : null;

                return new QnAMaker(
                    new QnAMakerEndpoint
                    {
                        EndpointKey = endpointKey,
                        Host = hostPath,
                        KnowledgeBaseId = knowledgebaseId,
                    },
                    qnaOptions);
            });
        }

        /// <summary>
        /// Called by the web host to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder for the host.</param>
        /// <param name="env">Information about the web hosting environment.</param>
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
