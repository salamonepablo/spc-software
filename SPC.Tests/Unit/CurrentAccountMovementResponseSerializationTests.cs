using System.Text.Json;
using FluentAssertions;
using SPC.API.Contracts.CurrentAccount;

namespace SPC.Tests.Unit;

public class CurrentAccountMovementResponseSerializationTests
{
    [Fact]
    public void DeserializeMovementResponse_WithNavigationMetadata_MapsExpectedFields()
    {
        // Arrange
        const string json = """
        {
          "id": 1,
          "movementDate": "2026-03-28T00:00:00",
          "documentType": "Factura A",
          "documentTypeCode": 101,
          "documentTypeShortCode": "FA",
          "documentTypeLabel": "Factura A",
          "documentTypeTooltip": "Invoice A",
          "documentNumber": 12345,
          "billingAmount": 1000,
          "budgetAmount": 0,
          "billingRunningBalance": 1000,
          "budgetRunningBalance": 0,
          "description": "Factura de prueba",
          "navigation": {
            "targetType": "document",
            "targetKind": "invoice",
            "targetRoute": "/invoices?search=12345",
            "targetId": "12345",
            "canOpen": true,
            "disabledReason": null
          }
        }
        """;

        // Act
        var movement = JsonSerializer.Deserialize<CurrentAccountMovementResponse>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert
        movement.Should().NotBeNull();
        movement!.DocumentTypeShortCode.Should().Be("FA");
        movement.DocumentTypeLabel.Should().Be("Factura A");
        movement.DocumentTypeTooltip.Should().Be("Invoice A");
        movement!.Navigation.TargetType.Should().Be("document");
        movement.Navigation.TargetKind.Should().Be("invoice");
        movement.Navigation.TargetRoute.Should().Be("/invoices?search=12345");
        movement.Navigation.TargetId.Should().Be("12345");
        movement.Navigation.CanOpen.Should().BeTrue();
    }

    [Fact]
    public void DeserializeMovementResponse_WithoutNavigationMetadata_UsesSafeDefaults()
    {
        // Arrange
        const string legacyJson = """
        {
          "id": 2,
          "movementDate": "2026-03-28T00:00:00",
          "documentType": "Pago",
          "documentTypeCode": 30,
          "documentNumber": 7001,
          "billingAmount": -250,
          "budgetAmount": 0,
          "billingRunningBalance": 750,
          "budgetRunningBalance": 0,
          "description": "Pago"
        }
        """;

        // Act
        var movement = JsonSerializer.Deserialize<CurrentAccountMovementResponse>(
            legacyJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert
        movement.Should().NotBeNull();
        movement!.DocumentTypeShortCode.Should().Be("OT");
        movement.Navigation.Should().NotBeNull();
        movement.Navigation.TargetType.Should().Be("other");
        movement.Navigation.TargetKind.Should().Be("other");
        movement.Navigation.CanOpen.Should().BeFalse();
    }
}
