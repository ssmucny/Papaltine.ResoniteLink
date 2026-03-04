# ResoniteLink.Builder

A declarative EDSL for constructing [ResoniteLink](https://github.com/Yellow-Dog-Man/ResoniteLink) slot hierarchies. Define nested slots, components, and members as a tree, then flatten the result into `DataModelOperation` batches ready to send over the link.

## Quick start

```fsharp
open ResoniteLink
open ResoniteLink.Builder

let tree =
    Reso.slot (Name = "Greeting", IsActive = true)
        [ Reso.comp (ComponentType = "[FrooxEngine]FrooxEngine.DynamicField<System.String>")
              [ "Value", Reso.str "Hello, world!" ] ]
        [ Reso.slot (Name = "Child") [] [] ]

tree
|> Reso.addSlotUnder Slot.ROOT_SLOT_ID
|> link.RunDataModelOperationBatch
```

## API overview

### Members

Create typed field values to attach to components:

```fsharp
Reso.bool true
Reso.str "hello"
Reso.int 42
Reso.long 100L
Reso.float 1.5f
Reso.double 3.14
Reso.float3 (1.0f, 2.0f, 3.0f)
Reso.floatQ (0.0f, 0.0f, 0.0f, 1.0f)
Reso.reference "some-target-id"
```

### Components

Build components with `Reso.comp`. The member list is a separate argument so it can be placed outside the parentheses:

```fsharp
Reso.comp (ComponentType = "[FrooxEngine]FrooxEngine.DynamicField<System.Boolean>")
    [ "Enabled", Reso.bool true
      "Value",   Reso.bool false ]
```

### Slots

Build slots with `Reso.slot`. Components and children are the second and third arguments respectively:

```fsharp
Reso.slot (Name = "Parent", Tag = "myTag")
    [] // components (empty here)
    [ Reso.slot (Name = "ChildA") [] []
      Reso.slot (Name = "ChildB") [] [] ]
```

All slot properties are optional: `ID`, `Name`, `IsActive`, `IsPersistent`, `Tag`, `Position`, `Rotation`, `Scale`, `OrderOffset`. IDs are auto-generated (GUID) during flattening for any slot that has children or components.

### Generating children from sequences

Because the children and component arguments are plain sequences, comprehensions work naturally:

```fsharp
Reso.slot (Name = "Generated") []
    [ for i in 1..5 -> Reso.slot (Name = $"Child{i}", OrderOffset = int64 i) [] [] ]
```

### Flattening

Flatten a tree into a batch of `DataModelOperation`s parented under a given slot ID:

```fsharp
// Single root node
let ops = Reso.addSlotUnder parentId node

// Multiple sibling nodes
let ops = Reso.addSlotsUnder parentId [ node1; node2; node3 ]
```

Operations are emitted in pre-order traversal.

## Items to note

- Declarative: Describe what the data looks like; not how to build it
- Composable: `SlotNode` trees are ordinary values; combine or generate them with standard F# expressions.
  -  Be careful if you plan on reusing SlotNodes, because the slots will have IDs (if not specified) set when operations are generated. This can cause duplicate ID issues if you add slots multiple times.
- .NET Standard 2.0: compatible with .NET and .NET Framework (Unity).
