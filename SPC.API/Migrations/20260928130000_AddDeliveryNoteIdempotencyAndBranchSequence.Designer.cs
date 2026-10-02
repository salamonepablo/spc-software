using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SPC.API.Data;

#nullable disable

namespace SPC.API.Migrations;

[DbContext(typeof(SPCDbContext))]
[Migration("20260928130000_AddDeliveryNoteIdempotencyAndBranchSequence")]
partial class AddDeliveryNoteIdempotencyAndBranchSequence
{
}
