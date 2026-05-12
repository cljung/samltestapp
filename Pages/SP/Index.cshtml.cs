using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
using System.Xml.Linq;

namespace SAMLTest.Pages.SP;

/// <summary>
/// This is the Index Page Model for the Service Provider
/// </summary>
public class IndexModel : PageModel {
    [DisplayName("MetadataURL"), Required]
    public string MetadataURL { get; set; }

    [DisplayName("Issuer")]
    public string Issuer { get; set; }

    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public IndexModel(IConfiguration configuration, IMemoryCache cache) {
        _configuration = configuration;
        _cache = cache;
    }
    private string GetSessionVariable(string key, string value ) {
        if (string.IsNullOrEmpty(value)) {
            value = HttpContext.Session.GetString(key);
            if (string.IsNullOrEmpty(value)) {
                value = _configuration.GetValue(key, "");
            }
        }
        return value;
    }
    private void SetSessionVariable(string key, string value) {
        if (null != value) {
            HttpContext.Session.SetString(key, value);
        }
    }
    public IActionResult OnGet(string MetadataURL, string Issuer) {        
        this.MetadataURL = GetSessionVariable("MetadataURL", MetadataURL);
        if (string.IsNullOrWhiteSpace(this.MetadataURL)) {
            this.MetadataURL = SAMLHelper.GetThisURL(this) + "/metadata";
        }
        this.Issuer = GetSessionVariable( "Issuer", Issuer );
        if ( string.IsNullOrWhiteSpace(this.Issuer) ) {
            this.Issuer = SAMLHelper.GetSPEntityID(_configuration);
        }
        SetSessionVariable("MetadataURL", this.MetadataURL);
        SetSessionVariable("Issuer", this.Issuer);
        return Page();
    }

    /// <summary>
    /// This Post Action is used to Generate the AuthN Request and redirect to the B2C Login endpoint
    /// </summary>
    public IActionResult OnPost( string MetadataURL, string Issuer ) {
        if ( !GetSAMLMetadata(MetadataURL, out string idpEntityID, out string ssoURL) ) {
            return BadRequest("Metadata URL invalid");
        }
        SetSessionVariable( "MetadataURL", MetadataURL );
        SetSessionVariable( "Issuer", Issuer );        
        string spEntityID = SAMLHelper.GetSPEntityID(_configuration);
        AuthnRequest AuthnReq = new AuthnRequest( ssoURL, spEntityID, SAMLHelper.GetThisURL(this), Issuer );
        string authReq = AuthnReq.ToString();
        _cache.Set(AuthnReq.ID, authReq, TimeSpan.FromMinutes(5));
        string cdoc = SAMLHelper.FormatSAMLRequest( authReq );
        string RelayState = SAMLHelper.FormatRelayState( idpEntityID, Issuer );
        ssoURL = ssoURL + "?SAMLRequest=" + cdoc + "&RelayState=" + RelayState;
        return Redirect(ssoURL);
    }

    private bool GetSAMLMetadata( string url, out string entityID, out string ssoServiceURL ) {
        ssoServiceURL = null;
        entityID = null;

        if( !_cache.TryGetValue( url, out string metadata ) ) {
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
            ssoServiceURL = doc.Descendants(md + "IDPSSODescriptor").Descendants(md + "SingleSignOnService")
                                .FirstOrDefault(x => x.Attribute("Binding")?.Value.Contains("HTTP-Redirect") == true)
                                ?.Attribute("Location")?.Value;
            _cache.Set(entityID, url, TimeSpan.FromMinutes(60));
        } catch( Exception ex ) {
            return false;            
        }
        return true;
    }
}
