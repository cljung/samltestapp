using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using SAMLTest.SAMLObjects;
using System;
using System.Collections.Generic;

namespace SAMLTest.Pages;

/// <summary>
/// This is the Metadata Page Model
/// </summary>
public class MetadataModel : PageModel {
    public string ServerName { get; private set; }
    public string ID { get; private set; }
    public string EntityID { get; private set; }
    public Boolean ShowView { get; private set; } = false;
    public List<SAMLUserAttribute> UserAttributes;

    private readonly IConfiguration _configuration;

    /// <summary>
    /// This Constructor is used to retrieve the Appsettings data
    /// </summary>
    public MetadataModel(IConfiguration configuration) {
        _configuration = configuration;
    }

    public void OnGet(string showpage="false")
    {
        EntityID = SAMLHelper.GetSPEntityID(_configuration);
        ID = Guid.NewGuid().ToString();
        UserAttributes = SAMLHelper.GetSAMLUserAttributes(_configuration);
        ServerName = SAMLHelper.GetThisURL(this);
        if (showpage != "false")
        {
            ShowView = true;
        }
    }

}
