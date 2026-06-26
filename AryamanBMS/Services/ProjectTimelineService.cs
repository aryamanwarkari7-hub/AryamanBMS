using System.Security.Claims;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;

namespace AryamanBMS.Services
{
    public class ProjectTimelineService
        : IProjectTimelineService
    {
        private readonly IProjectTimelineRepository
            _timelineRepository;

        private readonly IHttpContextAccessor
            _httpContextAccessor;

        public ProjectTimelineService(
            IProjectTimelineRepository timelineRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _timelineRepository = timelineRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task AddEventAsync(
            int projectId,
            string eventType,
            string eventTitle,
            string? eventDescription = null,
            string? relatedEntityType = null,
            int? relatedEntityId = null,
            string? previousValue = null,
            string? newValue = null,
            DateTime? eventDate = null,
            bool isSystemGenerated = true)
        {
            var user =
                _httpContextAccessor.HttpContext?.User;

            string? userId =
                user?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            string? userName =
                user?.Identity?.Name;

            var timeline = new ProjectTimelineModel
            {
                ProjectId = projectId,

                EventType = eventType.Trim(),

                EventTitle = eventTitle.Trim(),

                EventDescription =
                    eventDescription?.Trim(),

                RelatedEntityType =
                    relatedEntityType?.Trim(),

                RelatedEntityId = relatedEntityId,

                PreviousValue =
                    previousValue?.Trim(),

                NewValue =
                    newValue?.Trim(),

                EventDate =
                    eventDate ?? DateTime.Now,

                CreatedByUserId = userId,

                CreatedByName =
                    string.IsNullOrWhiteSpace(userName)
                        ? "System"
                        : userName,

                IsSystemGenerated =
                    isSystemGenerated,

                IsActive = true,

                CreatedOn = DateTime.Now
            };

            await _timelineRepository.AddAsync(timeline);
            await _timelineRepository.SaveAsync();
        }
    }
}