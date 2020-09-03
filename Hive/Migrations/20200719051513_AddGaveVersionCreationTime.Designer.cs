﻿// <auto-generated />
using System;
using System.Collections.Generic;
using System.Text.Json;
using Hive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Hive.Migrations
{
    [DbContext(typeof(ModsContext))]
    [Migration("20200719051513_AddGaveVersionCreationTime")]
    partial class AddGaveVersionCreationTime
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "5.0.0-preview.6.20312.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Hive.Models.Channel", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<JsonElement>("AdditionalData")
                        .HasColumnType("jsonb");

                    b.HasKey("Name");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("Hive.Models.GameVersion", b =>
                {
                    b.Property<Guid>("Guid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<JsonElement>("AdditionalData")
                        .HasColumnType("jsonb");

                    b.Property<Instant>("CreationTime")
                        .HasColumnType("timestamp");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Guid");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("GameVersions");
                });

            modelBuilder.Entity("Hive.Models.GameVersion_Mod_Joiner", b =>
                {
                    b.Property<Guid>("ModGuid")
                        .HasColumnType("uuid");

                    b.Property<Guid>("VersionGuid")
                        .HasColumnType("uuid");

                    b.HasIndex("ModGuid");

                    b.HasIndex("VersionGuid");

                    b.ToTable("GameVersion_Mod_Joiner");
                });

            modelBuilder.Entity("Hive.Models.LocalizedModInfo", b =>
                {
                    b.Property<Guid>("Guid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Changelog")
                        .HasColumnType("text");

                    b.Property<string>("Credits")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("OwningModGuid")
                        .HasColumnType("uuid");

                    b.HasKey("Guid");

                    b.HasIndex("OwningModGuid");

                    b.ToTable("ModLocalizations");
                });

            modelBuilder.Entity("Hive.Models.Mod", b =>
                {
                    b.Property<Guid>("Guid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<JsonElement>("AdditionalData")
                        .HasColumnType("jsonb");

                    b.Property<string[]>("Authors")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<string>("ChannelName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<IList<ModReference>>("Conflicts")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string[]>("Contributors")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<IList<ModReference>>("Dependencies")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("DownloadLink")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant?>("EditedAt")
                        .HasColumnType("timestamp");

                    b.Property<string>("ID")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<IList<ValueTuple<string, Uri>>>("Links")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Instant>("UploadedAt")
                        .HasColumnType("timestamp");

                    b.Property<string>("Uploader")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Guid");

                    b.HasIndex("ChannelName");

                    b.HasIndex("ID", "Version")
                        .IsUnique();

                    b.ToTable("Mods");
                });

            modelBuilder.Entity("Hive.Models.GameVersion_Mod_Joiner", b =>
                {
                    b.HasOne("Hive.Models.Mod", "Mod")
                        .WithMany()
                        .HasForeignKey("ModGuid")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Hive.Models.GameVersion", "Version")
                        .WithMany()
                        .HasForeignKey("VersionGuid")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Hive.Models.LocalizedModInfo", b =>
                {
                    b.HasOne("Hive.Models.Mod", "OwningMod")
                        .WithMany("Localizations")
                        .HasForeignKey("OwningModGuid")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Hive.Models.Mod", b =>
                {
                    b.HasOne("Hive.Models.Channel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
