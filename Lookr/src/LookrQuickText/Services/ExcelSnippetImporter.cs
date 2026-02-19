using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LookrQuickText.Models;

namespace LookrQuickText.Services;

public sealed class ExcelSnippetImporter
{
    public IReadOnlyList<QuickTextSnippet> Import(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Excel file not found.", filePath);
        }

        using var spreadsheet = SpreadsheetDocument.Open(filePath, false);

        var workbookPart = spreadsheet.WorkbookPart
            ?? throw new InvalidDataException("Workbook data is missing.");

        var firstSheet = workbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault()
            ?? throw new InvalidDataException("Workbook has no sheets.");

        var firstSheetId = firstSheet.Id?.Value;
        if (string.IsNullOrWhiteSpace(firstSheetId))
        {
            throw new InvalidDataException("Worksheet id is missing.");
        }

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(firstSheetId);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

        if (sheetData is null)
        {
            return Array.Empty<QuickTextSnippet>();
        }

        var rows = sheetData.Elements<Row>().ToList();
        if (rows.Count < 2)
        {
            return Array.Empty<QuickTextSnippet>();
        }

        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;

        var headers = ReadRow(rows[0], sharedStrings)
            .ToDictionary(
                pair => pair.Key,
                pair => NormalizeHeader(pair.Value),
                StringComparer.OrdinalIgnoreCase);

        var titleColumn = FindColumn(headers, "title", "name", "snippet", "subject");
        var contentColumn = FindColumn(headers, "content", "text", "body", "quicktext", "message", "template");
        var categoryColumn = FindColumn(headers, "category", "group", "folder", "section");
        var keywordsColumn = FindColumn(headers, "keywords", "keyword", "tags", "tag");

        if (titleColumn is null && contentColumn is null)
        {
            throw new InvalidDataException("Excel headers must include at least 'Title' or 'Content'.");
        }

        var snippets = new List<QuickTextSnippet>();

        foreach (var row in rows.Skip(1))
        {
            var values = ReadRow(row, sharedStrings);

            var title = GetValue(values, titleColumn);
            var content = GetValue(values, contentColumn);
            var category = GetValue(values, categoryColumn);
            var keywords = GetValue(values, keywordsColumn);

            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            title = string.IsNullOrWhiteSpace(title)
                ? BuildTitleFromContent(content)
                : title.Trim();

            content = content.Trim();
            category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
            keywords = keywords.Trim();

            snippets.Add(new QuickTextSnippet
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title,
                Content = content,
                Category = category,
                Keywords = keywords,
                LastUsedUtc = DateTime.UtcNow
            });
        }

        return snippets;
    }

    private static Dictionary<string, string> ReadRow(Row row, SharedStringTable? sharedStrings)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in row.Elements<Cell>())
        {
            var cellReference = cell.CellReference?.Value;
            if (string.IsNullOrWhiteSpace(cellReference))
            {
                continue;
            }

            var column = ExtractColumnName(cellReference);
            if (string.IsNullOrWhiteSpace(column))
            {
                continue;
            }

            values[column] = ResolveCellValue(cell, sharedStrings);
        }

        return values;
    }

    private static string ResolveCellValue(Cell cell, SharedStringTable? sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString)
        {
            if (cell.CellValue?.Text is null || sharedStrings is null)
            {
                return string.Empty;
            }

            if (!int.TryParse(cell.CellValue.Text, out var index))
            {
                return string.Empty;
            }

            var item = sharedStrings.Elements<SharedStringItem>().ElementAtOrDefault(index);
            return item?.InnerText ?? string.Empty;
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }

        if (cell.DataType?.Value == CellValues.Boolean)
        {
            return cell.CellValue?.Text == "1" ? "TRUE" : "FALSE";
        }

        return cell.CellValue?.Text ?? cell.InnerText ?? string.Empty;
    }

    private static string? FindColumn(IReadOnlyDictionary<string, string> headers, params string[] names)
    {
        foreach (var pair in headers)
        {
            if (names.Contains(pair.Value, StringComparer.OrdinalIgnoreCase))
            {
                return pair.Key;
            }
        }

        return null;
    }

    private static string GetValue(IReadOnlyDictionary<string, string> row, string? column)
    {
        if (column is null)
        {
            return string.Empty;
        }

        return row.TryGetValue(column, out var value) ? value ?? string.Empty : string.Empty;
    }

    private static string BuildTitleFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Imported Snippet";
        }

        var compact = content.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return compact.Length <= 50 ? compact : compact[..50] + "...";
    }

    private static string ExtractColumnName(string cellReference)
    {
        var builder = new StringBuilder();

        foreach (var character in cellReference)
        {
            if (char.IsLetter(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                continue;
            }

            break;
        }

        return builder.ToString();
    }

    private static string NormalizeHeader(string header)
    {
        return (header ?? string.Empty)
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
