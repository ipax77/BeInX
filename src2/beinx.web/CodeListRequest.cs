using Microsoft.AspNetCore.Components.Forms;

namespace beinx.web;

public class CodeListRequest
{
    public object Target { get; init; } = null!;
    public string PropertyName { get; init; } = string.Empty;
    public string CodeList { get; init; } = string.Empty;
    public EditContext EditContext { get; init; } = null!;
}