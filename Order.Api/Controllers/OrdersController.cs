
namespace Order.Api.Controllers
{

    using Microsoft.AspNetCore.Mvc;

    using Order.Api.Controllers.Requests;
    using OrderSystem.Application.Services;
    using System.Threading;

  
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersService _service;
   
        public OrdersController(OrdersService service) => _service = service; 

        [HttpPost]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var order = await _service.CreateAsync(ct);
            return Ok(order);
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        {
            await _service.ConfirmAsync(id, ct);
            return NoContent();
        }

        [HttpPost("{id}/mark-paid")]
        public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
        {
            await _service.MarkAsPaidAsync(id, ct);
            return NoContent();
        }

        [HttpPost("{id}/fail")]
        public async Task<IActionResult> Fail(Guid id, [FromBody] FailOrderRequest request, CancellationToken ct)
        {
            await _service.FailAsync(id, request?.Reason, ct);
            return NoContent();
        }
    }
}
