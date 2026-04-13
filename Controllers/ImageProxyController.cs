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
            if (req.Urls == null || req.Urls.Length == 0)
                return BadRequest("Missing urls");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            foreach (var url in req.Urls)
            {
                try
                {
                    var bytes = await client.GetByteArrayAsync(url);
                    if (bytes?.Length > 0)
                        return File(bytes, "image/jpeg");
                }
                catch
                {
                    // 次の URL を試す
                }
            }

            return BadRequest("No valid image found");
        }
    }

    public class UrlRequest
    {
        public string[] Urls { get; set; }
    }
}
