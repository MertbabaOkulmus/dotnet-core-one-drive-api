using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.IO;
using Microsoft.Azure.AppService.ApiApps.Service;

namespace _30._11._20
{
    class Program
    {
       
        private enum ClientType
        {
            Consumer,
            Business
        }
        private ClientType clientType { get; set; }

        public const string CLIENT_ID =     "*********************";
        public const string CLIENT_SECRET = "********************";
        public const string TENANT_ID =     "*************************";
        public static string[] SCOPES = { "Files.ReadWrite.All" };
        public static GraphServiceClient graphClient = null;
       //public static string TokenForUser = null;
        private DriveItem CurrentFolder { get; set; }
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
        public static async Task<string> GetTokenForUserAsync()
        {
            var confidentialClient = ConfidentialClientApplicationBuilder
                      .Create(CLIENT_ID)
                      .WithAuthority($"https://login.microsoftonline.com/$TENANT_ID/v2.0")
                      .WithClientSecret(CLIENT_SECRET)
                      .Build();

            var authResult = await confidentialClient
                              .AcquireTokenForClient(SCOPES)
                              .ExecuteAsync();

            return authResult.AccessToken;
        }

        public static System.IO.Stream GetFileStreamForUpload(out string originalFilename)
        {

            try
            {/*C:\Users\mertb\OneDrive\Masaüstü\EnXwDWrXMAAyC7h.jfif*/
                originalFilename = System.IO.Path.GetFileName("C:\\Users\\mertb\\OneDrive\\Masaüstü\\EnXwDWrXMAAyC7h.jfif");
                return new System.IO.FileStream("C:\\Users\\mertb\\OneDrive\\Masaüstü\\EnXwDWrXMAAyC7h.jfif", System.IO.FileMode.Open);
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
            var targetFolder = new Program().CurrentFolder;
            string filename;
            using (var stream = GetFileStreamForUpload(out filename))
            {
                if (stream != null)
                {
                    string folderPath = targetFolder.ParentReference == null
                        ? ""
                        : targetFolder.ParentReference.Path.Remove(0, 12) + "/" + Uri.EscapeUriString("deneme");
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
            graphClient = GetAuthenticatedClient();
            var users = graphClient.Users
            .Request()
            .GetAsync().GetAwaiter();

            Upload();
            Console.ReadKey();
        }

        //---------------------------------------
        public async Task SignIn()
        {

            try
            {
                graphClient = GetAuthenticatedClient();
            }
            catch (ServiceException exception)
            {

                PresentServiceException(exception);

            }

            try
            {
                await LoadFolderFromPath();

               // UpdateConnectedStateUx(true);
            }
            catch (ServiceException exception)
            {
                PresentServiceException(exception);
                graphClient = null;
            }
        }
        public async Task LoadFolderFromPath(string path = null)
        {
            if (null == graphClient) return;

            // Update the UI for loading something new
            

            try
            {
                DriveItem folder;

                var expandValue = this.clientType == ClientType.Consumer
                    ? "thumbnails,children($expand=thumbnails)"
                    : "thumbnails,children";

                if (path == null)
                {
                    folder = await graphClient.Drive.Root.Request().Expand(expandValue).GetAsync();
                }
                else
                {
                    folder =
                        await
                                 graphClient.Drive.Root.ItemWithPath("/" + path)
                                .Request()
                                .Expand(expandValue)
                                .GetAsync();
                }

                ProcessFolder(folder);
            }
            catch (Exception exception)
            {
                PresentServiceException(exception);
            }

            
        }

       

        private static void PresentServiceException(Exception exception)
        {
            string message = null;
            var oneDriveException = exception as ServiceException;
            if (oneDriveException == null)
            {
                message = exception.Message;
            }
            else
            {
                message = string.Format("{0}{1}", Environment.NewLine, oneDriveException.ToString());
            }

            Console.WriteLine(string.Format("OneDrive reported the following error: {0}", message));
        }
        private void ProcessFolder(DriveItem folder)
        {
            if (folder != null)
            {
                this.CurrentFolder = folder;

                LoadProperties(folder);

            }
        }
        private void LoadProperties(DriveItem item)
        {
            
        }

    }
}