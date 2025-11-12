using UnityEngine;

namespace ShadowRunners.Intro
{
    /// Attach this to your Main Menu. Wire your Play button's OnClick to CallPlay().
    public class MainMenuPlayHook : MonoBehaviour
    {
        public FirstRunVideoGate gate;

        public void CallPlay()
        {
            if (!gate) { Debug.LogWarning("[MainMenuPlayHook] Gate missing"); return; }
            gate.BeginOrBypass();
        }
    }
}
