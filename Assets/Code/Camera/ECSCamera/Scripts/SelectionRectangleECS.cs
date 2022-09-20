#if HYBRID_ENTITIES_CAMERA_CONVERSION
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

using static RTTCamera.UGuiUtils;

namespace RTTCamera
{
    public class SelectionRectangleECS : MonoBehaviour, Controls.ISelectionRectangleActions
    {
        private Controls controls;
        
        private EntityManager entityManager;
        private Entity cameraEntity;

        private bool ClickDragPerformed;
        private Vector2 StartLMouse;
        private Vector2 EndLMouse;
        
        private void Start()
        {
            controls = entityManager.GetComponentObject<Object_InputsControl>(cameraEntity).Value;
            controls.SelectionRectangle.SetCallbacks(this);
        }
        
        private void OnGUI()
        {
            if (!ClickDragPerformed) return;
            // Create a rect from both mouse positions
            Rect rect = GetScreenRect(StartLMouse, EndLMouse);
            DrawScreenRect(rect);
            DrawScreenRectBorder(rect, 1);
        }
        
        public void Initialize(EntityManager em, Entity ce)
        {
            entityManager = em;
            cameraEntity = ce;
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
#endif