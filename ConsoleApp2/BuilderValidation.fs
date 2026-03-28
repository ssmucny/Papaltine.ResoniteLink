module ConsoleApp2.BuilderValidation

open System
open System.Threading
open System.Threading.Tasks
open ResoniteLink
open ResoniteLink.RPath

module RPathSlot = ResoniteLink.RPath.Slot

let private componentTypes =
    [| "[FrooxEngine]FrooxEngine.Grabbable"
       "[ProtoFluxBindings]FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.AssetInput<[FrooxEngine]FrooxEngine.AudioClip>"
       "[FrooxEngine]FrooxEngine.TransformStreamDriver"
       "[ProtoFluxBindings]FrooxEngine.FrooxEngine.ProtoFlux.CoreNodes.ElementSource<[FrooxEngine]FrooxEngine.Slot>"
       "[ProtoFluxBindings]FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Slots.GetActiveUser"
       "[FrooxEngine]FrooxEngine.DynamicValueVariableDriver<[Renderite.Shared]Renderite.Shared.Key>"
       "[ProtoFluxBindings]FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.ValueRelay<float3>"
       "[ProtoFluxBindings]FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Actions.FireOnTrue>"
       "[FrooxEngine]FrooxEngine.StaticMesh"
       "[FrooxEngine]FrooxEngine.CommonAvatar.AvatarAnchorLocomotionRelease"
       "[FrooxEngine]FrooxEngine.SphereCollider"
       "[FrooxEngine]FrooxEngine.MeshRenderer"
       "[FrooxEngine]FrooxEngine.UnlitMaterial"
       "[FrooxEngine]FrooxEngine.PBS_Metallic"
       "[FrooxEngine]FrooxEngine.UIX.Image"
       "[FrooxEngine]FrooxEngine.ContextMenuItemSource"
       "[FrooxEngine]FrooxEngine.ObjectRoot"
       "[FrooxEngine]FrooxEngine.StaticAudioClip"
       "[FrooxEngine]FrooxEngine.StaticFont"
       "[FrooxEngine]FrooxEngine.BoxMesh"
       "[FrooxEngine]FrooxEngine.StaticShader"
       "[FrooxEngine]FrooxEngine.ProtoFlux.GlobalValue<string>"
       "[FrooxEngine]FrooxEngine.ValueCopy<string>"
       "[FrooxEngine]FrooxEngine.MeshCollider"
       "[FrooxEngine]FrooxEngine.StaticTexture2D"
       "[FrooxEngine]FrooxEngine.CommonAvatar.EyeRotationDriver"
       "[FrooxEngine]FrooxEngine.ButtonPressEventRelay" |]

let run (endpoint: Uri) =
    let runTask (task: Task) = task.GetAwaiter().GetResult()
    let runTaskResult (task: Task<'T>) = task.GetAwaiter().GetResult()

    let assertBatchSuccess (response: BatchResponse) =
        if not response.Success then
            failwithf "Batch operation failed: %s" response.ErrorInfo

        for individualResponse in response.Responses do
            if not individualResponse.Success then
                failwithf "Individual operation in batch failed: %s" individualResponse.ErrorInfo

    let assertTrue message condition =
        if not condition then
            failwith message

    let assertEqual message expected actual =
        if not (expected = actual) then
            failwithf "%s\nExpected: %A\nActual:   %A" message expected actual

    let childSeq (slot: ResoniteLink.Slot) : seq<ResoniteLink.Slot> =
        if isNull slot.Children then
            Seq.empty
        else
            slot.Children :> seq<ResoniteLink.Slot>

    let componentSeq (slot: ResoniteLink.Slot) : seq<Component> =
        if isNull slot.Components then
            Seq.empty
        else
            slot.Components :> seq<Component>

    let operationKinds (ops: ResizeArray<DataModelOperation>) =
        ops |> Seq.map (fun op -> op.GetType().Name) |> Seq.toArray

    printfn "\n=== Builder API integration validation against %s ===" (endpoint.ToString())

    let link = new LinkInterface()
    runTask (link.Connect(endpoint, CancellationToken.None))
    assertTrue "Unable to connect to websocket endpoint." link.IsConnected

    let integrationRoot =
        Reso.slot
            (Name = $"rpath-builder-integration-{Guid.NewGuid():N}", Tag = "integration")
            [ Reso.comp (ComponentType = componentTypes[3]) [] ]
            [ Reso.slot
                  (Name = "Initial Child", Tag = "created")
                  []
                  [ Reso.slot (Name = "Subslot", Tag = "Ohh", IsActive = false) [] [ Reso.slot (Name = "subsub") [] [] ] ] ]

    let integrationAddOps =
        RPathSlot.addUnder ResoniteLink.Slot.ROOT_SLOT_ID integrationRoot

    let addResponse = runTaskResult (link.RunDataModelOperationBatch integrationAddOps)
    assertBatchSuccess addResponse

    assertTrue "Expected addUnder to assign the root slot ID." (not (String.IsNullOrWhiteSpace integrationRoot.ID))

    let createdRootId = integrationRoot.ID

    let createdRootResult =
        runTaskResult (link.GetSlotData(GetSlot(SlotID = createdRootId, Depth = 2, IncludeComponentData = true)))

    assertTrue
        (sprintf "Expected newly created slot to be queryable: %s" createdRootResult.ErrorInfo)
        createdRootResult.Success

    integrationRoot.Name <- Field_string(Value = integrationRoot.Name.Value + "-patched")

    integrationRoot.Children <-
        ResizeArray(
            seq {
                yield! childSeq integrationRoot
                yield Reso.slot (ID = Reso.newID (), Name = "Patch Added Child", Tag = "patched") [] []
            }
        )

    integrationRoot.Components <-
        ResizeArray(
            seq {
                yield! componentSeq integrationRoot
                yield Reso.comp (ID = Reso.newID (), ComponentType = Array.randomChoice componentTypes) []
                yield Reso.comp (ID = Reso.newID (), ComponentType = Array.randomChoice componentTypes) []
                yield Reso.comp (ID = Reso.newID (), ComponentType = Array.randomChoice componentTypes) []
            }
        )

    integrationRoot.Children[0].IsActive <- Field_bool(Value = false)
    integrationRoot.Children[0].Name.Value <- "New Name"
    integrationRoot.Children.Add(Reso.slot ( Name = "Another Patch Child", Tag = "patched") [] [])

    let integrationPatchOps = RPathSlot.patch integrationRoot |> ResizeArray

    let patchResponse =
        runTaskResult (link.RunDataModelOperationBatch integrationPatchOps)

    assertBatchSuccess patchResponse

    let patchedRootResult =
        runTaskResult (link.GetSlotData(GetSlot(SlotID = createdRootId, Depth = 2, IncludeComponentData = true)))

    assertTrue
        (sprintf "Expected patched slot to be queryable: %s" patchedRootResult.ErrorInfo)
        patchedRootResult.Success

    let patchedRoot = patchedRootResult.Data

    assertTrue "Patch should update the slot name." (patchedRoot.Name.Value.EndsWith "-patched")
    
    let tree =
        Reso.slot (Name = "Slot Name")
            [ Reso.comp (ComponentType = "[FrooxEngine]FrooxEngine.MeshRenderer")
                  [ "SortingOrder", Reso.int 10 ] ]
            [ Reso.slot (Name = "Child") [] [] ]

    let addOps = Slot.addUnder Slot.ROOT_SLOT_ID tree
    link.RunDataModelOperationBatch addOps |> runTaskResult |> assertBatchSuccess

    tree.Name.Value <- "New Slot Name"
    tree.Components[0].Members["SortingOrder"] <- Reso.int 20
    let patchOps = Slot.patch tree
    link.RunDataModelOperationBatch patchOps |> runTaskResult |> assertBatchSuccess

    printfn "Builder integration checks passed for root slot %s" createdRootId
