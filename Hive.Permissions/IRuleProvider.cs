using Hive.Utilities;
using MathExpr.Syntax;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hive.Permissions
{
    /// <summary>
    /// An interface that can be implemented to provide <see cref="Rule"/>s to a <see cref="PermissionsManager{TContext}"/>.
    /// </summary>
    public interface IRuleProvider
    {
        /// <summary>
        /// Tries to get a rule with a given name.
        /// </summary>
        /// <remarks>
        /// <para>This method should be idempotent (that is, it should not matter how many times it is called) and should provide
        /// the same <see cref="Rule"/> object given the same <paramref name="name"/>. If the rule has changed, then a new object
        /// is expected.</para>
        /// </remarks>
        /// <param name="name">The name of the rule to get.</param>
        /// <param name="gotten">The rule, if any.</param>
        /// <returns><see langword="true"/> if the rule was found, <see langword="false"/> otherwise.</returns>
        // this should be idempotent, and always provide the same Rule object for the same parameter (unless it has changed)
        // (technically, that last part is not a hard requirement, however it is needed to prevent rules from being recompiled repeatedly
        bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten);

        /// <summary>
        /// Checks whether or not a rule with the given name has changed since the provided time.
        /// </summary>
        /// <remarks>
        /// <para>This should return <see langword="true"/> if a rule was added with that name, and similarly if it was removed since
        /// the specified time.</para>
        /// </remarks>
        /// <param name="name">The name of the rule to check.</param>
        /// <param name="time">The last time that the rule was checked.</param>
        /// <returns><see langword="true"/> if the rule has changed, <see langword="false"/> otherwise.</returns>
        // both of these operations should be idempotent. they should return *true* if this is the first time it is asked for a present perm,
        //   which means that *time* will be Instant.MinValue
        bool HasRuleChangedSince(StringView name, Instant time);

        /// <summary>
        /// Checks whether or not a rule has changed since the provided time.
        /// </summary>
        /// <remarks>
        /// <para>This should return <see langword="true"/> if a rule was added with that name, and similarly if it was removed since
        /// the specified time.</para>
        /// <para>This overload is provided because implementers may choose to store additional data in each <see cref="Rule"/> object,
        /// either using <see cref="Rule{TCustom}"/> or a custom derived type. As the exact object provided by <see cref="TryGetRule(StringView, out Rule)"/>
        /// will be passed to this method, this is a valid method of keeping track of something like the underlying database model.</para>
        /// </remarks>
        /// <param name="rule">The rule to check.</param>
        /// <param name="time">The last time that the rule was checked.</param>
        /// <returns><see langword="true"/> if the rule has changed, <see langword="false"/> otherwise.</returns>
        // this overload is for convenience, as the Rule may contain a reference to something useful for the provider
        bool HasRuleChangedSince(Rule rule, Instant time);

        /// <summary>
        /// Gets the current time, according to the provider.
        /// </summary>
        /// <remarks>
        /// <para>This is the source of all <see cref="DateTime"/> objects given to methods on this instance. It should be synced to 
        /// whatever time source you store to keep track of changes.</para>
        /// </remarks>
        // this should pull from a time source that is synced with the underlying rule store
        Instant CurrentTime => SystemClock.Instance.GetCurrentInstant();
    }

    /// <summary>
    /// An interface that, in addition to what <see cref="IRuleProvider"/> provides, allows an arbitrary transformation
    /// to be applied to the rule expressions before being compiled.
    /// </summary>
    [CLSCompliant(false)]
    public interface IPreCompileRuleProvider : IRuleProvider
    {
        /// <summary>
        /// Applies a transformation to the parsed expression before compiling.
        /// </summary>
        /// <remarks>
        /// <para>This is called right before any rule is compiled.</para>
        /// <para>This may be called simultaneously on multiple threads.</para>
        /// </remarks>
        /// <param name="expression">The parsed rule expression.</param>
        /// <returns>The transformed rule expression that will actually be compiled.</returns>
        MathExpression PreCompileTransform(MathExpression expression);
    }
}
