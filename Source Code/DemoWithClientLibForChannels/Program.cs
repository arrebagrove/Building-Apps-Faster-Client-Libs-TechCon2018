using OSIsoft.PIDevClub.PIWebApiClient;
using OSIsoft.PIDevClub.PIWebApiClient.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DemoWithClientLibForChannels
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            PIWebApiClient client = new PIWebApiClient("https://marc-rras.osisoft.int/piwebapi", false, "marc.adm", "kk");
            PIPoint point1 = client.Point.GetByPath("\\\\marc-pi2016\\sinusoid");
            PIPoint point2 = client.Point.GetByPath("\\\\marc-pi2016\\sinusoidu");
            PIPoint point3 = client.Point.GetByPath("\\\\marc-pi2016\\cdt158");
            List<string> webIds = new List<string>() { point1.WebId, point2.WebId, point3.WebId };

            //Example 10 - PI Web API Channels - StartStreamSets
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            IObserver<PIItemsStreamValues> observer = new CustomChannelObserver();
            Task channelTask = client.Channel.StartStreamSets(webIds, observer, cancellationSource.Token);
                        
            System.Threading.Thread.Sleep(120000);
            cancellationSource.Cancel();
            channelTask.Wait();
        }
    }
}
