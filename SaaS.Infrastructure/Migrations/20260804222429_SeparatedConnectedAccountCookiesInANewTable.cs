using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparatedConnectedAccountCookiesInANewTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Jobs_Runs_RunId1",
            //    table: "Jobs");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Leads_ConnectedAccounts_ConnectedAccountId",
            //    table: "Leads");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Leads_Runs_RunId1",
            //    table: "Leads");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Leads_TargetGroups_TargetGroupId",
            //    table: "Leads");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Runs_ConnectedAccounts_ConnectedAccountId",
            //    table: "Runs");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Runs_TargetGroups_TargetGroupId",
            //    table: "Runs");

            //migrationBuilder.DropIndex(
            //    name: "IX_Runs_ConnectedAccountId",
            //    table: "Runs");

            //migrationBuilder.DropIndex(
            //    name: "IX_Runs_TargetGroupId",
            //    table: "Runs");

            //migrationBuilder.DropIndex(
            //    name: "IX_Leads_ConnectedAccountId",
            //    table: "Leads");

            //migrationBuilder.DropIndex(
            //    name: "IX_Leads_RunId1",
            //    table: "Leads");

            //migrationBuilder.DropIndex(
            //    name: "IX_Leads_TargetGroupId",
            //    table: "Leads");

            //migrationBuilder.DropIndex(
            //    name: "IX_Jobs_RunId1",
            //    table: "Jobs");

            //migrationBuilder.DropColumn(
            //    name: "ConnectedAccountId",
            //    table: "Runs");

            //migrationBuilder.DropColumn(
            //    name: "TargetGroupId",
            //    table: "Runs");

            //migrationBuilder.DropColumn(
            //    name: "ConnectedAccountId",
            //    table: "Leads");

            //migrationBuilder.DropColumn(
            //    name: "RunId1",
            //    table: "Leads");

            //migrationBuilder.DropColumn(
            //    name: "TargetGroupId",
            //    table: "Leads");

            //migrationBuilder.DropColumn(
            //    name: "RunId1",
            //    table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CookiesEncrypted",
                table: "ConnectedAccounts");

            migrationBuilder.DropColumn(
                name: "CookiesExpireDate",
                table: "ConnectedAccounts");

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "ConnectedAccounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ConnectedAccountCookies",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    EncryptedCookies = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CookiesExpireDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedAccountCookies", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_ConnectedAccountCookies_ConnectedAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "ConnectedAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectedAccountCookies");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "ConnectedAccounts");

            //migrationBuilder.AddColumn<int>(
            //    name: "ConnectedAccountId",
            //    table: "Runs",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "TargetGroupId",
            //    table: "Runs",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "ConnectedAccountId",
            //    table: "Leads",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "RunId1",
            //    table: "Leads",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "TargetGroupId",
            //    table: "Leads",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "RunId1",
            //    table: "Jobs",
            //    type: "int",
            //    nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CookiesEncrypted",
                table: "ConnectedAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CookiesExpireDate",
                table: "ConnectedAccounts",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            //migrationBuilder.CreateIndex(
            //    name: "IX_Runs_ConnectedAccountId",
            //    table: "Runs",
            //    column: "ConnectedAccountId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Runs_TargetGroupId",
            //    table: "Runs",
            //    column: "TargetGroupId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Leads_ConnectedAccountId",
            //    table: "Leads",
            //    column: "ConnectedAccountId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Leads_RunId1",
            //    table: "Leads",
            //    column: "RunId1");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Leads_TargetGroupId",
            //    table: "Leads",
            //    column: "TargetGroupId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Jobs_RunId1",
            //    table: "Jobs",
            //    column: "RunId1");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Jobs_Runs_RunId1",
            //    table: "Jobs",
            //    column: "RunId1",
            //    principalTable: "Runs",
            //    principalColumn: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Leads_ConnectedAccounts_ConnectedAccountId",
            //    table: "Leads",
            //    column: "ConnectedAccountId",
            //    principalTable: "ConnectedAccounts",
            //    principalColumn: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Leads_Runs_RunId1",
            //    table: "Leads",
            //    column: "RunId1",
            //    principalTable: "Runs",
            //    principalColumn: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Leads_TargetGroups_TargetGroupId",
            //    table: "Leads",
            //    column: "TargetGroupId",
            //    principalTable: "TargetGroups",
            //    principalColumn: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Runs_ConnectedAccounts_ConnectedAccountId",
            //    table: "Runs",
            //    column: "ConnectedAccountId",
            //    principalTable: "ConnectedAccounts",
            //    principalColumn: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Runs_TargetGroups_TargetGroupId",
            //    table: "Runs",
            //    column: "TargetGroupId",
            //    principalTable: "TargetGroups",
            //    principalColumn: "Id");
        }
    }
}
