/// <summary>
/// Interface for GameObjects managed by MeteoritePool.
/// Called by the pool when an object is taken or returned.
/// </summary>
public interface IPoolable
{
    /// <summary>Called when the object is taken from the pool and activated.</summary>
    void OnSpawn();

    /// <summary>Called when the object is returned to the pool and deactivated.</summary>
    void OnDespawn();
}
