using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bond;
using Bond.Protocols;
using Bond.IO.Unsafe;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Net;

namespace SampleProject
{
    /// <summary>
    /// Sample project including client side code to insert and retrieve data from the
    /// prototype environment using a sample bond object that inherits from BaseItem
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            SchemaTest();
        }

        public static void SchemaTest()
        {
            var schema = Schema<SampleItem>.RuntimeSchema;

            // Create and cache deserializers for each schema you need
            // Good habit, These serializers for Bond use JIT compiling and is a large performance hit.
            var deserializers = new Dictionary<string, Deserializer<SimpleBinaryReader<InputBuffer>>>
                        {
                            {"SampleItem", new Deserializer<SimpleBinaryReader<InputBuffer>>(typeof(SampleItem), schema)}
                        };

            var item = GetFakeData();
            var id = item.Id.ToString();
            var subId = ConfigurationManager.AppSettings["SubscriptionId"];

            StoreItem(id, subId, SerializeBaseItem(item)).Wait();

            var retrievedItem = GetItem(id, subId, deserializers["SampleItem"]).Result;

            //you'll notice that the bonded property is still serialized. It needs to be deserialized before accessing.
            //non bonded fields are not still serialized and can be accessed directly.
            Console.WriteLine(string.Format("Retrieved sample from storage. Id: {0}. Bonded Property: {1}", retrievedItem.Id.ToString(), retrievedItem.sample.Deserialize().prop));
        }

        //Serializes bond object to byte array
        private static byte[] SerializeBaseItem(SampleItem item)
        {
            //Serialize to bond before going across the wire
            var output = new OutputBuffer();
            var writer = new SimpleBinaryWriter<OutputBuffer>(output);

            Serialize.To(writer, item);

            return output.Data.Array;
        }

        private static async Task StoreItem(string id, string subscriptionId, byte[] data)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString("id={id}?subscription-key={subscription-key}");

            // Specify your subscription key
            queryString["subscription-key"] = subscriptionId;
            queryString["id"] = id;

            var uri = "https://api.careotter.com/patient/record/?" + queryString;

            HttpResponseMessage response = null;

            // Specify request body
            using (var content = new ByteArrayContent(data))
            {
                response = await client.PutAsync(uri, content);
            }

            if (response.Content != null)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseString);
            }
        }

        private static async Task<SampleItem> GetItem(string id, string subscriptionId, Deserializer<SimpleBinaryReader<InputBuffer>> deserializer)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString("id={id}?subscription-key={subscription-key}");

            // Specify your subscription key
            queryString["subscription-key"] = subscriptionId;
            queryString["id"] = id;

            var uri = "https://api.careotter.com/patient/record/?" + queryString;

            var response = await client.GetAsync(uri);

            if (response.Content != null)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseString);
                    return null;
                }
                else
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    var input = new InputBuffer(bytes);
                    var reader = new SimpleBinaryReader<InputBuffer>(input);

                    return deserializer.Deserialize<SampleItem>(reader);
                }
            }

            return null;
        }

        private static SampleItem GetFakeData()
        {
            return new SampleItem()
            {
                sample = new Bonded<SampleContainer>(new SampleContainer()
                {
                    prop = "Test"
                }),
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                Id = Guid.NewGuid(),
                LastModifiedDate = DateTime.UtcNow
            };
        }
    }
}
