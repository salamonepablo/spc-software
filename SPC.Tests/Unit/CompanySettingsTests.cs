using FluentAssertions;
using SPC.Shared.Models;

namespace SPC.Tests.Unit;

/// <summary>
/// TDD Tests for Company Settings (withholding/perception agent flags).
/// 
/// Business Rules:
/// - Company can be IVA Withholding Agent (Agente de Retención IVA)
/// - Company can be IIBB Perception Agent (Agente de Percepción IIBB)
/// - These are independent flags
/// - IIBB perception only calculated if company is perception agent
/// </summary>
public class CompanySettingsTests
{
    [Fact]
    public void CompanySettings_HasIIBBPerceptionAgentFlag()
    {
        // Arrange & Act
        var settings = new CompanySettings
        {
            IsIIBBPerceptionAgent = true
        };

        // Assert
        settings.IsIIBBPerceptionAgent.Should().BeTrue();
    }

    [Fact]
    public void CompanySettings_HasIVAWithholdingAgentFlag()
    {
        // Arrange & Act
        var settings = new CompanySettings
        {
            IsIVAWithholdingAgent = true
        };

        // Assert
        settings.IsIVAWithholdingAgent.Should().BeTrue();
    }

    [Fact]
    public void CompanySettings_DefaultsToNotAgents()
    {
        // Arrange & Act
        var settings = new CompanySettings();

        // Assert
        settings.IsIIBBPerceptionAgent.Should().BeFalse();
        settings.IsIVAWithholdingAgent.Should().BeFalse();
    }

    [Fact]
    public void CompanySettings_CanBeBothAgents()
    {
        // Arrange & Act
        var settings = new CompanySettings
        {
            IsIVAWithholdingAgent = true,
            IsIIBBPerceptionAgent = true
        };

        // Assert
        settings.IsIVAWithholdingAgent.Should().BeTrue();
        settings.IsIIBBPerceptionAgent.Should().BeTrue();
    }
}
