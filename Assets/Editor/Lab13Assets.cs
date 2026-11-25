using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Лабораторна №13: анімації ворога (з Animation Event), ефект і звуки ударів, розміщення ворогів і NPC у сцені.
/// Частина PrototypeSceneBuilder (partial).
/// </summary>
public static partial class PrototypeSceneBuilder
{
    static AnimatorController enemyController;
    static GameObject fxHit;
    static AudioClip aHit, aStomp, aAlert;

    static void CreateLab13Assets()
    {
        CreateEnemyAnimations();
        fxHit = MakeFx("FX_Hit", MakeMaterial("Mat_Particle", glowTex), 14, 0.25f, 0.5f, 3f, 6f, 0.10f, 0.22f,
            new Color(1f, 0.9f, 0.5f), new Color(1f, 0.3f, 0.2f), 0.6f, 0.15f, false, 0f);
        CreateEnemyAudio();
    }

    // ---------------- анімації ворога ----------------

    static void CreateEnemyAnimations()
    {
        const float H = 1.0f;   // базова висота ворога
        var idle = MakeClip("Enemy_Idle", new[] { 0f, 0.5f, 1f },
            new[] { 0.90f, 0.88f, 0.90f }, new[] { 1.00f, 1.05f, 1.00f }, true, H);
        var walk = MakeClip("Enemy_Walk", new[] { 0f, 0.3f, 0.6f },
            new[] { 0.90f, 0.95f, 0.90f }, new[] { 1.00f, 0.94f, 1.00f }, true, H);
        var run = MakeClip("Enemy_Run", new[] { 0f, 0.15f, 0.3f },
            new[] { 0.90f, 1.02f, 0.90f }, new[] { 1.00f, 0.88f, 1.00f }, true, H);
        // Атака: замах (присідання) -> випад (удар у момент події OnAttackHit) -> повернення
        var attack = MakeClip("Enemy_Attack", new[] { 0f, 0.2f, 0.35f, 0.5f, 0.7f },
            new[] { 0.90f, 1.05f, 0.80f, 1.00f, 0.90f }, new[] { 1.00f, 0.85f, 1.25f, 0.95f, 1.00f }, false, H,
            0.35f, "OnAttackHit");
        var retreat = MakeClip("Enemy_Retreat", new[] { 0f, 0.2f, 0.4f },
            new[] { 0.80f, 0.98f, 0.80f }, new[] { 1.10f, 0.90f, 1.10f }, true, H);

        string cpath = AnimDir + "/Enemy.controller";
        AssetDatabase.DeleteAsset(cpath);
        enemyController = AnimatorController.CreateAnimatorControllerAtPath(cpath);
        enemyController.AddParameter("State", AnimatorControllerParameterType.Int);

        var sm = enemyController.layers[0].stateMachine;
        AnimatorState[] states =
        {
            sm.AddState("Idle"), sm.AddState("Patrol"), sm.AddState("Chase"), sm.AddState("Attack"), sm.AddState("Retreat")
        };
        states[0].motion = idle;
        states[1].motion = walk;
        states[2].motion = run;
        states[3].motion = attack;
        states[4].motion = retreat;
        sm.defaultState = states[1];

        for (int i = 0; i < states.Length; i++)
        {
            var t = sm.AddAnyStateTransition(states[i]);
            t.hasExitTime = false;
            t.duration = 0.05f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.Equals, i, "State");
        }

        EditorUtility.SetDirty(enemyController);
        AssetDatabase.SaveAssets();
    }

    // ---------------- звуки ----------------

    static void CreateEnemyAudio()
    {
        var rng = new System.Random(13);
        Func<float> noise = () => (float)(rng.NextDouble() * 2.0 - 1.0);
        const float TwoPi = 2f * Mathf.PI;

        aHit = MakeWav("sfx_hit", 0.18f, t =>
            (noise() * 0.5f * Mathf.Exp(-t * 35f) + Mathf.Sin(TwoPi * 140f * t) * Mathf.Exp(-t * 18f)) * 0.9f);

        aStomp = MakeWav("sfx_stomp", 0.15f, t =>
            (Mathf.Sin(TwoPi * (260f * t - 900f * t * t)) * Mathf.Exp(-t * 20f) + Mathf.Sin(TwoPi * 80f * t) * Mathf.Exp(-t * 30f)) * 0.8f);

        aAlert = MakeWav("sfx_alert", 0.25f, t =>
        {
            float f = t < 0.1f ? 600f : (t < 0.12f ? 0f : 900f);
            float env = (t < 0.1f ? 1f : (t < 0.12f ? 0f : Mathf.Clamp01(1f - (t - 0.12f) / 0.13f)));
            return Mathf.Sin(TwoPi * f * t) * env * 0.4f;
        });
    }

    // ---------------- вороги та NPC у сцені ----------------

    static void BuildEnemies()
    {
        var parent = new GameObject("Enemies");
        BuildEnemy("Guard_1", 4f, 1.5f, 6.5f, parent.transform);     // на лівій ділянці землі
        BuildEnemy("Guard_2", 23f, 22f, 25.5f, parent.transform);    // біля важеля перед виходом
    }

    static void BuildEnemy(string name, float x, float leftX, float rightX, Transform parent)
    {
        const float y = -3.0f;
        var enemy = new GameObject(name);
        enemy.transform.position = new Vector3(x, y, 0f);
        enemy.transform.SetParent(parent, true);

        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.mass = 20f;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var bc = enemy.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.9f, 1.0f);
        bc.sharedMaterial = noFriction;

        var visual = Box("Visual", new Vector2(x, y), new Vector2(0.9f, 1.0f), new Color(0.85f, 0.30f, 0.25f), 4, false, enemy.transform);
        var eye = Box("Eye", Vector2.zero, new Vector2(0.28f, 0.2f), new Color(1f, 0.9f, 0.3f), 6, false, visual.transform);
        eye.transform.localPosition = new Vector3(0.25f, 0.25f, 0f);

        var animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = enemyController;
        var events = visual.AddComponent<EnemyVisualEvents>();

        var ai = enemy.AddComponent<EnemyAI>();
        ai.leftX = leftX;
        ai.rightX = rightX;
        ai.animator = animator;
        ai.sprite = visual.GetComponent<SpriteRenderer>();
        ai.eye = eye.transform;
        events.ai = ai;
    }

    static void BuildNpc()
    {
        var npc = new GameObject("NPC_Clockmaker");
        npc.transform.position = new Vector3(-9.3f, -2.95f, 0f);

        var trigger = npc.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2f, 2f);

        var visual = Box("Visual", new Vector2(-9.3f, -2.95f), new Vector2(0.8f, 1.1f), new Color(0.62f, 0.46f, 0.82f), 4, false, npc.transform);
        var eye = Box("Eye", Vector2.zero, new Vector2(0.28f, 0.2f), Color.white, 6, false, visual.transform);
        eye.transform.localPosition = new Vector3(0.25f, 0.25f, 0f);
        var hat = Box("Hat", Vector2.zero, new Vector2(1.1f, 0.25f), new Color(0.25f, 0.18f, 0.12f), 6, false, visual.transform);
        hat.transform.localPosition = new Vector3(0f, 0.6f, 0f);

        var dlg = npc.AddComponent<DialogueNPC>();
        dlg.visual = visual.transform;
    }
}
