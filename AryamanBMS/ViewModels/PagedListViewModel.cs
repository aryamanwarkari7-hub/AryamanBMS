namespace AryamanBMS.ViewModels
{
    public class PagedListViewModel<T>
    {
        public IReadOnlyList<T> Items { get; set; }
            = Array.Empty<T>();

        public PaginationViewModel Pagination { get; set; }
            = new();
    }
}