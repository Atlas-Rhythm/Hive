using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Hive.Plugins.Resources;

namespace Hive.Plugins.Loading
{
    internal class PluginLoadContext : AssemblyLoadContext, IDisposable
    {
        private readonly AssemblyDependencyResolver resolver;
        private readonly List<PluginLoadContext> depencencyContexts = new();

        public PluginLoadContext(DirectoryInfo directory)
            : base((directory ?? throw new ArgumentNullException(nameof(directory))).Name, false)
        {
            PluginDirectory = directory;
            var dirname = directory.Name;
            var mainfile = directory.EnumerateFiles()
                .FirstOrDefault(f => f.Extension == ".dll" && Path.GetFileNameWithoutExtension(f.Name) == dirname);
            if (mainfile is null)
                throw new ArgumentException(SR.PluginLoad_NoPluginFileInPluginDir.Format($"{dirname}.dll", dirname), nameof(directory));

            MainFile = mainfile;
            resolver = new(mainfile.FullName);
        }

        public DirectoryInfo PluginDirectory { get; }
        public FileInfo MainFile { get; }

        public Assembly LoadPlugin()
            => LoadFromAssemblyPath(MainFile.FullName);

        internal void AddDependencyContext(PluginLoadContext ctx)
            => depencencyContexts.Add(ctx);

        private readonly ThreadLocal<bool> isResolving = new(static () => false);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // if this condition triggers, then we're in a cycle
            // returning immediately will break that cycle
            if (isResolving.Value)
                return null;

            var path = resolver.ResolveAssemblyToPath(assemblyName);
            if (path is not null)
            {
                return LoadFromAssemblyPath(path);
            }

            // if we couldn't find it in *our* ALC, we want to try to find it in our *dependencies'* ALCs, before finally falling back to the default ALC
            try
            {
                isResolving.Value = true;

                foreach (var dep in depencencyContexts)
                {
                    var asm = dep.Load(assemblyName);
                    if (asm is not null)
                        return asm;
                }
            }
            finally
            {
                isResolving.Value = false;
            }

            return null; // cannot load the assembly in this ALC
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (path is not null)
            {
                return LoadUnmanagedDllFromPath(path);
            }

            return IntPtr.Zero; // cannot load the dll in this ALC
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    isResolving.Dispose();
                }

                disposedValue = true;
            }
        }

        // ~PluginLoadContext()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
