namespace Resonite.RPath.C
{
    /// ID of a component, for ProtoGraph ports that reference a component. <c>default</c> is an unset ID.
    public readonly struct ComponentId<T>
    {
        public ComponentId(string componentId)
        {
            Value = componentId;
        }

        /// The raw ID, or null when unset
        public string Value { get; }

        /// <summary>
        /// Upcast a component ID to a wider type. The current type must be a proper subtype of the target type.
        /// </summary>
        /// <typeparam name="T2">The supertype to cast to</typeparam>
        /// <returns>The more general ID</returns>
        public ComponentId<T2> Cast<T2>() where T2 : T
        {
            return new ComponentId<T2>(Value);
        }
    }
}