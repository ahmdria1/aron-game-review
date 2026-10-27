// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Text;

namespace Aron.Logic
{
    /// <summary>Builds the links opened for Ahmed's web shops.</summary>
    public static class StoreLinks
    {
        public static bool IsWebUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u)) return false;
            return (u.Scheme == "https" || u.Scheme == "http") && !string.IsNullOrEmpty(u.Host);
        }

        /// <summary>Product page link, or null while the shop has no base URL and the product has no full link.</summary>
        public static string ProductUrl(WebShop shop, WebProduct product)
        {
            if (product == null) return null;
            string url = product.url == null ? "" : product.url.Trim();
            if (IsWebUrl(url)) return url;
            if (shop == null || !shop.IsOpen) return null;
            if (url.Length == 0) return shop.baseUrl.Trim();
            return Join(shop.baseUrl, url);
        }

        /// <summary>
        /// Where «إتمام الطلب» goes. Uses the checkout template when there is one, otherwise the home page.
        /// Null while the shop has no base URL.
        /// </summary>
        public static string CheckoutUrl(WebShop shop, GroceryCart cart)
        {
            if (shop == null || !shop.IsOpen) return null;
            string home = shop.baseUrl.Trim();
            string template = shop.checkoutTemplate == null ? "" : shop.checkoutTemplate.Trim();
            if (template.Length == 0 || cart == null || cart.IsEmpty) return home;

            var items = new StringBuilder();
            var ids = new StringBuilder();
            var qtys = new StringBuilder();
            foreach (var line in cart.Lines)
            {
                if (items.Length > 0) { items.Append(','); ids.Append(','); qtys.Append(','); }
                items.Append(line.id).Append(':').Append(line.quantity);
                ids.Append(line.id);
                qtys.Append(line.quantity);
            }
            string url = template
                .Replace("{base}", home.TrimEnd('/'))
                .Replace("{items}", Uri.EscapeDataString(items.ToString()))
                .Replace("{ids}", Uri.EscapeDataString(ids.ToString()))
                .Replace("{qtys}", Uri.EscapeDataString(qtys.ToString()))
                .Replace("{count}", cart.TotalQuantity.ToString());
            if (!IsWebUrl(url)) url = Join(home, url);
            return IsWebUrl(url) ? url : home;
        }

        private static string Join(string baseUrl, string path)
        {
            return baseUrl.Trim().TrimEnd('/') + "/" + path.TrimStart('/');
        }
    }
}
