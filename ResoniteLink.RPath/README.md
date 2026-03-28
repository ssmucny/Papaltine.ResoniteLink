# RPath

RPath is a composable query EDSL for [ResoniteLink](https://github.com/Yellow-Dog-Man/ResoniteLink).
It gives you a lazy `Query<'T>` pipeline for traversing the Resonite data model and shaping results with
F#-friendly combinators and C# extension methods.

## Package

- NuGet package: `Papaltine.ResoniteLink.RPath`
- Compatible with `.NET Standard 2.0`

```bash
dotnet add package Papaltine.ResoniteLink.RPath --version 0.3.0
```

## Core model

`Query<'T>` is an immutable query value:

- Building a query is lazy and does not contact the websocket.
- Executing a query requires a `LinkInterface`.
- Query values are composable and reusable.

In F#, `bind`/`>>=` make query-expanding steps explicit, while `map`/`filter`/`flatMap` are in-memory transformations.

## API surfaces

| Surface                                                   | Namespace                      | Primary use                        |
|-----------------------------------------------------------|--------------------------------|------------------------------------|
| `Query` module                                            | `ResoniteLink.RPath.Query`     | F# pipelines and combinators       |
| `Operators` module (`>>=`, `>=>`)                         | `ResoniteLink.RPath.Operators` | F# monadic style                   |
| `Slot` module wrappers                                    | `ResoniteLink.RPath.Slot`      | F# slot-rooted traversals          |
| F# extension methods                                      | `ResoniteLink.RPath`           | Method-chaining style in F# and C# |
| C# LINQ-style extensions (`Select`, `Where`, `Bind`, ...) | `ResoniteLink.RPath.CSharp`    | Idiomatic C# query style           |

## Namespaces

### F#

```fsharp
open ResoniteLink
open ResoniteLink.RPath
open ResoniteLink.RPath.Query       // Use `Query` module combinators without Query. prefix
open ResoniteLink.RPath.Operators   // for `>>=` and `>=>`
```

### C#

```csharp
using ResoniteLink;
using ResoniteLink.RPath;        // traversal + execution extensions
using ResoniteLink.RPath.CSharp; // Select/Where/Bind/SelectMany
```

## Quick start

### F# module style

```fsharp
open ResoniteLink
open ResoniteLink.RPath

let namesQuery =
    Query.root
    |> Query.childrenLite
    |> Query.filter _.Name.Value.Contains("Proxy")
    |> Query.map _.Name.Value
    |> Query.take 10

let names = Query.toArray namesQuery link
```

### F# extension style

```fsharp
open ResoniteLink
open ResoniteLink.RPath

let names =
    Query.root
        .Children(false)
        .Filter(_.Name.Value.Contains("Proxy"))
        .Map(_.Name.Value)
        .Take(10)
        .ToArray(link)
```

### C# style

```csharp
using ResoniteLink;
using ResoniteLink.RPath;
using ResoniteLink.RPath.CSharp;

var names = await Query.Root
    .Children(includeComponents: false)
    .Where(slot => slot.Name.Value.Contains("Proxy"))
    .Select(slot => slot.Name.Value)
    .Take(10)
    .ToArray(link);
```

## Traversal APIs

For `Query<Slot>`:

- `children includeComponents` / `childrenLite` / `childrenFull`
- `parent includeComponents` / `parentLite` / `parentFull`
- `ancestors includeComponents` / `ancestorsLite` / `ancestorsFull`
- `descendants includeComponents` / `descendantsLite` / `descendantsFull`
- `ancestorsAndSelf includeComponents` / `ancestorsAndSelfLite` / `ancestorsAndSelfFull`
- `descendantsAndSelf includeComponents` / `descendantsAndSelfLite` / `descendantsAndSelfFull`

For a single `Slot`, the `Slot` module provides equivalent wrappers with similar names.

Extension methods expose traversal as `.Children(...)`, `.Parent(...)`, `.Ancestors(...)`,
`.Descendants(...)`, `.AncestorsAndSelf(...)`, and `.DescendantsAndSelf(...)`.

## Component and member APIs

- `Query.components` / `.Components()`
- `Query.ofType` / `.OfType(typeName)`
- `Query.getMember<'T>` / `.Member<'T>(memberName)`
- `Query.dereferenceSlot`, `dereferenceSlotLite`, `dereferenceSlotFull` / `.DereferenceSlot(...)`
- `Query.dereferenceComponent` / `.DereferenceComponent()`

`getMember<'T>` / `Member<T>` skip values that are missing or incompatible with the requested member type.

## Composition and shaping

Query-producing composition:

- `bind`
- `Operators.(>>=)`
- `Operators.(>=>)`

Note that the bind operators do not batch any requests by themselves as you provide the mapping function. Use one of the
axis combinators directly to get batching behavior.

## Execution and error handling

Execution functions:

- `runAsync` / `.RunAsync(link)`
- `run` / `.Run(link)`
- `toArray` / `.ToArray(link)`
- `toResizeArray` / `.ToList(link)`
- `first` / `.First(link)`
- `firstOr` / `.FirstOr(defaultValue, link)`
- `toResult` / `.ToResult(link)`

`toResult`/`ToResult` convert `ResoniteLinkException` to `Result.Error`.
Exceptions thrown inside your own mapping/filtering code still propagate normally.

## Notes

- `take`, `skip`, and `slice` use F# sequence semantics.
  - `take` and `skip` throw if the source sequence has too few items.
  - Use `mapAll (Seq.truncate n)` when you need a non-throwing cap.
- Traversal returns query results and is not server-side pagination.
- Build queries once, execute many times by passing whichever `LinkInterface` you want at execution.
