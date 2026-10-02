using SPC.API.Contracts.DeliveryNotes;

namespace SPC.API.Services;

public interface IDeliveryNotePdfService
{
    byte[] Create(DeliveryNoteDetailResponse note);
}
