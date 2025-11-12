using UnityEngine;

namespace ShadowRunners.UI
{
    /// <summary>
    /// Keeps GameOverPanel invisible (alpha 0) during play, then fades it in when GameManager enters GameOver.
    /// Uses unscaled time so it still fades while Time.timeScale = 0.
    /// Put this on your GameOverPanel (which you keep active in the scene).
    /// </summary>
    [AddComponentMenu("ShadowRunners/UI/Game Over Fade Controller")]
    [RequireComponent(typeof(CanvasGroup))]
    public class GameOverFadeController : MonoBehaviour
    {
        [Header("Fade")]
        [Tooltip("Seconds to fade from 0 → 1 (unscaled).")]
        public float fadeInDuration = 0.35f;
        [Tooltip("Optional delay before fade begins (unscaled).")]
        public float fadeInDelay = 0.0f;

        [Header("Optional Fade-Out (when restarting)")]
        [Tooltip("Fade back to 0 when leaving GameOver.")]
        public bool fadeOutOnExit = true;
        [Tooltip("Seconds to fade 1 → 0 (unscaled).")]
        public float fadeOutDuration = 0.2f;

        CanvasGroup _cg;
        enum State { Running, Paused, GameOver }
        State _last = State.Running;
        float _t;         // timer for current fade
        float _dur;       // current fade duration
        float _delay;     // start delay
        bool _fadingIn;
        bool _fadingOut;

        void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            // Keep panel invisible & non-blocking until game over
            _cg.alpha = 0f;
            _cg.blocksRaycasts = false;
            _cg.interactable = false;
        }

        void Update()
        {
            var gm = ShadowRunners.Gameplay.GameManager.Instance;
            var s = State.Running;
            if (gm != null)
            {
                switch (gm.State)
                {
                    case ShadowRunners.Gameplay.GameManager.GameState.GameOver: s = State.GameOver; break;
                    case ShadowRunners.Gameplay.GameManager.GameState.Paused: s = State.Paused; break;
                    default: s = State.Running; break;
                }
            }

            if (s != _last)
            {
                if (s == State.GameOver)
                {
                    BeginFadeIn();
                }
                else if (_last == State.GameOver && fadeOutOnExit)
                {
                    BeginFadeOut();
                }
                else
                {
                    // Ensure hidden during normal play/pause (no blocking)
                    if (!_fadingIn && !_fadingOut)
                    {
                        _cg.alpha = 0f;
                        _cg.blocksRaycasts = false;
                        _cg.interactable = false;
                    }
                }
                _last = s;
            }

            TickFade();
        }

        void BeginFadeIn()
        {
            _fadingIn = true; _fadingOut = false;
            _t = 0f; _dur = Mathf.Max(0.001f, fadeInDuration);
            _delay = Mathf.Max(0f, fadeInDelay);
            // allow clicks only after we’re mostly visible
            _cg.blocksRaycasts = false;
            _cg.interactable = false;
        }

        void BeginFadeOut()
        {
            _fadingIn = false; _fadingOut = true;
            _t = 0f; _dur = Mathf.Max(0.001f, fadeOutDuration);
            _delay = 0f;
            // immediately stop blocking
            _cg.blocksRaycasts = false;
            _cg.interactable = false;
        }

        void TickFade()
        {
            if (!_fadingIn && !_fadingOut) return;

            float dt = Time.unscaledDeltaTime;

            if (_delay > 0f)
            {
                _delay -= dt;
                return;
            }

            _t += dt;
            float u = Mathf.Clamp01(_t / _dur);

            if (_fadingIn)
            {
                // Smooth in (ease-out)
                float a = 1f - Mathf.Pow(1f - u, 3f);
                _cg.alpha = a;
                if (u >= 0.85f) { _cg.blocksRaycasts = true; _cg.interactable = true; }
                if (u >= 1f) _fadingIn = false;
            }
            else // fading out
            {
                // Smooth out (ease-in)
                float a = Mathf.Pow(1f - u, 3f);
                _cg.alpha = a;
                if (u >= 1f)
                {
                    _fadingOut = false;
                    _cg.alpha = 0f;
                    _cg.blocksRaycasts = false;
                    _cg.interactable = false;
                }
            }
        }
    }
}
