using System.Text.RegularExpressions;

namespace AryamanBMS.Utilities.Helpers
{
    public static class GstinValidator
    {
        private const string Characters =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static readonly Regex StructureRegex = new(
            @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
            RegexOptions.Compiled);

        public static string? Normalize(string? gstin)
        {
            return string.IsNullOrWhiteSpace(gstin)
                ? null
                : gstin.Trim().ToUpperInvariant();
        }

        public static bool IsValid(string? gstin)
        {
            string? normalized = Normalize(gstin);

            if (normalized == null ||
                !StructureRegex.IsMatch(normalized))
            {
                return false;
            }

            int checksumTotal = 0;

            for (int index = 0; index < 14; index++)
            {
                int characterValue =
                    Characters.IndexOf(normalized[index]);

                int product =
                    characterValue * (index % 2 == 0 ? 1 : 2);

                checksumTotal +=
                    (product / Characters.Length) +
                    (product % Characters.Length);
            }

            int checkDigitIndex =
                (Characters.Length -
                    (checksumTotal % Characters.Length)) %
                Characters.Length;

            return normalized[14] == Characters[checkDigitIndex];
        }
    }
}