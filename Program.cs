using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;

namespace SAMLTest;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options => {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        builder.Services.AddMemoryCache();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        } else {
            // For Production use Custom error handling and ensure it uses HTTPS
            app.UseExceptionHandler("/Error");
            app.UseHsts();
            //var options = new RewriteOptions().AddRedirectToHttpsPermanent();
            //app.UseRewriter(options);
            app.UseHttpsRedirection();
        }

        // Use Status Code error handling to our custom page.
        app.UseStatusCodePagesWithRedirects("/Error?StatusCode={0}");
        // For the wwwroot folder
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.MapRazorPages();

        app.Run();
    }
}
