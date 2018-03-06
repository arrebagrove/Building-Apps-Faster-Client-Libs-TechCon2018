using Newtonsoft.Json;
using OSIsoft.PIDevClub.PIWebApiClient;
using OSIsoft.PIDevClub.PIWebApiClient.Client;
using OSIsoft.PIDevClub.PIWebApiClient.Model;
using OSIsoft.PIDevClub.PIWebApiClient.WebID;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DemoWithClientLib
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                await RunAsync();
            }).Wait();
            Console.Write("Press any key to continue...");
            Console.ReadKey();

        }

        private static async Task RunAsync()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            PIWebApiClient client = new PIWebApiClient("https://marc-rras.osisoft.int/piwebapi", false, "marc.adm", "kk");

            //Example 1 - Connecting to PI Web API
            PILanding homeLanding = await client.Home.GetAsync();
            Console.WriteLine("System Link is {0}", homeLanding.Links.System);



            //Example 2 - Getting the PI Objects Web ID
            PIDataServer dataServer = client.DataServer.GetByPath("\\\\MARC-PI2016");
            PIPoint sinusoidPoint = client.Point.GetByPath("\\\\MARC-PI2016\\SINUSOID", null);
            PIElement element = client.Element.GetByPath("\\\\MARC-PI2016\\CrossPlatformLab\\marc.adm");
            PIItemsAttribute attributes = client.Element.GetAttributes(element.WebId, null, 1000, null, false);
            PIAttribute attribute = client.Attribute.GetByPath(string.Format("{0}|{1}", "\\\\MARC-PI2016\\CrossPlatformLab\\marc.adm", attributes.Items[0].Name));

            Console.WriteLine("The PI Data Server Web ID is {0} and the PI Point Web ID is {1}", dataServer.WebId, sinusoidPoint.WebId);

            //Example 3 - Handling exceptions
            try
            {
                PIWebApiClient client2 = new PIWebApiClient("https://devdata.osisoft.com/piwebapi/", true);
                await client2.Home.GetAsync();
            }
            catch (ApiException e)
            {
                Console.WriteLine("Expected error: {0}", e.Message);
            }

            try
            {
                PIPoint point = client.Point.GetByPath("\\\\MARC-PI2016\\SINUSOID24323", null);
            }
            catch (ApiException e)
            {
                Console.WriteLine("Expected error: {0}", e.Message);
            }

            //Example 4 - Change the description of the PI Point
            string webId = sinusoidPoint.WebId;
            PIPoint updatePoint = new PIPoint();
            updatePoint.Descriptor = "New description";         
            ApiResponse<Object> response = client.Point.UpdateWithHttpInfo(webId, updatePoint);

            if (response.StatusCode < 300)
            {
                Console.WriteLine("The PI Point description was update successfully");
            }


            //Example 5 - Retrieving data in bulk
            PIPoint point1 = client.Point.GetByPath("\\\\marc-pi2016\\sinusoid");
            PIPoint point2 = client.Point.GetByPath("\\\\marc-pi2016\\sinusoidu");
            PIPoint point3 = client.Point.GetByPath("\\\\marc-pi2016\\cdt158");
            List<string> webIds = new List<string>() { point1.WebId, point2.WebId, point3.WebId };
            PIItemsStreamValues piItemsStreamValues = client.StreamSet.GetRecordedAdHoc(webId: webIds, startTime: "*-3d", endTime: "*");

            foreach (PIStreamValues piStreamValues in piItemsStreamValues.Items)
            {
                foreach (PITimedValue value in piStreamValues.Items)
                {
                    Console.WriteLine("PI Point: {0}, Value: {1}, Timestamp: {2}", piStreamValues.Name, value.Value, value.Timestamp);
                }
            }


            //Example 6 - Sending data in bulk
            var piStreamValuesList = new List<PIStreamValues>() {
                new PIStreamValues() {
                    WebId = point1.WebId,
                    Items = new List<PITimedValue>() {
                        new PITimedValue() { Value = 2, Timestamp = "*-1d" },
                        new PITimedValue() { Value = 3, Timestamp = "*-2d" }
                    },
                },
                new PIStreamValues() {
                    WebId = point2.WebId,
                    Items = new List<PITimedValue>()  {
                        new PITimedValue() { Value = 4, Timestamp = "*-1d" },
                        new PITimedValue() { Value = 5, Timestamp = "*-2d" }
                    },
                },
                new PIStreamValues() {
                    WebId = point3.WebId,
                    Items = new List<PITimedValue>() {
                        new PITimedValue() { Value = 6, Timestamp = "*-1d" },
                        new PITimedValue() { Value = 7, Timestamp = "*-2d" }
                    },
                },
            };
            ApiResponse<PIItemsItemsSubstatus> response2 = client.StreamSet.UpdateValuesAdHocWithHttpInfo(piStreamValuesList);

            if (response2.StatusCode < 300)
            {
                Console.WriteLine("The values were updated successfully.");
            }


            //Example 7 - PI Web API Batch
            Dictionary<string, PIRequest> batch = new Dictionary<string, PIRequest>()
            {
                { "1", new PIRequest("GET", "https://marc-rras.osisoft.int/piwebapi/points?path=\\\\marc-pi2016\\sinusoid") },
                { "2", new PIRequest("GET", "https://marc-rras.osisoft.int/piwebapi/points?path=\\\\marc-pi2016\\cdt158") },
                { "3", new PIRequest() {
                        Method = "GET",
                        Resource = "https://marc-rras.osisoft.int/piwebapi/streamsets/value?webid={0}&webid={1}",
                        Parameters = new List<string>() { "$.1.Content.WebId", "$.2.Content.WebId" },
                        ParentIds = new List<string>() { "1", "2" }
                    }
                }
            };
            Dictionary<string, PIResponse> batchResponse = await client.BatchApi.ExecuteAsync(batch);

            if (batchResponse.All(r => r.Value.Status == 200))
            {
                PIPoint pointBatch1 = JsonConvert.DeserializeObject<PIPoint>(batchResponse["1"].Content.ToString());
                PIPoint pointBatch2 = JsonConvert.DeserializeObject<PIPoint>(batchResponse["2"].Content.ToString());
                PIItemsStreamValue batchStreamValues = JsonConvert.DeserializeObject<PIItemsStreamValue>(batchResponse["3"].Content.ToString());
                foreach (PIStreamValue piStreamValue in batchStreamValues.Items)
                {
                    Console.WriteLine("PI Point: {0}, Value: {1}, Timestamp: {2}", piStreamValue.Name, piStreamValue.Value.Value, piStreamValue.Value.Timestamp);
                }
            }


            //Example 8 - Getting Web ID 2.0 information
            WebIdInfo webIdInfo = client.WebIdHelper.GetWebIdInfo(element.WebId);
            Console.WriteLine("Element Path is: {0}", webIdInfo.Path);
            WebIdInfo webIdInfo2 = client.WebIdHelper.GetWebIdInfo(attribute.WebId);
            Console.WriteLine("Attribute Path is: {0}", webIdInfo.Path);
            WebIdInfo webIdInfo4 = client.WebIdHelper.GetWebIdInfo(point1.WebId);
            Console.WriteLine("PI Point Path is: {0}", webIdInfo.Path);
            WebIdInfo webIdInfo3 = client.WebIdHelper.GetWebIdInfo(dataServer.WebId);
            Console.WriteLine("PI Data Server Path is: {0}", webIdInfo.Path);


            //Example 9 - Generating Web ID 2.0 using path
            string webId1PathOnly = client.WebIdHelper.GenerateWebIdByPath("\\\\MARC-PI2016", typeof(PIDataServer));
            PIDataServer dataServer2 = client.DataServer.Get(webId1PathOnly);
            string webId2PathOnly = client.WebIdHelper.GenerateWebIdByPath("\\\\MARC-PI2016\\CrossPlatformLab\\marc.adm|Heading", typeof(PIAttribute), typeof(PIElement));
            PIAttribute attribute2 = client.Attribute.Get(webId2PathOnly);


        }
    }
}
