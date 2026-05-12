using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SAMLTest.SAMLObjects;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace SAMLTest.Pages.IDP;

/// <summary>
/// This is the Index Page Model for the Identity Provider
/// </summary>
public class IndexModel : PageModel {
    [DisplayName("MetadataURL"), Required]
    public string MetadataURL { get; set; }

    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// This Constructor is used to retrieve the Appsettings data
    /// </summary>
    public IndexModel(IConfiguration configuration, IMemoryCache cache) {
        _configuration = configuration;
        _cache = cache;
    }

    public IActionResult OnGet() {
        MetadataURL = SAMLHelper.GetThisURL(this) + "/metadata";
        return Page();
    }
    /// <summary>
    /// This Post Action is used to Generate and POST the SAML Repsonse for and IDP initiated SSO
    /// </summary>
    public IActionResult OnPost(string MetadataURL) {
        if (!GetSAMLMetadata(MetadataURL, out string entityID, out string acsServiceURL)) {
            return BadRequest("Metadata URL invalid");
        }
        SAMLResponse Resp = new SAMLResponse(acsServiceURL, "", SAMLHelper.GetThisURL(this), _configuration);
        string SAMLResponse = Convert.ToBase64String(Encoding.UTF8.GetBytes(Resp.ToString()));

        return Content( SAMLHelper.GeneratePost(SAMLResponse, acsServiceURL, "SAMLResponse"), "text/html");
        

    }
    private bool GetSAMLMetadata(string url, out string entityID, out string acsServiceURL) {
        acsServiceURL = null;
        entityID = null;

        if (!_cache.TryGetValue(url, out string metadata)) {
            HttpClient client = new HttpClient();
            HttpResponseMessage res = client.GetAsync(url).Result;
            metadata = res.Content.ReadAsStringAsync().Result;
            if (!res.IsSuccessStatusCode) {
                client.Dispose();
                return false;
            }
            client.Dispose();
            _cache.Set(url, metadata, TimeSpan.FromMinutes(60));
        }

        try {
            XDocument doc = XDocument.Parse(metadata);
            XNamespace md = "urn:oasis:names:tc:SAML:2.0:metadata";
            entityID = doc.Root?.Attribute("entityID")?.Value;
            var acs = doc.Descendants(md + "SPSSODescriptor")
                            .Descendants(md + "AssertionConsumerService")
                            .FirstOrDefault(x => x.Attribute("Binding")?.Value.Contains("HTTP-POST") == true)
                            ?? doc.Descendants(md + "AssertionConsumerService").FirstOrDefault();
            acsServiceURL = acs?.Attribute("Location")?.Value;

            _cache.Set(entityID, url, TimeSpan.FromMinutes(60));
        } catch (Exception ex) {
            return false;
        }
        return true;
    }

}