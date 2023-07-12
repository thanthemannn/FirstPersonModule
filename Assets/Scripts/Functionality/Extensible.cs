using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;

using UnityEngine;
using UnityEngine.Events;

public static class Extensible
{

    public class RaycastByDistance : IComparer
    {
        int IComparer.Compare(object a, object b)
        {
            RaycastHit hit1 = (RaycastHit)a;
            RaycastHit hit2 = (RaycastHit)b;
            return hit1.distance.CompareTo(hit2.distance);
        }
    }

    [Flags]
    public enum Directions
    {
        up = 1 << 0,
        down = 1 << 1,
        left = 1 << 2,
        right = 1 << 3
    }

    #region Timer

    public static string ToDebugString(this System.Diagnostics.Stopwatch stopWatch, string label = "StopWatch")
    {
        return string.Format("{0} : {1} ticks | {2} ms", label, stopWatch.ElapsedTicks, stopWatch.ElapsedMilliseconds);
    }

    #endregion

    #region Mesh

    public static Mesh CreateCube(float halfSize = 1)
    {
        Vector3[] vertices = {
            new Vector3 (0, 0, 0),
            new Vector3 (halfSize, 0, 0),
            new Vector3 (halfSize, halfSize, 0),
            new Vector3 (0, halfSize, 0),
            new Vector3 (0, halfSize, halfSize),
            new Vector3 (halfSize, halfSize, halfSize),
            new Vector3 (halfSize, 0, halfSize),
            new Vector3 (0, 0, halfSize),
        };

        int[] triangles = {
            0, 2, 1, //face front
			0, 3, 2,
            2, 3, 4, //face top
			2, 4, 5,
            1, 2, 5, //face right
			1, 5, 6,
            0, 7, 4, //face left
			0, 4, 3,
            5, 4, 7, //face back
			5, 7, 6,
            0, 6, 7, //face bottom
			0, 1, 6
        };

        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.Optimize();
        mesh.RecalculateNormals();
        return mesh;
    }

    #endregion

    #region Vector2

    public static Vector2 ApplyDeadzone(this Vector2 vector2, float deadzone)
    {
        //Squaring deadzone is more efficient than getting the square route of magnitude
        if (vector2.sqrMagnitude < UMath.IntPow(deadzone))
            return Vector2.zero;

        return vector2;
    }
    public static Vector2 DirectionTowards(this Vector2 vector2, Vector2 target)
    {
        return (target - vector2).normalized;
    }

    /// <summary>
    /// Returns the degree (360 degrees, 180 - 180) of the provided Vector2, relative to Vector2.zero.
    /// </summary>
    public static float ToDeg(this Vector2 direction) => direction.ToRad() * Mathf.Rad2Deg;
    public static float ToDeg(this Vector2Int direction) => direction.ToRad() * Mathf.Rad2Deg;

    public static float ToRad(this Vector2 direction) => Mathf.Atan2(direction.y, direction.x);
    public static float ToRad(this Vector2Int direction) => Mathf.Atan2(direction.y, direction.x);

    public static Vector2 SwapCoordinates(this Vector2 vector)
    {
        return new Vector2(vector.y, vector.x);
    }

    public enum Dimension2 { x, y }

    /// <summary>
    /// Returns a vector2 with the changed value in the chosen dimension. Keeps the other dimension the same as source.
    /// </summary>
    public static Vector2 SetDimension(this Vector2 vector2, float value, Dimension2 dimension)
    {
        switch (dimension)
        {
            case Dimension2.x:
                return new Vector2(value, vector2.y);
            case Dimension2.y:
                return new Vector2(vector2.x, value);
        }

        return vector2;
    }

    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;
        //v.x = (cos * tx) - (sin * ty);
        //v.y = (sin * tx) + (cos * ty);
        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    /// <summary>
    /// Returns a vector2 with the changed x in the chosen dimension. Keeps y the same as source.
    /// </summary>
    public static Vector2 SetX(this Vector2 vector2, float value) { return vector2.SetDimension(value, Dimension2.x); }
    /// <summary>
    /// Returns a vector2 with the changed y in the chosen dimension. Keeps x the same as source.
    /// </summary>
    public static Vector2 SetY(this Vector2 vector2, float value) { return vector2.SetDimension(value, Dimension2.y); }

    #endregion

    #region Vector3
    public static Vector3 DirectionTowards(this Vector3 vector3, Vector3 target)
    {
        return (target - vector3).normalized;
    }

    public enum Dimension3 { x, y, z }
    /// <summary>
    /// Returns a vector3 with the changed value in the chosen dimension. Keeps the other dimensions the same as source.
    /// </summary>
    public static Vector3 SetDimension(this Vector3 vector3, float value, Dimension3 dimension)
    {
        switch (dimension)
        {
            case Dimension3.x:
                return new Vector3(value, vector3.y, vector3.z);
            case Dimension3.y:
                return new Vector3(vector3.x, value, vector3.z);
            case Dimension3.z:
                return new Vector3(vector3.x, vector3.y, value);
        }

        return vector3;
    }

    public static Vector3 RotateAroundPivot(this Vector3 point, Vector3 pivot, Quaternion angle)
    {
        Vector3 dir = point - pivot;
        dir = angle * dir;
        point = dir + pivot;
        return point;
    }


    /// <summary>
    /// Returns a vector3 with the changed x in the chosen dimension. Keeps the other dimensions the same as source.
    /// </summary>
    public static Vector3 SetX(this Vector3 vector3, float value) { return vector3.SetDimension(value, Dimension3.x); }
    /// <summary>
    /// Returns a vector3 with the changed y in the chosen dimension. Keeps the other dimensions the same as source.
    /// </summary>
    public static Vector3 SetY(this Vector3 vector3, float value) { return vector3.SetDimension(value, Dimension3.y); }
    /// <summary>
    /// Returns a vector3 with the changed z in the chosen dimension. Keeps the other dimensions the same as source.
    /// </summary>
    public static Vector3 SetZ(this Vector3 vector3, float value) { return vector3.SetDimension(value, Dimension3.z); }

    public static Vector2 To2D(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.y);
    }

    #endregion

    #region Quaternion

    public static Quaternion Difference(this Quaternion from, Quaternion to)
    {
        return to * Quaternion.Inverse(from);
    }

    #endregion

    #region LayerMask

    public static bool ContainsLayer(this LayerMask mask, int layer)
    {
        return mask == (mask | (1 << layer));
    }

    private static Dictionary<int, int> _masksByLayer2D = new Dictionary<int, int>();
    public static LayerMask GetLayerMaskFromCollisionMatrix2D(this int layer)
    {
        if (_masksByLayer2D.TryGetValue(layer, out int layerMask))
            return layerMask;

        for (int i = 0; i < 32; i++)
        {
            if (!Physics2D.GetIgnoreLayerCollision(layer, i))
            {
                layerMask = layerMask | 1 << i;
            }
        }

        _masksByLayer2D.Add(layer, layerMask);
        return layerMask;
    }

    private static Dictionary<int, int> _masksByLayer3D = new Dictionary<int, int>();
    public static LayerMask GetLayerMaskFromCollisionMatrix(this int layer)
    {
        if (_masksByLayer3D.TryGetValue(layer, out int layerMask))
            return layerMask;

        for (int i = 0; i < 32; i++)
        {
            if (!Physics.GetIgnoreLayerCollision(layer, i))
            {
                layerMask = layerMask | 1 << i;
            }
        }

        _masksByLayer3D.Add(layer, layerMask);
        return layerMask;
    }

    #endregion

    #region CharacterController

    public static bool Cast(this CharacterController controller, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
    {
        return controller.Cast(direction, out hitInfo, maxDistance, ~0);
    }

    public static bool Cast(this CharacterController controller, Vector3 direction, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask)
    {
        Vector3 pos = controller.transform.position + controller.center;
        Vector3 offset = Vector3.up * (controller.height * .5f - controller.radius);
        return Physics.CapsuleCast(pos - offset, pos + offset, controller.radius, direction, out hitInfo, maxDistance, layerMask);
    }



    #endregion

    #region Collider

    public static bool Cast(this CapsuleCollider collider, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
    {
        return collider.Cast(direction, out hitInfo, maxDistance, ~0);
    }

    public static bool Cast(this CapsuleCollider collider, Vector3 direction, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask)
    {
        Vector3 offsetDir = collider.transform.up;
        if (collider.direction == 0)
            offsetDir = collider.transform.right;
        else if (collider.direction == 2)
            offsetDir = collider.transform.forward;

        Vector3 pos = collider.transform.position + collider.center;
        Vector3 offset = offsetDir * (collider.height * .5f - collider.radius);
        return Physics.CapsuleCast(pos - offset, pos + offset, collider.radius, direction, out hitInfo, maxDistance, layerMask);
    }

    #endregion

    #region Transform

    public static bool UnderParent(this Transform transform, Transform parent)
    {
        if (transform == parent)
            return true;


        Transform p = transform.parent;
        if (p != null)
            return UnderParent(p, parent);
        else
            return false;
    }

    public static Vector2 position2D(this Transform tform)
    {
        return tform.position;
    }

    static public Transform GetChild(this Transform transform, string tag) //Find child by tag
    {
        int children = transform.childCount;
        for (int i = 0; i < children; ++i) //search to see if there are any children that are within the specified layer
        {
            if (transform.GetChild(i).gameObject.tag == tag)
                return transform.GetChild(i);
        }
        return null;
    }

    static public Transform GetChildByName(this Transform transform, string name, bool includeChildrenChildren = false) //Find child by name
    {
        int children = transform.childCount;
        for (int i = 0; i < children; ++i) //search to see if there are any children that are within the specified layer
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.name == name)
                return child;
            else if (includeChildrenChildren && child.childCount > 0)
            {
                Transform newChild = child.GetChildByName(name, true);
                if (newChild != null)
                    return newChild;
            }
        }
        return null;
    }

    static public Transform[] GetChildren(this Transform transform, bool includeSubChildren = false)
    {
        if (includeSubChildren)
            return GetAllChildren(transform);

        if (transform.childCount > 0) //if the object has children
        {
            Transform[] children = new Transform[transform.childCount];
            for (int i = 0; i < children.Length; i++) //place each child in array
            {
                children[i] = transform.GetChild(i);
            }
            return children;
        }
        else
        {
            return null;
        }
    }

    static public Transform[] GetAllChildren(this Transform transform)
    {
        List<Transform> children = new List<Transform>();
        children.AddRange(transform.GetChildren(false));

        int count = children.Count;
        for (int i = 0; i < count; i++)
        {
            if (children[i].childCount > 0)
                children.AddRange(children[i].GetAllChildren());
        }

        return children.ToArray();
    }

    /// <summary>
    /// Finds the Monobehaviour nearest the transform within the given list.
    /// </summary>
    public static T FindNearest<T>(this Transform focalTransform, List<T> objects) where T : MonoBehaviour
    {
        if (objects.Count == 0)
            return null;

        T nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (T obj in objects)
        {
            float dist = UMath.SqrDistance(focalTransform.position, obj.transform.position);

            if (dist < nearestDist)
            {
                nearest = obj;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Finds the GameObject nearest the transform within the given list.
    /// </summary>
    public static GameObject FindNearest(this Transform focalTransform, List<GameObject> objects)
    {
        if (objects.Count == 0)
            return null;

        GameObject nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (GameObject obj in objects)
        {
            float dist = UMath.SqrDistance(focalTransform.position, obj.transform.position);

            if (dist < nearestDist)
            {
                nearest = obj;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    public static Vector3 GoTo(this Transform transform, Vector2 position, bool keepZ = true)
    {
        return transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    public static Vector3 LocalGoTo(this Transform transform, Vector2 position, bool keepZ = true)
    {
        return transform.localPosition = new Vector3(position.x, position.y, transform.localPosition.z);
    }

    #endregion

    #region Linq
    public static void ForEach<T>(this ICollection<T> source, System.Action<T> action)
    {
        foreach (T element in source)
        {
            action(element);
        }
    }

    public static T GetFromType<T, G>(this ICollection<G> source) where T : G
    {
        return source.OfType<T>().FirstOrDefault();
    }

    public static T[] GetComponents<T>(this ICollection<Component> source) where T : Component
    {
        List<T> components = new List<T>();
        foreach (Component element in source)
        {
            T c = element.GetComponent<T>();
            if (c)
                components.Add(c);
        }
        return components.ToArray();
    }

    /// <summary>Usage: int i = list.BinarySearch(a => a.IntProp == 1);
    ///<para>See: https://www.growingwiththeweb.com/2013/01/a-list-extension-that-takes-lambda.html</para></summary>
    public static int BinarySearch<T>(this List<T> list, Func<T, int> compare)
    where T : class
    {
        Func<T, T, int> newCompare = (a, b) => compare(a);
        return list.BinarySearch((T)null, newCompare);
    }

    /// <summary>Usage: list.BinarySearch(item, (a, b) => a.IntProp.CompareTo(b.IntProp));
    ///<para>See: https://www.growingwiththeweb.com/2013/01/a-list-extension-that-takes-lambda.html</para></summary>
    public static int BinarySearch<T>(this List<T> list,
        T item,
        Func<T, T, int> compare)
    {
        return list.BinarySearch(item, new ComparisonComparer<T>(compare));
    }

    /// <summary>Usage: SomeType obj = list.BinarySearchOrDefault(item, (a, b) => a.IntProp.CompareTo(b.IntProp));
    ///<para>See: https://www.growingwiththeweb.com/2013/01/a-list-extension-that-takes-lambda.html</para></summary>
    public static T BinarySearchOrDefault<T>(this List<T> list,
        T item,
        Func<T, T, int> compare)
    {
        int i = list.BinarySearch(item, compare);
        if (i >= 0)
            return list[i];
        return default(T);
    }

    public class ComparisonComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        public ComparisonComparer(Func<T, T, int> compare)
        {
            if (compare == null)
            {
                throw new ArgumentNullException("comparison");
            }
            comparison = new Comparison<T>(compare);
        }

        public int Compare(T x, T y)
        {
            return comparison(x, y);
        }
    }
    #endregion

    #region GameObject
    public static Collider2D GetFootCollider(this GameObject obj, bool returnRootColliderIfNull = true)
    {
        Transform footChild = obj.transform.GetChild("FootCollider");
        if (footChild && footChild.GetComponent<Collider2D>())
            return footChild.GetComponent<Collider2D>();
        else if (returnRootColliderIfNull)
            return obj.GetComponent<Collider2D>();
        else
            return null;
    }

    /// <summary>
    /// If this object (or object parent) is the same as the provided object
    /// </summary>
    public static bool IsObjectOrParentObject(this GameObject thisObj, GameObject obj)
    {
        if (thisObj == obj)
            return true;

        if (thisObj.transform.parent && thisObj.transform.parent == obj)
            return true;

        return false;
    }

    public static bool IsPrefab(this GameObject thisObj)
    {
        return thisObj.scene.name == null;
    }

    #endregion

    #region ScriptableObject

    /// <summary>
    /// Creates and returns a clone of any given scriptable object.
    /// </summary>
    public static T Clone<T>(this T scriptableObject) where T : ScriptableObject
    {
        return scriptableObject.Clone(scriptableObject.name);
    }

    /// <summary>
    /// Creates and returns a clone of any given scriptable object.
    /// </summary>
    public static T Clone<T>(this T scriptableObject, string name) where T : ScriptableObject
    {
        if (scriptableObject == null)
        {
            Debug.LogError($"ScriptableObject was null. Returning default {typeof(T)} object.");
            return (T)ScriptableObject.CreateInstance(typeof(T));
        }

        T instance = UnityEngine.Object.Instantiate(scriptableObject);
        instance.name = name; // remove (Clone) from name
        return instance;
    }

    #endregion

    #region Animator

    //Checks if animator has parameter of the name
    //Should be assigned to a bool at initialization, as it can be hefty to run every frame
    public static bool HasParameter(this Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    public static bool HasParameter(this Animator animator, int paramHash)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == paramHash)
                return true;
        }
        return false;
    }

    public static bool[] HasParameters(this Animator animator, string[] paramHashes)
    {
        int[] hashes = paramHashes.Select(p => Animator.StringToHash(p)).ToArray();
        return animator.HasParameters(hashes);
    }

    public static bool[] HasParameters(this Animator animator, int[] paramHashes)
    {
        int len = paramHashes.Length;
        bool[] hasParameters = new bool[len];

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            for (int i = 0; i < len; i++)
            {
                if (param.nameHash == paramHashes[i])
                    hasParameters[i] = true;
            }
        }

        return hasParameters;
    }

    //Checks if animator has parameter of the name and type
    //Should be assigned to a bool at initialization, as it can be hefty to run every frame
    public static bool HasParameter(this Animator animator, string paramName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == type && param.name == paramName)
                return true;
        }
        return false;
    }

    public static bool HasParameter(this Animator animator, int paramHash, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == type && param.nameHash == paramHash)
                return true;
        }
        return false;
    }

    public static bool CheckSetBool(this Animator animator, string name, bool value) => CheckSetBool(animator, Animator.StringToHash(name), value);

    public static bool CheckSetBool(this Animator animator, int hash, bool value)
    {
        if (animator.HasParameter(hash, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(hash, value);
            return true;
        }

        return false;
    }

    public static bool CheckSetFloat(this Animator animator, string name, float value) => CheckSetFloat(animator, Animator.StringToHash(name), value);

    public static bool CheckSetFloat(this Animator animator, int hash, float value)
    {
        if (animator.HasParameter(hash, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(hash, value);
            return true;
        }

        return false;
    }

    public static bool CheckSetInteger(this Animator animator, string name, int value) => CheckSetInteger(animator, Animator.StringToHash(name), value);
    public static bool CheckSetInteger(this Animator animator, int hash, int value)
    {
        if (animator.HasParameter(hash, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(hash, value);
            return true;
        }

        return false;
    }

    public static bool CheckSetTrigger(this Animator animator, string name) => CheckSetTrigger(animator, Animator.StringToHash(name));

    public static bool CheckSetTrigger(this Animator animator, int hash)
    {
        if (animator.HasParameter(hash, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(hash);
            return true;
        }

        return false;
    }

    public static bool CheckResetTrigger(this Animator animator, string name) => CheckResetTrigger(animator, Animator.StringToHash(name));
    public static bool CheckResetTrigger(this Animator animator, int hash)
    {
        if (animator.HasParameter(hash, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(hash);
            return true;
        }

        return false;
    }

    #endregion

    #region Component

    public static T[] GetArrayComponents<T>(this Component[] array) where T : Component
    {
        return array.Select(o => o.GetComponent<T>()).ToArray();
    }

    public static T GetComponentInChildrenOnly<T>(this Component component) where T : Component
    {
        T[] comps = component.GetComponentsInChildren<T>();

        foreach (T comp in comps)
        {
            if (comp.gameObject != component.gameObject)
                return comp;
        }

        return null;
    }

    ///////////////////////////////////////////////////////////
    // Essentially a reimplementation of 
    // GameObject.GetComponentInChildren< T >()
    // Major difference is that this DOES NOT skip deactivated 
    // game objects
    ///////////////////////////////////////////////////////////
    public static T GetComponentInAllChildren<T>(this Component component) where T : Component
    {
        // if we don't find the component in this object 
        // recursively iterate children until we do
        T tRetComponent = component.GetComponent<T>();

        if (null == tRetComponent)
        {
            // transform is what makes the hierarchy of GameObjects, so 
            // need to access it to iterate children
            Transform trnsRoot = component.transform;
            int iNumChildren = trnsRoot.childCount;

            // could have used foreach(), but it causes GC churn
            for (int iChild = 0; iChild < iNumChildren; ++iChild)
            {
                // recursive call to this function for each child
                // break out of the loop and return as soon as we find 
                // a component of the specified type
                tRetComponent = trnsRoot.GetChild(iChild).GetComponentInAllChildren<T>();
                if (null != tRetComponent)
                {
                    break;
                }
            }
        }

        return tRetComponent;
    }

    #endregion

    #region Bool
    public static int ToInt(this bool boolean)
    {
        return boolean ? 1 : 0;
    }

    public static int ToSign(this bool boolean)
    {
        return boolean ? 1 : -1;
    }
    #endregion

    #region Int
    public static bool ToBool(this int integer)
    {
        return integer > 0 ? true : false;
    }
    #endregion

    #region String

    #endregion

    #region Color

    // public static void SetAlpha(this ref Color color, float alpha)
    // {
    //     Color c = color;
    //     c.a = alpha;
    //     color = c;
    // }

    public static Color WithAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    public static string ToHexString(this Color color, bool includeAlpha = false)
    {
        string hex = UMath.PercentToHexString(color.r) + UMath.PercentToHexString(color.g) + UMath.PercentToHexString(color.b);

        if (includeAlpha)
            hex += UMath.PercentToHexString(color.a);

        return hex;
    }

    public static Color Invert(this Color color)
    {
        return new Color(1 - color.r, 1 - color.g, 1 - color.b);
    }

    #endregion

    #region  AnimationCurve

    public static float FirstTime(this AnimationCurve curve) => curve.keys[0].time;
    public static float LastTime(this AnimationCurve curve) => curve.keys[curve.keys.Length - 1].time;
    public static float FirstValue(this AnimationCurve curve) => curve.keys[0].value;
    public static float LastValue(this AnimationCurve curve) => curve.keys[curve.keys.Length - 1].value;

    /// <summary>
    /// Coroutine that uses curve to play an animation and repeatedly invoke a callback delegate with the curves evaluation as the float value parameter.
    /// </summary>
    /// <param name="curve">The animation curve used.</param>
    /// <param name="stepAction">The callback function assigned for every time the curve has been evaluated within the coroutine.</param>
    /// <param name="deltaTimeModifier">Callback with a float return type that can be used to modify the speed that the coroutine progresses.</param>
    /// <returns>IEnumerator: Should be used within the Monobehaviour StartCoroutine(IEnumerator coroutine) method.</returns>
    public static IEnumerator Animate(this AnimationCurve curve, System.Action<float> stepAction, Func<float> deltaTimeModifier = null)
    {
        bool hasTimeMod = deltaTimeModifier != null;

        float endTime = curve.LastTime();
        for (float t = 0; t < endTime; t += hasTimeMod ? deltaTimeModifier.Invoke() * Time.deltaTime : Time.deltaTime)
        {
            stepAction.Invoke(curve.Evaluate(t));
            yield return null;
        }
        stepAction.Invoke(curve.Evaluate(endTime));
    }

    /// <summary>
    /// Coroutine that uses curve to play an animation (in reverse) and repeatedly invoke a callback delegate with the curves evaluation as the float value parameter.
    /// </summary>
    /// <param name="curve">The animation curve used.</param>
    /// <param name="stepAction">The callback function assigned for every time the curve has been evaluated within the coroutine.</param>
    /// <param name="deltaTimeModifier">Callback with a float return type that can be used to modify the speed that the coroutine progresses.</param>
    /// <returns>IEnumerator: Should be used within the Monobehaviour StartCoroutine(IEnumerator coroutine) method.</returns>
    public static IEnumerator AnimateReverse(this AnimationCurve curve, System.Action<float> stepAction, Func<float> deltaTimeModifier = null)
    {
        bool hasTimeMod = deltaTimeModifier != null;

        for (float t = curve.LastTime(); t > 0; t -= hasTimeMod ? deltaTimeModifier.Invoke() * Time.deltaTime : Time.deltaTime)
        {
            stepAction.Invoke(curve.Evaluate(t));

            yield return null;
        }
        stepAction.Invoke(curve.Evaluate(0));
    }

    #endregion

    #region RenderTexture

    public static Texture2D ToTexture2D(this RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, rTex.graphicsFormat, TextureCreationFlags.None);
        var old_rt = RenderTexture.active;
        RenderTexture.active = rTex;

        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = old_rt;
        return tex;
    }

    #endregion

    #region LineRenderer

    /// <summary>
    /// Get the length of all the line renderer points.
    /// </summary>
    public static float GetLength(this LineRenderer lineRenderer)
    {
        return lineRenderer.GetLength(0, lineRenderer.positionCount - 1);
    }

    /// <summary>
    /// Get the length of all the line renderer points from a specified start to end point.
    /// </summary>
    public static float GetLength(this LineRenderer lineRenderer, int startPointIndex, int endPointIndex)
    {
        //Make sure we don't overflow the position array
        int lastPoint = lineRenderer.positionCount;
        if (startPointIndex >= lastPoint)
            startPointIndex = lastPoint - 1;
        if (endPointIndex >= lastPoint)
            endPointIndex = lastPoint - 1;

        //return zero if checking the same index
        if (startPointIndex == endPointIndex)
            return 0;

        //Make sure startPoint is less than endPoint
        if (startPointIndex > endPointIndex)
        {
            int buffer = endPointIndex;
            endPointIndex = startPointIndex;
            startPointIndex = buffer;
        }

        //Add all the points between start and end
        float length = 0;
        for (int i = startPointIndex; i < endPointIndex; i++)
        {
            length += Vector2.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
        }

        return length;
    }

    #endregion

    #region GameObject[]

    public static Transform[] GetTransforms(this GameObject[] gameObjects)
    {
        List<Transform> transforms = new List<Transform>();
        foreach (GameObject obj in gameObjects)
            transforms.Add(obj.transform);

        return transforms.ToArray();
    }

    #endregion

    #region Array

    public static void Clear(this System.Object[] array, int stopIndex = -1) => array.Clear(0, stopIndex);
    public static void Clear(this System.Object[] array, int startIndex, int stopIndex)
    {
        if (stopIndex < 0) stopIndex = array.Length;
        for (int i = startIndex; i < stopIndex; i++)
            array[i] = null;
    }

    #endregion

    #region List

    ///<summary> Only adds value to list if not already a part of the list</summary>
    public static void AddExclusive<T>(this List<T> list, T value)
    {
        if (!list.Contains(value))
            list.Add(value);
    }

    #endregion

    #region Enum

    public static IEnumerable<Enum> GetFlags(this Enum input)
    {
        foreach (Enum value in Enum.GetValues(input.GetType()))
            if (Convert.ToInt32(value) != 0 && input.HasFlag(value))
                yield return value;
    }

    #endregion

    #region InputAction

    public static bool ReadButton(this InputAction inputAction)
    {
        return inputAction.ReadValue<float>() > .5f;
    }

    #endregion


    #region AudioSource

    public static async void PlayOneShotDelayed(this AudioSource source, AudioClip clip, float delay)
    {
        if (delay > 0)
            await Task.Delay((int)(delay * 1000));

        source.PlayOneShot(clip);
    }

    #endregion
}

[System.Serializable] public class UnityEventInt : UnityEvent<int> { }

[System.Serializable] public class UnityEventFloat : UnityEvent<float> { }

[System.Serializable] public class UnityEventString : UnityEvent<string> { }