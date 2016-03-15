using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GenedosePdfExample
{
    public class Program
    {
        public static void Main(string[] argv)
        {
            try
            {
                var model = new ReportModel
                {
                    pgxid = new Guid("guid-here"),
                    productIds = new[] { 88 },
                    lifestyles = new[] { 2, 3, 8, 17 }
                };
                using (var p = new PdfDownloader("template-here", "oauth-token-here"))
                {
                    using (var fs = File.OpenWrite(@"c:\temp\" + DateTime.Now.Ticks + ".pdf"))
                    {
                        p.DownloadAsync(model, fs).Wait();
                    }
                }
                Console.WriteLine("done!");
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Console.WriteLine("press enter to continue");
            Console.ReadLine();
        }
    }

    public class ReportModel
    {
        public Guid pgxid { get; set; }
        public IList<int> productIds { get; set; }
        public IList<int> lifestyles { get; set; }
        public IList<int> indications { get; set; }
    }

    public class PdfDownloader : IDisposable
    {
        private HttpClientHandler _handler;
        private HttpClient _client;

        public PdfDownloader(String template, String oauthToken) 
            : this("api.coriell-services.com", template, oauthToken) { }

        public PdfDownloader(String host, String template, String oauthToken)
        {
            _handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                PreAuthenticate = true
            };

            var urib = new UriBuilder() {
                Scheme = "https",
                Host = host,
                Path = "report/dynamic",
                Query = "template=" + template
            };

            _client = new HttpClient(_handler) { BaseAddress = urib.Uri };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);
        }

        public async Task DownloadAsync(ReportModel model, Stream target)
        {
            var response = await _client.PostAsJsonAsync((String) null, model);
            response.EnsureSuccessStatusCode();
            var stm = await response.Content.ReadAsStreamAsync();
            using (stm)
            {
                await stm.CopyToAsync(target);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                    _handler.Dispose();
                }
                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
