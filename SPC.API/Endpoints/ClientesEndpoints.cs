using SPC.API.Contracts.Customers;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Customers CRUD operations
/// </summary>
public static class CustomersEndpoints
{
    public static IEndpointRouteBuilder MapCustomersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes")
            .WithTags("Customers");

        // GET /api/clientes/count - Count active customers (optimized)
        group.MapGet("/count", async (ICustomersService service) =>
        {
            var total = await service.CountAsync();
            return Results.Ok(new { total });
        })
        .WithName("GetCustomersCount")
        .WithDescription("Returns count of active customers (optimized query)");

        // GET /api/clientes - List active customers (paginated)
        group.MapGet("/", async (int? skip, int? take, ICustomersService service) =>
        {
            // If no pagination params, return first 50 for performance
            var clientes = await service.GetPagedAsync(skip ?? 0, take ?? 50);
            return Results.Ok(clientes);
        })
        .WithName("GetCustomers")
        .WithDescription("Returns active customers (paginated, default 50)");

        // GET /api/clientes/all - List ALL active customers (legacy, for dropdowns)
        group.MapGet("/all", async (ICustomersService service) =>
        {
            var clientes = await service.GetAllAsync();
            return Results.Ok(clientes);
        })
        .WithName("GetAllCustomers")
        .WithDescription("Returns ALL active customers (use with caution)");

        // GET /api/clientes/{id} - Get customer by ID
        group.MapGet("/{id:int}", async (int id, ICustomersService service) =>
        {
            var cliente = await service.GetByIdAsync(id);
            return cliente != null
                ? Results.Ok(cliente)
                : Results.NotFound(new { error = "Customer no encontrado" });
        })
        .WithName("GetCustomerById")
        .WithDescription("Returns a customer by ID");

        // GET /api/clientes/buscar?Name=xxx - Search by internal Id or name
        group.MapGet("/buscar", async (string? Name, ICustomersService service) =>
        {
            if (string.IsNullOrWhiteSpace(Name))
                return Results.BadRequest(new { error = "Debe proporcionar un Name para buscar" });

            var clientes = await service.SearchAsync(Name);
            return Results.Ok(clientes);
        })
        .WithName("SearchCustomers")
        .WithDescription("Search customers by internal Id or name (CompanyName or TradeName)");

        // POST /api/clientes - Create new customer
        group.MapPost("/", async (CreateCustomerRequest request, ICustomersService service) =>
        {
            var cliente = await service.CreateAsync(request);
            return Results.Created($"/api/clientes/{cliente.Id}", cliente);
        })
        .WithName("CreateCustomer")
        .WithDescription("Creates a new customer");

        // PUT /api/clientes/{id} - Update customer
        group.MapPut("/{id:int}", async (int id, UpdateCustomerRequest request, ICustomersService service) =>
        {
            var cliente = await service.UpdateAsync(id, request);
            return cliente != null
                ? Results.Ok(cliente)
                : Results.NotFound(new { error = "Customer no encontrado" });
        })
        .WithName("UpdateCustomer")
        .WithDescription("Updates an existing customer");

        // DELETE /api/clientes/{id} - Soft delete customer
        group.MapDelete("/{id:int}", async (int id, ICustomersService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "Customer no encontrado" });
        })
        .WithName("DeleteCustomer")
        .WithDescription("Soft deletes a customer (sets IsActive = false)");

        return app;
    }
}
