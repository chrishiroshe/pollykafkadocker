using Microsoft.AspNetCore.Mvc;

namespace Order.Api.Controllers
{
    [ApiController]
    [Route("payments")]
    public class PaymentsController : ControllerBase
    {
        [HttpPost("{orderId}")]
        public async Task<IActionResult> Charge(Guid orderId, [FromQuery] bool fail = false, CancellationToken ct = default)
        {
            await Task.Delay(200, ct);
            if (fail) return StatusCode(500, "Forced failure");
            return Ok();
        }
    }
}
