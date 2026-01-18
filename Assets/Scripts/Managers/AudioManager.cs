using UnityEngine;

namespace TapBlitz.Managers
{
    /// <summary>
    /// Centralised audio manager for TapBlitz playable ad.
    /// All clips assigned in Inspector. Uses PlayOneShot for overlapping SFX.
    /// Audio is unlocked on first user gesture (WebGL requirement).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX — Gameplay")]
        [SerializeField] private AudioClip tapHitClip;
        [SerializeField] private AudioClip tapMissClip;
        [SerializeField] private AudioClip targetExpireClip;
        [SerializeField] private AudioClip comboTierUpClip;
        [SerializeField] private AudioClip comboBreakClip;
        [SerializeField] private AudioClip bombTapClip;

        [Header("SFX — UI")]
        [SerializeField] private AudioClip countdownTickClip;
        [SerializeField] private AudioClip countdownGoClip;
        [SerializeField] private AudioClip ctaJingleClip;
        [SerializeField] private AudioClip starPopClip;
        [SerializeField] private AudioClip uiClickClip;

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip   bgMusicClip;
        [SerializeField] [Range(0f,1f)] private float musicVolume = 0.35f;

        [Header("Volume")]
        [SerializeField] [Range(0f,1f)] private float sfxVolume = 0.9f;

        private AudioSource sfxSource;
        private bool audioUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance  = this;
            sfxSource = GetComponent<AudioSource>();
            sfxSource.playOnAwake = false;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL — mute until user gesture
            AudioListener.volume = 0f;
#else
            audioUnlocked = true;
#endif
        }

        private void Start()
        {
            if (musicSource && bgMusicClip)
            {
                musicSource.clip   = bgMusicClip;
                musicSource.loop   = true;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!audioUnlocked && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
            {
                audioUnlocked        = true;
                AudioListener.volume = 1f;
            }
#endif
        }

        // ── Gameplay SFX ──────────────────────────────────────────────────────

        public void PlayTapHit()       => PlaySfx(tapHitClip,       Random.Range(0.9f, 1.1f));
        public void PlayTapMiss()      => PlaySfx(tapMissClip,       1f);
        public void PlayTargetExpire() => PlaySfx(targetExpireClip,  Random.Range(0.85f, 1.05f));
        public void PlayComboTierUp()  => PlaySfx(comboTierUpClip,   1f);
        public void PlayComboBreak()   => PlaySfx(comboBreakClip,    1f);
        public void PlayBombTap()      => PlaySfx(bombTapClip,       1f);

        // ── UI SFX ────────────────────────────────────────────────────────────

        public void PlayCountdownTick() => PlaySfx(countdownTickClip, 1f);
        public void PlayCountdownGo()   => PlaySfx(countdownGoClip,   1f);
        public void PlayCTAJingle()     => PlaySfx(ctaJingleClip,     1f);
        public void PlayStarPop()       => PlaySfx(starPopClip,       Random.Range(0.95f, 1.1f));
        public void PlayUIClick()       => PlaySfx(uiClickClip,       1f);

        // ── Volume control (called by Luna mute event) ─────────────────────────

        public void SetMute(bool muted)
        {
            AudioListener.volume = muted ? 0f : 1f;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void PlaySfx(AudioClip clip, float pitch = 1f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
