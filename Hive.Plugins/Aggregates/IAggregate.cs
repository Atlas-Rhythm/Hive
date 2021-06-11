namespace Hive.Plugins.Aggregates
{
    /// <summary>
    /// An interface that provides access to an implementation of <typeparamref name="T"/> that aggregates
    /// the results of some set of other implementations.
    /// </summary>
    /// <typeparam name="T">The interface type to aggregate.</typeparam>
    public interface IAggregate<out T>
        where T : class
    {
        /// <summary>
        /// Gets the aggregated interface implementation.
        /// </summary>
        T Instance { get; }
    }
}
