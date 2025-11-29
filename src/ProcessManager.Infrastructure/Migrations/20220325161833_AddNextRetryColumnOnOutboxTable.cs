using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ProcessManager.Infrastructure.Migrations
{
    public partial class AddNextRetryColumnOnOutboxTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>("NextRetryDate", "OutboxMessages", nullable: true);
            migrationBuilder.AddColumn<int>("RetryAttempt", "OutboxMessages", nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedDateType",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedDateTypeNextRetryDate",
                table: "OutboxMessages",
                columns: new string[] { "ProcessedDate", "Type", "NextRetryDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedDateTypeNextRetryDate",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedDateType",
                table: "OutboxMessages",
                columns: new string[] { "ProcessedDate", "Type" });

            migrationBuilder.DropColumn("NextRetryDate", "OutboxMessages");
            migrationBuilder.DropColumn("RetryAttempt", "OutboxMessages");
        }
    }
}
