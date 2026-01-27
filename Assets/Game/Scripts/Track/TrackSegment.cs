using UnityEngine;

public class TrackSegment : MonoBehaviour
{
    [Header("Segment Info")]
    [SerializeField] private float segmentLength = 20f;
    [SerializeField] private bool isCurve = false;
    
    public float SegmentLength => segmentLength;
    public bool IsCurve => isCurve;

    private void OnDrawGizmos()
    {
        Gizmos.color = isCurve ? Color.cyan : Color.green;
        Gizmos.DrawWireCube(transform.position + transform.forward * segmentLength * 0.5f, 
                           new Vector3(10f, 0.5f, segmentLength));
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}
