using Humanoid.Model.Data;

namespace Features.Appearance.Data
{
    public enum AppearanceModelType
    {
        Hair,
        Head,
        Eyebrow,
        FacialHair,
        Others
    }
    
    public struct AppearanceDefaultModelData
    {
        public HumanoidModelInfoData ModelInfo;
        public string Alias;
        public AppearanceModelType Type;
    }
}