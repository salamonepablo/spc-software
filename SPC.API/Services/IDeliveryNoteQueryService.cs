using SPC.API.Contracts.DeliveryNotes;

namespace SPC.API.Services;

public interface IDeliveryNoteQueryService
{
    Task<DeliveryNoteSearchResult> SearchAsync(DeliveryNoteSearchRequest request, CancellationToken cancellationToken = default);
    Task<DeliveryNoteDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
