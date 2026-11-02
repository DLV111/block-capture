using UnityEngine;
using System.Buffers;

/// <summary>
/// Handles player movement with smooth touch controls for Block Capture.
/// Optimized for mobile with pooled buffers to prevent GC spikes.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Tooltip("Constant movement speed in units per second")]
    private float moveSpeed = 5f;
    
    [SerializeField, Range(0.01f, 1f), Tooltip("Steering interpolation factor (lower = smoother, higher = snappier)")]
    private float steeringSmoothing = 0.15f;
    
    [SerializeField, Tooltip("Maximum number of trail segments to track")]
    private int trailBufferSize = 20;

    // Internal state
    private Vector3 currentPosition;
    private Vector3 currentDirection = Vector3.zero;
    private Vector3 targetWorldPosition;
    private bool isMoving = false;
    private bool useNewInputSystem = false;

    // Pooled trail buffer (ring buffer for O(1) operations)
    private Vector2[] trailBuffer;
    private int trailHead = 0;
    private int trailCount = 0;

    private void Awake()
    {
        // Pre-allocate trail buffer from shared pool
        trailBuffer = ArrayPool<Vector2>.Shared.Rent(trailBufferSize);
        
        currentPosition = transform.position;
        targetWorldPosition = currentPosition;
        
        // Check for new Input System
        useNewInputSystem = TryGetComponent(out PlayerInput _);
    }

    private void Update()
    {
        HandleInput();
        UpdateMovement();
        UpdateTrail();
    }

    private void HandleInput()
    {
        Vector3 screenPos = Vector3.zero;
        
        // Try new Input System first, fallback to legacy
        if (useNewInputSystem)
        {
            screenPos = GetTouchPositionNewInputSystem();
        }
        
        if (screenPos == Vector3.zero)
        {
            screenPos = GetTouchPositionLegacy();
        }

        if (screenPos != Vector3.zero)
        {
            // Convert screen to world position (2D)
            if (Camera.main != null)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane)
                );
                targetWorldPosition = new Vector3(worldPos.x, worldPos.y, 0f);
            }
            else
            {
                // Fallback: assume screen center if no camera
                targetWorldPosition = new Vector3(screenPos.x, screenPos.y, 0f);
            }
            
            isMoving = true;
        }
    }

    private Vector3 GetTouchPositionLegacy()
    {
        // Primary: touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began 
                || touch.phase == TouchPhase.Moved 
                || touch.phase == TouchPhase.Stationary)
            {
                return touch.position;
            }
        }
        
        // Fallback: mouse for editor testing
        if (Input.GetMouseButton(0))
        {
            return Input.mousePosition;
        }
        
        return Vector3.zero;
    }

    private Vector3 GetTouchPositionNewInputSystem()
    {
        // Would integrate with UnityEngine.InputSystem here
        // For now, return zero to fallback to legacy
        return Vector3.zero;
    }

    private void UpdateMovement()
    {
        if (!isMoving) return;

        // Direction from player to target
        Vector3 toTarget = targetWorldPosition - currentPosition;
        float distance = toTarget.magnitude;
        
        // If we've reached the target (within small threshold), stop
        if (distance < 0.1f)
        {
            isMoving = false;
            return;
        }

        Vector3 desiredDirection = toTarget.normalized;
        
        // Smooth steering using time-based lerp (frame-rate independent)
        // steeringSmoothing is per-second rate, so multiply by deltaTime * 60 for ~60fps reference
        float t = 1f - Mathf.Pow(1f - steeringSmoothing, Time.deltaTime * 60f);
        currentDirection = Vector3.Lerp(currentDirection, desiredDirection, t);
        
        // Apply movement
        currentPosition += currentDirection * moveSpeed * Time.deltaTime;
        transform.position = currentPosition;
    }

    private void UpdateTrail()
    {
        if (!isMoving) return;

        // Ring buffer: O(1) insert, no allocations
        trailBuffer[trailHead] = new Vector2(currentPosition.x, currentPosition.y);
        trailHead = (trailHead + 1) % trailBuffer.Length;
        trailCount = Mathf.Min(trailCount + 1, trailBuffer.Length);
    }

    private void OnDestroy()
    {
        // Return buffer to pool - critical for preventing memory leaks
        if (trailBuffer != null)
        {
            ArrayPool<Vector2>.Shared.Return(trailBuffer, clearArray: true);
            trailBuffer = null;
        }
    }

    /// <summary>
    /// Gets the current trail segments for collision detection.
    /// Caller MUST return the array via ReturnTrailSegments().
    /// </summary>
    public Vector2[] GetTrailSegments(int maxSegments = 20)
    {
        int count = Mathf.Min(maxSegments, trailCount);
        Vector2[] segments = ArrayPool<Vector2>.Shared.Rent(count);
        
        for (int i = 0; i < count; i++)
        {
            // Read newest-first (reverse chronological)
            int index = (trailHead - 1 - i + trailBuffer.Length) % trailBuffer.Length;
            segments[i] = trailBuffer[index];
        }
        
        return segments;
    }

    /// <summary>
    /// Returns trail segments array to the pool.
    /// </summary>
    public void ReturnTrailSegments(Vector2[] segments)
    {
        if (segments != null)
        {
            ArrayPool<Vector2>.Shared.Return(segments, clearArray: true);
        }
    }

    /// <summary>
    /// Checks if the player's trail intersects with the given point.
    /// Uses pooled buffer internally to avoid allocations.
    /// </summary>
    public bool CheckTrailIntersection(Vector2 point, float threshold = 0.5f)
    {
        if (trailCount < 2) return false;
        
        int checkCount = Mathf.Min(trailCount - 1, trailBufferSize - 1);
        float thresholdSq = threshold * threshold;
        
        for (int i = 0; i < checkCount; i++)
        {
            int idx1 = (trailHead - 1 - i + trailBuffer.Length) % trailBuffer.Length;
            int idx2 = (trailHead - 2 - i + trailBuffer.Length) % trailBuffer.Length;
            
            Vector2 a = trailBuffer[idx1];
            Vector2 b = trailBuffer[idx2];
            
            // Quick distance check to line segment
            if (DistanceToSegmentSquared(point, a, b) < thresholdSq)
                return true;
        }
        
        return false;
    }

    private static float DistanceToSegmentSquared(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector2 closest = a + ab * t;
        return (p - closest).sqrMagnitude;
    }

    // Public properties for external access
    public Vector3 CurrentPosition => currentPosition;
    public Vector3 CurrentDirection => currentDirection;
    public bool IsMoving => isMoving;
    public float MoveSpeed => moveSpeed;
    public int TrailCount => trailCount;
}