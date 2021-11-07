using System;
using Microsoft.Extensions.Options;
using Serilog;

namespace Hive.Configuration
{
    /// <summary>
    /// Provides extension methods for <see cref="IOptions{TOptions}"/>
    /// </summary>
    public static class OptionsExtensions
    {
        /// <summary>
        /// Attempts to load a configuration, logging and throwing if there are validation exceptions.
        /// </summary>
        /// <typeparam name="TValue">The type wrapped by the options</typeparam>
        /// <param name="val">The options instance</param>
        /// <param name="logger">The logger instance to report to</param>
        /// <param name="configDescriptor">The configuration descriptor to report errors about</param>
        /// <returns></returns>
        public static TValue TryLoad<TValue>(this IOptions<TValue> val, ILogger logger, string configDescriptor = "Root") where TValue : class
        {
            if (val is null)
                throw new ArgumentNullException(nameof(val));
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            try
            {
                return val.Value;
            }
            catch (OptionsValidationException ex)
            {
                logger.Error("Invalid {ConfigDescriptor} configuration!", configDescriptor);
                foreach (var f in ex.Failures)
                {
                    logger.Error("{Failure}", f);
                }
                throw;
            }
        }
    }
}
