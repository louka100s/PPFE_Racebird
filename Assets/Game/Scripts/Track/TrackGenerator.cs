using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    [Header("Track Settings")]
    [SerializeField] private int segmentCount = 20;
    [SerializeField] private float segmentLength = 20f;
    [SerializeField] private float trackWidth = 10f;
    
    [Header("Track Variation")]
    [SerializeField] private float curveChance = 0.3f;
    [SerializeField] private float maxCurveAngle = 30f;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject straightSegmentPrefab;
    [SerializeField] private GameObject curveSegmentPrefab;
    
    private void Start()
    {
        GenerateTrack();
    }

    private void GenerateTrack()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        for (int i = 0; i < segmentCount; i++)
        {
            bool isCurve = Random.value < curveChance && curveSegmentPrefab != null;
            GameObject segmentPrefab = isCurve ? curveSegmentPrefab : straightSegmentPrefab;

            if (segmentPrefab == null)
            {
                CreateProceduralSegment(currentPosition, currentRotation, isCurve);
            }
            else
            {
                GameObject segment = Instantiate(segmentPrefab, currentPosition, currentRotation, transform);
                segment.name = $"Segment_{i}_{(isCurve ? "Curve" : "Straight")}";
            }

            currentPosition += currentRotation * Vector3.forward * segmentLength;

            if (isCurve)
            {
                float angle = Random.Range(-maxCurveAngle, maxCurveAngle);
                currentRotation *= Quaternion.Euler(0f, angle, 0f);
            }
        }
    }

    private void CreateProceduralSegment(Vector3 position, Quaternion rotation, bool isCurve)
    {
        GameObject segment = new GameObject(isCurve ? "Curve_Segment" : "Straight_Segment");
        segment.transform.SetParent(transform);
        segment.transform.position = position;
        segment.transform.rotation = rotation;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.SetParent(segment.transform);
        ground.transform.localPosition = new Vector3(0f, -0.5f, segmentLength * 0.5f);
        ground.transform.localScale = new Vector3(trackWidth, 1f, segmentLength);
        ground.name = "Ground";

        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.3f, 0.3f, 0.35f);
        }

        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.transform.SetParent(segment.transform);
        leftWall.transform.localPosition = new Vector3(-trackWidth * 0.5f, 0.5f, segmentLength * 0.5f);
        leftWall.transform.localScale = new Vector3(0.5f, 2f, segmentLength);
        leftWall.name = "LeftWall";

        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.transform.SetParent(segment.transform);
        rightWall.transform.localPosition = new Vector3(trackWidth * 0.5f, 0.5f, segmentLength * 0.5f);
        rightWall.transform.localScale = new Vector3(0.5f, 2f, segmentLength);
        rightWall.name = "RightWall";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(trackWidth, 0.1f, segmentLength));
    }
}
