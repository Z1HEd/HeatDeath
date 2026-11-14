#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// Testing shortcuts, only in the editor and development builds. Installs itself, so the scene doesn't change.
public class DevShortcuts : MonoBehaviour
{
    private const int BigLevelStep = 5;
    // Jump to the start of sector 1, 2, ... Past the sector list it's endless mode.
    private static readonly Key[] SectorKeys = { Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10 };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var host = new GameObject(nameof(DevShortcuts));
        DontDestroyOnLoad(host);
        host.AddComponent<DevShortcuts>();

        Debug.Log($"[Dev] Shortcuts: F1 +1 level, F2 +{BigLevelStep} levels, " +
                  "F3 skip wave, F4 toggle invincibility, " +
                  $"F5-F{4 + SectorKeys.Length} jump to sector 1-{SectorKeys.Length}, F11 ship-module draft, " +
                  "F12 weapon draft. Each cleared wave logs [Stats]: damage dealt per source.");
    }

    // After the scene's Awake, so GameController's statistics exist.
    private void Start()
    {
        if (GameController.Instance != null)
            GameController.Instance.Statistics.OnWaveEnded += LogWave;
    }

    private void OnDestroy()
    {
        if (GameController.Instance != null)
            GameController.Instance.Statistics.OnWaveEnded -= LogWave;
    }

    // One line per source, highest damage first, then the damage the player took.
    private static void LogWave(StatisticsPeriod wave)
    {
        int number = EnemySpawnController.Instance != null ? EnemySpawnController.Instance.WaveCounter : 0;
        float seconds = Mathf.Max(wave.Duration, 0.01f);
        var report = new StringBuilder($"[Stats] Wave {number}, {seconds:F1} s");

        foreach (var pair in wave.DealtBySource.OrderByDescending(pair => pair.Value.Damage))
        {
            SourceStatistics stats = pair.Value;
            report.Append($"\n  {pair.Key.DisplayName}: {stats.Damage:F0} damage ({stats.Damage / seconds:F1}/s), " +
                          $"shields {stats.ShieldDamage:F0}, hull {stats.HullDamage:F0}, {stats.Kills} kills");
        }

        report.Append($"\n  Taken: shields {wave.ShieldDamageTaken:F0}, hull {wave.HullDamageTaken:F0}");
        Debug.Log(report.ToString());
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.f1Key.wasPressedThisFrame) AddLevels(1);
        if (keyboard.f2Key.wasPressedThisFrame) AddLevels(BigLevelStep);
        if (keyboard.f3Key.wasPressedThisFrame) SkipWave();
        if (keyboard.f4Key.wasPressedThisFrame) ToggleInvincibility();
        if (keyboard.f11Key.wasPressedThisFrame) QueueDraft(ui => ui.QueueShipModuleDraft());
        if (keyboard.f12Key.wasPressedThisFrame) QueueDraft(ui => ui.QueueWeaponDraft());

        for (int i = 0; i < SectorKeys.Length; i++)
            if (keyboard[SectorKeys[i]].wasPressedThisFrame)
                JumpToSector(i + 1);
    }

    private static void JumpToSector(int sector)
    {
        if (EnemySpawnController.Instance != null)
            EnemySpawnController.Instance.JumpToSector(sector);
    }

    private static void AddLevels(int levels)
    {
        if (GameController.Instance != null)
            GameController.Instance.AddLevels(levels);
    }

    private static void SkipWave()
    {
        if (EnemySpawnController.Instance != null)
            EnemySpawnController.Instance.SkipWave();
    }

    private static void QueueDraft(System.Action<GameUIController> queue)
    {
        if (GameController.Instance == null || GameController.Instance.Player == null)
        {
            Debug.Log("[Dev] No player yet.");
            return;
        }

        queue(GameUIController.Instance);
    }

    private void ToggleInvincibility()
    {
        Player player = GameController.Instance != null ? GameController.Instance.Player : null;
        if (player == null)
        {
            Debug.Log("[Dev] No player yet.");
            return;
        }

        if (player.effectManager.IsActive(this))
        {
            player.effectManager.Remove(this);
            player.IgnoresKnockback = false;
            Debug.Log("[Dev] Invincibility off");
            return;
        }

        var tags = new List<TagDefinition> { Resources.Load<TagDefinition>("ModuleTags/ShipCore") };
        var permanent = new ResourceStat(StatType.Duration, float.PositiveInfinity, 0f);
        var invincibility = new StatModifierEffect(StatType.Invincibility, new StatModifier(1f, 0f), tags, permanent);
        player.effectManager.Apply(this, new Effect[] { invincibility });
        player.IgnoresKnockback = true;
        Debug.Log("[Dev] Invincibility on");
    }
}
#endif
