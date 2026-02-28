using UnityEngine;

public class sliding_door : SpaceshipPart //the animators are still in the animtion file, but now it's using hardcode
{
    public enum OpenDirection
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3
    }

    [Header("Door Settings")]
    [SerializeField] private OpenDirection openDirection = OpenDirection.Left;
    [SerializeField] private float slideDistanceMultiplier = 1.0f;
    [SerializeField] private float slideSpeed = 2f;
    [SerializeField] private Transform doorMesh; 

    private Vector3 closedPosition;
    private Vector3 openPosition;
    
    private bool isDoorOpen = false;

    private void Start()
    {
        if (doorMesh == null)
            doorMesh = transform;

        closedPosition = doorMesh.localPosition;
        CalculateOpenPosition();
    }

    private void CalculateOpenPosition()
    {
        Collider col = doorMesh.GetComponent<Collider>();
        float distance = 2f; 

        if (col != null)
        {
            switch (openDirection)
            {
                case OpenDirection.Left:
                case OpenDirection.Right:
                    distance = col.bounds.size.x;
                    break;
                case OpenDirection.Up:
                case OpenDirection.Down:
                    distance = col.bounds.size.y;
                    break;
            }
        }

        distance *= slideDistanceMultiplier; 

        switch (openDirection)
        {
            case OpenDirection.Left:
                openPosition = closedPosition + (Vector3.left * distance);
                break;
            case OpenDirection.Right:
                openPosition = closedPosition + (Vector3.right * distance);
                break;
            case OpenDirection.Up:
                openPosition = closedPosition + (Vector3.up * distance);
                break;
            case OpenDirection.Down:
                openPosition = closedPosition + (Vector3.down * distance);
                break;
        }
    }

    protected override void Update()
    {
        base.Update();
        
        Vector3 targetPos = isDoorOpen ? openPosition : closedPosition;
        doorMesh.localPosition = Vector3.Lerp(doorMesh.localPosition, targetPos, Time.deltaTime * slideSpeed);
    }

    public override void OnClicked()
    {
        base.OnClicked();
        
        if (isDoorOpen)
            CloseDoor();
        else
            OpenDoor();
    }

    public void OpenDoor()
    {
        isDoorOpen = true;
    }

    public void CloseDoor()
    {
        isDoorOpen = false;
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         OpenDoor();
    //     }
    // }
    // private void OnTriggerExit(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         CloseDoor();
    //     }
    // }
} 