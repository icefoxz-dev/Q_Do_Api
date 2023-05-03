using Microsoft.Azure.Functions.Worker.Http;
using OrderHelperLib;

namespace Utls;

public static class HttpRequestDataExtension
{
    public static async Task<DataBag?> GetBagAsync(this HttpRequestData req)
    {
        var body = await req.ReadAsStringAsync();
        return DataBag.Deserialize(body);
    }
}