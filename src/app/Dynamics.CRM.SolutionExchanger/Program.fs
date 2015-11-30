module Dynamics.Crm.SolutionExchangerMain

open Dynamics.Crm.SolutionExchanger
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
let OptionToString (opt : string option)=
    match opt with
    | Some str -> "Present"
    | None -> "Missing"

/// Used for exporting solution from CRM and saving it to file
let Export args =
    let url = FindOption "/url:" args
    let user = FindOption "/user:" args
    let password = FindOption "/password:" args
    let solution = FindOption "/solution:" args
    let managedString = FindOption "/managed:" args
    let workingDir = FindOption "/workingdir:" args
    let filename = FindOption "/filename:" args
    let allSolutionsText = FindOption "/allSolutions:" args
    let allOrganizationsText = FindOption "/allOrganizations:" args
    let managed = bool.Parse managedString.Value

    let allSolutions = 
        if allSolutionsText.IsSome then
            let parsedSuccess, parsedValue = bool.TryParse(allSolutionsText.Value) 
            if parsedSuccess then
                printf "Set all solutions to %b\n" parsedValue
                Some parsedValue
            else
                printf "%s" "Failed to parse all solutions flag\n"
                None 
        else
            None

    let allOrganizations = 
        if allOrganizationsText.IsSome then
            let parsedSuccess, parsedValue = bool.TryParse(allOrganizationsText.Value) 
            if parsedSuccess then
                printf "Set all organizations to %b\n" parsedValue
                Some parsedValue
            else
                printf "%s" "Failed to parse all organizations flag\n"
                None 
        else
            None

    if url.IsNone || user.IsNone || password.IsNone || (solution.IsNone && (allSolutions.IsNone || not allSolutions.Value) && (allOrganizations.IsNone || not allOrganizations.Value)) || managedString.IsNone then
        printf "Values missing: Needed /url (%s), /user (%s), /password (%s), (/solution (%s) or /allSolutions:true or allOrganizations:true) and /managed (%s)" (OptionToString url) (OptionToString user) (OptionToString password) (OptionToString solution) (OptionToString managedString)
        1
    else
        if allSolutions.IsSome && allSolutions.Value then
            let solutions = ExportAllSolutions (fun cred ->
                                { cred with
                                    Password = password.Value
                                    Username = user.Value
                                    Url = url.Value
                                }) managed

            let dir = if workingDir.IsNone then "" else workingDir.Value
            
            solutions
                |> Seq.iter (fun (solution, uniqueName) -> 
                    if solution.IsSome then
                        WriteSolutionToFile (uniqueName + ".zip") solution.Value dir
                    else
                        printf "Skipping solution %s, since it has no value." uniqueName)
            0
        else if allOrganizations.IsSome && allOrganizations.Value then
            let solutions = ExportAllOrganizations (fun cred ->
                                { cred with
                                    Password = password.Value
                                    Username = user.Value
                                    Url = url.Value
                                }) managed

            let dir = if workingDir.IsNone then "" else workingDir.Value
            
            solutions
                |> Seq.iter(fun (friendlyName, solutions) ->
                    solutions
                        |> Seq.iter(fun (solution, uniqueName) -> 
                            if solution.IsSome then 
                                WriteSolutionToFile (uniqueName + ".zip") solution.Value (Path.Combine(dir, friendlyName))
                            else
                                printf "Skipping solution %s, since it has no value." uniqueName))
            0
        else
            let solution = ExportSolution (fun cred ->
                                { cred with
                                    Password = password.Value
                                    Username = user.Value
                                    Url = url.Value
                                }) solution.Value managed

            if solution.IsNone then
                failwith "Failed to export solution. Please check, whether the solution might be corrupted. Exiting"

            let solutionName = if filename.IsNone then "Solution.zip" else filename.Value
            let dir = if workingDir.IsNone then "" else workingDir.Value
        
            WriteSolutionToFile solutionName solution.Value dir
            0
                
            
/// Used for importing solution to CRM
let Import args = 
    let url = FindOption "/url:" args
    let user = FindOption "/user:" args
    let password = FindOption "/password:" args
    let filename = FindOption "/filename:" args

    if url.IsNone || user.IsNone || password.IsNone || filename.IsNone then
        printf "Values missing: Needed /url (%s), /user (%s), /password (%s) and /filename (%s)" (OptionToString url) (OptionToString user) (OptionToString password) (OptionToString filename)
        1
    else
        ImportSolution (fun cred ->
                             { cred with
                                    Password = password.Value
                                    Username = user.Value
                                    Url = url.Value
                             }) filename.Value
        0

/// Used for publishing all customizations in CRM
let Publish args =
    let url = FindOption "/url:" args
    let user = FindOption "/user:" args
    let password = FindOption "/password:" args

    if url.IsNone || user.IsNone || password.IsNone then
        printf "Values missing: Needed /url (%s), /user (%s) and /password (%s)" (OptionToString url) (OptionToString user) (OptionToString password)
        1
    else
        PublishAll (fun cred ->
                        { cred with
                            Password = password.Value
                            Username = user.Value
                            Url = url.Value
                        })
        0

[<EntryPoint>]
let main argv = 
    if argv.Length < 1 then 
        printf "%s%s%s%s%s%s%s%s%s%s" 
            "Usage SolutionExchanger.exe [Export | Import | Publish] [/user: | /password: | /url: | /solution: | /managed: | /filename: | /workingdir: | /allSolutions: | /allOrganizations:]\n" 
            "/user              -   Required. Username for authenticating with CRM endpoint\n"
            "/password          -   Required. Password for authenticating with CRM endpoint\n"
            "/url               -   Required. Url of CRM endpoint\n"
            "/solution          -   Required if exporting. Unique name of solution to export. Can be replaced by allSolutions to get all unmanaged solutions in organization\n"
            "/allSolutions      -   Pass like /allSolutions:true to Export all unmanaged solutions in organization\n"
            "/allOrganizations  -   Pass like /allOrganizations:true to Export all unmanaged solutions in all organizations\n"
            "/managed           -   Required if exporting. Pass 'true' for exporting managed, false for unmanaged\n"
            "/filename          -   Required if importing. Pass full path to solution. If Exporting sets name of exported solution file\n"
            "/workingdir        -   Sets working directory for writing exported solution to file\n"
        1 
    else
        match argv.[0].ToLowerInvariant() with
        | "export" -> Export argv
        | "import" -> Import argv
        | "publish" -> Publish argv
        | _ -> printf "Either enter 'export', 'import' or 'publish' as first parameter!"
               1