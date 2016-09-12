# Dynamics CRM Solution Exchanger
Command Line tool for importing and exporting solutions to and from Dynamics CRM, as well as publishing customization
There is also an integration into FAKE - F# Make!

# Purpose
The primary purpose for this tool was to create automated backups of solutions, in extension to the full database backups of organizations.
You can hook up this tool in your CI and run it on a daily basis by using the [FAKE - F# Make Integration](https://github.com/fsharp/FAKE/blob/master/src/app/FakeLib/DynamicsCRMHelper.fs).
If no credentials are specified in your calls, the tool will simply run with the default user credentials, who is executing it.

# Usage
`SolutionExchanger.exe [Export | Import | Publish] [/user: | /password: | /url: | /solution: | /managed: | /filename: | /workingdir: | /allSolutions: | /allOrganizations:]`

  `/user`              -   Username for authenticating with CRM endpoint. If no user and password are given, fallback to default credentials
  
  `/password`          -   Password for authenticating with CRM endpoint. If no user and password are given, fallback to default credentials
  
  `/url`               -   Required. Url of CRM endpoint
  
  `/solution`          -   Required if exporting. Unique name of solution to export. Can be replaced by allSolutions to get all unmanaged solutions in organization
  
  `/allSolutions`      -   Pass like /allSolutions:true to Export all unmanaged solutions in organization
  
  `/allOrganizations`  -   Pass like /allOrganizations:true to Export all unmanaged solutions in all organizations
  
  `/managed`           -   Required if exporting. Pass 'true' for exporting managed, false for unmanaged
  
  `/filename`          -   Required if importing. Pass full path to solution. If Exporting sets name of exported solution file
  
  `/workingdir`        -   Sets working directory for writing exported solution to file
  
  `/timeout`           -   Set timeout property of OrganizationServiceProxy to this value. Enter integer value which represents the minutes

# Nuget
A frequently updated NuGet package is available [here](https://www.nuget.org/packages/Dynamics.CRM.SolutionExchanger/)

# License
This project is released under MIT license.
However, please find the license terms for the referenced Microsoft SDKs (Microsoft.Xrm.Sdk, Microsoft.Xrm.Client, Microsoft.Crm.Sdk.Proxy) in the root folder as "CRM-SDK-LicenseTerms.docx"
