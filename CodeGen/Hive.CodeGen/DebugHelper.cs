using System.Diagnostics;

namespace Hive.CodeGen
{
    internal static class DebugHelper
    {
        public static void Attach()
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                // TODO: make a better way to control whether or not this triggers
                //Debugger.Launch();
            }
#endif 
        }
    }
}
