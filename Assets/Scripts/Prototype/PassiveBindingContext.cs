namespace DiceRevolver.Prototype
{
    public readonly struct PassiveBindingContext
    {
        public PassiveBindingContext(int face)
            : this(face, null)
        {
        }

        public PassiveBindingContext(
            int face,
            System.Func<int, ProjectileTypeDefinition> baseProjectileTypeResolver)
        {
            Face = face;
            this.baseProjectileTypeResolver = baseProjectileTypeResolver;
        }

        private readonly System.Func<int, ProjectileTypeDefinition> baseProjectileTypeResolver;
        public int Face { get; }
        public ProjectileTypeDefinition BaseProjectileType =>
            baseProjectileTypeResolver?.Invoke(Face);
    }
}
