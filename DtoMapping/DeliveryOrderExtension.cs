using Utls;

namespace OrderApiFun.DtoMapping
{
    //public static class DeliveryOrderExtension
    //{
    //    public static DeliveryOrder ToEntity(this DeliveryOrderDto d, User user, User receiver)
    //    {
    //        var e = Entity.Instance<DeliveryOrder>();
    //        e.StartCoordinates = d.StartCoordinates.ToEntity();
    //        e.EndCoordinates = d.EndCoordinates.ToEntity();
    //        e.User = user;
    //        e.UserId = user.Id;
    //        e.ReceiverUserId = receiver.Id;
    //        e.ReceiverUser = receiver;
    //        e.ReceiverInfo = new ReceiverInfo
    //        {
    //            Name = receiver.Name,
    //            PhoneNumber = receiver.PhoneNumber,
    //            NormalizedPhoneNumber = receiver.NormalizedPhoneNumber
    //        };
    //        e.DeliveryInfo = new DeliveryInfo
    //        {
    //            Distance = d.Distance,
    //            Price = d.Price,
    //            Weight = d.Weight
    //        };
    //        e.Status = DeliveryOrderStatus.Created;
    //        return e;
    //    }
    //    public static DeliveryOrder ToEntity(this DeliveryOrderDto d, User user)
    //    {
    //        var e = Entity.Instance<DeliveryOrder>();
    //        e.StartCoordinates = d.StartCoordinates.ToEntity();
    //        e.EndCoordinates = d.EndCoordinates.ToEntity();
    //        e.User = user;
    //        e.UserId = user.Id;
    //        e.ReceiverInfo = new ReceiverInfo
    //        {
    //            Name = d.ReceiverName,
    //            PhoneNumber = d.ReceiverPhoneNumber,
    //            NormalizedPhoneNumber = MyPhone.NormalizePhoneNumber(d.ReceiverPhoneNumber)
    //        };
    //        e.DeliveryInfo = new DeliveryInfo
    //        {
    //            Distance = d.Distance,
    //            Price = d.Price,
    //            Weight = d.Weight
    //        };
    //        e.Status = DeliveryOrderStatus.Created;
    //        return e;
    //    }
    //    public static DeliveryOrderDto ToDto(this DeliveryOrder d)
    //    {
    //        var o = new DeliveryOrderDto();
    //        o.StartCoordinates = d.StartCoordinates.ToDto();
    //        o.EndCoordinates = d.EndCoordinates.ToDto();
    //        o.ReceiverId = d.ReceiverUserId;
    //        o.ReceiverName = d.ReceiverInfo.Name;
    //        o.ReceiverPhoneNumber = d.ReceiverInfo.PhoneNumber;
    //        o.Distance = d.DeliveryInfo.Distance;
    //        o.Weight = d.DeliveryInfo.Weight;
    //        o.Price = d.DeliveryInfo.Price;
    //        return o;
    //    }
    //    public static DeliveryOrderDto.CoordinatesDto ToDto(this Coordinates c)
    //    {
    //        var o = new DeliveryOrderDto.CoordinatesDto();
    //        o.Address = c.Address;
    //        o.Latitude = c.Latitude;
    //        o.Longitude = c.Longitude;
    //        return o;
    //    }
    //    public static Coordinates ToEntity(this DeliveryOrderDto.CoordinatesDto c)
    //    {
    //        var o = new Coordinates();
    //        o.Address = c.Address;
    //        o.Latitude = c.Latitude;
    //        o.Longitude = c.Longitude;
    //        return o;
    //    }
    //}
}
