using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorInvoice.Db.Migrations
{
    /// <inheritdoc />
    public partial class WorkList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "InvoiceParties",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TempWorkEntries",
                columns: table => new
                {
                    TempWorkEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LastModified = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    WorkEntriesBlob = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempWorkEntries", x => x.TempWorkEntryId);
                });

            migrationBuilder.CreateTable(
                name: "WorkEntries",
                columns: table => new
                {
                    WorkEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntryGuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    Job = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    Billed = table.Column<bool>(type: "INTEGER", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    InvoicePartyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkEntries", x => x.WorkEntryId);
                    table.ForeignKey(
                        name: "FK_WorkEntries_InvoiceParties_InvoicePartyId",
                        column: x => x.InvoicePartyId,
                        principalTable: "InvoiceParties",
                        principalColumn: "InvoicePartyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_EntryGuid",
                table: "WorkEntries",
                column: "EntryGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_InvoicePartyId",
                table: "WorkEntries",
                column: "InvoicePartyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TempWorkEntries");

            migrationBuilder.DropTable(
                name: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "InvoiceParties");
        }
    }
}
