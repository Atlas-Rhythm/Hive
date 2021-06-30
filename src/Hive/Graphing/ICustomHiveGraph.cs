using GraphQL.Types;

namespace Hive.Graphing
{
    /// <summary>
    /// An interface which provides structure for adding extra configuration to graph types.
    /// </summary>
    public interface ICustomHiveGraph<T> where T : IObjectGraphType
    {
        /// <summary>
        /// Called when a graph type is installing extra graph configurations.
        /// </summary>
        /// <param name="graphType">The graph type to add to.</param>
        void Configure(T graphType);
    }
}
