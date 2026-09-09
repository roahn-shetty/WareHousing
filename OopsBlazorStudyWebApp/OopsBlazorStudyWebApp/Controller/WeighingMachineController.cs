using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OopsBlazorStudyWebApp.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeighingMachineController : ControllerBase
    {
        [HttpGet("weight")]
        public IActionResult GetWeight()
        {
            double weight = Math.Round(
                Random.Shared.NextDouble() * 100,
                3
            );

            return Ok(new
            {
                success = true,
                weight = weight,
                unit = "KG"
            });
        }
    }
}
