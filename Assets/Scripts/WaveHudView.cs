using PvZ2.Foundation;
using UnityEngine;
using UnityEngine.UI;

public sealed class WaveHudView : MonoBehaviour
{
    [SerializeField] WaveManager manager;
    [SerializeField] Text waveText;
    [SerializeField] Text enemyText;
    [SerializeField] GameObject finalWaveBanner;
    [SerializeField] GameObject victoryPanel;
    [SerializeField] GameObject defeatPanel;
    
    [SerializeField] Button defeatRestartButton;
[SerializeField] Button restartButton;
    float bannerTimer;

    public void Configure(WaveManager source, Text waveLabel, Text enemyLabel, GameObject finalBanner,
        GameObject victory, GameObject defeat, Button restart, Button defeatRestart)
    {
        manager = source; waveText = waveLabel; enemyText = enemyLabel; finalWaveBanner = finalBanner;
        victoryPanel = victory; defeatPanel = defeat; restartButton = restart; defeatRestartButton = defeatRestart;
    }

    void Start()
    {
        if (manager == null) return;
        manager.OnWaveChanged += HandleWave;
        manager.OnAliveEnemiesChanged += HandleAlive;
        manager.OnStateChanged += HandleState;
        manager.OnFinalWaveStarted += HandleFinalWave;
        
        defeatRestartButton?.onClick.AddListener(manager.RestartLevel);
restartButton?.onClick.AddListener(manager.RestartLevel);
        HandleWave(manager.CurrentWaveNumber, manager.TotalWaves);
        HandleAlive(manager.AliveEnemies);
        HandleState(manager.State);
        if (finalWaveBanner != null) finalWaveBanner.SetActive(false);
    }

    void Update()
    {
        if (bannerTimer <= 0f || finalWaveBanner == null) return;
        bannerTimer -= Time.deltaTime;
        if (bannerTimer <= 0f) finalWaveBanner.SetActive(false);
    }

    void HandleWave(int current, int total)
    {
        if (waveText != null) waveText.text = current > 0 ? $"WAVE {current}/{total}" : "GET READY";
    }

    void HandleAlive(int value)
    {
        if (enemyText != null) enemyText.text = $"ZOMBIES {value}";
    }

    void HandleState(GameState state)
    {
        if (victoryPanel != null) victoryPanel.SetActive(state == GameState.Victory);
        if (defeatPanel != null) defeatPanel.SetActive(state == GameState.Defeat);
    }

    void HandleFinalWave()
    {
        if (finalWaveBanner == null) return;
        finalWaveBanner.SetActive(true);
        bannerTimer = 2.5f;
    }

    void OnDestroy()
    {
        if (manager == null) return;
        manager.OnWaveChanged -= HandleWave;
        manager.OnAliveEnemiesChanged -= HandleAlive;
        manager.OnStateChanged -= HandleState;
        manager.OnFinalWaveStarted -= HandleFinalWave;
        
        defeatRestartButton?.onClick.RemoveListener(manager.RestartLevel);
restartButton?.onClick.RemoveListener(manager.RestartLevel);
    }
}