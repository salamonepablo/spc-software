"""
Export Access database tables to CSV files for SQL Server import
"""

import pyodbc
import csv
import os
from pathlib import Path

# Configuration
ACCESS_DB = r"C:\TrabajosActivos\SPC-Core\Db_SPC_SI.mdb"
OUTPUT_DIR = Path(r"C:\Programmes\spc-software\SPC.Migration\data")

# Tables to export (in order of dependencies)
TABLES = [
    # Documents (the big ones that timeout)
    ("FacturaC", "facturas_c.csv"),
    ("FacturaD", "facturas_d.csv"),
    ("RemitoC", "remitos_c.csv"),
    ("RemitoD", "remitos_d.csv"),
    ("PresupuestoC", "presupuestos_c.csv"),
    ("PresupuestoD", "presupuestos_d.csv"),
    ("NotaCreditoC", "notas_credito_c.csv"),
    ("NotaCreditoD", "notas_credito_d.csv"),
    ("NotaDebitoC", "notas_debito_c.csv"),
    ("NotaDebitoD", "notas_debito_d.csv"),
    ("NotaDebitoIC", "notas_debito_i_c.csv"),
    ("NotaDebitoID", "notas_debito_i_d.csv"),
    ("ConsignacionesC", "consignaciones_c.csv"),
    ("ConsignacionesD", "consignaciones_d.csv"),
    # Payments
    ("PagoC", "pagos_c.csv"),
    ("PagoD", "pagos_d.csv"),
    # Current Account
    ("CtaCte", "cta_cte.csv"),
    ("MovimientosCtaCte", "movimientos_cta_cte.csv"),
]


def export_table(cursor, table_name, output_file):
    """Export a single table to CSV"""
    print(f"  Exporting {table_name}...", end=" ")

    try:
        cursor.execute(f"SELECT * FROM [{table_name}]")
        columns = [column[0] for column in cursor.description]
        rows = cursor.fetchall()

        with open(output_file, "w", newline="", encoding="utf-8") as f:
            writer = csv.writer(f, delimiter=";", quoting=csv.QUOTE_MINIMAL)
            writer.writerow(columns)
            for row in rows:
                # Convert values to strings, handling None and special chars
                clean_row = []
                for val in row:
                    if val is None:
                        clean_row.append("")
                    elif isinstance(val, str):
                        clean_row.append(val.replace("\n", " ").replace("\r", " "))
                    else:
                        clean_row.append(str(val))
                writer.writerow(clean_row)

        print(f"OK ({len(rows)} rows)")
        return len(rows)
    except Exception as e:
        print(f"ERROR: {e}")
        return 0


def main():
    print("=" * 50)
    print("  Access to CSV Export Tool")
    print("=" * 50)
    print()

    # Create output directory
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    # Connect to Access
    conn_str = f"DRIVER={{Microsoft Access Driver (*.mdb, *.accdb)}};DBQ={ACCESS_DB};"
    print(f"Connecting to: {ACCESS_DB}")

    try:
        conn = pyodbc.connect(conn_str)
        cursor = conn.cursor()
        print("Connected OK")
        print()
    except Exception as e:
        print(f"Connection failed: {e}")
        return

    # Export tables
    print("Exporting tables:")
    total_rows = 0

    for table_name, output_file in TABLES:
        output_path = OUTPUT_DIR / output_file
        rows = export_table(cursor, table_name, output_path)
        total_rows += rows

    conn.close()

    print()
    print("=" * 50)
    print(f"  Export complete! {total_rows} total rows")
    print(f"  Files saved to: {OUTPUT_DIR}")
    print("=" * 50)


if __name__ == "__main__":
    main()
