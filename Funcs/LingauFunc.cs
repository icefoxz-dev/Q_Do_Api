using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderApiFun.Core.Middlewares;
using OrderApiFun.Core.Services;
using Utls;

namespace Do_Api.Funcs;

public class LingauFunc
{
    private LingauManager LingauManager { get; }

    public LingauFunc(LingauManager lingauManager)
    {
        LingauManager = lingauManager;
    }

    [Function(nameof(User_GetLingauBalance))]
    public async Task<HttpResponseData> User_GetLingauBalance(
        [HttpTrigger(AuthorizationLevel.Function, "get")]
        HttpRequestData req,
        FunctionContext context)
    {
        var log = context.GetLogger(nameof(User_GetLingauBalance));
        log.LogInformation("C# HTTP trigger function processed a request.");

        var userId = context.Items[Auth.UserId].ToString();
        var bag = await req.GetBagAsync();
        // Retrieve the user's Lingau balance
        var balance = await LingauManager.GetLingauBalanceAsync(userId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync($"User's Lingau balance: {balance}");
        return response;
    }
}