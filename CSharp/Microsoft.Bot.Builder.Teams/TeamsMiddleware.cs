﻿// <copyright file="TeamsMiddleware.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Bot.Builder.Teams
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Rest.TransientFaultHandling;

    /// <summary>
    /// Teams Middleware. This middleware needs to be registered in the Middleware pipeline at the time of Adapter initialization.
    /// </summary>
    /// <seealso cref="IMiddleware" />
    public class TeamsMiddleware : IMiddleware
    {
        /// <summary>
        /// The application credential map. This is used to ensure we don't try to get tokens for Bot everytime.
        /// </summary>
        private readonly ConcurrentDictionary<string, MicrosoftAppCredentials> appCredentialMap =
            new ConcurrentDictionary<string, MicrosoftAppCredentials>();

        /// <summary>
        /// The credential provider.
        /// </summary>
        private readonly ICredentialProvider credentialProvider;

        /// <summary>
        /// The connector client retry policy.
        /// </summary>
        private readonly RetryPolicy connectorClientRetryPolicy;

        /// <summary>
        /// The delegating handler used to process requests.
        /// </summary>
        private readonly DelegatingHandler delegatingHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsMiddleware"/> class.
        /// </summary>
        /// <param name="credentialProvider">The credential provider.</param>
        /// <param name="connectorClientRetryPolicy">The connector client retry policy.</param>
        /// <param name="delegatingHandler">The delegating handler.</param>
        public TeamsMiddleware(
            ICredentialProvider credentialProvider,
            RetryPolicy connectorClientRetryPolicy = null,
            DelegatingHandler delegatingHandler = null)
        {
            this.credentialProvider = credentialProvider;
            this.connectorClientRetryPolicy = connectorClientRetryPolicy;
            this.delegatingHandler = delegatingHandler;
        }

        /// <summary>
        /// Processess an incoming activity and if it is for MsTeams attaches <see cref="ITeamsExtensions"/> instances along with the context.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        /// <param name="nextDelegate">The delegate to call to continue the bot middleware pipeline.</param>
        /// <returns>
        /// Task tracking operation.
        /// </returns>
        /// <remarks>
        /// Middleware calls the <paramref name="nextDelegate" /> delegate to pass control to
        /// the next middleware in the pipeline. If middleware doesn’t call the next delegate,
        /// the adapter does not call any of the subsequent middleware’s request handlers or the
        /// bot’s receive handler, and the pipeline short circuits.
        /// <para>The <paramref name="context" /> provides information about the
        /// incoming activity, and other data needed to process the activity.</para>
        /// </remarks>
        /// <seealso cref="ITurnContext" />
        /// <seealso cref="Schema.IActivity" />
#pragma warning disable UseAsyncSuffix // Use Async suffix
        public async Task OnTurn(ITurnContext context, MiddlewareSet.NextDelegate nextDelegate)
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            BotAssert.ContextNotNull(context);

            if (context.Activity.ChannelId.Equals("msteams", StringComparison.OrdinalIgnoreCase))
            {
                // BotFrameworkAdapter when processing activity, post Auth adds BotIdentity into the context.
                ClaimsIdentity claimsIdentity = context.Services.Get<ClaimsIdentity>("BotIdentity");

                // If we failed to find ClaimsIdentity, create a new AnonymousIdentity. This tells us that Auth is off.
                if (claimsIdentity == null)
                {
                    claimsIdentity = new ClaimsIdentity(new List<Claim>(), "anonymous");
                }

                ITeamsConnectorClient teamsConnectorClient = await this.CreateConnectorClientAsync(context.Activity.ServiceUrl, claimsIdentity).ConfigureAwait(false);

                context.Services.Add((ITeamsExtensions)new TeamsExtensions(context, teamsConnectorClient));
            }

            await nextDelegate().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the connector client asynchronous.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="claimsIdentity">The claims identity.</param>
        /// <returns>ConnectorClient instance.</returns>
        /// <exception cref="NotSupportedException">ClaimsIdemtity cannot be null. Pass Anonymous ClaimsIdentity if authentication is turned off.</exception>
        private async Task<ITeamsConnectorClient> CreateConnectorClientAsync(string serviceUrl, ClaimsIdentity claimsIdentity)
        {
            if (claimsIdentity == null)
            {
                throw new NotSupportedException("ClaimsIdemtity cannot be null. Pass Anonymous ClaimsIdentity if authentication is turned off.");
            }

            // For requests from channel App Id is in Audience claim of JWT token. For emulator it is in AppId claim. For
            // unauthenticated requests we have anonymouse identity provided auth is disabled.
            var botAppIdClaim = claimsIdentity.Claims?.SingleOrDefault(claim => claim.Type == AuthenticationConstants.AudienceClaim)
                ??
                claimsIdentity.Claims?.SingleOrDefault(claim => claim.Type == AuthenticationConstants.AppIdClaim);

            // For anonymous requests (requests with no header) appId is not set in claims.
            if (botAppIdClaim != null)
            {
                string botId = botAppIdClaim.Value;
                var appCredentials = await this.GetAppCredentialsAsync(botId).ConfigureAwait(false);
                return this.CreateConnectorClient(serviceUrl, appCredentials);
            }
            else
            {
                return this.CreateConnectorClient(serviceUrl);
            }
        }

        /// <summary>
        /// Gets the application credentials. App Credentials are cached so as to ensure we are not refreshing
        /// token everytime.
        /// </summary>
        /// <param name="appId">The application identifier (AAD Id for the bot).</param>
        /// <returns>App credentials.</returns>
        private async Task<MicrosoftAppCredentials> GetAppCredentialsAsync(string appId)
        {
            if (appId == null)
            {
                return MicrosoftAppCredentials.Empty;
            }

            if (!this.appCredentialMap.TryGetValue(appId, out var appCredentials))
            {
                string appPassword = await this.credentialProvider.GetAppPasswordAsync(appId).ConfigureAwait(false);
                appCredentials = new MicrosoftAppCredentials(appId, appPassword);
                this.appCredentialMap[appId] = appCredentials;
            }

            return appCredentials;
        }

        /// <summary>
        /// Creates the connector client.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="appCredentials">The application credentials for the bot.</param>
        /// <returns>Connector client instance.</returns>
        private ITeamsConnectorClient CreateConnectorClient(string serviceUrl, MicrosoftAppCredentials appCredentials = null)
        {
            TeamsConnectorClient connectorClient;

            if (appCredentials == null)
            {
                appCredentials = new MicrosoftAppCredentials(appId: null, password: null);
            }

            if (this.delegatingHandler == null)
            {
                connectorClient = new TeamsConnectorClient(new Uri(serviceUrl), appCredentials);
            }
            else
            {
                connectorClient = new TeamsConnectorClient(new Uri(serviceUrl), appCredentials, this.delegatingHandler);
            }

            if (this.connectorClientRetryPolicy != null)
            {
                connectorClient.SetRetryPolicy(this.connectorClientRetryPolicy);
            }

            return connectorClient;
        }
    }
}
