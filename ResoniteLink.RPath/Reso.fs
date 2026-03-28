namespace ResoniteLink.RPath

open System
open System.Collections.Generic
open ResoniteLink

module private Helpers =
    let inline make< ^T, 'V when ^T: (member set_Value: 'V -> unit)> (constructor: unit -> ^T) (value: 'V voption) =
        let field = constructor ()

        value
        |> ValueOption.iter (fun v -> (^T: (member set_Value: 'V -> unit) (field, v)))

        field

open Helpers

/// <summary>
/// EDSL for declaratively constructing ResoniteLink slot hierarchies and components.
/// </summary>
type Reso =

    static member bool([<Struct>] ?value) : Member = make Field_bool value

    static member str([<Struct>] ?value) : Member = make Field_string value

    static member int([<Struct>] ?value) : Member = make Field_int value
    static member int2([<Struct>] ?value) : Member = make Field_int2 value
    static member int3([<Struct>] ?value) : Member = make Field_int3 value
    static member int4([<Struct>] ?value) : Member = make Field_int4 value

    static member long([<Struct>] ?value) : Member = make Field_long value
    static member long2([<Struct>] ?value) : Member = make Field_long2 value
    static member long3([<Struct>] ?value) : Member = make Field_long3 value
    static member long4([<Struct>] ?value) : Member = make Field_long4 value

    static member double([<Struct>] ?value) : Member = make Field_double value
    static member double2([<Struct>] ?value) : Member = make Field_double2 value
    static member double3([<Struct>] ?value) : Member = make Field_double3 value
    static member double4([<Struct>] ?value) : Member = make Field_double4 value

    static member float([<Struct>] ?value) : Member = make Field_float value
    static member float2([<Struct>] ?value) : Member = make Field_float2 value
    static member float3([<Struct>] ?value) : Member = make Field_float3 value
    static member float4([<Struct>] ?value) : Member = make Field_float4 value

    static member floatQ([<Struct>] ?value) : Member = make Field_floatQ value

    static member float2(x: float32, y: float32) : Member =
        Field_float2(Value = float2 (x = x, y = y))

    static member float3(x: float32, y: float32, z: float32) : Member =
        Field_float3(Value = float3 (x = x, y = y, z = z))

    static member float4(x: float32, y: float32, z: float32, w: float32) : Member =
        Field_float4(Value = (float4 (x = x, y = y, z = z, w = w)))

    static member double2(x: float, y: float) : Member =
        Field_double2(Value = double2 (x = x, y = y))

    static member double3(x: float, y: float, z: float) : Member =
        Field_double3(Value = double3 (x = x, y = y, z = z))

    static member double4(x: float, y: float, z: float, w: float) : Member =
        Field_double4(Value = (double4 (x = x, y = y, z = z, w = w)))

    static member int2(x: int32, y: int32) : Member = Field_int2(Value = int2 (x = x, y = y))

    static member int3(x: int32, y: int32, z: int32) : Member =
        Field_int3(Value = int3 (x = x, y = y, z = z))

    static member int4(x: int32, y: int32, z: int32, w: int32) : Member =
        Field_int4(Value = (int4 (x = x, y = y, z = z, w = w)))

    static member long2(x: int64, y: int64) : Member =
        Field_long2(Value = long2 (x = x, y = y))

    static member long3(x: int64, y: int64, z: int64) : Member =
        Field_long3(Value = long3 (x = x, y = y, z = z))

    static member long4(x: int64, y: int64, z: int64, w: int64) : Member =
        Field_long4(Value = (long4 (x = x, y = y, z = z, w = w)))

    static member floatQ(x: float32, y: float32, z: float32, w: float32) : Member =
        Field_floatQ(Value = (floatQ (x = x, y = y, z = z, w = w)))

    static member reference([<Struct>] ?targetId: string) : Member =
        let memberRef = Reference()
        targetId |> ValueOption.iter (fun v -> memberRef.TargetID <- v)
        memberRef

    /// <summary>
    /// Creates a component with the given member key-value pairs.
    /// </summary>
    static member comp([<Struct>] ?ID: string, [<Struct>] ?ComponentType: string) =
        fun (members: #seq<struct (string * Member)>) ->
            let dict = Dictionary<string, Member>()

            for struct (key, value) in members do
                dict[key] <- value

            let builtComponent = Component(Members = dict)

            ID |> ValueOption.iter (fun id -> builtComponent.ID <- id)
            ComponentType |> ValueOption.iter (fun ct -> builtComponent.ComponentType <- ct)
            builtComponent

    /// <summary>
    /// Creates a slot with the given properties.
    /// </summary>
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
        fun (components: #seq<Component>) (children: #seq<Slot>) ->
            let slot = Slot()

            ID |> ValueOption.iter (fun id -> slot.ID <- id)
            Name |> ValueOption.iter (fun n -> slot.Name <- Field_string(Value = n))
            IsActive |> ValueOption.iter (fun a -> slot.IsActive <- Field_bool(Value = a))

            IsPersistent
            |> ValueOption.iter (fun p -> slot.IsPersistent <- Field_bool(Value = p))

            Tag |> ValueOption.iter (fun t -> slot.Tag <- Field_string(Value = t))
            Position |> ValueOption.iter (fun p -> slot.Position <- Field_float3(Value = p))
            Rotation |> ValueOption.iter (fun r -> slot.Rotation <- Field_floatQ(Value = r))
            Scale |> ValueOption.iter (fun sc -> slot.Scale <- Field_float3(Value = sc))

            OrderOffset
            |> ValueOption.iter (fun o -> slot.OrderOffset <- Field_long(Value = o))

            slot.Children <- ResizeArray children
            slot.Components <- ResizeArray components

            slot

    /// <summary>
    /// Prefix for locally generated IDs to indicate that they need Add operations.
    /// </summary>
    static member internal FreshIdPrefix = "toAdd_"

    /// <summary>
    /// Generates a fresh local ID for new slots/components that reference each other.
    /// </summary>
    static member newID() : string =
        Reso.FreshIdPrefix + Guid.NewGuid().ToString()
