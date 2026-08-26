using AryamanBMS.Models;
namespace AryamanBMS.Business.Interfaces;
public interface ICreditNoteService { Task<CreditNoteValidationData> ValidateAsync(CreditNoteModel note); Task CreateAsync(CreditNoteModel note, string? userId); }
