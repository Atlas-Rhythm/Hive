using Hive.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Permissions
{
    public class PermissionContext
    {
        public User? User { get; set; }
        public Mod? Mod { get; set; }
        public Channel? Channel { get; set; }
        public GameVersion? GameVersion { get; set; }
    }
}