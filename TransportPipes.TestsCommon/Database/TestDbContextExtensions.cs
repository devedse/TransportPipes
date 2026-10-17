using System.Linq.Expressions;
using TransportPipes.Database;

namespace TransportPipes.TestsCommon.Database;

/// <summary>
/// Small helpers so tests can read and write rows directly (like they did with OrmLite's IDbConnection extensions)
/// </summary>
public static class TestDbContextExtensions
{
    public static List<T> Select<T>(this TransportPipesDbContext dbContext, Expression<Func<T, bool>>? predicate = null) where T : class
    {
        IQueryable<T> query = dbContext.Set<T>();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        return query.ToList();
    }

    public static T Single<T>(this TransportPipesDbContext dbContext, Expression<Func<T, bool>> predicate) where T : class
    {
        return dbContext.Set<T>().Single(predicate);
    }

    /// <summary>
    /// Inserts the entity (its generated Id is set afterwards)
    /// </summary>
    public static T Insert<T>(this TransportPipesDbContext dbContext, T entity) where T : class
    {
        dbContext.Set<T>().Add(entity);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();
        return entity;
    }

    public static void InsertAll<T>(this TransportPipesDbContext dbContext, IEnumerable<T> entities) where T : class
    {
        dbContext.Set<T>().AddRange(entities);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();
    }
}
