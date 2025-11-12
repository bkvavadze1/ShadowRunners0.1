using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace ShadowRunners.Intro
{
    [AddComponentMenu("ShadowRunners/Intro/First Run Video Gate")]
    public class FirstRunVideoGate : MonoBehaviour
    {
        public enum NextAction { None, InvokeUnityEvent, LoadSceneByName, PlayInEngineIntro }

        [Header("Gate")]
        public bool firstRunOnly = true;
        public string playerPrefsKey = "SR_IntroVideoSeen";

        [Header("Video")]
        public VideoClip videoClip;                           // assign your MP4 here
        public string streamingAssetsFileName = "Shadow Runners Intro.mp4";
        public VideoPlayer videoPlayer;                       // assign the VideoPlayer component
        public RawImage videoSurface;                         // full-screen RawImage
        public CanvasGroup fadeGroup;                         // CanvasGroup on the same RawImage
        public float fadeSeconds = 0.2f;

        [Header("Audio")]
        public AudioSource audioSource;                       // optional

        [Header("Skip")]
        public bool skippable = true;
        [Tooltip("Delay before skip inputs are accepted (prevents the Play click from skipping).")]
        public float skipArmDelay = 0.35f;

        [Header("After Video")]
        public NextAction onFinish = NextAction.LoadSceneByName;
        public string sceneName = "Runner_Greybox";
        public GameObject inEngineIntroGO;
        public UnityEvent onIntroFinished;

        // internals
        RenderTexture _rt;
        bool _playing, _wasPrepared, _skipRequested, _skipArmed;
        float _armAtTime;

        void Reset()
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        void Awake()
        {
            if (!videoPlayer) videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.isLooping = false;
            videoPlayer.aspectRatio = VideoAspectRatio.FitVertically;

            if (audioSource)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.EnableAudioTrack(0, true);
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }
            else
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            }

            HideSurface(true);
        }

        public void BeginOrBypass()
        {
            int seen = PlayerPrefs.GetInt(playerPrefsKey, 0);
            if (firstRunOnly && seen == 1)
            {
                DoNextAction();
                return;
            }
            PlayIntroVideo();
        }

        public void PlayIntroVideo()
        {
            if (_playing) return;
            _skipRequested = false;
            _skipArmed = false;

            // source
            if (videoClip)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClip;
            }
            else
            {
                string url = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsFileName);
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = url;
            }

            // target (RawImage via RenderTexture)
            if (videoSurface)
            {
                if (_rt != null) { if (_rt.IsCreated()) _rt.Release(); Destroy(_rt); _rt = null; }
                _rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                _rt.Create();
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = _rt;
                videoSurface.texture = _rt;
            }
            else
            {
                videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
                videoPlayer.targetCameraAlpha = 1f;
            }

            // subs
            videoPlayer.prepareCompleted += HandlePrepared;
            videoPlayer.loopPointReached += HandleFinished;
            videoPlayer.errorReceived += HandleError;

            ShowSurface();
            videoPlayer.Prepare();

            _wasPrepared = false;
            _playing = true;

            // arm skip a bit later so the Play click doesn't skip
            _armAtTime = Time.unscaledTime + Mathf.Max(0.05f, skipArmDelay);
        }

        void Update()
        {
            if (!_playing || !skippable) return;

            if (!_skipArmed && Time.unscaledTime >= _armAtTime)
                _skipArmed = true;

            if (_skipArmed && !_skipRequested)
            {
                bool pressed = Input.anyKeyDown || Input.GetMouseButtonDown(0) ||
                               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
                if (pressed)
                {
                    _skipRequested = true;
                    StopVideoImmediate();
                    HandleFinished(videoPlayer);
                }
            }
        }

        void HandlePrepared(VideoPlayer vp)
        {
            _wasPrepared = true;
            vp.Play();
            if (audioSource) audioSource.Play();
        }

        void HandleFinished(VideoPlayer vp)
        {
            if (firstRunOnly)
            {
                PlayerPrefs.SetInt(playerPrefsKey, 1);
                PlayerPrefs.Save();
            }

            vp.prepareCompleted -= HandlePrepared;
            vp.loopPointReached -= HandleFinished;
            vp.errorReceived -= HandleError;

            HideSurface();
            _playing = false;

            DoNextAction();
        }

        void HandleError(VideoPlayer vp, string msg)
        {
            Debug.LogWarning("[FirstRunVideoGate] Video error: " + msg);
            HandleFinished(vp); // fail open
        }

        void StopVideoImmediate()
        {
            if (!_playing) return;
            try
            {
                if (videoPlayer.isPlaying) videoPlayer.Stop();
                if (audioSource && audioSource.isPlaying) audioSource.Stop();
            }
            catch { }
        }

        void ShowSurface()
        {
            if (fadeGroup)
            {
                StopAllCoroutines();
                StartCoroutine(FadeCanvas(fadeGroup, 1f, fadeSeconds));
            }
            if (videoSurface) videoSurface.enabled = true;
            // block clicks from reaching menu while visible
            if (fadeGroup) { fadeGroup.blocksRaycasts = true; fadeGroup.interactable = true; }
        }

        void HideSurface(bool instant = false)
        {
            if (fadeGroup)
            {
                if (instant)
                {
                    fadeGroup.alpha = 0f;
                    fadeGroup.blocksRaycasts = false;
                    fadeGroup.interactable = false;
                }
                else
                {
                    StopAllCoroutines();
                    StartCoroutine(FadeCanvas(fadeGroup, 0f, fadeSeconds));
                }
            }
            if (videoSurface) videoSurface.enabled = false;

            if (_rt) { if (_rt.IsCreated()) _rt.Release(); Destroy(_rt); _rt = null; }
        }

        System.Collections.IEnumerator FadeCanvas(CanvasGroup cg, float target, float secs)
        {
            float a0 = cg.alpha; float t = 0f; secs = Mathf.Max(0.01f, secs);
            while (t < secs)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(a0, target, t / secs);
                yield return null;
            }
            cg.alpha = target;
            cg.blocksRaycasts = target > 0.5f;
            cg.interactable = target > 0.5f;
        }

        void DoNextAction()
        {
            switch (onFinish)
            {
                case NextAction.None:
                    break;

                case NextAction.InvokeUnityEvent:
                    onIntroFinished?.Invoke();
                    break;

                case NextAction.LoadSceneByName:
                    if (!string.IsNullOrEmpty(sceneName))
                        SceneManager.LoadScene(sceneName);
                    break;

                case NextAction.PlayInEngineIntro:
                    if (inEngineIntroGO) inEngineIntroGO.SetActive(true);
                    onIntroFinished?.Invoke();
                    break;
            }
        }
    }
}
