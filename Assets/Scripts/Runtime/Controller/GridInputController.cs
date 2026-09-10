using Match2.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Match2.Controller
{
    /// <summary>Translates a pointer press into a grid index and forwards it to <see cref="GameFlowController"/>.</summary>
    public class GridInputController : MonoBehaviour
    {
        [SerializeField] private GridView gridView;
        [SerializeField] private Camera worldCamera;

        private GameFlowController flowController;

        public void Initialize(GameFlowController controller)
        {
            flowController = controller;
        }

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private void Update()
        {
            if (flowController == null)
                return;

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return;

            Vector2 screenPosition = pointer.position.ReadValue();
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0f;

            if (gridView.TryGetIndexAtWorldPosition(worldPosition, out int index))
                flowController.TryBlastAt(index);
        }
    }
}
