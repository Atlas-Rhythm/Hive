using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hive.Plugins
{
    /// <summary>
    /// An <see cref="Expression"/> representing a <see langword="foreach" /> loop, iterating over some enumerable type.
    /// </summary>
    public class ForeachExpression : Expression
    {
        /// <inheritdoc/>
        public override bool CanReduce => true;
        /// <inheritdoc/>
        public override ExpressionType NodeType => (ExpressionType)1000;
        /// <inheritdoc/>
        public override Type Type => typeof(void);

        /// <summary>
        /// Gets the type of the elements of the enumeration.
        /// </summary>
        public Type EnumerationType { get; }
        /// <summary>
        /// Gets the type of the <see cref="IEnumerator"/> being used for iteration.
        /// </summary>
        public Type EnumeratorType { get; }
        /// <summary>
        /// Gets the method on the enumerable that gets its enumerator.
        /// </summary>
        public MethodInfo GetEnumerator { get; }
        /// <summary>
        /// Gets the property on <see cref="EnumeratorType"/> that gets is current value.
        /// </summary>
        public PropertyInfo Current { get; }
        /// <summary>
        /// Gets the expression representing the value to enumerate.
        /// </summary>
        public Expression Enumerable { get; }
        /// <summary>
        /// Gets the variable that is populated with the value during each iteration.
        /// </summary>
        public ParameterExpression LoopVariable { get; }
        /// <summary>
        /// Gets the expression to be evaluated as the body of the loop.
        /// </summary>
        public Expression Body { get; }
        /// <summary>
        /// Gets the <see cref="LabelTarget"/> that can be used with <see cref="Expression.Break(LabelTarget)"/> to break out of the loop early.
        /// </summary>
        public LabelTarget BreakLabel { get; }

        /// <summary>
        /// Constructs a new <see cref="ForeachExpression"/> without specifying a break label.
        /// </summary>
        /// <param name="enumerable">An expression representing the value to enumerate.</param>
        /// <param name="loopVariable">The variable to populate with the current value in each iteration.</param>
        /// <param name="body">The body of the loop.</param>
        public ForeachExpression(Expression enumerable, ParameterExpression loopVariable, Expression body)
            : this(enumerable, loopVariable, Label(), body)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ForeachExpression"/> specifying a break label.
        /// </summary>
        /// <param name="enumerable">An expression representing the value to enumerate.</param>
        /// <param name="loopVariable">The variable to populate with the current value in each iteration.</param>
        /// <param name="break">The <see cref="LabelTarget"/> to use to break out of the loop.</param>
        /// <param name="body">The body of the loop.</param>
        public ForeachExpression(Expression enumerable, ParameterExpression loopVariable, LabelTarget @break, Expression body)
        {
            Enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
            LoopVariable = loopVariable ?? throw new ArgumentNullException(nameof(loopVariable));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            BreakLabel = @break ?? throw new ArgumentNullException(nameof(@break));

            if (!typeof(IEnumerable).IsAssignableFrom(enumerable.Type))
                throw new ArgumentException("A foreach loop can only take IEnumerable and IEnumerable<T>", nameof(enumerable));

            var getEnum = enumerable.Type.GetMethod(
                nameof(IEnumerable.GetEnumerator), 
                BindingFlags.Public | BindingFlags.Instance, 
                null, 
                Array.Empty<Type>(), 
                Array.Empty<ParameterModifier>()
            );
            if (getEnum == null)
                throw new ArgumentException("Enumerable has no public member GetEnumerator()", nameof(enumerable));

            GetEnumerator = getEnum;

            if (!typeof(IEnumerator).IsAssignableFrom(getEnum.ReturnType))
                throw new ArgumentException("Enumerator for enumerable is not an enumerator", nameof(enumerable));

            EnumeratorType = getEnum.ReturnType;

            var current = EnumeratorType.GetProperty(nameof(IEnumerator.Current), BindingFlags.Public | BindingFlags.Instance);
            if (current == null)
                throw new ArgumentException("Enumerator for enumerable does not have a Current property", nameof(enumerable));

            Current = current;
            EnumerationType = current.PropertyType;

            if (!loopVariable.Type.IsAssignableFrom(EnumerationType))
                throw new ArgumentException($"Loop variable cannot be assigned from enumeration type {EnumerationType}", nameof(loopVariable));
        }

        private static readonly MethodInfo EnumeratorMoveNext = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext), BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo DisposableDispose = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose), BindingFlags.Public | BindingFlags.Instance);

        /// <inheritdoc/>
        public override Expression Reduce()
        {
            var enumerator = Variable(EnumeratorType);

            return Block(
                new[] { enumerator },
                Assign(enumerator, Call(Enumerable, GetEnumerator)),
                TryFinally(
                    Loop(
                        IfThenElse(
                            Call(enumerator, EnumeratorMoveNext),
                            Block(
                                new[] { LoopVariable },
                                Assign(LoopVariable, Property(enumerator, Current)),
                                Body
                            ),
                            Break(BreakLabel)
                        ),
                        BreakLabel
                    ),
                    Call(Convert(enumerator, typeof(IDisposable)), DisposableDispose)
                )
            );
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"Foreach({LoopVariable} in {Enumerable}, {Body})";
    }
}
