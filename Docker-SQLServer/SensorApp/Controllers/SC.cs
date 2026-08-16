using Microsoft.AspNetCore.Mvc;
using SensorApp.Mdl;
using SensorApp.Mgr;

namespace SensorApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class SC : ControllerBase
    {
        [HttpGet("data")]
        public IActionResult G([FromQuery] string tp = "0", [FromQuery] string did = "0",
            [FromQuery] string df = "", [FromQuery] string dt = "")
        {
            return Ok(SM.GetAll(tp, did, df, dt));
        }

        [HttpPost("data")]
        public IActionResult P([FromBody] D d)
        {
            return Ok(new { ok = SM.Save(d) });
        }

        [HttpGet("dev")]
        public IActionResult GD([FromQuery] string st = "0")
        {
            return Ok(SM.GetDevs(st));
        }

        [HttpPost("dev")]
        public IActionResult PD([FromBody] D d)
        {
            return Ok(new { ok = SM.SaveDev(d) });
        }

        [HttpGet("calc")]
        public IActionResult C([FromQuery] int did = 1)
        {
            return Ok(SM.Calc(did));
        }

        [HttpGet("log")]
        public IActionResult GL([FromQuery] string did = "0", [FromQuery] string flg = "-1")
        {
            return Ok(SM.GetLog(did, flg));
        }

        [HttpGet("stats")]
        public IActionResult GS([FromQuery] int did = 1)
        {
            return Ok(SM.Stats(did));
        }
    }
}
