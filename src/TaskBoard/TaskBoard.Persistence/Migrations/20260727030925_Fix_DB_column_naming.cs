using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskBoard.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_DB_column_naming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreadtedBy",
                table: "ActivityLogs",
                newName: "CreatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "ActivityLogs",
                newName: "CreadtedBy");
        }
    }
}
