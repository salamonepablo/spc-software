using SPC.API.Contracts.CurrentAccount;
using SPC.API.Services;
using SPC.API.Services.CurrentAccount;
using SPC.Shared.Models;
using System.Globalization;

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
            IDocumentTypeResolver documentTypeResolver,
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
            var movementResponses = new List<CurrentAccountMovementResponse>(result.Movements.Count);
            foreach (var movement in result.Movements)
            {
                var resolvedType = await documentTypeResolver.ResolveAsync(movement);
                movementResponses.Add(new CurrentAccountMovementResponse
                {
                    Id = movement.Id,
                    MovementDate = movement.MovementDate,
                    DocumentType = resolvedType.Label ?? GetDocumentTypeName(movement.DocumentType),
                    DocumentTypeCode = resolvedType.LegacyCode,
                    DocumentTypeShortCode = resolvedType.ShortCode,
                    DocumentTypeLabel = resolvedType.Label,
                    DocumentTypeTooltip = resolvedType.Tooltip,
                    DocumentNumber = movement.DocumentNumber,
                    BillingAmount = movement.BillingAmount,
                    BudgetAmount = movement.BudgetAmount,
                    BillingRunningBalance = movement.BillingRunningBalance,
                    BudgetRunningBalance = movement.BudgetRunningBalance,
                    TotalRunningBalance = movement.BillingRunningBalance + movement.BudgetRunningBalance,
                    Description = movement.Description,
                    Navigation = BuildNavigationMetadata(movement, resolvedType.ShortCode)
                });
            }

            var response = new CurrentAccountMovementsResponse
            {
                CustomerId = customerId,
                CustomerName = customer.CompanyName,
                BillingBalance = result.BillingBalance,
                BudgetBalance = result.BudgetBalance,
                TotalBalance = result.TotalBalance,
                InitialBillingBalance = result.InitialBillingBalance,
                InitialBudgetBalance = result.InitialBudgetBalance,
                InitialTotalBalance = result.InitialTotalBalance,
                FinalBillingBalance = result.FinalBillingBalance,
                FinalBudgetBalance = result.FinalBudgetBalance,
                FinalTotalBalance = result.FinalTotalBalance,
                TotalCount = result.TotalCount,
                GuardrailApplied = result.GuardrailApplied,
                GuardrailMode = result.GuardrailMode,
                WarningCode = result.WarningCode,
                WarningMessage = result.WarningMessage,
                ReturnedCount = result.ReturnedCount,
                RangeDays = result.RangeDays,
                Movements = movementResponses
            };

            return Results.Ok(response);
        })
        .WithName("GetCurrentAccountMovements")
        .WithDescription("Returns paginated movements for a customer's current account with optional filters");

        group.MapGet("/{customerId:int}/movements/range", async (
            int customerId,
            DateTime dateFrom,
            DateTime dateTo,
            int? line,
            ICurrentAccountService service,
            IDocumentTypeResolver documentTypeResolver,
            ICustomersService customersService) =>
        {
            var customer = await customersService.GetByIdAsync(customerId);
            if (customer == null)
            {
                return Results.NotFound(new { error = "Cliente no encontrado" });
            }

            if (line.HasValue && line != 1 && line != 2)
            {
                return Results.BadRequest(new { error = "El filtro 'line' debe ser 1 (Billing) o 2 (Budget)" });
            }

            var result = await service.GetMovementsByRangeAsync(customerId, dateFrom, dateTo, line);
            if (string.Equals(result.GuardrailMode, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new
                {
                    code = result.WarningCode,
                    message = result.WarningMessage,
                    guardrailMode = result.GuardrailMode,
                    rangeDays = result.RangeDays
                });
            }

            var movementResponses = new List<CurrentAccountMovementResponse>(result.Movements.Count);
            foreach (var movement in result.Movements)
            {
                var resolvedType = await documentTypeResolver.ResolveAsync(movement);
                movementResponses.Add(new CurrentAccountMovementResponse
                {
                    Id = movement.Id,
                    MovementDate = movement.MovementDate,
                    DocumentType = resolvedType.Label ?? GetDocumentTypeName(movement.DocumentType),
                    DocumentTypeCode = resolvedType.LegacyCode,
                    DocumentTypeShortCode = resolvedType.ShortCode,
                    DocumentTypeLabel = resolvedType.Label,
                    DocumentTypeTooltip = resolvedType.Tooltip,
                    DocumentNumber = movement.DocumentNumber,
                    BillingAmount = movement.BillingAmount,
                    BudgetAmount = movement.BudgetAmount,
                    BillingRunningBalance = movement.BillingRunningBalance,
                    BudgetRunningBalance = movement.BudgetRunningBalance,
                    TotalRunningBalance = movement.BillingRunningBalance + movement.BudgetRunningBalance,
                    Description = movement.Description,
                    Navigation = BuildNavigationMetadata(movement, resolvedType.ShortCode)
                });
            }

            var response = new CurrentAccountMovementsResponse
            {
                CustomerId = customerId,
                CustomerName = customer.CompanyName,
                BillingBalance = result.BillingBalance,
                BudgetBalance = result.BudgetBalance,
                TotalBalance = result.TotalBalance,
                InitialBillingBalance = result.InitialBillingBalance,
                InitialBudgetBalance = result.InitialBudgetBalance,
                InitialTotalBalance = result.InitialTotalBalance,
                FinalBillingBalance = result.FinalBillingBalance,
                FinalBudgetBalance = result.FinalBudgetBalance,
                FinalTotalBalance = result.FinalTotalBalance,
                TotalCount = result.TotalCount,
                GuardrailApplied = result.GuardrailApplied,
                GuardrailMode = result.GuardrailMode,
                WarningCode = result.WarningCode,
                WarningMessage = result.WarningMessage,
                ReturnedCount = result.ReturnedCount,
                RangeDays = result.RangeDays,
                Movements = movementResponses
            };

            return Results.Ok(response);
        })
        .WithName("GetCurrentAccountMovementsRange")
        .WithDescription("Returns full-range movements for explicit date search with guardrail metadata");

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

            DocumentType.Other => "Otros",
            _ => type.ToString()
        };
    }

    private static CurrentAccountNavigationMetadataResponse BuildNavigationMetadata(
        CurrentAccountMovement movement,
        string? resolvedDocumentTypeShortCode)
    {
        var documentNumber = movement.DocumentNumber.ToString(CultureInfo.InvariantCulture);
        var encodedNumber = Uri.EscapeDataString(documentNumber);
        var shortCode = resolvedDocumentTypeShortCode?.Trim().ToUpperInvariant();

        return shortCode switch
        {
            "FA" or "FB" => new CurrentAccountNavigationMetadataResponse
            {
                TargetType = "document",
                TargetKind = "invoice",
                TargetRoute = $"/invoices?search={encodedNumber}",
                TargetId = documentNumber,
                CanOpen = true
            },

            "PR" => new CurrentAccountNavigationMetadataResponse
            {
                TargetType = "document",
                TargetKind = "quote",
                TargetRoute = $"/quotes?search={encodedNumber}",
                TargetId = documentNumber,
                CanOpen = true
            },

            "NCA" or "NCB" => new CurrentAccountNavigationMetadataResponse
            {
                TargetType = "document",
                TargetKind = "credit-note",
                TargetRoute = $"/credit-notes/{encodedNumber}?customerId={movement.CustomerId}",
                TargetId = documentNumber,
                CanOpen = true
            },

            "NDA" or "NDB" => new CurrentAccountNavigationMetadataResponse
            {
                TargetType = "document",
                TargetKind = "debit-note",
                TargetRoute = $"/debit-notes/{encodedNumber}?customerId={movement.CustomerId}",
                TargetId = documentNumber,
                CanOpen = true
            },

            "PG" => new CurrentAccountNavigationMetadataResponse
            {
                TargetType = "payment",
                TargetKind = "payment",
                TargetRoute = $"/payments/{encodedNumber}?customerId={movement.CustomerId}",
                TargetId = documentNumber,
                CanOpen = true
            },

            "SI" => new CurrentAccountNavigationMetadataResponse
            {
                TargetType = "initial-balance",
                TargetKind = "initial-balance",
                TargetId = documentNumber,
                CanOpen = false,
                DisabledReason = "Saldo inicial sin detalle navegable"
            },

            _ => new CurrentAccountNavigationMetadataResponse
            {
                TargetType = "other",
                TargetKind = "other",
                TargetId = documentNumber,
                CanOpen = false,
                DisabledReason = "No hay vista de detalle para este movimiento"
            }
        };
    }
}
