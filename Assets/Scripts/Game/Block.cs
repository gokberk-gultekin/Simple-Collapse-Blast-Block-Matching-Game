using UnityEngine;

// Any GameObject that has block must also have a SpriteRenderer
[RequireComponent(typeof(SpriteRenderer))]
public class Block : MonoBehaviour
{
    // Block grid pos. values
    public int row;
    public int col;
    public int colorId;

    // Cache component for optimization
    private SpriteRenderer spriteRenderer;

    // Public accessor to avoid GetComponent calls in BoardManager
    public SpriteRenderer SpriteRenderer
    {
        get 
        {
            if (spriteRenderer == null) 
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            return spriteRenderer;
        }
    }

    private void Awake()
    {   
        // Cache the SpriteRenderer once on initialization
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Block initialization
    public void Init(int _row, int _col, int _colorId, Sprite _sprite)
    {
        row = _row;
        col = _col;
        colorId = _colorId;

        SpriteRenderer.sprite = _sprite;

        // Name tag for debugging if needed
        gameObject.name = $"Block_{col}_{row}";

    }
}