﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TrustGuard.Data;

#nullable disable

namespace TrustGuard.Migrations
{
    [DbContext(typeof(TrustGuardContext))]
    [Migration("20230629112127_KeyPairMigration")]
    partial class KeyPairMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TrustGuard.Data.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Avatar")
                        .HasColumnType("text");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SecurityCode")
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Account", (string)null);
                });

            modelBuilder.Entity("TrustGuard.Data.Application", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AccountId")
                        .HasColumnType("integer");

                    b.Property<string>("AppLogo")
                        .HasColumnType("text");

                    b.Property<string>("AppName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("AppType")
                        .HasColumnType("integer");

                    b.Property<string>("ClientId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ClientSecret")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("DomainId")
                        .HasColumnType("integer");

                    b.Property<bool?>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime?>("ModifiedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("DomainId");

                    b.ToTable("Application", (string)null);
                });

            modelBuilder.Entity("TrustGuard.Data.BasePoint", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ApplicationId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("DomainId")
                        .HasColumnType("integer");

                    b.Property<bool?>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<bool?>("IsSuspicious")
                        .HasColumnType("boolean");

                    b.Property<string>("x")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("y")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationId");

                    b.HasIndex("DomainId");

                    b.ToTable("BasePoint", (string)null);
                });

            modelBuilder.Entity("TrustGuard.Data.Demand", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool?>("IsSeen")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("IssuedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("Demand", (string)null);
                });

            modelBuilder.Entity("TrustGuard.Data.Domain", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("N")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("a")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("b")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("count")
                        .HasColumnType("integer");

                    b.Property<string>("p")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("webcode")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("x")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("y")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Domain", (string)null);
                });

            modelBuilder.Entity("TrustGuard.Data.KeyPair", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AccessToken")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("AccountId")
                        .HasColumnType("integer");

                    b.Property<int>("BasePointId")
                        .HasColumnType("integer");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SecureKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("BasePointId");

                    b.ToTable("KeyPair");
                });

            modelBuilder.Entity("TrustGuard.Data.Application", b =>
                {
                    b.HasOne("TrustGuard.Data.Account", "Account")
                        .WithMany("Applications")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TrustGuard.Data.Domain", "Domain")
                        .WithMany("Applications")
                        .HasForeignKey("DomainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");

                    b.Navigation("Domain");
                });

            modelBuilder.Entity("TrustGuard.Data.BasePoint", b =>
                {
                    b.HasOne("TrustGuard.Data.Application", "Application")
                        .WithMany("BasePoints")
                        .HasForeignKey("ApplicationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TrustGuard.Data.Domain", "Domain")
                        .WithMany()
                        .HasForeignKey("DomainId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Application");

                    b.Navigation("Domain");
                });

            modelBuilder.Entity("TrustGuard.Data.KeyPair", b =>
                {
                    b.HasOne("TrustGuard.Data.Account", "Account")
                        .WithMany("KeyPairs")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TrustGuard.Data.BasePoint", "BasePoint")
                        .WithMany("KeyPairs")
                        .HasForeignKey("BasePointId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");

                    b.Navigation("BasePoint");
                });

            modelBuilder.Entity("TrustGuard.Data.Account", b =>
                {
                    b.Navigation("Applications");

                    b.Navigation("KeyPairs");
                });

            modelBuilder.Entity("TrustGuard.Data.Application", b =>
                {
                    b.Navigation("BasePoints");
                });

            modelBuilder.Entity("TrustGuard.Data.BasePoint", b =>
                {
                    b.Navigation("KeyPairs");
                });

            modelBuilder.Entity("TrustGuard.Data.Domain", b =>
                {
                    b.Navigation("Applications");
                });
#pragma warning restore 612, 618
        }
    }
}
