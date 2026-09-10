using Match2.View;
using UnityEngine;
using UnityEngine.Pool;

namespace Match2.Systems.Pooling
{
    /// <summary>
    /// Wraps Unity's built-in <see cref="ObjectPool{T}"/> around the block
    /// prefab so gameplay never calls Instantiate/Destroy directly.
    /// </summary>
    public class BlockPoolService
    {
        private readonly ObjectPool<BlockView> pool;

        public BlockPoolService(BlockView prefab, Transform parent, int defaultCapacity = 64, int maxSize = 512)
        {
            pool = new ObjectPool<BlockView>(
                createFunc: () => Object.Instantiate(prefab, parent),
                actionOnGet: view => view.gameObject.SetActive(true),
                actionOnRelease: view => view.gameObject.SetActive(false),
                actionOnDestroy: view => Object.Destroy(view.gameObject),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public BlockView Get() => pool.Get();

        public void Release(BlockView view) => pool.Release(view);
    }
}
