using SPC.API.Contracts.DeliveryNotes;

namespace SPC.API.Services;

public interface IDeliveryNoteCommandService
{
    Task<(DeliveryNoteResponse Note, bool IsReplay)> CreateAsync(CreateDeliveryNoteRequest request, string? idempotencyKey = null);
    Task<DeliveryNoteNextNumberResponse> GetNextNumberAsync(int branchId);
}
