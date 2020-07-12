using Hive.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Hive.Permissions
{
    /// <summary>
    /// An exception that ocurred while compiling or executing rules in the permission system.
    /// </summary>
    public class PermissionException : Exception
    {
        /// <summary>
        /// Gets the action that was being evaluated when this error ocurred.
        /// </summary>
        public StringView Action { get; }
        /// <summary>
        /// Gets the rule that was being compiled or evaluated when this error ocurred, if any.
        /// </summary>
        public Rule? Rule { get; }

        /// <summary>
        /// Constructs a <see cref="PermissionException"/> with a message, action, and rule.
        /// </summary>
        /// <param name="message">The message associated with this error.</param>
        /// <param name="action">The action being executed when this error ocurred.</param>
        /// <param name="rule">The rule being compiled or executed when this error ocurred.</param>
        public PermissionException(string message, StringView action, Rule? rule) : base(message)
        {
            Action = action;
            Rule = rule;
        }

        /// <summary>
        /// Constructs a <see cref="PermissionException"/> with a message, inner exception, action, and rule.
        /// </summary>
        /// <param name="message">The message associated with this error.</param>
        /// <param name="innerException">The exception that the constructed exception should wrap.</param>
        /// <param name="action">The action being executed when this error ocurred.</param>
        /// <param name="rule">The rule being compiled or executed when this error ocurred.</param>
        public PermissionException(string message, Exception innerException, StringView action, Rule? rule) : base(message, innerException)
        {
            Action = action;
            Rule = rule;
        }
    }
}
