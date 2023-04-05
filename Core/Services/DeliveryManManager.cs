using Microsoft.EntityFrameworkCore;
using OrderDbLib;
using OrderDbLib.Entities;

namespace OrderApiFun.Core.Services;

public class DeliveryManManager
{
    private OrderDbContext Db { get; }

    public DeliveryManManager(OrderDbContext db)
    {
        Db = db;
    }

    public async Task<DeliveryMan?> FindByIdAsync(int deliveryManId, bool includeDeleted = false) => includeDeleted
        ? await Db.DeliveryMen.FirstOrDefaultAsync(d => d.Id == deliveryManId)
        : await Db.DeliveryMen.Where(d => !d.IsDeleted).FirstOrDefaultAsync(d => d.Id == deliveryManId);

    public async Task<DeliveryMan> CreateDeliveryManAsync(User user)
    {
        var man = Entity.Instance<DeliveryMan>();
        man.UserId = user.Id;
        man.IsWorking = true;
        man.User = user;
        Db.DeliveryMen.Add(man);
        await Db.SaveChangesAsync();
        return man;
    }
    public async Task<DeliveryMan> UpdateDeliveryManAsync(DeliveryMan deliveryMan)
    {
        Db.DeliveryMen.Update(deliveryMan);
        await Db.SaveChangesAsync();
        return deliveryMan;
    }

    public async Task DeleteDeliveryManAsync(int id)
    {
        var deliveryMan = await FindByIdAsync(id);
        if (deliveryMan != null)
        {
            deliveryMan.DeleteEntity();
            await Db.SaveChangesAsync();
        }
    }

    public async Task<DeliveryMan?> FindByUserIdAsync(string userId) => await Db.DeliveryMen.FirstOrDefaultAsync(d => !d.IsDeleted && d.UserId == userId);
}