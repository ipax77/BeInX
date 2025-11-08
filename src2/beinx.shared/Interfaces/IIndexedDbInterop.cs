namespace beinx.shared.Interfaces;

public interface IIndexedDbInterop
{
    Task<T> CallAsync<T>(string method, params object[] args);
    Task CallVoidAsync(string method, params object[] args);
    void Dispose();
    Task<List<Draft<object>>> GetAllDrafts();
}