using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using XtreamCache.Actors;

namespace XtreamCache.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IpInfoController : ControllerBase
    {
        private readonly ILogger<IpInfoController> _logger;
        private readonly ActorSystem _actorSystem;

        public IpInfoController(ILogger<IpInfoController> logger, ActorSystem actorSystem)
        {
            _logger = logger;
            _actorSystem = actorSystem;
        }

        [HttpGet("{ip}")]
        public async Task<IActionResult> Get(string ip)
        {
            var ipNumber = ToInt(ip);
            var query = new CacheQuery<IPInformation>(m => ipNumber >= m.From && ipNumber <= m.To);
            var actor = _actorSystem.ActorOf(Props.Create(() => new CacheQueryActor<IPInformation>(TimeSpan.FromSeconds(10), true)));
            var result = await actor.Ask<CacheQueryResponse<IPInformation>>(query);
            return Ok(result);
        }

        static long ToInt(string addr)
        {
            return (long)(uint)IPAddress.NetworkToHostOrder(
                 (int)IPAddress.Parse(addr).Address);
        }

        static string ToAddr(long address)
        {
            return IPAddress.Parse(address.ToString()).ToString();
        }
    }
}
