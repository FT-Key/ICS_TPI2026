using Dsw2025Tpi.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Dsw2025Tpi.Tests.Infrastructure;

/// <summary>
/// Base de datos SQLite en memoria compartida entre contextos, con contadores de
/// comandos SQL y de invocaciones a SaveChanges para verificar el comportamiento batch.
/// </summary>
internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqlCommandCounter _commandCounter = new();

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<Dsw2025TpiContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_commandCounter)
            .Options;

        using var context = new Dsw2025TpiContext(Options);
        context.Database.EnsureCreated();
    }

    public DbContextOptions<Dsw2025TpiContext> Options { get; }

    public IReadOnlyList<string> Commands => _commandCounter.Commands;

    public CountingContext CreateContext() => new(Options);

    public void ResetCounters() => _commandCounter.Reset();

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

internal sealed class CountingContext : Dsw2025TpiContext
{
    public CountingContext(DbContextOptions<Dsw2025TpiContext> options) : base(options)
    {
    }

    public int SaveChangesCalls { get; private set; }

    public override int SaveChanges()
    {
        SaveChangesCalls++;
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return base.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class SqlCommandCounter : DbCommandInterceptor
{
    private readonly List<string> _commands = [];

    public IReadOnlyList<string> Commands => _commands;

    public void Reset() => _commands.Clear();

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        _commands.Add(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _commands.Add(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
