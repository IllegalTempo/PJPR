public interface IPoolable
{
    /// Called when the object is taken from the pool and activated.
    void OnSpawn();

    /// Called when the object is returned to the pool and deactivated.
    void OnDespawn();
}
