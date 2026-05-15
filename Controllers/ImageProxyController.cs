using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace MyGoodsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageProxyController : ControllerBase
    {
        private readonly HttpClient _client;

        public ImageProxyController()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        }

        // ★ メイン入口（どのサイトでもここに投げるだけ）
        [HttpPost]
        public async Task<IActionResult> FetchImage([FromBody] UrlRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Url))
                return BadRequest("Missing url");

            var url = req.Url;

            // 画像URLならそのまま返す
            if (TryGetImageMime(url, out var mime))
            {
                try
                {
                    var bytes = await _client.GetByteArrayAsync(url);
                    return File(bytes, mime);
                }
                catch
                {
                    return BadRequest("Failed to fetch direct image");
                }
            }

            // ------------------------------
            // サイト判定
            // ------------------------------
            //ムービック
            if (url.Contains("movic.jp"))
                return await FetchMovic(url);

            //アニハピ（NEO GATE通販）
            if (url.Contains("anihapi-online.com"))
                return await FetchAnihapi(url);

            // 今後サイトが増えたらここに追加するだけ
            // if (url.Contains("animate-onlineshop.jp")) return await FetchAnimate(url);
            // if (url.Contains("suruga-ya.jp")) return await FetchSurugaya(url);

            return BadRequest("Unsupported site");
        }

        /// <summary>画像URL判定</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool TryGetImageMime(string url, out string mime)
        {
            mime = "image/jpeg"; // デフォルト

            if (string.IsNullOrWhiteSpace(url))
                return false;

            var lower = url.ToLower();

            if (lower.EndsWith(".png"))
            {
                mime = "image/png";
                return true;
            }
            if (lower.EndsWith(".webp"))
            {
                mime = "image/webp";
                return true;
            }
            if (lower.EndsWith(".gif"))
            {
                mime = "image/gif";
                return true;
            }
            if (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg"))
            {
                mime = "image/jpeg";
                return true;
            }

            return false;
        }

        /// <summary>ムービック画像取得</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<IActionResult> FetchMovic(string url)
        {
            var codeMatch = Regex.Match(url, @"g/([A-Za-z0-9\-]+)/?$");
            if (!codeMatch.Success)
                return BadRequest("Invalid Movic URL");

            var code = codeMatch.Groups[1].Value;

            if (code.StartsWith("g"))
                code = code.Substring(1);

            // 再販コード補正
            code = ConvertReprintCode(code);

            var candidates = new[]
            {
                $"https://d38cuxvdcwawa4.cloudfront.net/img/goods/L/{code}.jpg",
                $"https://d38cuxvdcwawa4.cloudfront.net/img/goods/L/{code}-l.jpg",
                $"https://d38cuxvdcwawa4.cloudfront.net/img/goods/M/{code}.jpg",
                $"https://d38cuxvdcwawa4.cloudfront.net/img/goods/{code}.jpg"
            };

            foreach (var c in candidates)
            {
                try
                {
                    var bytes = await _client.GetByteArrayAsync(c);
                    if (bytes?.Length > 0)
                        return File(bytes, "image/jpeg");
                }
                catch { }
            }

            return BadRequest("Movic image not found");
        }

        private string ConvertReprintCode(string code)
        {
            var parts = code.Split('-');
            if (parts.Length == 3 && parts[2].Length == 5 && parts[2].StartsWith("9"))
            {
                parts[2] = "0" + parts[2].Substring(1);
                return string.Join("-", parts);
            }
            return code;
        }

        /// <summary>アニハピ（NEO GATE通販）画像取得</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<IActionResult> FetchAnihapi(string url)
        {
            var html = await _client.GetStringAsync(url);

            // BASE の商品画像パターンを直接拾う
            var match = Regex.Match(
                html,
                "https://baseec-img-mng\\.akamaized\\.net/images/item/origin/[^\"]+",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
                return BadRequest("Anihapi image not found");

            var imgUrl = match.Value;

            var bytes = await _client.GetByteArrayAsync(imgUrl);

            return File(bytes, "image/jpeg");
        }

        public class UrlRequest
        {
            public string Url { get; set; }
        }
    }
}
