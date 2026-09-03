using System;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260903100000_AddBrokerUserAuth0UserId")]
    public partial class AddBrokerUserAuth0UserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Auth0UserId",
                table: "BrokerUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerUsers_Auth0UserId",
                table: "BrokerUsers",
                column: "Auth0UserId",
                unique: true,
                filter: "[Auth0UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BrokerUsers_Auth0UserId",
                table: "BrokerUsers");

            migrationBuilder.DropColumn(
                name: "Auth0UserId",
                table: "BrokerUsers");
        }
    }
}
