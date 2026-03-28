namespace ResoniteLink.RPath

open ResoniteLink

/// <summary>
/// Axis operations rooted at a single slot. All functions are thin wrappers over
/// <c>Query.wrap slot |&gt; Query.*</c> — refer to the <see cref="Query"/> module for
/// full semantics and remarks on each operation.
/// </summary>
module Slot =

    /// <summary>Gets the direct children of a slot.</summary>
    let children (includeComponents: bool) (slot: Slot) : Query<Slot> =
        Query.wrap slot |> Query.children includeComponents

    /// <summary>Gets direct children of a slot that satisfy a predicate.</summary>
    let inline child ([<InlineIfLambda>] childPredicate) (includeComponents: bool) (slot: Slot) : Query<Slot> =
        slot |> children includeComponents |> Query.filter childPredicate

    /// <summary>Gets the direct children of a slot without component data.</summary>
    let childrenLite (slot: Slot) : Query<Slot> = children false slot

    /// <summary>Gets the direct children of a slot with full component data.</summary>
    let childrenFull (slot: Slot) : Query<Slot> = children true slot

    /// <summary>Gets all descendants of a slot (children, grandchildren, etc.).</summary>
    let descendants (includeComponents: bool) (slot: Slot) : Query<Slot> =
        Query.wrap slot |> Query.descendants includeComponents

    /// <summary>Gets all descendants of a slot without component data.</summary>
    let descendantsLite (slot: Slot) : Query<Slot> = descendants false slot

    /// <summary>Gets all descendants of a slot with full component data.</summary>
    let descendantsFull (slot: Slot) : Query<Slot> = descendants true slot

    /// <summary>Gets the slot and all of its descendants (children, grandchildren, etc.).</summary>
    let descendantsAndSelf (includeComponents: bool) (slot: Slot) : Query<Slot> =
        Query.wrap slot |> Query.descendantsAndSelf includeComponents

    /// <summary>Gets the slot and all of its descendants without component data.</summary>
    let descendantsAndSelfLite (slot: Slot) : Query<Slot> = descendantsAndSelf false slot

    /// <summary>Gets the slot and all of its descendants with full component data.</summary>
    let descendantsAndSelfFull (slot: Slot) : Query<Slot> = descendantsAndSelf true slot

    /// <summary>Gets the parent of a slot.</summary>
    let parent (includeComponents: bool) (slot: Slot) : Query<Slot> =
        Query.wrap slot |> Query.parent includeComponents

    /// <summary>Gets the parent of a slot without component data.</summary>
    let parentLite (slot: Slot) : Query<Slot> = parent false slot

    /// <summary>Gets the parent of a slot with full component data.</summary>
    let parentFull (slot: Slot) : Query<Slot> = parent true slot

    /// <summary>Gets all ancestors of a slot (parent, grandparent, etc.) up to the root.</summary>
    let ancestors (includeComponents: bool) (slot: Slot) : Query<Slot> =
        Query.wrap slot |> Query.ancestors includeComponents

    /// <summary>Gets all ancestors of a slot without component data.</summary>
    let ancestorsLite (slot: Slot) : Query<Slot> = ancestors false slot

    /// <summary>Gets all ancestors of a slot with full component data.</summary>
    let ancestorsFull (slot: Slot) : Query<Slot> = ancestors true slot

    /// <summary>Gets the slot and all of its ancestors (parent, grandparent, etc.) up to the root.</summary>
    let ancestorsAndSelf (includeComponents: bool) (slot: Slot) : Query<Slot> =
        Query.wrap slot |> Query.ancestorsAndSelf includeComponents

    /// <summary>Gets the slot and all of its ancestors without component data.</summary>
    let ancestorsAndSelfLite (slot: Slot) : Query<Slot> = ancestorsAndSelf false slot

    /// <summary>Gets the slot and all of its ancestors with full component data.</summary>
    let ancestorsAndSelfFull (slot: Slot) : Query<Slot> = ancestorsAndSelf true slot
