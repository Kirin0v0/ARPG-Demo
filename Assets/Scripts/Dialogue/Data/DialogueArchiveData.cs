using System;
using System.Collections.Generic;

namespace Dialogue.Data
{
    [Serializable]
    public class DialogueArchiveData
    {
        public Dictionary<string, string> parameters = new(); // 键为对话树参数的唯一标识符，值为持久化数据
    }
}