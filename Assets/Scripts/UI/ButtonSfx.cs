using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Stealth.Audio;

namespace Stealth.UI
{
    /// <summary>Adds hover and click sounds to a button without touching the button code.</summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSfx : MonoBehaviour, IPointerEnterHandler
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => AudioManager.PlaySfx2D(SoundId.UiClick));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AudioManager.PlaySfx2D(SoundId.UiHover, 0.7f);
        }
    }
}
