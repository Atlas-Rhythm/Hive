namespace Hive.Dependencies

open FSharp.Control
open System

[<Struct>]
type private ResolveImpl<'Mod, 'ModRef, 'Version, 'VerRange>(access: IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange>) =
    
    /// Collects the references from the specified member using the specified joiner.
    member inline private _.collectTypeReqs mem join mods =
        let access = access
        mods
        |> Seq.collect mem
        |> Seq.fold (fun map (ref: 'ModRef) ->
            let id = access.ID ref in
            match Map.tryFind id map with
            | Some range -> Map.add id (join (ValueSome (access.Range ref)) range) map
            | None -> Map.add id (ValueSome (access.Range ref)) map) Map.empty

    member private this.collectDeps mods =
        this.collectTypeReqs access.Dependencies (Helpers.unwrapMap2 access.And) mods
    member private this.collectConflicts mods =
        this.collectTypeReqs access.Conflicts (ValueOption.map2 access.Either) mods

    member private this.collectReqs mods =
        let access = access
        let deps = this.collectDeps mods
        let conflicts = this.collectConflicts mods
        Helpers.mapMerge (fun a b -> Helpers.unwrapMap2 access.And a (ValueOption.map access.Not b)) deps conflicts
        |> Helpers.mapKeyValues
        |> Seq.map (fun struct(id, range) ->
            match range with
            | ValueSome(r) -> access.CreateRef id r
            | _ -> raise (DependencyRangeInvalidException id))

    static member private resolveLoop (access: IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange>) collectReqs mods =
        async {
            let allRefs = (mods
                |> collectReqs
                |> Seq.filter (fun (r: 'ModRef) -> 
                    Seq.exists (fun (m: 'Mod) -> 
                        (access.ID m) = (access.ID r) && (access.Matches (access.Range r) (access.Version m))
                    ) mods |> not))

            let mods = (mods
                |> Seq.filter (fun (m: 'Mod) ->
                    Seq.exists (fun (r: 'ModRef) -> (access.ID r) = (access.ID m)) allRefs |> not))

            let! moreMods = (allRefs
                    |> Seq.map (fun ref -> struct(ref, access.ModsMatching ref |> Async.AwaitTask))
                    |> Seq.map Helpers.asyncTupleB
                    |> Helpers.asyncSeq)
            let moreMods = (moreMods
                |> Seq.map (fun struct(ref, mods) ->
                    try Ok (Helpers.maxMod access mods)
                    with | _ -> Error ref)
                |> Seq.toList)

            let errors = moreMods |> Seq.choose (function | Error e -> Some e | _ -> None) |> Seq.toList
            if List.length errors > 0 then
                raise (errors
                        |> Seq.map VersionNotFoundException
                        |> Seq.cast
                        |> AggregateException)

            let justMods = moreMods |> Seq.choose (function | Ok v -> Some v | _ -> None) |> Seq.toList
            if List.length justMods > 0 then
                return! (justMods
                    |> Seq.append mods
                    |> ResolveImpl.resolveLoop access collectReqs)
            else 
                return justMods |> Seq.append mods
        }

    member this.resolve mods =
        ResolveImpl.resolveLoop access this.collectReqs mods |> Helpers.asyncStartAsTask

module Resolver =
    let Resolve (access: IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange>) (mods: 'Mod seq) =
        (ResolveImpl access).resolve (Seq.toList mods)
