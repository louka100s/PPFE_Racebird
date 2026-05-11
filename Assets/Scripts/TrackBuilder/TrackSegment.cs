using UnityEngine;

namespace Racebird.TrackBuilding
{
    /// <summary>
    /// Defines the entry and exit connection points for a track segment.
    /// Place on each route prefab with two child GameObjects as anchors.
    /// </summary>
    public class TrackSegment : MonoBehaviour
    {
        [Tooltip("Child transform at the beginning of the segment, oriented in the travel direction.")]
        public Transform entryPoint;

        [Tooltip("Child transform at the end of the segment, oriented in the travel direction.")]
        public Transform exitPoint;
    }
}
