using UnityEngine;

namespace Stealth.Core
{
    /// <summary>Single place that decides whether the mouse is captured by the camera or free for menus.</summary>
    public static class CursorService
    {
        public static void Capture()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void Release()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
