using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AryamanBMS.Services
{
    public class ProposalDocumentService
        : IProposalDocumentService
    {
        private const string DocxContentType =
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IWebHostEnvironment _environment;

        private static readonly Dictionary<
            string,
            ProposalListStyle> StructuredPlaceholders =
                new(StringComparer.Ordinal)
                {
                    ["{{Scope}}"] =
                        ProposalListStyle.Numbered,

                    ["{{Deliverables}}"] =
                        ProposalListStyle.Numbered,

                    ["{{OutOfScope}}"] =
                        ProposalListStyle.Numbered,

                    ["{{CustomerResponsibilities}}"] =
                        ProposalListStyle.Numbered,

                    ["{{Dependencies}}"] =
                        ProposalListStyle.Bullet,

                    ["{{Assumptions}}"] =
                        ProposalListStyle.Bullet,

                    ["{{Risks}}"] =
                        ProposalListStyle.Bullet,

                    ["{{PaymentTerms}}"] =
                        ProposalListStyle.Numbered
                };

        public ProposalDocumentService(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            IWebHostEnvironment environment)
        {
            _context = context;
            _fileStorage = fileStorage;
            _environment = environment;
        }

        public async Task<ProposalDocumentVersionModel>
            GenerateAsync(
                ProposalModel proposal,
                string generatedByUserId)
        {
            if (proposal.ProposalId <= 0)
            {
                throw new InvalidOperationException(
                    "Proposal must be saved before generating its document.");
            }

            if (string.IsNullOrWhiteSpace(
                    generatedByUserId))
            {
                throw new InvalidOperationException(
                    "Generated-by user is required.");
            }

            var client =
                await _context.Clients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ClientId ==
                        proposal.ClientId);

            if (client == null)
            {
                throw new InvalidOperationException(
                    "Client record was not found.");
            }

            if (!proposal.ProposalTemplateId.HasValue)
            {
                throw new InvalidOperationException(
                    "No proposal template was selected.");
            }

            var template =
                await _context.ProposalTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ProposalTemplateId ==
                        proposal.ProposalTemplateId.Value);

            if (template == null)
            {
                throw new InvalidOperationException(
                    "The selected proposal template was not found.");
            }

            var templateBytes =
                await _fileStorage.DownloadAsync(
                    template.StoredFilePath);

            if (templateBytes == null ||
                templateBytes.Length == 0)
            {
                throw new FileNotFoundException(
                    "The selected proposal template file was not found.");
            }

            int nextDocumentVersion =
                await _context
                    .ProposalDocumentVersions
                    .Where(x =>
                        x.ProposalId ==
                        proposal.ProposalId)
                    .Select(x =>
                        (int?)x.VersionNumber)
                    .MaxAsync() ?? 0;

            nextDocumentVersion++;

            string revision =
                NormalizeRevision(
                    proposal.RevisionNumber);

            string proposalNumber =
                SanitizeFileName(
                    proposal.ProposalNumber);

            string generatedFileName =
                $"{proposalNumber}_Rev{revision}_V{nextDocumentVersion}.docx";

            string storedFileName =
                $"{Guid.NewGuid():N}.docx";

            string relativePath =
                Path.Combine(
                        "ProposalDocuments",
                        storedFileName)
                    .Replace("\\", "/");

            string outputDirectory =
                Path.Combine(
                    _environment.ContentRootPath,
                    "App_Data",
                    "ProposalDocuments");

            Directory.CreateDirectory(
                outputDirectory);

            string physicalPath =
                Path.Combine(
                    outputDirectory,
                    storedFileName);

            var placeholders =
                BuildPlaceholderDictionary(
                    proposal,
                    client);

            byte[] generatedBytes =
                GenerateDocumentBytes(
                    templateBytes,
                    placeholders);

            await File.WriteAllBytesAsync(
                physicalPath,
                generatedBytes);

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var previousVersions =
                    await _context
                        .ProposalDocumentVersions
                        .Where(x =>
                            x.ProposalId ==
                                proposal.ProposalId &&
                            x.IsCurrent)
                        .ToListAsync();

                foreach (var previous in
                         previousVersions)
                {
                    previous.IsCurrent = false;
                }

                var documentVersion =
                    new ProposalDocumentVersionModel
                    {
                        ProposalId =
                            proposal.ProposalId,

                        ProposalTemplateId =
                            template.ProposalTemplateId,

                        VersionNumber =
                            nextDocumentVersion,

                        OriginalFileName =
                            generatedFileName,

                        StoredFilePath =
                            relativePath,

                        ContentType =
                            DocxContentType,

                        FileSize =
                            generatedBytes.LongLength,

                        GeneratedByUserId =
                            generatedByUserId,

                        GeneratedOn =
                            DateTime.Now,

                        IsCurrent =
                            true,

                        Remarks =
                            $"Generated using template " +
                            $"{template.TemplateName}, " +
                            $"version {template.VersionNumber}."
                    };

                await _context
                    .ProposalDocumentVersions
                    .AddAsync(documentVersion);

                var trackedProposal =
                    await _context.Proposals
                        .FirstOrDefaultAsync(x =>
                            x.ProposalId ==
                            proposal.ProposalId);

                if (trackedProposal == null)
                {
                    throw new InvalidOperationException(
                        "Proposal record was not found.");
                }

                trackedProposal.ProposalTemplateId =
                    template.ProposalTemplateId;

                trackedProposal.FileName =
                    generatedFileName;

                trackedProposal.StoredFileName =
                    storedFileName;

                trackedProposal.FilePath =
                    relativePath;

                trackedProposal.FileExtension =
                    ".docx";

                trackedProposal.ContentType =
                    DocxContentType;

                trackedProposal.FileSize =
                    generatedBytes.LongLength;

                trackedProposal.VersionNo =
                    nextDocumentVersion;

                trackedProposal.UpdatedOn =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return documentVersion;
            }
            catch
            {
                await transaction.RollbackAsync();

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }

                throw;
            }
        }

        private static Dictionary<string, string>
            BuildPlaceholderDictionary(
                ProposalModel proposal,
                ClientModel client)
        {
            string clientAddress =
                BuildClientAddress(client);

            decimal proposalAmount =
                proposal.ProposalAmount ?? 0m;

            return new Dictionary<string, string>
            {
                ["{{ProposalTitle}}"] =
                    Clean(proposal.ProposalTitle),

                ["{{ClientName}}"] =
                    Clean(client.ClientName),

                ["{{ProposalNumber}}"] =
                    Clean(proposal.ProposalNumber),

                ["{{RevisionNumber}}"] =
                    NormalizeRevision(
                        proposal.RevisionNumber),

                ["{{ProposalDate}}"] =
                    proposal.ProposalDate
                        .ToString("dd-MMM-yyyy"),

                ["{{PreparedBy}}"] =
                    Clean(proposal.PreparedBy),

                ["{{PreparedByDesignation}}"] =
                    Clean(
                        proposal.PreparedByDesignation),

                ["{{ClientAddress}}"] =
                    clientAddress,

                ["{{ContactPerson}}"] =
                    Clean(client.ContactPerson),

                ["{{ClientEmail}}"] =
                    Clean(client.Email),

                ["{{ClientPhone}}"] =
                    Clean(client.Phone),

                ["{{ClientGSTNumber}}"] =
                    Clean(client.GSTNumber),

                ["{{ClientPANNumber}}"] =
                    Clean(client.PANNumber),

                ["{{ProblemStatement}}"] =
                    Clean(proposal.ProblemStatement),

                ["{{Timeline}}"] =
                    Clean(proposal.Timeline),

                ["{{TechnicalSolution}}"] =
                    Clean(proposal.TechnicalSolution),

                ["{{Scope}}"] =
                    Clean(proposal.Scope),

                ["{{Deliverables}}"] =
                    Clean(proposal.Deliverables),

                ["{{Output}}"] =
                    Clean(proposal.Deliverables),

                ["{{OutOfScope}}"] =
                    Clean(proposal.OutOfScope),

                ["{{CustomerResponsibilities}}"] =
                    Clean(
                        proposal.CustomerResponsibilities),

                ["{{Dependencies}}"] =
                    Clean(proposal.Dependencies),

                ["{{Assumptions}}"] =
                    Clean(proposal.Assumptions),

                ["{{Risks}}"] =
                    Clean(proposal.Risks),

                ["{{Warranty}}"] =
                    Clean(proposal.Warranty),

                ["{{CommercialDescription}}"] =
                    Clean(
                        proposal.CommercialDescription),

                ["{{Currency}}"] =
                    Clean(
                        proposal.Currency,
                        "INR"),

                ["{{ProposalAmount}}"] =
                    proposalAmount.ToString(
                        "N2",
                        CultureInfo.GetCultureInfo(
                            "en-IN")),

                ["{{AmountInWords}}"] =
                    ConvertAmountToWords(
                        proposalAmount),

                ["{{PaymentTerms}}"] =
                    Clean(proposal.PaymentTerms),

                ["{{ValidUntil}}"] =
                    proposal.ValidUntil.HasValue
                        ? proposal.ValidUntil.Value
                            .ToString("dd-MMM-yyyy")
                        : string.Empty
            };
        }

        private static byte[] GenerateDocumentBytes(
            byte[] templateBytes,
            IReadOnlyDictionary<string, string>
                replacements)
        {
            using var stream =
                new MemoryStream();

            stream.Write(
                templateBytes,
                0,
                templateBytes.Length);

            stream.Position = 0;

            using (var document =
                   WordprocessingDocument.Open(
                       stream,
                       true))
            {
                var mainDocumentPart =
                    document.MainDocumentPart;

                if (mainDocumentPart?.Document != null)
                {
                    var numberingIds =
                        EnsureListNumbering(
                            mainDocumentPart);

                    ReplaceStructuredPlaceholders(
                        mainDocumentPart.Document,
                        replacements,
                        numberingIds);

                    ReplacePlaceholders(
                        mainDocumentPart.Document,
                        replacements);

                    mainDocumentPart.Document.Save();
                }

                if (mainDocumentPart != null)
                {
                    foreach (var headerPart in
                             mainDocumentPart.HeaderParts)
                    {
                        if (headerPart.Header == null)
                            continue;

                        ReplacePlaceholders(
                            headerPart.Header,
                            replacements);

                        headerPart.Header.Save();
                    }

                    foreach (var footerPart in
                             mainDocumentPart.FooterParts)
                    {
                        if (footerPart.Footer == null)
                            continue;

                        ReplacePlaceholders(
                            footerPart.Footer,
                            replacements);

                        footerPart.Footer.Save();
                    }
                }
            }

            return stream.ToArray();
        }

        private static void ReplaceStructuredPlaceholders(
            OpenXmlElement root,
            IReadOnlyDictionary<string, string>
                replacements,
            ListNumberingIds numberingIds)
        {
            var paragraphs =
                root.Descendants<Paragraph>()
                    .ToList();

            foreach (var paragraph in paragraphs)
            {
                string paragraphText =
                    string.Concat(
                            paragraph
                                .Descendants<Text>()
                                .Select(x => x.Text))
                        .Trim();

                var matchingPlaceholder =
                    StructuredPlaceholders
                        .FirstOrDefault(x =>
                            string.Equals(
                                paragraphText,
                                x.Key,
                                StringComparison.Ordinal));

                if (string.IsNullOrWhiteSpace(
                        matchingPlaceholder.Key))
                {
                    continue;
                }

                replacements.TryGetValue(
                    matchingPlaceholder.Key,
                    out string? replacementValue);

                int numberingId =
                    matchingPlaceholder.Value ==
                    ProposalListStyle.Numbered
                        ? numberingIds.NumberedId
                        : numberingIds.BulletId;

                ReplaceParagraphWithList(
                    paragraph,
                    replacementValue,
                    numberingId);
            }
        }

        private static void ReplaceParagraphWithList(
            Paragraph originalParagraph,
            string? replacementValue,
            int numberingId)
        {
            var parent =
                originalParagraph.Parent;

            if (parent == null)
                return;

            var items =
                GetStructuredLines(
                    replacementValue);

            if (items.Count == 0)
            {
                var blankParagraph =
                    CreateListParagraph(
                        originalParagraph,
                        string.Empty,
                        null);

                parent.InsertBefore(
                    blankParagraph,
                    originalParagraph);

                originalParagraph.Remove();

                return;
            }

            foreach (string item in items)
            {
                var newParagraph =
                    CreateListParagraph(
                        originalParagraph,
                        item,
                        numberingId);

                parent.InsertBefore(
                    newParagraph,
                    originalParagraph);
            }

            originalParagraph.Remove();
        }

        private static Paragraph CreateListParagraph(
            Paragraph templateParagraph,
            string value,
            int? numberingId)
        {
            var paragraph =
                new Paragraph();

            ParagraphProperties properties;

            if (templateParagraph
                    .ParagraphProperties != null)
            {
                properties =
                    (ParagraphProperties)
                    templateParagraph
                        .ParagraphProperties
                        .CloneNode(true);
            }
            else
            {
                properties =
                    new ParagraphProperties();
            }

            properties.RemoveAllChildren<
                NumberingProperties>();

            if (numberingId.HasValue)
            {
                var numberingProperties =
                    new NumberingProperties(
                        new NumberingLevelReference
                        {
                            Val = 0
                        },
                        new NumberingId
                        {
                            Val =
                                numberingId.Value
                        });

                properties.AppendChild(
                    numberingProperties);
            }

            properties.SpacingBetweenLines =
                new SpacingBetweenLines
                {
                    After = "80",
                    Line = "276",
                    LineRule =
                        LineSpacingRuleValues.Auto
                };

            paragraph.AppendChild(properties);

            var run =
                new Run();

            var sourceRunProperties =
                templateParagraph
                    .Descendants<Run>()
                    .FirstOrDefault()?
                    .RunProperties;

            if (sourceRunProperties != null)
            {
                run.RunProperties =
                    (RunProperties)
                    sourceRunProperties
                        .CloneNode(true);
            }

            run.AppendChild(
                new Text(value)
                {
                    Space =
                        SpaceProcessingModeValues.Preserve
                });

            paragraph.AppendChild(run);

            return paragraph;
        }

        private static List<string> GetStructuredLines(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            string normalized =
                value.Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Trim();

            var newlineItems =
                normalized
                    .Split(
                        '\n',
                        StringSplitOptions
                            .RemoveEmptyEntries |
                        StringSplitOptions
                            .TrimEntries)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .ToList();

            List<string> items;

            if (newlineItems.Count > 1)
            {
                items = newlineItems;
            }
            else
            {
                items =
                    Regex.Split(
                            normalized,
                            @"(?=\b\d+[.)]\s+)")
                        .Select(x => x.Trim())
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x))
                        .ToList();

                if (items.Count <= 1)
                {
                    items =
                        Regex.Split(
                                normalized,
                                @"(?=[•●▪]\s*)")
                            .Select(x => x.Trim())
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .ToList();
                }
            }

            return items
                .Select(RemoveManualListPrefix)
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private static string RemoveManualListPrefix(
            string value)
        {
            return Regex.Replace(
                    value.Trim(),
                    @"^(?:\d+[.)]|[•●▪\-])\s*",
                    string.Empty)
                .Trim();
        }

        private static ListNumberingIds EnsureListNumbering(
            MainDocumentPart mainDocumentPart)
        {
            var numberingPart =
                mainDocumentPart
                    .NumberingDefinitionsPart;

            if (numberingPart == null)
            {
                numberingPart =
                    mainDocumentPart
                        .AddNewPart<
                            NumberingDefinitionsPart>();

                numberingPart.Numbering =
                    new Numbering();
            }

            numberingPart.Numbering ??=
                new Numbering();

            int nextAbstractId =
                numberingPart.Numbering
                    .Elements<AbstractNum>()
                    .Select(x =>
                        x.AbstractNumberId?.Value ?? 0)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

            int nextNumberingId =
                numberingPart.Numbering
                    .Elements<NumberingInstance>()
                    .Select(x =>
                        x.NumberID?.Value ?? 0)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

            int numberedAbstractId =
                nextAbstractId;

            int bulletAbstractId =
                nextAbstractId + 1;

            int numberedId =
                nextNumberingId;

            int bulletId =
                nextNumberingId + 1;

            var numberedAbstract =
                CreateNumberedAbstractDefinition(
                    numberedAbstractId);

            var bulletAbstract =
                CreateBulletAbstractDefinition(
                    bulletAbstractId);

            var numberedInstance =
                new NumberingInstance(
                    new AbstractNumId
                    {
                        Val =
                            numberedAbstractId
                    })
                {
                    NumberID =
                        numberedId
                };

            var bulletInstance =
                new NumberingInstance(
                    new AbstractNumId
                    {
                        Val =
                            bulletAbstractId
                    })
                {
                    NumberID =
                        bulletId
                };

            numberingPart.Numbering.Append(
                numberedAbstract,
                bulletAbstract,
                numberedInstance,
                bulletInstance);

            numberingPart.Numbering.Save();

            return new ListNumberingIds(
                numberedId,
                bulletId);
        }

        private static AbstractNum
            CreateNumberedAbstractDefinition(
                int abstractNumberingId)
        {
            var level =
                new Level
                {
                    LevelIndex = 0
                };

            level.Append(
                new StartNumberingValue
                {
                    Val = 1
                });

            level.Append(
                new NumberingFormat
                {
                    Val =
                        NumberFormatValues.Decimal
                });

            level.Append(
                new LevelText
                {
                    Val = "%1."
                });

            level.Append(
                new LevelJustification
                {
                    Val =
                        LevelJustificationValues.Left
                });

            level.Append(
                new PreviousParagraphProperties(
                    new Indentation
                    {
                        Left = "720",
                        Hanging = "360"
                    }));

            return new AbstractNum(level)
            {
                AbstractNumberId =
                    abstractNumberingId
            };
        }

        private static AbstractNum
            CreateBulletAbstractDefinition(
                int abstractNumberingId)
        {
            var level =
                new Level
                {
                    LevelIndex = 0
                };

            level.Append(
                new StartNumberingValue
                {
                    Val = 1
                });

            level.Append(
                new NumberingFormat
                {
                    Val =
                        NumberFormatValues.Bullet
                });

            level.Append(
                new LevelText
                {
                    Val = "•"
                });

            level.Append(
                new LevelJustification
                {
                    Val =
                        LevelJustificationValues.Left
                });

            level.Append(
                new PreviousParagraphProperties(
                    new Indentation
                    {
                        Left = "720",
                        Hanging = "360"
                    }));

            return new AbstractNum(level)
            {
                AbstractNumberId =
                    abstractNumberingId
            };
        }

        private static void ReplacePlaceholders(
            OpenXmlElement root,
            IReadOnlyDictionary<string, string>
                replacements)
        {
            var paragraphs =
                root.Descendants<Paragraph>()
                    .ToList();

            foreach (var paragraph in paragraphs)
            {
                ReplaceInParagraph(
                    paragraph,
                    replacements);
            }

            ReplaceInLooseTextNodes(
                root,
                replacements);
        }

        private static void ReplaceInParagraph(
            Paragraph paragraph,
            IReadOnlyDictionary<string, string>
                replacements)
        {
            var textNodes =
                paragraph.Descendants<Text>()
                    .ToList();

            if (textNodes.Count == 0)
                return;

            string combinedText =
                string.Concat(
                    textNodes.Select(x => x.Text));

            string replacedText =
                ReplaceTokens(
                    combinedText,
                    replacements);

            if (string.Equals(
                    combinedText,
                    replacedText,
                    StringComparison.Ordinal))
            {
                return;
            }

            textNodes[0].Text =
                replacedText;

            textNodes[0].Space =
                SpaceProcessingModeValues.Preserve;

            for (int i = 1;
                 i < textNodes.Count;
                 i++)
            {
                textNodes[i].Text =
                    string.Empty;
            }
        }

        private static void ReplaceInLooseTextNodes(
            OpenXmlElement root,
            IReadOnlyDictionary<string, string>
                replacements)
        {
            var paragraphTextNodes =
                root.Descendants<Paragraph>()
                    .SelectMany(x =>
                        x.Descendants<Text>())
                    .ToHashSet();

            foreach (var textNode in
                     root.Descendants<Text>())
            {
                if (paragraphTextNodes.Contains(
                        textNode))
                {
                    continue;
                }

                textNode.Text =
                    ReplaceTokens(
                        textNode.Text,
                        replacements);

                textNode.Space =
                    SpaceProcessingModeValues.Preserve;
            }
        }

        private static string ReplaceTokens(
            string source,
            IReadOnlyDictionary<string, string>
                replacements)
        {
            string result =
                source;

            foreach (var replacement in
                     replacements)
            {
                result =
                    result.Replace(
                        replacement.Key,
                        replacement.Value ??
                        string.Empty,
                        StringComparison.Ordinal);
            }

            return result;
        }

        private static string BuildClientAddress(
            ClientModel client)
        {
            var parts =
                new List<string>();

            AddUniqueAddressPart(
                parts,
                client.Address);

            AddUniqueAddressPart(
                parts,
                client.City);

            AddUniqueAddressPart(
                parts,
                client.State);

            return string.Join(", ", parts);
        }

        private static void AddUniqueAddressPart(
            List<string> parts,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string cleaned =
                value.Trim();

            bool alreadyIncluded =
                parts.Any(existing =>
                    existing.Contains(
                        cleaned,
                        StringComparison.OrdinalIgnoreCase) ||
                    cleaned.Contains(
                        existing,
                        StringComparison.OrdinalIgnoreCase));

            if (!alreadyIncluded)
            {
                parts.Add(cleaned);
            }
        }

        private static string NormalizeRevision(
            string? revision)
        {
            return string.IsNullOrWhiteSpace(
                    revision)
                ? "00"
                : revision.Trim();
        }

        private static string Clean(
            string? value,
            string defaultValue = "")
        {
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : value.Trim();
        }

        private static string SanitizeFileName(
            string? value)
        {
            string result =
                string.IsNullOrWhiteSpace(value)
                    ? "Proposal"
                    : value.Trim();

            foreach (char invalidCharacter in
                     Path.GetInvalidFileNameChars())
            {
                result =
                    result.Replace(
                        invalidCharacter,
                        '_');
            }

            return result;
        }

        private static string ConvertAmountToWords(
            decimal amount)
        {
            long rupees =
                Convert.ToInt64(
                    Math.Floor(amount));

            int paise =
                Convert.ToInt32(
                    Math.Round(
                        (amount - rupees) * 100,
                        MidpointRounding.AwayFromZero));

            if (paise == 100)
            {
                rupees++;
                paise = 0;
            }

            string result =
                $"Rupees {NumberToIndianWords(rupees)}";

            if (paise > 0)
            {
                result +=
                    $" and {NumberToIndianWords(paise)} Paise";
            }

            return result + " Only";
        }

        private static string NumberToIndianWords(
            long number)
        {
            if (number == 0)
                return "Zero";

            if (number < 0)
            {
                return "Minus " +
                       NumberToIndianWords(
                           Math.Abs(number));
            }

            var words =
                new StringBuilder();

            if (number >= 10_000_000)
            {
                words.Append(
                    NumberToIndianWords(
                        number / 10_000_000));

                words.Append(" Crore ");

                number %= 10_000_000;
            }

            if (number >= 100_000)
            {
                words.Append(
                    NumberToIndianWords(
                        number / 100_000));

                words.Append(" Lakh ");

                number %= 100_000;
            }

            if (number >= 1_000)
            {
                words.Append(
                    NumberToIndianWords(
                        number / 1_000));

                words.Append(" Thousand ");

                number %= 1_000;
            }

            if (number >= 100)
            {
                words.Append(
                    NumberToIndianWords(
                        number / 100));

                words.Append(" Hundred ");

                number %= 100;
            }

            if (number > 0)
            {
                string[] units =
                {
                    "",
                    "One",
                    "Two",
                    "Three",
                    "Four",
                    "Five",
                    "Six",
                    "Seven",
                    "Eight",
                    "Nine",
                    "Ten",
                    "Eleven",
                    "Twelve",
                    "Thirteen",
                    "Fourteen",
                    "Fifteen",
                    "Sixteen",
                    "Seventeen",
                    "Eighteen",
                    "Nineteen"
                };

                string[] tens =
                {
                    "",
                    "",
                    "Twenty",
                    "Thirty",
                    "Forty",
                    "Fifty",
                    "Sixty",
                    "Seventy",
                    "Eighty",
                    "Ninety"
                };

                if (number < 20)
                {
                    words.Append(
                        units[number]);
                }
                else
                {
                    words.Append(
                        tens[number / 10]);

                    if (number % 10 > 0)
                    {
                        words.Append(' ');

                        words.Append(
                            units[number % 10]);
                    }
                }
            }

            return words.ToString().Trim();
        }

        private enum ProposalListStyle
        {
            Numbered,
            Bullet
        }

        private sealed record ListNumberingIds(
            int NumberedId,
            int BulletId);
    }
}