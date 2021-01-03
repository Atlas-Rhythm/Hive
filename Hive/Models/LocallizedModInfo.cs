using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Models
{
    /// <summary>
    /// Represents a collection of localized information for a <see cref="Mod"/>.
    /// </summary>
    public class LocalizedModInfo
    {
        /// <summary>
        /// The language of this localized information
        /// </summary>
        public string Language { get; set; } = null!;

        /// <summary>
        /// The localized name of the mod
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The localized description of the mod
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// The localized changelog of the mod, if it exists
        /// </summary>
        public string? Changelog { get; set; }

        /// <summary>
        /// The localized credits of the mod, if they exist
        /// </summary>
        public string? Credits { get; set; }

        private Mod? owningMod;

        /// <summary>
        /// The <see cref="Mod"/> that this information describes
        /// </summary>
        [BackingField(nameof(owningMod))]
        [DisallowNull]
        [NotNull]
        public Mod OwningMod
        {
            get => owningMod ?? throw new InvalidOperationException();
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                _ = owningMod?.Localizations.Remove(this);
                owningMod = value;
                if (!owningMod.Localizations.Contains(this))
                    owningMod.Localizations.Add(this);
            }
        }

        #region DB Schema stuff

        /// <summary>
        /// The Guid of this LocalizedModInfo, used as a primary key
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We use Guid as a name. Could be changed in the future.")]
        public Guid Guid { get; set; }

        #endregion DB Schema stuff
    }
}
