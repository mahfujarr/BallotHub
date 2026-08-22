using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BallotHub.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionToVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "Votes",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE votes
                SET PositionId = candidates.PositionId
                FROM Votes AS votes
                INNER JOIN Candidates AS candidates ON candidates.Id = votes.CandidateId;

                IF EXISTS (SELECT 1 FROM Votes WHERE PositionId IS NULL)
                BEGIN
                    ;THROW 50000, 'Cannot map every existing vote to a position.', 1;
                END;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "PositionId",
                table: "Votes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Votes_ElectionId_UserId",
                table: "Votes");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ElectionId_PositionId_UserId",
                table: "Votes",
                columns: new[] { "ElectionId", "PositionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votes_PositionId",
                table: "Votes",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Positions_PositionId",
                table: "Votes",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Positions_PositionId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_Votes_ElectionId_PositionId_UserId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_Votes_PositionId",
                table: "Votes");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "Votes");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ElectionId_UserId",
                table: "Votes",
                columns: new[] { "ElectionId", "UserId" },
                unique: true);
        }
    }
}
