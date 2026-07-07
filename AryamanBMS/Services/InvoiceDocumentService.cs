using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

using QuestPdfDocument = QuestPDF.Fluent.Document;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace AryamanBMS.Services
{
    public class InvoiceDocumentService
        : IInvoiceDocumentService
    {
        private const string DocxContentType =
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private const string PdfContentType =
            "application/pdf";

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public InvoiceDocumentService(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<
            IReadOnlyList<InvoiceDocumentVersionModel>>
            GenerateAsync(
                int invoiceId,
                string generatedByUserId)
        {
            if (invoiceId <= 0)
            {
                throw new InvalidOperationException(
                    "A valid invoice is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    generatedByUserId))
            {
                throw new InvalidOperationException(
                    "Generated-by user is required.");
            }

            

            var invoice =
                await _context.Invoices
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.Project)
                    .Include(x => x.Proposal)
                    .Include(x => x.PurchaseOrder)
                    .Include(x => x.InvoiceDetails)
                    .FirstOrDefaultAsync(x =>
                        x.InvoiceId == invoiceId &&
                        !x.IsDeleted);

            if (invoice == null)
            {
                throw new InvalidOperationException(
                    "Invoice record was not found.");
            }

            var companyProfile = await _context.CompanyProfiles
        .AsNoTracking()
        .FirstOrDefaultAsync(x =>
            x.IsActive);

            if (companyProfile == null)
            {
                throw new InvalidOperationException(
                    "Active company profile was not found.");
            }

            bool isProforma =
                string.Equals(
                    invoice.InvoiceType,
                    "Proforma Invoice",
                    StringComparison.OrdinalIgnoreCase);

            string templateFileName =
                isProforma
                    ? "ProformaInvoiceTemplate.docx"
                    : "TaxInvoiceTemplate.docx";

            string templatePath =
                Path.Combine(
                    _environment.ContentRootPath,
                    "App_Data",
                    "InvoiceTemplates",
                    templateFileName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    $"Invoice template was not found: " +
                    $"{templateFileName}");
            }

            byte[] templateBytes =
                await File.ReadAllBytesAsync(
                    templatePath);

            int nextVersion =
                await _context
                    .InvoiceDocumentVersions
                    .Where(x =>
                        x.InvoiceId == invoiceId)
                    .Select(x =>
                        (int?)x.VersionNumber)
                    .MaxAsync() ?? 0;

            nextVersion++;

            string safeInvoiceNo =
                SanitizeFileName(
                    invoice.InvoiceNo);

            string typePrefix =
                isProforma
                    ? "Proforma"
                    : "TaxInvoice";

            string docxFileName =
                $"{typePrefix}_{safeInvoiceNo}_V{nextVersion}.docx";

            string pdfFileName =
                $"{typePrefix}_{safeInvoiceNo}_V{nextVersion}.pdf";

            string docxStoredFileName =
                $"{Guid.NewGuid():N}.docx";

            string pdfStoredFileName =
                $"{Guid.NewGuid():N}.pdf";

            string relativeDocxPath =
                Path.Combine(
                        "InvoiceDocuments",
                        docxStoredFileName)
                    .Replace("\\", "/");

            string relativePdfPath =
                Path.Combine(
                        "InvoiceDocuments",
                        pdfStoredFileName)
                    .Replace("\\", "/");

            string outputDirectory =
                Path.Combine(
                    _environment.ContentRootPath,
                    "App_Data",
                    "InvoiceDocuments");

            Directory.CreateDirectory(
                outputDirectory);

            string physicalDocxPath =
                Path.Combine(
                    outputDirectory,
                    docxStoredFileName);

            string physicalPdfPath =
                Path.Combine(
                    outputDirectory,
                    pdfStoredFileName);

            byte[] docxBytes =
                GenerateDocx(
                    templateBytes,
                    invoice,
                    companyProfile);

            byte[] pdfBytes =
              GeneratePdf(
                  invoice,
                  companyProfile);

            await File.WriteAllBytesAsync(
                physicalDocxPath,
                docxBytes);

            await File.WriteAllBytesAsync(
                physicalPdfPath,
                pdfBytes);

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var previousCurrentDocuments =
                    await _context
                        .InvoiceDocumentVersions
                        .Where(x =>
                            x.InvoiceId == invoiceId &&
                            x.IsCurrent)
                        .ToListAsync();

                foreach (var previous in
                         previousCurrentDocuments)
                {
                    previous.IsCurrent = false;
                }

                var docxVersion =
                    new InvoiceDocumentVersionModel
                    {
                        InvoiceId =
                            invoiceId,

                        VersionNumber =
                            nextVersion,

                        DocumentFormat =
                            "DOCX",

                        OriginalFileName =
                            docxFileName,

                        StoredFilePath =
                            relativeDocxPath,

                        ContentType =
                            DocxContentType,

                        FileSize =
                            docxBytes.LongLength,

                        GeneratedByUserId =
                            generatedByUserId,

                        GeneratedOn =
                            DateTime.Now,

                        IsCurrent =
                            true,

                        Remarks =
                            $"{invoice.InvoiceType} editable document."
                    };

                var pdfVersion =
                    new InvoiceDocumentVersionModel
                    {
                        InvoiceId =
                            invoiceId,

                        VersionNumber =
                            nextVersion,

                        DocumentFormat =
                            "PDF",

                        OriginalFileName =
                            pdfFileName,

                        StoredFilePath =
                            relativePdfPath,

                        ContentType =
                            PdfContentType,

                        FileSize =
                            pdfBytes.LongLength,

                        GeneratedByUserId =
                            generatedByUserId,

                        GeneratedOn =
                            DateTime.Now,

                        IsCurrent =
                            true,

                        Remarks =
                            $"{invoice.InvoiceType} final PDF document."
                    };

                await _context
                    .InvoiceDocumentVersions
                    .AddRangeAsync(
                        docxVersion,
                        pdfVersion);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new List<
                    InvoiceDocumentVersionModel>
                {
                    docxVersion,
                    pdfVersion
                };
            }
            catch
            {
                await transaction.RollbackAsync();

                if (File.Exists(
                        physicalDocxPath))
                {
                    File.Delete(
                        physicalDocxPath);
                }

                if (File.Exists(
                        physicalPdfPath))
                {
                    File.Delete(
                        physicalPdfPath);
                }

                throw;
            }
        }

        private static byte[] GenerateDocx(
            byte[] templateBytes,
            InvoiceModel invoice,
            CompanyProfileModel companyProfile)
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
                var mainPart =
                    document.MainDocumentPart;

                if (mainPart?.Document == null)
                {
                    throw new InvalidOperationException(
                        "Invoice template is invalid.");
                }

                var replacements =
                    BuildReplacements(invoice,companyProfile);

                ReplaceInvoiceItemRows(
                    mainPart.Document,
                    invoice.InvoiceDetails
                        .OrderBy(x => x.SortOrder)
                        .ToList());

                ReplaceTokens(
                    mainPart.Document,
                    replacements);

                foreach (var header in
                         mainPart.HeaderParts)
                {
                    if (header.Header == null)
                        continue;

                    ReplaceTokens(
                        header.Header,
                        replacements);

                    header.Header.Save();
                }

                foreach (var footer in
                         mainPart.FooterParts)
                {
                    if (footer.Footer == null)
                        continue;

                    ReplaceTokens(
                        footer.Footer,
                        replacements);

                    footer.Footer.Save();
                }

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private static Dictionary<string, string>BuildReplacements(InvoiceModel invoice,CompanyProfileModel companyProfile)
        {
            bool isProforma =
                string.Equals(
                    invoice.InvoiceType,
                    "Proforma Invoice",
                    StringComparison.OrdinalIgnoreCase);

            decimal taxableTotal =
                invoice.SubTotal - invoice.Discount;

            decimal cgst =
                !isProforma && !invoice.IsInterState
                    ? Math.Round(invoice.GSTAmount / 2m, 2)
                    : 0;

            decimal sgst =
                !isProforma && !invoice.IsInterState
                    ? Math.Round(invoice.GSTAmount / 2m, 2)
                    : 0;

            decimal igst =
                !isProforma && invoice.IsInterState
                    ? invoice.GSTAmount
                    : 0;

            string workOrder =
                Clean(invoice.PurchaseOrder?.OrderNumber);

            string vendorReference =
                Clean(invoice.PurchaseOrder?.VendorReference);

            string taxType =
                isProforma
                    ? string.Empty
                    : invoice.IsInterState
                        ? "IGST"
                        : "CGST + SGST";

            return new Dictionary<string, string>
            {
                ["{{InvoiceTitle}}"] =
                    isProforma
                        ? "PROFORMA INVOICE"
                        : "TAX INVOICE",

                ["{{InvoiceType}}"] =  Clean(invoice.InvoiceType),

                ["{{InvoiceNo}}"] =  Clean(invoice.InvoiceNo),

                ["{{InvoiceDate}}"] = invoice.InvoiceDate.ToString("dd-MMM-yyyy"),

                ["{{DueDate}}"] = invoice.DueDate?.ToString("dd-MMM-yyyy")
                    ?? string.Empty,

                ["{{InvoiceStatus}}"] =   Clean(invoice.InvoiceStatus),

                ["{{ClientName}}"] =     Clean(invoice.Client?.ClientName),

                ["{{ClientAddress}}"] =  Clean(invoice.BillingAddress),

                ["{{ClientGSTNo}}"] =   isProforma
                                        ? string.Empty
                                        : Clean(invoice.GSTNo),

                ["{{ClientEmail}}"] =
                    Clean(invoice.Client?.Email),

                ["{{ClientPhone}}"] =
                    Clean(invoice.Client?.Phone),

                ["{{ContactPerson}}"] =
                    Clean(invoice.Client?.ContactPerson),

                ["{{ProjectName}}"] =
                    Clean(invoice.ProjectName),

                ["{{WorkOrderNo}}"] =
                    workOrder,

                ["{{VendorRegistrationNo}}"] =  Clean(companyProfile.VendorRegistrationNumber),

                ["{{VendorReference}}"] =
                    vendorReference,

                ["{{PaymentTerms}}"] =
                    Clean(invoice.PaymentTerms),

                ["{{Remarks}}"] =
                    Clean(invoice.Remarks),

                ["{{SubTotal}}"] =
                    FormatAmount(invoice.SubTotal),

                ["{{Discount}}"] =
                    FormatAmount(invoice.Discount),

                ["{{TaxableTotal}}"] =
                    FormatAmount(taxableTotal),

                ["{{GSTAmount}}"] =
                    FormatAmount(
                        isProforma
                            ? 0
                            : invoice.GSTAmount),

                ["{{CGSTAmount}}"] =
                    FormatAmount(cgst),

                ["{{SGSTAmount}}"] =
                    FormatAmount(sgst),

                ["{{IGSTAmount}}"] =
                    FormatAmount(igst),

                ["{{GrandTotal}}"] =
                    FormatAmount(invoice.GrandTotal),

                ["{{PaidAmount}}"] =
                    FormatAmount(invoice.PaidAmount),

                ["{{BalanceAmount}}"] =
                    FormatAmount(invoice.BalanceAmount),

                ["{{AmountInWords}}"] =
                    ConvertAmountToWords(invoice.GrandTotal),

                ["{{TaxType}}"] =
                    taxType,

                /*
                 * Replace these with Company Settings later.
                 */
                ["{{CompanyName}}"] =
                    Clean(companyProfile.CompanyName),
                    
                ["{{CompanyAddress}}"] =
                    Clean(companyProfile.Address),
                    
                 ["{{CompanyEmail}}"] =
                    Clean(companyProfile.Email),
                    
                  ["{{CompanyPhone}}"] =
                    Clean(companyProfile.Phone),
                    
                 ["{{CompanyGSTIN}}"] =
                    Clean(companyProfile.GSTIN),
                    
                 ["{{CompanyPAN}}"] =
                    Clean(companyProfile.PAN),

                ["{{SACCode}}"] =   Clean(invoice.SACCode),

                ["{{ContactPerson}}"] = Clean(invoice.Client?.ContactPerson),

                ["{{ReceiverName}}"] = string.Empty,

               ["{{SACCode}}"] =  Clean(invoice.SACCode),

                ["{{ReceiverName}}"] =  string.Empty,

                ["{{BankName}}"] =    Clean(companyProfile.BankName),

                ["{{AccountNumber}}"] =  Clean(companyProfile.AccountNumber),

                ["{{IFSCCode}}"] = Clean(companyProfile.IFSCCode),

                ["{{BankBranch}}"] = Clean(companyProfile.BankBranch),

                ["{{AuthorizedSignatory}}"] =  Clean(companyProfile.AuthorizedSignatory),

                ["{{ReceiverName}}"] = string.Empty
            };
        }

        private static void ReplaceInvoiceItemRows(
    WordDocument document,
            IReadOnlyList<InvoiceDetailsModel>
                invoiceItems)
        {
            /*
             * In the Word template, one item row must contain:
             *
             * {{ItemSrNo}}
             * {{ItemDescription}}
             * {{ItemRate}}
             * {{ItemQty}}
             * {{ItemAmount}}
             */

            var templateRow =
                document
                    .Descendants<TableRow>()
                    .FirstOrDefault(row =>
                        GetElementText(row)
                            .Contains(
                                "{{ItemSrNo}}",
                                StringComparison.Ordinal));

            if (templateRow == null)
            {
                throw new InvalidOperationException(
                    "Invoice item template row was not found.");
            }

            var parent =
                templateRow.Parent;

            if (parent == null)
            {
                throw new InvalidOperationException(
                    "Invoice item table is invalid.");
            }

            int serialNumber = 1;

            foreach (var item in invoiceItems)
            {
                var clonedRow =
                    (TableRow)
                    templateRow.CloneNode(true);

                string description =
                    string.IsNullOrWhiteSpace(
                        item.Description)
                        ? item.ItemName
                        : $"{item.ItemName}{Environment.NewLine}" +
                          $"{item.Description}";

                var itemValues =
                    new Dictionary<string, string>
                    {
                        ["{{ItemSrNo}}"] =
                            serialNumber.ToString(),
                    
                        ["{{ItemName}}"] =
                            Clean(item.ItemName),
                    
                        ["{{ItemDescription}}"] =
                            Clean(item.Description),
                    
                        ["{{ItemQty}}"] =
                            item.Qty.ToString("0.##"),
                    
                        ["{{ItemUnit}}"] =
                            Clean(item.Unit),
                    
                        ["{{ItemRate}}"] =
                            FormatAmount(item.Rate),
                    
                        ["{{ItemGSTPercent}}"] =
                            item.GSTPercent.ToString("0.##"),
                    
                        ["{{ItemGSTAmount}}"] =
                            FormatAmount(item.GSTAmount),
                    
                        ["{{ItemAmount}}"] =
                            FormatAmount(item.Amount)
                    };

                ReplaceTokens(
                    clonedRow,
                    itemValues);

                parent.InsertBefore(
                    clonedRow,
                    templateRow);

                serialNumber++;
            }

            templateRow.Remove();
        }

        private static void ReplaceTokens(
            OpenXmlElement root,
            IReadOnlyDictionary<string, string>
                replacements)
        {
            var paragraphs =
                root.Descendants<Paragraph>()
                    .ToList();

            foreach (var paragraph in paragraphs)
            {
                var textNodes =
                    paragraph.Descendants<Text>()
                        .ToList();

                if (textNodes.Count == 0)
                    continue;

                string combinedText =
                    string.Concat(
                        textNodes.Select(x =>
                            x.Text));

                string replacedText =
                    combinedText;

                foreach (var replacement in
                         replacements)
                {
                    replacedText =
                        replacedText.Replace(
                            replacement.Key,
                            replacement.Value,
                            StringComparison.Ordinal);
                }

                if (combinedText ==
                    replacedText)
                {
                    continue;
                }

                textNodes[0].Text =
                    replacedText;

                textNodes[0].Space =
                    SpaceProcessingModeValues
                        .Preserve;

                for (int i = 1;
                     i < textNodes.Count;
                     i++)
                {
                    textNodes[i].Text =
                        string.Empty;
                }
            }
        }

        private static string GetElementText(
            OpenXmlElement element)
        {
            return string.Concat(
                element.Descendants<Text>()
                    .Select(x => x.Text));
        }

        private static byte[] GeneratePdf(
            InvoiceModel invoice,CompanyProfileModel companyProfile)
        {
            bool isProforma =
                invoice.InvoiceType ==
                "Proforma Invoice";

            decimal cgst =
                !isProforma &&
                !invoice.IsInterState
                    ? invoice.GSTAmount / 2
                    : 0;

            decimal sgst =
                !isProforma &&
                !invoice.IsInterState
                    ? invoice.GSTAmount / 2
                    : 0;

            decimal igst =
                !isProforma &&
                invoice.IsInterState
                    ? invoice.GSTAmount
                    : 0;

            var document = QuestPdfDocument.Create(container =>
    {
                    container.Page(page =>
                    {
                        page.Size(
                            PageSizes.A4);

                        page.MarginLeft(20);
                        page.MarginRight(20);
                        page.MarginBottom(20);

                        /*
                         * Leave space for the printed letterhead.
                         */
                        page.MarginTop(110);

                        page.DefaultTextStyle(
                            x => x.FontSize(8));

                        page.Content()
                            .Border(1)
                            .Padding(3)
                            .Column(column =>
                            {
                                column.Spacing(0);

                                column.Item()
                                    .BorderBottom(1)
                                    .AlignCenter()
                                    .Padding(3)
                                    .Text(
                                        isProforma
                                            ? "PROFORMA INVOICE"
                                            : "TAX INVOICE")
                                    .Bold()
                                    .FontSize(11);

                                column.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns
                                                    .RelativeColumn(3);

                                                columns
                                                    .RelativeColumn(1);

                                                columns
                                                    .RelativeColumn(1);
                                            });

                                        AddPdfCell(
                                            table,
                                            $"To:\n" +
                                            $"{invoice.Client?.ClientName}\n" +
                                            $"{invoice.BillingAddress}");

                                        AddPdfCell(
                                            table,
                                            "WORK ORDER:\n" +
                                            Clean(
                                                invoice.PurchaseOrder?
                                                    .OrderNumber));

                                        AddPdfCell(
                                            table,
                                            "Vendor Reg. No.:\n" +
                                            Clean(
                                                companyProfile
                                                    .VendorRegistrationNumber));

                                        AddPdfCell(
                                             table,
                                             "SELLER GSTIN:\n" +
                                             (isProforma
                                                 ? string.Empty
                                                 : Clean(companyProfile.GSTIN)));

                                        AddPdfCell(
                                              table,
                                              "SAC CODE:\n" +
                                              Clean(invoice.SACCode));

                                        AddPdfCell(
                                              table,
                                              "PAN NO.:\n" +
                                              Clean(companyProfile.PAN));

                                        AddPdfCell(
                                            table,
                                            $"KIND ATTN:\n" +
                                            $"{invoice.Client?.ContactPerson}");

                                        AddPdfCell(
                                            table,
                                            $"INVOICE DATE:\n" +
                                            $"{invoice.InvoiceDate:dd-MM-yyyy}");

                                        AddPdfCell(
                                            table,
                                            $"INVOICE NO.:\n" +
                                            $"{invoice.InvoiceNo}");
                                    });

                                column.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns
                                                    .ConstantColumn(35);

                                                columns
                                                    .RelativeColumn(5);

                                                columns
                                                    .RelativeColumn(1.3f);

                                                columns
                                                    .RelativeColumn(1.1f);

                                                columns
                                                    .RelativeColumn(1.5f);
                                            });

                                        AddPdfHeader(
                                            table,
                                            "SR. NO.");

                                        AddPdfHeader(
                                            table,
                                            "DESCRIPTION");

                                        AddPdfHeader(
                                            table,
                                            "RATE");

                                        AddPdfHeader(
                                            table,
                                            "QUANTITY");

                                        AddPdfHeader(
                                            table,
                                            "AMOUNT");

                                        int srNo = 1;

                                        foreach (var item in
                                                 invoice.InvoiceDetails
                                                     .OrderBy(x =>
                                                         x.SortOrder))
                                        {
                                            AddPdfCell(
                                                table,
                                                srNo.ToString());

                                            AddPdfCell(
                                                table,
                                                string.IsNullOrWhiteSpace(
                                                    item.Description)
                                                    ? item.ItemName
                                                    : $"{item.ItemName}\n" +
                                                      $"{item.Description}");

                                            AddPdfCell(
                                                table,
                                                FormatAmount(
                                                    item.Rate),
                                                true);

                                            AddPdfCell(
                                                table,
                                                $"{item.Qty:0.##} " +
                                                $"{item.Unit}",
                                                true);

                                            AddPdfCell(
                                                table,
                                                FormatAmount(
                                                    item.Amount),
                                                true);

                                            srNo++;
                                        }
                                    });

                                AddPdfTotalRow(
                                    column,
                                    "SUB TOTAL",
                                    invoice.SubTotal);

                                if (!isProforma)
                                {
                                    if (invoice.IsInterState)
                                    {
                                        AddPdfTotalRow(
                                            column,
                                            "IGST",
                                            igst);
                                    }
                                    else
                                    {
                                        AddPdfTotalRow(
                                            column,
                                            "CGST",
                                            cgst);

                                        AddPdfTotalRow(
                                            column,
                                            "SGST",
                                            sgst);
                                    }
                                }

                                AddPdfTotalRow(
                                    column,
                                    "GRAND TOTAL",
                                    invoice.GrandTotal,
                                    true);

                                column.Item()
                                    .Border(1)
                                    .Padding(3)
                                    .Text(
                                        $"IN WORDS: " +
                                        $"{ConvertAmountToWords(invoice.GrandTotal)}");

                               column.Item()
                                 .Border(1)
                                 .Padding(5)
                                 .Column(bank =>
                                 {
                                     bank.Item()
                                         .Text(
                                             $"Bank Details: " +
                                             $"{Clean(companyProfile.AccountName)}")
                                         .Bold();
                              
                                     bank.Item()
                                         .Text(
                                             $"Bank Name: " +
                                             $"{Clean(companyProfile.BankName)}");
                              
                                     bank.Item()
                                         .Text(
                                             $"Account No.: " +
                                             $"{Clean(companyProfile.AccountNumber)}");
                              
                                     bank.Item()
                                         .Text(
                                             $"IFSC Code: " +
                                             $"{Clean(companyProfile.IFSCCode)}");
                              
                                     bank.Item()
                                         .Text(
                                             $"Branch: " +
                                             $"{Clean(companyProfile.BankBranch)}");
                              
                                     bank.Item()
                                         .Text(
                                             $"Remarks: " +
                                             $"{Clean(invoice.Remarks)}");
                                 });

                                column.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns
                                                    .RelativeColumn();

                                                columns
                                                    .RelativeColumn();
                                            });

                                        AddPdfSignatureCell(
                                             table,
                                             "FOR ARYAMAN TECHNOLOGIES PVT. LTD.\n\n\n\n" +
                                             $"{Clean(companyProfile.AuthorizedSignatory)}\n" +
                                             "AUTHORIZED SIGNATORY");

                                        AddPdfSignatureCell(
                                            table,
                                            "RECEIVER'S SIGNATURE,\n\n\n\n\n");
                                    });
                            });
                    });
                });

            return document.GeneratePdf();
        }

        private static void AddPdfHeader(
            TableDescriptor table,
            string text)
        {
            table.Cell()
                .Border(1)
                .Padding(2)
                .AlignCenter()
                .Text(text)
                .Bold();
        }

        private static void AddPdfCell(
            TableDescriptor table,
            string? text,
            bool rightAligned = false)
        {
            var cell =
                table.Cell()
                    .Border(1)
                    .MinHeight(25)
                    .Padding(2);

            if (rightAligned)
            {
                cell.AlignRight()
                    .Text(
                        Clean(text));
            }
            else
            {
                cell.Text(
                    Clean(text));
            }
        }

        private static void AddPdfTotalRow(
            ColumnDescriptor column,
            string label,
            decimal amount,
            bool bold = false)
        {
            column.Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(
                        columns =>
                        {
                            columns
                                .RelativeColumn(4);

                            columns
                                .RelativeColumn(1);
                        });

                    var labelCell =
                        table.Cell()
                            .Border(1)
                            .Padding(2)
                            .AlignRight();

                    var amountCell =
                        table.Cell()
                            .Border(1)
                            .Padding(2)
                            .AlignRight();

                    if (bold)
                    {
                        labelCell.Text(label)
                            .Bold();

                        amountCell
                            .Text(
                                FormatAmount(amount))
                            .Bold();
                    }
                    else
                    {
                        labelCell.Text(label);

                        amountCell.Text(
                            FormatAmount(amount));
                    }
                });
        }

        private static void AddPdfSignatureCell(
            TableDescriptor table,
            string text)
        {
            table.Cell()
                .Border(1)
                .MinHeight(115)
                .Padding(3)
                .Text(text);
        }

        private static string FormatAmount(
            decimal amount)
        {
            return amount.ToString(
                "N2",
                CultureInfo.GetCultureInfo(
                    "en-IN"));
        }

        private static string Clean(
            string? value)
        {
            return string.IsNullOrWhiteSpace(
                    value)
                ? string.Empty
                : value.Trim();
        }

        private static string SanitizeFileName(
            string? value)
        {
            string result =
                string.IsNullOrWhiteSpace(value)
                    ? "Invoice"
                    : value.Trim();

            foreach (char invalid in
                     Path.GetInvalidFileNameChars())
            {
                result =
                    result.Replace(
                        invalid,
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
    }
}