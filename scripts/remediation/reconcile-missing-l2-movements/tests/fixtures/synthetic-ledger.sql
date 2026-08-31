-- SYNTHETIC TEST DATA ONLY. Never run against an operational database.
-- Disposable SQL Server fixture mirroring only the application columns used by PR2.
CREATE TABLE dbo.CurrentAccountMovements (
    Id int NOT NULL PRIMARY KEY,
    MovementDate datetime2 NOT NULL,
    CustomerId int NOT NULL,
    DocumentType int NOT NULL, -- SPC.Shared.Models.DocumentType.Quote = 20 (PR)
    DocumentNumber bigint NOT NULL,
    BillingAmount decimal(18,2) NOT NULL,
    BudgetAmount decimal(18,2) NOT NULL,
    BillingRunningBalance decimal(18,2) NOT NULL,
    BudgetRunningBalance decimal(18,2) NOT NULL,
    Description nvarchar(200) NULL
);
CREATE TABLE dbo.CurrentAccounts (
    Id int NOT NULL PRIMARY KEY,
    CustomerId int NOT NULL UNIQUE,
    BillingBalance decimal(18,2) NOT NULL,
    BudgetBalance decimal(18,2) NOT NULL,
    TotalBalance decimal(18,2) NOT NULL,
    LastUpdated datetime2 NOT NULL
);
CREATE TABLE dbo.Quotes (
    Id int NOT NULL PRIMARY KEY,
    BranchId int NOT NULL,
    QuoteNumber bigint NOT NULL,
    QuoteDate datetime2 NOT NULL,
    CustomerId int NOT NULL,
    Total decimal(18,2) NOT NULL,
    IsVoided bit NOT NULL
);

-- Four scoped synthetic customers, one intentionally non-scoped account, nine zero-L2 PR targets.
INSERT dbo.CurrentAccounts VALUES
 (1, 101, 100.00, 0.00, 100.00, '2026-01-01'), (2, 102, 200.00, 0.00, 200.00, '2026-01-01'),
 (3, 103, 300.00, 0.00, 300.00, '2026-01-01'), (4, 104, 400.00, 0.00, 400.00, '2026-01-01'),
 (5, 105, 500.00, 77.00, 577.00, '2026-01-01');
INSERT dbo.CurrentAccountMovements VALUES
 (1, '2026-01-01',101,20,1001,10.00,0.00,10.00,0.00,N'synthetic target 1'),
 (2, '2026-01-01',101,20,1002,20.00,0.00,30.00,0.00,N'synthetic target 2'),
 (3, '2026-01-01',101,20,1003,30.00,0.00,60.00,0.00,N'synthetic target 3'),
 (4, '2026-01-01',102,20,1004,40.00,0.00,40.00,0.00,N'synthetic target 4'),
 (5, '2026-01-01',102,20,1005,50.00,0.00,90.00,0.00,N'synthetic target 5'),
 (6, '2026-01-01',103,20,1006,60.00,0.00,60.00,0.00,N'synthetic target 6'),
 (7, '2026-01-01',103,20,1007,70.00,0.00,130.00,0.00,N'synthetic target 7'),
 (8, '2026-01-01',104,20,1008,80.00,0.00,80.00,0.00,N'synthetic target 8'),
 (9, '2026-01-01',104,20,1009,90.00,0.00,170.00,0.00,N'synthetic target 9'),
 (10,'2026-01-01',101,99,9001,11.00,12.00,71.00,12.00,N'synthetic non-target'),
 (11,'2026-01-01',105,20,9002,13.00,14.00,13.00,14.00,N'synthetic non-scoped');
INSERT dbo.Quotes VALUES
 (201,1,1001,'2026-01-01',101,11.00,0),(202,1,1002,'2026-01-01',101,12.00,0),(203,1,1003,'2026-01-01',101,13.00,0),
 (204,1,1004,'2026-01-01',102,14.00,0),(205,1,1005,'2026-01-01',102,15.00,0),(206,1,1006,'2026-01-01',103,16.00,0),
 (207,1,1007,'2026-01-01',103,17.00,0),(208,1,1008,'2026-01-01',104,18.00,0),(209,1,1009,'2026-01-01',104,19.00,0);
-- Harness variants mutate only this disposable fixture to prove each guarded rollback path.
