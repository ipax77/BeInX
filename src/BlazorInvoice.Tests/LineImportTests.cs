using BlazorInvoice.Shared;
using BlazorInvoice.Weblib.Services;

namespace BlazorInvoice.Tests;

[TestClass]
public class LineImportTests
{
    [TestMethod]
    public void ImportLines_ValidInput_ReturnsSuccess()
    {
        var text = @"datum	beschreibung	start	ende	stunden	    stundensatz	    summe
02.04.2025	test1	06:00	07:30	01:30	60	90,00 €
02.04.2025	test2	16:30	17:45	01:15	60	75,00 €
03.04.2025	test3	06:00	10:15	04:15	60	255,00 €
08.04.2025	test4	07:20	10:00	02:40	60	160,00 €
08.04.2025	test5	16:45	19:00	02:15	60	135,00 €
09.04.2025	test6	07:00	11:25	04:25	60	265,00 €
10.04.2025	test7	06:00	09:55	03:55	60	235,00 €
14.04.2025	test8	07:30	08:45	01:15	60	75,00 €
15.04.2025	test9	09:00	10:30	01:30	60	90,00 €
15.04.2025	test10	17:30	19:30	02:00	60	120,00 €
16.04.2025	test11	09:00	11:15	02:15	60	135,00 €
17.04.2025	test12	06:30	09:45	03:15	60	195,00 €
17.04.2025	test13	18:00	19:00	01:00	60	60,00 €
22.04.2025	test14	07:00	10:00	03:00	60	180,00 €
24.04.2025	test15	07:00	09:55	02:55	60	175,00 €
28.04.2025	test16	08:15	10:00	01:45	60	105,00 €
29.04.2025	test17	07:45	10:30	02:45	60	165,00 €
30.04.2025	test18	08:45	11:00	02:15	60	135,00 €
";
        var result = LineImportService.ImportLines(text);

        Assert.IsTrue(result.IsSuccess, "Expected success but got failure: " + result.Message);
        Assert.AreEqual(2_650, result.InvoiceLines.Sum(x => x.LineTotal), "Expected line amount to be 2.650,00 €.");
    }

    [TestMethod]
    public void ImportLines_WithoutHeader_ReturnsSuccess()
    {
        var text = @"02.04.2025	test1	06:00	07:30	01:30	60	90,00 €
02.04.2025	test2	16:30	17:45	01:15	60	75,00 €
03.04.2025	test3	06:00	10:15	04:15	60	255,00 €
08.04.2025	test4	07:20	10:00	02:40	60	160,00 €
08.04.2025	test5	16:45	19:00	02:15	60	135,00 €
09.04.2025	test6	07:00	11:25	04:25	60	265,00 €
10.04.2025	test7	06:00	09:55	03:55	60	235,00 €
14.04.2025	test8	07:30	08:45	01:15	60	75,00 €
15.04.2025	test9	09:00	10:30	01:30	60	90,00 €
15.04.2025	test10	17:30	19:30	02:00	60	120,00 €
16.04.2025	test11	09:00	11:15	02:15	60	135,00 €
17.04.2025	test12	06:30	09:45	03:15	60	195,00 €
17.04.2025	test13	18:00	19:00	01:00	60	60,00 €
22.04.2025	test14	07:00	10:00	03:00	60	180,00 €
24.04.2025	test15	07:00	09:55	02:55	60	175,00 €
28.04.2025	test16	08:15	10:00	01:45	60	105,00 €
29.04.2025	test17	07:45	10:30	02:45	60	165,00 €
30.04.2025	test18	08:45	11:00	02:15	60	135,00 €
";
        var result = LineImportService.ImportLines(text);

        Assert.IsTrue(result.IsSuccess, "Expected success but got failure: " + result.Message);
        Assert.AreEqual(2_650, result.InvoiceLines.Sum(x => x.LineTotal), "Expected line amount to be 2.650,00 €.");
    }

    [TestMethod]
    public void ImportLines_DateFormat_EN_ReturnsSuccess()
    {
        var text = @"05/23/2025	test1	06:00	07:30	01:30	60	90,00 €
";
        var result = LineImportService.ImportLines(text);
        Assert.IsTrue(result.IsSuccess, "Expected success but got failure: " + result.Message);
        Assert.AreEqual(90, result.InvoiceLines[0].LineTotal, "Expected line amount to be 90.00 €.");
    }

    [TestMethod]
    public void ImportLines_DateFormat_SP_FR_ReturnsSuccess()
    {
        var text = @"23/05/2025	test1	06:00	07:30	01:30	60	90,00 €
";
        var result = LineImportService.ImportLines(text);
        Assert.IsTrue(result.IsSuccess, "Expected success but got failure: " + result.Message);
        Assert.AreEqual(90, result.InvoiceLines[0].LineTotal, "Expected line amount to be 90.00 €.");
    }

    [TestMethod]
    public void ImportLines_DateFormat_SampleLine_ReturnsSuccess()
    {
        var text = @"5/23/2025	Project A	08:00	12:00	4:00	50	200.00 €
";
        var result = LineImportService.ImportLines(text);
        Assert.IsTrue(result.IsSuccess, "Expected success but got failure: " + result.Message);
        Assert.AreEqual(200, result.InvoiceLines[0].LineTotal, "Expected line amount to be 200.00 €.");
    }
}
