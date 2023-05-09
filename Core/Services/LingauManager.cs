using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderDbLib;
using OrderDbLib.Entities;

namespace OrderApiFun.Core.Services;

public class LingauManager
{
    private OrderDbContext Db { get; }
    public LingauManager(OrderDbContext db)
    {
        Db = db;
    }

    public async Task UpdateLingauBalanceAsync(string userId, float amount,ILogger log)
    {
        var user = await GetUserAsync(userId);
        var lastCredit = user.Lingau.Credit;
        user.Lingau.Credit += amount;
        user.Lingau.UpdateFileTimeStamp();
        user.UpdateFileTimeStamp();
        log.LogInformation($"Lingau balance updated: Amount : {amount}, User[{userId}] from {lastCredit} to {user.Lingau.Credit}.");
        await Db.SaveChangesAsync();
    }

    public async Task<Lingau> GetLingauAsync(string userId)
    {
        var user = await GetUserAsync(userId);
        return user.Lingau;
    }
    public async Task<float> GetLingauBalanceAsync(string userId)
    {
        var lingau = await GetLingauAsync(userId);
        return lingau.Credit;
    }
    private async Task<User?> GetUserAsync(string userId)
    {
        return await Db.Users.Include(u => u.Lingau).FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
    }

}