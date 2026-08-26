using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IDebitNoteService
{
    Task<DebitNoteValidationData> ValidateAsync(DebitNoteModel note);
    Task CreateAsync(DebitNoteModel note, string? userId);
}
