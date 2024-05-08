using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Than.Physics3D
{
    [RequireComponent(typeof(PhysicsBody))]
    [DefaultExecutionOrder(1)]
    public abstract class PhysicsBodyModule : MonoBehaviour
    {
        [HideInInspector][SerializeField] public PhysicsBody physicsBody { get; private set; }
        [HideInInspector][SerializeField] bool physicsBodyAssigned = false;

        void GetPhysicsBody(bool force = false)
        {
            if (force || !physicsBodyAssigned)
            {
                physicsBodyAssigned = TryGetComponent<PhysicsBody>(out PhysicsBody pb);

                if (physicsBodyAssigned)
                    physicsBody = pb;
            }
        }

        void OnValidate()
        {
            GetPhysicsBody(true);
        }

        void Awake()
        {
            GetPhysicsBody();
        }
    }
}
