using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitAddin.Tasks.Example.Revit.Commands;
using ricaun.Revit.UI.Tasks;

namespace RevitAddin.AdskAuth.Example.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public static IRevitTask RevitTask => revitTaskService;
        private static RevitTaskService revitTaskService;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {

            revitTaskService = new RevitTaskService(commandData.Application);
            revitTaskService.Initialize();
            // Execute the command
            Execute();

            return Result.Succeeded;
        }

        private async void Execute()
        {
            // replace with your own callback url
            string callBackUrl = "http://localhost:8080/api/auth/callback";
            // replace with your own client id
            string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            Globals.ClientId = clientId;
            Globals.CallbackURL = callBackUrl;
            await RevitTask.Run(() =>
            {

                string codeVerifier = RandomString(64);
                string codeChallenge = GenerateCodeChallenge(codeVerifier);
                Globals.codeVerifier = codeVerifier;
                RedirectToLogin(codeChallenge);
                return Task.CompletedTask;
            });
        }

        private static Random _random = new Random();


        private async void RedirectToLogin(string codeChallenge)
        {
            string[] prefixes = { "http://localhost:8080/api/auth/" };
            var url = $"https://developer.api.autodesk.com/authentication/v2/authorize?response_type=code&client_id={Globals.ClientId}&redirect_uri={HttpUtility.UrlEncode(Globals.CallbackURL)}&scope=data:read&prompt=login&code_challenge={codeChallenge}&code_challenge_method=S256";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
            await SimpleListenerExample(prefixes);
        }

        public async Task SimpleListenerExample(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("HttpListener is not supported in this context!");
            }

            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            listener.Start();
            //Console.WriteLine("Listening...");
            // Note: The GetContext method blocks while waiting for a request.
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;

            try
            {
                string authCode = request.Url.Query.ToString().Split('=')[1];
                await GetPKCEToken(authCode).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            // Construct a response.
            string responseString = "<HTML><BODY> You can move to the form!</BODY></HTML>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
            listener.Stop();
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());

            //Note: The use of the Random class makes this unsuitable for anything security related, such as creating passwords or tokens.Use the RNGCryptoServiceProvider class if you need a strong random number generator
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var b64Hash = Convert.ToBase64String(hash);
            var code = Regex.Replace(b64Hash, "\\+", "-");
            code = Regex.Replace(code, "\\/", "_");
            code = Regex.Replace(code, "=+$", "");
            return code;
        }

        private async Task GetPKCEToken(string authCode)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v2/token"),
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "client_id", Globals.ClientId },
                        { "code_verifier", Globals.codeVerifier },
                        { "code", authCode },
                        { "scope", "data:read" },
                        { "grant_type", "authorization_code" },
                        { "redirect_uri", Globals.CallbackURL }
                    }),
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    string bodystring = await response.Content.ReadAsStringAsync();
                    JObject bodyjson = JObject.Parse(bodystring);
                    Globals.AccessToken = bodyjson["access_token"]?.Value<string>();
                    Globals.RefreshToken = bodyjson["refresh_token"]?.Value<string>();
                    Console.WriteLine("Access Token: " + Globals.AccessToken);
                    Console.WriteLine("Refresh Token: " + Globals.RefreshToken);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }

}