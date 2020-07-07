using Hive.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Hive.Permissions
{
    public class PermissionException : Exception
    {
        public StringView Action { get; }
        public Rule? Rule { get; }

        public PermissionException(string message, StringView action, Rule? rule) : base(message)
        {
            Action = action;
            Rule = rule;
        }

        public PermissionException(string message, Exception innerException, StringView action, Rule? rule) : base(message, innerException)
        {
            Action = action;
            Rule = rule;
        }
    }
}
