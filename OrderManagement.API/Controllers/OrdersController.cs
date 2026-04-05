using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.API.CQRS.Commands;
using OrderManagement.API.CQRS.Queries;
using OrderManagement.API.DTOs;
using Shared.Contracts.Enums;

namespace OrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/orders/checkout
    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout(
        [FromBody] CreateOrderDto dto)
    {
        var result = await _mediator.Send(new CheckoutOrderCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // GET /api/orders
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAll(
        [FromQuery] OrderStatus? status = null)
    {
        var result = await _mediator.Send(new GetOrdersQuery(status));
        return Ok(result);
    }

    // GET /api/orders/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    // GET /api/orders/{id}/status
    [HttpGet("{id}/status")]
    public async Task<ActionResult<object>> GetStatus(Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(new { result.Id, result.Status, result.FailureReason });
    }
}