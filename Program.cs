using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

// Dokobit Portal API Example - Mobile ID Signing
namespace DokobitDotnetExample
{
    class Program
    {
        public class Api
        {
            public static string accessToken = ""; //Enter Your Dokobit Portal API access token
            
        }

        public class Endpoint
        {
            public static string endpoint = "https://beta.dokobit.com/api/";

            public static string SigningCreate(string accessToken)
            {
                return endpoint + "signing/create.json?access_token=" + accessToken;
            }

            public static string SigningDownload(string token, string accessToken)
            {
                return endpoint + "signing/" + token + "/download?access_token=" + accessToken;
            }

            public static string SigningStatus(string token, string accessToken)
            {
                return endpoint + "signing/" + token + "/status.json?access_token=" + accessToken;
            }
        }


        [DataContract]
        public class Response
        {
            [DataMember(Name = "status")]
            public string Status { get; set; }
            [DataMember(IsRequired = false, Name = "message")]
            public string Message { get; set; }
            [DataMember(IsRequired = false, Name = "errors")]
            public IEnumerable<string> Errors { get; set; }
            [DataMember(IsRequired = false, Name = "code")]
            public string Code { get; set; }
        }

        [DataContract, KnownType(typeof(Response))]
        public class SigningCreateResponse : Response
        {
            [DataMember(IsRequired = false, Name = "token")]
            public string Token { get; set; }

        }

        public static SigningCreateResponse CreateSigning(byte[] document, string email, string code)
        {
            using (var client = new HttpClient())
            {
                using (var content =
                    new MultipartFormDataContent("Upload----" + DateTime.Now))
                {
                    content.Add(new StringContent("pdf"), "type");
                    content.Add(new StringContent("Agreement"), "name");
                    content.Add(new StringContent("test.pdf"), "files[0][name]");
                    content.Add(new StringContent(Convert.ToBase64String(document)), "files[0][content]");
                    content.Add(
                        new StringContent(
                            BitConverter.ToString(SHA256.Create().ComputeHash(document)).Replace("-", "").ToLower()),
                        "files[0][digest]");
                    content.Add(new StringContent("John"), "signers[0][name]");
                    content.Add(new StringContent("Doe"), "signers[0][surname]");
                    content.Add(new StringContent(email), "signers[0][email]");
                    content.Add(new StringContent("false"), "require_account");
                    if (code.Length > 0)
                    {
                        // if you do not want to specify exact person for document signing and require login in case require_account=false is used comment these lines 
                        content.Add(new StringContent(code), "signers[0][code]"); 
                        content.Add(new StringContent("lt"), "signers[0][country_code]");
                    }


                    using (
                        var message =
                            client.PostAsync(Endpoint.SigningCreate(Api.accessToken),
                                content))
                    {
                        var input = message.Result;
                        var serializator = new DataContractJsonSerializer(typeof(SigningCreateResponse));
                        return (SigningCreateResponse)serializator.ReadObject(input.Content.ReadAsStreamAsync().Result);
                    }
                }
            }
        }

        public static void printResponse(Response response)
        {
            if (response != null)
            {
                if (response.Status != null)
                {
                    Console.WriteLine("Status: " + response.Status + "\n");
                }

                if (response.Message != null)
                {
                    Console.WriteLine("Message: " + response.Message + "\n");
                }

                if (response.Errors != null && response.Errors.Count() > 0)
                {
                    Console.WriteLine("Errors:\n");
                    foreach (var error in response.Errors)
                    {
                        Console.WriteLine("\t" + error + "\n");
                    }
                }
            }
            else
            {
                Console.WriteLine("Failed to receive response\n");
            }
        }

        static void Main(string[] args)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            string fileName = args.Length > 0 ? args[0] : @"../../test.pdf"; // example pdf file to sign
            string email = args.Length > 1 ? args[1] : "test+dotnet@dokobit.com"; // enter signers email to receive invitation
            string code = args.Length > 2 ? args[2] : ""; // enter personal code, please note current variable is country_code=lt

            byte[] contentData = System.IO.File.ReadAllBytes(fileName);
            var response = CreateSigning(contentData, email, code);

            if (response.Status == "ok")
            {

                Console.WriteLine("Dokobit Portal API signing creation example:");
 
                Console.WriteLine("\nIf you want to check Your signing status, use: " + Endpoint.SigningStatus(response.Token, Api.accessToken));

                Console.WriteLine("\nIf you want to download Your signing, please visit in browser: " + Endpoint.SigningDownload(response.Token, Api.accessToken));

            }
            else
            {
                printResponse(response);
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}
