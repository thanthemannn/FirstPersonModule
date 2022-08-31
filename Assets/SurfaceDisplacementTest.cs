using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class SurfaceDisplacementTest : MonoBehaviour
{
    Material materialInstance;
    public Transform displacementTransform;

    static readonly int HASH_DISPLACE_POSITION = Shader.PropertyToID("_DisplacePosition");
    static readonly int HASH_DISPLACE_RADIUS = Shader.PropertyToID("_DisplaceRadius");
    static readonly int HASH_DISPLACE_FREQUENCY = Shader.PropertyToID("_Frequency");
    static readonly int HASH_DISPLACE_AMOUNT = Shader.PropertyToID("_Amount");
    static readonly int HASH_DISPLACE_SPEED = Shader.PropertyToID("_Speed");

    void Start()
    {
        materialInstance = GetComponent<MeshRenderer>().material;

        displacementTransform.position = materialInstance.GetVector(HASH_DISPLACE_POSITION);
        displacementTransform.localScale = new Vector3(materialInstance.GetFloat(HASH_DISPLACE_RADIUS), materialInstance.GetFloat(HASH_DISPLACE_AMOUNT), materialInstance.GetFloat(HASH_DISPLACE_SPEED));
    }

    void LateUpdate()
    {
        materialInstance.SetVector(HASH_DISPLACE_POSITION, displacementTransform.position);
        materialInstance.SetFloat(HASH_DISPLACE_RADIUS, displacementTransform.localScale.x);

        materialInstance.SetFloat(HASH_DISPLACE_AMOUNT, displacementTransform.localScale.y);
        materialInstance.SetFloat(HASH_DISPLACE_SPEED, displacementTransform.localScale.z);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(displacementTransform.position, displacementTransform.localScale.x);
    }
}
