module ConsoleApp2.QueryValidation

open System
open System.Threading
open System.Threading.Tasks
open ResoniteLink
open ResoniteLink.RPath
open ResoniteLink.RPath.Operators

let run (endpoint: Uri) =
    let runValueTask (valueTask: ValueTask<'T>) =
        valueTask.AsTask().GetAwaiter().GetResult()

    let runTask (task: Task) = task.GetAwaiter().GetResult()

    let assertTrue message condition =
        if not condition then
            failwith message

    let assertArrayEqual message (expected: 'T[]) (actual: 'T[]) =
        if expected <> actual then
            failwithf "%s\nExpected: %A\nActual: %A" message expected actual

    let slotIds (slots: Slot[]) = slots |> Array.map _.ID
    let componentIds (components: Component[]) = components |> Array.map _.ID

    let previewLimit = 8

    let preview (items: 'T[]) =
        if items.Length <= previewLimit then
            items
        else
            items[.. previewLimit - 1]

    let printPreview label (toText: 'T -> string) (items: 'T[]) =
        let shown = items |> preview |> Array.map toText
        let suffix = if items.Length > shown.Length then "; ..." else ""
        printfn "%s (%d): [%s%s]" label items.Length (String.concat "; " shown) suffix

    let printOptionalSlot label (slot: Slot option) =
        match slot with
        | Some s -> printfn "%s: %s (%s)" label s.Name.Value s.ID
        | None -> printfn "%s: <none>" label

    let printTypeBreakdown (components: Component[]) =
        components
        |> Array.countBy _.ComponentType
        |> Array.sortByDescending snd
        |> preview
        |> Array.iter (fun (componentType, count) -> printfn "  - %s (%d)" componentType count)

    printfn "Connecting to %s" (endpoint.ToString())

    let link = new LinkInterface()
    runTask (link.Connect(endpoint, CancellationToken.None))

    let runToArray query =
        Query.toArray query link |> runValueTask

    let runFirst query = Query.first query link |> runValueTask

    let runFirstOr defaultValue query =
        Query.firstOr defaultValue query link |> runValueTask

    let runToResult query =
        Query.toResult query link |> runValueTask

    assertTrue "Unable to connect to websocket endpoint." link.IsConnected
    printfn "Connected."

    let rootSlot =
        Query.root
        |> runFirst
        |> Option.defaultWith (fun () -> failwith "Root query did not return a root slot.")

    printfn "Root slot: %s (%s)" rootSlot.Name.Value rootSlot.ID

    printfn "\n--- Traversal parity: module vs extension methods ---"

    let moduleChildren =
        Query.root |> Query.childrenLite |> Query.mapAll (Seq.truncate 25) |> runToArray

    let extensionChildren =
        Query.root.Children(false).MapAll(Seq.truncate 25).ToArray(link) |> runValueTask

    printPreview "moduleChildren IDs" id (slotIds moduleChildren)
    printPreview "moduleChildren names" id (moduleChildren |> Array.map _.Name.Value)
    printPreview "extensionChildren IDs" id (slotIds extensionChildren)

    assertArrayEqual
        "childrenLite and .Children(false) should return the same slots in the same order."
        (slotIds moduleChildren)
        (slotIds extensionChildren)

    printfn "Validated children parity across %d rows." moduleChildren.Length

    printfn "\n--- Operator parity: >>= and >=> ---"
    let moduleOperatorChain = Slot.childrenLite >=> Slot.parentLite

    let viaOperators =
        Query.root
        >>= moduleOperatorChain
        |> Query.mapAll (Seq.truncate 25)
        |> Query.map _.ID
        |> runToArray

    let viaBind =
        Query.root
        |> Query.bind Slot.childrenLite
        |> Query.bind Slot.parentLite
        |> Query.mapAll (Seq.truncate 25)
        |> Query.map _.ID
        |> runToArray

    printPreview "viaOperators IDs" id viaOperators
    printPreview "viaBind IDs" id viaBind

    assertArrayEqual "Operator composition should match explicit bind composition." viaBind viaOperators
    printfn "Validated >>= / >=> composition parity."

    printfn "\n--- Pure combinators: map, filter, flatMap, andThen ---"

    let collectTokens (names: string seq) : ValueTask<string seq> =
        task {
            return
                names
                |> Seq.collect (fun name -> name.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) :> seq<string>)
                |> Seq.filter (fun token -> token.Length >= 3)
                |> Seq.distinct
                |> Seq.sort
                |> Seq.truncate 20
        }
        |> ValueTask<string seq>

    let moduleTokens =
        Query.root
        |> Query.childrenLite
        |> Query.mapAll (Seq.truncate 60)
        |> Query.map _.Name.Value
        |> Query.andThen collectTokens
        |> runToArray

    let extensionTokens =
        Query.root.Children(false).MapAll(Seq.truncate 60).Map(_.Name.Value).AndThen(collectTokens).ToArray(link)
        |> runValueTask

    printPreview "moduleTokens" id moduleTokens
    printPreview "extensionTokens" id extensionTokens

    assertArrayEqual "Module and extension token pipelines should produce the same values." moduleTokens extensionTokens
    printfn "Validated map/filter/flatMap-style shaping via andThen parity (%d tokens)." moduleTokens.Length

    printfn "\n--- Sequence shaping: take, skip, slice, itemAt, first, firstOr ---"
    let childrenIds = Query.root |> Query.childrenLite |> Query.map _.ID |> runToArray

    printPreview "childrenIds" id childrenIds

    assertTrue "Expected at least 4 root children to validate slice/itemAt examples." (childrenIds.Length >= 4)

    let expectedSlice = childrenIds |> Array.skip 1 |> Array.take 2

    let moduleSlice =
        Query.root
        |> Query.childrenLite
        |> Query.map _.ID
        |> Query.slice (1, 3)
        |> runToArray

    let extensionSlice =
        Query.root.Children(false).Map(_.ID).Slice(1, 3).ToArray(link) |> runValueTask

    printPreview "expectedSlice" id expectedSlice
    printPreview "moduleSlice" id moduleSlice
    printPreview "extensionSlice" id extensionSlice

    assertArrayEqual "slice should match in-memory expectation." expectedSlice moduleSlice
    assertArrayEqual "Query.slice and .Slice should return identical output." moduleSlice extensionSlice
    printfn "Validated slice parity and consistency with in-memory expectation: %A." expectedSlice

    let moduleItem = Query.root |> Query.childrenLite |> Query.itemAt 2 |> runFirst

    let extensionItem = Query.root.Children(false).At(2).First(link) |> runValueTask

    printOptionalSlot "moduleItem" moduleItem
    printOptionalSlot "extensionItem" extensionItem

    let moduleItemID = moduleItem |> Option.map _.ID
    let extensionItemID = extensionItem |> Option.map _.ID

    if moduleItemID <> extensionItemID then
        failwithf
            "itemAt and .At should target the same element.\nExpected: %A\nActual:   %A"
            moduleItemID
            extensionItemID

    let firstFromEmptyModule = Query.empty<Slot> |> runFirstOr rootSlot

    let firstFromEmptyExtension =
        (Query.empty<Slot>).FirstOr(rootSlot, link) |> runValueTask

    let firstFromEmptyExtensionSlot: Slot = firstFromEmptyExtension

    printfn "firstFromEmptyModule fallback ID: %s" firstFromEmptyModule.ID
    printfn "firstFromEmptyExtension fallback ID: %s" firstFromEmptyExtensionSlot.ID

    if firstFromEmptyModule.ID <> firstFromEmptyExtensionSlot.ID then
        failwithf
            "firstOr and .FirstOr should agree on fallback values.\nExpected: %A\nActual:   %A"
            firstFromEmptyModule.ID
            firstFromEmptyExtensionSlot.ID

    printfn "Validated shape/materialization combinators."

    printfn "\n--- Component APIs: components, ofType, getMember, dereferenceSlot ---"
    let sampledBranch = Query.root |> Query.childrenLite |> Query.itemAt 0

    let sampledBranchSlot =
        sampledBranch
        |> runFirst
        |> Option.defaultWith (fun () -> failwith "Expected root to have at least one child for component diagnostics.")

    printfn "Sampled branch slot: %s (%s)" sampledBranchSlot.Name.Value sampledBranchSlot.ID

    let sampledComponents =
        sampledBranch
        |> Query.descendantsAndSelfLite
        |> Query.mapAll (Seq.truncate 120)
        |> Query.components
        |> Query.mapAll (Seq.truncate 120)
        |> runToArray

    assertTrue "Expected to discover at least one component in the current session." (sampledComponents.Length > 0)
    printPreview "sampledComponents IDs" id (componentIds sampledComponents)
    printfn "Top sampled component types:"
    printTypeBreakdown sampledComponents
    let sampledType = sampledComponents[0].ComponentType
    printfn "Selected component type for ofType parity: %s" sampledType

    let moduleTypedComponents =
        sampledBranch
        |> Query.descendantsAndSelfLite
        |> Query.mapAll (Seq.truncate 120)
        |> Query.components
        |> Query.ofType sampledType
        |> Query.mapAll (Seq.truncate 30)
        |> runToArray

    let extensionTypedComponents =
        Query.root
            .Children(false)
            .At(0)
            .DescendantsAndSelf(false)
            .MapAll(Seq.truncate 120)
            .Components()
            .OfType(sampledType)
            .MapAll(Seq.truncate 30)
            .ToArray(link)
        |> runValueTask

    printPreview "moduleTypedComponents IDs" id (componentIds moduleTypedComponents)
    printPreview "extensionTypedComponents IDs" id (componentIds extensionTypedComponents)

    assertTrue
        "ofType should only return components of the requested type."
        (moduleTypedComponents |> Array.forall (fun c -> c.ComponentType = sampledType))

    assertArrayEqual
        "Query.ofType and .OfType should return equivalent components."
        (componentIds moduleTypedComponents)
        (componentIds extensionTypedComponents)

    let referenceMemberCandidate =
        sampledComponents
        |> Array.tryPick (fun comp ->
            comp.Members
            |> Seq.tryPick (fun pair ->
                match pair.Value with
                | :? Reference as reference when not (isNull reference.TargetID) -> Some(comp.ID, pair.Key)
                | _ -> None))

    match referenceMemberCandidate with
    | Some(componentID, memberName) ->
        printfn "Reference member candidate: component=%s member=%s" componentID memberName

        let moduleDereferencedTargets =
            Query.findComponentByID componentID
            |> Query.getMember<Reference> memberName
            |> Query.dereferenceComponent
            |> Query.mapAll (Seq.truncate 20)
            |> Query.map _.ID
            |> runToArray

        let extensionDereferencedTargets =
            (Query.findComponentByID componentID)
                .Member<Reference>(memberName)
                .DereferenceComponent()
                .MapAll(Seq.truncate 20)
                .Map(fun slot -> slot.ID)
                .ToArray(link)
            |> runValueTask

        printPreview "moduleDereferencedTargets" id moduleDereferencedTargets
        printPreview "extensionDereferencedTargets" id extensionDereferencedTargets

        assertArrayEqual
            "getMember + dereferenceSlotLite should match the extension API chain."
            moduleDereferencedTargets
            extensionDereferencedTargets

        printfn
            "Validated member + reference dereference example using component %s and member %s."
            componentID
            memberName
    | None ->
        printfn
            "Skipped member+dereference validation: no component with a non-null Reference member was discovered in this session."

    printfn "\n--- Slot module wrappers parity ---"

    let slotModuleChildren =
        rootSlot
        |> Slot.childrenLite
        |> Query.mapAll (Seq.truncate 25)
        |> Query.map _.ID
        |> runToArray

    let slotExtensionChildren =
        rootSlot.GetChildren(false).MapAll(Seq.truncate 25).Map(_.ID).ToArray(link)
        |> runValueTask

    printPreview "slotModuleChildren IDs" id slotModuleChildren
    printPreview "slotExtensionChildren IDs" id slotExtensionChildren

    assertArrayEqual
        "Slot.childrenShallow should match Slot extension .Children(false)."
        slotModuleChildren
        slotExtensionChildren

    printfn "Validated Slot module wrappers parity."

    printfn "\n--- Error handling example: toResult ---"

    let invalidSlotResult =
        Query.findSlotByID "__rpath_invalid_slot_id__" |> runToResult

    match invalidSlotResult with
    | Ok _ -> failwith "Expected Query.toResult to return Error for an invalid slot ID."
    | Error error -> printfn "Query.toResult returned Error for an invalid slot ID as expected: %s" error.Message

    let invalidSlotResultExtension =
        (Query.findSlotByID "__rpath_invalid_slot_id__").ToResult(link) |> runValueTask

    match invalidSlotResultExtension with
    | Ok _ -> failwith "Expected .ToResult to return Error for an invalid slot ID."
    | Error error -> printfn ".ToResult returned Error for an invalid slot ID as expected: %s" error.Message

    printfn "\n--- Large query test ---"

    let largeQuery =
        Query.root
        |> Query.descendantsAndSelfLite
        |> Query.ancestorsAndSelfLite
        |> Query.parentLite
        |> Query.childrenFull
        |> Query.components
        |> Query.map _.ComponentType
        |> runToArray

    let sampleSize = min largeQuery.Length 100

    printfn
        "Large query returned %d results. Random sample (%d):\n%A"
        largeQuery.Length
        sampleSize
        (if sampleSize = 0 then
             [||]
         else
             largeQuery |> Array.randomSample sampleSize)

    printfn "\nAll QueryTest validations completed successfully against %s" (endpoint.ToString())
