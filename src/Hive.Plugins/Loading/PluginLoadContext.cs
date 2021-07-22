using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Hive.Plugins.Resources;

namespace Hive.Plugins.Loading
{
    internal class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver resolver;

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

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var path = resolver.ResolveAssemblyToPath(assemblyName);
            if (path is not null)
            {
                return LoadFromAssemblyPath(path);
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
    }
}
