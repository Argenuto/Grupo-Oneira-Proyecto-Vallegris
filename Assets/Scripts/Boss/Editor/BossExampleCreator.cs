#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BossExampleCreator
{
    [MenuItem("Tools/Boss/Crear patrón de ejemplo")]
    public static void CreateExample()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Boss"))
            AssetDatabase.CreateFolder("Assets/Data", "Boss");

        var pattern = ScriptableObject.CreateInstance<BossPattern>();
        pattern.loopMode = BossPattern.LoopMode.Loop;
        pattern.delayBetweenLoops = 0.5f;
        pattern.actions.Add(new PatrolAction { passes = 2, speed = 5f, damageOnContact = true, telegraphDuration = 0.5f });
        pattern.actions.Add(new WaitAction { duration = 0.5f });
        pattern.actions.Add(new ProjectileBurstAction { count = 5, spreadAngle = 50f, projectileSpeed = 8f, telegraphDuration = 0.8f });
        pattern.actions.Add(new WaitAction { duration = 1f });

        AssetDatabase.CreateAsset(pattern, "Assets/Data/Boss/BossPattern_Walker.asset");
        AssetDatabase.SaveAssets();
        Selection.activeObject = pattern;
    }
}
#endif
