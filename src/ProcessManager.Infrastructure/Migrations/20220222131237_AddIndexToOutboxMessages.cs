using Microsoft.EntityFrameworkCore.Migrations;

namespace ProcessManager.Infrastructure.Migrations
{
    public partial class AddIndexToOutboxMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedDateType",
                table: "OutboxMessages",
                columns: new string[] { "ProcessedDate", "Type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedDateType",
                table: "OutboxMessages");
        }
    }
}
