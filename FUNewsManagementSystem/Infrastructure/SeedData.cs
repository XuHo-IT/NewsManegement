using FUNewsManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Infrastructure
{
    public static class SeedData
    {
        public static async Task EnsureSeedAdminAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            if (!await db.Database.CanConnectAsync())
            {
                return;
            }

            var adminEmail = configuration["AdminAccount:Email"];
            var adminPassword = configuration["AdminAccount:Password"];
            var adminFullName = configuration["AdminAccount:FullName"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var adminExists = await db.SystemAccounts
                .AnyAsync(x => x.AccountEmail == adminEmail && !x.IsDeleted);

            if (adminExists)
            {
                return;
            }

            var currentMaxId = await db.SystemAccounts.MaxAsync(x => (short?)x.AccountID) ?? 0;
            var admin = new SystemAccount
            {
                AccountID = (short)(currentMaxId + 1),
                AccountEmail = adminEmail,
                AccountName = string.IsNullOrWhiteSpace(adminFullName) ? "System Administrator" : adminFullName,
                AccountRole = 1,
                AccountPasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                AccountPassword = null,
                IsDeleted = false
            };

            db.SystemAccounts.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
