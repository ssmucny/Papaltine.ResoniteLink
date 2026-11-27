namespace ResoniteLink.RPath

open System.Collections.Generic
open System.Text.Json.Serialization
open ResoniteLink

module private Helper =
    let value (c: ComponentId<_>) = c.Value

/// <summary>
/// Typed wrapper for Component that can be parameterized by the component type it represents. Able to get the typed ID with TypedID() method.
/// Able to use anywhere a Component is used.
/// </summary>
/// <remarks>Some type inference locations may require an explicit upcast/Erased to properly infer the supertype.</remarks>
[<Sealed>]
type Component<'t>(ComponentType: string, ?ID: string, ?Members: Dictionary<string, Member>, [<Struct>] ?IsReferenceOnly: bool) =
    inherit Component(
        ID = Option.defaultWith (fun () -> let c = ComponentId.Fresh<'t>() in c.Value) ID,
        ComponentType = ComponentType,
        Members = Option.defaultWith Dictionary Members,
        IsReferenceOnly = defaultValueArg IsReferenceOnly false)
    
    member this.TypedID() =
        ComponentId<'t> this.ID
    
    /// Up casts to the normal Component type. Same as `c :> Component`.
    [<JsonIgnore>]
    member this.Erased = this :> Component