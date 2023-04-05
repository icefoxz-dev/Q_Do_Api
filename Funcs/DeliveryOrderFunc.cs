using System.Net;
using Mapster;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using OrderApiFun.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OrderApiFun.Core.Middlewares;
using OrderDbLib.Entities;
using OrderHelperLib;
using Utls;

namespace OrderApiFun.Funcs
{
    public class DeliveryOrderFunc
    {
        private DeliveryOrderService DoService { get; }
        private DeliveryManManager DmManager { get; }
        private UserManager<User> UserManager { get; }

        public DeliveryOrderFunc(DeliveryOrderService doService, UserManager<User> userManager, DeliveryManManager dmManager)
        {
            DoService = doService;
            UserManager = userManager;
            DmManager = dmManager;
        }

        [Function(nameof(User_CreateDeliveryOrder))]
        public async Task<HttpResponseData> User_CreateDeliveryOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(User_CreateDeliveryOrder));
            //test Instance:
            //var dto = InstanceTestDeliverDto();
            //log.LogWarning(Json.Serialize(dto));
            //throw new NotImplementedException();
            log.LogInformation("C# HTTP trigger function processed a request.");
            var userId = context.Items[Auth.UserId].ToString();
            // Deserialize the request body to DeliveryOrder
            DeliveryOrderDto? orderDto;
            try
            {
                orderDto = await req.ReadFromJsonAsync<DeliveryOrderDto>();
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
            var newDo = await DoService.CreateDeliveryOrderAsync(userId,order, log);
            var createdResponse = req.CreateResponse(HttpStatusCode.Created);
            //createdResponse.Headers.Add("Location", $"deliveryorder/{newOrder.Id}");
            await createdResponse.WriteAsJsonAsync(newDo.Adapt<DeliveryOrderDto>());
            return createdResponse;

            DeliveryOrderDto InstanceTestDeliverDto()
            {
                var d = new DeliveryOrderDto();
                d.ItemInfo = new ItemInfoDto
                {
                    Height = 1.5f,
                    Length = 3f,
                    Quantity = 1,
                    Weight = 5f,
                    Width = 1.2f,
                    Remark = "Help me post!"
                };
                d.StartCoordinates = new CoordinatesDto
                {
                    Address = "10 Long Lama",
                    Latitude = 3.211,
                    Longitude = 123.1213
                };
                d.EndCoordinates = new CoordinatesDto
                {
                    Address = "112 Long Lama",
                    Latitude = 3.12,
                    Longitude = 173.1233
                };
                d.ReceiverInfo = new ReceiverInfoDto
                {
                    Name = "Abun",
                    PhoneNumber = "0123456495"
                };
                d.DeliveryInfo = new DeliveryInfoDto
                {
                    Distance = 10,
                    Price = 20,
                    Weight = 16
                };
                d.Status = DeliveryOrderStatus.Closed;
                return d;
            }

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
                
                var deliveryAssignment = await req.ReadFromJsonAsync<DeliveryAssignmentDto>();

                if (deliveryAssignment == null)
                {
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteStringAsync("Invalid request payload.");
                    return errorResponse;
                }

                //assign deliveryMan
                await DoService.AssignDeliveryManAsync(deliveryAssignment.DeliveryOrderId, deliveryManId);

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
                await response.WriteStringAsync(message);
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
