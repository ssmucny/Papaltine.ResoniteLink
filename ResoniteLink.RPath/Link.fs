[<RequireQualifiedAccess>]
module ResoniteLink.RPath.Link

open System
open System.Threading.Tasks
open ResoniteLink

/// The session rejected the batch (`batchError`) and/or individual operations (batch index, message).
exception BatchFailed of batchError: string * failed: struct (int * string * DataModelOperation) array with
    override this.Message =
        seq {
            if not (String.IsNullOrEmpty this.batchError) then
                this.batchError

            yield!
                this.failed
                |> Seq.map (fun struct (index, message, op) -> $"operation {index}: {message}. {op.ToString()}")
        }
        |> String.concat "\n"


/// <summary>
/// Runs a batch of <see cref="DataModelOperation"/>s and checks every nested response. The batch as a whole can
/// report success even when individual operations failed, so each one is inspected.
/// </summary>
/// <returns><c>Ok (responses.Count)</c>, otherwise an exception listing every failed operation.</returns>
let RunBatch
    (operations: ResizeArray<DataModelOperation>)
    (link: LinkInterface)
    : Task<Result<unit, exn>> =
    task {
        try
            let! response = link.RunDataModelOperationBatch operations

            let failedOperations =
                response.Responses
                |> Seq.indexed
                |> Seq.filter (fun (_, operationResponse) -> not operationResponse.Success)
                |> Seq.map (fun (index, operationResponse) -> struct (index, operationResponse.ErrorInfo, operations[index]))
                |> Seq.toArray

            return
                if response.Success && Array.isEmpty failedOperations then
                    Ok()
                else
                    Error(BatchFailed(response.ErrorInfo, failedOperations))
        with error ->
            return Error(error)
    }

let AddUnder (parentId: SlotId) (slot: Slot) (link: LinkInterface) =
    RunBatch (Slot.AddUnder parentId slot) link
    
let AddSlotsUnder (parentId: SlotId) (slots: Slot seq) (link: LinkInterface) =
    RunBatch (Slot.AddSlotsUnder parentId slots) link
    
let Patch (slot: Slot) (link: LinkInterface) =
   RunBatch (Slot.Patch slot) link
    
let PatchSlots (slots: Slot seq) (link: LinkInterface) =
    RunBatch (Slot.PatchSlots slots) link
