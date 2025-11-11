namespace pax.BBToast;

public record ToastOptions
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ToastType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SmallTitle { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Duration { get; set; } = 5000;
    public string? BiIcon { get; set; }
}
