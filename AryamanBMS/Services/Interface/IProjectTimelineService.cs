namespace AryamanBMS.Services.Interfaces
{
    public interface IProjectTimelineService
    {
        Task AddEventAsync(
            int projectId,
            string eventType,
            string eventTitle,
            string? eventDescription = null,
            string? relatedEntityType = null,
            int? relatedEntityId = null,
            string? previousValue = null,
            string? newValue = null,
            DateTime? eventDate = null,
            bool isSystemGenerated = true);
    }
}