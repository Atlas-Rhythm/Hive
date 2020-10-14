using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hive.Services
{
    /// <summary>
    /// A CDN provider that allows objects to be uploaded, deleted, and queried.
    /// </summary>
    public interface ICdnProvider
    {
        /// <summary>
        /// Uploads an object with a particular name containing the given data.
        /// </summary>
        /// <param name="name">The (not unique) name associated with the object.</param>
        /// <param name="data">The data associated with the object.</param>
        /// <param name="expireAt">The time that the object should expire at, if it should expire at all.</param>
        /// <returns>A <see cref="CdnObject"/> that references the uploaded object.</returns>
        Task<CdnObject> UploadObject(string name, Stream data, Instant? expireAt);

        /// <summary>
        /// Removes the expiration time for the given object, causing it to exist until otherwise removed.
        /// </summary>
        /// <remarks>
        /// This should never fail, even if the object no longer exists.
        /// </remarks>
        /// <param name="link">The object to remove the expiration time for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveExpiry(CdnObject link);

        /// <summary>
        /// Sets an expiry time on the given object, replacing any existing expiration.
        /// </summary>
        /// <remarks>
        /// This may fail if the object no longer exists.
        /// </remarks>
        /// <param name="link">The object to set the expiration time for.</param>
        /// <param name="expireAt">The time that the object will expire at.</param>
        /// <returns>A task reperesenting the asynchronous operation.</returns>
        Task SetExpiry(CdnObject link, Instant expireAt);

        /// <summary>
        /// Attempts to delete an object.
        /// </summary>
        /// <param name="link">The object to delete.</param>
        /// <returns><see langword="true"/> if the object was sucessfully deleted, <see langword="false"/> otherwise.</returns>
        Task<bool> TryDeleteObject(CdnObject link);

        /// <summary>
        /// Gets the actual public URL for an object.
        /// </summary>
        /// <param name="link">The object to query.</param>
        /// <returns>The URL for the object referenced by <paramref name="link"/>.</returns>
        Task<Uri> GetObjectActualUrl(CdnObject link);
        /// <summary>
        /// Gets the name associated with an object.
        /// </summary>
        /// <param name="link">The object to query.</param>
        /// <returns>The name associated with the object referenced by <paramref name="link"/>.</returns>
        Task<string> GetObjectName(CdnObject link);
    }

    /// <summary>
    /// A structure representing a link to an object on a CDN.
    /// </summary>
    /// <remarks>
    /// Two <see cref="CdnObject"/>s that have the same <see cref="UniqueId"/>s, but were created
    /// from different <see cref="ICdnProvider"/>s will compare equal.
    /// </remarks>
    public readonly struct CdnObject : IEquatable<CdnObject>
    {
        /// <summary>
        /// A unique identifier used by an <see cref="ICdnProvider"/> to identify the object 
        /// represented by this link.
        /// </summary>
        /// <remarks>
        /// This may or may not be anything useful to view or modify. Its value is entirely
        /// up to the <see cref="ICdnProvider"/> that produced/consumes it.
        /// </remarks>
        public string UniqueId { get; }

        [JsonConstructor]
        public CdnObject(string uniqueId) => UniqueId = uniqueId;

        public override bool Equals(object? obj)
            => obj is CdnObject link && Equals(link);

        public bool Equals(CdnObject other)
            => UniqueId == other.UniqueId;

        public override int GetHashCode()
            => HashCode.Combine(UniqueId);

        public static bool operator ==(CdnObject left, CdnObject right) => left.Equals(right);

        public static bool operator !=(CdnObject left, CdnObject right) => !(left == right);
    }
}
