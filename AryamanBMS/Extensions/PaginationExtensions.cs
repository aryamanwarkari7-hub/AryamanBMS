using AryamanBMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Extensions
{
    public static class PaginationExtensions
    {
        public static async Task<PagedListViewModel<T>>
            ToPagedListAsync<T>(
                this IQueryable<T> query,
                int page,
                int pageSize,
                Dictionary<string, string>? routeValues = null)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            int totalRecords = await query.CountAsync();

            int totalPages = pageSize > 0
                ? (int)Math.Ceiling(
                    totalRecords / (double)pageSize)
                : 0;

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedListViewModel<T>
            {
                Items = items,

                Pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    RouteValues = routeValues
                        ?? new Dictionary<string, string>()
                }
            };
        }
    }
}