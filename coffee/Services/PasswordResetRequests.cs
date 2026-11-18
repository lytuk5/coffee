using Microsoft.Extensions.Caching.Memory;

namespace CafeDuaHau.Services;

public sealed class PasswordResetRequests : IDisposable
{
    private readonly MemoryCache requests = new(new MemoryCacheOptions { SizeLimit = 500 });
    private readonly object gate = new();

    public sealed record Request(Guid Id, Guid CustomerId, string UserName, string PhoneNumber, string PasswordHash)
    {
        public bool Approved { get; internal set; }
    }

    public Request? Get(Guid id)
    {
        lock (gate)
            return requests.Get<Request>(id);
    }

    public void Add(Request request)
    {
        lock (gate)
            requests.Set(request.Id, request, new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
    }

    public IReadOnlyList<Request> Pending()
    {
        lock (gate)
            return requests.Keys.OfType<Guid>().Select(id => requests.Get<Request>(id))
                .OfType<Request>().Where(request => !request.Approved).ToList();
    }

    public void Approve(Guid id)
    {
        lock (gate)
        {
            if (requests.Get<Request>(id) is { } request)
                request.Approved = true;
        }
    }

    public void Remove(Guid id)
    {
        lock (gate)
            requests.Remove(id);
    }

    public void Dispose() => requests.Dispose();
}
