using Hive.Permissions;
using Hive.Utilities;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Hive.Logging
{
    /// <summary>
    /// A proxy logger for permissions logging.
    /// </summary>
    public class PermissionsProxy : Permissions.Logging.ILogger
    {
        private readonly ILogger logger;

        /// <summary>
        /// Construct the proxy from a <see cref="ILogger"/>
        /// </summary>
        /// <param name="log"></param>
        public PermissionsProxy([DisallowNull] ILogger log)
        {
            if (log is null)
                throw new ArgumentNullException(nameof(log));
            logger = log.ForContext<Permissions.Logging.ILogger>();
        }

        /// <inheritdoc/>
        public void Info(string message, object[] messageInfo, string api, StringView action, Rule? currentRule, object manager)
        {
            logger
                .ForContext("Manager", manager)
                .ForContext("Api", api)
                .ForContext("MoreInfo", messageInfo)
                .Information("{Api}: While processing {Rule} for {Action}: {Message}", api, currentRule, action, message);
        }

        /// <inheritdoc/>
        public void Warn(string message, [DisallowNull] object[] messageInfo, string api, StringView action, Rule? currentRule, object manager)
        {
            if (messageInfo is null)
                throw new ArgumentNullException(nameof(messageInfo));
            var log = logger.ForContext("Manager", manager).ForContext("Api", api);

            if (messageInfo.Length > 0 && messageInfo[0] is Exception e)
            {
                log.ForContext("MoreInfo", messageInfo.Skip(1))
                    .Warning(e, "{Api}: While processing {Rule} for {Action}: {Message}", api, currentRule, action, message);
            }
            else
            {
                log.ForContext("MoreInfo", messageInfo)
                    .Warning("{Api}: While processing {Rule} for {Action}: {Message}", api, currentRule, action, message);
            }
        }
    }
}
