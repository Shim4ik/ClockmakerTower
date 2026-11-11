using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Лабораторна №12: генерація анімацій (Animator Controller), префабів Particle System та звуків (WAV).
/// Частина PrototypeSceneBuilder (partial).
/// </summary>
public static partial class PrototypeSceneBuilder
{
    const string AnimDir = "Assets/Animations";
    const string PrefabDir = "Assets/Prefabs";
    const string AudioDir = "Assets/Audio";

    static AnimatorController animController;
    static GameObject fxGear, fxDust, fxSteam, fxDeath;
    static AudioClip aJump, aLand, aGear, aStepStone, aStepWood, aLever, aDoor, aDeath, aLamp, aAmbient;

    static void CreateLab12Assets()
    {
        EnsureDir(AnimDir);
        EnsureDir(PrefabDir);
        EnsureDir(AudioDir);
        CreateAnimations();
        CreateEffects();
        CreateAudio();
    }

    // ======================= АНІМАЦІЇ =======================

    static AnimationClip MakeClip(string name, float[] times, float[] sx, float[] sy, bool loop)
    {
        var clip = new AnimationClip { frameRate = 30f };
        var kx = new Keyframe[times.Length];
        var ky = new Keyframe[times.Length];
        var kp = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++)
        {
            kx[i] = new Keyframe(times[i], sx[i]);
            ky[i] = new Keyframe(times[i], sy[i]);
            // Зсув по Y, щоб «ноги» залишались на землі при зміні висоти
            kp[i] = new Keyframe(times[i], (sy[i] - 1.1f) * 0.5f);
        }
        clip.SetCurve("", typeof(Transform), "localScale.x", new AnimationCurve(kx));
        clip.SetCurve("", typeof(Transform), "localScale.y", new AnimationCurve(ky));
        clip.SetCurve("", typeof(Transform), "localPosition.y", new AnimationCurve(kp));

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = AnimDir + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void CreateAnimations()
    {
        var idleClip = MakeClip("Player_Idle", new[] { 0f, 0.6f, 1.2f }, new[] { 0.80f, 0.77f, 0.80f }, new[] { 1.10f, 1.16f, 1.10f }, true);
        var runClip = MakeClip("Player_Run", new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f }, new[] { 0.80f, 0.90f, 0.80f, 0.90f, 0.80f }, new[] { 1.10f, 0.98f, 1.10f, 0.98f, 1.10f }, true);
        var jumpClip = MakeClip("Player_Jump", new[] { 0f, 0.12f, 0.3f }, new[] { 0.80f, 0.68f, 0.68f }, new[] { 1.10f, 1.32f, 1.32f }, false);
        var fallClip = MakeClip("Player_Fall", new[] { 0f, 0.2f, 0.4f }, new[] { 0.72f, 0.75f, 0.72f }, new[] { 1.22f, 1.18f, 1.22f }, true);

        string cpath = AnimDir + "/Player.controller";
        AssetDatabase.DeleteAsset(cpath);
        animController = AnimatorController.CreateAnimatorControllerAtPath(cpath);
        animController.AddParameter("Speed", AnimatorControllerParameterType.Float);
        animController.AddParameter("VelocityY", AnimatorControllerParameterType.Float);
        animController.AddParameter("Grounded", AnimatorControllerParameterType.Bool);

        var sm = animController.layers[0].stateMachine;
        var idle = sm.AddState("Idle");
        var run = sm.AddState("Run");
        var jump = sm.AddState("Jump");
        var fall = sm.AddState("Fall");
        idle.motion = idleClip;
        run.motion = runClip;
        jump.motion = jumpClip;
        fall.motion = fallClip;
        sm.defaultState = idle;

        AnimatorStateTransition t;

        t = idle.AddTransition(run);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        t = run.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        t = sm.AddAnyStateTransition(jump);
        t.hasExitTime = false; t.duration = 0.05f; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");

        t = sm.AddAnyStateTransition(fall);
        t.hasExitTime = false; t.duration = 0.05f; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        t.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocityY");

        t = jump.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

        t = fall.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

        EditorUtility.SetDirty(animController);
        AssetDatabase.SaveAssets();
    }

    // ======================= ЧАСТИНКИ =======================

    static GameObject MakeFx(string name, Material mat, int count,
        float lifeMin, float lifeMax, float speedMin, float speedMax, float sizeMin, float sizeMax,
        Color c1, Color c2, float gravity, float radius, bool cone, float endScale)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.stopAction = ParticleSystemStopAction.Destroy;   // самознищення після завершення

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled = true;
        if (cone)
        {
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = radius;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // конус спрямований угору
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
        }

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorLife.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, endScale)));

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = mat;
        rend.sortingOrder = 30;

        string path = PrefabDir + "/" + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static void CreateEffects()
    {
        Material mat = MakeMaterial("Mat_Particle", glowTex);

        fxGear = MakeFx("FX_GearPickup", mat, 20, 0.4f, 0.8f, 2f, 5f, 0.12f, 0.28f,
            new Color(1f, 0.85f, 0.25f), new Color(1f, 0.6f, 0.15f), 0.5f, 0.1f, false, 0f);

        fxDust = MakeFx("FX_Dust", mat, 8, 0.3f, 0.5f, 0.5f, 1.5f, 0.15f, 0.3f,
            new Color(0.85f, 0.75f, 0.65f, 0.8f), new Color(0.7f, 0.6f, 0.5f, 0.8f), -0.05f, 0.3f, false, 1.5f);

        fxSteam = MakeFx("FX_Steam", mat, 25, 0.8f, 1.4f, 1.5f, 3f, 0.3f, 0.6f,
            new Color(0.95f, 0.95f, 0.95f, 0.7f), new Color(0.8f, 0.85f, 0.9f, 0.6f), -0.1f, 0.4f, true, 2f);

        fxDeath = MakeFx("FX_Death", mat, 40, 0.5f, 1f, 3f, 8f, 0.15f, 0.35f,
            new Color(1f, 0.45f, 0.1f), new Color(0.9f, 0.15f, 0.1f), 1.2f, 0.2f, false, 0f);
    }

    // ======================= ЗВУК =======================

    static AudioClip MakeWav(string name, float seconds, Func<float, float> sample)
    {
        const int sampleRate = 22050;
        int n = Mathf.RoundToInt(seconds * sampleRate);
        string path = AudioDir + "/" + name + ".wav";

        using (var fs = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            int dataLen = n * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataLen);
            w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);            // PCM
            w.Write((short)1);            // моно
            w.Write(sampleRate);
            w.Write(sampleRate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataLen);
            for (int i = 0; i < n; i++)
            {
                float v = Mathf.Clamp(sample(i / (float)sampleRate), -1f, 1f);
                w.Write((short)(v * 30000f));
            }
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    static void CreateAudio()
    {
        var rng = new System.Random(12);
        Func<float> noise = () => (float)(rng.NextDouble() * 2.0 - 1.0);
        const float TwoPi = 2f * Mathf.PI;

        aJump = MakeWav("sfx_jump", 0.2f, t =>
            Mathf.Sin(TwoPi * (300f * t + 1000f * t * t)) * Mathf.Clamp01(1f - t / 0.2f) * 0.6f);

        aLand = MakeWav("sfx_land", 0.18f, t =>
            (Mathf.Sin(TwoPi * 90f * t) * Mathf.Exp(-t * 25f) + noise() * 0.3f * Mathf.Exp(-t * 60f)) * 0.9f);

        aGear = MakeWav("sfx_gear", 0.35f, t =>
        {
            float a = Mathf.Sin(TwoPi * 880f * t) * Mathf.Exp(-t * 12f);
            float b = t > 0.08f ? Mathf.Sin(TwoPi * 1320f * (t - 0.08f)) * Mathf.Exp(-(t - 0.08f) * 10f) : 0f;
            return (a + b) * 0.45f;
        });

        float lp = 0f;
        aStepStone = MakeWav("sfx_step_stone", 0.07f, t =>
        {
            lp = lp * 0.6f + noise() * 0.4f;
            return lp * Mathf.Exp(-t * 70f) * 1.2f;
        });

        aStepWood = MakeWav("sfx_step_wood", 0.09f, t =>
            (Mathf.Sin(TwoPi * 170f * t) * Mathf.Exp(-t * 45f) * 0.8f + noise() * 0.15f * Mathf.Exp(-t * 80f)));

        aLever = MakeWav("sfx_lever", 0.2f, t =>
            noise() * 0.6f * Mathf.Exp(-t * 90f) + Mathf.Sin(TwoPi * 220f * t) * Mathf.Exp(-t * 20f) * 0.4f);

        float lp2 = 0f;
        aDoor = MakeWav("sfx_door", 1.2f, t =>
        {
            lp2 = lp2 * 0.9f + noise() * 0.1f;
            float env = Mathf.Sin(Mathf.PI * t / 1.2f);
            return (Mathf.Sin(TwoPi * 60f * t) * 0.5f + lp2 * 2.5f + Mathf.Sin(TwoPi * 220f * t) * 0.08f) * env * 0.7f;
        });

        aDeath = MakeWav("sfx_death", 0.5f, t =>
        {
            float ph = TwoPi * (500f * t - 380f * t * t);
            return (Mathf.Sin(ph) + 0.3f * Mathf.Sin(2f * ph)) * Mathf.Clamp01(1f - t / 0.5f) * 0.5f;
        });

        aLamp = MakeWav("sfx_lamp", 0.06f, t =>
            Mathf.Sin(TwoPi * 1200f * t) * Mathf.Exp(-t * 60f) * 0.5f);

        // Фоновий гул вежі + цокання годинника (4 с, зациклюється без швів)
        aAmbient = MakeWav("amb_tower", 4f, t =>
        {
            float amp = 0.8f + 0.2f * Mathf.Sin(TwoPi * 0.25f * t);
            float drone = (0.25f * Mathf.Sin(TwoPi * 56f * t) + 0.12f * Mathf.Sin(TwoPi * 112f * t) + 0.06f * Mathf.Sin(TwoPi * 168f * t)) * amp;
            int idx = (int)(t / 0.5f);
            float k = t - idx * 0.5f;
            float f = (idx % 2 == 0) ? 1800f : 1500f;
            float tick = (noise() * 0.25f + 0.2f * Mathf.Sin(TwoPi * f * k)) * Mathf.Exp(-k * 110f);
            return drone * 0.8f + tick;
        });
    }

    // ======================= МЕНЕДЖЕРИ У СЦЕНІ =======================

    static void BuildManagers()
    {
        var audio = new GameObject("AudioManager").AddComponent<AudioManager>();
        audio.jump = aJump;
        audio.land = aLand;
        audio.stepStone = aStepStone;
        audio.stepWood = aStepWood;
        audio.gear = aGear;
        audio.lever = aLever;
        audio.door = aDoor;
        audio.death = aDeath;
        audio.lamp = aLamp;
        audio.ambient = aAmbient;

        var fx = new GameObject("EffectsManager").AddComponent<EffectsManager>();
        fx.gearFx = fxGear;
        fx.dustFx = fxDust;
        fx.steamFx = fxSteam;
        fx.deathFx = fxDeath;
    }
}
