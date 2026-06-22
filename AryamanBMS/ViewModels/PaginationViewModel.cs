namespace AryamanBMS.ViewModels
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages =>
            PageSize <= 0
                ? 0
                : (int)Math.Ceiling(
                    TotalRecords / (double)PageSize);

        public bool HasPreviousPage =>
            CurrentPage > 1;

        public bool HasNextPage =>
            CurrentPage < TotalPages;

        public string ActionName { get; set; } = "Index";

        public string? ControllerName { get; set; }

        public Dictionary<string, string> RouteValues { get; set; }
            = new();
    }
}