using SPC.API.Contracts.Clientes;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Clientes CRUD operations
/// </summary>
public static class ClientesEndpoints
{
    public static IEndpointRouteBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes")
            .WithTags("Clientes");

        // GET /api/clientes - List all active customers
        group.MapGet("/", async (IClientesService service) =>
        {
            var clientes = await service.GetAllAsync();
            return Results.Ok(clientes);
        })
        .WithName("GetClientes")
        .WithDescription("Returns all active customers");

        // GET /api/clientes/{id} - Get customer by ID
        group.MapGet("/{id:int}", async (int id, IClientesService service) =>
        {
            var cliente = await service.GetByIdAsync(id);
            return cliente != null
                ? Results.Ok(cliente)
                : Results.NotFound(new { error = "Cliente no encontrado" });
        })
        .WithName("GetClienteById")
        .WithDescription("Returns a customer by ID");

        // GET /api/clientes/buscar?nombre=xxx - Search by name
        group.MapGet("/buscar", async (string? nombre, IClientesService service) =>
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return Results.BadRequest(new { error = "Debe proporcionar un nombre para buscar" });

            var clientes = await service.SearchAsync(nombre);
            return Results.Ok(clientes);
        })
        .WithName("SearchClientes")
        .WithDescription("Search customers by name (RazonSocial or NombreFantasia)");

        // POST /api/clientes - Create new customer
        group.MapPost("/", async (CreateClienteRequest request, IClientesService service) =>
        {
            var cliente = await service.CreateAsync(request);
            return Results.Created($"/api/clientes/{cliente.Id}", cliente);
        })
        .WithName("CreateCliente")
        .WithDescription("Creates a new customer");

        // PUT /api/clientes/{id} - Update customer
        group.MapPut("/{id:int}", async (int id, UpdateClienteRequest request, IClientesService service) =>
        {
            var cliente = await service.UpdateAsync(id, request);
            return cliente != null
                ? Results.Ok(cliente)
                : Results.NotFound(new { error = "Cliente no encontrado" });
        })
        .WithName("UpdateCliente")
        .WithDescription("Updates an existing customer");

        // DELETE /api/clientes/{id} - Soft delete customer
        group.MapDelete("/{id:int}", async (int id, IClientesService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "Cliente no encontrado" });
        })
        .WithName("DeleteCliente")
        .WithDescription("Soft deletes a customer (sets Activo = false)");

        return app;
    }
}
