using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    [Header("Spawners a controlar")]
    public List<EnemySpawner> spawners = new List<EnemySpawner>();

    [Header("Rondas")]
    public int startCount = 7;            // enemigos en la ronda 1
    public int addPerWave = 20;            // 20% por ronda
    public float betweenWavesDelay = 3f;  // pausa entre rondas

    [Header("Velocidad de enemigos")]
    public float speedIncreasePercent = 5f;   // +5% por ronda
    public bool scaleFromPrefabSpeed = true;  // multiplicar la velocidad que traen

    [Header("HUD")]
    public TextMeshProUGUI waveText;         // arrastrá el texto de ronda acá
    public TextMeshProUGUI killsText;        // arrastrá el texto de kills acá


    int _waveIndex = 0;              // 1 = primera ronda (lo usamos 1-based)
    int _targetThisWave;             // cuántos deben spawnearse en total en la ronda
    int _spawnedThisWave;
    int _aliveThisWave;

    int _totalKills;                 // kills acumuladas en toda la partida

    float CurrentSpeedMultiplier => Mathf.Pow(1f + speedIncreasePercent / 100f, _waveIndex - 1);

    void OnEnable()
    {
        foreach (var sp in spawners)
        {
            if (!sp) continue;
            sp.OnSpawned += HandleSpawned;   // escuchar nacimientos
            sp.StopSpawner();                // el manager controla cuándo arrancan
        }

        StartCoroutine(RunWaves());
    }

    void OnDisable()
    {
        foreach (var sp in spawners)
        {
            if (!sp) continue;
            sp.OnSpawned -= HandleSpawned;
            sp.StopSpawner();
        }
    }

    IEnumerator RunWaves()
    {
        while (true)
        {
            _waveIndex++;
            _spawnedThisWave = 0;
            _aliveThisWave = 0;

            if (_waveIndex == 1)
            {
                _targetThisWave = startCount;
            }
            else
            {
                float multiplier = 1f + (addPerWave / 100f);
                _targetThisWave = Mathf.RoundToInt(_targetThisWave * multiplier);
            }

            UpdateHUD();

            foreach (var sp in spawners)
                sp?.StartSpawner();

            while (_spawnedThisWave < _targetThisWave)
                yield return null;

            foreach (var sp in spawners)
                sp?.StopSpawner();

            while (_aliveThisWave > 0)
                yield return null;

            yield return new WaitForSeconds(betweenWavesDelay);
        }
    }

    void HandleSpawned(GameObject enemyGO)
    {
        // Si por alguna razón ya llegamos al objetivo, ignoramos extras
        if (_spawnedThisWave >= _targetThisWave)
            return;

        _spawnedThisWave++;
        _aliveThisWave++;

        // escalar velocidad del chaser según la ronda
        var chaser = enemyGO.GetComponent<EnemyChaser>() ?? enemyGO.GetComponentInChildren<EnemyChaser>();
        if (chaser)
        {
            float baseSpeed = chaser.moveSpeed; // valor que trae desde el prefab
            if (scaleFromPrefabSpeed)
                chaser.SetMoveSpeed(baseSpeed * CurrentSpeedMultiplier);
            else
                chaser.SetMoveSpeed(CurrentSpeedMultiplier);
        }

        // enganchar relay de muerte
        var relay = enemyGO.GetComponent<EnemyDeathRelay>();
        if (!relay) relay = enemyGO.AddComponent<EnemyDeathRelay>();

        relay.OnDied += () =>
        {
            _aliveThisWave = Mathf.Max(0, _aliveThisWave - 1);
            _totalKills++;
            UpdateHUD();
        };

        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (waveText)
        {
            waveText.text = $"Ronda: {_waveIndex}";
        }

        if (killsText)
        {
            killsText.text = $"Kills: {_totalKills}";
        }
    }
}
