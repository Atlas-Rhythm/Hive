using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace Hive.Plugins.Loading
{
    internal class ContainerConfigurator
    {
        private readonly List<Action<HostBuilderContext, object>> actions = new();

        public void Run(HostBuilderContext hostContext, object container)
        {
            foreach (var action in actions)
            {
                action(hostContext, container);
            }
        }

        public void ConfigureContainer<TContainer>(Action<HostBuilderContext, TContainer> action)
            => actions.Add((hc, c) => action(hc, (TContainer)c));
        public void ConfigureContainerNoCtx<TContainer>(Action<TContainer> action)
            => ConfigureContainer<TContainer>((_, c) => action(c));
    }
}
