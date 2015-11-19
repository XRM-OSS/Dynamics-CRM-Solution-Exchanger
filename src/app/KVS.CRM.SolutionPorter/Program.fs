module KVS.Crm.SolutionPorterMain

open KVS.Crm.SolutionPorter
open System.Globalization

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

    if url.IsNone || user.IsNone || password.IsNone || solution.IsNone || managedString.IsNone then
        printf "Values missing: Needed /url (%s), /user (%s), /password (%s), /solution (%s) and /managed (%s)" (OptionToString url) (OptionToString user) (OptionToString password) (OptionToString solution) (OptionToString managedString)
        1
    else
        let managed = bool.Parse managedString.Value

        let solution = ExportSolution (fun cred ->
                         { cred with
                            Password = password.Value
                            Username = user.Value
                            Url = url.Value
                         }) solution.Value managed

        let solutionName = if filename.IsNone then "Solution.zip" else filename.Value
        let dir = if workingDir.IsNone then "" else workingDir.Value
        
        WriteSolutionToFile solutionName solution dir
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
        printf "%s%s%s%s%s%s%s%s" 
            "Usage SolutionManager.exe [Export | Import | Publish] [/user: | /password: | /url: | /solution: | /managed: | /filename: | /workingdir:]\n" 
            "/user       -   Required. Username for authenticating with CRM endpoint\n"
            "/password   -   Required. Password for authenticating with CRM endpoint\n"
            "/url        -   Required. Url of CRM endpoint\n"
            "/solution   -   Required if exporting. Unique name of solution to export\n"
            "/managed    -   Required if exporting. Pass 'true' for exporting managed, false for unmanaged\n"
            "/filename   -   Required if importing. Pass full path to solution. If Exporting sets name of exported solution file\n"
            "/workingdir -   Sets working directory for writing exported solution to file\n"
        1 
    else
        match argv.[0].ToLowerInvariant() with
        | "export" -> Export argv
        | "import" -> Import argv
        | "publish" -> Publish argv
        | _ -> printf "Either enter 'export', 'import' or 'publish' as first parameter!"
               1