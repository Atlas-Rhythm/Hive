using Hive.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive
{
    public class PermissionContext
    {
        public User? User { get; }
        public Mod? Mod { get; }
        public Channel? Channel { get; }
        public GameVersion? GameVersion { get; }
    }
}
