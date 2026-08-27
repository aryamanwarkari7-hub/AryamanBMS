using AryamanBMS.Models;
namespace AryamanBMS.Business.Interfaces;
public interface IAdvanceReceiptQueryService { Task<List<AdvanceReceiptModel>> GetAllAsync(string? search, string sortBy, string sortOrder); }
