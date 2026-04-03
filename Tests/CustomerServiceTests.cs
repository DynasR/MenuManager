using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class CustomerServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new CustomerService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Customer> SeedCustomerAsync(string name = "Alice")
    {
        var customer = new Customer
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PasswordHash = Array.Empty<byte>(),
            PasswordSalt = Array.Empty<byte>()
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCustomers()
    {
        await SeedCustomerAsync("Alice");
        await SeedCustomerAsync("Bob");

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturns_WithCreatedAtPopulated()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await _service.CreateAsync(new CreateCustomerRequest
        {
            Name = "Charlie",
            Email = "charlie@example.com",
            City = "Lyon"
        });

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Charlie");
        result.Email.Should().Be("charlie@example.com");
        result.City.Should().Be("Lyon");
        result.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateCustomerRequest { Name = "X" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsCorrectly()
    {
        var customer = await SeedCustomerAsync("OldName");

        var result = await _service.UpdateAsync(customer.Id, new UpdateCustomerRequest
        {
            Name = "NewName",
            Phone = "0600000000",
            City = "Marseille"
        });

        result.Should().NotBeNull();
        result!.Name.Should().Be("NewName");
        result.Phone.Should().Be("0600000000");
        result.City.Should().Be("Marseille");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenCustomerHasMenuPlans()
    {
        var customer = await SeedCustomerAsync();

        _db.MenuPlans.Add(new MenuPlan
        {
            Name = "Plan janvier",
            Month = 1,
            Year = 2026,
            CustomerId = customer.Id,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(customer.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemoves_WhenClean()
    {
        var customer = await SeedCustomerAsync();

        var result = await _service.DeleteAsync(customer.Id);

        result.Should().BeTrue();
        _db.Customers.Should().BeEmpty();
    }
}
