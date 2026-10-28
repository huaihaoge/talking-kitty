using UnityEngine;

namespace TalkingKitty.ChildLock
{
    /// <summary>
    /// Simple PIN child lock. Stores a salted PIN hash and a locked flag in
    /// PlayerPrefs. While locked, the Back key (Escape on Android) must NOT
    /// quit the app — callers must ask <see cref="ShouldAllowQuit"/> first.
    /// </summary>
    public class ChildLockController : MonoBehaviour
    {
        public const string PinHashKey = "tk_pin_hash";
        public const string LockedKey = "tk_locked";

        /// <summary>Whether the app is currently locked.</summary>
        public bool IsLocked => PlayerPrefs.GetInt(LockedKey, 0) == 1;

        /// <summary>Whether a PIN has been set.</summary>
        public bool HasPin => PlayerPrefs.HasKey(PinHashKey);

        private void Update()
        {
            // When locked, swallow the Back key (Escape on Android) so the
            // OS "back to quit" behavior is blocked. We never call
            // Application.Quit from Back while locked.
            if (IsLocked && Input.GetKeyDown(KeyCode.Escape))
            {
                // Consumed: locked — Back does not quit.
            }
        }

        public void SetLocked(bool locked)
        {
            PlayerPrefs.SetInt(LockedKey, locked ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetPin(string pin)
        {
            PlayerPrefs.SetString(PinHashKey, PinHasher.Hash(pin));
            PlayerPrefs.Save();
        }

        public bool VerifyPin(string pin)
        {
            if (!HasPin)
                return false;

            return PlayerPrefs.GetString(PinHashKey) == PinHasher.Hash(pin);
        }

        /// <summary>
        /// Bootstrap/quit path must consult this: false while locked, so
        /// pressing Back never quits the app.
        /// </summary>
        public bool ShouldAllowQuit()
        {
            return !IsLocked;
        }
    }
}