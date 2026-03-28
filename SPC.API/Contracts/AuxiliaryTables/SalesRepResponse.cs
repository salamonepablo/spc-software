using SPC.Shared.Models;
using System.Linq;

namespace SPC.API.Contracts.AuxiliaryTables;

/// <summary>
/// Response DTO for SalesRep data returned by API.
/// </summary>
public class SalesRepResponse
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? LastName { get; set; }
    public string Name { get; set; } = "";
    public string? CUIL { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? DNI { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? HireDate { get; set; }
    public decimal CommissionPercent { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    public static SalesRepResponse From(SalesRep salesRep)
    {
        var nameParts = new[] { salesRep.FirstName, salesRep.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

        return new SalesRepResponse
        {
            Id = salesRep.Id,
            EmployeeCode = salesRep.EmployeeCode,
            FirstName = salesRep.FirstName,
            LastName = salesRep.LastName,
            Name = string.Join(" ", nameParts),
            CUIL = salesRep.CUIL,
            Address = salesRep.Address,
            City = salesRep.City,
            Province = salesRep.Province,
            PostalCode = salesRep.PostalCode,
            DNI = salesRep.DNI,
            Phone = salesRep.Phone,
            Mobile = salesRep.Mobile,
            Email = salesRep.Email,
            DateOfBirth = salesRep.DateOfBirth,
            HireDate = salesRep.HireDate,
            CommissionPercent = salesRep.CommissionPercent,
            Notes = salesRep.Notes,
            IsActive = salesRep.IsActive
        };
    }
}
