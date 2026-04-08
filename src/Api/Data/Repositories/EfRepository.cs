using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Data;
using Core.Domain;

namespace Api.Data
{
    public class EfRepository<T>(ILogger<EfRepository<T>> logger, ApiDbContext context) : IRepository<T> where T : BaseEntity
    {
        private readonly ApiDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<EfRepository<T>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private DbSet<T> _entities;

        #region Properties


        public virtual IQueryable<T> Table => Entities;

        public virtual IQueryable<T> TableNoTracking => Entities.AsNoTracking();

        protected virtual DbSet<T> Entities
        {
            get
            {
                return _entities ??= _context.Set<T>();
            }
        }

        public virtual T GetById(object id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            var key = Convert.ToInt32(id);
            return Entities.FirstOrDefault(x => x.Id == key);
        }

        public virtual void Insert(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            try
            {
                Entities.Add(entity);
                _context.SaveChanges();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error inserting entity of type {EntityType}", typeof(T));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error inserting entity of type {EntityType}", typeof(T));
                throw;
            }
        }

        public virtual void Insert(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            try
            {
                foreach (var entity in entities)
                    Entities.Add(entity);

                _context.SaveChanges();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error inserting entities of type {EntityType}", typeof(T));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error inserting entities of type {EntityType}", typeof(T));
                throw;
            }
        }

        public virtual void Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            try
            {
                _context.Update(entity);
                _context.SaveChanges();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error updating entity of type {EntityType}", typeof(T));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating entity of type {EntityType}", typeof(T));
                throw;
            }
        }

        public virtual void Delete(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            try
            {
                Entities.Remove(entity);
                _context.SaveChanges();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error deleting entity of type {EntityType}", typeof(T));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting entity of type {EntityType}", typeof(T));
                throw;
            }
            catch (ArgumentUpdate argEx)
            {
                _logger.LogError(argEx, "Unexpected error deleting entity of type {EntityType}", typeof(T));
                throw;
            }
        }


        public virtual void Delete(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            try
            {
                foreach (var entity in entities)
                    Entities.Remove(entity);

                _context.SaveChanges();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error deleting entities of type {EntityType}", typeof(T));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting entities of type {EntityType}", typeof(T));
                throw;
            }
        }

        public IQueryable<T> GetAllIncluding(params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = Entities;
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }

            return query;
        }

        #endregion
    }
}
