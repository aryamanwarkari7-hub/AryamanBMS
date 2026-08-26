namespace AryamanBMS.Models;

public class CreditNoteValidationData
{
    public IReadOnlyDictionary<string, string> Errors { get; init; } = new Dictionary<string, string>();
}
