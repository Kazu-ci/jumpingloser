using UnityEngine;

public class AttackHitBoxVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float displayTime;
    private float timer;

    public void Init(Vector3 center, Vector3 size, Quaternion rotation, float time, Color color)
    {
        Vector3[] corners = GetBoxCorners(center, size, rotation);
        Vector3[][] edges = GetBoxEdges(corners);

        foreach (var edge in edges)
        {
            var lr = new GameObject("Edge").AddComponent<LineRenderer>();
            lr.transform.parent = transform;
            lr.positionCount = 2;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.useWorldSpace = true;
            lr.SetPositions(edge);
        }

        displayTime = time;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= displayTime)
        {
            Destroy(gameObject);
        }
    }

    private Vector3[] GetBoxCorners(Vector3 center, Vector3 size, Quaternion rotation)
    {
        Vector3 half = size * 0.5f;

        Vector3[] corners = new Vector3[8];
        corners[0] = center + rotation * new Vector3(-half.x, -half.y, -half.z);
        corners[1] = center + rotation * new Vector3(half.x, -half.y, -half.z);
        corners[2] = center + rotation * new Vector3(half.x, -half.y, half.z);
        corners[3] = center + rotation * new Vector3(-half.x, -half.y, half.z);

        corners[4] = center + rotation * new Vector3(-half.x, half.y, -half.z);
        corners[5] = center + rotation * new Vector3(half.x, half.y, -half.z);
        corners[6] = center + rotation * new Vector3(half.x, half.y, half.z);
        corners[7] = center + rotation * new Vector3(-half.x, half.y, half.z);

        return corners;
    }

    private Vector3[][] GetBoxEdges(Vector3[] c)
    {
        return new Vector3[][]
        {
        new [] { c[0], c[1] },
        new [] { c[1], c[2] },
        new [] { c[2], c[3] },
        new [] { c[3], c[0] },

        new [] { c[4], c[5] },
        new [] { c[5], c[6] },
        new [] { c[6], c[7] },
        new [] { c[7], c[4] },

        new [] { c[0], c[4] },
        new [] { c[1], c[5] },
        new [] { c[2], c[6] },
        new [] { c[3], c[7] },
        };
    }
}