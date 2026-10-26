using UnityEngine;

public class ManaCardState : MonoBehaviour
{
    public bool IsExhaused { get; private set; }

    public void Exhaust() {
        IsExhaused = true;
        UpdateVisual();
    }

    public void Ready() { 
        IsExhaused = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        transform.localRotation = IsExhaused ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
    }

}
