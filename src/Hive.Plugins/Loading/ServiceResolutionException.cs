using System;
using Hive.Plugins.Resources;

namespace Hive.Plugins.Loading
{
    /// <summary>
    /// An exception that is thrown when service resolution fails.
    /// </summary>
    public class ServiceResolutionException : Exception
    {
        /// <summary>
        /// Constructs a new <see cref="ServiceResolutionException"/> with the default message.
        /// </summary>
        public ServiceResolutionException() : base(SR.ServiceResolutionException_DefaultMessage)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ServiceResolutionException"/> with the specified message.
        /// </summary>
        /// <param name="message">The message to construct the exception with.</param>
        public ServiceResolutionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ServiceResolutionException"/> with the specified message and inner exception.
        /// </summary>
        /// <param name="message">The message to construct the exception with.</param>
        /// <param name="innerException">The exception which caused this one.</param>
        public ServiceResolutionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
