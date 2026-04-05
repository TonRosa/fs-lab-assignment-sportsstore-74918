using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.API.CQRS.Queries;
using OrderManagement.API.Data;
using OrderManagement.API.DTOs;
using OrderManagement.API.Models;

namespace OrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly OrderDbContext _db;

    public CustomersController(IMediator mediator, OrderDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    // GET /api/customers/{id}/orders
    [HttpGet("{id}/orders")]
    public async Task<ActionResult<List<OrderDto>>> GetCustomerOrders(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerOrdersQuery(id));
        return Ok(result);
    }

    // POST /api/customers (for creating test customers)
    [HttpPost]
    public async Task<ActionResult<Customer>> Create([FromBody] CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            Email = dto.Email
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCustomerOrders),
            new { id = customer.Id }, customer);
    }
}

public class CreateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}