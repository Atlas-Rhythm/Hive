using Hive.Controllers;
using Hive.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Tests.Endpoints
{
    public class UploadController
    {
        private readonly ITestOutputHelper helper;

        public UploadController(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public void EnsureIUploadPluginValid()
        {
            var emptyPlugin = new HiveDefaultUploadPlugin();

            var aggregate = new Aggregate<IUploadPlugin>(new[] { emptyPlugin });

            _ = aggregate.Instance;
        }

    }
}
