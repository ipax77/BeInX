namespace BlazorInvoice.Shared;

public class InvoiceListRequest
{
    public string? Filter { get; set; }
    public List<TableOrder> TableOrders { get; set; } = [];
    public bool Unpaid { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 1000;
}

public class TableOrder
{
    public string PropertyName { get; set; } = string.Empty;
    public bool Ascending { get; set; }
}