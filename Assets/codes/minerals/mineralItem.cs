using UnityEngine;

public class mineralItem : Item
{

    public void onDropped()
    {
        rb.isKinematic = false;
        rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        transform.parent = null;
    }


}
