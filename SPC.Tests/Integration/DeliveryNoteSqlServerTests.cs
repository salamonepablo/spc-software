using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SPC.API.Contracts.DeliveryNotes;
using SPC.API.Data;
using SPC.API.Services;
using Xunit.Abstractions;

namespace SPC.Tests.Integration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DeliveryNoteSqlServerCollection
{
    public const string Name = "Delivery note SQL Server";
}

[Collection(DeliveryNoteSqlServerCollection.Name)]
public sealed class DeliveryNoteSqlServerTests(ITestOutputHelper output)
{
    private const string ConnectionVariable = "SPC_REMITO_TEST_CONNECTION";
    private const string ExpectedServer = "09b5ac392dbd";
    private const string ExpectedDatabase = "SPC TEST";
    private static readonly DateTime TestDate = new(2026, 10, 2);

    [SqlServerFact]
    public async Task ReplayAndChangedPayloadPreserveSingleStockAndSequenceEffect()
    {
        await WithGuardedDatabase(async (connection, options) =>
        {
            var ids = await ReadExistingIds(connection);
            var key = NewKey();
            var before = await ReadState(options, ids, key);
            var request = NewRequest(ids, key, 1, true);
            DeliveryNoteResponse created;
            await using (var db = new SPCDbContext(options))
                created = (await new DeliveryNoteCommandService(db).CreateAsync(request, key)).Note;
            var afterCreate = await ReadState(options, ids, key);
            Assert.Equal(before.Stock - 1m, afterCreate.Stock);
            Assert.Equal(before.Sequence + 1, afterCreate.Sequence);
            Assert.Equal(before.KeyNoteCount + 1, afterCreate.KeyNoteCount);
            Assert.Equal(1, afterCreate.KeyDetailCount);
            Log($"create branch={ids.BranchId} id={created.Id} number={created.DeliveryNoteNumber} stock {before.Stock}->{afterCreate.Stock} sequence {before.Sequence}->{afterCreate.Sequence}");

            await using (var db = new SPCDbContext(options))
            {
                var replay = await new DeliveryNoteCommandService(db).CreateAsync(NewRequest(ids, key, 1, true), key);
                Assert.True(replay.IsReplay);
                Assert.Equal(created.Id, replay.Note.Id);
            }
            var afterReplay = await ReadState(options, ids, key);
            Assert.Equal(afterCreate, afterReplay);
            Log($"replay id={created.Id}; state unchanged stock={afterReplay.Stock}, sequence={afterReplay.Sequence}");

            await using (var db = new SPCDbContext(options))
                await Assert.ThrowsAsync<DeliveryNoteIdempotencyConflictException>(() =>
                    new DeliveryNoteCommandService(db).CreateAsync(NewRequest(ids, key, 2, true), key));
            var afterConflict = await ReadState(options, ids, key);
            Assert.Equal(afterReplay, afterConflict);
            Log("changed-payload conflict had no effects");
        });
    }

    [SqlServerFact]
    public async Task ExistingManualNumberWithNewKeyHasNoEffects()
    {
        await WithGuardedDatabase(async (connection, options) =>
        {
            var ids = await ReadExistingIds(connection);
            var key = NewKey();
            var existingNumber = await Scalar<long>(connection,
                "SELECT TOP (1) DeliveryNoteNumber FROM dbo.DeliveryNotes WHERE BranchId=@branch ORDER BY Id",
                ("@branch", ids.BranchId));
            var before = await ReadState(options, ids, key);
            var request = NewRequest(ids, key, 1, true);
            request.RequestedDeliveryNoteNumber = existingNumber;
            await using (var db = new SPCDbContext(options))
                await Assert.ThrowsAsync<DeliveryNoteNumberConflictException>(() =>
                    new DeliveryNoteCommandService(db).CreateAsync(request, key));
            var after = await ReadState(options, ids, key);
            Assert.Equal(before, after);
            Log($"duplicate manual number branch={ids.BranchId} number={existingNumber}; stock={after.Stock}, sequence={after.Sequence}, key headers/details={after.KeyNoteCount}/{after.KeyDetailCount}");
        });
    }

    [SqlServerFact]
    public async Task FailureAfterSaveWritesExactNoteAndStockInsideTransactionThenRollsBack()
    {
        await WithGuardedDatabase(async (connection, options) =>
        {
            var ids = await ReadExistingIds(connection);
            var key = NewKey();
            var before = await ReadState(options, ids, key);
            var interceptor = new ThrowAfterWriteInterceptor(key, ids.ProductId, ids.BranchId, before.Stock, before.Sequence);
            var interceptedOptions = new DbContextOptionsBuilder<SPCDbContext>(options)
                .AddInterceptors(interceptor).Options;
            await using (var db = new SPCDbContext(interceptedOptions))
                await Assert.ThrowsAsync<InjectedAfterSaveException>(() =>
                    new DeliveryNoteCommandService(db).CreateAsync(NewRequest(ids, key, 1, true), key));

            Assert.True(interceptor.SawExpectedWrite);
            var after = await ReadState(options, ids, key);
            Assert.Equal(0, after.KeyNoteCount);
            Assert.Equal(0, after.KeyDetailCount);
            Assert.Equal(before.Stock, after.Stock);
            Assert.Equal(before.Sequence, after.Sequence);
            Log($"rollback branch={ids.BranchId} transient id={interceptor.HeaderId} number={interceptor.Number}; inside transaction header/details=1/{interceptor.DetailCount}, stock {before.Stock}->{interceptor.InTransactionStock}, sequence {before.Sequence}->{interceptor.InTransactionSequence}; external key counts={after.KeyNoteCount}/{after.KeyDetailCount}, stock={after.Stock}, sequence={after.Sequence}");
        });
    }

    [SqlServerFact]
    public async Task ConcurrentAutomaticNumbersPersistUniqueNotesAndAccountForExpectedFailures()
    {
        await WithGuardedDatabase(async (connection, options) =>
        {
            var ids = await ReadExistingIds(connection);
            const int attempts = 4;
            var keys = Enumerable.Range(0, attempts).Select(_ => NewKey()).ToArray();
            var before = await ReadState(options, ids, NewKey());
            var tasks = keys.Select(key => CreateConcurrent(options, ids, key)).ToArray();
            var results = await Task.WhenAll(tasks);
            var successes = results.Where(r => r.Note != null).ToArray();
            Assert.NotEmpty(successes);
            Assert.Equal(successes.Length, successes.Select(r => r.Note!.DeliveryNoteNumber).Distinct().Count());
            var after = await ReadState(options, ids, keys[0]);
            Assert.Equal(before.Sequence + successes.Length, after.Sequence);
            foreach (var result in results.Where(r => r.Note == null))
            {
                Assert.True(result.ExpectedFailure);
                var failedState = await ReadState(options, ids, result.Key);
                Assert.Equal(0, failedState.KeyNoteCount);
                Assert.Equal(0, failedState.KeyDetailCount);
            }
            foreach (var result in successes)
            {
                var saved = await ReadState(options, ids, result.Key);
                Assert.Equal(1, saved.KeyNoteCount);
                Assert.Equal(1, saved.KeyDetailCount);
                Log($"concurrent success branch={ids.BranchId} id={result.Note!.Id} number={result.Note.DeliveryNoteNumber} key={result.Key}");
            }
            Log($"concurrency successes={successes.Length}/{attempts}; sequence {before.Sequence}->{after.Sequence}; failures have zero exact-key headers/details");
        });
    }

    [SqlServerFact]
    public async Task CreatingInOneBranchDoesNotAdvanceAnotherBranchSequence()
    {
        await WithGuardedDatabase(async (connection, options) =>
        {
            var first = await ReadExistingIds(connection);
            var secondBranchId = await Scalar<int>(connection,
                "SELECT TOP (1) Id FROM dbo.Branches WHERE IsActive=1 AND Id<>@branch ORDER BY Id", ("@branch", first.BranchId));
            var second = first with { BranchId = secondBranchId };
            var keyA = NewKey(); var keyB = NewKey();
            var beforeA = await ReadState(options, first, keyA);
            var beforeB = await ReadState(options, second, keyB);
            DeliveryNoteResponse noteA; DeliveryNoteResponse noteB;
            await using (var db = new SPCDbContext(options))
                noteA = (await new DeliveryNoteCommandService(db).CreateAsync(NewRequest(first, keyA, 1, false), keyA)).Note;
            var middleA = await ReadState(options, first, keyA);
            var middleB = await ReadState(options, second, keyB);
            Assert.Equal(beforeA.Sequence + 1, middleA.Sequence);
            Assert.Equal(beforeB.Sequence, middleB.Sequence);
            await using (var db = new SPCDbContext(options))
                noteB = (await new DeliveryNoteCommandService(db).CreateAsync(NewRequest(second, keyB, 1, false), keyB)).Note;
            var afterA = await ReadState(options, first, keyA);
            var afterB = await ReadState(options, second, keyB);
            Assert.Equal(middleA.Sequence, afterA.Sequence);
            Assert.Equal(beforeB.Sequence + 1, afterB.Sequence);
            Assert.Equal(first.BranchId, noteA.BranchId); Assert.Equal(second.BranchId, noteB.BranchId);
            Assert.NotEqual(noteA.DeliveryNoteNumber, noteB.DeliveryNoteNumber);
            Log($"branch isolation: {first.BranchId} id={noteA.Id} number={noteA.DeliveryNoteNumber} sequence {beforeA.Sequence}->{afterA.Sequence}; {second.BranchId} id={noteB.Id} number={noteB.DeliveryNoteNumber} sequence {beforeB.Sequence}->{afterB.Sequence}");
        });
    }

    private static async Task<ConcurrentResult> CreateConcurrent(DbContextOptions<SPCDbContext> options,
        (int BranchId, int CustomerId, int ProductId) ids, string key)
    {
        try
        {
            await using var db = new SPCDbContext(options);
            var result = await new DeliveryNoteCommandService(db).CreateAsync(NewRequest(ids, key, 1, false), key);
            return new(key, result.Note, false);
        }
        catch (Exception exception) when (IsExpectedConcurrencyFailure(exception))
        {
            return new(key, null, true);
        }
    }

    private static bool IsExpectedConcurrencyFailure(Exception exception)
    {
        if (exception is DeliveryNoteNumberConflictException) return true;
        if (exception is DbUpdateException { InnerException: SqlException sql } && sql.Number is 1205 or 2601 or 2627) return true;
        return exception is SqlException direct && direct.Number is 1205 or 2601 or 2627;
    }

    private static async Task WithGuardedDatabase(Func<SqlConnection, DbContextOptions<SPCDbContext>, Task> test)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"SQL tests are marked skipped when {ConnectionVariable} is absent.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, ExpectedDatabase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Refusing SQL integration test: {ConnectionVariable} Initial Catalog must be exactly {ExpectedDatabase}.");
        if (!IsLoopback(builder.DataSource))
            throw new InvalidOperationException($"Refusing SQL integration test: {ConnectionVariable} DataSource must be localhost or 127.0.0.1 on port 1433.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var identity = await Scalar<string>(connection, "SELECT CONVERT(nvarchar(128), @@SERVERNAME) + N'|' + DB_NAME()");
        if (!string.Equals(identity, $"{ExpectedServer}|{ExpectedDatabase}", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing SQL integration test: connected SQL server/database identity did not match the required SPC TEST target.");
        var options = new DbContextOptionsBuilder<SPCDbContext>().UseSqlServer(connectionString).Options;
        await test(connection, options);
    }

    public sealed class SqlServerFactAttribute : FactAttribute
    {
        public SqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
                Skip = $"No SQL executed: {ConnectionVariable} is absent. Opt in only with the dedicated local SPC TEST connection.";
        }
    }

    private static bool IsLoopback(string dataSource)
    {
        var source = dataSource.Trim();
        return source.Equals("localhost,1433", StringComparison.OrdinalIgnoreCase)
            || source.Equals("127.0.0.1,1433", StringComparison.OrdinalIgnoreCase)
            || source.Equals("tcp:localhost,1433", StringComparison.OrdinalIgnoreCase)
            || source.Equals("tcp:127.0.0.1,1433", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(int BranchId, int CustomerId, int ProductId)> ReadExistingIds(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1) b.Id, c.Id FROM dbo.Branches b CROSS JOIN dbo.Customers c
            WHERE b.IsActive=1 AND c.IsActive=1 ORDER BY b.Id, c.Id;
            SELECT TOP (1) p.Id FROM dbo.Products p WHERE p.Code=N'110' AND p.IsActive=1;
            SELECT TOP (1) w.Id FROM dbo.Warehouses w WHERE w.Id=1 AND w.IsActive=1 AND w.AssociatedSalesRepId=3;
            SELECT TOP (1) s.Id FROM dbo.SalesReps s WHERE s.Id=3 AND s.IsActive=1;
            SELECT TOP (1) st.Id FROM dbo.Stocks st JOIN dbo.Products p ON p.Id=st.ProductId
                WHERE p.Code=N'110' AND st.WarehouseId=1;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new InvalidOperationException("Required active branch/customer fixture was not found.");
        var branch = reader.GetInt32(0); var customer = reader.GetInt32(1);
        await reader.NextResultAsync(); if (!await reader.ReadAsync()) throw new InvalidOperationException("Required active product code 110 was not found.");
        var product = reader.GetInt32(0);
        await reader.NextResultAsync(); if (!await reader.ReadAsync()) throw new InvalidOperationException("Required active warehouse 1 for salesperson 3 was not found.");
        await reader.NextResultAsync(); if (!await reader.ReadAsync()) throw new InvalidOperationException("Required active salesperson 3 was not found.");
        await reader.NextResultAsync(); if (!await reader.ReadAsync()) throw new InvalidOperationException("Required stock row for product code 110 / warehouse 1 was not found.");
        return (branch, customer, product);
    }

    private static async Task<TestState> ReadState(DbContextOptions<SPCDbContext> options,
        (int BranchId, int CustomerId, int ProductId) ids, string key)
    {
        await using var db = new SPCDbContext(options);
        var stock = await db.Stocks.Where(s => s.ProductId == ids.ProductId && s.WarehouseId == 1).Select(s => s.Quantity).SingleAsync();
        var sequence = await db.BranchDeliveryNoteSequences.Where(s => s.BranchId == ids.BranchId)
            .Select(s => (long?)s.NextDeliveryNoteNumber).SingleOrDefaultAsync() ?? 1;
        var keyedNotes = db.DeliveryNotes.Where(n => n.IdempotencyKey == key);
        var noteCount = await keyedNotes.CountAsync();
        var noteIds = keyedNotes.Select(n => n.Id);
        var detailCount = await db.DeliveryNoteDetails.CountAsync(d => noteIds.Contains(d.DeliveryNoteId));
        return new(stock, sequence, noteCount, detailCount);
    }

    private static CreateDeliveryNoteRequest NewRequest((int BranchId, int CustomerId, int ProductId) ids,
        string key, decimal quantity, bool adjustStock) => new()
    {
        BranchId = ids.BranchId, CustomerId = ids.CustomerId, SalesRepId = 3, DeliveryNoteDate = TestDate,
        AdjustStock = adjustStock, IdempotencyKey = key,
        Details = [new CreateDeliveryNoteDetailRequest { ProductId = ids.ProductId, Quantity = quantity }]
    };

    private static string NewKey() => $"sql-remito-{Guid.NewGuid():N}";
    private void Log(string evidence) => output.WriteLine(evidence);

    private static async Task<T> Scalar<T>(SqlConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand(); command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        var scalar = await command.ExecuteScalarAsync();
        if (scalar is null or DBNull) throw new InvalidOperationException("Expected a SQL scalar result.");
        return (T)Convert.ChangeType(scalar, typeof(T));
    }

    private sealed class ThrowAfterWriteInterceptor(string key, int productId, int branchId, decimal beforeStock, long beforeSequence) : SaveChangesInterceptor
    {
        public bool SawExpectedWrite { get; private set; }
        public int HeaderId { get; private set; }
        public long Number { get; private set; }
        public int DetailCount { get; private set; }
        public decimal InTransactionStock { get; private set; }
        public long InTransactionSequence { get; private set; }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            var db = (SPCDbContext)eventData.Context!;
            var note = await db.DeliveryNotes.SingleAsync(n => n.IdempotencyKey == key, cancellationToken);
            HeaderId = note.Id; Number = note.DeliveryNoteNumber;
            DetailCount = await db.DeliveryNoteDetails.CountAsync(d => d.DeliveryNoteId == HeaderId, cancellationToken);
            InTransactionStock = await db.Stocks.Where(s => s.ProductId == productId && s.WarehouseId == 1).Select(s => s.Quantity).SingleAsync(cancellationToken);
            InTransactionSequence = await db.BranchDeliveryNoteSequences.Where(s => s.BranchId == branchId)
                .Select(s => (long?)s.NextDeliveryNoteNumber).SingleOrDefaultAsync(cancellationToken) ?? 1;
            SawExpectedWrite = HeaderId > 0 && DetailCount == 1 && InTransactionStock == beforeStock - 1m && InTransactionSequence == beforeSequence + 1;
            if (!SawExpectedWrite) throw new InvalidOperationException("Exact-key header/detail and expected stock/sequence writes were not visible in the transaction.");
            throw new InjectedAfterSaveException();
        }
    }

    private sealed record TestState(decimal Stock, long Sequence, int KeyNoteCount, int KeyDetailCount);
    private sealed record ConcurrentResult(string Key, SPC.API.Contracts.DeliveryNotes.DeliveryNoteResponse? Note, bool ExpectedFailure);
    private sealed class InjectedAfterSaveException : Exception;
}
