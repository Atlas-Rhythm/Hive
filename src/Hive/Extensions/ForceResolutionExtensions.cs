using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hive.Extensions
{
    /// <summary>
    /// Extensions for DbSet
    /// </summary>
    public static class ForceResolutionExtensions
    {
        // This is needed because DbSet implements both IQueryable and IAsyncEnumerable
        // Since we're now pulling in System.Linq.Async to be able to work with IAsyncEnumerable,
        //   DbSet now causes ambiguous resolution issues. By providing a more specific extension,
        //   we can control where those calls go with no usage site code changes.
        /// <summary>
        /// Performs a Where check.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbset"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static IQueryable<T> Where<T>(this DbSet<T> dbset, Expression<Func<T, bool>> expr)
            where T : class
            => ((IQueryable<T>)dbset).Where(expr);

        /// <summary>
        /// Performs a Where check.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbset"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static IQueryable<T> Where<T>(this DbSet<T> dbset, Expression<Func<T, int, bool>> expr)
            where T : class
            => ((IQueryable<T>)dbset).Where(expr);

        /// <summary>
        /// FirstOrDefault but async
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbset"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task<T> FirstOrDefaultAsync<T>(this DbSet<T> dbset, CancellationToken token = default)
            where T : class
            => ((IQueryable<T>)dbset).FirstOrDefaultAsync(token);

        /// <summary>
        /// FirstOrDefault but async
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbset"></param>
        /// <param name="expr"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task<T> FirstOrDefaultAsync<T>(this DbSet<T> dbset, Expression<Func<T, bool>> expr, CancellationToken token = default)
            where T : class
            => ((IQueryable<T>)dbset).FirstOrDefaultAsync(expr, token);
    }
}
