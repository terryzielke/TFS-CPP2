using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Import settings for the Mixamo X Bot files in CharacterFolder.
// Every file is imported as Humanoid, and each "X Bot@<Name>.fbx" clip is renamed to <Name>.
public class PlayerCharacterImporter : AssetPostprocessor
{
    // Clips that should play once instead of looping
    private static readonly string[] OneShotClips = { "Jumping", "Running Jump" };

    private bool IsCharacterAsset => assetPath.StartsWith(PlayerCharacterSetup.CharacterFolder + "/");

    private void OnPreprocessModel()
    {
        if (!IsCharacterAsset) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = assetPath.Contains("@");
    }

    private void OnPreprocessAnimation()
    {
        if (!IsCharacterAsset || !assetPath.Contains("@")) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        string clipName = fileName.Substring(fileName.IndexOf('@') + 1);
        bool loop = System.Array.IndexOf(OneShotClips, clipName) < 0;

        // Keep any Start/End frame trims made in the Animation tab
        ModelImporterClipAnimation[] existing = importer.clipAnimations;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            if (existing.Length > 0 && existing[0].firstFrame >= clip.firstFrame && existing[0].lastFrame <= clip.lastFrame)
            {
                clip.firstFrame = existing[0].firstFrame;
                clip.lastFrame = existing[0].lastFrame;
            }

            clip.name = clipName;
            clip.loopTime = loop;

            // The CharacterController moves the player, so the body has to stay in place.
            // Rotation is baked into the pose so the bot keeps facing forward.
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;

            // XZ drift is NOT baked into the pose: it becomes root motion, which the Animator
            // ignores (applyRootMotion = false). Baking it makes the body slide and snap back each loop.
            clip.lockRootPositionXZ = false;

            // Looping clips stay on the ground. Jump clips leave their vertical motion out
            // so the physics jump is the only thing lifting the player.
            clip.lockRootHeightY = loop;
            clip.keepOriginalPositionY = loop;
            clip.heightFromFeet = loop;
        }
        importer.clipAnimations = clips;
    }
}

// Editor tool: Tools > Setup Player Character
// Builds the player Animator Controller and puts the X Bot model on the Player prefab.
public static class PlayerCharacterSetup
{
    public const string CharacterFolder = "Assets/_Art/Characters/XBot";
    private const string ModelPath = CharacterFolder + "/X Bot.fbx";
    private const string ControllerPath = CharacterFolder + "/PlayerAnimator.controller";
    private const string PrefabPath = "Assets/_Prefabs/Player.prefab";
    private const string ModelChildName = "Model";

    [MenuItem("Tools/Setup Player Character")]
    private static void Setup()
    {
        // Reimport so PlayerCharacterImporter's settings are applied
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { CharacterFolder }))
        {
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        AnimatorController controller = BuildController();
        if (controller == null) return;

        if (SetupPrefab(controller))
        {
            EditorUtility.DisplayDialog("Player Character", "Player prefab now uses the X Bot model and PlayerAnimator controller.", "OK");
        }
    }

    private static AnimationClip LoadClip(string clipName)
    {
        string path = $"{CharacterFolder}/X Bot@{clipName}.fbx";
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
        }
        Debug.LogError($"Could not find an animation clip in {path}");
        return null;
    }

    private static AnimatorController BuildController()
    {
        AnimationClip idle = LoadClip("Idle");
        AnimationClip running = LoadClip("Running");
        AnimationClip runBackward = LoadClip("Run Backward");
        AnimationClip leftStrafe = LoadClip("Left Strafe");
        AnimationClip rightStrafe = LoadClip("Right Strafe");
        AnimationClip jumping = LoadClip("Jumping");
        AnimationClip runningJump = LoadClip("Running Jump");
        if (!idle || !running || !runBackward || !leftStrafe || !rightStrafe || !jumping || !runningJump) return null;

        // Rebuild from scratch so running the tool again gives the same result
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Forward", AnimatorControllerParameterType.Float);
        controller.AddParameter("Strafe", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Locomotion: 2D blend on Strafe (x) and Forward (y)
        AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree);
        tree.blendType = BlendTreeType.FreeformDirectional2D;
        tree.blendParameter = "Strafe";
        tree.blendParameterY = "Forward";
        tree.AddChild(idle, Vector2.zero);
        tree.AddChild(running, new Vector2(0, 1));
        tree.AddChild(runBackward, new Vector2(0, -1));
        tree.AddChild(leftStrafe, new Vector2(-1, 0));
        tree.AddChild(rightStrafe, new Vector2(1, 0));
        sm.defaultState = locomotion;

        AnimatorState jump = sm.AddState("Jump", new Vector3(500, 0, 0));
        jump.motion = jumping;
        AnimatorState runJump = sm.AddState("Running Jump", new Vector3(500, 120, 0));
        runJump.motion = runningJump;

        // Standing still uses the standing jump, moving uses the running jump
        AddJumpTransition(sm, jump, AnimatorConditionMode.Less);
        AddJumpTransition(sm, runJump, AnimatorConditionMode.Greater);

        foreach (AnimatorState state in new[] { jump, runJump })
        {
            AnimatorStateTransition land = state.AddTransition(locomotion);
            land.hasExitTime = false;
            land.duration = 0.15f;
            land.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        }

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void AddJumpTransition(AnimatorStateMachine sm, AnimatorState target, AnimatorConditionMode speedMode)
    {
        // From Any State so holding jump restarts the jump each time the player lands
        AnimatorStateTransition t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false;
        t.duration = 0.1f;
        t.canTransitionToSelf = true;
        t.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        t.AddCondition(speedMode, 0.1f, "Speed");
    }

    private static bool SetupPrefab(AnimatorController controller)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogError($"Could not load {ModelPath}");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // The capsule mesh is replaced by the character model (colliders stay)
            Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(root.GetComponent<MeshFilter>());

            Transform oldModel = root.transform.Find(ModelChildName);
            if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.name = ModelChildName;

            // Put the model's feet at the bottom of the CharacterController
            CharacterController cc = root.GetComponent<CharacterController>();
            float feetY = cc != null ? cc.center.y - cc.height / 2f - cc.skinWidth : -1f;
            instance.transform.SetLocalPositionAndRotation(new Vector3(0, feetY, 0), Quaternion.identity);

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return true;
    }
}
