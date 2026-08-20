namespace DiceRevolver.Prototype
{
    public readonly struct PassiveBindingContext
    {
        public PassiveBindingContext(int face)
        {
            Face = face;
        }

        public int Face { get; }
    }
}
