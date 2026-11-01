#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TikTokLiveGame.Editor
{
    public sealed class DjModelImporter : AssetPostprocessor
    {
        private bool IsDjModel => assetPath.StartsWith("Assets/Resources/DJ/") && assetPath.EndsWith(".fbx");
        public override uint GetVersion() => 4;

        private void OnPreprocessModel()
        {
            if (!IsDjModel) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            bool mint = assetPath.Contains("/DJ/Mint/");
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            HumanDescription description = importer.humanDescription;
            // Do not reuse a cached skeleton description from Mint's accessory armatures.
            description.skeleton = System.Array.Empty<SkeletonBone>();
            var bones = new System.Collections.Generic.List<HumanBone>();
            string[] human = { "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
                "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
                "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes", "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes" };
            string[] mintBones = { "Pelvis", "Spine", "Spine1", "Spine2", "Neck", "Head",
                "L-Clavicle", "L-UpperArm", "L-Forearm", "L-Hand", "R-Clavicle", "R-UpperArm", "R-Forearm", "R-Hand",
                "L-Thigh", "L-Calf", "L-Foot", "L-Toe0", "R-Thigh", "R-Calf", "R-Foot", "R-Toe0" };
            string[] meganBones = { "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head",
                "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm", "RightForeArm", "RightHand",
                "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase" };
            for (int i = 0; i < human.Length; i++)
                bones.Add(new HumanBone { humanName = human[i], boneName = mint ? "Bip001-" + mintBones[i] : "mixamorig2:" + meganBones[i], limit = new HumanLimit { useDefaultValues = true } });
            description.human = bones.ToArray();
            description.armStretch = 0.05f;
            description.legStretch = 0.05f;
            description.upperArmTwist = description.lowerArmTwist = 0.5f;
            description.upperLegTwist = description.lowerLegTwist = 0.5f;
            importer.humanDescription = description;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsDjModel) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            for (int index = 0; index < clips.Length; index++)
            {
                clips[index].name = index == 0 ? "DJ Performance" : $"DJ Performance {index + 1}";
                clips[index].loopTime = true;
                clips[index].loopPose = true;
                clips[index].keepOriginalPositionXZ = true;
                clips[index].keepOriginalPositionY = true;
                clips[index].keepOriginalOrientation = true;
            }
            importer.clipAnimations = clips;
        }

        private void OnPostprocessModel(GameObject model)
        {
            if (!IsDjModel) return;
            foreach (Renderer modelRenderer in model.GetComponentsInChildren<Renderer>(true))
            {
                modelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                modelRenderer.receiveShadows = true;
            }
        }
    }
}
#endif
