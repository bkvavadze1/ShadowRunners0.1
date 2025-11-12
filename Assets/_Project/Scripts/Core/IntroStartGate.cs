// Assets/_Project/Scripts/UI/IntroStartGate.cs
using UnityEngine;
using UnityEngine.EventSystems;
using ShadowRunners.Gameplay; // RunnerMotor

namespace ShadowRunners.Systems
{
    /// <summary>
    /// Freezes gameplay until intro is dismissed.
    /// Robust across first run and re-runs (after crash/restart).
    /// - Captures speed pre-freeze (when valid).
    /// - Remembers a sane start speed for subsequent runs.
    /// - Snaps runner to target speed on begin (no slow ramp).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class IntroStartGate : MonoBehaviour, IPointerClickHandler
    {
        [Header("Refs")]
        [Tooltip("Panel shown at start (active = frozen).")]
        public GameObject introPanel;
        [Tooltip("Optional CanvasGroup on the intro panel (auto-found if null).")]
        public CanvasGroup introCanvasGroup;
        [Tooltip("Runner motor to disable until start.")]
        public RunnerMotor runner;
        [Tooltip("Optional: TrackManager to disable until start.")]
        public MonoBehaviour trackManager;
        [Tooltip("Optional: swipe or keyboard input script to disable until start.")]
        public MonoBehaviour inputComponent;

        [Header("Blockers (overlays that must be closed first)")]
        [Tooltip("If any of these are active, the gate stays armed (e.g., PowerupsInfo poster).")]
        public GameObject[] additionalBlockers;

        [Header("Behavior")]
        [Tooltip("Also pause time/audio while intro is up.")]
        public bool freezeTimeAndAudio = true;

        [Tooltip("Preferred start speed. If > 0, ALWAYS use this on begin (best fix for slow re-runs).")]
        public float preferredStartSpeed = 8f;

        [Tooltip("Fallback start speed if nothing else is valid (> 0 to use, else 0 = use built-in fallback 6).")]
        public float fallbackStartSpeed = 0f;

        [Tooltip("Optional fade time for intro panel alpha on dismiss.")]
        public float fadeSeconds = 0.12f;

        // --- internals ---
        bool _armed;
        bool _waiting;

        float _prevTimeScale = 1f;
        float _prevFixedDeltaTime = 0.02f; // default

        float _capturedSpeed;
        bool _capturedSpeedValid;

        Animator _runnerAnimator;

        // Persist across scene reloads to keep a good speed for subsequent runs
        static float sLastKnownGoodSpeed = 0f;

        const float EPS = 0.05f;

        void Awake()
        {
            if (!runner) runner = FindObjectOfType<RunnerMotor>(true);
            if (runner && !_runnerAnimator) _runnerAnimator = runner.GetComponentInChildren<Animator>(true);

            if (!introPanel)
            {
                var go = GameObject.Find("IntroPanel") ?? GameObject.Find("IntroPanelController");
                if (go) introPanel = go;
            }

            if (introPanel && !introCanvasGroup)
                introCanvasGroup = introPanel.GetComponent<CanvasGroup>();

            if (!FindObjectOfType<EventSystem>())
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        void OnEnable() => TryArmFromPanelState();
        void Start() => TryArmFromPanelState();

        void TryArmFromPanelState()
        {
            if (!introPanel)
            {
                enabled = false;
                return;
            }

            if (introPanel.activeInHierarchy)
            {
                if (introCanvasGroup)
                {
                    introCanvasGroup.alpha = 1f;
                    introCanvasGroup.interactable = true;
                    introCanvasGroup.blocksRaycasts = true;
                }
                introPanel.transform.SetAsLastSibling();
                ArmFreeze();
            }
            else
            {
                enabled = false;
            }
        }

        void ArmFreeze()
        {
            _armed = true;
            _waiting = true;

            if (runner)
            {
                // Capture a meaningful pre-freeze speed ONLY if it's already running.
                if (runner.forwardSpeed > EPS)
                {
                    _capturedSpeed = runner.forwardSpeed;
                    _capturedSpeedValid = true;
                    // keep a global remembered value for later re-runs
                    sLastKnownGoodSpeed = _capturedSpeed;
                }

                runner.forwardSpeed = 0f;
                runner.enabled = false;
            }

            if (trackManager) trackManager.enabled = false;
            if (inputComponent) inputComponent.enabled = false;

            if (freezeTimeAndAudio)
            {
                _prevTimeScale = Time.timeScale;
                _prevFixedDeltaTime = Time.fixedDeltaTime;
                Time.timeScale = 0f;
                Time.fixedDeltaTime = 0.02f * Time.timeScale; // 0 while paused
                AudioListener.pause = true;
                if (_runnerAnimator) _runnerAnimator.speed = 0f;
            }
        }

        void Update()
        {
            if (!_armed || !_waiting) return;

            if (AnyBlockerActive()) return;

            if (Input.anyKeyDown || Input.touchCount > 0)
                BeginGame();
        }

        bool AnyBlockerActive()
        {
            if (additionalBlockers != null)
            {
                for (int i = 0; i < additionalBlockers.Length; i++)
                {
                    var go = additionalBlockers[i];
                    if (go && go.activeInHierarchy) return true;
                }
            }
            return false;
        }

        public void BeginGame()
        {
            if (!_armed) return;
            if (AnyBlockerActive()) return;

            if (introPanel)
            {
                if (introCanvasGroup && fadeSeconds > 0f && gameObject.activeInHierarchy)
                    StartCoroutine(FadeOutThenDisable());
                else
                    introPanel.SetActive(false);
            }

            if (freezeTimeAndAudio)
            {
                Time.timeScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;
                Time.fixedDeltaTime = (_prevFixedDeltaTime > 0f) ? _prevFixedDeltaTime : 0.02f;
                AudioListener.pause = false;
                if (_runnerAnimator) _runnerAnimator.speed = 1f;
            }

            if (runner)
            {
                runner.enabled = true;

                // Determine the best start speed in priority order:
                float targetSpeed =
                    (preferredStartSpeed > EPS) ? preferredStartSpeed :
                    (sLastKnownGoodSpeed > EPS) ? sLastKnownGoodSpeed :
                    (_capturedSpeedValid && _capturedSpeed > EPS) ? _capturedSpeed :
                    (fallbackStartSpeed > EPS) ? fallbackStartSpeed :
                    6f; // ultimate fallback

                runner.forwardSpeed = targetSpeed;
                sLastKnownGoodSpeed = targetSpeed; // remember for next runs

                // If your RunnerMotor has acceleration state, reset it here (pseudo):
                // runner.targetSpeed = targetSpeed;
                // runner.ResetAccelerationTimer();
            }

            if (trackManager) trackManager.enabled = true;
            if (inputComponent) inputComponent.enabled = true;

            _waiting = false;
            _armed = false;
            enabled = false;
        }

        System.Collections.IEnumerator FadeOutThenDisable()
        {
            float t = 0f;
            float start = introCanvasGroup.alpha;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                introCanvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeSeconds);
                yield return null;
            }
            introCanvasGroup.alpha = 0f;
            if (introPanel) introPanel.SetActive(false);
        }

        public void OnPointerClick(PointerEventData _) => BeginGame();

        void OnDestroy()
        {
            if (_armed)
            {
                if (freezeTimeAndAudio)
                {
                    Time.timeScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;
                    Time.fixedDeltaTime = (_prevFixedDeltaTime > 0f) ? _prevFixedDeltaTime : 0.02f;
                    AudioListener.pause = false;
                    if (_runnerAnimator) _runnerAnimator.speed = 1f;
                }
                if (runner) runner.enabled = true;
                if (trackManager) trackManager.enabled = true;
                if (inputComponent) inputComponent.enabled = true;
            }
        }
    }
}
