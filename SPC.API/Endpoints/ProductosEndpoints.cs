using SPC.API.Contracts.Productos;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Productos CRUD operations
/// </summary>
public static class ProductosEndpoints
{
    public static IEndpointRouteBuilder MapProductosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/productos")
            .WithTags("Productos");

        // GET /api/productos - List all active products
        group.MapGet("/", async (IProductosService service) =>
        {
            var productos = await service.GetAllAsync();
            return Results.Ok(productos);
        })
        .WithName("GetProductos")
        .WithDescription("Returns all active products");

        // GET /api/productos/{id} - Get product by ID
        group.MapGet("/{id:int}", async (int id, IProductosService service) =>
        {
            var producto = await service.GetByIdAsync(id);
            return producto != null
                ? Results.Ok(producto)
                : Results.NotFound(new { error = "Producto no encontrado" });
        })
        .WithName("GetProductoById")
        .WithDescription("Returns a product by ID");

        // GET /api/productos/buscar?descripcion=xxx - Search by description or code
        group.MapGet("/buscar", async (string? descripcion, IProductosService service) =>
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                return Results.BadRequest(new { error = "Debe proporcionar una descripción" });

            var productos = await service.SearchAsync(descripcion);
            return Results.Ok(productos);
        })
        .WithName("SearchProductos")
        .WithDescription("Search products by description or code");

        // POST /api/productos - Create new product
        group.MapPost("/", async (CreateProductoRequest request, IProductosService service) =>
        {
            var producto = await service.CreateAsync(request);
            return Results.Created($"/api/productos/{producto.Id}", producto);
        })
        .WithName("CreateProducto")
        .WithDescription("Creates a new product");

        // PUT /api/productos/{id} - Update product
        group.MapPut("/{id:int}", async (int id, UpdateProductoRequest request, IProductosService service) =>
        {
            var producto = await service.UpdateAsync(id, request);
            return producto != null
                ? Results.Ok(producto)
                : Results.NotFound(new { error = "Producto no encontrado" });
        })
        .WithName("UpdateProducto")
        .WithDescription("Updates an existing product");

        // DELETE /api/productos/{id} - Soft delete product
        group.MapDelete("/{id:int}", async (int id, IProductosService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "Producto no encontrado" });
        })
        .WithName("DeleteProducto")
        .WithDescription("Soft deletes a product (sets Activo = false)");

        return app;
    }
}
