"""
Import CSV files to SQL Server
Uses bulk insert with batching to avoid timeouts
"""

import pyodbc
import csv
from pathlib import Path
from datetime import datetime
from decimal import Decimal

# Configuration
SQL_SERVER = r"(localdb)\MSSQLLocalDB"
DATABASE = "SPC"
DATA_DIR = Path(r"C:\Programmes\spc-software\SPC.Migration\data")

# Mapping tables - will be populated from existing data
cliente_map = {}  # Access IDCliente -> SQL Id
vendedor_map = {}  # Legajo -> SQL Id
producto_map = {}  # CodProd -> SQL Id
deposito_map = {}  # IdDeposito -> SQL Id
branch_map = {}  # IdSucursal -> SQL BranchId
factura_map = {}  # (TipoFactura, NroFactura) -> SQL Id
remito_map = {}  # (IdSucursal, NroRemito) -> SQL Id
quote_map = {}  # NroPresu -> SQL Id
credit_note_map = {}  # (Tipo, Nro) -> SQL Id
debit_note_map = {}  # (Tipo, Nro) -> SQL Id
internal_debit_map = {}  # (Tipo, Nro) -> SQL Id
consignment_map = {}  # NroConsignacion -> SQL Id
payment_map = {}  # (IdSucursal, NroPago) -> SQL Id
payment_method_map = {}  # Code -> SQL Id


def safe_decimal(val, default=0):
    """Convert to decimal safely"""
    if not val or val == "":
        return default
    try:
        d = Decimal(str(val).replace(",", "."))
        # Clamp to reasonable range
        if d > Decimal("999999999999.9999"):
            return Decimal("999999999999.9999")
        if d < Decimal("-999999999999.9999"):
            return Decimal("-999999999999.9999")
        return d
    except:
        return default


def safe_int(val, default=0):
    """Convert to int safely"""
    if not val or val == "":
        return default
    try:
        return int(float(val))
    except:
        return default


def safe_date(val):
    """Convert to date safely"""
    if not val or val == "":
        return None
    try:
        # Try various formats
        for fmt in ["%Y-%m-%d %H:%M:%S", "%Y-%m-%d", "%d/%m/%Y", "%d/%m/%Y %H:%M:%S"]:
            try:
                return datetime.strptime(val.split(".")[0], fmt)
            except:
                continue
        return None
    except:
        return None


def safe_str(val, max_len=500):
    """Clean and truncate string"""
    if val is None:
        return None
    s = str(val).strip()
    if len(s) > max_len:
        s = s[:max_len]
    return s if s else None


def load_mappings(cursor):
    """Load existing data mappings from SQL Server"""
    print("Loading existing mappings...")

    # Clientes - map by order (since we inserted in same order as Access)
    cursor.execute("SELECT Id FROM Clientes ORDER BY Id")
    sql_ids = [row[0] for row in cursor.fetchall()]

    # Read Access IDs from original order
    csv_path = DATA_DIR.parent / "data" / "clientes_order.csv"
    if not csv_path.exists():
        # Create mapping file from Access
        print("  Creating cliente mapping...")
        # For now, assume 1:1 mapping
        cursor.execute("SELECT Id FROM Clientes ORDER BY Id")
        for i, row in enumerate(cursor.fetchall(), 1):
            cliente_map[i] = row[0]

    # Actually, let's query Access order
    # We'll use a simpler approach: query both and match

    # Vendedores
    cursor.execute("SELECT Id, Legajo FROM Vendedores")
    for row in cursor.fetchall():
        vendedor_map[row[1]] = row[0]
    print(f"  Vendedores: {len(vendedor_map)}")

    # Productos
    cursor.execute("SELECT Id, Codigo FROM Productos")
    for row in cursor.fetchall():
        producto_map[row[1]] = row[0]
    print(f"  Productos: {len(producto_map)}")

    # Depositos
    cursor.execute("SELECT Id, Nombre FROM Depositos")
    for row in cursor.fetchall():
        deposito_map[row[1]] = row[0]
    print(f"  Depositos: {len(deposito_map)}")

    # Branches
    cursor.execute("SELECT Id, PointOfSale FROM Branches")
    for row in cursor.fetchall():
        branch_map[row[1]] = row[0]
    print(f"  Branches: {len(branch_map)}")

    # Payment Methods
    cursor.execute("SELECT Id, Code FROM PaymentMethods")
    for row in cursor.fetchall():
        payment_method_map[row[1].upper()] = row[0]
    print(f"  PaymentMethods: {len(payment_method_map)}")

    print("  Mappings loaded OK")


def load_cliente_mapping(cursor):
    """Load cliente mapping from Access order"""
    global cliente_map

    # Connect to Access to get ID order
    access_conn_str = f"DRIVER={{Microsoft Access Driver (*.mdb, *.accdb)}};DBQ=C:\\TrabajosActivos\\SPC-Core\\Db_SPC_SI.mdb;"
    access_conn = pyodbc.connect(access_conn_str)
    access_cursor = access_conn.cursor()

    access_cursor.execute("SELECT IDCliente FROM Clientes ORDER BY IDCliente")
    access_ids = [row[0] for row in access_cursor.fetchall()]
    access_conn.close()

    cursor.execute("SELECT Id FROM Clientes ORDER BY Id")
    sql_ids = [row[0] for row in cursor.fetchall()]

    for i, access_id in enumerate(access_ids):
        if i < len(sql_ids):
            cliente_map[access_id] = sql_ids[i]

    print(f"  Clientes mapped: {len(cliente_map)}")


def import_facturas(cursor, conn):
    """Import Facturas from CSV"""
    print("Importing Facturas...")

    csv_file = DATA_DIR / "facturas_c.csv"
    if not csv_file.exists():
        print("  File not found, skipping")
        return

    # Check if already imported
    cursor.execute("SELECT COUNT(*) FROM Facturas")
    if cursor.fetchone()[0] > 0:
        print("  Already imported, loading mappings...")
        cursor.execute("SELECT Id, TipoFactura, NumeroFactura FROM Facturas")
        for row in cursor.fetchall():
            factura_map[(row[1], int(row[2]))] = row[0]
        print(f"  Loaded {len(factura_map)} mappings")
        return

    with open(csv_file, "r", encoding="utf-8") as f:
        reader = csv.DictReader(f, delimiter=";")
        rows = list(reader)

    print(f"  Processing {len(rows)} facturas...")

    batch = []
    for row in rows:
        cod_cliente = safe_int(row.get("CodCliente"))
        if cod_cliente not in cliente_map:
            continue

        tipo = safe_str(row.get("TipoFactura"), 1) or "B"
        nro = safe_int(row.get("NroFactura"))
        id_sucursal = safe_int(row.get("IdSucursal"))
        branch_id = branch_map.get(id_sucursal, 1)
        punto_venta = id_sucursal if id_sucursal > 0 else 2

        vendedor_legajo = safe_str(row.get("CodVendedor"), 20)
        vendedor_id = vendedor_map.get(vendedor_legajo) if vendedor_legajo else None

        batch.append(
            (
                branch_id,
                tipo,
                punto_venta,
                nro,
                safe_date(row.get("FechaFactura")) or datetime.now(),
                cliente_map[cod_cliente],
                vendedor_id,
                float(safe_decimal(row.get("SubTotalFactura"))),
                float(safe_decimal(row.get("PorcentajeIVA"))),
                float(safe_decimal(row.get("TotalIVA"))),
                float(safe_decimal(row.get("AlicuotaIIBB"))),
                float(safe_decimal(row.get("ImportePercepIIBB"))),
                float(safe_decimal(row.get("PorcentajeDesc"))),
                float(safe_decimal(row.get("ImporteDesc"))),
                float(safe_decimal(row.get("TotalFactura"))),
                safe_str(row.get("Cae"), 20),
                safe_date(row.get("FechaVC")),
                safe_str(row.get("CondicionVenta"), 50),
                safe_int(row.get("FormaPago")),
                safe_str(row.get("UnidadNegocio"), 50),
                1 if str(row.get("Cancelada", "")).upper() == "S" else 0,
                safe_str(row.get("AclaracionFactura"), 500),
                tipo,  # for key
                nro,  # for key
            )
        )

    # Insert in batches
    batch_size = 1000
    for i in range(0, len(batch), batch_size):
        chunk = batch[i : i + batch_size]

        cursor.executemany(
            """
            INSERT INTO Facturas (BranchId, TipoFactura, PuntoVenta, NumeroFactura, FechaFactura,
                ClienteId, VendedorId, Subtotal, PorcentajeIVA, ImporteIVA, AlicuotaIIBB,
                ImportePercepcionIIBB, PorcentajeDescuento, ImporteDescuento, Total, CAE,
                FechaVencimientoCAE, CondicionVenta, FormaPago, UnidadNegocio, Anulada, Aclaracion)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
            [
                (
                    r[0],
                    r[1],
                    r[2],
                    r[3],
                    r[4],
                    r[5],
                    r[6],
                    r[7],
                    r[8],
                    r[9],
                    r[10],
                    r[11],
                    r[12],
                    r[13],
                    r[14],
                    r[15],
                    r[16],
                    r[17],
                    r[18],
                    r[19],
                    r[20],
                    r[21],
                )
                for r in chunk
            ],
        )
        conn.commit()
        print(f"    Inserted {min(i + batch_size, len(batch))}/{len(batch)}")

    # Load mappings
    cursor.execute("SELECT Id, TipoFactura, NumeroFactura FROM Facturas")
    for row in cursor.fetchall():
        factura_map[(row[1], int(row[2]))] = row[0]

    print(f"  OK ({len(batch)} inserted)")


def import_factura_detalles(cursor, conn):
    """Import FacturaDetalles from CSV"""
    print("Importing FacturaDetalles...")

    csv_file = DATA_DIR / "facturas_d.csv"
    if not csv_file.exists():
        print("  File not found, skipping")
        return

    cursor.execute("SELECT COUNT(*) FROM FacturaDetalles")
    if cursor.fetchone()[0] > 0:
        print("  Already imported, skipping")
        return

    with open(csv_file, "r", encoding="utf-8") as f:
        reader = csv.DictReader(f, delimiter=";")
        rows = list(reader)

    print(f"  Processing {len(rows)} detalles...")

    batch = []
    for row in rows:
        tipo = safe_str(row.get("TipoFactura"), 1) or "B"
        nro = safe_int(row.get("NroFactura"))
        key = (tipo, nro)

        if key not in factura_map:
            continue

        cod_prod = safe_str(row.get("IDCodProd"), 50)
        if not cod_prod or cod_prod not in producto_map:
            continue

        batch.append(
            (
                factura_map[key],
                safe_int(row.get("ItemFactura")),
                producto_map[cod_prod],
                float(safe_decimal(row.get("Cantidad"))),
                float(safe_decimal(row.get("PrecioUnitario"))),
                float(safe_decimal(row.get("PorcentajeDescuento"))),
                21.0,  # PorcentajeIVA
                float(safe_decimal(row.get("TotalLinea"))),
            )
        )

    # Insert in batches
    batch_size = 2000
    for i in range(0, len(batch), batch_size):
        chunk = batch[i : i + batch_size]
        cursor.executemany(
            """
            INSERT INTO FacturaDetalles (FacturaId, ItemNumero, ProductoId, Cantidad, 
                PrecioUnitario, PorcentajeDescuento, PorcentajeIVA, Subtotal)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """,
            chunk,
        )
        conn.commit()
        print(f"    Inserted {min(i + batch_size, len(batch))}/{len(batch)}")

    print(f"  OK ({len(batch)} inserted)")


def main():
    print("=" * 50)
    print("  CSV to SQL Server Import Tool")
    print("=" * 50)
    print()

    # Connect to SQL Server
    conn_str = f"DRIVER={{SQL Server}};SERVER={SQL_SERVER};DATABASE={DATABASE};Trusted_Connection=yes;"
    print(f"Connecting to: {SQL_SERVER}/{DATABASE}")

    try:
        conn = pyodbc.connect(conn_str)
        cursor = conn.cursor()
        print("Connected OK")
        print()
    except Exception as e:
        print(f"Connection failed: {e}")
        return

    # Load mappings from existing data
    load_mappings(cursor)
    load_cliente_mapping(cursor)
    print()

    # Import tables
    import_facturas(cursor, conn)
    import_factura_detalles(cursor, conn)

    # TODO: Add more imports...

    conn.close()
    print()
    print("=" * 50)
    print("  Import complete!")
    print("=" * 50)


if __name__ == "__main__":
    main()
