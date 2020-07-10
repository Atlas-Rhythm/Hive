using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Hive.Models
{
    public class Channel
    {
        // this could also act as a primary key
        [Key]
        public string Name { get; set; } = null!;

        // like Mod's
        public JsonElement AdditionalData { get; set; }

        public bool IsPublic { get; set; } = true;

        // TODO: is there anything else that needs to be on any particular channel object?

        public static void Configure(ModelBuilder b)
        {
            b.Entity<Channel>();
        }
    }
}