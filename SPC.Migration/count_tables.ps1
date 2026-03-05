$cn = New-Object System.Data.SqlClient.SqlConnection('Server=(localdb)\MSSQLLocalDB;Database=SPC;Integrated Security=true;TrustServerCertificate=True')
$cn.Open()
$cmd = $cn.CreateCommand()
$cmd.CommandText = @"
SELECT 'Clientes' Name, COUNT(*) Cnt FROM Clientes
UNION ALL SELECT 'Productos', COUNT(*) FROM Productos
UNION ALL SELECT 'FacturaC', COUNT(*) FROM Facturas
UNION ALL SELECT 'FacturaD', COUNT(*) FROM FacturaDetalles
UNION ALL SELECT 'RemitoC', COUNT(*) FROM Remitos
UNION ALL SELECT 'RemitoD', COUNT(*) FROM RemitoDetalles
UNION ALL SELECT 'PresupuestoC', COUNT(*) FROM Quotes
UNION ALL SELECT 'PresupuestoD', COUNT(*) FROM QuoteDetails
UNION ALL SELECT 'NotaCreditoC', COUNT(*) FROM CreditNotes
UNION ALL SELECT 'NotaCreditoD', COUNT(*) FROM CreditNoteDetails
UNION ALL SELECT 'NotaDebitoC', COUNT(*) FROM DebitNotes
UNION ALL SELECT 'NotaDebitoD', COUNT(*) FROM DebitNoteDetails
UNION ALL SELECT 'NotaDebitoIC', COUNT(*) FROM InternalDebitNotes
UNION ALL SELECT 'NotaDebitoID', COUNT(*) FROM InternalDebitNoteDetails
UNION ALL SELECT 'ConsignacionesC', COUNT(*) FROM Consignments
UNION ALL SELECT 'ConsignacionesD', COUNT(*) FROM ConsignmentDetails
UNION ALL SELECT 'PagoC', COUNT(*) FROM Payments
UNION ALL SELECT 'PagoD', COUNT(*) FROM PaymentDetails
UNION ALL SELECT 'CtaCte', COUNT(*) FROM CurrentAccounts
UNION ALL SELECT 'MovimientosCtaCte', COUNT(*) FROM CurrentAccountMovements
"@
$r = $cmd.ExecuteReader()
while ($r.Read()) {
  Write-Output ($r.GetString(0) + ':' + $r.GetInt32(1))
}
$r.Close()
$cn.Close()
