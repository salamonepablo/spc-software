# SPC - Sistema de Gestión Comercial

## Contexto del Proyecto
Sistema de gestión comercial para empresa de baterías, migrado desde Visual Basic 6.0 + Access a tecnologías modernas.

## Stack Tecnológico
- **Backend**: ASP.NET Core 10 (Minimal APIs)
- **Frontend**: Blazor Server
- **Base de Datos**: SQLite (desarrollo) / SQL Server (producción)
- **ORM**: Entity Framework Core 10

## Estructura de la Solución
```
spc-software/
├── SPC.API/           # API REST
├── SPC.Shared/        # Modelos compartidos
├── SPC.Web/           # Frontend Blazor
└── docs/              # Documentación BD Access original
```

## Base de Datos Original (Access)
La documentación de la BD Access está en `/docs/`:
- `doc_DB_SPC_SI_Tables.txt` - Estructura de tablas
- `doc_DB_SPC_SI_Queries.txt` - Consultas existentes

### Tablas Principales
| Categoría | Tablas |
|-----------|--------|
| Clientes | Clientes, DomiciliosClientes, CtaCte |
| Productos | Productos, Stock, Rubros, UnidadesMedida, Depositos |
| Facturación | FacturaC/D, PresupuestoC/D, RemitoC/D |
| Notas | NotaCreditoC/D, NotaDebitoC/D |
| Pagos | PagoC/D, RecibosC/D, CodPago |
| Stock | MovIntStockC/D, MovimientosCtaCte |
| Auxiliares | Paises, Provincias, Localidades, CondicionIva |

## Convenciones de Código
- Usar español para nombres de entidades de negocio
- Documentar métodos públicos con XML comments
- Soft delete (campo Activo) en lugar de eliminación física
- Patrón Cabecera/Detalle para documentos (Factura/FacturaDetalle)

## Comandos Útiles
```bash
# Ejecutar API
cd SPC.API && dotnet run

# Ejecutar Web
cd SPC.Web && dotnet run

# Restaurar paquetes
dotnet restore

# Compilar toda la solución
dotnet build
```

## Endpoints API Existentes
- `GET /api/clientes` - Listar clientes
- `GET /api/clientes/{id}` - Obtener cliente
- `GET /api/clientes/buscar?nombre=xxx` - Buscar
- `POST /api/clientes` - Crear
- `PUT /api/clientes/{id}` - Actualizar
- `DELETE /api/clientes/{id}` - Soft delete
- `GET /api/productos`, `GET /api/vendedores`, etc.

## Pendientes
- [ ] Completar modelos: Presupuesto, NotaCredito, Pagos, CtaCte
- [ ] UI Blazor para gestión de Clientes y Productos
- [ ] Endpoints de Facturas con lógica de stock
- [ ] Autenticación y autorización
- [ ] Migración de datos desde Access
