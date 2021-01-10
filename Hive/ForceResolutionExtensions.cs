using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hive
{
    public static class ForceResolutionExtensions
    {
        // This is needed because DbSet implements both IQueryable and IAsyncEnumerable
        // Since we're now pulling in System.Linq.Async to be able to work with IAsyncEnumerable,
        //   DbSet now causes ambiguous resolution issues. By providing a more specific extension,
        //   we can control where those calls go with no usage site code changes.
        public static IQueryable<T> Where<T>(this DbSet<T> dbset, Expression<Func<T, bool>> expr)
            where T : class
            => ((IQueryable<T>)dbset).Where(expr);
        public static IQueryable<T> Where<T>(this DbSet<T> dbset, Expression<Func<T, int, bool>> expr)
            where T : class
            => ((IQueryable<T>)dbset).Where(expr);

        public static Task<T> FirstOrDefaultAsync<T>(this DbSet<T> dbset, CancellationToken token = default)
            where T : class
            => ((IQueryable<T>)dbset).FirstOrDefaultAsync(token);
        public static Task<T> FirstOrDefaultAsync<T>(this DbSet<T> dbset, Expression<Func<T, bool>> expr, CancellationToken token = default)
            where T : class
            => ((IQueryable<T>)dbset).FirstOrDefaultAsync(expr, token);
    }
}
