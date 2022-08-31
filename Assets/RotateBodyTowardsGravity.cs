using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateBodyTowardsGravity : MonoBehaviour
{
    public Transform gravityBody;

    void FixedUpdate()
    {
        Vector3 dir = (transform.position - gravityBody.position).normalized;

        transform.rotation = Quaternion.FromToRotation(transform.up, dir) * transform.rotation;
    }
}
