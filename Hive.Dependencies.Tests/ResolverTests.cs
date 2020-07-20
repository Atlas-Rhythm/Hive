using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hive.Dependencies.Tests
{
    public class ResolverTests
    {
        private class Accessor : IValueAccessor<object, object, object, object>
        {
            public FSharpValueOption<object> And(object a, object b)
            {
                return FSharpValueOption<object>.Some(a);
            }

            public int Compare(object a, object b)
            {
                return 0;
            }

            public IEnumerable<object> Conflicts(object mod_)
            {
                return Enumerable.Empty<object>();
            }

            public object CreateRef(string id, object range)
            {
                return range;
            }

            public IEnumerable<object> Dependencies(object mod_)
            {
                return Enumerable.Empty<object>();
            }

            public object Either(object a, object b)
            {
                return a;
            }

            public string ID(object mod_)
            {
                return "";
            }

            public bool Matches(object range, object version)
            {
                return false;
            }

            public Task<IEnumerable<object>> ModsMatching(object @ref)
            {
                return Task.FromResult(Enumerable.Empty<object>());
            }

            public object Not(object a)
            {
                return a;
            }

            public object Range(object @ref)
            {
                return @ref;
            }

            public object Version(object mod_)
            {
                return mod_;
            }
        }

        [Fact]
        public async Task TestResolver()
        {
            var accessor = new Accessor();

            var res = await Resolver.Resolve(accessor, Enumerable.Empty<object>());

            _ = res;
        }
    }
}
