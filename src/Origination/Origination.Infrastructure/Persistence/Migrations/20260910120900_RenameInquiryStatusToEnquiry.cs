using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Origination.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameInquiryStatusToEnquiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Cases SET Status = N'Enquiry' WHERE Status = N'Inquiry';
                UPDATE Cases SET Status = N'NotProceeded' WHERE Status IN (N'Declined', N'Submitted');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Cases SET Status = N'Inquiry' WHERE Status = N'Enquiry';
                """);
        }
    }
}
