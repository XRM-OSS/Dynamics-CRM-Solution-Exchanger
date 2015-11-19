module KVS.Crm.SolutionPorter

open Microsoft.Xrm.Client
open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Client.Services
open Microsoft.Crm.Sdk.Messages
open System
open System.Configuration
open System.IO
open System.ServiceModel.Description
open System.Diagnostics

type CrmEndpointParams =
    {
        Url : string
        Username : string
        Password : string
    }

let CrmEndpointDefaults = 
    {
        Url = ""
        Username = ""
        Password = ""
    }

/// Creates Organization Service for communicating with Dynamics CRM
/// ## Parameters
///
///  - `username` - Username for authentication
///  - `password` - Password for authentication
///  - `url` - URL that is used to connect to CRM
let private CreateOrganizationService username password url =
    printfn "Creating Organization Service: %A" url
    try
        let credentials = new ClientCredentials()
        credentials.UserName.UserName <- username
        credentials.UserName.Password <- password
        let serviceUri = new Uri(url)
        let proxy = new OrganizationServiceProxy(serviceUri, null, credentials, null)
        proxy.EnableProxyTypes()
        printfn "Successfully created organization service"
        Some(proxy :> IOrganizationService)
    with   
        | ex -> failwith (sprintf "Error while creating organization service: %A" ex.Message)

/// Publishes all solution component changes.
/// ## Parameters
///
///  - `organizationService` - Organization Service to use for executing requests
let PublishAll crmEndpoint =
    printfn "Publishing all"
    let serviceParams = crmEndpoint CrmEndpointDefaults
    let organizationService = CreateOrganizationService serviceParams.Username serviceParams.Password serviceParams.Url
    if organizationService.IsNone then
        failwith "Could not create connection to CRM, check your endpoint config"
    let publishRequest = new PublishAllXmlRequest()
    let response = organizationService.Value.Execute(publishRequest)
    printfn "Successfully published all" 

/// Writes solution byte[] to file. If a file with the same name is present in given path, it is being overridden.
/// ## Parameters
///
///  - `fileName` - File name for file that is created
///  - `solution` - Solution as byte[] that was retrieved from ExportSolution
///  - `path` - Path to write file to, be sure to pass with trailing backslash
let WriteSolutionToFile fileName solution path = 
    printfn "Writing solution to file %A" (path + fileName)
    let filePath = path + fileName
    File.WriteAllBytes(filePath, solution)
    printfn "Successfully wrote solution to file"
            
/// Exports solution from Dynamics CRM and stores it in memory as byte[]
/// ## Parameters
///
///  - `organizationService` - Organization Service to use for executing requests
///  - `solutionName` - Unique name of solution that should be exported
///  - `managed` - Boolean: True for exporting as managed solution, false for exporting as unmanaged
let ExportSolution crmEndpoint solutionName (managed : bool) =
    printfn "Exporting solution %A" (solutionName + ": " + if managed then "Managed" else "Unmanaged")
    let serviceParams = crmEndpoint CrmEndpointDefaults
    let organizationService = CreateOrganizationService serviceParams.Username serviceParams.Password serviceParams.Url
    if organizationService.IsNone then
        failwith "Could not create connection to CRM, check your endpoint config"
    let exportSolutionRequest = new ExportSolutionRequest( Managed = managed, SolutionName = solutionName )
    let response = organizationService.Value.Execute(exportSolutionRequest) :?> ExportSolutionResponse
    printfn "Successfully exported solution"
    response.ExportSolutionFile

/// Imports zipped solution file to Dynamics CRM
/// ## Parameters
///
///  - `organizationService` - Organization Service to use for executing requests
///  - `path` - Full path to zipped solution file
let ImportSolution crmEndpoint path =
    printfn "Importing solution %A" path
    let serviceParams = crmEndpoint CrmEndpointDefaults
    let organizationService = CreateOrganizationService serviceParams.Username serviceParams.Password serviceParams.Url
    if organizationService.IsNone then
        failwith "Could not create connection to CRM, check your endpoint config"
    if not (File.Exists(path)) then
        failwith "File at path %A does not exist!" path
    let file = File.ReadAllBytes(path)
    let importSolutionRequest = new ImportSolutionRequest( CustomizationFile = file )
    let response = organizationService.Value.Execute(importSolutionRequest) :?> ImportSolutionResponse
    printfn "Successfully imported solution"