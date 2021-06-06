﻿// <auto-generated />
using System;
using System.Collections.Generic;
using System.Text.Json;
using Hive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Hive.Migrations
{
    [DbContext(typeof(HiveContext))]
    partial class ModsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.2");

            modelBuilder.Entity("GameVersionMod", b =>
                {
                    b.Property<Guid>("SupportedModsId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("SupportedVersionsGuid")
                        .HasColumnType("uuid");

                    b.HasKey("SupportedModsId", "SupportedVersionsGuid");

                    b.HasIndex("SupportedVersionsGuid");

                    b.ToTable("GameVersionMod");
                });

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

                    b.Property<Guid>("OwningModId")
                        .HasColumnType("uuid");

                    b.HasKey("Guid");

                    b.HasIndex("OwningModId");

                    b.ToTable("ModLocalizations");
                });

            modelBuilder.Entity("Hive.Models.Mod", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<JsonElement>("AdditionalData")
                        .HasColumnType("jsonb");

                    b.Property<string[]>("Authors")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<string>("ChannelName")
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

                    b.Property<IList<ValueTuple<string, Uri>>>("Links")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("ReadableID")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant>("UploadedAt")
                        .HasColumnType("timestamp");

                    b.Property<string>("Uploader")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ChannelName");

                    b.HasIndex("ReadableID", "Version")
                        .IsUnique();

                    b.ToTable("Mods");
                });

            modelBuilder.Entity("Hive.Models.User", b =>
                {
                    b.Property<string>("AlternativeId")
                        .HasColumnType("text");

                    b.Property<Dictionary<string, JsonElement>>("AdditionalData")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("AlternativeId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("GameVersionMod", b =>
                {
                    b.HasOne("Hive.Models.Mod", null)
                        .WithMany()
                        .HasForeignKey("SupportedModsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Hive.Models.GameVersion", null)
                        .WithMany()
                        .HasForeignKey("SupportedVersionsGuid")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Hive.Models.LocalizedModInfo", b =>
                {
                    b.HasOne("Hive.Models.Mod", "OwningMod")
                        .WithMany("Localizations")
                        .HasForeignKey("OwningModId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("OwningMod");
                });

            modelBuilder.Entity("Hive.Models.Mod", b =>
                {
                    b.HasOne("Hive.Models.Channel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelName");

                    b.Navigation("Channel");
                });

            modelBuilder.Entity("Hive.Models.Mod", b =>
                {
                    b.Navigation("Localizations");
                });
#pragma warning restore 612, 618
        }
    }
}
