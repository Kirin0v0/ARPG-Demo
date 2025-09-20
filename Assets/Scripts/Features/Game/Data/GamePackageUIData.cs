using Package.Data;
using Package.Runtime;

namespace Features.Game.Data
{
    public class GamePackageUIData
    {
        public PackageGroup PackageGroup;
        public bool MultipleSelected; // 是否多选
        public bool Focused; // 是否聚焦
    }

    public class GamePackagePlaceholderUIData
    {
        public bool Focused;
    }
}