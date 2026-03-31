using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPC.API.Migrations
{
    /// <inheritdoc />
    public partial class SplitServicesCQRSLite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CasualDeliveryNotes_SalesRepes_SalesRepId",
                table: "CasualDeliveryNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Consignments_SalesRepes_SalesRepId",
                table: "Consignments");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditNotes_SalesRepes_SalesRepId",
                table: "CreditNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_CondicionesIva_TaxConditionId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_SalesRepes_SalesRepId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_ZonasVenta_SalesZoneId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_DebitNotes_SalesRepes_SalesRepId",
                table: "DebitNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryNotes_SalesRepes_SalesRepId",
                table: "DeliveryNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_InternalDebitNotes_SalesRepes_SalesRepId",
                table: "InternalDebitNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_SalesRepes_SalesRepId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categorys_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_UnidadesMedida_UnitOfMeasureId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_SalesRepes_SalesRepId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_SalesRepes_SalesRepAsociadoId",
                table: "Warehouses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ZonasVenta",
                table: "ZonasVenta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnidadesMedida",
                table: "UnidadesMedida");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesRepes",
                table: "SalesRepes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CondicionesIva",
                table: "CondicionesIva");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categorys",
                table: "Categorys");

            migrationBuilder.RenameTable(
                name: "ZonasVenta",
                newName: "SalesZones");

            migrationBuilder.RenameTable(
                name: "UnidadesMedida",
                newName: "UnitsOfMeasure");

            migrationBuilder.RenameTable(
                name: "SalesRepes",
                newName: "SalesReps");

            migrationBuilder.RenameTable(
                name: "CondicionesIva",
                newName: "TaxConditions");

            migrationBuilder.RenameTable(
                name: "Categorys",
                newName: "Categories");

            migrationBuilder.RenameColumn(
                name: "SalesRepAsociadoId",
                table: "Warehouses",
                newName: "AssociatedSalesRepId");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "Warehouses",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Direccion",
                table: "Warehouses",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "Activo",
                table: "Warehouses",
                newName: "IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_Warehouses_SalesRepAsociadoId",
                table: "Warehouses",
                newName: "IX_Warehouses_AssociatedSalesRepId");

            migrationBuilder.RenameColumn(
                name: "Cantidad",
                table: "Stocks",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "StockMinimo",
                table: "Products",
                newName: "MinimumStock");

            migrationBuilder.RenameColumn(
                name: "PrecioVenta",
                table: "Products",
                newName: "SalePrice");

            migrationBuilder.RenameColumn(
                name: "PrecioQuote",
                table: "Products",
                newName: "QuotePrice");

            migrationBuilder.RenameColumn(
                name: "PrecioInvoice",
                table: "Products",
                newName: "InvoicePrice");

            migrationBuilder.RenameColumn(
                name: "PrecioCosto",
                table: "Products",
                newName: "CostPrice");

            migrationBuilder.RenameColumn(
                name: "PorcentajeIVA",
                table: "Products",
                newName: "VATPercent");

            migrationBuilder.RenameColumn(
                name: "Observaciones",
                table: "Products",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Descripcion",
                table: "Products",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "CodigoProveedor",
                table: "Products",
                newName: "SupplierCode");

            migrationBuilder.RenameColumn(
                name: "Codigo",
                table: "Products",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "Activo",
                table: "Products",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "UnidadNegocio",
                table: "Invoices",
                newName: "SalesCondition");

            migrationBuilder.RenameColumn(
                name: "TipoInvoice",
                table: "Invoices",
                newName: "InvoiceType");

            migrationBuilder.RenameColumn(
                name: "PuntoVenta",
                table: "Invoices",
                newName: "PointOfSale");

            migrationBuilder.RenameColumn(
                name: "PorcentajeIVA",
                table: "Invoices",
                newName: "VATPercent");

            migrationBuilder.RenameColumn(
                name: "PorcentajeDescuento",
                table: "Invoices",
                newName: "IIBBPercent");

            migrationBuilder.RenameColumn(
                name: "Observaciones",
                table: "Invoices",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "NumeroInvoice",
                table: "Invoices",
                newName: "InvoiceNumber");

            migrationBuilder.RenameColumn(
                name: "ImportePercepcionIIBB",
                table: "Invoices",
                newName: "VATAmount");

            migrationBuilder.RenameColumn(
                name: "ImporteIVA",
                table: "Invoices",
                newName: "IncludedVAT");

            migrationBuilder.RenameColumn(
                name: "ImporteDescuento",
                table: "Invoices",
                newName: "DiscountAmount");

            migrationBuilder.RenameColumn(
                name: "IVAContenido",
                table: "Invoices",
                newName: "IIBBPerceptionAmount");

            migrationBuilder.RenameColumn(
                name: "FormaPago",
                table: "Invoices",
                newName: "PaymentMethodId");

            migrationBuilder.RenameColumn(
                name: "FechaVencimientoCAE",
                table: "Invoices",
                newName: "CAEExpirationDate");

            migrationBuilder.RenameColumn(
                name: "FechaInvoice",
                table: "Invoices",
                newName: "InvoiceDate");

            migrationBuilder.RenameColumn(
                name: "CondicionVenta",
                table: "Invoices",
                newName: "BusinessUnit");

            migrationBuilder.RenameColumn(
                name: "Anulada",
                table: "Invoices",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "AlicuotaIIBB",
                table: "Invoices",
                newName: "DiscountPercent");

            migrationBuilder.RenameColumn(
                name: "Aclaracion",
                table: "Invoices",
                newName: "Clarification");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_TipoInvoice_PuntoVenta_NumeroInvoice",
                table: "Invoices",
                newName: "IX_Invoices_InvoiceType_PointOfSale_InvoiceNumber");

            migrationBuilder.RenameColumn(
                name: "PrecioUnitario",
                table: "InvoiceDetails",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "PorcentajeIVA",
                table: "InvoiceDetails",
                newName: "VATPercent");

            migrationBuilder.RenameColumn(
                name: "PorcentajeDescuento",
                table: "InvoiceDetails",
                newName: "DiscountPercent");

            migrationBuilder.RenameColumn(
                name: "ItemNumero",
                table: "InvoiceDetails",
                newName: "ItemNumber");

            migrationBuilder.RenameColumn(
                name: "Cantidad",
                table: "InvoiceDetails",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "UnidadNegocio",
                table: "DeliveryNotes",
                newName: "BusinessUnit");

            migrationBuilder.RenameColumn(
                name: "TipoFactura",
                table: "DeliveryNotes",
                newName: "InvoiceType");

            migrationBuilder.RenameColumn(
                name: "PuntoVenta",
                table: "DeliveryNotes",
                newName: "PointOfSale");

            migrationBuilder.RenameColumn(
                name: "Observaciones",
                table: "DeliveryNotes",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "NumeroDeliveryNote",
                table: "DeliveryNotes",
                newName: "DeliveryNoteNumber");

            migrationBuilder.RenameColumn(
                name: "LocalidadEntrega",
                table: "DeliveryNotes",
                newName: "DeliveryCity");

            migrationBuilder.RenameColumn(
                name: "FechaDeliveryNote",
                table: "DeliveryNotes",
                newName: "DeliveryNoteDate");

            migrationBuilder.RenameColumn(
                name: "Facturado",
                table: "DeliveryNotes",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "DireccionEntrega",
                table: "DeliveryNotes",
                newName: "DeliveryAddress");

            migrationBuilder.RenameColumn(
                name: "Anulado",
                table: "DeliveryNotes",
                newName: "IsInvoiced");

            migrationBuilder.RenameColumn(
                name: "Aclaracion",
                table: "DeliveryNotes",
                newName: "Clarification");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryNotes_BranchId_NumeroDeliveryNote",
                table: "DeliveryNotes",
                newName: "IX_DeliveryNotes_BranchId_DeliveryNoteNumber");

            migrationBuilder.RenameColumn(
                name: "ItemNumero",
                table: "DeliveryNoteDetails",
                newName: "ItemNumber");

            migrationBuilder.RenameColumn(
                name: "Cantidad",
                table: "DeliveryNoteDetails",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "Telefono",
                table: "Customers",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "RazonSocial",
                table: "Customers",
                newName: "CompanyName");

            migrationBuilder.RenameColumn(
                name: "ProvinciaPadronIIBB",
                table: "Customers",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "Provincia",
                table: "Customers",
                newName: "Province");

            migrationBuilder.RenameColumn(
                name: "PorcentajeDescuento",
                table: "Customers",
                newName: "IIBBPercent");

            migrationBuilder.RenameColumn(
                name: "Observaciones",
                table: "Customers",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "NombreFantasia",
                table: "Customers",
                newName: "TradeName");

            migrationBuilder.RenameColumn(
                name: "Localidad",
                table: "Customers",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "LimiteCredito",
                table: "Customers",
                newName: "CreditLimit");

            migrationBuilder.RenameColumn(
                name: "FechaAlta",
                table: "Customers",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "Direccion",
                table: "Customers",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "CodigoPostal",
                table: "Customers",
                newName: "IIBBRegistryProvince");

            migrationBuilder.RenameColumn(
                name: "Celular",
                table: "Customers",
                newName: "Mobile");

            migrationBuilder.RenameColumn(
                name: "AlicuotaIIBB",
                table: "Customers",
                newName: "DiscountPercent");

            migrationBuilder.RenameColumn(
                name: "Activo",
                table: "Customers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "SalesZones",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Descripcion",
                table: "SalesZones",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Activa",
                table: "SalesZones",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "UnitsOfMeasure",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Codigo",
                table: "UnitsOfMeasure",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "Telefono",
                table: "SalesReps",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "Provincia",
                table: "SalesReps",
                newName: "Province");

            migrationBuilder.RenameColumn(
                name: "PorcentajeComision",
                table: "SalesReps",
                newName: "CommissionPercent");

            migrationBuilder.RenameColumn(
                name: "Observaciones",
                table: "SalesReps",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "SalesReps",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "Localidad",
                table: "SalesReps",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "Legajo",
                table: "SalesReps",
                newName: "EmployeeCode");

            migrationBuilder.RenameColumn(
                name: "FechaNacimiento",
                table: "SalesReps",
                newName: "HireDate");

            migrationBuilder.RenameColumn(
                name: "FechaIngreso",
                table: "SalesReps",
                newName: "DateOfBirth");

            migrationBuilder.RenameColumn(
                name: "Domicilio",
                table: "SalesReps",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "CodigoPostal",
                table: "SalesReps",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "Celular",
                table: "SalesReps",
                newName: "Mobile");

            migrationBuilder.RenameColumn(
                name: "Apellido",
                table: "SalesReps",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "Activo",
                table: "SalesReps",
                newName: "IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_SalesRepes_Legajo",
                table: "SalesReps",
                newName: "IX_SalesReps_EmployeeCode");

            migrationBuilder.RenameColumn(
                name: "TipoInvoice",
                table: "TaxConditions",
                newName: "InvoiceType");

            migrationBuilder.RenameColumn(
                name: "Descripcion",
                table: "TaxConditions",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Codigo",
                table: "TaxConditions",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "Categories",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Descripcion",
                table: "Categories",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Activo",
                table: "Categories",
                newName: "IsActive");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesZones",
                table: "SalesZones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnitsOfMeasure",
                table: "UnitsOfMeasure",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesReps",
                table: "SalesReps",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxConditions",
                table: "TaxConditions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Calle (SalesReps)");

            migrationBuilder.AddForeignKey(
                name: "FK_CasualDeliveryNotes_SalesReps_SalesRepId",
                table: "CasualDeliveryNotes",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Consignments_SalesReps_SalesRepId",
                table: "Consignments",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditNotes_SalesReps_SalesRepId",
                table: "CreditNotes",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_SalesReps_SalesRepId",
                table: "Customers",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_SalesZones_SalesZoneId",
                table: "Customers",
                column: "SalesZoneId",
                principalTable: "SalesZones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_TaxConditions_TaxConditionId",
                table: "Customers",
                column: "TaxConditionId",
                principalTable: "TaxConditions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DebitNotes_SalesReps_SalesRepId",
                table: "DebitNotes",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_SalesReps_SalesRepId",
                table: "DeliveryNotes",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InternalDebitNotes_SalesReps_SalesRepId",
                table: "InternalDebitNotes",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_SalesReps_SalesRepId",
                table: "Invoices",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_UnitsOfMeasure_UnitOfMeasureId",
                table: "Products",
                column: "UnitOfMeasureId",
                principalTable: "UnitsOfMeasure",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_SalesReps_SalesRepId",
                table: "Quotes",
                column: "SalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_SalesReps_AssociatedSalesRepId",
                table: "Warehouses",
                column: "AssociatedSalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CasualDeliveryNotes_SalesReps_SalesRepId",
                table: "CasualDeliveryNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Consignments_SalesReps_SalesRepId",
                table: "Consignments");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditNotes_SalesReps_SalesRepId",
                table: "CreditNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_SalesReps_SalesRepId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_SalesZones_SalesZoneId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_TaxConditions_TaxConditionId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_DebitNotes_SalesReps_SalesRepId",
                table: "DebitNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryNotes_SalesReps_SalesRepId",
                table: "DeliveryNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_InternalDebitNotes_SalesReps_SalesRepId",
                table: "InternalDebitNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_SalesReps_SalesRepId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_UnitsOfMeasure_UnitOfMeasureId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_SalesReps_SalesRepId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_SalesReps_AssociatedSalesRepId",
                table: "Warehouses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnitsOfMeasure",
                table: "UnitsOfMeasure");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxConditions",
                table: "TaxConditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesZones",
                table: "SalesZones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesReps",
                table: "SalesReps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.RenameTable(
                name: "UnitsOfMeasure",
                newName: "UnidadesMedida");

            migrationBuilder.RenameTable(
                name: "TaxConditions",
                newName: "CondicionesIva");

            migrationBuilder.RenameTable(
                name: "SalesZones",
                newName: "ZonasVenta");

            migrationBuilder.RenameTable(
                name: "SalesReps",
                newName: "SalesRepes");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Categorys");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Warehouses",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Warehouses",
                newName: "Activo");

            migrationBuilder.RenameColumn(
                name: "AssociatedSalesRepId",
                table: "Warehouses",
                newName: "SalesRepAsociadoId");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Warehouses",
                newName: "Direccion");

            migrationBuilder.RenameIndex(
                name: "IX_Warehouses_AssociatedSalesRepId",
                table: "Warehouses",
                newName: "IX_Warehouses_SalesRepAsociadoId");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Stocks",
                newName: "Cantidad");

            migrationBuilder.RenameColumn(
                name: "VATPercent",
                table: "Products",
                newName: "PorcentajeIVA");

            migrationBuilder.RenameColumn(
                name: "SupplierCode",
                table: "Products",
                newName: "CodigoProveedor");

            migrationBuilder.RenameColumn(
                name: "SalePrice",
                table: "Products",
                newName: "PrecioVenta");

            migrationBuilder.RenameColumn(
                name: "QuotePrice",
                table: "Products",
                newName: "PrecioQuote");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Products",
                newName: "Observaciones");

            migrationBuilder.RenameColumn(
                name: "MinimumStock",
                table: "Products",
                newName: "StockMinimo");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Products",
                newName: "Activo");

            migrationBuilder.RenameColumn(
                name: "InvoicePrice",
                table: "Products",
                newName: "PrecioInvoice");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Products",
                newName: "Descripcion");

            migrationBuilder.RenameColumn(
                name: "CostPrice",
                table: "Products",
                newName: "PrecioCosto");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Products",
                newName: "Codigo");

            migrationBuilder.RenameColumn(
                name: "VATPercent",
                table: "Invoices",
                newName: "PorcentajeIVA");

            migrationBuilder.RenameColumn(
                name: "VATAmount",
                table: "Invoices",
                newName: "ImportePercepcionIIBB");

            migrationBuilder.RenameColumn(
                name: "SalesCondition",
                table: "Invoices",
                newName: "UnidadNegocio");

            migrationBuilder.RenameColumn(
                name: "PointOfSale",
                table: "Invoices",
                newName: "PuntoVenta");

            migrationBuilder.RenameColumn(
                name: "PaymentMethodId",
                table: "Invoices",
                newName: "FormaPago");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Invoices",
                newName: "Observaciones");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "Invoices",
                newName: "Anulada");

            migrationBuilder.RenameColumn(
                name: "InvoiceType",
                table: "Invoices",
                newName: "TipoInvoice");

            migrationBuilder.RenameColumn(
                name: "InvoiceNumber",
                table: "Invoices",
                newName: "NumeroInvoice");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "Invoices",
                newName: "FechaInvoice");

            migrationBuilder.RenameColumn(
                name: "IncludedVAT",
                table: "Invoices",
                newName: "ImporteIVA");

            migrationBuilder.RenameColumn(
                name: "IIBBPerceptionAmount",
                table: "Invoices",
                newName: "IVAContenido");

            migrationBuilder.RenameColumn(
                name: "IIBBPercent",
                table: "Invoices",
                newName: "PorcentajeDescuento");

            migrationBuilder.RenameColumn(
                name: "DiscountPercent",
                table: "Invoices",
                newName: "AlicuotaIIBB");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Invoices",
                newName: "ImporteDescuento");

            migrationBuilder.RenameColumn(
                name: "Clarification",
                table: "Invoices",
                newName: "Aclaracion");

            migrationBuilder.RenameColumn(
                name: "CAEExpirationDate",
                table: "Invoices",
                newName: "FechaVencimientoCAE");

            migrationBuilder.RenameColumn(
                name: "BusinessUnit",
                table: "Invoices",
                newName: "CondicionVenta");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_InvoiceType_PointOfSale_InvoiceNumber",
                table: "Invoices",
                newName: "IX_Invoices_TipoInvoice_PuntoVenta_NumeroInvoice");

            migrationBuilder.RenameColumn(
                name: "VATPercent",
                table: "InvoiceDetails",
                newName: "PorcentajeIVA");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "InvoiceDetails",
                newName: "PrecioUnitario");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "InvoiceDetails",
                newName: "Cantidad");

            migrationBuilder.RenameColumn(
                name: "ItemNumber",
                table: "InvoiceDetails",
                newName: "ItemNumero");

            migrationBuilder.RenameColumn(
                name: "DiscountPercent",
                table: "InvoiceDetails",
                newName: "PorcentajeDescuento");

            migrationBuilder.RenameColumn(
                name: "PointOfSale",
                table: "DeliveryNotes",
                newName: "PuntoVenta");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "DeliveryNotes",
                newName: "Observaciones");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "DeliveryNotes",
                newName: "Facturado");

            migrationBuilder.RenameColumn(
                name: "IsInvoiced",
                table: "DeliveryNotes",
                newName: "Anulado");

            migrationBuilder.RenameColumn(
                name: "InvoiceType",
                table: "DeliveryNotes",
                newName: "TipoFactura");

            migrationBuilder.RenameColumn(
                name: "DeliveryNoteNumber",
                table: "DeliveryNotes",
                newName: "NumeroDeliveryNote");

            migrationBuilder.RenameColumn(
                name: "DeliveryNoteDate",
                table: "DeliveryNotes",
                newName: "FechaDeliveryNote");

            migrationBuilder.RenameColumn(
                name: "DeliveryCity",
                table: "DeliveryNotes",
                newName: "LocalidadEntrega");

            migrationBuilder.RenameColumn(
                name: "DeliveryAddress",
                table: "DeliveryNotes",
                newName: "DireccionEntrega");

            migrationBuilder.RenameColumn(
                name: "Clarification",
                table: "DeliveryNotes",
                newName: "Aclaracion");

            migrationBuilder.RenameColumn(
                name: "BusinessUnit",
                table: "DeliveryNotes",
                newName: "UnidadNegocio");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryNotes_BranchId_DeliveryNoteNumber",
                table: "DeliveryNotes",
                newName: "IX_DeliveryNotes_BranchId_NumeroDeliveryNote");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "DeliveryNoteDetails",
                newName: "Cantidad");

            migrationBuilder.RenameColumn(
                name: "ItemNumber",
                table: "DeliveryNoteDetails",
                newName: "ItemNumero");

            migrationBuilder.RenameColumn(
                name: "TradeName",
                table: "Customers",
                newName: "NombreFantasia");

            migrationBuilder.RenameColumn(
                name: "Province",
                table: "Customers",
                newName: "Provincia");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "Customers",
                newName: "ProvinciaPadronIIBB");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Customers",
                newName: "Telefono");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Customers",
                newName: "Observaciones");

            migrationBuilder.RenameColumn(
                name: "Mobile",
                table: "Customers",
                newName: "Celular");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Customers",
                newName: "Activo");

            migrationBuilder.RenameColumn(
                name: "IIBBRegistryProvince",
                table: "Customers",
                newName: "CodigoPostal");

            migrationBuilder.RenameColumn(
                name: "IIBBPercent",
                table: "Customers",
                newName: "PorcentajeDescuento");

            migrationBuilder.RenameColumn(
                name: "DiscountPercent",
                table: "Customers",
                newName: "AlicuotaIIBB");

            migrationBuilder.RenameColumn(
                name: "CreditLimit",
                table: "Customers",
                newName: "LimiteCredito");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Customers",
                newName: "FechaAlta");

            migrationBuilder.RenameColumn(
                name: "CompanyName",
                table: "Customers",
                newName: "RazonSocial");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Customers",
                newName: "Localidad");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Customers",
                newName: "Direccion");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "UnidadesMedida",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "UnidadesMedida",
                newName: "Codigo");

            migrationBuilder.RenameColumn(
                name: "InvoiceType",
                table: "CondicionesIva",
                newName: "TipoInvoice");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "CondicionesIva",
                newName: "Descripcion");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "CondicionesIva",
                newName: "Codigo");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ZonasVenta",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "ZonasVenta",
                newName: "Activa");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ZonasVenta",
                newName: "Descripcion");

            migrationBuilder.RenameColumn(
                name: "Province",
                table: "SalesRepes",
                newName: "Provincia");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "SalesRepes",
                newName: "CodigoPostal");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "SalesRepes",
                newName: "Telefono");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "SalesRepes",
                newName: "Observaciones");

            migrationBuilder.RenameColumn(
                name: "Mobile",
                table: "SalesRepes",
                newName: "Celular");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "SalesRepes",
                newName: "Localidad");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "SalesRepes",
                newName: "Activo");

            migrationBuilder.RenameColumn(
                name: "HireDate",
                table: "SalesRepes",
                newName: "FechaNacimiento");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "SalesRepes",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "EmployeeCode",
                table: "SalesRepes",
                newName: "Legajo");

            migrationBuilder.RenameColumn(
                name: "DateOfBirth",
                table: "SalesRepes",
                newName: "FechaIngreso");

            migrationBuilder.RenameColumn(
                name: "CommissionPercent",
                table: "SalesRepes",
                newName: "PorcentajeComision");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "SalesRepes",
                newName: "Apellido");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "SalesRepes",
                newName: "Domicilio");

            migrationBuilder.RenameIndex(
                name: "IX_SalesReps_EmployeeCode",
                table: "SalesRepes",
                newName: "IX_SalesRepes_Legajo");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Categorys",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Categorys",
                newName: "Activo");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Categorys",
                newName: "Descripcion");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnidadesMedida",
                table: "UnidadesMedida",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CondicionesIva",
                table: "CondicionesIva",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ZonasVenta",
                table: "ZonasVenta",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesRepes",
                table: "SalesRepes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categorys",
                table: "Categorys",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Calle (SalesRepes)");

            migrationBuilder.AddForeignKey(
                name: "FK_CasualDeliveryNotes_SalesRepes_SalesRepId",
                table: "CasualDeliveryNotes",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Consignments_SalesRepes_SalesRepId",
                table: "Consignments",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditNotes_SalesRepes_SalesRepId",
                table: "CreditNotes",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_CondicionesIva_TaxConditionId",
                table: "Customers",
                column: "TaxConditionId",
                principalTable: "CondicionesIva",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_SalesRepes_SalesRepId",
                table: "Customers",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_ZonasVenta_SalesZoneId",
                table: "Customers",
                column: "SalesZoneId",
                principalTable: "ZonasVenta",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DebitNotes_SalesRepes_SalesRepId",
                table: "DebitNotes",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_SalesRepes_SalesRepId",
                table: "DeliveryNotes",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InternalDebitNotes_SalesRepes_SalesRepId",
                table: "InternalDebitNotes",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_SalesRepes_SalesRepId",
                table: "Invoices",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categorys_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categorys",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_UnidadesMedida_UnitOfMeasureId",
                table: "Products",
                column: "UnitOfMeasureId",
                principalTable: "UnidadesMedida",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_SalesRepes_SalesRepId",
                table: "Quotes",
                column: "SalesRepId",
                principalTable: "SalesRepes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_SalesRepes_SalesRepAsociadoId",
                table: "Warehouses",
                column: "SalesRepAsociadoId",
                principalTable: "SalesRepes",
                principalColumn: "Id");
        }
    }
}
