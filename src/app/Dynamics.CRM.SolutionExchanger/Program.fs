module Dynamics.Crm.SolutionExchangerMain

open Dynamics.Crm.SolutionExchanger
open System
open System.Globalization
open System.IO

/// Used for determing, whether an argument is a valid parameter. If it starts with the parameter flag, then return the rest
let (|StartsWith|_|) (start : string) (text : string) =
    if text.StartsWith(start, true, CultureInfo.InvariantCulture) then 
        Some (text.Substring start.Length)
    else
        None

/// Used for finding a parameter inside the  arguments array and returning it, else None
let FindOption (opt : string) (args : string[]) =
    let results = Seq.map (fun arg -> match arg with
                                        | StartsWith opt rest -> rest
                                        | _ -> "") args

    Seq.tryFind(fun res -> res <> "") results

/// Used for transforming string options, returns "Present" if value was set, else "missing"
let OptionToString (opt : option<'T>)=
    match opt with
    | Some value -> "Present"
    | None -> "Missing"

let ExportAllSolutions connectionString (managed:bool option) (workingDir:string option) timeOut = 
    let solutions = ExportAllSolutions (fun cred ->
                                { cred with
                                    ConnectionString = connectionString
                                    TimeOut = timeOut
                                }) managed.Value

    let dir = if workingDir.IsNone then @".\" else workingDir.Value
            
    solutions
        |> Seq.iter (fun (solution, uniqueName) -> 
            if solution.IsSome then
                WriteSolutionToFile (uniqueName + ".zip") solution.Value dir
            else
                printf "Skipping solution %s, since it has no value.\n" uniqueName)
    0

let ExportSolution connectionString (sol:string option) (filename:string option) (managed:bool option) (workingDir: string option) timeOut =
    let solution = ExportSolution (fun cred ->
                                        { cred with
                                            ConnectionString = connectionString
                                            TimeOut = timeOut
                                        }) sol.Value managed.Value

    if solution.IsNone then
        failwith "Failed to export solution. Please check, whether the solution might be corrupted. Exiting\n"

    let solutionName = if filename.IsNone then "Solution.zip" else filename.Value
    
    let dir = if workingDir.IsNone then @".\" else workingDir.Value
        
    WriteSolutionToFile solutionName solution.Value dir
    0

let parseBool (input:string option) referenceName =
    if input.IsSome then
            let parsedSuccess, parsedValue = bool.TryParse(input.Value) 
            if parsedSuccess then
                printf "Set %s to %b\n" referenceName parsedValue
                Some parsedValue
            else
                printf "Failed to parse %s\n" referenceName
                None 
        else
            None

let parseInt (input:string option) referenceName =
    if input.IsSome then
            let parsedSuccess, parsedValue = Int32.TryParse(input.Value, NumberStyles.Any, CultureInfo.InvariantCulture) 
            if parsedSuccess then
                printf "Set %s to %i\n" referenceName parsedValue
                Some parsedValue
            else
                printf "Failed to parse %s\n" referenceName
                None 
        else
            None

/// Used for exporting solution from CRM and saving it to file
let Export args =
    let connectionString = FindOption "/connectionString:" args
    let solution = FindOption "/solution:" args
    let managedText = FindOption "/managed:" args
    let workingDir = FindOption "/workingdir:" args
    let filename = FindOption "/filename:" args
    let allSolutionsText = FindOption "/allSolutions:" args
    let timeOutText = FindOption "/timeout:" args
    
    let allSolutions = parseBool allSolutionsText "allSolutions"
    let managed = parseBool managedText "managed"
    let timeOut = parseInt timeOutText "timeout"

    if connectionString.IsNone || (solution.IsNone && (allSolutions.IsNone || not allSolutions.Value)) || managed.IsNone then
        printf "Values missing: Needed /connectionString (%s), (/solution (%s) or /allSolutions:true or allOrganizations:true) and /managed (%s)\n" (OptionToString connectionString) (OptionToString solution) (OptionToString managed)
        1
    else
        if allSolutions.IsSome && allSolutions.Value then
            ExportAllSolutions connectionString.Value managed workingDir timeOut
        else
            ExportSolution connectionString.Value solution filename managed workingDir timeOut
                
            
/// Used for importing solution to CRM
let Import args = 
    let connectionString = FindOption "/connectionString:" args
    let filename = FindOption "/filename:" args

    let timeOutText = FindOption "/timeout:" args
    let timeOut = parseInt timeOutText "timeout"

    if connectionString.IsNone || filename.IsNone then
        printf "Values missing: Needed /connectionString (%s) and /filename (%s)\n" (OptionToString connectionString) (OptionToString filename)
        1
    else
        ImportSolution (fun cred ->
                             { cred with
                                    ConnectionString = connectionString.Value
                                    TimeOut = timeOut
                             }) filename.Value
        0

/// Used for publishing all customizations in CRM
let Publish args =
    let connectionString = FindOption "/connectionString:" args
    
    let timeOutText = FindOption "/timeout:" args
    let timeOut = parseInt timeOutText "timeout"


    if connectionString.IsNone then
        printf "Values missing: Needed /url (%s)\n" (OptionToString connectionString)
        1
    else
        PublishAll (fun cred ->
                        { cred with
                            ConnectionString = connectionString.Value
                            TimeOut = timeOut
                        })
        0

[<EntryPoint>]
let main argv = 
    if argv.Length < 1 then 
        printf "%s%s%s%s%s%s%s%s%s%s%s" 
            "Usage SolutionExchanger.exe [Export | Import | Publish] [/user: | /password: | /url: | /solution: | /managed: | /filename: | /workingdir: | /allSolutions: | /allOrganizations:]\n" 
            "/connectionString  -   Connection string for connecting to CRM. Always required.\n"
            "/password          -   Password for authenticating with CRM endpoint. If no user and password are given, fallback to default credentials\n"
            "/url               -   Required. Url of CRM endpoint\n"
            "/solution          -   Required if exporting. Unique name of solution to export. Can be replaced by allSolutions to get all unmanaged solutions in organization\n"
            "/allSolutions      -   Pass like /allSolutions:true to Export all unmanaged solutions in organization\n"
            "/allOrganizations  -   Pass like /allOrganizations:true to Export all unmanaged solutions in all organizations\n"
            "/managed           -   Required if exporting. Pass 'true' for exporting managed, false for unmanaged\n"
            "/filename          -   Required if importing. Pass full path to solution. If Exporting sets name of exported solution file\n"
            "/workingdir        -   Sets working directory for writing exported solution to file\n"
            "/timeout           -   Set timeout property of OrganizationServiceProxy to this value. Enter integer value which represents the minutes"
        1 
    else
        match argv.[0].ToLowerInvariant() with
        | "export" -> Export argv
        | "import" -> Import argv
        | "publish" -> Publish argv
        | _ -> printf "Either enter 'export', 'import' or 'publish' as first parameter!\n"
               1