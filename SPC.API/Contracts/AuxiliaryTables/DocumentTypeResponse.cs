using SPC.Shared.Models;

namespace SPC.API.Contracts.AuxiliaryTables;

/// <summary>
/// Response DTO for DocumentTypeMaster data returned by API.
/// </summary>
public class DocumentTypeResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string ShortCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string LabelEs { get; set; } = "";
    public string LabelEn { get; set; } = "";
    public int BalanceImpact { get; set; }
    public bool IsBillingLine { get; set; }
    public bool IsActive { get; set; }

    public static DocumentTypeResponse From(DocumentTypeMaster documentType)
    {
        return new DocumentTypeResponse
        {
            Id = documentType.Id,
            Code = documentType.Code,
            ShortCode = documentType.ShortCode,
            Description = documentType.Description,
            LabelEs = documentType.LabelEs,
            LabelEn = documentType.LabelEn,
            BalanceImpact = documentType.BalanceImpact,
            IsBillingLine = documentType.IsBillingLine,
            IsActive = documentType.IsActive
        };
    }
}
