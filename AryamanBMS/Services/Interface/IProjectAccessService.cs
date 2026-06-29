using System.Security.Claims;
using AryamanBMS.Models;

namespace AryamanBMS.Services.Interfaces
{
    public interface IProjectAccessService
    {
        bool IsAdminOrHR(ClaimsPrincipal user);

        Task<int?> GetCurrentEmployeeIdAsync(ClaimsPrincipal user);

        Task<bool> CanAccessProjectAsync(
            ClaimsPrincipal user,
            int projectId);

        Task<IQueryable<ProjectModel>> ApplyProjectFilterAsync(
            ClaimsPrincipal user,
            IQueryable<ProjectModel> projects);
    }
}