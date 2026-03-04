namespace ResoniteLink.Builder

open System
open System.Collections.Generic
open ResoniteLink

/// <summary>
/// An opaque slot hierarchy node. Constructed via <see cref="Reso"/> and flattened into
/// <see cref="DataModelOperation"/> batches via <see cref="Reso.addSlotUnder"/> or
/// <see cref="Reso.addSlotsUnder"/>.
/// </summary>
/// <remarks>
/// Parent references and container slot IDs are wired automatically during flattening.
/// Warning: flattening mutates the internal Slots' Parent field in place.
/// </remarks>
[<Struct>]
type SlotNode = private SlotNode of struct (Slot * ResizeArray<Component> * ResizeArray<SlotNode>)


module private Helpers =
    let inline make< ^T, 'V when ^T: (member set_Value: 'V -> unit)> (constructor: unit -> ^T) (value: 'V voption) =
        let field = constructor ()

        value
        |> ValueOption.iter (fun v -> (^T: (member set_Value: 'V -> unit) (field, v)))

        field

open Helpers

/// <summary>
/// EDSL for declaratively constructing ResoniteLink slot hierarchies.
/// </summary>
/// <remarks>
/// <para>Use <c>Reso.slot</c> to create slots with optional properties, components, and children.</para>
/// <para>Use <c>Reso.comp</c> to create components with typed members.</para>
/// <para>Use <c>Reso.addSlotUnder</c> or <c>Reso.addSlotsUnder</c> to flatten the tree into a batch of operations.</para>
/// </remarks>
type Reso =

    static member bool([<Struct>] ?value) : Member = make Field_bool value

    static member str([<Struct>] ?value) : Member = make Field_string value

    static member int([<Struct>] ?value) : Member = make Field_int value

    static member long([<Struct>] ?value) : Member = make Field_long value

    static member float([<Struct>] ?value) : Member = make Field_float value

    static member double([<Struct>] ?value) : Member = make Field_double value

    static member float3([<Struct>] ?value: float3) : Member = make Field_float3 value

    static member floatQ([<Struct>] ?value: floatQ) : Member = make Field_floatQ value

    static member float3(x: float32, y: float32, z: float32) : Member =
        Field_float3(Value = float3 (x = x, y = y, z = z))

    static member floatQ(x: float32, y: float32, z: float32, w: float32) : Member =
        Field_floatQ(Value = (floatQ (x = x, y = y, z = z, w = w)))

    static member reference([<Struct>] ?targetId: string) : Member =
        let m = Reference()
        targetId |> ValueOption.iter (fun v -> m.TargetID <- v)
        m

    /// <summary>
    /// Creates a component with the given member key-value pairs. Returns a curried builder
    /// so component members can be supplied as a list argument outside the parentheses.
    /// </summary>
    /// <example>
    /// <code>
    /// Reso.comp(ComponentType = "[FrooxEngine]FrooxEngine.DynamicField&lt;System.Boolean&gt;")
    ///     [ "Value", Reso.bool true ]
    /// </code>
    /// </example>
    static member comp([<Struct>] ?ID: string, [<Struct>] ?ComponentType: string) =
        fun (members: #seq<struct (string * Member)>) ->
            let dict = Dictionary<string, Member>()

            for struct (key, value) in members do
                dict[key] <- value

            let c = Component(Members = dict)

            ID |> ValueOption.iter (fun id -> c.ID <- id)
            ComponentType |> ValueOption.iter (fun ct -> c.ComponentType <- ct)
            c

    /// <summary>
    /// Creates a slot with the given properties. Returns a curried builder accepting
    /// a list of components then a list of child <see cref="SlotNode"/>s.
    /// A GUID is generated automatically for <c>ID</c> during flattening if the slot
    /// has children or components and no ID was specified.
    /// </summary>
    /// <example>
    /// <code>
    /// Reso.slot(ID = "mySlot", Name = "Hello")
    ///     [ Reso.comp(ComponentType = "SomeType") [ "Enabled", Reso.bool true ] ]
    ///     [ Reso.slot(Name = "Child") [] [] ]
    /// </code>
    /// </example>
    static member slot
        (
            [<Struct>] ?ID: string,
            [<Struct>] ?Name: string,
            [<Struct>] ?IsActive: bool,
            [<Struct>] ?IsPersistent: bool,
            [<Struct>] ?Tag: string,
            [<Struct>] ?Position: float3,
            [<Struct>] ?Rotation: floatQ,
            [<Struct>] ?Scale: float3,
            [<Struct>] ?OrderOffset: int64
        ) =
        fun (components: #seq<Component>) (children: #seq<SlotNode>) ->
            let s = Slot()

            ID |> ValueOption.iter (fun id -> s.ID <- id)
            Name |> ValueOption.iter (fun n -> s.Name <- Field_string(Value = n))
            IsActive |> ValueOption.iter (fun a -> s.IsActive <- Field_bool(Value = a))

            IsPersistent
            |> ValueOption.iter (fun p -> s.IsPersistent <- Field_bool(Value = p))

            Tag |> ValueOption.iter (fun t -> s.Tag <- Field_string(Value = t))
            Position |> ValueOption.iter (fun p -> s.Position <- Field_float3(Value = p))
            Rotation |> ValueOption.iter (fun r -> s.Rotation <- Field_floatQ(Value = r))
            Scale |> ValueOption.iter (fun sc -> s.Scale <- Field_float3(Value = sc))

            OrderOffset
            |> ValueOption.iter (fun o -> s.OrderOffset <- Field_long(Value = o))

            SlotNode(s, ResizeArray(components), ResizeArray(children))

    /// <summary>
    /// Flattens a <see cref="SlotNode"/> tree into a batch of <see cref="DataModelOperation"/>s
    /// parented under the given slot ID.
    /// </summary>
    /// <remarks>
    /// Operations are emitted in pre-order so parent slots exist before their children.
    /// GUIDs are assigned to any slot that needs an ID but was not given one.
    /// Mutates each node's <see cref="Slot.Parent"/> field.
    /// </remarks>
    static member addSlotUnder (parentId: string) (node: SlotNode) : ResizeArray<DataModelOperation> =
        Reso.FlattenNode(parentId, node) |> ResizeArray

    /// <summary>
    /// Flattens multiple sibling <see cref="SlotNode"/> trees into a single operation batch,
    /// all parented under the given slot ID.
    /// </summary>
    static member addSlotsUnder (parentId: string) (nodes: #seq<SlotNode>) : ResizeArray<DataModelOperation> =
        seq {
            for node in nodes do
                yield! Reso.FlattenNode(parentId, node)
        }
        |> ResizeArray

    static member private FlattenNode(parentId: string, node: SlotNode) : seq<DataModelOperation> =
        seq {
            let (SlotNode(slot, components, children)) = node

            if (components.Count > 0 || children.Count > 0) && String.IsNullOrEmpty slot.ID then
                slot.ID <- Guid.NewGuid().ToString()

            slot.Parent <- Reference(TargetID = parentId)

            yield AddSlot(Data = slot)

            for comp in components do
                yield AddComponent(Data = comp, ContainerSlotId = slot.ID)

            for child in children do
                yield! Reso.FlattenNode(slot.ID, child)
        }
