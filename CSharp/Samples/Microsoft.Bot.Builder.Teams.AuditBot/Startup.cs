﻿// <copyright file="Startup.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Bot.Builder.Teams.AuditBot
{
    using System;
    using System.Linq;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Bot.Builder.Abstractions;
    using Microsoft.Bot.Builder.Abstractions.Teams;
    using Microsoft.Bot.Builder.Integration;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Builder.Teams.Middlewares;
    using Microsoft.Bot.Builder.Teams.StateStorage;
    using Microsoft.Bot.Configuration;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Starts up the web application.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Is current hosting environment production.
        /// </summary>
        private bool isProduction = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">The hosting env.</param>
        public Startup(IHostingEnvironment env)
        {
            this.isProduction = env.IsProduction();

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures the services. This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            string secretKey = this.Configuration.GetSection("botFileSecret")?.Value;
            string botFilePath = this.Configuration.GetSection("botFilePath")?.Value;

            // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            BotConfiguration botConfig = null;
            try
            {
                botConfig = BotConfiguration.Load(botFilePath ?? @".\BotConfiguration.bot", secretKey);
            }
            catch
            {
                string msg = "Error reading bot file. Please ensure you have valid botFilePath and botFileSecret set for your environment.\n" +
                    " - The botFileSecret is available under appsettings for your Azure Bot Service bot.\n" +
                    " - If you are running this bot locally, consider adding a appsettings.json file with botFilePath and botFileSecret.\n" +
                    " - See https://aka.ms/about-bot-file to learn more about .bot file its use and bot configuration.\n\n";
                throw new InvalidOperationException(msg);
            }

            services.AddSingleton(sp => botConfig);

            // Retrieve current endpoint.
            string environment = this.isProduction ? "production" : "development";
            ConnectedService botService = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == environment).FirstOrDefault();
            if (!(botService is EndpointService endpointService))
            {
                throw new InvalidOperationException($"The .bot file does not contain an endpoint with name '{environment}'.");
            }

            services.AddTransient<IActivityProcessor, TeamsActivityProcessor>();
            services.AddTransient<ITeamsConversationUpdateActivityHandler, TeamsConversationUpdateActivityHandler>();
            services.AddTransient<IMessageActivityHandler, MessageActivityHandler>();

            services.AddBot<AuditBot>(options =>
            {
                IStorage dataStore = new MemoryStorage();

                // --> Adding conversation state handler which understands a team as single conversation.
                options.State.Add(new TeamSpecificConversationState(dataStore));

                options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                // Drop all activities not coming from Microsoft Teams.
                options.Middleware.Add(new DropNonTeamsActivitiesMiddleware());

                // --> Add Teams Middleware.
                options.Middleware.Add(
                    new TeamsMiddleware(
                        options.CredentialProvider));

                // Drop all non team messages.
                options.Middleware.Add(new DropChatActivitiesMiddleware());
            });

            services.AddSingleton(sp =>
            {
                BotFrameworkOptions options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions must be configured prior to setting up the State Accessors");
                }

                TeamSpecificConversationState conversationState = options.State.OfType<TeamSpecificConversationState>().FirstOrDefault();
                if (conversationState == null)
                {
                    throw new InvalidOperationException("ConversationState must be defined and added before adding conversation-scoped state accessors.");
                }

                // Create the custom state accessor.
                // State accessors enable other components to read and write individual properties of state.
                var accessors = new AuditLogAccessor(conversationState)
                {
                    AuditLog = conversationState.CreateProperty<TeamOperationHistory>(AuditLogAccessor.AuditLogName),
                };

                return accessors;
            });
        }

        /// <summary>
        /// Configures the specified application. This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The hosting env.</param>
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
