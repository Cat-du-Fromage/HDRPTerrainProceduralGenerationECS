using UnityEngine;
using UnityEngine.InputSystem;

using static RTTCamera.UGuiUtils;

namespace RTTCamera
{
    [RequireComponent(typeof(CameraSystem))]
    public class SelectionRectangle : MonoBehaviour, Controls.ISelectionRectangleActions
    {
        private CameraSystem cameraSystem;
        
        [field:SerializeField] public bool ClickDragPerformed{ get; private set; }
        [field:SerializeField] public Vector2 StartLMouse{ get; private set; }
        [field:SerializeField] public Vector2 EndLMouse{ get; private set; }

        private void Awake()
        {
            cameraSystem = GetComponent<CameraSystem>();
        }

        private void Start()
        {
            cameraSystem.controls.SelectionRectangle.Enable();
            cameraSystem.controls.SelectionRectangle.SetCallbacks(this);
        }

        private void OnDestroy()
        {
            cameraSystem.controls.SelectionRectangle.Disable();
        }

        //==================================================================================================================
        //Rectangle OnScreen
        //==================================================================================================================
        private void OnGUI()
        {
            if (!ClickDragPerformed) return;
            // Create a rect from both mouse positions
            Rect rect = GetScreenRect(StartLMouse, EndLMouse);
            DrawScreenRect(rect);
            DrawScreenRectBorder(rect, 1);
        }
        
        private bool IsDragSelection() => Vector2.SqrMagnitude(EndLMouse - StartLMouse) >= 128;
        public void OnLeftMouseClickAndMove(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                StartLMouse = EndLMouse = context.ReadValue<Vector2>();
                ClickDragPerformed = false;
            }
            else if(context.performed)
            {
                EndLMouse = context.ReadValue<Vector2>();
                ClickDragPerformed = IsDragSelection();
            }
            else
            {
                ClickDragPerformed = false;
            }
        }
    }
}
