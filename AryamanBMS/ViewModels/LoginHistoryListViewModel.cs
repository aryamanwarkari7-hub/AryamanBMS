using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class LoginHistoryListViewModel
    {
        public List<LoginHistoryModel> Records { get; set; } = new();

        public string? SearchText { get; set; }

        public string? EventType { get; set; }

        public string? Result { get; set; }

        public int? Month { get; set; }

        public int? Year { get; set; }

        public List<int> AvailableYears { get; set; } = new();

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int TotalRecords { get; set; }


    }
}