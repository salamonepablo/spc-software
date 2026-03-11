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

        // GET /api/clientes - List all active customers
        group.MapGet("/", async (ICustomersService service) =>
        {
            var clientes = await service.GetAllAsync();
            return Results.Ok(clientes);
        })
        .WithName("GetCustomers")
        .WithDescription("Returns all active customers");

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

        // GET /api/clientes/buscar?nombre=xxx - Search by name
        group.MapGet("/buscar", async (string? nombre, ICustomersService service) =>
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return Results.BadRequest(new { error = "Debe proporcionar un nombre para buscar" });

            var clientes = await service.SearchAsync(nombre);
            return Results.Ok(clientes);
        })
        .WithName("SearchCustomers")
        .WithDescription("Search customers by name (RazonSocial or NombreFantasia)");

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
        .WithDescription("Soft deletes a customer (sets Activo = false)");

        return app;
    }
}
