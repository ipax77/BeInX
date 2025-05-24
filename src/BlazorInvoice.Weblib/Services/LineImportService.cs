using BlazorInvoice.Shared;
using pax.XRechnung.NET.AnnotatedDtos;
using System.Globalization;

namespace BlazorInvoice.Weblib.Services;

public static class LineImportService
{
    public static LineImportResult ImportLines(string import)
    {
        if (string.IsNullOrEmpty(import))
        {
            return LineImportResult.Failure("Nothing to import.");
        }

        var lines = import.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).ToList();

        if (lines.Count == 0)
        {
            return LineImportResult.Failure("No lines found.");
        }

        // Parse headers and validate
        var headers = lines[0].Split('\t', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).ToList();

        var headerDict = GetHeaderIndices(headers);

        var missingHeaders = headerDict.Where(x => x.Value < 0)
            .Select(s => s.Key.ToString())
            .ToList();

        if (missingHeaders.Count != 0)
        {
            // return LineImportResult.Failure($"Missing headers: {string.Join(", ", missingHeaders)}");
            headerDict = defaultHeader;
            lines.Insert(1, lines[0]);
        }

        var maxIndex = headerDict.Values.Max();

        // Parse data rows
        var invoiceLines = new List<InvoiceLineAnnotationDto>();
        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrEmpty(lines[i].Trim()))
            {
                continue;
            }
            var fields = lines[i].Split('\t', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).ToList();

            if (fields.Count <= maxIndex)
            {
                return LineImportResult.Failure($"Missing entities in line {i}: {lines[i]}");
            }

            try
            {
                var description = fields[headerDict[LineHeader.Desc]];
                var date = fields[headerDict[LineHeader.Date]];
                var start = fields[headerDict[LineHeader.Start]];
                var end = fields[headerDict[LineHeader.End]];
                var hours = ParseHoursToDecimal(fields[headerDict[LineHeader.Hours]]);
                var rate = ParseCurrency(fields[headerDict[LineHeader.Rate]], i);
                var amount = ParseCurrency(fields[headerDict[LineHeader.Amount]], i);

                DateTime startDate = ParseDate(date + " " + start);
                DateTime endDate = ParseDate(date + " " + end);

                var invoiceLine = new InvoiceLineAnnotationDto
                {
                    Id = i.ToString(),
                    Name = description,
                    // Description = date,
                    Quantity = (double)hours,
                    QuantityCode = "HUR",
                    UnitPrice = (double)rate,
                    StartDate = startDate,
                    EndDate = endDate,
                };

                invoiceLines.Add(invoiceLine);
            }
            catch (Exception ex)
            {
                return LineImportResult.Failure($"Error processing line {i}: {lines[i]}. {ex.Message}");
            }
        }

        return LineImportResult.Success(invoiceLines);
    }

    public static DateTime ParseDate(string dateStr)
    {
        string[] formats = {
            "d.M.yyyy HH:mm", // German
            "d.M.yyyy HH:mm", // German
            "d/M/yyyy HH:mm", // French, Spanish
            "M/d/yyyy HH:mm", // English (US)
            "yyyy-M-d HH:mm", // ISO
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out DateTime parsed))
            {
                return parsed;
            }
        }

        throw new FormatException("Unsupported date format.");
    }

    private static decimal ParseCurrency(string currency, int line)
    {
        string cleanedCurrency = currency.Trim('€').Trim();

        if (decimal.TryParse(cleanedCurrency, NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal result))
        {
            return decimal.Round(result, 2, MidpointRounding.AwayFromZero);
        }

        if (decimal.TryParse(cleanedCurrency, NumberStyles.Currency, CultureInfo.InvariantCulture, out result))
        {
            return decimal.Round(result, 2, MidpointRounding.AwayFromZero);
        }

        if (decimal.TryParse(cleanedCurrency, NumberStyles.Currency, new CultureInfo("de-DE"), out result))
        {
            return decimal.Round(result, 2, MidpointRounding.AwayFromZero);
        }

        throw new FormatException($"Invalid currency format in line {line}: {currency}");
    }


    private static Dictionary<LineHeader, int> GetHeaderIndices(List<string> headers)
    {
        var headerDict = new Dictionary<LineHeader, int>
        {
            [LineHeader.Date] = GetHeaderIndex(headers, "Datum"),
            [LineHeader.Desc] = GetHeaderIndex(headers, "Beschreibung"),
            [LineHeader.Start] = GetHeaderIndex(headers, "start"),
            [LineHeader.End] = GetHeaderIndex(headers, "ende"),
            [LineHeader.Hours] = GetHeaderIndex(headers, "stunden"),
            [LineHeader.Rate] = GetHeaderIndex(headers, "stundensatz"),
            [LineHeader.Amount] = headers.Contains("summe") ? GetHeaderIndex(headers, "summe") : headers.Count - 1
        };
        return headerDict;
    }

    private static Dictionary<LineHeader, int> defaultHeader =>
        new Dictionary<LineHeader, int>
        {
            [LineHeader.Date] = 0,
            [LineHeader.Desc] = 1,
            [LineHeader.Start] = 2,
            [LineHeader.End] = 3,
            [LineHeader.Hours] = 4,
            [LineHeader.Rate] = 5,
            [LineHeader.Amount] = 6
        };

    private static int GetHeaderIndex(List<string> headers, string headerName)
    {
        return headers.FindIndex(h => string.Equals(h.Trim(), headerName, StringComparison.OrdinalIgnoreCase));
    }

    private static decimal ParseHoursToDecimal(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            throw new ArgumentException("Time string is null or empty.");
        }

        decimal hours;

        if (time.Contains(':'))
        {
            var parts = time.Split(':');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out var dateHours) ||
                !int.TryParse(parts[1], out var minutes))
            {
                throw new FormatException($"Invalid time format: {time}. Expected format HH:mm.");
            }

            hours = dateHours + (minutes / 60m);
        }
        else
        {
            if (!decimal.TryParse(time, NumberStyles.Float, CultureInfo.CurrentCulture, out hours) &&
                !decimal.TryParse(time, NumberStyles.Float, CultureInfo.InvariantCulture, out hours) &&
                !decimal.TryParse(time, NumberStyles.Float, new CultureInfo("de-DE"), out hours))
            {
                throw new FormatException($"Invalid time format: {time}. Expected format HH:mm or decimal hours.");
            }
        }

        return Math.Round(hours, 6, MidpointRounding.AwayFromZero);
    }


    private enum LineHeader
    {
        Date,
        Desc,
        Start,
        End,
        Hours,
        Rate,
        Amount
    }
}

