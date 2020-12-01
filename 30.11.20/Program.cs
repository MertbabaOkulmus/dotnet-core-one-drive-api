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
        public const string CLIENT_ID =     "*********************";
        public const string CLIENT_SECRET = "********************";
        public const string TENANT_ID =     "*************************";
        public static string[] SCOPES = { "Files.ReadWrite.All" };
        public static GraphServiceClient graphClient = null;
        private DriveItem CurrentFolder { get; set; }

        // Verilen bağlam ve kaynak kimliği için bir erişim belirteci alın. İlk önce bir girişimde bulunulur
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
                    //  requestMessage.Headers.Add("SampleID", "uwp-csharp-apibrowser-sample");

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


        //Kullanıcı için Token alındığı kısım
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


        // Yükleme İçin Dosyanın path bilgilerini  alındığı kısım
        public static System.IO.Stream GetFileStreamForUpload(out string originalFilename)
        {

            try
            {   /*C:\Users\mertb\OneDrive\Masaüstü\deneme.pdf*/
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

            //Upload();
            Console.ReadKey();
        }       

    }
}