#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Testing shortcuts, only in the editor and development builds. Installs itself, so the scene doesn't change.
public class DevShortcuts : MonoBehaviour
{
    private const int BigLevelStep = 5;
    // Jump to the start of sector 1, 2, ... Past the sector list it's endless mode.
    private static readonly Key[] SectorKeys = { Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10 };

    private StatModifierEffect invincibilityEffect;
    private Player invinciblePlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var host = new GameObject(nameof(DevShortcuts));
        DontDestroyOnLoad(host);
        host.AddComponent<DevShortcuts>();

        Debug.Log($"[Dev] Shortcuts: F1 +1 level, F2 +{BigLevelStep} levels, " +
                  "F3 skip wave, F4 toggle invincibility, " +
                  $"F5-F{4 + SectorKeys.Length} jump to sector 1-{SectorKeys.Length}");
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

    private void ToggleInvincibility()
    {
        Player player = GameController.Instance != null ? GameController.Instance.Player : null;
        if (player == null)
        {
            Debug.Log("[Dev] No player yet.");
            return;
        }

        if (invincibilityEffect != null && invinciblePlayer == player)
        {
            player.effectManager.RemoveEffect(invincibilityEffect);
            player.IgnoresKnockback = false;
            invincibilityEffect = null;
            Debug.Log("[Dev] Invincibility off");
            return;
        }

        var tags = new List<TagDefinition> { Resources.Load<TagDefinition>("ModuleTags/ShipCore") };
        var permanent = new ResourceStat(StatType.Duration, float.PositiveInfinity, 0f);
        invincibilityEffect = new StatModifierEffect(StatType.Invincibility, new StatModifier(1f, 0f), tags, permanent);
        invinciblePlayer = player;
        player.effectManager.AddEffect(invincibilityEffect);
        player.IgnoresKnockback = true;
        Debug.Log("[Dev] Invincibility on");
    }
}
#endif
