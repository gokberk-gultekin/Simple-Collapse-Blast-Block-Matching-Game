using UnityEngine;
using System;
using UnityEngine.EventSystems;

/** Handle user inputs during gameplay */
public class InputHandler : MonoBehaviour
{
    public static event Action<Block> OnBlockClicked;

    private Camera mainCamera;

    void Awake()
    {
        // Cache Camera.main for optimization
        mainCamera = Camera.main;
    }

    void Start()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("EventSystem is not found. " +
                "UI blocking will not work!");
        }
    }

    void Update()
    {
        // Check for user touches and clicks
        if (Input.GetMouseButtonDown(0))
        {
            DetectObject();
        }
    }

    void DetectObject()
    {
        // Ignore input when interacting with UI elements
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Additional mobile-specific check for multi-touch situations
#if UNITY_IOS
    if (Input.touchCount > 0) 
    {
        Touch touch = Input.GetTouch(0);
        if (EventSystem.current != null && 
            EventSystem.current.IspointerOverGameObject(touch.fingerId))
        {
            return;
        }
    }
#endif
        // Convert screen position to world position
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);


        // Detect hits and notify listeners
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider != null)
        {
            Block hitBlock = hit.collider.GetComponent<Block>();
            if (hitBlock != null)
            {
                OnBlockClicked?.Invoke(hitBlock);
            }
        }
    }
}