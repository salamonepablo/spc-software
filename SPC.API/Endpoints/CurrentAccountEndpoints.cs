using SPC.API.Contracts.CurrentAccount;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Current Account queries (read-only)
/// </summary>
public static class CurrentAccountEndpoints
{
    public static IEndpointRouteBuilder MapCurrentAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/current-accounts")
            .WithTags("Current Account");

        // GET /api/current-accounts/{customerId} - Get account balance
        group.MapGet("/{customerId:int}", async (int customerId, ICurrentAccountService service, ICustomersService customersService) =>
        {
            // Get the account (may be null if no movements yet)
            var account = await service.GetAccountAsync(customerId);

            // Get customer info for response
            var customer = await customersService.GetByIdAsync(customerId);
            if (customer == null)
            {
                return Results.NotFound(new { error = "Cliente no encontrado" });
            }

            var response = new CurrentAccountResponse
            {
                CustomerId = customerId,
                CustomerName = customer.CompanyName,
                CUIT = customer.CUIT,
                Address = customer.Address,
                City = customer.City,
                Province = customer.Province,
                PostalCode = customer.PostalCode,
                BillingBalance = account?.BillingBalance ?? 0,
                BudgetBalance = account?.BudgetBalance ?? 0,
                TotalBalance = account?.TotalBalance ?? 0,
                LastUpdated = account?.LastUpdated ?? DateTime.Now
            };

            return Results.Ok(response);
        })
        .WithName("GetCurrentAccount")
        .WithDescription("Returns current account balance for a customer");

        // GET /api/current-accounts/{customerId}/movements - Get movements with filters and pagination
        group.MapGet("/{customerId:int}/movements", async (
            int customerId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? line,
            int? skip,
            int? take,
            ICurrentAccountService service,
            ICustomersService customersService) =>
        {
            // Validate customer exists
            var customer = await customersService.GetByIdAsync(customerId);
            if (customer == null)
            {
                return Results.NotFound(new { error = "Cliente no encontrado" });
            }

            // Validate line filter
            if (line.HasValue && line != 1 && line != 2)
            {
                return Results.BadRequest(new { error = "El filtro 'line' debe ser 1 (Billing) o 2 (Budget)" });
            }

            // Get filtered movements
            var result = await service.GetMovementsFilteredAsync(
                customerId,
                dateFrom,
                dateTo,
                line,
                skip ?? 0,
                take ?? 50);

            // Map to response
            var response = new CurrentAccountMovementsResponse
            {
                CustomerId = customerId,
                CustomerName = customer.CompanyName,
                BillingBalance = result.BillingBalance,
                BudgetBalance = result.BudgetBalance,
                TotalBalance = result.TotalBalance,
                TotalCount = result.TotalCount,
                Movements = result.Movements.Select(m => new CurrentAccountMovementResponse
                {
                    Id = m.Id,
                    MovementDate = m.MovementDate,
                    DocumentType = GetDocumentTypeName(m.DocumentType),
                    DocumentTypeCode = (int)m.DocumentType,
                    DocumentNumber = m.DocumentNumber,
                    BillingAmount = m.BillingAmount,
                    BudgetAmount = m.BudgetAmount,
                    BillingRunningBalance = m.BillingRunningBalance,
                    BudgetRunningBalance = m.BudgetRunningBalance,
                    Description = m.Description
                }).ToList()
            };

            return Results.Ok(response);
        })
        .WithName("GetCurrentAccountMovements")
        .WithDescription("Returns paginated movements for a customer's current account with optional filters");

        return app;
    }

    /// <summary>
    /// Maps DocumentType enum to friendly Spanish name
    /// </summary>
    private static string GetDocumentTypeName(DocumentType type)
    {
        return type switch
        {
            // Generic types
            DocumentType.Invoice => "Factura",
            DocumentType.CreditNote => "Nota de Crédito",
            DocumentType.DebitNote => "Nota de Débito",
            DocumentType.InternalDebitNote => "Nota de Débito Interna",
            DocumentType.Quote => "Presupuesto",
            DocumentType.Payment => "Pago",
            DocumentType.Receipt => "Recibo",

            // Billing (L1) - Documentos fiscales
            DocumentType.InvoiceA => "Factura A",
            DocumentType.InvoiceB => "Factura B",
            DocumentType.CreditNoteA => "Nota de Crédito A",
            DocumentType.CreditNoteB => "Nota de Crédito B",
            DocumentType.DebitNoteA => "Nota de Débito A",
            DocumentType.DebitNoteB => "Nota de Débito B",
            DocumentType.PaymentBilling => "Pago (Facturación)",
            DocumentType.PaymentVoidBilling => "Anulación Pago (Facturación)",

            // Budget (L2) - Documentos internos
            DocumentType.QuoteVoid => "Anulación Presupuesto",
            DocumentType.PaymentBudget => "Pago (Presupuesto)",
            DocumentType.PaymentVoidBudget => "Anulación Pago (Presupuesto)",
            DocumentType.InternalDebitA => "Débito Interno A",
            DocumentType.InternalDebitB => "Débito Interno B",

            DocumentType.Other => "Otro",
            _ => type.ToString()
        };
    }
}
