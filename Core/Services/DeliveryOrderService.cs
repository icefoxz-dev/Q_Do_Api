using OrderLib.Entities;
using OrderLib;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Utls;

namespace OrderApiFun.Core.Services
{
    public class DeliveryOrderService
    {
        private OrderDbContext Db { get; }
        private UserManager<User> UserManager { get; }
        private DeliveryManManager DeliveryManManager { get; }
        public DeliveryOrderService(OrderDbContext db, UserManager<User> userManager, DeliveryManManager deliveryManManager)
        {
            Db = db;
            UserManager = userManager;
            DeliveryManManager = deliveryManManager;
        }

        public async Task<DeliveryOrder> CreateDeliveryOrderAsync(string? userId, DeliveryOrder newOrder, ILogger log)
        {
            var user = await UserManager.FindByIdAsync(userId);
            if (user == null) throw new NullReferenceException("user is null!");
            newOrder.User = user;
            newOrder.UserId = userId;
            var receiver = await UserManager.FindByIdAsync(newOrder.ReceiverUserId);
            if (receiver != null)
            {
                newOrder.ReceiverInfo = CreateOrderReceiverInfo(receiver.Name, receiver.PhoneNumber);
                newOrder.ReceiverUserId = receiver.Id;
                newOrder.ReceiverUser = receiver;
            }
            else
            {
                newOrder.ReceiverInfo =
                    CreateOrderReceiverInfo(newOrder.ReceiverInfo.Name, newOrder.ReceiverInfo.PhoneNumber);
            }

            Db.DeliveryOrders.Add(newOrder);
            await Db.SaveChangesAsync();

            log.LogInformation($"DeliveryOrder created with ID: {newOrder.Id}");
            return newOrder;

            ReceiverInfo CreateOrderReceiverInfo(string receiverName, string phoneNumber)
            {
                return new ReceiverInfo
                {
                    Name = receiverName,
                    PhoneNumber = phoneNumber,
                    NormalizedPhoneNumber = MyPhone.NormalizePhoneNumber(phoneNumber)
                };
            }
        }

        private (bool isValid,string message) ValidateDeliveryOrder(DeliveryOrder order)
        {
            var isValid = true;
            var message = string.Empty;
            // Add validation logic as needed

            if (order.StartCoordinates == null
                || order.EndCoordinates == null)
            {
                isValid = false;
                message = "Coordinates error!";
            }

            if (string.IsNullOrWhiteSpace(order.ReceiverInfo.Name)
                || string.IsNullOrWhiteSpace(order.ReceiverInfo.NormalizedPhoneNumber))
            {
                isValid = false;
                message = "Receiver info error!";
            }

            if (order.DeliveryInfo.Distance is default(float)
                || order.DeliveryInfo.Weight is default(float))
            {
                isValid = false;
                message = "Delivery info error!";
            }

            // Additional validations can be added as needed
            return (isValid, message);
        }

        public async Task<DeliveryOrder> UpdateOrderStatusByDeliveryManAsync(int deliveryManId, int orderId, DeliveryOrderStatus newStatus)
        {
            // 验证DeliveryMan的权限
            var deliveryMan = await DeliveryManManager.FindByIdAsync(deliveryManId);
            if (deliveryMan == null)
            {
                throw new InvalidOperationException("DeliveryMan not found");
            }

            var order = await FindByIdAsync(orderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found");
            }

            // 验证订单状态的顺序限制
            if (!IsValidStatusTransition(order.Status, newStatus))
            {
                throw new InvalidOperationException($"Invalid status transition: {order.Status}->{newStatus}");
            }

            await UpdateOrderStatusAsync(order.Id, newStatus);

            return order;
            bool IsValidStatusTransition(DeliveryOrderStatus currentStatus, DeliveryOrderStatus status)
            {
                return currentStatus switch
                {
                    DeliveryOrderStatus.Created => status == DeliveryOrderStatus.Accepted,
                    DeliveryOrderStatus.Accepted => status == DeliveryOrderStatus.InProgress,
                    DeliveryOrderStatus.InProgress => status is DeliveryOrderStatus.Delivered or DeliveryOrderStatus.Exception,
                    DeliveryOrderStatus.Delivered => false, // 无法更改已送达订单的状态
                    DeliveryOrderStatus.Exception => false, // 无法更改异常订单的状态
                    DeliveryOrderStatus.Canceled => false, // 无法更改取消订单的状态
                    DeliveryOrderStatus.Closed => false, // 无法更改关闭订单的状态
                    _ => throw new ArgumentOutOfRangeException(nameof(currentStatus))
                };
            }
        }

        public async Task<DeliveryOrder> UpdateOrderStatusBySenderAsync(int orderId, DeliveryOrderStatus newStatus, string senderId)
        {
            var order = await Db.DeliveryOrders.FindAsync(orderId);

            if (order == null)
            {
                throw new InvalidOperationException("Order not found.");
            }

            if (order.UserId != senderId)
            {
                throw new InvalidOperationException("The sender does not have permission to update this order.");
            }

            // 验证订单状态的顺序限制
            if (!IsValidStatusTransition(order.Status, newStatus))
            {
                throw new InvalidOperationException($"Invalid status transition: {order.Status}->{newStatus}");
            }

            await UpdateOrderStatusAsync(orderId, newStatus);
            return order;

            bool IsValidStatusTransition(DeliveryOrderStatus currentStatus, DeliveryOrderStatus status)
            {
                return currentStatus switch
                {
                    DeliveryOrderStatus.Created => status == DeliveryOrderStatus.Canceled,
                    DeliveryOrderStatus.Accepted => false, // 无法更改承接订单的状态
                    DeliveryOrderStatus.InProgress => false, // 无法更改运送中订单的状态
                    DeliveryOrderStatus.Delivered => status == DeliveryOrderStatus.Exception,
                    DeliveryOrderStatus.Exception => false, // 无法更改异常订单的状态
                    DeliveryOrderStatus.Canceled => false, // 无法更改取消订单的状态
                    DeliveryOrderStatus.Closed => false, // 无法更改关闭订单的状态
                    _ => throw new ArgumentOutOfRangeException(nameof(currentStatus))
                };
            }

        }

        private async Task<DeliveryOrder?> FindByIdAsync(int orderId) =>
            await Db.DeliveryOrders.FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == orderId);

        /// <summary>
        /// 分配工作给DeliveryMan, 状态 = <see cref="DeliveryOrderStatus.Accepted"/>
        /// </summary>
        /// <param name="deliveryOrderId"></param>
        /// <param name="deliveryManId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task AssignDeliveryManAsync(int deliveryOrderId, int deliveryManId)
        {
            var deliveryOrder = await Db.DeliveryOrders.FindAsync(deliveryOrderId);
            var deliveryMan = await DeliveryManManager.FindByIdAsync(deliveryManId);
            if (deliveryMan == null)
                throw new NullReferenceException($"DeliveryMan[{deliveryManId}] not found!");
            if (deliveryOrder == null)
                throw new ArgumentException("Delivery order not found.");
            deliveryOrder.DeliveryManId = deliveryMan.Id;
            deliveryOrder.DeliveryMan = deliveryMan;
            deliveryOrder.Status = DeliveryOrderStatus.Accepted;
            deliveryOrder.UpdateFileTimeStamp();
            await Db.SaveChangesAsync();
        }

        /// <summary>
        /// 更新订单状态
        /// </summary>
        /// <param name="deliveryOrderId"></param>
        /// <param name="newStatus"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async Task UpdateOrderStatusAsync(int deliveryOrderId, DeliveryOrderStatus newStatus)
        {
            var deliveryOrder = await Db.DeliveryOrders.FindAsync(deliveryOrderId);
            if (deliveryOrder == null)
            {
                throw new ArgumentException("Delivery order not found.");
            }

            deliveryOrder.Status = newStatus;
            deliveryOrder.UpdateFileTimeStamp();
            await Db.SaveChangesAsync();
        }

    }
}
