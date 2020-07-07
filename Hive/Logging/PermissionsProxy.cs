using Hive.Permissions;
using Hive.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Logging
{
    public class PermissionsProxy : Permissions.Logging.ILogger
    {
        private readonly ILogger logger;

        public PermissionsProxy(ILogger log) => logger = log.ForContext<Permissions.Logging.ILogger>();

        public void Info(string message, object[] messageInfo, StringView action, Rule? currentRule, object manager)
        {
            logger
                .ForContext("Manager", manager)
                .ForContext("MoreInfo", messageInfo)
                .Information("While processing {Rule} for {Action}: {Message}", currentRule, action, message);
        }

        public void Warn(string message, object[] messageInfo, StringView action, Rule? currentRule, object manager)
        {
            var log = logger.ForContext("Manager", manager);

            if (messageInfo.Length > 0 && messageInfo[0] is Exception e)
            {
                log.ForContext("MoreInfo", messageInfo.Skip(1))
                    .Warning(e, "While processing {Rule} for {Action}: {Message}", currentRule, action, message);
            }
            else
            {
                log.ForContext("MoreInfo", messageInfo)
                    .Warning("While processing {Rule} for {Action}: {Message}", currentRule, action, message);
            }
        }
    }
}
