using UnityEngine;

public class DraggableBlock : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging;
    private Camera cam;
    private GridManager gridManager;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    [SerializeField] private float moveSpeed = 10f;

    private ContactFilter2D contactFilter;
    private RaycastHit2D[] hitBuffer = new RaycastHit2D[5];

    private void Start()
    {
        cam = Camera.main;
        gridManager = GridManager.Instance;
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        contactFilter.useLayerMask = true;

        SnapToGrid();
    }

    private void SnapToGrid()
    {
        Vector2Int gridPos = gridManager.GetGridPosition(transform.position);
        gridPos.x = Mathf.Clamp(gridPos.x, gridManager.MinX, gridManager.MaxX);
        gridPos.y = Mathf.Clamp(gridPos.y, gridManager.MinY, gridManager.MaxY);
        Vector3 snappedPos = gridManager.GetWorldPosition(gridPos);

        Vector2 snapMovement = (Vector2)snappedPos - rb.position;

        if (snapMovement.magnitude < 0.05f)
        {
            rb.MovePosition(snappedPos);
            return;
        }

        int hitCount = rb.Cast(snapMovement.normalized, contactFilter, hitBuffer, snapMovement.magnitude + 0.01f);
        bool canSnap = true;

        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].collider != null && hitBuffer[i].collider.gameObject != gameObject)
            {
                if (hitBuffer[i].distance <= 0.01f)
                    continue;

                canSnap = false;
                break;
            }
        }

        if (canSnap)
        {
            rb.MovePosition(snappedPos);
            Debug.Log("Snapped to grid");
        }
        else
        {
            Debug.LogWarning($"SnapToGrid failed: {rb.position} → {snappedPos} blocked.");
        }
    }

    private void Update()
    {
        if (isDragging)
        {
            DragBlock();
        }
    }

    private void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;

            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            offset = transform.position - mouseWorldPos;
        }
    }

    private void DragBlock()
    {
        if (!Input.GetMouseButton(0)) return;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector3 targetPos = mouseWorldPos + offset;

        Vector2Int gridTargetPos = gridManager.GetGridPosition(targetPos);

        if (!gridManager.IsInsideGrid(gridTargetPos))
        {
            gridTargetPos.x = Mathf.Clamp(gridTargetPos.x, gridManager.MinX, gridManager.MaxX);
            gridTargetPos.y = Mathf.Clamp(gridTargetPos.y, gridManager.MinY, gridManager.MaxY);
            targetPos = gridManager.GetWorldPosition(gridTargetPos);
        }

        MoveWithCollisionAndSliding(targetPos);
    }

    private void MoveWithCollisionAndSliding(Vector3 targetPos)
    {
        Vector2 movement = (Vector2)targetPos - rb.position;
        float distance = movement.magnitude;
        if (distance < 0.001f) return;

        Vector2 direction = movement / distance;

        int hitCount = rb.Cast(direction, contactFilter, hitBuffer, distance);
        if (hitCount == 0)
        {
            rb.MovePosition(targetPos);
            return;
        }

        float closestDistance = distance;
        Vector2 closestNormal = Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].collider.gameObject != gameObject && hitBuffer[i].distance < closestDistance)
            {
                closestDistance = hitBuffer[i].distance;
                closestNormal = hitBuffer[i].normal;
            }
        }

        if (closestDistance < distance)
        {
            float actualDistance = Mathf.Max(0, closestDistance - 0.01f);
            Vector2 newPosition = rb.position + direction * actualDistance;
            rb.MovePosition(newPosition);
        }

        Vector2 remainingMovement = (Vector2)targetPos - rb.position;

        if (closestNormal != Vector2.zero && remainingMovement.magnitude > 0.001f)
        {
            Vector2 slideDirection = Vector2.Perpendicular(closestNormal) * Mathf.Sign(Vector2.Dot(Vector2.Perpendicular(closestNormal), remainingMovement));
            float slideDistance = Vector2.Dot(remainingMovement.normalized, slideDirection) * remainingMovement.magnitude;

            hitCount = rb.Cast(slideDirection, contactFilter, hitBuffer, Mathf.Abs(slideDistance));

            float maxSlideDistance = Mathf.Abs(slideDistance);
            for (int i = 0; i < hitCount; i++)
            {
                if (hitBuffer[i].collider.gameObject != gameObject && hitBuffer[i].distance < maxSlideDistance)
                {
                    maxSlideDistance = hitBuffer[i].distance - 0.01f;
                }
            }

            if (maxSlideDistance > 0)
            {
                Vector2 slideMovement = slideDirection * maxSlideDistance;
                rb.MovePosition(rb.position + slideMovement);
            }
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
        SnapToGrid();
    }
}
