using System.ComponentModel.DataAnnotations;

namespace SPC.Shared.Models;

/// <summary>
/// Saldo de cuenta corriente del cliente.
/// Almacena los saldos actuales de ambas lineas.
/// </summary>
public class CurrentAccount
{
    public int Id { get; set; }
    
    /// <summary>Cliente (PK en Access es IDCliente)</summary>
    public int CustomerId { get; set; }
    public Cliente? Customer { get; set; }
    
    /// <summary>Saldo Linea 1 (Billing: Facturas, NC, ND, Pagos L1)</summary>
    public decimal BillingBalance { get; set; } = 0;
    
    /// <summary>Saldo Linea 2 (Budget: Presupuestos, Pagos L2)</summary>
    public decimal BudgetBalance { get; set; } = 0;
    
    /// <summary>Saldo Total (L1 + L2)</summary>
    public decimal TotalBalance { get; set; } = 0;
    
    /// <summary>Fecha de ultima actualizacion</summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Movimiento de cuenta corriente.
/// Registro historico de cada operacion que afecta el saldo.
/// </summary>
public class CurrentAccountMovement
{
    public int Id { get; set; }
    
    /// <summary>Fecha del movimiento</summary>
    public DateTime MovementDate { get; set; }
    
    /// <summary>Cliente</summary>
    public int CustomerId { get; set; }
    public Cliente? Customer { get; set; }
    
    /// <summary>Tipo de documento</summary>
    public DocumentType DocumentType { get; set; }
    
    /// <summary>Numero de documento</summary>
    public long DocumentNumber { get; set; }
    
    /// <summary>Importe Linea 1 (Billing) - positivo o negativo</summary>
    public decimal BillingAmount { get; set; } = 0;
    
    /// <summary>Importe Linea 2 (Budget) - positivo o negativo</summary>
    public decimal BudgetAmount { get; set; } = 0;
    
    /// <summary>Saldo parcial Billing despues del movimiento</summary>
    public decimal BillingRunningBalance { get; set; } = 0;
    
    /// <summary>Saldo parcial Budget despues del movimiento</summary>
    public decimal BudgetRunningBalance { get; set; } = 0;
    
    /// <summary>Descripcion adicional</summary>
    [StringLength(200)]
    public string? Description { get; set; }
}
