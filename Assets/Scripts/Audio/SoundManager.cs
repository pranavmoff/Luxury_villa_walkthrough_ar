using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource ambientSource;
    public AudioSource sfxSource;
    public AudioSource footstepSource;
    public AudioSource uiSource;

    private AudioClip ambientClip;
    private AudioClip footstepClip;
    private AudioClip jumpClip;
    private AudioClip landClip;
    private AudioClip inspectEnterClip;
    private AudioClip inspectExitClip;
    private AudioClip uiHoverClip;
    private AudioClip uiClickClip;
    private AudioClip dayNightClip;

    private float footstepTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupAudioSources();
        GenerateProceduralClips();
    }

    private void Start()
    {
        PlayAmbient();
    }

    private void SetupAudioSources()
    {
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.volume = 0.25f;
            ambientSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.7f;
        }

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 0f;
            footstepSource.volume = 0.4f;
        }

        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.spatialBlend = 0f;
            uiSource.volume = 0.5f;
        }
    }

    private void GenerateProceduralClips()
    {
        // 1. Ambient wind/city murmur clip (Looped, 3 seconds)
        int sampleRate = 44100;
        int ambientSamples = sampleRate * 3;
        float[] ambData = new float[ambientSamples];
        float prevVal = 0f;
        for (int i = 0; i < ambientSamples; i++)
        {
            float white = (Random.value * 2f - 1f) * 0.05f;
            prevVal = Mathf.Lerp(prevVal, white, 0.03f); // Low pass filter
            float subHum = Mathf.Sin(2f * Mathf.PI * 55f * (i / (float)sampleRate)) * 0.02f;
            ambData[i] = prevVal + subHum;
        }
        ambientClip = AudioClip.Create("ProceduralAmbient", ambientSamples, 1, sampleRate, false);
        ambientClip.SetData(ambData, 0);

        // 2. Footstep clip (Soft organic tap)
        int stepSamples = (int)(sampleRate * 0.08f);
        float[] stepData = new float[stepSamples];
        for (int i = 0; i < stepSamples; i++)
        {
            float t = i / (float)stepSamples;
            float env = Mathf.Exp(-t * 30f);
            float noise = (Random.value * 2f - 1f) * 0.4f;
            float thud = Mathf.Sin(2f * Mathf.PI * (80f - 40f * t) * (i / (float)sampleRate));
            stepData[i] = (thud * 0.6f + noise * 0.4f) * env;
        }
        footstepClip = AudioClip.Create("ProceduralStep", stepSamples, 1, sampleRate, false);
        footstepClip.SetData(stepData, 0);

        // 3. Jump clip
        int jumpSamples = (int)(sampleRate * 0.15f);
        float[] jumpData = new float[jumpSamples];
        for (int i = 0; i < jumpSamples; i++)
        {
            float t = i / (float)jumpSamples;
            float env = 1f - t;
            float freq = Mathf.Lerp(120f, 260f, t);
            jumpData[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * env * 0.5f;
        }
        jumpClip = AudioClip.Create("ProceduralJump", jumpSamples, 1, sampleRate, false);
        jumpClip.SetData(jumpData, 0);

        // 4. Land clip
        int landSamples = (int)(sampleRate * 0.12f);
        float[] landData = new float[landSamples];
        for (int i = 0; i < landSamples; i++)
        {
            float t = i / (float)landSamples;
            float env = Mathf.Exp(-t * 25f);
            landData[i] = Mathf.Sin(2f * Mathf.PI * 60f * (i / (float)sampleRate)) * env * 0.7f;
        }
        landClip = AudioClip.Create("ProceduralLand", landSamples, 1, sampleRate, false);
        landClip.SetData(landData, 0);

        // 5. Inspect Enter (Warm pleasant chime)
        int enterSamples = (int)(sampleRate * 0.6f);
        float[] enterData = new float[enterSamples];
        for (int i = 0; i < enterSamples; i++)
        {
            float t = i / (float)enterSamples;
            float env = Mathf.Exp(-t * 5f);
            float c1 = Mathf.Sin(2f * Mathf.PI * 523.25f * (i / (float)sampleRate)); // C5
            float e1 = Mathf.Sin(2f * Mathf.PI * 659.25f * (i / (float)sampleRate)); // E5
            float g1 = Mathf.Sin(2f * Mathf.PI * 783.99f * (i / (float)sampleRate)); // G5
            float b1 = Mathf.Sin(2f * Mathf.PI * 1046.50f * (i / (float)sampleRate)); // C6
            enterData[i] = (c1 * 0.3f + e1 * 0.3f + g1 * 0.25f + b1 * 0.15f) * env * 0.5f;
        }
        inspectEnterClip = AudioClip.Create("ProceduralInspectEnter", enterSamples, 1, sampleRate, false);
        inspectEnterClip.SetData(enterData, 0);

        // 6. Inspect Exit (Soft swoosh out)
        int exitSamples = (int)(sampleRate * 0.4f);
        float[] exitData = new float[exitSamples];
        for (int i = 0; i < exitSamples; i++)
        {
            float t = i / (float)exitSamples;
            float env = Mathf.Sin(t * Mathf.PI);
            float freq = Mathf.Lerp(440f, 220f, t);
            exitData[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * env * 0.35f;
        }
        inspectExitClip = AudioClip.Create("ProceduralInspectExit", exitSamples, 1, sampleRate, false);
        inspectExitClip.SetData(exitData, 0);

        // 7. UI Hover
        int hoverSamples = (int)(sampleRate * 0.04f);
        float[] hoverData = new float[hoverSamples];
        for (int i = 0; i < hoverSamples; i++)
        {
            float t = i / (float)hoverSamples;
            hoverData[i] = Mathf.Sin(2f * Mathf.PI * 1200f * (i / (float)sampleRate)) * (1f - t) * 0.15f;
        }
        uiHoverClip = AudioClip.Create("ProceduralUIHover", hoverSamples, 1, sampleRate, false);
        uiHoverClip.SetData(hoverData, 0);

        // 8. UI Click
        int clickSamples = (int)(sampleRate * 0.06f);
        float[] clickData = new float[clickSamples];
        for (int i = 0; i < clickSamples; i++)
        {
            float t = i / (float)clickSamples;
            clickData[i] = Mathf.Sin(2f * Mathf.PI * 880f * (i / (float)sampleRate)) * Mathf.Exp(-t * 40f) * 0.35f;
        }
        uiClickClip = AudioClip.Create("ProceduralUIClick", clickSamples, 1, sampleRate, false);
        uiClickClip.SetData(clickData, 0);

        // 9. Day/Night switch
        int dnSamples = (int)(sampleRate * 0.5f);
        float[] dnData = new float[dnSamples];
        for (int i = 0; i < dnSamples; i++)
        {
            float t = i / (float)dnSamples;
            float env = Mathf.Exp(-t * 6f);
            float f = Mathf.Lerp(300f, 600f, t);
            dnData[i] = Mathf.Sin(2f * Mathf.PI * f * (i / (float)sampleRate)) * env * 0.35f;
        }
        dayNightClip = AudioClip.Create("ProceduralDayNight", dnSamples, 1, sampleRate, false);
        dayNightClip.SetData(dnData, 0);
    }

    public void PlayAmbient()
    {
        if (ambientSource != null && ambientClip != null)
        {
            ambientSource.clip = ambientClip;
            ambientSource.Play();
        }
    }

    public void PlayFootstep(bool isSprinting)
    {
        footstepTimer -= Time.deltaTime;
        float interval = isSprinting ? 0.32f : 0.48f;
        if (footstepTimer <= 0f)
        {
            footstepTimer = interval;
            if (footstepSource != null && footstepClip != null)
            {
                footstepSource.pitch = Random.Range(0.9f, 1.15f);
                footstepSource.PlayOneShot(footstepClip, isSprinting ? 0.45f : 0.3f);
            }
        }
    }

    public void PlayJump()
    {
        if (sfxSource != null && jumpClip != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(jumpClip, 0.5f);
        }
    }

    public void PlayLand()
    {
        if (sfxSource != null && landClip != null)
        {
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(landClip, 0.6f);
        }
    }

    public void PlayInspectEnter()
    {
        if (sfxSource != null && inspectEnterClip != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(inspectEnterClip, 0.6f);
        }
    }

    public void PlayInspectExit()
    {
        if (sfxSource != null && inspectExitClip != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(inspectExitClip, 0.5f);
        }
    }

    public void PlayUIHover()
    {
        if (uiSource != null && uiHoverClip != null)
        {
            uiSource.pitch = Random.Range(0.98f, 1.02f);
            uiSource.PlayOneShot(uiHoverClip, 0.2f);
        }
    }

    public void PlayUIClick()
    {
        if (uiSource != null && uiClickClip != null)
        {
            uiSource.pitch = 1f;
            uiSource.PlayOneShot(uiClickClip, 0.4f);
        }
    }

    public void PlayDayNightSwitch()
    {
        if (sfxSource != null && dayNightClip != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(dayNightClip, 0.4f);
        }
    }
}
