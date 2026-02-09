using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    [Header("Track Settings")]
    [SerializeField] private float trackWidth = 10f;
    [SerializeField] private float wallHeight = 2f;
    
    [Header("Straight Sections")]
    [SerializeField] private float straightSegmentLength = 10f;
    
    [Header("Curve Sections")]
    [SerializeField] private float curveSegmentLength = 5f;
    [SerializeField] private float curveAnglePerSegment = 3f;
    
    private Transform straightContainer;
    private Transform curveContainer;
    
    private void Start()
    {
        ClearPreviousTrack();
        CreateContainers();
        GenerateTrack();
    }

    private void ClearPreviousTrack()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private void CreateContainers()
    {
        straightContainer = new GameObject("Straight_Sections").transform;
        straightContainer.SetParent(transform);
        straightContainer.localPosition = Vector3.zero;
        
        curveContainer = new GameObject("Curve_Sections").transform;
        curveContainer.SetParent(transform);
        curveContainer.localPosition = Vector3.zero;
    }

    private void GenerateTrack()
    {
        Vector3 currentPos = Vector3.zero;
        float currentAngle = 0f;

        GenerateStraight(ref currentPos, ref currentAngle, 10, straightSegmentLength);
        GenerateCurve(ref currentPos, ref currentAngle, 30, curveSegmentLength, -curveAnglePerSegment);
        GenerateStraight(ref currentPos, ref currentAngle, 5, straightSegmentLength);
        GenerateCurve(ref currentPos, ref currentAngle, 30, curveSegmentLength, curveAnglePerSegment);
        GenerateStraight(ref currentPos, ref currentAngle, 10, straightSegmentLength);
    }

    private void GenerateStraight(ref Vector3 currentPos, ref float currentAngle, int segmentCount, float segmentLength)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            PlaceSegment(currentPos, currentAngle, segmentLength, false);
            currentPos += Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * segmentLength;
        }
    }

    private void GenerateCurve(ref Vector3 currentPos, ref float currentAngle, int segmentCount, float segmentLength, float anglePerSegment)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            PlaceSegment(currentPos, currentAngle, segmentLength, true);
            currentPos += Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * segmentLength;
            currentAngle += anglePerSegment;
        }
    }

    private void PlaceSegment(Vector3 position, float yRotation, float length, bool isCurve)
    {
        GameObject segment = new GameObject(isCurve ? "Curve_Segment" : "Straight_Segment");
        segment.transform.SetParent(isCurve ? curveContainer : straightContainer);
        segment.transform.position = position;
        segment.transform.rotation = Quaternion.Euler(0, yRotation, 0);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.SetParent(segment.transform);
        ground.transform.localPosition = new Vector3(0f, -0.5f, length * 0.5f);
        ground.transform.localScale = new Vector3(trackWidth, 1f, length);
        ground.name = "Ground";

        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = isCurve ? new Color(0.4f, 0.35f, 0.3f) : new Color(0.3f, 0.3f, 0.35f);
        }

        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.transform.SetParent(segment.transform);
        leftWall.transform.localPosition = new Vector3(-trackWidth * 0.5f, wallHeight * 0.5f, 0f);
        leftWall.transform.localScale = new Vector3(0.5f, wallHeight, length);
        leftWall.name = "LeftWall";

        Renderer leftWallRenderer = leftWall.GetComponent<Renderer>();
        if (leftWallRenderer != null)
        {
            leftWallRenderer.material.color = new Color(0.2f, 0.2f, 0.25f);
        }

        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.transform.SetParent(segment.transform);
        rightWall.transform.localPosition = new Vector3(trackWidth * 0.5f, wallHeight * 0.5f, 0f);
        rightWall.transform.localScale = new Vector3(0.5f, wallHeight, length);
        rightWall.name = "RightWall";

        Renderer rightWallRenderer = rightWall.GetComponent<Renderer>();
        if (rightWallRenderer != null)
        {
            rightWallRenderer.material.color = new Color(0.2f, 0.2f, 0.25f);
        }
    }
}
