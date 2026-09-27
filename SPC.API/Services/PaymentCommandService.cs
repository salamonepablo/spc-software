using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SPC.API.Contracts.Payments;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

public class PaymentCommandService : IPaymentCommandService
{
    private readonly SPCDbContext _db;
    private readonly IPaymentQueryService _queryService;
    private readonly ICurrentAccountService _currentAccountService;

    public PaymentCommandService(
        SPCDbContext db,
        IPaymentQueryService queryService,
        ICurrentAccountService currentAccountService)
    {
        _db = db;
        _queryService = queryService;
        _currentAccountService = currentAccountService;
    }

    public async Task<PaymentCreationResult> CreateAsync(CreatePaymentRequest request, string idempotencyKey)
    {
        ValidateRequest(request, idempotencyKey);
        var fingerprint = CreateFingerprint(request);

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var existing = await _db.Payments
            .Where(payment => payment.BranchId == request.BranchId && payment.IdempotencyKey == idempotencyKey)
            .Select(payment => new { payment.Id, payment.RequestFingerprint })
            .SingleOrDefaultAsync();

        if (existing != null)
        {
            if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                throw new PaymentIdempotencyConflictException();

            return new PaymentCreationResult(await GetCreatedPaymentAsync(existing.Id), true);
        }

        var customerExists = await _db.Customers.AnyAsync(customer => customer.Id == request.CustomerId && customer.IsActive);
        if (!customerExists)
            throw new InvalidOperationException($"Customer {request.CustomerId} no encontrado o inactivo");

        var branchExists = await _db.Branches.AnyAsync(branch => branch.Id == request.BranchId && branch.IsActive);
        if (!branchExists)
            throw new InvalidOperationException($"Sucursal {request.BranchId} no encontrada o inactiva");

        var appliesTo = ParseAppliesTo(request.AppliesTo);
        if (appliesTo == AccountLineType.Budget && !_currentAccountService.IsDualLineEnabled())
            throw new InvalidOperationException("La Línea 2 requiere la capacidad DualLineCurrentAccount habilitada");

        var methodIds = request.Details.Select(detail => detail.PaymentMethodId).Distinct().ToList();
        var validMethodCount = await _db.PaymentMethods
            .CountAsync(method => methodIds.Contains(method.Id) && method.IsActive);
        if (validMethodCount != methodIds.Count)
            throw new InvalidOperationException("Una o más formas de pago no son válidas o están inactivas");

        var total = request.Details.Sum(detail => detail.Amount);
        var payment = new Payment
        {
            BranchId = request.BranchId,
            PaymentNumber = await AllocatePaymentNumberAsync(request.BranchId),
            PaymentDate = request.PaymentDate.Date,
            CustomerId = request.CustomerId,
            TotalAmount = total,
            AppliesTo = appliesTo,
            AppliesToDescription = NormalizeOptional(request.Notes),
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = fingerprint,
            Details = request.Details.Select((detail, index) => new PaymentDetail
            {
                LineNumber = index + 1,
                PaymentMethodId = detail.PaymentMethodId,
                Amount = detail.Amount,
                Notes = NormalizeOptional(detail.Notes)
            }).ToList()
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        await _currentAccountService.RecordMovementAsync(
            request.CustomerId,
            DocumentType.Payment,
            payment.PaymentNumber,
            appliesTo == AccountLineType.Billing ? -total : 0,
            appliesTo == AccountLineType.Budget ? -total : 0,
            $"Pago {payment.PaymentNumber}",
            payment.PaymentDate);

        if (transaction != null)
            await transaction.CommitAsync();
        return new PaymentCreationResult(await GetCreatedPaymentAsync(payment.Id), false);
    }

    public async Task<PaymentDetailResponse> VoidAsync(int paymentId, string? reason, string idempotencyKey)
        {
            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
                : null;

            var payment = await _db.Payments
                .Include(item => item.Details)
                .SingleOrDefaultAsync(item => item.Id == paymentId)
                ?? throw new KeyNotFoundException("Pago no encontrado");

            if (payment.IsVoided)
                return await GetCreatedPaymentAsync(payment.Id);

            payment.IsVoided = true;
            payment.AppliesToDescription = AppendVoidReason(payment.AppliesToDescription, reason);

            await _currentAccountService.RecordMovementAsync(
                payment.CustomerId,
                payment.AppliesTo == AccountLineType.Billing ? DocumentType.PaymentVoidBilling : DocumentType.PaymentVoidBudget,
                payment.PaymentNumber,
                payment.AppliesTo == AccountLineType.Billing ? payment.TotalAmount : 0,
                payment.AppliesTo == AccountLineType.Budget ? payment.TotalAmount : 0,
                $"Anulación Pago {payment.PaymentNumber}",
                payment.PaymentDate);

            await _db.SaveChangesAsync();
            if (transaction != null)
                await transaction.CommitAsync();

            return await GetCreatedPaymentAsync(payment.Id);
        }

        private async Task<long> AllocatePaymentNumberAsync(int branchId)
    {
        if (!_db.Database.IsRelational())
        {
            var sequence = await _db.BranchPaymentSequences.SingleOrDefaultAsync(item => item.BranchId == branchId);
            if (sequence == null)
            {
                sequence = new BranchPaymentSequence { BranchId = branchId, NextPaymentNumber = 1 };
                _db.BranchPaymentSequences.Add(sequence);
            }

            var number = sequence.NextPaymentNumber;
            sequence.NextPaymentNumber++;
            return number;
        }

        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            IF NOT EXISTS (SELECT 1 FROM dbo.BranchPaymentSequences WITH (UPDLOCK, HOLDLOCK) WHERE BranchId = {branchId})
                INSERT INTO dbo.BranchPaymentSequences (BranchId, NextPaymentNumber) VALUES ({branchId}, 1)
            """);

        await using var command = _db.Database.GetDbConnection().CreateCommand();
        command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            UPDATE dbo.BranchPaymentSequences
            SET NextPaymentNumber = NextPaymentNumber + 1
            OUTPUT INSERTED.NextPaymentNumber - 1
            WHERE BranchId = @branchId
            """;
        var branchIdParameter = command.CreateParameter();
        branchIdParameter.ParameterName = "@branchId";
        branchIdParameter.Value = branchId;
        command.Parameters.Add(branchIdParameter);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private async Task<PaymentDetailResponse> GetCreatedPaymentAsync(int paymentId)
    {
        return await _queryService.GetByIdAsync(paymentId)
            ?? throw new InvalidOperationException("No se pudo recuperar el pago creado");
    }

    private static void ValidateRequest(CreatePaymentRequest request, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
            throw new InvalidOperationException("Idempotency-Key es requerido y no puede superar 128 caracteres");
        if (request.Details.Count == 0)
            throw new InvalidOperationException("El pago debe incluir al menos una forma de pago");
        if (request.Details.Any(detail => detail.Amount <= 0))
            throw new InvalidOperationException("Todos los importes deben ser positivos");
        if (request.Details.Sum(detail => detail.Amount) <= 0)
            throw new InvalidOperationException("El total del pago debe ser positivo");
        if (request.PaymentDate == default)
            throw new InvalidOperationException("La fecha de pago es requerida");
    }

    private static AccountLineType ParseAppliesTo(string appliesTo)
    {
        if (string.Equals(appliesTo, "Billing", StringComparison.OrdinalIgnoreCase))
            return AccountLineType.Billing;
        if (string.Equals(appliesTo, "Budget", StringComparison.OrdinalIgnoreCase))
            return AccountLineType.Budget;

        throw new InvalidOperationException("AppliesTo debe ser Billing o Budget");
    }

    private static string CreateFingerprint(CreatePaymentRequest request)
    {
        var details = request.Details
            .Select(detail => $"{detail.PaymentMethodId}|{detail.Amount.ToString("0.00", CultureInfo.InvariantCulture)}|{NormalizeOptional(detail.Notes) ?? ""}")
            .OrderBy(detail => detail, StringComparer.Ordinal);
        var canonical = string.Join("\n", new[]
        {
            request.CustomerId.ToString(CultureInfo.InvariantCulture),
            request.BranchId.ToString(CultureInfo.InvariantCulture),
            request.PaymentDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            request.AppliesTo.Trim().ToUpperInvariant(),
            NormalizeOptional(request.Notes) ?? "",
            string.Join("\n", details)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? AppendVoidReason(string? description, string? reason)
        {
            var normalizedReason = NormalizeOptional(reason);
            if (normalizedReason == null)
                return description;

            var value = string.IsNullOrWhiteSpace(description)
                ? $"Anulación: {normalizedReason}"
                : $"{description} | Anulación: {normalizedReason}";
            return value.Length <= 200 ? value : value[..200];
        }
}

public sealed class PaymentIdempotencyConflictException : InvalidOperationException
{
    public PaymentIdempotencyConflictException() : base("Idempotency-Key ya fue usado con una solicitud diferente")
    {
    }
}
