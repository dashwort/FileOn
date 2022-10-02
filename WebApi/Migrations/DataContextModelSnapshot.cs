﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebApi.Helpers;

#nullable disable

namespace WebApi.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.9");

            modelBuilder.Entity("WebApi.Entities.CopyJob", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ArchivePath")
                        .HasColumnType("TEXT");

                    b.Property<int>("IdToUpdate")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PathToFile")
                        .HasColumnType("TEXT");

                    b.Property<int>("Retries")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("processed")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("CopyJobs");
                });

            modelBuilder.Entity("WebApi.Entities.FFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ArchivePath")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Extension")
                        .HasColumnType("TEXT");

                    b.Property<int>("FFolderId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FullPath")
                        .HasColumnType("TEXT");

                    b.Property<string>("Hash")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Iszip")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("ParentFolder")
                        .HasColumnType("TEXT");

                    b.Property<long>("Size")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("FFolderId");

                    b.ToTable("FFiles");
                });

            modelBuilder.Entity("WebApi.Entities.FFolder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("FFolders");
                });

            modelBuilder.Entity("WebApi.Entities.FFile", b =>
                {
                    b.HasOne("WebApi.Entities.FFolder", "FFolder")
                        .WithMany("FFiles")
                        .HasForeignKey("FFolderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FFolder");
                });

            modelBuilder.Entity("WebApi.Entities.FFolder", b =>
                {
                    b.Navigation("FFiles");
                });
#pragma warning restore 612, 618
        }
    }
}
