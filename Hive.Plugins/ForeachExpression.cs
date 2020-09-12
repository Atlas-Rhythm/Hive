using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hive.Plugins
{
    public class ForeachExpression : Expression
    {
        public override bool CanReduce => true;
        public override ExpressionType NodeType => (ExpressionType)1000;
        public override Type Type => typeof(void);

        public Type EnumerationType { get; }
        public Type EnumeratorType { get; }
        public MethodInfo GetEnumerator { get; }
        public PropertyInfo Current { get; }
        public Expression Enumerable { get; }
        public ParameterExpression LoopVariable { get; }
        public Expression Body { get; }

        public ForeachExpression(Expression enumerable, ParameterExpression loopVariable, Expression body)
        {
            Enumerable = enumerable;
            LoopVariable = loopVariable;
            Body = body;

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

        public override Expression Reduce()
        {
            var enumerator = Variable(EnumeratorType);
            var exitLabel = Label();

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
                            Break(exitLabel)
                        ),
                        exitLabel
                    ),
                    Call(Convert(enumerator, typeof(IDisposable)), DisposableDispose)
                )
            );
        }

        public override string ToString()
            => $"Foreach({LoopVariable} in {Enumerable}, {Body})";
    }
}
