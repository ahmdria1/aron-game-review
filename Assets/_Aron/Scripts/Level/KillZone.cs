// (c) Ahmed Alamrani. All rights reserved.
using Aron.Player;
using UnityEngine;

namespace Aron.Level
{
    /// <summary>Falling off the map. Always lethal: into the water on coast and river stages, a pit elsewhere.</summary>
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerController>();
            if (p == null) return;
            var stage = Stages.StageBuilder.Current != null ? Stages.StageBuilder.Current.Stage : null;
            p.Die(Logic.DeathActs.ForFall(stage != null ? stage.terrain : null));
        }
    }
}
