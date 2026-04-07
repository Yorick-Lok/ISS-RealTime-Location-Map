using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Microsoft.Web.WebView2.WinForms;

namespace ISS_RealTime_Location_Map
{
    public partial class Form1 : Form
    {
        private WebView2 webView;
        private Timer timer = new Timer();

        public Form1()
        {
            InitializeComponent();

            // Setup WebView2
            webView = new WebView2 { Dock = DockStyle.Fill };
            this.Controls.Add(webView);

            this.Load += Form1_Load;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await webView.EnsureCoreWebView2Async(null);
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "map.html");
            webView.Source = new Uri(path);

            // Wait for HTML to load completely
            webView.NavigationCompleted += async (s, ev) =>
            {
                // Tiny delay to ensure JS functions exist
                await Task.Delay(100);
            };
        }

        // ISS API classes
        public class IssLocation { public double latitude { get; set; } public double longitude { get; set; } }
        public class AstroResponse { public int number { get; set; } public List<Person> people { get; set; } }
        public class Person { public string name { get; set; } public string craft { get; set; } }

        private async Task FetchISSData()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var issJson = await client.GetStringAsync("https://api.wheretheiss.at/v1/satellites/25544");
                    var iss = JsonConvert.DeserializeObject<IssLocation>(issJson);

                    var crewJson = await client.GetStringAsync("http://api.open-notify.org/astros.json");
                    var crew = JsonConvert.DeserializeObject<AstroResponse>(crewJson);

                    if (webView?.CoreWebView2 != null)
                    {
                        await webView.CoreWebView2.ExecuteScriptAsync(
                            $"updateISS({iss.latitude}, {iss.longitude});"
                        );

                        var crewNames = string.Join(",",
                            crew.people
                                .Where(p => p.craft == "ISS")
                                .Select(p => $"'{p.name.Replace("'", "\\'")}'")
                        );

                        await webView.CoreWebView2.ExecuteScriptAsync(
                            $"updateCrew([{crewNames}]);"
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}