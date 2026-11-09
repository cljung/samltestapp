using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SAMLTest.SAMLObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SAMLTest.Pages.SP;

/// <summary>
/// This is the Assertion Consumer Page Model
/// This page will be posted to from outside this application
/// thuys the Ignore Anti Forgery Token below
/// </summary>
[IgnoreAntiforgeryToken(Order = 1001)]
public class AssertionConsumerModel : PageModel
{
    public String SessionId { get; private set; }
    public string SAMLRequest { get; set; }
    public String SAMLResponse { get; private set; }
    public Dictionary<string, string> attrsandvals { get; private set; }
    public string MetadataURL { get; set; }
    public string EntityID { get; set; }
    public string Issuer { get; set; }
    public String NameId { get; private set; }

    private readonly IMemoryCache _cache;

    public AssertionConsumerModel(IMemoryCache cache) {
        _cache = cache;
    }

    public IActionResult OnPost(string SAMLResponse, string RelayState) {
        SAMLHelper.ParseRelayState(RelayState, out string eID, out string issuer);
        EntityID = eID;
        Issuer = issuer;

        byte[] ENcSAMLByteArray = Convert.FromBase64String(SAMLResponse);
        string sml = System.Text.ASCIIEncoding.ASCII.GetString(ENcSAMLByteArray);
        XmlDocument doc = new XmlDocument();
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
        nsmgr.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
        doc.LoadXml(sml);
        XmlElement root = doc.DocumentElement;

        string statusCode = root.SelectSingleNode("/samlp:Response/samlp:Status/samlp:StatusCode/@Value", nsmgr).Value;
        if (statusCode.Trim() != "urn:oasis:names:tc:SAML:2.0:status:Success") {
            string statusMessage = root.SelectSingleNode("/samlp:Response/samlp:Status/samlp:StatusMessage", nsmgr).InnerText;
            return Redirect("/Error?ErrorMessage=" + statusMessage);
        }

        string oid = string.Empty;
        string tid = string.Empty;
        string userId = string.Empty;
        string displayName = string.Empty;        
        string[] groups = null;

        XmlNodeList nodes = root.SelectNodes("/samlp:Response/saml:Assertion/saml:AttributeStatement/saml:Attribute", nsmgr); 
        this.attrsandvals = new Dictionary<string, string>();
        foreach (XmlNode node in nodes) {
            String attrname = node.Attributes["Name"].Value;
            String val = "";
            if (node.HasChildNodes && node.ChildNodes.Count > 1) {
                var values = node.ChildNodes.Cast<XmlNode>().Select(item => item.InnerText).ToList();
                val = string.Join(", ", values);
            } else {
                val = node.InnerText;
            }
            this.attrsandvals.Add(attrname, val);
            switch( attrname ) {
                case "http://schemas.microsoft.com/identity/claims/objectidentifier":
                    oid = val;
                    break;
                case "http://schemas.microsoft.com/identity/claims/tenantid":
                    tid = val;
                    break;
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":
                    userId = val;
                    break;
                case "http://schemas.microsoft.com/identity/claims/displayname":
                    displayName = val;
                    break;
                case "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups":
                    groups = val.Split(", ");
                    break;
            }
        }

        this.SAMLResponse = sml;
        this.SessionId = root.SelectSingleNode("/samlp:Response/saml:Assertion/saml:AuthnStatement/@SessionIndex", nsmgr).Value;
        this.NameId = root.SelectSingleNode("/samlp:Response/saml:Assertion/saml:Subject/saml:NameID", nsmgr).InnerText;
        // For IDP Initiated SSO, InResponseTo doesn't exist
        var nodeInResponseTo = root.SelectSingleNode("/samlp:Response/@InResponseTo", nsmgr);
        if (null != nodeInResponseTo) {
            string ID = nodeInResponseTo.Value;
            if (_cache.TryGetValue(ID, out string request)) {
                _cache.Remove(ID);
            }
            this.SAMLRequest = request;
        } else {
            this.SAMLRequest = string.Empty;
        }
        Login( tid, oid, userId, displayName, groups );
        return Page();        
    }

    public async void Login(string tid, string oid, string userId, string displayName, string[] groups ) {
        var claims = new List<Claim> {
            new Claim("tenantId", tid),
            new Claim(ClaimTypes.NameIdentifier, oid),
            new Claim(ClaimTypes.Upn, userId),
            new Claim(ClaimTypes.Email, userId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Role, "User")
        };
        if ( null != groups ) {
            foreach (var group in groups) {
                claims.Add(new Claim("groups", group));
                claims.Add(new Claim(ClaimTypes.Role, group));
            }
        }
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20) // Session lifetime
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), 
            authProperties);
    }

}