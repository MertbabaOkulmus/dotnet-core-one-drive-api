using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.IO;

namespace _30._11._20
{
    class Program
    {
        public const string clientId = "56606873-1d18-4a7d-b3c6-83f39c3a213a";
        public static string[] Scopes = { "Files.ReadWrite.All" };
        public static GraphServiceClient graphClient = null;
        public static PublicClientApplication IdentityClientApp = new PublicClientApplication(clientId);
        public static string TokenForUser = null;
        public static DateTimeOffset Expiration;
        private ClientType clientType { get; set; }
        private DriveItem CurrentFolder { get; set; }
        private DriveItem SelectedItem { get; set; }
        public static GraphServiceClient GetAuthenticatedClient()
        {
            if (graphClient == null)
            {
                // Create Microsoft Graph client.
                try
                {
                    graphClient = new GraphServiceClient(
                        "https://graph.microsoft.com/v1.0",
                        new DelegateAuthenticationProvider(
                            async (requestMessage) =>
                            {
                                var token = await GetTokenForUserAsync();
                                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                                // This header has been added to identify our sample in the Microsoft Graph service.  If extracting this code for your project please remove.
                                requestMessage.Headers.Add("SampleID", "uwp-csharp-apibrowser-sample");

                            }));
                    return graphClient;
                }

                catch (Exception ex)
                {
                    Debug.WriteLine("Could not create a graph client: " + ex.Message);
                }
            }

            return graphClient;
        }
        public static  async Task<string> GetTokenForUserAsync()
        {
            AuthenticationResult authResult;
            try
            {
                authResult = await IdentityClientApp.AcquireTokenSilentAsync(Scopes);
                TokenForUser = authResult.IdToken;// token id token ile değiştirlidi 
            }

            catch (Exception)
            {
                if (TokenForUser == null || Expiration <= DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    authResult = await IdentityClientApp.AcquireTokenAsync(Scopes);

                    TokenForUser = authResult.IdToken;
                    Expiration = authResult.ExpiresOn;
                }
            }

            return TokenForUser;
        }

       public static System.IO.Stream GetFileStreamForUpload(string targetFolderName, out string originalFilename)
        {
           // OpenFileDialog dialog = new OpenFileDialog();
           
            //dialog.Title = "Upload to " + targetFolderName;
            //dialog.Filter = "All Files (*.*)|*.*";
            //dialog.CheckFileExists = true;
            //var response = dialog.ShowDialog();
            //if (response != DialogResult.OK)
            //{
            //    originalFilename = null;
            //    return null;
            //}

            try
            {
                originalFilename = System.IO.Path.GetFileName("");
                return new System.IO.FileStream("", System.IO.FileMode.Open);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error uploading file: " + ex.Message);
                originalFilename = null;
                return null;
            }
        }


        public static async void Upload()
        {
            var targetFolder = this.CurrentFolder;
            string filename;
            using (var stream = GetFileStreamForUpload(targetFolder.Name, out filename))
            {
                if (stream != null)
                {
                    // Since the ItemWithPath method is available only at Drive.Root, we need to strip
                    // /drive/root: (12 characters) from the parent path string.
                    string folderPath = targetFolder.ParentReference == null
                        ? ""
                        : targetFolder.ParentReference.Path.Remove(0, 12) + "/" + Uri.EscapeUriString(targetFolder.Name);
                    var uploadPath = folderPath + "/" + Uri.EscapeUriString(System.IO.Path.GetFileName(filename));

                    
                        var uploadedItem =
                            await
                               graphClient.Drive.Root.ItemWithPath(uploadPath).Content.Request().PutAsync<DriveItem>(stream);

                        Console.WriteLine("Uploaded with ID: " + uploadedItem.Id);
                    
                }
            }
        }
        static void Main(string[] args)
        {
            graphClient =GetAuthenticatedClient();
            Upload();
            Console.ReadKey();
        }
    }
}
