using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Task = System.Threading.Tasks.Task;

namespace RemoveUnmanagedGuests
{
    internal class Program
    {
        private static readonly int MaxNoBatchItems = 20;

        private static readonly string[] Scopes = new[] { "User.Read", "User.Invite.All", "User.ReadWrite.All", "Directory.ReadWrite.All" };

        // set by configuration
        private static string? ClientId;
        private static string? TenantId;
        private static string? ResetRedemption;
        private static string? SendInvitationMessage;
        private static bool ResetAllGuestUsers;

        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.Write("Please enter your tenant identifier (instructions for finding it are available here: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-how-to-find-tenant");
                Console.ResetColor();
                Console.WriteLine("):");
                TenantId = Console.ReadLine();
                if (!Guid.TryParse(TenantId, out var guidTenantId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The tenant identifier should be a GUID");
                }
                else
                {
                    break;
                }
            }

            using IHost host = Host.CreateDefaultBuilder(args).Build();
            IConfiguration config = host.Services.GetRequiredService<IConfiguration>();
            ClientId = config.GetValue<string>("clientId");
            if (string.IsNullOrEmpty(ClientId))
            {
                throw new ArgumentNullException(nameof(ClientId));
            }

            PromptForSelection();

            Console.Write("Log in with a user that has these roles: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("User Administrator, Guest Inviter, Application Administrator and Directory Writer, or is a Global Administator");
            Console.ResetColor();
            Console.WriteLine();

            ResetAllGuestUsers = config.GetValue<bool>("resetAllGuestUsers");
            var clientApplication =
                PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();
            var result = await clientApplication.AcquireTokenWithDeviceCode(
                Scopes,
                deviceCodeResult =>
                {
                        // This will print the message on the console which tells the user where to go sign-in using
                        // a separate browser and the code to enter once they sign in.
                        // The AcquireTokenWithDeviceCode() method will poll the server after firing this
                        // device code callback to look for the successful login of the user via that browser.
                        // This background polling (whose interval and timeout data is also provided as fields in the
                        // deviceCodeCallback class) will occur until:
                        // * The user has successfully logged in via browser and entered the proper code
                        // * The timeout specified by the server for the lifetime of this code (typically ~15 minutes) has been reached
                        // * The developing application calls the Cancel() method on a CancellationToken sent into the method.
                        //   If this occurs, an OperationCanceledException will be thrown (see catch below for more details).
                        Console.WriteLine(deviceCodeResult.Message);
                    return Task.FromResult(0);
                }).ExecuteAsync();
            string accessToken = result.AccessToken;

            while (true)
            {
                GraphServiceClient graphClient = new(new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                        .Headers
                        .Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    return Task.CompletedTask;
                }));
                HttpClient httpClient = new();

                IList<UnmanagedUser> users = await GetUnmanagedUsers(graphClient, httpClient);
                await AddInvitationsInBatch(graphClient, users);

                PromptForSelection();
            }
        }

        private static void PromptForSelection()
        {
            Console.WriteLine();
            Console.WriteLine("Please choose an option:");
            Console.WriteLine("1 - reporting only (find out how many unmanaged users are in the tenant)");
            Console.WriteLine("2 - reset redemption status and send invitation email");
            Console.WriteLine("3 - reset redemption status but do NOT send invitation email");
            Console.WriteLine("Any other key to exit");
            Console.WriteLine();

            string option = Console.ReadLine();
            switch (option)
            {
                case "1":
                    SendInvitationMessage = "false";
                    ResetRedemption = "false";
                    break;
                case "2":
                    SendInvitationMessage = "true";
                    ResetRedemption = "true";
                    break;
                case "3":
                    SendInvitationMessage = "false";
                    ResetRedemption = "true";
                    break;
                default:
                    Environment.Exit(0);
                    return;
            }
        }

        private static async Task<IList<UnmanagedUser>> GetUnmanagedUsers(GraphServiceClient graphClient, HttpClient httpClient)
        {
            Console.WriteLine();
            Console.WriteLine("Looking for unmanaged users...");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("(This may take a few minutes, depending on the number of users in your tenant)");
            Console.ResetColor();
            Console.WriteLine();
            var response = await graphClient.Users.Request().Filter("userType eq 'Guest'").GetAsync();
            List<UnmanagedUser> unmanagedUserList = new();

            long guestUserCount = 0;
            await ProcessGuestUsers(httpClient, response.CurrentPage, unmanagedUserList);
            guestUserCount += response.CurrentPage.Count;
            while (response.NextPageRequest != null)
            {
                response = await response.NextPageRequest.GetAsync();
                await ProcessGuestUsers(httpClient, response.CurrentPage, unmanagedUserList);
                guestUserCount += response.CurrentPage.Count;
            }

            string fileName = "UnmanagedUsers.csv";
            using StreamWriter file = new(fileName);
            Console.WriteLine($"Found {guestUserCount} guest users and {unmanagedUserList.Count} viral users. See {Path.GetFullPath($"./{fileName}")} for details.");
            foreach (var user in unmanagedUserList)
            {
                file.WriteLine($"{user.Id},{user.Email}");
            }
            return unmanagedUserList;
        }

        private static async Task ProcessGuestUsers(HttpClient httpClient, IList<User> users, List<UnmanagedUser> viralUserList)
        {
            if (ResetAllGuestUsers)
            {
                foreach (var user in users)
                {
                    viralUserList.Add(new UnmanagedUser { Id = user.Id, Email = user.Mail });
                }
            }
            else
            {
                foreach (var user in users.Where(item => !string.IsNullOrEmpty(item.Mail)))
                {
                    if (!user.Identities.Any(item => item.Issuer == "ExternalAzureAD"))
                    {
                        continue;
                    }

                    string urlEncodedMail = HttpUtility.UrlEncode(user.Mail);
                    HttpResponseMessage response = await httpClient.SendAsync(
                        new HttpRequestMessage(
                            HttpMethod.Get,
                            $"https://login.microsoftonline.com/common/userrealm?user={urlEncodedMail}&api-version=2.1"));
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error response {response.StatusCode} - {response.ReasonPhrase} for {user.Mail}");
                        Console.ResetColor();
                    }

                    string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (responseContent.Contains(@"""IsViral"":true", StringComparison.OrdinalIgnoreCase))
                    {
                        viralUserList.Add(new UnmanagedUser { Id = user.Id, Email = user.Mail });
                    }
                }
            }
        }

        private static async Task AddInvitationsInBatch(GraphServiceClient graphClient, IList<UnmanagedUser> users)
        {
            if (ResetRedemption == "false" && SendInvitationMessage == "false")
            {
                return;
            }

            List<BatchRequestContent> batches = new();
            var batchRequestContent = new BatchRequestContent();
            for (int index = 0; index < users.Count; ++index)
            {
                var item = users[index];
                string content = $@"{{
    ""invitedUserEmailAddress"": ""{item.Email}"",
    ""inviteRedirectUrl"": ""https://myapps.microsoft.com?tenantId={TenantId}"",
    ""resetRedemption"": {ResetRedemption},
    ""sendInvitationMessage"": {SendInvitationMessage},
    ""invitedUser"": {{
        ""id"": ""{item.Id}"",
        ""@odata.type"": ""microsoft.graph.user""
    }}
            }}
            ";
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://graph.microsoft.com/beta/invitations")
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                };

                BatchRequestStep requestStep = new(index.ToString(), httpRequestMessage, null);
                batchRequestContent.AddBatchRequestStep(requestStep);

                if (index > 0 && index % MaxNoBatchItems == 0)
                {
                    batches.Add(batchRequestContent);
                    batchRequestContent = new BatchRequestContent();
                }
            }

            if (batchRequestContent.BatchRequestSteps.Count < MaxNoBatchItems)
            {
                batches.Add(batchRequestContent);
            }

            if (batches.Count == 0 && batchRequestContent != null)
            {
                batches.Add(batchRequestContent);
            }

            Console.WriteLine($"{DateTime.UtcNow} Sending invitations...");
#if DEBUG
            Console.WriteLine($"{DateTime.UtcNow} {batches.Count} batches created. Submitting...");
#endif
            List<(string invitationId, string statusCode, string reasonPhrase)> failedInvitations = new();
            List<string> successfulInvitations = new();
            foreach (BatchRequestContent batch in batches)
            {
                BatchResponseContent response;
                try
                {
                    response = await graphClient.Batch.Request().PostAsync(batch);
                }
                catch (ClientException ex)
                {
                    Console.WriteLine($"Encountered an error: {ex.Message}");
                    continue;
                }

                Dictionary<string, HttpResponseMessage> responses = await response.GetResponsesAsync();
                foreach (string key in responses.Keys)
                {
                    HttpResponseMessage httpResponse = await response.GetResponseByIdAsync(key);
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var jsonContent = JsonObject.Parse(responseContent);
                    var invitationId = (string)jsonContent["id"];
                    if (!string.IsNullOrEmpty(invitationId) && !httpResponse.IsSuccessStatusCode)
                    {
                        (string, string, string) tuple = (invitationId, responses[key].StatusCode.ToString(), responses[key].ReasonPhrase);
                        failedInvitations.Add(tuple);
                    }
                    else
                    {
                        successfulInvitations.Add(invitationId);
                    }
#if DEBUG
                    Console.WriteLine($"Response code: {responses[key].StatusCode}-{responses[key].ReasonPhrase}-{invitationId}");
#endif
                }

#if DEBUG
                Console.WriteLine($"{DateTime.UtcNow} Done processing batch");
#endif
            }

            Console.WriteLine($"Done. Failed invitations: {failedInvitations.Count}. Successful invitations: {successfulInvitations.Count}");
            const string FailedInvitationsFileName = "FailedInvitations.csv";
            if (failedInvitations.Count > 0)
            {
                Console.WriteLine($"See {FailedInvitationsFileName} for failure details.");
                using StreamWriter file = new(FailedInvitationsFileName);
                foreach (var (invitationId, statusCode, reasonPhrase) in failedInvitations)
                {
                    file.WriteLine($"{invitationId},{statusCode},{reasonPhrase}");
                }
            }
        }

        private class UnmanagedUser
        {
            public string Id { get; set; }
            public string Email { get; set; }
        }
    }
}