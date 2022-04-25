using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalSmtp.Server.Infrastructure.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImapState",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastUid = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImapState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Log = table.Column<string>(type: "TEXT", nullable: false),
                    ClientAddress = table.Column<string>(type: "TEXT", nullable: false),
                    ClientName = table.Column<string>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NumberOfMessages = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionError = table.Column<string>(type: "TEXT", nullable: false),
                    SessionErrorType = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImapUid = table.Column<long>(type: "INTEGER", nullable: false),
                    From = table.Column<string>(type: "TEXT", nullable: false),
                    To = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    MimeParseError = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttachmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUnread = table.Column<bool>(type: "INTEGER", nullable: false),
                    RelayError = table.Column<string>(type: "TEXT", nullable: false),
                    SecureConnection = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageRelays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    To = table.Column<string>(type: "TEXT", nullable: false),
                    SendDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageRelays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageRelays_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageRelays_MessageId",
                table: "MessageRelays",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SessionId",
                table: "Messages",
                column: "SessionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImapState");

            migrationBuilder.DropTable(
                name: "MessageRelays");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Sessions");
        }
    }
}
