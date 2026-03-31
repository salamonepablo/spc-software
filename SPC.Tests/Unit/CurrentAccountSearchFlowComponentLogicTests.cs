extern alias SPCWEB;

using FluentAssertions;
using Moq;
using System.Reflection;
using CurrentAccountDto = SPCWEB::SPC.Web.Services.Models.CurrentAccountDto;
using CurrentAccountMovementsDto = SPCWEB::SPC.Web.Services.Models.CurrentAccountMovementsDto;
using CustomerDto = SPCWEB::SPC.Web.Services.Models.CustomerDto;
using IApiService = SPCWEB::SPC.Web.Services.IApiService;
using CuentaCorrienteIndex = SPCWEB::SPC.Web.Components.Pages.CuentaCorriente.Index;

namespace SPC.Tests.Unit;

public class CurrentAccountSearchFlowComponentLogicTests
{
    [Fact]
    public async Task HandleCustomerSelected_DoesNotAutoLoadMovements_BeforeBuscar()
    {
        // Arrange
        var api = new Mock<IApiService>();
        api.Setup(x => x.GetCurrentAccountAsync(7))
            .ReturnsAsync(new CurrentAccountDto { CustomerId = 7, CustomerName = "ACME" });

        var component = CreateComponent(api.Object);
        var customer = new CustomerDto { Id = 7, CompanyName = "ACME" };

        // Act
        await InvokePrivateAsync(component, "HandleCustomerSelected", customer);

        // Assert
        api.Verify(x => x.GetCurrentAccountAsync(7), Times.Once);
        api.Verify(x => x.GetCurrentAccountMovementsByRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task SearchMovements_UsesDefaultDateRange_TodayMinusOneYear_ToToday()
    {
        // Arrange
        var api = new Mock<IApiService>();
        api.Setup(x => x.GetCurrentAccountAsync(9))
            .ReturnsAsync(new CurrentAccountDto { CustomerId = 9, CustomerName = "Default Range" });
        api.Setup(x => x.GetCurrentAccountMovementsByRangeAsync(9, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(new CurrentAccountMovementsDto());

        var component = CreateComponent(api.Object);
        InvokePrivate(component, "OnInitialized");
        SetPrivateField(component, "selectedCustomer", new CustomerDto { Id = 9, CompanyName = "Default Range" });

        // Act
        await InvokePrivateAsync(component, "SearchMovements");

        // Assert
        var expectedFrom = DateTime.Today.AddYears(-1);
        var expectedTo = DateTime.Today;

        api.Verify(x => x.GetCurrentAccountMovementsByRangeAsync(
            9,
            It.Is<DateTime>(d => d.Date == expectedFrom.Date),
            It.Is<DateTime>(d => d.Date == expectedTo.Date),
            null), Times.Once);
    }

    [Fact]
    public async Task SearchMovements_UsesEditedDates_WhenUserChangesFilters()
    {
        // Arrange
        var api = new Mock<IApiService>();
        api.Setup(x => x.GetCurrentAccountAsync(12))
            .ReturnsAsync(new CurrentAccountDto { CustomerId = 12, CustomerName = "Manual" });
        api.Setup(x => x.GetCurrentAccountMovementsByRangeAsync(12, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 2))
            .ReturnsAsync(new CurrentAccountMovementsDto());

        var component = CreateComponent(api.Object);
        InvokePrivate(component, "OnInitialized");
        SetPrivateField(component, "selectedCustomer", new CustomerDto { Id = 12, CompanyName = "Manual" });
        SetPrivateField(component, "dateFrom", new DateTime(2025, 02, 10));
        SetPrivateField(component, "dateTo", new DateTime(2025, 03, 15));
        SetPrivateField(component, "lineFilter", "2");

        // Act
        await InvokePrivateAsync(component, "SearchMovements");

        // Assert
        api.Verify(x => x.GetCurrentAccountMovementsByRangeAsync(
            12,
            new DateTime(2025, 02, 10),
            new DateTime(2025, 03, 15),
            2), Times.Once);
    }

    private static CuentaCorrienteIndex CreateComponent(IApiService api)
    {
        var component = new CuentaCorrienteIndex();
        SetProperty(component, "Api", api);
        SetProperty(component, "Navigation", new TestNavigationManager());
        return component;
    }

    private static void InvokePrivate(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, null);
    }

    private static async Task InvokePrivateAsync(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);

        var task = method.Invoke(target, args) as Task;
        task.Should().NotBeNull();
        await task!;
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, fieldName);
        field.SetValue(target, value);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(target.GetType().FullName, propertyName);
        property.SetValue(target, value);
    }

    private sealed class TestNavigationManager : Microsoft.AspNetCore.Components.NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }
}
