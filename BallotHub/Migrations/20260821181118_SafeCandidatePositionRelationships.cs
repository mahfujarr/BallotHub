using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BallotHub.Migrations
{
    /// <inheritdoc />
    public partial class SafeCandidatePositionRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Elections_ElectionId",
                table: "Candidates");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Elections_ElectionId",
                table: "Candidates",
                column: "ElectionId",
                principalTable: "Elections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Elections_ElectionId",
                table: "Candidates");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Elections_ElectionId",
                table: "Candidates",
                column: "ElectionId",
                principalTable: "Elections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
