namespace Hive.Dependencies

module internal Helpers = 
    /// <summary>Unwraps <c>'a voption voption</c> to <c>'a voption</c></summary>
    let unwrap (option: 'a voption voption) =
        match option with
        | ValueSome(a) -> a
        | ValueNone -> ValueNone

    /// <summary>A shortcut for <c>ValueOption.map2 f a b |> unwrap</c></summary>
    let unwrapMap2 f a b =
        ValueOption.map2 f a b |> unwrap

    /// Gets the keys and values in a map as a ValueTuple seq
    let mapKeyValues (m: Map<'a, 'b>) =
        m |> Seq.map (fun k -> struct(k.Key, k.Value))

    /// Transforms a sequence of Async's to an Async of a sequence
    let asyncSeq seq =
        async {
            let mutable results = []
            for item in seq do
                let! result = item
                results <- results @ [result]
            return results :> seq<_>
        }
        
    /// Transforms a tuple of Async's to an Async of a tuple
    let asyncTuple tup =
        async {
            let struct(a, b) = tup
            let! a = a
            let! b = b
            return struct(a, b)
        }
    /// Transforms a tuple of Async's to an Async of a tuple
    let asyncTupleA tup =
        async {
            let struct(a, b) = tup
            let! a = a
            return struct(a, b)
        }
    /// Transforms a tuple of Async's to an Async of a tuple
    let asyncTupleB tup =
        async {
            let struct(a, b) = tup
            let! b = b
            return struct(a, b)
        }

    /// Compares two values, and returns the larget of them.
    let max comparer a b =
        match comparer a b with
        | v when v > 0 -> a
        | v when v < 0 -> b
        | _ -> a

    /// Finds the largest value in the sequence based on the comparer.
    let maxOfSeq comparer seq =
        seq |> Seq.reduce (max comparer)

    /// Compares two mods by their version.
    let compareMods (access: IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange>) a b =
        access.Compare (access.Version a) (access.Version b)

    /// Fins the mod with the highest version in the sequence.
    let maxMod access seq =
        maxOfSeq (compareMods access) seq