// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Config
{
    [CreateAssetMenu(menuName = "Aron/Enemy Config", fileName = "EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        public int health = 1;
        public float patrolSpeed = 2f;
        public float chaseSpeed = 4f;
        public float detectRadius = 7f;
        public float loseRadius = 11f;
        public float turnSpeed = 540f;
        public bool canChase = true;
        public int datesDropped = 1;
    }
}
