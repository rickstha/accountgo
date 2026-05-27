using Core.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Api.Data
{
    public static class AuditLogHelper
    {
        // Thread-safe collection
        public static readonly List<(DateTime Time, EntityEntry Entry)> AddedEntities
            = new();

        public static List<AuditLog> GetChangesForAuditLog(
            EntityEntry dbEntry,
            string username)
        {
            var result = new List<AuditLog>();

            if (dbEntry == null)
                return result;

            try
            {
                Type entryType = dbEntry.Entity.GetType();

                // Get table attribute safely
                TableAttribute tableAttr =
                    entryType.GetCustomAttributes(typeof(TableAttribute), false)
                        .FirstOrDefault() as TableAttribute;

                if (tableAttr == null && entryType.BaseType != null)
                {
                    tableAttr =
                        entryType.BaseType
                            .GetCustomAttributes(typeof(TableAttribute), false)
                            .FirstOrDefault() as TableAttribute;
                }

                string tableName = tableAttr?.Name ?? entryType.Name;

                if (tableName.Contains("_"))
                {
                    tableName = tableName.Substring(0, tableName.IndexOf("_"));
                }

                bool isSchemaExt = tableAttr?.Schema == "ext";

                AuditableEntity auditableEntity = null;

                if (!isSchemaExt)
                {
                    auditableEntity = FillAuditableEntityAndAttributes(
                        dbEntry,
                        tableName);
                }

                // safer key lookup
                var keyProperty = entryType.GetProperties()
                    .FirstOrDefault(p =>
                        p.GetCustomAttributes(typeof(KeyAttribute), false).Any());

                string keyName = keyProperty?.Name;

                DateTime changeTime = DateTime.UtcNow;

                switch (dbEntry.State)
                {
                    case EntityState.Added:

                        if (isSchemaExt ||
                            IsEntityAuditable(auditableEntity))
                        {
                            string newValues =
                                ObjectFieldsValues(dbEntry);

                            result.Add(CreateAuditLog(
                                username,
                                changeTime,
                                AuditEventTypes.Added,
                                tableName,
                                null,
                                null,
                                null,
                                newValues));

                            lock (AddedEntities)
                            {
                                AddedEntities.Add((changeTime, dbEntry));
                            }
                        }

                        break;

                    case EntityState.Deleted:

                        if (isSchemaExt ||
                            IsEntityAuditable(auditableEntity))
                        {
                            string deletedValues =
                                ObjectFieldsValues(dbEntry);

                            string recordId = keyName != null
                                ? dbEntry.Property(keyName)
                                    ?.CurrentValue
                                    ?.ToString()
                                : null;

                            result.Add(CreateAuditLog(
                                username,
                                changeTime,
                                AuditEventTypes.Deleted,
                                tableName,
                                recordId,
                                null,
                                null,
                                deletedValues));
                        }

                        break;

                    case EntityState.Modified:

                        foreach (var propertyMetadata
                                 in dbEntry.Metadata.GetProperties())
                        {
                            string propertyName =
                                propertyMetadata.Name;

                            if (!isSchemaExt &&
                                !IsAttributeAuditable(
                                    auditableEntity,
                                    tableName,
                                    propertyName))
                            {
                                continue;
                            }

                            var property =
                                dbEntry.Property(propertyName);

                            if (Equals(
                                property.CurrentValue,
                                property.OriginalValue))
                            {
                                continue;
                            }

                            string recordId = keyName != null
                                ? dbEntry.Property(keyName)
                                    ?.CurrentValue
                                    ?.ToString()
                                : null;

                            result.Add(CreateAuditLog(
                                username,
                                changeTime,
                                AuditEventTypes.Modified,
                                tableName,
                                recordId,
                                propertyName,
                                property.OriginalValue?.ToString(),
                                property.CurrentValue?.ToString()));
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                // Replace with proper logger if available
                Console.WriteLine($"Audit error: {ex}");
            }

            return result;
        }

        #region Private Methods

        private static AuditLog CreateAuditLog(
            string username,
            DateTime changeTime,
            AuditEventTypes type,
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
                AuditEventType = (int)type,
                TableName = tableName,
                RecordId = recordId,
                FieldName = fieldName,
                OriginalValue = originalValue,
                NewValue = newValue
            };
        }

        private static bool IsEntityAuditable(
            AuditableEntity auditableEntity)
        {
            return auditableEntity != null;
        }

        private static bool IsAttributeAuditable(
            AuditableEntity auditableEntity,
            string tableName,
            string columnName)
        {
            if (auditableEntity?.AuditableAttributes == null)
                return false;

            return auditableEntity.AuditableAttributes.Any(attr =>
                attr.AttributeName == columnName &&
                attr.AuditableEntity.EntityName == tableName);
        }

        private static string ObjectFieldsValues(EntityEntry dbEntry)
        {
            var values = new List<string>();

            foreach (var propertyMetadata
                     in dbEntry.Metadata.GetProperties())
            {
                string propertyName = propertyMetadata.Name;

                var property = dbEntry.Property(propertyName);

                values.Add(
                    $"{propertyName}:{property.CurrentValue}");
            }

            return string.Join("|", values);
        }

        private static AuditableEntity FillAuditableEntityAndAttributes(
            EntityEntry dbEntry,
            string tableName)
        {
            return ((ApiDbContext)dbEntry.Context)
                .AuditableEntities
                .Include(a => a.AuditableAttributes)
                .FirstOrDefault(e => e.EntityName == tableName);
        }

        #endregion
    }
}