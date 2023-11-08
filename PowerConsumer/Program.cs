using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PowerConsumer;
using System.Text;

class Consumer
{
    private const string _Messurement = "hehexdtest";
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
            try
            {
                while (true)
                {
                    var cr = consumer.Consume(cts.Token);

                    if (cr.Message.Value is not null)
                    {

                        Data data = await ParseData(cr.Message.Value);

                        DateTime dateTime = await UnixToDateTime(data.Timestamp);

                        using (var client = new HttpClient())
                        {
                            DateTime currentTime = DateTime.UtcNow;
                            long unixTime = ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();

                            string payload = $"{_Messurement},house_id={data.HouseId},month={dateTime.Month} value={data.Kwh.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} {unixTime}";
                            var content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");
                            bool send = true;
                            while (send)
                            {
                                try
                                {
                                    HttpResponseMessage? response = await client.PostAsync("http://172.16.250.15:8428/write", content);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        Console.WriteLine("Sent Data");
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
                }
            }
            catch (OperationCanceledException)
            {
                // Ctrl-C was pressed.
            }
            finally
            {
                consumer.Close();
            }
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
}