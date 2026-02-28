using UnityEngine;

public class sliding_door : SpaceshipPart
{
    [SerializeField] private Animator animator;
    [SerializeField] private string openParameterName = "IsOpen";

    private bool isDoorOpen = false;

    private void Start()
    {
        // Get the Animator component attached to this GameObject if not assigned in Inspector
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // Toggle the door when clicked (requires interactable setup)
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
        if (isDoorOpen) return;
        
        isDoorOpen = true;
        if (animator != null)
            animator.SetBool(openParameterName, true);
    }

    public void CloseDoor()
    {
        if (!isDoorOpen) return;
        
        isDoorOpen = false;
        if (animator != null)
            animator.SetBool(openParameterName, false);
    }

    // Automatically open when a Player enters the trigger area
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OpenDoor();
        }
    }

    // Automatically close when a Player leaves the trigger area
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CloseDoor();
        }
    }
} 