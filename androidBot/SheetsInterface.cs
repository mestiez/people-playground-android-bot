using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace AndroidBot
{
    public static class SheetsInterface
    {
        public static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        public static string ApplicationName = "Biscuit bot";
        public const string CredentialsPath = "credentials.json";

        private static UserCredential credential;
        private static SheetsService service;

        // Ziek hard gekopieerd van https://developers.google.com/sheets/api/quickstart/dotnet
        public static void Authenticate()
        {
            using (var stream =new FileStream(CredentialsPath, FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
        }

        public static void InitialiseService()
        {
            if (credential == null)
                throw new Exception("Attempt to initialise service without credentials");

            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            }); ;
        }

        public static IList<IList<object>> GetValues(string spreadsheetId, string range)
        {
            var request =  service.Spreadsheets.Values.Get(spreadsheetId, range);
            var v = request.Execute();
            return v.Values;
        }
    }
}
