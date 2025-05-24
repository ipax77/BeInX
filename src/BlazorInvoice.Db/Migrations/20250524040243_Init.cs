using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorInvoice.Db.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppConfigs",
                columns: table => new
                {
                    AppConfigId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastBackup = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BackupFolder = table.Column<string>(type: "TEXT", nullable: false),
                    BackupInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    CultureName = table.Column<string>(type: "TEXT", nullable: false),
                    SchematronValidationUri = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShowFormDescriptions = table.Column<bool>(type: "INTEGER", precision: 0, nullable: false),
                    ShowValidationWarnings = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExportEmbedPdf = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExportValidate = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExportFinalize = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExportType = table.Column<int>(type: "INTEGER", nullable: false),
                    StatsMonthEndDay = table.Column<int>(type: "INTEGER", nullable: false),
                    StatsIsMonthNotQuater = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppConfigs", x => x.AppConfigId);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceParties",
                columns: table => new
                {
                    InvoicePartyId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LogoReferenceId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StreetName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PostCode = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RegistrationName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TaxId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BuyerReference = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CompanyId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Logo = table.Column<byte[]>(type: "BLOB", nullable: true),
                    IsSeller = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceParties", x => x.InvoicePartyId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMeans",
                columns: table => new
                {
                    PaymentMeansId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Iban = table.Column<string>(type: "TEXT", maxLength: 22, nullable: false),
                    Bic = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PaymentMeansTypeCode = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMeans", x => x.PaymentMeansId);
                });

            migrationBuilder.CreateTable(
                name: "TempInvoices",
                columns: table => new
                {
                    TempInvoiceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    InvoiceBlob = table.Column<byte[]>(type: "BLOB", nullable: false),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    SellerPartyId = table.Column<int>(type: "INTEGER", nullable: true),
                    BuyerPartyId = table.Column<int>(type: "INTEGER", nullable: true),
                    PaymentMeansId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempInvoices", x => x.TempInvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GlobalTaxCategory = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    GlobalTaxScheme = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    GlobalTax = table.Column<double>(type: "REAL", nullable: false),
                    Id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    InvoiceTypeCode = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    PaymentTermsNote = table.Column<string>(type: "TEXT", nullable: false),
                    PayableAmount = table.Column<double>(type: "REAL", nullable: false),
                    SellerPartyId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsImported = table.Column<bool>(type: "INTEGER", nullable: false),
                    BuyerPartyId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentMeansId = table.Column<int>(type: "INTEGER", nullable: false),
                    XmlInvoiceCreated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    XmlInvoiceSha1Hash = table.Column<string>(type: "TEXT", nullable: true),
                    XmlInvoiceBlob = table.Column<byte[]>(type: "BLOB", nullable: true),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalAmountWithoutVat = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_Invoices_InvoiceParties_BuyerPartyId",
                        column: x => x.BuyerPartyId,
                        principalTable: "InvoiceParties",
                        principalColumn: "InvoicePartyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invoices_InvoiceParties_SellerPartyId",
                        column: x => x.SellerPartyId,
                        principalTable: "InvoiceParties",
                        principalColumn: "InvoicePartyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invoices_PaymentMeans_PaymentMeansId",
                        column: x => x.PaymentMeansId,
                        principalTable: "PaymentMeans",
                        principalColumn: "PaymentMeansId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalDocumentReferences",
                columns: table => new
                {
                    AdditionalDocumentReferenceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MimeCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DocumentDescription = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Content = table.Column<byte[]>(type: "BLOB", nullable: false),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalDocumentReferences", x => x.AdditionalDocumentReferenceId);
                    table.ForeignKey(
                        name: "FK_AdditionalDocumentReferences_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    InvoiceLineId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Id = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<double>(type: "REAL", nullable: false),
                    QuantityCode = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    UnitPrice = table.Column<double>(type: "REAL", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.InvoiceLineId);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalDocumentReferences_InvoiceId",
                table: "AdditionalDocumentReferences",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceParties_Email",
                table: "InvoiceParties",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceParties_IsSeller",
                table: "InvoiceParties",
                column: "IsSeller");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BuyerPartyId",
                table: "Invoices",
                column: "BuyerPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Id",
                table: "Invoices",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IssueDate",
                table: "Invoices",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentMeansId",
                table: "Invoices",
                column: "PaymentMeansId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SellerPartyId",
                table: "Invoices",
                column: "SellerPartyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdditionalDocumentReferences");

            migrationBuilder.DropTable(
                name: "AppConfigs");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "TempInvoices");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "InvoiceParties");

            migrationBuilder.DropTable(
                name: "PaymentMeans");
        }
    }
}
