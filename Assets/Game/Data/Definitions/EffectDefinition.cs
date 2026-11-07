using UnityEngine;

namespace ValorChronicle.Data.Definitions
{
    [CreateAssetMenu(
        fileName = "EffectDefinition",
        menuName = "Valor Chronicle/Definitions/Effect")]
    public sealed class EffectDefinition : GameDefinition
    {
        [SerializeField]
        private string displayNameKey;

        [SerializeField]
        private string descriptionKey;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        [Tooltip("Hides this effect from combat status slots while keeping "
            + "its gameplay modifier active.")]
        private bool hideInHud;

        public string DisplayNameKey => displayNameKey;
        public string DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
        public bool HideInHud => hideInHud;
    }
}
