using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class LoginHistoryRepository : ILoginHistoryRepository
    {
        private readonly LoginHistoryDbContext _context;


        public LoginHistoryRepository(LoginHistoryDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(
            LoginHistoryModel history)
        {
            _context.TableLoginHistory.Add(history);

            await _context.SaveChangesAsync();
        }

        public async Task<List<LoginHistoryModel>> GetRecentAsync(
            int count)
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

        public async Task<bool> HasSuccessfulLoginTodayAsync(
            string userId)
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

        public async Task<List<int>> GetAvailableYearsAsync()
        {
            return await _context.TableLoginHistory
                .AsNoTracking()
                .Select(x => x.OccurredOn.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();
        }

        public async Task<(List<LoginHistoryModel> Records, int TotalRecords)>
            SearchAsync(
                string? searchText,
                string? eventType,
                string? result,
                int? month,
                int? year,
                int page,
                int pageSize)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 15;
            }

            var query = _context.TableLoginHistory
                .AsNoTracking()
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                query = query.Where(x =>
                    x.AttemptedUserName.Contains(search) ||
                    (x.IpAddress != null &&
                     x.IpAddress.Contains(search)) ||
                    (x.User != null &&
                     x.User.FullName != null &&
                     x.User.FullName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(x =>
                    x.EventType == eventType);
            }

            if (result == "Success")
            {
                query = query.Where(x =>
                    x.IsSuccessful);
            }
            else if (result == "Failed")
            {
                query = query.Where(x =>
                    !x.IsSuccessful);
            }

            if (year.HasValue)
            {
                query = query.Where(x =>
                    x.OccurredOn.Year == year.Value);
            }

            if (month.HasValue)
            {
                query = query.Where(x =>
                    x.OccurredOn.Month == month.Value);
            }

            int totalRecords = await query.CountAsync();

            var records = await query
                .OrderByDescending(x => x.OccurredOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (records, totalRecords);
        }
    }
}