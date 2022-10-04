using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CopyJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdToUpdate = table.Column<int>(type: "INTEGER", nullable: false),
                    PathToFile = table.Column<string>(type: "TEXT", nullable: true),
                    ArchivePath = table.Column<string>(type: "TEXT", nullable: true),
                    Retries = table.Column<int>(type: "INTEGER", nullable: false),
                    processed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopyJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoldersToMonitor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Exists = table.Column<bool>(type: "INTEGER", nullable: false),
                    Extensions = table.Column<string>(type: "TEXT", nullable: true),
                    FullPath = table.Column<string>(type: "TEXT", nullable: true),
                    MaxSize = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoldersToMonitor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FolderToMonitorId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FFolders_FoldersToMonitor_FolderToMonitorId",
                        column: x => x.FolderToMonitorId,
                        principalTable: "FoldersToMonitor",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FFolderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    FullPath = table.Column<string>(type: "TEXT", nullable: true),
                    ParentFolder = table.Column<string>(type: "TEXT", nullable: true),
                    Hash = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Extension = table.Column<string>(type: "TEXT", nullable: true),
                    ArchivePath = table.Column<string>(type: "TEXT", nullable: true),
                    Iszip = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FFiles_FFolders_FFolderId",
                        column: x => x.FFolderId,
                        principalTable: "FFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FFiles_FFolderId",
                table: "FFiles",
                column: "FFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_FFolders_FolderToMonitorId",
                table: "FFolders",
                column: "FolderToMonitorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CopyJobs");

            migrationBuilder.DropTable(
                name: "FFiles");

            migrationBuilder.DropTable(
                name: "FFolders");

            migrationBuilder.DropTable(
                name: "FoldersToMonitor");
        }
    }
}
