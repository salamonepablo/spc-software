using SPC.API.Contracts.Products;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Products CRUD operations
/// </summary>
public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/productos")
            .WithTags("Products");

        // GET /api/productos - List all active products
        group.MapGet("/", async (IProductsService service) =>
        {
            var productos = await service.GetAllAsync();
            return Results.Ok(productos);
        })
        .WithName("GetProducts")
        .WithDescription("Returns all active products");

        // GET /api/productos/{id} - Get product by ID
        group.MapGet("/{id:int}", async (int id, IProductsService service) =>
        {
            var producto = await service.GetByIdAsync(id);
            return producto != null
                ? Results.Ok(producto)
                : Results.NotFound(new { error = "Product no encontrado" });
        })
        .WithName("GetProductById")
        .WithDescription("Returns a product by ID");

        // GET /api/productos/buscar?descripcion=xxx - Search by description or code
        group.MapGet("/buscar", async (string? descripcion, IProductsService service) =>
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                return Results.BadRequest(new { error = "Debe proporcionar una descripción" });

            var productos = await service.SearchAsync(descripcion);
            return Results.Ok(productos);
        })
        .WithName("SearchProducts")
        .WithDescription("Search products by description or code");

        // POST /api/productos - Create new product
        group.MapPost("/", async (CreateProductRequest request, IProductsService service) =>
        {
            var producto = await service.CreateAsync(request);
            return Results.Created($"/api/productos/{producto.Id}", producto);
        })
        .WithName("CreateProduct")
        .WithDescription("Creates a new product");

        // PUT /api/productos/{id} - Update product
        group.MapPut("/{id:int}", async (int id, UpdateProductRequest request, IProductsService service) =>
        {
            var producto = await service.UpdateAsync(id, request);
            return producto != null
                ? Results.Ok(producto)
                : Results.NotFound(new { error = "Product no encontrado" });
        })
        .WithName("UpdateProduct")
        .WithDescription("Updates an existing product");

        // DELETE /api/productos/{id} - Soft delete product
        group.MapDelete("/{id:int}", async (int id, IProductsService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "Product no encontrado" });
        })
        .WithName("DeleteProduct")
        .WithDescription("Soft deletes a product (sets Activo = false)");

        return app;
    }
}
