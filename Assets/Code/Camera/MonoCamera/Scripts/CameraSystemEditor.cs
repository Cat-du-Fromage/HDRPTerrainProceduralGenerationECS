using UnityEngine;

namespace RTTCamera
{
#if UNITY_EDITOR
    public partial class CameraSystem : MonoBehaviour
    {
        private void OnValidate()
        {
            AddOrRemoveSelectionRectangleComponent();
        }

        private void AddOrRemoveSelectionRectangleComponent()
        {
            bool hasComponent = TryGetComponent(out SelectionRectangle comp);
            if (!DrawRectangle && hasComponent)
            {
                if (Application.isPlaying)
                {
                    Destroy(comp);
                    return;
                }
                UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(comp);
            }
        }
    }
#endif
}
