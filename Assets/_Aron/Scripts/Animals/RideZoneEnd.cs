// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Animals
{
    /// <summary>A hitching post at the edge of the desert. Riding in here gets you off; the animal stays tied up.</summary>
    public class RideZoneEnd : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var rider = other.GetComponentInParent<Rider>();
            if (rider != null && rider.IsMounted) rider.Dismount();
        }
    }
}
