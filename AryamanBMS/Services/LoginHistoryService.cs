using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class LoginHistoryService : ILoginHistoryService
    {
        private readonly ApplicationDbContext _context;

        public LoginHistoryService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordAsync(
            string attemptedUserName,
            string eventType,
            bool isSuccessful,
            string? userId = null,
            string? failureReason = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var history = new LoginHistoryModel
            {
                UserId = string.IsNullOrWhiteSpace(userId)
        ? null
        : userId,

                AttemptedUserName =
        string.IsNullOrWhiteSpace(attemptedUserName)
            ? "Unknown"
            : attemptedUserName.Trim()[..Math.Min(attemptedUserName.Trim().Length, 256)],

                EventType =
        string.IsNullOrWhiteSpace(eventType)
            ? "Unknown"
            : eventType.Trim()[..Math.Min(eventType.Trim().Length, 50)],

                IsSuccessful = isSuccessful,

                FailureReason =
        string.IsNullOrWhiteSpace(failureReason)
            ? null
            : failureReason.Trim()[..Math.Min(failureReason.Trim().Length, 250)],

                IpAddress =
        string.IsNullOrWhiteSpace(ipAddress)
            ? null
            : ipAddress.Trim()[..Math.Min(ipAddress.Trim().Length, 45)],

                UserAgent =
        string.IsNullOrWhiteSpace(userAgent)
            ? null
            : userAgent.Trim()[..Math.Min(userAgent.Trim().Length, 500)],

                OccurredOn = DateTime.Now
            };

            _context.TableLoginHistory.Add(history);

            await _context.SaveChangesAsync();
        }

        public async Task<List<LoginHistoryModel>> GetRecentAsync(
            int count = 100)
        {
            if (count <= 0)
            {
                count = 100;
            }

            if (count > 500)
            {
                count = 500;
            }

            return await _context.TableLoginHistory
                .AsNoTracking()
                .Include(x => x.User)
                .OrderByDescending(x => x.OccurredOn)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> HasSuccessfulLoginTodayAsync(string userId)
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            return await _context.TableLoginHistory
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.EventType == "Login" &&
                    x.IsSuccessful &&
                    x.OccurredOn >= today &&
                    x.OccurredOn < tomorrow);
        }
    }
}