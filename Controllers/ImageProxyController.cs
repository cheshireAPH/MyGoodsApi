using Microsoft.AspNetCore.Mvc;

namespace MyGoodsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageProxyController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> FetchImage([FromBody] UrlRequest req)
        {
            if (string.IsNullOrEmpty(req.Url))
                return BadRequest("Missing url");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            var bytes = await client.GetByteArrayAsync(req.Url);

            return File(bytes, "image/jpeg");
        }
    }

    public class UrlRequest
    {
        public string Url { get; set; }
    }
}
