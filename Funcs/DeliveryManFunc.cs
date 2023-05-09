using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using OrderApiFun.Core.Middlewares;
using OrderApiFun.Core.Services;
using OrderDbLib.Entities;
using OrderHelperLib;

namespace Do_Api.Funcs;

public class DeliveryManFunc
{
    private DeliveryManManager DeliveryManManager { get; }
    private UserManager<User> UserManager { get; }

    public DeliveryManFunc(DeliveryManManager deliveryManManager, UserManager<User> userManager)
    {
        DeliveryManManager = deliveryManManager;
        UserManager = userManager;
    }

    [Function(nameof(User_CreateDeliveryMan))]
    public async Task<HttpResponseData> User_CreateDeliveryMan([HttpTrigger(AuthorizationLevel.Function, "post")] 
        HttpRequestData req, 
        FunctionContext context)
    {
        var log = context.GetLogger(nameof(User_CreateDeliveryMan));
        // 解析请求主体获取用户ID
        var userId = context.Items[Auth.UserId].ToString();
        var user = await UserManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new NullReferenceException($"User not found! Id = {userId}");
        }
        // 调用DeliveryManManager创建DeliveryMan
        var deliveryMan = await DeliveryManManager.CreateDeliveryManAsync(user);

        // 返回响应
        var response = req.CreateResponse(HttpStatusCode.Created);
        // 在实际项目中，您可能需要序列化返回的DeliveryMan对象为JSON
        await response.WriteStringAsync(DataBag.Serialize($"DeliveryMan created with ID: {deliveryMan.Id}"));
        return response;
    }
}