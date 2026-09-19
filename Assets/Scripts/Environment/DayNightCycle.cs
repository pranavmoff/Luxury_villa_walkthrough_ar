using UnityEngine;
using System.Collections.Generic;

public enum TimeOfDay
{
    Day,
    GoldenHour,
    Night
}

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Sun Directional Light")]
    public Light sunLight;

    [Header("Night & Exterior Lights")]
    public List<Light> nightLights = new List<Light>();
    public List<GameObject> windowEmissives = new List<GameObject>();

    [Header("Presets")]
    public Vector3 daySunRotation = new Vector3(50f, -30f, 0f);
    public Color daySunColor = new Color(1f, 0.96f, 0.88f);
    public float daySunIntensity = 1.3f;
    public Color dayAmbientColor = new Color(0.6f, 0.7f, 0.85f);

    public Vector3 sunsetSunRotation = new Vector3(15f, -70f, 0f);
    public Color sunsetSunColor = new Color(1f, 0.55f, 0.25f);
    public float sunsetSunIntensity = 1.0f;
    public Color sunsetAmbientColor = new Color(0.7f, 0.45f, 0.35f);

    public Vector3 nightSunRotation = new Vector3(-35f, 40f, 0f);
    public Color nightSunColor = new Color(0.2f, 0.35f, 0.65f);
    public float nightSunIntensity = 0.15f;
    public Color nightAmbientColor = new Color(0.08f, 0.1f, 0.18f);

    public TimeOfDay CurrentTime { get; private set; } = TimeOfDay.Day;

    private float transitionT = 1f;
    private Vector3 fromRot, toRot;
    private Color fromColor, toColor;
    private float fromIntensity, toIntensity;
    private Color fromAmb, toAmb;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetTimeOfDay(TimeOfDay.Day, true);
    }

    private void Update()
    {
        if (InputBridge.GetKeyDown(KeyCode.T))
        {
            CycleNextTimeOfDay();
        }

        if (transitionT < 1f)
        {
            transitionT += Time.deltaTime * 1.5f;
            float t = Mathf.SmoothStep(0f, 1f, transitionT);

            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(Vector3.Lerp(fromRot, toRot, t));
                sunLight.color = Color.Lerp(fromColor, toColor, t);
                sunLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
            }
            RenderSettings.ambientLight = Color.Lerp(fromAmb, toAmb, t);
        }
    }

    public void CycleNextTimeOfDay()
    {
        switch (CurrentTime)
        {
            case TimeOfDay.Day:
                SetTimeOfDay(TimeOfDay.GoldenHour);
                break;
            case TimeOfDay.GoldenHour:
                SetTimeOfDay(TimeOfDay.Night);
                break;
            case TimeOfDay.Night:
                SetTimeOfDay(TimeOfDay.Day);
                break;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDayNightSwitch();
        }
    }

    public void SetTimeOfDay(TimeOfDay tod, bool instant = false)
    {
        CurrentTime = tod;
        transitionT = instant ? 1f : 0f;

        fromRot = sunLight != null ? sunLight.transform.eulerAngles : daySunRotation;
        fromColor = sunLight != null ? sunLight.color : daySunColor;
        fromIntensity = sunLight != null ? sunLight.intensity : daySunIntensity;
        fromAmb = RenderSettings.ambientLight;

        bool isNight = (tod == TimeOfDay.Night);
        bool isSunset = (tod == TimeOfDay.GoldenHour);

        switch (tod)
        {
            case TimeOfDay.Day:
                toRot = daySunRotation;
                toColor = daySunColor;
                toIntensity = daySunIntensity;
                toAmb = dayAmbientColor;
                break;
            case TimeOfDay.GoldenHour:
                toRot = sunsetSunRotation;
                toColor = sunsetSunColor;
                toIntensity = sunsetSunIntensity;
                toAmb = sunsetAmbientColor;
                break;
            case TimeOfDay.Night:
                toRot = nightSunRotation;
                toColor = nightSunColor;
                toIntensity = nightSunIntensity;
                toAmb = nightAmbientColor;
                break;
        }

        if (instant && sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(toRot);
            sunLight.color = toColor;
            sunLight.intensity = toIntensity;
            RenderSettings.ambientLight = toAmb;
        }

        foreach (var l in nightLights)
        {
            if (l != null) l.enabled = isNight || isSunset;
        }

        foreach (var we in windowEmissives)
        {
            if (we != null) we.SetActive(isNight || isSunset);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTimeOfDayLabel(tod.ToString());
        }
    }
}
