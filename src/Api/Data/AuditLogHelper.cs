using Core.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Api.Data
{
    public static class AuditLogHelper
    {
        public static readonly ConcurrentQueue<(DateTime Time, string EntityName, string RecordId)> AddedEntities
            = new();

        public static List<AuditLog> GetChangesForAuditLog(EntityEntry dbEntry, string username)
        {
            var result = new List<AuditLog>();

            if (dbEntry == null)
                return result;

            try
            {
                var entityType = dbEntry.Entity.GetType();

                var tableAttribute =
                    entityType.GetCustomAttributes(typeof(TableAttribute), true)
                    .OfType<TableAttribute>()
                    .FirstOrDefault()
                    ??
                    entityType.BaseType?
                        .GetCustomAttributes(typeof(TableAttribute), true)
                        .OfType<TableAttribute>()
                        .FirstOrDefault();

                string tableName = tableAttribute?.Name ?? entityType.Name;

                if (tableName.Contains("_"))
                    tableName = tableName[..tableName.IndexOf("_")];

                bool isSchemaExt = tableAttribute?.Schema == "ext";

                AuditableEntity auditableEntity = null;

                if (!isSchemaExt)
                    auditableEntity = FillAuditableEntityAndAttributes(dbEntry, tableName);

                var primaryKey = dbEntry.Metadata.FindPrimaryKey();

                string recordId = null;

                if (primaryKey != null)
                {
                    var keyProperty = primaryKey.Properties.FirstOrDefault();

                    if (keyProperty != null)
                    {
                        recordId = dbEntry.Property(keyProperty.Name)
                                          .CurrentValue?
                                          .ToString();
                    }
                }

                DateTime changeTime = DateTime.UtcNow;

                switch (dbEntry.State)
                {
                    case EntityState.Added:

                        if (isSchemaExt || IsEntityAuditable(auditableEntity))
                        {
                            result.Add(CreateAuditLog(
                                username,
                                changeTime,
                                AuditEventTypes.Added,
                                tableName,
                                recordId,
                                null,
                                null,
                                ObjectFieldsValues(dbEntry)));

                            AddedEntities.Enqueue((changeTime, tableName, recordId));
                        }

                        break;

                    case EntityState.Deleted:

                        if (isSchemaExt || IsEntityAuditable(auditableEntity))
                        {
                            if (primaryKey != null)
                            {
                                var key = primaryKey.Properties.First();
                                recordId = dbEntry.Property(key.Name).OriginalValue?.ToString();
                            }

                            result.Add(CreateAuditLog(
                                username,
                                changeTime,
                                AuditEventTypes.Deleted,
                                tableName,
                                recordId,
                                null,
                                null,
                                ObjectFieldsValues(dbEntry)));
                        }

                        break;

                    case EntityState.Modified:

                        foreach (var property in dbEntry.Properties)
                        {
                            if (!property.IsModified)
                                continue;

                            if (!isSchemaExt &&
                                !IsAttributeAuditable(
                                    auditableEntity,
                                    tableName,
                                    property.Metadata.Name))
                            {
                                continue;
                            }

                            result.Add(CreateAuditLog(
                                username,
                                changeTime,
                                AuditEventTypes.Modified,
                                tableName,
                                recordId,
                                property.Metadata.Name,
                                property.OriginalValue?.ToString(),
                                property.CurrentValue?.ToString()));
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        private static AuditLog CreateAuditLog(
            string username,
            DateTime changeTime,
            AuditEventTypes eventType,
            string tableName,
            string recordId,
            string fieldName,
            string originalValue,
            string newValue)
        {
            return new AuditLog
            {
                UserName = username,
                AuditEventDateUTC = changeTime,
                AuditEventType = (int)eventType,
                TableName = tableName,
                RecordId = recordId,
                FieldName = fieldName,
                OriginalValue = originalValue,
                NewValue = newValue
            };
        }

        private static bool IsEntityAuditable(AuditableEntity entity)
        {
            return entity != null;
        }

        private static bool IsAttributeAuditable(
            AuditableEntity entity,
            string tableName,
            string columnName)
        {
            return entity?.AuditableAttributes?.Any(a =>
                a.AttributeName == columnName &&
                a.AuditableEntity.EntityName == tableName) == true;
        }

        private static string ObjectFieldsValues(EntityEntry entry)
        {
            return string.Join("|",
                entry.Properties.Select(p =>
                    $"{p.Metadata.Name}:{p.CurrentValue}"));
        }

        private static AuditableEntity FillAuditableEntityAndAttributes(
            EntityEntry dbEntry,
            string tableName)
        {
            if (dbEntry.Context is not ApiDbContext context)
                return null;

            return context.AuditableEntities
                .Include(a => a.AuditableAttributes)
                .FirstOrDefault(a => a.EntityName == tableName);
        }
    }
}