using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using SAMLTest.SAMLObjects;

namespace SAMLTest.Pages;

/// <summary>
/// This is the Help Page Model
/// </summary>
public class HelpModel : PageModel {
    private readonly IConfiguration _configuration;
    public string EntityID { get; private set; }
    public string ServerName { get; private set; }

    public HelpModel(IConfiguration configuration) {
        _configuration = configuration;
    }

    public void OnGet() {
        EntityID = SAMLHelper.GetSPEntityID(_configuration);
        ServerName = SAMLHelper.GetThisURL(this);
    }

}
