using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gamp.ConsoleApp
{
    class Program
    {
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly CancellationToken _token = _cts.Token;
        private static readonly System.Timers.Timer _timer = new System.Timers.Timer();        

        static void Main(string[] args)
        {            
            SetupTimer();
            _timer.Start();
            //Timer_Elapsed(null, null);

            Console.ReadLine();

            _timer.Stop();
            CleanUp();
        }

        private static void SetupTimer()
        {
            _timer.Interval = 1000;
            _timer.Elapsed += Timer_Elapsed;
        }

        private static void CleanUp()
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();

            _cts.Cancel();
            _cts.Dispose();           
        }

        private static async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var blockId = await ReadHistoryAsync();                
                var response = await TrackEvent(blockId.ToString());
            }
            catch(OperationCanceledException ex)
            {
                // cancelled
            }
        }

        private static async Task<string> ReadHistoryAsync()
        {
            _token.ThrowIfCancellationRequested();
            var block_Id = await ReadIrreversibleBlockAsync();            

            Console.WriteLine(block_Id);

            return block_Id;

        }

        private static async Task<string> ReadIrreversibleBlockAsync()
        {
            _token.ThrowIfCancellationRequested();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://wax.cryptolions.io/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync(
                    "/v1/chain/get_info", _token);
                response.EnsureSuccessStatusCode();                

                var action = await response.Content.ReadAsStringAsync();

                JObject joResponse = JObject.Parse(action);
                var last_irreversible_block_id = joResponse["last_irreversible_block_id"];                

                return last_irreversible_block_id.Value<string>();
            }    
        }

        public static async Task<HttpResponseMessage> TrackEvent(string blockId)
        {
            _token.ThrowIfCancellationRequested();

            using (var httpClient = new HttpClient())
            {
                var postData = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("v", "1"),
                    new KeyValuePair<string, string>("tid", "UA-186872631-1"),
                    new KeyValuePair<string, string>("cid", "1"),
                    new KeyValuePair<string, string>("t", "event"),
                    new KeyValuePair<string, string>("ec", "last_irreversible_block"),
                    new KeyValuePair<string, string>("ea", blockId)
                };

                return await httpClient.PostAsync("https://www.google-analytics.com/collect", new FormUrlEncodedContent(postData), _token);
            }
        }
    }
}
