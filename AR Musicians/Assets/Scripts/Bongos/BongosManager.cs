using System;
using UnityEngine;

public class BongosManager : InstrumentDefiner
{
    [Header("Bongo Settings")]
    [SerializeField] private int circleResolution = 50; // How smooth the circle looks
    [SerializeField] private float drumHeight = 0.15f; // Depth of the drum for colliders

    // Event to notify game logic
    public static event Action<DefinedCircle> OnBongoDefined;

    private DefinedCircle? currentCircle = null;

    protected override void UpdateDrawing()
    {
        // While defining, just connect the dots (P1 -> P2 -> P3)
        // Once defined, we will draw the full circle.
        if (!isDefined)
        {
            lineRenderer.positionCount = capturedPoints.Count;
            lineRenderer.SetPositions(capturedPoints.ToArray());
            lineRenderer.enabled = true;
        }
    }

    protected override void FinalizeDefinition()
    {
        if (capturedPoints.Count < 3) return;

        // 1. Calculate the Circle from 3 points
        try
        {
            currentCircle = CalculateCircleFrom3Points(capturedPoints[0], capturedPoints[1], capturedPoints[2]);

            // 2. Draw the Circle
            DrawCircleVisuals(currentCircle.Value);

            // 3. Create Physics Object
            CreateBongoCollider(currentCircle.Value);

            isDefined = true;
            IsActive = false; // Stop accepting inputs

            Debug.Log($"Bongo Defined! R={currentCircle.Value.Radius}");
            OnBongoDefined?.Invoke(currentCircle.Value);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to define bongo (points might be collinear): " + e.Message);
            ResetDefinition();
        }
    }

    /// <summary>
    /// Core Math: Finds circumcenter and normal of 3D triangle.
    /// </summary>
    private DefinedCircle CalculateCircleFrom3Points(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Calculate Normal (The plane the drum sits on)
        Vector3 v1 = p2 - p1;
        Vector3 v2 = p3 - p1;
        Vector3 normal = Vector3.Cross(v1, v2).normalized;

        // Ensure normal faces the player (assuming player is roughly 'up' or 'back')
        Vector3 toCamera = Camera.main.transform.position - p1;
        if (Vector3.Dot(normal, toCamera) < 0)
        {
            normal = -normal;
        }

        // Calculate Center (Intersection of perpendicular bisectors)
        // We use a geometric approach:
        // The center is the intersection of two planes bisecting the chords, 
        // intersected with the triangle's plane.

        // Midpoints
        Vector3 m1 = (p1 + p2) / 2f;
        Vector3 m2 = (p2 + p3) / 2f;

        // Vectors in the plane perpendicular to the chords
        Vector3 dir1 = Vector3.Cross(v1, normal).normalized;
        Vector3 dir2 = Vector3.Cross(p3 - p2, normal).normalized;

        // Line-Line Intersection in 3D (guaranteed to intersect since on same plane)
        // L1 = m1 + t * dir1
        // L2 = m2 + u * dir2
        // We solve for intersection.

        Vector3 p1_to_p2 = m2 - m1;
        float det = Vector3.Dot(Vector3.Cross(dir1, dir2), normal);

        if (Mathf.Abs(det) < 0.001f) throw new Exception("Points are collinear");

        // Use Cramer's rule adaptation for vector lines
        float t = Vector3.Dot(Vector3.Cross(p1_to_p2, dir2), normal) / det;

        Vector3 center = m1 + (dir1 * t);
        float radius = Vector3.Distance(center, p1);

        return new DefinedCircle(center, normal, radius);
    }

    private void DrawCircleVisuals(DefinedCircle circle)
    {
        lineRenderer.positionCount = circleResolution + 1;
        lineRenderer.loop = true; // Close the loop

        // Generate points around the normal axis
        // We need two orthogonal vectors on the plane to map the circle
        Vector3 tangent = Vector3.Cross(circle.Normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.Cross(circle.Normal, Vector3.right);
        tangent.Normalize();
        Vector3 binormal = Vector3.Cross(circle.Normal, tangent).normalized;

        for (int i = 0; i <= circleResolution; i++)
        {
            float angle = i * 2 * Mathf.PI / circleResolution;
            float x = Mathf.Cos(angle) * circle.Radius;
            float y = Mathf.Sin(angle) * circle.Radius;

            Vector3 point = circle.Center + (tangent * x) + (binormal * y);
            lineRenderer.SetPosition(i, point);
        }
    }

    private void CreateBongoCollider(DefinedCircle circle)
    {
        // Create a cylinder to represent the hit zone
        GameObject hitZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hitZone.name = "BongoHitZone";

        // Position: Center, but shifted down slightly so the top face matches the rim
        Vector3 pos = circle.Center - (circle.Normal * (drumHeight / 2));

        // Rotation: Look at normal
        Quaternion rot = Quaternion.LookRotation(circle.Normal) * Quaternion.Euler(90, 0, 0);
        hitZone.transform.SetPositionAndRotation(pos, rot);

        // Scale: Diameter (Radius*2), Height, Diameter
        hitZone.transform.localScale = new Vector3(circle.Radius * 2, drumHeight / 2, circle.Radius * 2);

        // Material setup
        hitZone.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
        SetupTransparentMaterial(hitZone.GetComponent<Renderer>().material);

        spawnedVisuals.Add(hitZone);
    }

    // Helper copied from your code for transparency
    private void SetupTransparentMaterial(Material material)
    {
        material.SetFloat("_Mode", 3);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.color = new Color(1, 0.5f, 0, 0.3f); // Orange-ish transparent
    }
}
