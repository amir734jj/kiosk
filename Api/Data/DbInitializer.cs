using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Data;

/// <summary>
/// Migration-free schema sync. <c>EnsureCreated</c> builds the schema for a brand
/// new database, but it never adds tables introduced after the database was first
/// created. This creates any tables defined in the model that are not yet present
/// (e.g. after adding a new entity such as <c>KioskAgents</c>) without touching
/// existing tables or data.
/// </summary>
public static class DbInitializer
{
    public static void EnsureSchema(AppDbContext db)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();

        // Brand-new database: let EF build the full schema in one shot.
        if (!creator.Exists())
        {
            db.Database.EnsureCreated();
            return;
        }

        var existingTables = db.Database
            .SqlQueryRaw<string>(
                "SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'")
            .ToList()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var model = db.Model;
        var differ = db.GetService<IMigrationsModelDiffer>();
        var sqlGenerator = db.GetService<IMigrationsSqlGenerator>();

        // Diff an empty database against the current model to get the create
        // operations for every table, then keep only the missing ones.
        var operations = differ.GetDifferences(null, model.GetRelationalModel());

        var missingTables = operations
            .OfType<CreateTableOperation>()
            .Select(op => op.Name)
            .Where(name => !existingTables.Contains(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (missingTables.Count == 0)
        {
            return;
        }

        var wanted = operations.Where(op => op switch
        {
            CreateTableOperation ct => missingTables.Contains(ct.Name),
            CreateIndexOperation ci => missingTables.Contains(ci.Table),
            _ => false
        }).ToList();

        foreach (var command in sqlGenerator.Generate(wanted, model))
        {
            db.Database.ExecuteSqlRaw(command.CommandText);
        }
    }
}
