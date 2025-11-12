using UnityEngine;
using UnityEngine.Video;

/// Plays the VideoPlayer whenever this GameObject is enabled (e.g., when GameOverPanel shows),
/// and pauses it when disabled. Perfect if your GameOverPanel is already shown/hidden by your HUD.
[AddComponentMenu("ShadowRunners/UI/Video Auto Play On Enable")]
public class VideoAutoPlayOnEnable : MonoBehaviour
{
    public VideoPlayer videoPlayer;         // auto-finds on this GO if null

    void Awake()
    {
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer)
        {
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
        }
    }

    void OnEnable()
    {
        if (videoPlayer) videoPlayer.Play();
    }

    void OnDisable()
    {
        if (videoPlayer && videoPlayer.isPlaying) videoPlayer.Pause();
    }
}
