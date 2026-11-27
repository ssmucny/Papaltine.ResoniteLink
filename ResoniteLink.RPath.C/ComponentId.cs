namespace ResoniteLink.RPath.C
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
        /// <typeparam name="TFrom">The more specific type to widen from</typeparam>
        /// <returns>The more general ID</returns>
        public static ComponentId<T> From<TFrom>(ComponentId<TFrom> id) where TFrom : T
        {
            return new ComponentId<T>(id.Value);
        }
    }
}