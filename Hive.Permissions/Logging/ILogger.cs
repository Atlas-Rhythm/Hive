using Hive.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Permissions.Logging
{
    /// <summary>
    /// An interface that provides logging for a <see cref="PermissionsManager{TContext}"/>.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message for this log.</param>
        /// <param name="messageInfo">A list of objects that are associated with the message.</param>
        /// <param name="api">The name of the API this error ocurred while executing.</param>
        /// <param name="action">The action that is being evaluated when the message is logged.</param>
        /// <param name="currentRule">The <see cref="Rule"/> that is being evaluated when the message is logged, if any.</param>
        /// <param name="manager">The <see cref="PermissionsManager{TContext}"/> instance.</param>
        void Info(string message, object[] messageInfo, string api, StringView action, Rule? currentRule, object manager);
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message for this log.</param>
        /// <param name="messageInfo">A list of objects that are associated with the message.</param>
        /// <param name="api">The name of the API this error ocurred while executing.</param>
        /// <param name="action">The action that is being evaluated when the message is logged.</param>
        /// <param name="currentRule">The <see cref="Rule"/> that is being evaluated when the message is logged, if any.</param>
        /// <param name="manager">The <see cref="PermissionsManager{TContext}"/> instance.</param>
        void Warn(string message, object[] messageInfo, string api, StringView action, Rule? currentRule, object manager);
    }
}
