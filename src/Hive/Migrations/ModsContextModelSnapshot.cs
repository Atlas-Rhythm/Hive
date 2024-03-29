﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Hive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hive.Migrations
{
    [DbContext(typeof(HiveContext))]
    partial class ModsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

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

                    b.Property<ArbitraryAdditionalData>("AdditionalData")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Name");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("Hive.Models.GameVersion", b =>
                {
                    b.Property<Guid>("Guid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<ArbitraryAdditionalData>("AdditionalData")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Instant>("CreationTime")
                        .HasColumnType("timestamp with time zone");

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

                    b.Property<ArbitraryAdditionalData>("AdditionalData")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("ChannelName")
                        .HasColumnType("text");

                    b.Property<IList<ModReference>>("Conflicts")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<IList<ModReference>>("Dependencies")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("DownloadLink")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant?>("EditedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<IList<ValueTuple<string, Uri>>>("Links")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("ReadableID")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant>("UploadedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("UploaderAlternativeId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ChannelName");

                    b.HasIndex("UploaderAlternativeId");

                    b.HasIndex("ReadableID", "Version")
                        .IsUnique();

                    b.ToTable("Mods");
                });

            modelBuilder.Entity("Hive.Models.User", b =>
                {
                    b.Property<string>("AlternativeId")
                        .HasColumnType("text");

                    b.Property<ArbitraryAdditionalData>("AdditionalData")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("AlternativeId");

                    b.HasIndex("AlternativeId");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ModUser", b =>
                {
                    b.Property<Guid>("AuthoredId")
                        .HasColumnType("uuid");

                    b.Property<string>("AuthorsAlternativeId")
                        .HasColumnType("text");

                    b.HasKey("AuthoredId", "AuthorsAlternativeId");

                    b.HasIndex("AuthorsAlternativeId");

                    b.ToTable("ModUser");
                });

            modelBuilder.Entity("ModUser1", b =>
                {
                    b.Property<Guid>("ContributedToId")
                        .HasColumnType("uuid");

                    b.Property<string>("ContributorsAlternativeId")
                        .HasColumnType("text");

                    b.HasKey("ContributedToId", "ContributorsAlternativeId");

                    b.HasIndex("ContributorsAlternativeId");

                    b.ToTable("ModUser1");
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

                    b.HasOne("Hive.Models.User", "Uploader")
                        .WithMany("Uploaded")
                        .HasForeignKey("UploaderAlternativeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Channel");

                    b.Navigation("Uploader");
                });

            modelBuilder.Entity("ModUser", b =>
                {
                    b.HasOne("Hive.Models.Mod", null)
                        .WithMany()
                        .HasForeignKey("AuthoredId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Hive.Models.User", null)
                        .WithMany()
                        .HasForeignKey("AuthorsAlternativeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ModUser1", b =>
                {
                    b.HasOne("Hive.Models.Mod", null)
                        .WithMany()
                        .HasForeignKey("ContributedToId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Hive.Models.User", null)
                        .WithMany()
                        .HasForeignKey("ContributorsAlternativeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Hive.Models.Mod", b =>
                {
                    b.Navigation("Localizations");
                });

            modelBuilder.Entity("Hive.Models.User", b =>
                {
                    b.Navigation("Uploaded");
                });
#pragma warning restore 612, 618
        }
    }
}
