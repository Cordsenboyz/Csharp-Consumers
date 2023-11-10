using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PowerConsumer;
using System.Text;

class Consumer
{
    private const string _Messurement = "NielsPower";
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
        .AddJsonFile("C:/Users/niels/source/repos/Consumers/appsettings.json")
        .Build();

        var config = configuration.GetSection("Consumer").Get<ConsumerConfig>().ThrowIfContainsNonUserConfigurable();
        var topic = configuration.GetValue<string>("General:PowerTopic");

        CancellationTokenSource cts = new CancellationTokenSource();

        List<Model> model = new();

        using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
        {
            Console.WriteLine($"Starting Consumer . . .");
            consumer.Subscribe(topic);
            List<Data> batchData = new List<Data>();

            try
            {
                while (true)
                {
                    var cr = consumer.Consume(cts.Token);

                    if (cr.Message.Value is not null)
                    {
                        Data data = await ParseData(cr.Message.Value);
                        batchData.Add(data);

                        if (batchData.Count >= 5000)
                        {
                            await SendBatchData(batchData);
                            batchData.Clear();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ctrl-C was pressed.
            }
            finally
            {
                if (batchData.Count > 0)
                {
                    await SendBatchData(batchData);
                }

                consumer.Close();
            }
        }

        static async Task<Data> ParseData(string data)
        {
            var parsedData = JsonConvert.DeserializeObject<Data>(data);
            return parsedData;
        }

        static async Task<DateTime> UnixToDateTime(int timestamp)
        {
            var offset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return offset.UtcDateTime;
        }

        static async Task SendBatchData(List<Data> batchData)
        {
            using (var client = new HttpClient())
            {
                List<string> payloads = new List<string>();

                foreach (var data in batchData)
                {
                    DateTime dateTime = await UnixToDateTime(data.Timestamp);
                    long unixTime = ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
                    string season = GetSeason(dateTime);

                    string payload = $"{_Messurement},house_id={data.HouseId},month={dateTime.Month},season={season} value={data.Kwh.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} {unixTime}";
                    payloads.Add(payload);
                }

                string combinedPayload = string.Join("\n", payloads);

                var content = new StringContent(combinedPayload, Encoding.UTF8, "application/x-www-form-urlencoded");
                bool send = true;
                while (send)
                {
                    try
                    {
                        HttpResponseMessage? response = await client.PostAsync("http://172.16.250.15:8428/write", content);
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Sent Batch Data");
                            send = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        continue;
                    }
                }
            }
        }
        static string GetSeason(DateTime date)
        {
            int month = date.Month;

            if (month >= 3 && month <= 5)
            {
                return "Spring";
            }
            else if (month >= 6 && month <= 8)
            {
                return "Summer";
            }
            else if (month >= 9 && month <= 11)
            {
                return "Fall";
            }
            else
            {
                return "Winter";
            }
        }
    }
}