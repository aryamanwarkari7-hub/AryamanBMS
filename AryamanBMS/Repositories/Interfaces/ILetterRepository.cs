using AryamanBMS.Models;

public interface ILetterRepository
{
    IQueryable<LetterModel> Letters { get; }

    Task<LetterModel?> GetByIdAsync(int id);

    Task AddAsync(LetterModel letter);

    Task UpdateAsync(LetterModel letter);

    Task DeleteAsync(LetterModel letter);

    Task SaveAsync();
}