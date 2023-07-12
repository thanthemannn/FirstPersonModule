using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] int startingNode = 0;
    [Range(0, 1)][SerializeField] float platformTestFrac = 0;

    public bool closedLoop = false;
    public Transform platform;
    public Transform positionNodesRoot;
    Vector3 targetPos;
    Quaternion targetRot;
    Vector3 lastPos;
    Quaternion lastRot;

    float nodeDistance;
    int totalNodes;
    Transform[] nodes;
    public float speed = 1;

    int targetIndex = 1;
    int nextDir = 1;

    void Awake()
    {
        totalNodes = positionNodesRoot.childCount;
        nodes = positionNodesRoot.GetAllChildren();
        Transform lastNode = nodes[startingNode];
        platform.position = lastNode.position;
        platform.rotation = lastNode.rotation;

        targetIndex = startingNode + nextDir;
        SetTarget(targetIndex);
    }

    void FixedUpdate()
    {
        platform.position = Vector3.MoveTowards(platform.position, targetPos, speed * Time.fixedDeltaTime);

        float currentDist = Vector3.Distance(platform.position, targetPos);

        float t = 1 - currentDist / Vector3.Distance(lastPos, targetPos);
        if (float.IsNaN(t)) t = 1;
        platform.rotation = Quaternion.Lerp(lastRot, targetRot, t);

        //*Find next target
        if (t >= 1)
        {
            if (!closedLoop && (targetIndex == 0 || targetIndex == totalNodes - 1))
            {
                nextDir *= -1;
            }

            targetIndex = UMath.Mod(targetIndex + nextDir, totalNodes);

            SetTarget(targetIndex);
        }
    }

    void SetTarget(int index)
    {
        targetIndex = index;

        lastPos = platform.position;
        lastRot = platform.rotation;

        Transform targetNode = nodes[targetIndex];
        targetPos = targetNode.position;
        targetRot = targetNode.rotation;

        nodeDistance = Vector3.Distance(lastPos, targetPos);
    }

    void OnValidate()
    {
        totalNodes = positionNodesRoot.childCount;
        nodes = positionNodesRoot.GetAllChildren();

        if (Application.isPlaying)
            return;

        int lastNode = totalNodes - (closedLoop ? 1 : 2);
        startingNode = Mathf.Clamp(startingNode, 0, lastNode);
        Transform start = nodes[startingNode];
        Transform end = nodes[(startingNode + 1) % totalNodes];
        platform.position = Vector3.Lerp(start.position, end.position, platformTestFrac);
        platform.rotation = Quaternion.Lerp(start.rotation, end.rotation, platformTestFrac);
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(MovingPlatform))]
public class MovingPlatformEditor : Editor
{
    void Awake()
    {
        Selection.selectionChanged -= SelectionChanged;
        Selection.selectionChanged += SelectionChanged;
    }

    void SelectionChanged()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;

        if (!target)
        {
            Selection.selectionChanged -= SelectionChanged;
            return;
        }

        MovingPlatform p = target as MovingPlatform;
        if (!p.isActiveAndEnabled || Selection.activeGameObject == null)
            return;

        if (Selection.activeGameObject.transform.UnderParent(p.transform))
            SceneView.duringSceneGui += this.OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!target)
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
            return;
        }

        MovingPlatform p = target as MovingPlatform;
        if (!p.isActiveAndEnabled)
            return;

        int len = p.positionNodesRoot.childCount;

        Transform first = p.positionNodesRoot.GetChild(0);
        Transform prev = first;
        DrawNode(prev);
        for (int i = 1; i < len; i++)
        {
            Transform next = p.positionNodesRoot.GetChild(i);

            DrawNode(next);

            DrawLine(prev.position, next.position, p.closedLoop);
            prev = next;
        }

        if (p.closedLoop)
        {
            Transform next = first;
            DrawLine(prev.position, next.position, true);
        }
    }

    void DrawLine(Vector3 start, Vector3 end, bool closedLoop)
    {
        Handles.color = Color.yellow;
        Handles.DrawLine(start, end);

        Vector3 half = Vector3.Lerp(start, end, .5f);
        Quaternion rot = Quaternion.LookRotation(start.DirectionTowards(end));

        float size = .2f;
        Vector3 nudge = Vector3.zero;

        if (!closedLoop)
        {
            nudge = rot * Vector3.forward * size * .5f;
            Handles.ConeHandleCap(0, half - nudge, rot * Quaternion.Euler(0, 180, 0), size, EventType.Repaint);
        }

        Handles.ConeHandleCap(0, half + nudge, rot, .2f, EventType.Repaint);
    }

    void DrawNode(Transform transform)
    {
        if (Selection.activeObject == transform.gameObject)
            return;

        Handles.color = Handles.xAxisColor;
        Handles.ArrowHandleCap(0, transform.position, transform.rotation * Quaternion.Euler(0, 90, 0), .3f, EventType.Repaint);

        Handles.color = Handles.yAxisColor;
        Handles.ArrowHandleCap(0, transform.position, transform.rotation * Quaternion.Euler(-90, 0, 0), .3f, EventType.Repaint);

        Handles.color = Handles.zAxisColor;
        Handles.ArrowHandleCap(0, transform.position, transform.rotation, .3f, EventType.Repaint);
    }
}


#endif
