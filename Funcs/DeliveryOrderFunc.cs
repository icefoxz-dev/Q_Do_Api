using System.Net;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderApiFun.Core.Middlewares;
using OrderApiFun.Core.Services;
using OrderDbLib.Entities;
using OrderHelperLib;
using OrderHelperLib.DtoModels.DeliveryOrders;
using Utls;

namespace Do_Api.Funcs
{
    public class DeliveryOrderFunc
    {
        private LingauManager LingauManager { get; }
        private DeliveryOrderService DoService { get; }
        private DeliveryManManager DmManager { get; }
        private UserManager<User> UserManager { get; }

        public DeliveryOrderFunc(DeliveryOrderService doService, UserManager<User> userManager, DeliveryManManager dmManager, LingauManager lingauManager)
        {
            DoService = doService;
            UserManager = userManager;
            DmManager = dmManager;
            LingauManager = lingauManager;
        }

        [Function(nameof(User_CreateDeliveryOrder))]
        public async Task<HttpResponseData> User_CreateDeliveryOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(User_CreateDeliveryOrder));
            var bag = await req.GetBagAsync();
            //test Instance:
            //var dto = InstanceTestDeliverDto();
            //log.LogWarning(Json.Serialize(dto));
            //throw new NotImplementedException();
            log.LogInformation("C# HTTP trigger function processed a request.");
            var userId = context.Items[Auth.UserId].ToString();
            // Deserialize the request body to DeliveryOrder
            DeliveryOrderDto? orderDto = null;
            try
            {
                orderDto = bag.Get<DeliveryOrderDto>(0);
            }
            catch (Exception e)
            {
                log.LogWarning($"Invalid request body.\n{e}");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid request body.");
                return badRequestResponse;
            }

            // Add the new order to the database using the DeliveryOrderService
            var order = orderDto.Adapt<DeliveryOrder>();
            var newDo = await DoService.CreateDeliveryOrderAsync(userId, order, log);
            var createdResponse = req.CreateResponse(HttpStatusCode.Created);
            //createdResponse.Headers.Add("Location", $"deliveryorder/{newOrder.Id}");
            await createdResponse.WriteAsJsonAsync(DataBag.Serialize(newDo.Adapt<DeliveryOrderDto>()));
            return createdResponse;
        }

        [Function(nameof(User_GetAllDeliveryOrders))]
        public async Task<HttpResponseData> User_GetAllDeliveryOrders(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(User_GetAllDeliveryOrders));
            log.LogInformation("C# HTTP trigger function processed a request.");

            var userId = context.Items[Auth.UserId].ToString();

            // Retrieve DataBag from request
            var bag = await req.GetBagAsync();

            // Get the 'limit' and 'page' values from the DataBag
            var limit = 10;
            var page = 0;

            try
            {
                limit = bag.Get<int>(0);
                page = bag.Get<int>(1);
            }
            catch (Exception _)
            {
                // ignored
            }

            // Retrieve paginated DeliveryOrders for the user from the database using the DeliveryOrderService
            var deliveryOrders = await DoService.GetDeliveryOrdersAsync(userId, limit, page, log);

            // Check if there are any DeliveryOrders for the user
            if (deliveryOrders == null || !deliveryOrders.Any())
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("No DeliveryOrders found for the user.");
                return notFoundResponse;
            }

            // Convert the DeliveryOrders to a list of DeliveryOrderDto objects
            var deliveryOrdersDto = deliveryOrders.Select(order => order.Adapt<DeliveryOrderDto>()).ToList();

            // Create an HTTP 200 (OK) response and write the DeliveryOrderDto objects as JSON
            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteStringAsync(DataBag.SerializeWithName(nameof(deliveryOrdersDto.GetType), deliveryOrdersDto));
            return okResponse;
        }

        [Function(nameof(User_PayDeliveryOrderByCredit))]
        public async Task<HttpResponseData> User_PayDeliveryOrderByCredit(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(User_PayDeliveryOrderByCredit));
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Retrieve the request body
            var userId = context.Items[Auth.UserId].ToString();
            DeliveryOrder? order;
            try
            {
                var bag = await req.GetBagAsync();
                var deliveryOrderId = bag.Get<int>(0);
                order = await DoService.GetDeliveryOrderAsync(userId, deliveryOrderId);
            }
            catch (Exception e)
            {
                log.LogWarning($"Invalid request body.\n{e}");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid request body.");
                return badRequestResponse;
            }

            var userLingau = await LingauManager.GetLingauAsync(userId);
            if (order.PaymentInfo.Price > userLingau.Credit)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Not enough Lingau.");
                return badRequestResponse;
            }

            await DoService.PayDeliveryOrderByLingauAsync(userId, order, log);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(DataBag.Serialize(userLingau.Adapt<LingauDto>()));
            return response;
        }

        [Function(nameof(DeliveryMan_AssignDeliveryMan))]
        public async Task<HttpResponseData> DeliveryMan_AssignDeliveryMan(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(DeliveryMan_AssignDeliveryMan));
            log.LogInformation("C# HTTP trigger function processed a request.");
            try
            {
                var deliveryManId = GetDeliveryManId(context);

                var bag = await req.GetBagAsync();
                var oId = bag.Get<string>(0);
                var orderId = int.Parse(oId);

                if (bag == null)
                {
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteStringAsync("Invalid request payload.");
                    return errorResponse;
                }

                //assign deliveryMan
                await DoService.AssignDeliveryManAsync(orderId, deliveryManId);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("Delivery man assigned successfully.");
                return response;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error assigning delivery man.");

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error assigning delivery man.");
                return errorResponse;
            }
        }
        //从context中获取当前的DeliveryManId
        private static int GetDeliveryManId(FunctionContext context)
        {
            if (!int.TryParse(context.Items[Auth.DeliverManId].ToString(), out var deliveryManId))
                throw new NullReferenceException($"DeliveryMan[{deliveryManId}] not found!");
            return deliveryManId;
        }

        [Function(nameof(DeliveryMan_UpdateOrderStatus))]
        public async Task<HttpResponseData> DeliveryMan_UpdateOrderStatus(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, 
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(DeliveryMan_UpdateOrderStatus));
            log.LogInformation("C# HTTP trigger function processed a request.");
            var deliveryManId = GetDeliveryManId(context);
            DeliverySetStatusDto? dto;
            try
            {
                dto = await req.ReadFromJsonAsync<DeliverySetStatusDto>();

                if (dto == null || dto.DeliveryOrderId <= 0)
                {
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteStringAsync("Invalid request payload.");
                    return errorResponse;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error parsing request payload.");

                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid request payload.");
                return errorResponse;
            }

            var message = string.Empty;
            try
            {
                var status = (DeliveryOrderStatus)dto.Status;
                message = $"Order[{dto.DeliveryOrderId}].Update({status})";
                // Assuming you have a static instance of the DeliveryOrderService
                await DoService.UpdateOrderStatusByDeliveryManAsync(deliveryManId, dto.DeliveryOrderId, status);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(DataBag.Serialize(message));
                return response;
            }
            catch (Exception ex)
            {
                message = $"Error {message}!";
                log.LogError(ex, message);

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error setting order status to {dto.Status}.");
                return errorResponse;
            }
        }
    }
}
