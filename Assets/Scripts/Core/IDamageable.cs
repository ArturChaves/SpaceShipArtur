namespace SpaceShip.Core
{
    public interface IDamageable
    {
        Faction Side { get; }

        bool TakeHit(int damage);
    }
}
