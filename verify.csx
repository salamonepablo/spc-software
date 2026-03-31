#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 9.0.0"
#r "SPC.API/bin/Debug/net10.0/SPC.API.dll"
#r "SPC.Shared/bin/Debug/net10.0/SPC.Shared.dll"

using Microsoft.EntityFrameworkCore;
using SPC.API.Data;

var connStr = "Server=(localdb)\MSSQLLocalDB;Database=SPC;Trusted_Connection=True;TrustServerCertificate=True;";
var options = new DbContextOptionsBuilder<SPCDbContext>().UseSqlServer(connStr).Options;
using var db = new SPCDbContext(options);

Console.WriteLine($"Clientes: {db.Clientes.Count()}");
Console.WriteLine($"Productos: {db.Productos.Count()}");
foreach (var c in db.Clientes.Take(3))
    Console.WriteLine($"  - {c.RazonSocial} ({c.CUIT})");
