using UnityEngine;

namespace Character.Ability.Appearance
{
    public class SlimeAppearanceAbility : CharacterAppearanceAbility
    {
        [SerializeField] protected SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material redMaterial;
        [SerializeField] private Material yellowMaterial;
        [SerializeField] private Material blueMaterial;
        [SerializeField] private Material greenMaterial;

        public override void SetAppearance(object[] payload)
        {
            if (payload == null || payload.Length == 0 || payload[0] is not string material)
            {
                return;
            }

            switch (material)
            {
                case "red":
                    skinnedMeshRenderer.material = redMaterial;
                    break;
                case "yellow":
                    skinnedMeshRenderer.material = yellowMaterial;
                    break;
                case "blue":
                    skinnedMeshRenderer.material = blueMaterial;
                    break;
                case "green":
                    skinnedMeshRenderer.material = greenMaterial;
                    break;
                default:
                    skinnedMeshRenderer.material = defaultMaterial;
                    break;
            }
        }
    }
}