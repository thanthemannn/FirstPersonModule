using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace Than
{
    [System.Serializable]
    public class Pool<T> where T : MonoBehaviour
    {
        public AssetReferenceGameObject addressableReference;
        public int prewarmSize = 3;
        public bool allowInstantiationBeyondPrewarm = true;

        public List<T> activeObjects { get; private set; } = new List<T>();
        public Stack<T> inactivePooled { get; private set; } = new Stack<T>();
        public bool setup { get; private set; } = false;

        public async void Setup()
        {
            setup = true;

            prewarmTask = Prewarm(prewarmSize);
            await prewarmTask;
        }

        public Task prewarmTask { get; private set; }
        public async Task Prewarm(int count, Transform assignParent = null)
        {
            Task<T>[] prewarmTasks = new Task<T>[prewarmSize];
            for (int i = 0; i < prewarmSize; i++)
            {
                prewarmTasks[i] = CreatePooledItem(assignParent);
                ReturnToPool(prewarmTasks[i]);
            }

            await Task.WhenAll(prewarmTasks);
        }

        async void ReturnToPool(Task<T> task)
        {
            await task;
            ReturnToPool(task.Result, false);
        }

        public void ReturnAllToPool()
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                ReturnToPool(activeObjects[i], true);
            }
        }

        public void ReturnToPool(T obj, bool runChecks = true)
        {
            if (!runChecks || activeObjects.Contains(obj))
                activeObjects.Remove(obj);

            if (!runChecks || !inactivePooled.Contains(obj))
            {
                inactivePooled.Push(obj);

                if (!runChecks || obj.gameObject.activeSelf)
                    obj.gameObject.SetActive(false);
            }
        }

        async Task<T> CreatePooledItem(Transform parent = null)
        {
            var creation = addressableReference.InstantiateAsync(parent);
            Task<GameObject> createTask = creation.Task;
            await createTask;

            T obj = createTask.Result.GetComponent<T>();

            return obj;
        }

        public async Task<T[]> Get(int count, bool active = true, Transform assignParent = null, System.Action<T> action = null)
        {
            T[] objs = new T[count];

            for (int i = 0; i < count; i++)
            {
                if (inactivePooled.Count > 0)
                {
                    objs[i] = inactivePooled.Pop();
                }
                else if (allowInstantiationBeyondPrewarm)
                {
                    var task = CreatePooledItem(assignParent);
                    await task;
                    objs[i] = task.Result;
                }
                else
                {
                    break;
                }

                objs[i].gameObject.SetActive(active);
            }

            activeObjects.AddRange(objs);

            if (action != null)
            {
                for (int i = 0; i < count; i++)
                    action.Invoke(objs[i]);
            }

            return objs;
        }

        public async Task<T> Get(bool active = true, Transform assignParent = null, System.Action<T> action = null)
        {
            var task = Get(1, active, assignParent, action);
            await task;
            return task.Result[0];
        }
    }
}