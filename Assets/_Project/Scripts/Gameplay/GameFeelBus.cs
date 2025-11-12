using System;
using UnityEngine;

namespace ShadowRunners.Feel
{
    /// Lightweight global events for game feel.
    public static class GameFeelBus
    {
        /// Raised when player jumps.
        public static event Action OnJump;

        /// Raised when player starts a slide.
        public static event Action OnSlide;

        /// Raised when a shield absorbs a hit.
        /// Arg: obstacle GameObject (the visual root we neutralized), may be null.
        public static event Action<GameObject> OnShieldAbsorb;

        public static void RaiseJump() => OnJump?.Invoke();
        public static void RaiseSlide() => OnSlide?.Invoke();
        public static void RaiseShieldAbsorb(GameObject obstacleRoot) => OnShieldAbsorb?.Invoke(obstacleRoot);
    }
}
