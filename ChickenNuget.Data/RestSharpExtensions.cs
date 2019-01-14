using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ChickenNuget.Data
{
    public static class RestSharpExtensions
    {
        public static JToken ToJson(this IRestResponse response)
        {
            return (JToken)JsonConvert.DeserializeObject(response.Content);
        }

        public static JArray ToJsonArray(this IRestResponse response)
        {
            return (JArray)JsonConvert.DeserializeObject(response.Content);
        }

        public static JObject ToJsonObject(this IRestResponse response)
        {
            return (JObject)JsonConvert.DeserializeObject(response.Content);
        }
    }
}