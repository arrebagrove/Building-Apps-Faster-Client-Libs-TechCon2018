using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Task.Run(async () =>
            {
                await RunAsync();
            }).Wait();
            Console.Write("Press any key to continue...");
            Console.ReadKey();

        }

        private static async Task RunAsync()
        {
            //Example 1 - Connecting to PI Web API
            string baseUrl = "https://localhost/piwebapi";
            JObject homeLanding = await MakeRequest(baseUrl, "GET");
            Console.WriteLine("System Link is {0}", homeLanding["Links"]["System"].ToString());

            //Example 2 - Getting the PI Objects Web ID
            JObject dataServer = await MakeRequest(baseUrl + "/dataservers?name=SATURN-MARCOS", "GET");
            JObject sinusoidPoint = await MakeRequest(baseUrl + "/points?path=\\\\SATURN-MARCOS\\sinusoid", "GET");
            JObject element = await MakeRequest(baseUrl + "/elements?path=\\\\SATURN-MARCOS\\Talk\\Element1", "GET");
            JObject attribute = await MakeRequest(baseUrl + "/attributes?path=\\\\SATURN-MARCOS\\Talk\\Element1|Attribute1", "GET");

            Console.WriteLine("The PI Data Server Web ID is {0} and the PI Point Web ID is {1}", dataServer["WebId"], sinusoidPoint["WebId"]);

            ////Example 3 - Handling exceptions
            try
            {
                JObject homeLanding2 = await MakeRequest("https://localhost/piwebapi/", "GET");
            }
            catch (Exception e)
            {
                Console.WriteLine("Expected error: {0}", e.Message);
            }

            try
            {
                JObject point = await MakeRequest(baseUrl + "/points?path=\\\\SATURN-MARCOS\\sinusoid3424", "GET");
            }
            catch (Exception e)
            {
                Console.WriteLine("Expected error: {0}", e.Message);
            }

            ////Example 4 - Change the description of the PI Point
            string webId = sinusoidPoint["WebId"].ToString();
            JObject updatePoint = new JObject();
            updatePoint["Descriptor"] = "New description";
            await MakeRequest(baseUrl + "/points/" + webId, "PATCH", updatePoint);

            ////Example 5 - Retrieving data in bulk
            JObject point1 = await MakeRequest(baseUrl + "/points?path=\\\\SATURN-MARCOS\\sinusoid", "GET");
            JObject point2 = await MakeRequest(baseUrl + "/points?path=\\\\SATURN-MARCOS\\sinusoidu", "GET");
            JObject point3 = await MakeRequest(baseUrl + "/points?path=\\\\SATURN-MARCOS\\cdt158", "GET");

            string serviceUrl = string.Format("/streamsets/recorded?webId={0}&webId={1}&webId={2}", point1["WebId"], point2["WebId"], point3["WebId"]);
            JObject piItemsStreamValues = await MakeRequest(baseUrl + serviceUrl, "GET");

            foreach (var piStreamValues in piItemsStreamValues["Items"])
            {
                foreach (var value in piStreamValues["Items"])
                {
                    Console.WriteLine("PI Point: {0}, Value: {1}, Timestamp: {2}", piStreamValues["Name"], value["Value"], value["Timestamp"]);
                }
            }


            ////Example 6 - Sending data in bulk

            JObject streamValue1 = new JObject();
            JObject streamValue2 = new JObject();
            JObject streamValue3 = new JObject();
            JObject value1 = new JObject();
            JObject value2 = new JObject();
            JObject value3 = new JObject();
            JObject value4 = new JObject();
            JObject value5 = new JObject();
            JObject value6 = new JObject();
            value1["Value"] = 2;
            value1["Timestamp"] = "*-1d";
            value2["Value"] = 3;
            value2["Timestamp"] = "*-2d";
            value3["Value"] = 4;
            value3["Timestamp"] = " * -1d";
            value4["Value"] = 5;
            value4["Timestamp"] = " * -2d";
            value5["Value"] = 6;
            value5["Timestamp"] = " * -1d";
            value6["Value"] = 7;
            value6["Timestamp"] = " * -2d";
            streamValue1["WebId"] = point1["WebId"];
            streamValue1["Items"] = new JArray() { value1, value2 };
            streamValue2["WebId"] = point2["WebId"];
            streamValue2["Items"] = new JArray() { value3, value4 };
            streamValue3["WebId"] = point3["WebId"];
            streamValue3["Items"] = new JArray() { value5, value6 };

            object array = new JArray() { streamValue1, streamValue2, streamValue3 };

            await MakeRequest(baseUrl + "/streamsets/recorded", "POST", array);

            Console.WriteLine("The values were updated successfully.");


            ////Example 7 - PI Web API Batch
            dynamic batch = new JObject();
            batch["1"] = new JObject();
            batch["2"] = new JObject();
            batch["3"] = new JObject();
            batch["1"].Method = "GET";
            batch["1"].Resource = "https://localhost/piwebapi/points?path=\\\\SATURN-MARCOS\\sinusoid";
            batch["2"].Method = "GET";
            batch["2"].Resource = "https://localhost/piwebapi/points?path=\\\\SATURN-MARCOS\\cdt158";
            batch["3"].Method = "GET";
            batch["3"].Resource = "https://localhost/piwebapi/streamsets/value?webid={0}&webid={1}";
            batch["3"].Parameters = new JArray() { "$.1.Content.WebId", "$.2.Content.WebId" };
            batch["3"].ParentIds = new JArray() { "1", "2" };

            JObject batchResponse = await MakeRequest(baseUrl + "/batch", "POST", batch);


            JToken pointBatch1 = batchResponse["1"];
            JToken pointBatch2 = batchResponse["2"]["Content"];
            JToken batchStreamValues = batchResponse["3"]["Content"];
            foreach (var piStreamValue in batchStreamValues["Items"])
            {
                Console.WriteLine("PI Point: {0}, Value: {1}, Timestamp: {2}", piStreamValue["Name"], piStreamValue["Value"]["Value"], piStreamValue["Value"]["Timestamp"]);
            }
        }

        private static async Task<JObject> MakeRequest(string url, string method, object data = null)
        {
            HttpMethod httpMethod = new HttpMethod(method);
            var request = new HttpRequestMessage(httpMethod, url);

            if ((httpMethod.Method.ToUpper() == "PUT") || (httpMethod.Method.ToUpper() == "POST") || (httpMethod.Method.ToUpper() == "PATCH"))
            {
                string json = JsonConvert.SerializeObject(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            //Kerberos
            //handler.UseDefaultCredentials = true;

            //Basic authentication
            handler.Credentials = new NetworkCredential("username", "password");
            HttpClient httpClient = new HttpClient(handler);
            HttpResponseMessage httpMessage = await httpClient.SendAsync(request);
            if (httpMessage.IsSuccessStatusCode == true)
            {
                using (var stream = await httpMessage.Content.ReadAsStreamAsync())
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        string response = await streamReader.ReadToEndAsync();
                        return JsonConvert.DeserializeObject<JObject>(response);
                    }
                }
            }
            else
            {
                throw new Exception("HTTP Status Code was not successful.");
            }
        }
    }
}
