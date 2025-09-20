namespace Camera.Data
{
    public enum CameraScene
    {
        Normal,
        Selection,
        Custom,
    }

    public struct CameraSceneData
    {
        private const int SelectionFlag = 1 << 0;
        private const int CustomFlag = 1 << 1;

        private int _flags;

        public CameraScene Scene
        {
            get
            {
                if ((_flags & CustomFlag) != 0)
                {
                    return CameraScene.Custom;
                }
                else if ((_flags & SelectionFlag) != 0)
                {
                    return CameraScene.Selection;
                }
                else
                {
                    return CameraScene.Normal;
                }
            }
        }

        public CameraSceneData EnterCustom()
        {
            return new CameraSceneData
            {
                _flags = _flags | CustomFlag
            };
        }
        
        public CameraSceneData ExitCustom()
        {
            return new CameraSceneData
            {
                _flags = _flags & ~CustomFlag
            };
        }

        public CameraSceneData EnterSelection()
        {
            return new CameraSceneData
            {
                _flags = _flags | SelectionFlag
            };
        }
        
        public CameraSceneData ExitSelection()
        {
            return new CameraSceneData
            {
                _flags = _flags & ~SelectionFlag
            };
        }

    }
}