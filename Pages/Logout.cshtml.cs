using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SAMLTest.SAMLObjects;
using System;
using System.Linq;
using System.Xml.Linq;

namespace SAMLTest.Pages;

public class LogoutModel : PageModel {
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    public string SAMLLogoutRequest { get; set; }
    public LogoutModel(IConfiguration configuration, IMemoryCache cache) {
        _configuration = configuration;
        _cache = cache;
    }

    public IActionResult OnGet( string SessionId,string NameId, string Issuer, string EntityID) {
        if (String.IsNullOrEmpty(SessionId) || String.IsNullOrEmpty(NameId) || String.IsNullOrEmpty(Issuer) || String.IsNullOrEmpty(EntityID)) {
            if (_cache.TryGetValue(this.HttpContext.Session.Id, out string logoutReq)) {
                _cache.Remove(this.HttpContext.Session.Id);
                SAMLLogoutRequest = logoutReq;
            }
            return Page();
        } else {
            return OnPost(SessionId, NameId, Issuer, EntityID);
        }
    }

    public IActionResult OnPost( string SessionId, string NameId, string Issuer, string EntityID ) {
        if (!GetSAMLMetadata( EntityID, out string ssoURL)) {
            return BadRequest("No session - User not logged in");
        }
        string spEntityID = SAMLHelper.GetSPEntityID(_configuration);
        LogoutRequest logoutRequest = new LogoutRequest(ssoURL, SAMLHelper.GetThisURL(this), SessionId, NameId, Issuer);
        string logoutReq = logoutRequest.ToString();
        _cache.Set( this.HttpContext.Session.Id, logoutReq, TimeSpan.FromMinutes(5));
        ssoURL = ssoURL  + "?SAMLRequest=" + SAMLHelper.FormatSAMLRequestLogout( logoutReq );
        return Redirect(ssoURL);

    }
    private bool GetSAMLMetadata( string entityID, out string ssoServiceURL) {
        ssoServiceURL = null;
        // in order to logout, you should have successfully logged in (and cached this)
        if (!_cache.TryGetValue( entityID, out string url)) {
            return false;
        }
        if (!_cache.TryGetValue(url, out string metadata)) {
            return false;            
        }
        try {
            XDocument doc = XDocument.Parse(metadata);
            XNamespace md = "urn:oasis:names:tc:SAML:2.0:metadata";
            entityID = doc.Root?.Attribute("entityID")?.Value;
            ssoServiceURL = doc.Descendants(md + "IDPSSODescriptor").Descendants(md + "SingleLogoutService")
                                .FirstOrDefault(x => x.Attribute("Binding")?.Value.Contains("HTTP-Redirect") == true)
                                ?.Attribute("Location")?.Value;
        } catch (Exception ex) {
            return false;
        }
        return true;
    }

}
