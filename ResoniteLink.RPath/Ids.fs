namespace ResoniteLink.RPath

open System
open ResoniteLink

/// <summary>
/// ID of a field's <c>Value</c> member (not of the component that owns it), for ProtoGraph ports that read or drive a value.
/// </summary>
/// <typeparam name="t">The type of value held by the field</typeparam>
/// <remarks><c>default</c> (or <see cref="FieldId.none"/>) is an unset ID, which leaves a port unwired.</remarks>
[<Struct>]
type FieldId<'t> =
    internal
    | FieldId of fieldId: string

    /// The raw ID, or null when unset
    member this.Value =
        let (FieldId id) = this
        id

/// ID of a slot, for ProtoGraph ports that reference a <c>Slot</c>. <c>default</c> is an unset ID.
[<Struct>]
type SlotId =
    internal
    | SlotId of slotId: string

    /// The raw ID, or null when unset
    member this.Value =
        let (SlotId id) = this
        id
        
    static member Root = SlotId Slot.ROOT_SLOT_ID

/// ID of a component, for ProtoGraph ports that reference a component. <c>default</c> is an unset ID.
[<Struct>]
type ComponentId =
    internal
    | ComponentId of componentId: string

    /// The raw ID, or null when unset
    member this.Value =
        let (ComponentId id) = this
        id

module private IdValidation =
    let nonEmpty (id: string) =
        if String.IsNullOrWhiteSpace id then
            invalidArg (nameof id) "Resonite IDs must be non-empty strings. Use _.None for an explicitly empty ID"

        id

[<RequireQualifiedAccess>]
module FieldId =
    /// Unset ID, which leaves the port unwired
    let None<'t> : FieldId<'t> = Unchecked.defaultof<FieldId<'t>>

    /// Wrap a raw Resonite ID of a field's <c>Value</c> member
    let ofString<'t> (id: string) : FieldId<'t> = FieldId(IdValidation.nonEmpty id)

    /// A new unique ID, to assign to a field's <c>Value</c> member and to pass to a port that wires to it
    let Fresh<'t> () : FieldId<'t> =
        FieldId(typeof<'t>.Name + "_" + Guid.NewGuid().ToString())

[<RequireQualifiedAccess>]
module SlotId =
    /// Unset ID, which leaves the port unwired
    let None = Unchecked.defaultof<SlotId>

    /// Wrap a raw Resonite slot ID
    let ofString (id: string) : SlotId = SlotId(IdValidation.nonEmpty id)

    /// A new unique ID, to assign to a slot and to pass to a required reference
    let Fresh<'t> () : SlotId =
        SlotId("Slot_" + Guid.NewGuid().ToString())

[<RequireQualifiedAccess>]
module ComponentId =
    /// Unset ID, which leaves the port unwired
    let None = Unchecked.defaultof<ComponentId>

    /// Wrap a raw Resonite component ID
    let ofString (id: string) : ComponentId = ComponentId(IdValidation.nonEmpty id)

    /// A new unique ID, to assign to a component and to pass to a required reference
    let Fresh<'t> () : ComponentId =
        ComponentId("Component_" + Guid.NewGuid().ToString())