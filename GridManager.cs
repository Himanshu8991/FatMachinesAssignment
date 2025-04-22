using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int rows = 10;
    public int cols = 10;
    public float cellSize = 1f;

    public int MinX => -cols / 2;
    public int MaxX => cols / 2 - 1;
    public int MinY => -rows / 2;
    public int MaxY => rows / 2 - 1;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos) =>
        new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0);

    public Vector2Int GetGridPosition(Vector3 worldPos) =>
        new Vector2Int(Mathf.RoundToInt(worldPos.x / cellSize), Mathf.RoundToInt(worldPos.y / cellSize));

    public bool IsInsideGrid(Vector2Int pos) =>
        pos.x >= MinX && pos.x <= MaxX && pos.y >= MinY && pos.y <= MaxY;
}
