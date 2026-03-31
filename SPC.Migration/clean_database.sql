-- =============================================
-- SPC Database Cleanup Script
-- Run this BEFORE reimporting data
-- =============================================

USE SPC;
GO

PRINT 'Starting database cleanup...';
PRINT '';

-- Disable all foreign key constraints
PRINT 'Disabling foreign key constraints...';
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- Delete in reverse dependency order
PRINT 'Deleting MovimientosCtaCte...';
DELETE FROM MovimientosCtaCte;

PRINT 'Deleting CurrentAccounts...';
DELETE FROM CurrentAccounts;

PRINT 'Deleting PaymentDetails...';
DELETE FROM PaymentDetails;

PRINT 'Deleting Payments...';
DELETE FROM Payments;

PRINT 'Deleting ConsignmentDetails...';
DELETE FROM ConsignmentDetails;

PRINT 'Deleting Consignments...';
DELETE FROM Consignments;

PRINT 'Deleting InternalDebitNoteDetails...';
DELETE FROM InternalDebitNoteDetails;

PRINT 'Deleting InternalDebitNotes...';
DELETE FROM InternalDebitNotes;

PRINT 'Deleting DebitNoteDetails...';
DELETE FROM DebitNoteDetails;

PRINT 'Deleting DebitNotes...';
DELETE FROM DebitNotes;

PRINT 'Deleting CreditNoteDetails...';
DELETE FROM CreditNoteDetails;

PRINT 'Deleting CreditNotes...';
DELETE FROM CreditNotes;

PRINT 'Deleting QuoteDetails...';
DELETE FROM QuoteDetails;

PRINT 'Deleting Quotes...';
DELETE FROM Quotes;

PRINT 'Deleting RemitoDetalles...';
DELETE FROM RemitoDetalles;

PRINT 'Deleting Remitos...';
DELETE FROM Remitos;

PRINT 'Deleting FacturaDetalles...';
DELETE FROM FacturaDetalles;

PRINT 'Deleting Facturas...';
DELETE FROM Facturas;

PRINT 'Deleting StockMovementDetails...';
DELETE FROM StockMovementDetails;

PRINT 'Deleting StockMovements...';
DELETE FROM StockMovements;

PRINT 'Deleting Stocks...';
DELETE FROM Stocks;

PRINT 'Deleting CustomerAddresses...';
DELETE FROM CustomerAddresses;

PRINT 'Deleting Clientes...';
DELETE FROM Clientes;

PRINT 'Deleting Productos...';
DELETE FROM Productos;

PRINT 'Deleting Vendedores...';
DELETE FROM Vendedores;

PRINT 'Deleting Depositos...';
DELETE FROM Depositos;

PRINT 'Deleting Branches...';
DELETE FROM Branches;

PRINT 'Deleting ZonasVenta...';
DELETE FROM ZonasVenta;

PRINT 'Deleting PaymentMethods...';
DELETE FROM PaymentMethods;

PRINT 'Deleting Rubros...';
DELETE FROM Rubros;

PRINT 'Deleting UnidadesMedida...';
DELETE FROM UnidadesMedida;

PRINT 'Deleting CondicionesIva...';
DELETE FROM CondicionesIva;

-- Reset identity seeds
PRINT '';
PRINT 'Resetting identity seeds...';

DBCC CHECKIDENT ('Clientes', RESEED, 0);
DBCC CHECKIDENT ('Productos', RESEED, 0);
DBCC CHECKIDENT ('Vendedores', RESEED, 0);
DBCC CHECKIDENT ('Depositos', RESEED, 0);
DBCC CHECKIDENT ('Stocks', RESEED, 0);
DBCC CHECKIDENT ('Facturas', RESEED, 0);
DBCC CHECKIDENT ('FacturaDetalles', RESEED, 0);
DBCC CHECKIDENT ('Remitos', RESEED, 0);
DBCC CHECKIDENT ('RemitoDetalles', RESEED, 0);
DBCC CHECKIDENT ('Quotes', RESEED, 0);
DBCC CHECKIDENT ('QuoteDetails', RESEED, 0);
DBCC CHECKIDENT ('CreditNotes', RESEED, 0);
DBCC CHECKIDENT ('CreditNoteDetails', RESEED, 0);
DBCC CHECKIDENT ('DebitNotes', RESEED, 0);
DBCC CHECKIDENT ('DebitNoteDetails', RESEED, 0);
DBCC CHECKIDENT ('InternalDebitNotes', RESEED, 0);
DBCC CHECKIDENT ('InternalDebitNoteDetails', RESEED, 0);
DBCC CHECKIDENT ('Consignments', RESEED, 0);
DBCC CHECKIDENT ('ConsignmentDetails', RESEED, 0);
DBCC CHECKIDENT ('Payments', RESEED, 0);
DBCC CHECKIDENT ('PaymentDetails', RESEED, 0);
DBCC CHECKIDENT ('CurrentAccounts', RESEED, 0);
DBCC CHECKIDENT ('MovimientosCtaCte', RESEED, 0);
DBCC CHECKIDENT ('CustomerAddresses', RESEED, 0);
DBCC CHECKIDENT ('StockMovements', RESEED, 0);
DBCC CHECKIDENT ('StockMovementDetails', RESEED, 0);
DBCC CHECKIDENT ('Branches', RESEED, 0);
DBCC CHECKIDENT ('ZonasVenta', RESEED, 0);
DBCC CHECKIDENT ('PaymentMethods', RESEED, 0);
DBCC CHECKIDENT ('Rubros', RESEED, 0);
DBCC CHECKIDENT ('UnidadesMedida', RESEED, 0);
DBCC CHECKIDENT ('CondicionesIva', RESEED, 0);

-- Re-enable all foreign key constraints
PRINT '';
PRINT 'Re-enabling foreign key constraints...';
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO

PRINT '';
PRINT '=============================================';
PRINT '  Database cleanup complete!';
PRINT '=============================================';
GO
