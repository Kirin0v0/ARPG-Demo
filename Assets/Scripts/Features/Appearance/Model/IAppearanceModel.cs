using System.Collections.Generic;
using Character.Data;
using Features.Appearance.Data;
using Framework.Core.LiveData;
using Humanoid;
using Humanoid.Data;

namespace Features.Appearance.Model
{
    public interface IAppearanceModel
    {
        LiveData<HumanoidCharacterRace> GetSelectedRace();
        LiveData<AppearanceDefaultModelData?> GetSelectedHair();
        LiveData<AppearanceDefaultModelData?> GetSelectedHead();
        LiveData<AppearanceDefaultModelData?> GetSelectedEyebrow();
        LiveData<AppearanceDefaultModelData?> GetSelectedFacialHair();
        LiveData<HumanoidAppearanceColor> GetConfigurationColor();
        LiveData<HumanoidAppearanceData> GetAppearance();

        void SelectRace(HumanoidCharacterRace race);
        void SelectPreviousHair();
        void SelectNextHair();
        void SelectPreviousHead();
        void SelectNextHead();
        void SelectPreviousEyebrow();
        void SelectNextEyebrow();
        void SelectPreviousFacialHair();
        void SelectNextFacialHair();
        void SetConfigurationColor(HumanoidAppearanceColor color);
        void RandomHeadPart();
        void RandomBodyPart();
    }
}