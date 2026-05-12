# SAML Test App

This repo contains a SAML Test App for SP initiated SSO where Entra is the Identity Provider. Other IDPs might work, but it is testedt with Entra.

A working deployment is available at [https://samltestapp.azurewebsites.net/](https://samltestapp.azurewebsites.net/). 
You can use this instance without building and deploying the solution yourself. 
The [Help](https://samltestapp.azurewebsites.net/Help) page describes how you register it in Entra, but the steps are:

## Steps to add samltestapp to your Entra tenant:

1. Goto Entra ID > Enterprise apps > +New application > +Create your own application
1. Enter a name, like SAMLTestApp and select Integrate any other application
1. Select Single sign-on
1. Select Edit for the Basic SAML Configuration
1. Enter '748f981c36434853ae702032edae49e0' for the Identifier (Entity ID). This value comes from [appsettings.json](appsettings.json).
1. Enter 'https://samltestapp.azurewebsites.net/SP/AssertionConsumer' for the Reply URL (Assertion Consumer Service URL)
1. Click Save


## To test:

1. Scroll down and copy the App Federation Metadata Url link
1. Open https://samltestapp.azurewebsites.net/SP/ in the browser
1. Paste in the metadata URL
1. Paste in '748f981c36434853ae702032edae49e0' as the Issuer
1. Click Login

## To add more claims

As an excersise, you can add group memberships to the SAML response.

1. Goto Entra ID > Enterprise apps > samltestapp
1. Attributes & Claims > Edit
1. +Add group claim
1. Security groups
1. objectID
1. Save
